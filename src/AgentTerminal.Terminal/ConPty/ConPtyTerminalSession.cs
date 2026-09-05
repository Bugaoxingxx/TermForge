using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;
using AgentTerminal.Infrastructure.Win32;
using Microsoft.Win32.SafeHandles;

namespace AgentTerminal.Terminal.ConPty;

/// <summary>
/// 基于 Windows ConPTY (PseudoConsole) 的终端底层会话实现。
/// 严格遵循 ITerminalSession 契约与生命周期转移规范，集成 Job Object 守护与有状态 UTF-8 解码。
/// </summary>
public sealed class ConPtyTerminalSession : ITerminalSession
{
    private readonly ShellProfile _profile;
    private readonly JobObjectHelper _jobObject;
    private readonly bool _ownsJobObject;
    private readonly object _stateLock = new();

    private IntPtr _hPC = IntPtr.Zero;
    private IntPtr _hpcPtr = IntPtr.Zero;
    private IntPtr _hProcess = IntPtr.Zero;
    private IntPtr _hThread = IntPtr.Zero;
    private IntPtr _lpAttributeList = IntPtr.Zero;

    private SafeFileHandle? _inputWriteHandle;
    private SafeFileHandle? _outputReadHandle;
    private FileStream? _inputStream;
    private FileStream? _outputStream;

    private Task? _readTask;
    private Task? _waitExitTask;
    private CancellationTokenSource? _cts;
    private bool _processExitedFired;
    private bool _disposed;

    public string Id { get; }

    public string Title { get; private set; }

    public TerminalState State { get; private set; } = TerminalState.Created;

    public TerminalDimensions Dimensions { get; private set; }

    public int? ExitCode { get; private set; }

    public int? ProcessId { get; private set; }

    public event EventHandler<string>? OutputReceived;

    public event EventHandler<byte[]>? BinaryOutputReceived;

    public event EventHandler<int>? ProcessExited;

    public event EventHandler<TerminalState>? StateChanged;

    public ConPtyTerminalSession(
        ShellProfile profile,
        TerminalDimensions? dimensions = null,
        JobObjectHelper? jobObject = null,
        string? id = null)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Id = id ?? Guid.NewGuid().ToString("N");
        Title = string.IsNullOrWhiteSpace(profile.Name) ? "PowerShell" : profile.Name;
        Dimensions = dimensions ?? TerminalDimensions.Default;

        if (jobObject != null)
        {
            _jobObject = jobObject;
            _ownsJobObject = false;
        }
        else
        {
            _jobObject = new JobObjectHelper();
            _ownsJobObject = true;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            if (State != TerminalState.Created)
            {
                throw new InvalidOperationException($"Session cannot be started from state '{State}'. It must be in 'Created' state.");
            }
            SetState(TerminalState.Starting);
        }

        _cts = new CancellationTokenSource();

        try
        {
            // 校验工作目录
            string? workingDir = _profile.WorkingDirectory;
            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                if (!Directory.Exists(workingDir))
                {
                    throw new DirectoryNotFoundException($"Working directory does not exist: {workingDir}");
                }
            }
            else
            {
                workingDir = Environment.CurrentDirectory;
            }

            // 构造安全属性
            var sa = new PseudoConsoleApi.SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<PseudoConsoleApi.SECURITY_ATTRIBUTES>(),
                bInheritHandle = false,
                lpSecurityDescriptor = IntPtr.Zero
            };

            // 创建用于与 ConPTY 通信的管道
            // ConPTY 读取 inputRead，我们向 inputWrite 写入
            if (!PseudoConsoleApi.CreatePipe(out var hInputRead, out var hInputWrite, ref sa, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create input pipe.");
            }

            // ConPTY 向 outputWrite 写入，我们从 outputRead 读取
            if (!PseudoConsoleApi.CreatePipe(out var hOutputRead, out var hOutputWrite, ref sa, 0))
            {
                PseudoConsoleApi.CloseHandle(hInputRead);
                PseudoConsoleApi.CloseHandle(hInputWrite);
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create output pipe.");
            }

            // 创建 PseudoConsole
            var coord = new PseudoConsoleApi.COORD((short)Dimensions.Columns, (short)Dimensions.Rows);
            int hr = PseudoConsoleApi.CreatePseudoConsole(coord, hInputRead, hOutputWrite, 0, out _hPC);

            if (hr != 0)
            {
                PseudoConsoleApi.CloseHandle(hInputRead);
                PseudoConsoleApi.CloseHandle(hOutputWrite);
                PseudoConsoleApi.CloseHandle(hInputWrite);
                PseudoConsoleApi.CloseHandle(hOutputRead);
                throw new Win32Exception(hr, $"CreatePseudoConsole failed with HRESULT 0x{hr:X8}.");
            }

            _inputWriteHandle = new SafeFileHandle(hInputWrite, ownsHandle: true);
            _outputReadHandle = new SafeFileHandle(hOutputRead, ownsHandle: true);
            _inputStream = new FileStream(_inputWriteHandle, FileAccess.Write, 4096, isAsync: false);
            _outputStream = new FileStream(_outputReadHandle, FileAccess.Read, 4096, isAsync: false);

            // 初始化进程属性列表并绑定 ConPTY
            IntPtr lpSize = IntPtr.Zero;
            PseudoConsoleApi.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
            _lpAttributeList = Marshal.AllocHGlobal(lpSize);
            if (!PseudoConsoleApi.InitializeProcThreadAttributeList(_lpAttributeList, 1, 0, ref lpSize))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "InitializeProcThreadAttributeList failed.");
            }

            if (!PseudoConsoleApi.UpdateProcThreadAttribute(
                _lpAttributeList,
                0,
                (IntPtr)PseudoConsoleApi.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                _hPC,
                (IntPtr)IntPtr.Size,
                IntPtr.Zero,
                IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateProcThreadAttribute failed.");
            }

            // 组装 StartupInfoEx
            var startupInfo = new PseudoConsoleApi.STARTUPINFOEX();
            startupInfo.StartupInfo.cb = Marshal.SizeOf<PseudoConsoleApi.STARTUPINFOEX>();
            // Prevent redirected parent standard handles (e.g. vstest pipes) from
            // bypassing ConPTY. Null standard handles let the pseudoconsole supply them.
            // https://github.com/microsoft/terminal/discussions/15814
            startupInfo.StartupInfo.dwFlags = PseudoConsoleApi.STARTF_USESTDHANDLES;
            startupInfo.StartupInfo.hStdInput = IntPtr.Zero;
            startupInfo.StartupInfo.hStdOutput = IntPtr.Zero;
            startupInfo.StartupInfo.hStdError = IntPtr.Zero;
            startupInfo.lpAttributeList = _lpAttributeList;

            IntPtr pStartupInfo = Marshal.AllocHGlobal(startupInfo.StartupInfo.cb);
            Marshal.StructureToPtr(startupInfo, pStartupInfo, false);

            // 构造命令行（处理空格路径）
            string executable = _profile.ExecutablePath;
            string commandLine = string.IsNullOrWhiteSpace(_profile.Arguments)
                ? (executable.Contains(' ') ? $"\"{executable}\"" : executable)
                : (executable.Contains(' ') ? $"\"{executable}\" {_profile.Arguments}" : $"{executable} {_profile.Arguments}");

            // 构造环境变量块（合并并覆盖，不污染宿主）
            IntPtr envBlock = CreateEnvironmentBlock(_profile.EnvironmentVariables);

            uint creationFlags = PseudoConsoleApi.EXTENDED_STARTUPINFO_PRESENT | PseudoConsoleApi.CREATE_UNICODE_ENVIRONMENT;

            bool processCreated = false;
            try
            {
                processCreated = PseudoConsoleApi.CreateProcessW(
                    lpApplicationName: null,
                    lpCommandLine: commandLine,
                    lpProcessAttributes: IntPtr.Zero,
                    lpThreadAttributes: IntPtr.Zero,
                    bInheritHandles: false,
                    dwCreationFlags: creationFlags,
                    lpEnvironment: envBlock,
                    lpCurrentDirectory: workingDir,
                    lpStartupInfo: pStartupInfo,
                    lpProcessInformation: out var processInfo);

                if (!processCreated)
                {
                    int win32Error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(win32Error, $"Failed to create terminal process '{commandLine}'. Win32 Error: {win32Error}");
                }

                _hProcess = processInfo.hProcess;
                _hThread = processInfo.hThread;
                ProcessId = processInfo.dwProcessId;

                // 立即将进程加入 Job Object 实施受控保护
                if (!_jobObject.AssignProcess(_hProcess))
                {
                    throw new InvalidOperationException("Failed to assign terminal process to Job Object.");
                }
            }
            finally
            {
                if (pStartupInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pStartupInfo);
                }

                // ConPTY 建立与进程启动完成后，释放主进程持有的管道对端句柄
                if (hInputRead != IntPtr.Zero) PseudoConsoleApi.CloseHandle(hInputRead);
                if (hOutputWrite != IntPtr.Zero) PseudoConsoleApi.CloseHandle(hOutputWrite);

                if (envBlock != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(envBlock);
                }
            }

            // 成功启动，转入 Running 状态
            lock (_stateLock)
            {
                SetState(TerminalState.Running);
            }

            // 启动后台流式读取与进程退出监听
            _readTask = Task.Run(ReadOutputLoopAsync);
            _waitExitTask = Task.Run(WaitForProcessExitAsync);
        }
        catch (Exception)
        {
            lock (_stateLock)
            {
                SetState(TerminalState.Failed);
            }
            await CleanupResourcesAsync();
            throw;
        }
    }

    public async Task WriteAsync(string data, CancellationToken cancellationToken = default)
    {
        if (State != TerminalState.Running)
        {
            throw new InvalidOperationException($"Cannot write to terminal in state '{State}'. Session must be Running.");
        }

        if (_inputStream == null || string.IsNullOrEmpty(data))
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(data);
        await _inputStream.WriteAsync(bytes, cancellationToken);
        await _inputStream.FlushAsync(cancellationToken);
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (State != TerminalState.Running)
        {
            throw new InvalidOperationException($"Cannot write to terminal in state '{State}'. Session must be Running.");
        }

        if (_inputStream == null || buffer.IsEmpty)
        {
            return;
        }

        await _inputStream.WriteAsync(buffer, cancellationToken);
        await _inputStream.FlushAsync(cancellationToken);
    }

    public Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken = default)
    {
        if (State != TerminalState.Running)
        {
            throw new InvalidOperationException($"Cannot resize terminal in state '{State}'. Session must be Running.");
        }

        var newDimensions = new TerminalDimensions(columns, rows);

        if (_hPC != IntPtr.Zero)
        {
            var coord = new PseudoConsoleApi.COORD((short)columns, (short)rows);
            int hr = PseudoConsoleApi.ResizePseudoConsole(_hPC, coord);
            if (hr != 0)
            {
                throw new Win32Exception(hr, $"ResizePseudoConsole failed with HRESULT 0x{hr:X8}.");
            }
        }

        Dimensions = newDimensions;
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            if (State == TerminalState.Exited || State == TerminalState.Failed)
            {
                return;
            }

            if (State == TerminalState.Created)
            {
                SetState(TerminalState.Exited);
                return;
            }

            SetState(TerminalState.Stopping);
        }

        _cts?.Cancel();

        // 关闭 ConPTY 会向 Shell 发送 SIGHUP 并断开管道，使读取任务正常终结
        if (_hPC != IntPtr.Zero)
        {
            PseudoConsoleApi.ClosePseudoConsole(_hPC);
            _hPC = IntPtr.Zero;
        }

        await CleanupResourcesAsync();

        lock (_stateLock)
        {
            if (State != TerminalState.Exited)
            {
                SetState(TerminalState.Exited);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await StopAsync();

        if (_ownsJobObject)
        {
            _jobObject.Dispose();
        }
    }

    private async Task ReadOutputLoopAsync()
    {
        if (_outputStream == null) return;

        byte[] buffer = new byte[8192];
        char[] charBuffer = new char[8192];
        var decoder = Encoding.UTF8.GetDecoder();

        try
        {
            while (!_cts!.IsCancellationRequested)
            {
                int bytesRead = await _outputStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                if (bytesRead <= 0)
                {
                    break;
                }

                // 触发原始二进制数据事件
                byte[] rawData = new byte[bytesRead];
                System.Buffer.BlockCopy(buffer, 0, rawData, 0, bytesRead);
                BinaryOutputReceived?.Invoke(this, rawData);

                // 使用 stateful decoder 进行跨分块无损 UTF-8 解码
                int charsDecoded = decoder.GetChars(buffer, 0, bytesRead, charBuffer, 0, flush: false);
                if (charsDecoded > 0)
                {
                    string text = new string(charBuffer, 0, charsDecoded);
                    OutputReceived?.Invoke(this, text);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Exception in terminal read loop: {ex.Message}");
        }
    }

    private void WaitForProcessExitAsync()
    {
        if (_hProcess == IntPtr.Zero) return;

        // 同步等待进程句柄退出
        PseudoConsoleApi.WaitForSingleObject(_hProcess, PseudoConsoleApi.INFINITE);

        int exitCode = 0;
        if (PseudoConsoleApi.GetExitCodeProcess(_hProcess, out var code))
        {
            exitCode = code;
        }

        ExitCode = exitCode;

        lock (_stateLock)
        {
            if (!_processExitedFired)
            {
                _processExitedFired = true;
                ProcessExited?.Invoke(this, exitCode);
            }

            if (State == TerminalState.Running || State == TerminalState.Stopping)
            {
                SetState(TerminalState.Exited);
            }
        }
    }

    private async Task CleanupResourcesAsync()
    {
        try
        {
            if (_inputStream != null)
            {
                await _inputStream.DisposeAsync();
                _inputStream = null;
            }

            if (_outputStream != null)
            {
                await _outputStream.DisposeAsync();
                _outputStream = null;
            }

            _inputWriteHandle?.Dispose();
            _inputWriteHandle = null;

            _outputReadHandle?.Dispose();
            _outputReadHandle = null;

            if (_lpAttributeList != IntPtr.Zero)
            {
                PseudoConsoleApi.DeleteProcThreadAttributeList(_lpAttributeList);
                Marshal.FreeHGlobal(_lpAttributeList);
                _lpAttributeList = IntPtr.Zero;
            }

            if (_hpcPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_hpcPtr);
                _hpcPtr = IntPtr.Zero;
            }

            if (_hThread != IntPtr.Zero)
            {
                PseudoConsoleApi.CloseHandle(_hThread);
                _hThread = IntPtr.Zero;
            }

            if (_hProcess != IntPtr.Zero)
            {
                PseudoConsoleApi.CloseHandle(_hProcess);
                _hProcess = IntPtr.Zero;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning($"Error cleaning up terminal resources: {ex.Message}");
        }
    }

    private void SetState(TerminalState newState)
    {
        if (State != newState)
        {
            State = newState;
            StateChanged?.Invoke(this, newState);
        }
    }

    private static IntPtr CreateEnvironmentBlock(Dictionary<string, string> customVariables)
    {
        var merged = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 继承宿主进程环境变量
        var currentEnv = Environment.GetEnvironmentVariables();
        foreach (System.Collections.DictionaryEntry entry in currentEnv)
        {
            if (entry.Key is string k && entry.Value is string v)
            {
                merged[k] = v;
            }
        }

        // 自定义变量覆盖
        foreach (var (k, v) in customVariables)
        {
            merged[k] = v;
        }

        // 构建 double-null-terminated Unicode string block
        var sb = new StringBuilder();
        foreach (var (k, v) in merged)
        {
            sb.Append(k).Append('=').Append(v).Append('\0');
        }
        sb.Append('\0');

        string block = sb.ToString();
        return Marshal.StringToHGlobalUni(block);
    }
}
