using System.Text;
using System.Windows;
using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;
using AgentTerminal.Terminal.Buffer;
using AgentTerminal.Terminal.VT;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentTerminal.Docking.ViewModels;

/// <summary>
/// 终端文档视图模型，实现 IMdiDocument 与完整 MDI 状态管理。
/// 包含单会话调试交互、有界输出缓冲区、生命周期命令与 UI 节流刷新。
/// </summary>
public partial class TerminalDocumentViewModel : ObservableObject, IMdiDocument, IAsyncDisposable
{
    public const int MaxOutputBufferSize = 1024 * 1024; // 1 MiB 显示缓冲字符上限
    public const int MaxPendingBufferSize = 1024 * 1024; // 1 MiB 待刷新队列字符上限
    private const string TruncationNoticeHeader = "[输出已截断：已达 1 MiB 显示缓冲上限...]\n";

    private readonly Func<ShellProfile, ITerminalSession>? _sessionFactory;
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _pendingBuffer = new();
    private readonly object _bufferLock = new();
    private readonly System.Windows.Threading.DispatcherTimer? _flushTimer;

    private double _restoreLeft = 40.0;
    private double _restoreTop = 40.0;
    private double _restoreWidth = 640.0;
    private double _restoreHeight = 420.0;

    [ObservableProperty]
    private string _id;

    [ObservableProperty]
    private string _title = "PowerShell 7";

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private MdiWindowState _windowState = MdiWindowState.Normal;

    [ObservableProperty]
    private double _left = 40.0;

    [ObservableProperty]
    private double _top = 40.0;

    [ObservableProperty]
    private double _width = 640.0;

    [ObservableProperty]
    private double _height = 420.0;

    [ObservableProperty]
    private int _zIndex = 1;

    [ObservableProperty]
    private ShellProfile _profile;

    [ObservableProperty]
    private TerminalState _state = TerminalState.Created;

    [ObservableProperty]
    private int? _exitCode;

    [ObservableProperty]
    private int? _processId;

    [ObservableProperty]
    private int _columns = 120;

    [ObservableProperty]
    private int _rows = 30;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _outputText = string.Empty;

    [ObservableProperty]
    private bool _isTruncated;

    [ObservableProperty]
    private bool _isDiagnosticMode;

    [ObservableProperty]
    private string _statusMessage = "就绪 (Created)";

    public ITerminalBuffer Buffer { get; }
    public VtParser Parser { get; }

    public ITerminalSession? Session { get; private set; }

    public event EventHandler? RequestClose;

    public bool CanStart => State == TerminalState.Created || State == TerminalState.Exited || State == TerminalState.Failed;
    public bool CanStop => State == TerminalState.Running || State == TerminalState.Starting;
    public bool CanSend => State == TerminalState.Running;
    public bool CanInterrupt => State == TerminalState.Running;
    public bool CanResize => State == TerminalState.Running;

    public TerminalDocumentViewModel(
        ShellProfile? profile = null,
        ITerminalSession? session = null,
        Func<ShellProfile, ITerminalSession>? sessionFactory = null,
        string? id = null,
        string? title = null)
    {
        Id = id ?? Guid.NewGuid().ToString("N");
        Profile = profile ?? ShellProfile.CreatePowerShellCore();
        Title = title ?? Profile.Name;
        _sessionFactory = sessionFactory;

        Buffer = new TerminalBuffer(Columns, Rows);
        Parser = new VtParser(Buffer);
        Parser.TitleChanged += (_, newTitle) =>
        {
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.BeginInvoke(() => Title = newTitle);
                }
                else
                {
                    Title = newTitle;
                }
            }
        };

        if (Application.Current?.Dispatcher != null)
        {
            _flushTimer = new System.Windows.Threading.DispatcherTimer(
                TimeSpan.FromMilliseconds(16),
                System.Windows.Threading.DispatcherPriority.Background,
                OnFlushTimerTick,
                Application.Current.Dispatcher)
            {
                IsEnabled = false
            };
        }

        if (session != null)
        {
            AttachSession(session);
        }
    }

    public void AttachSession(ITerminalSession session)
    {
        if (Session != null && Session != session)
        {
            DetachSession(Session);
        }

        Session = session;
        Columns = session.Dimensions.Columns;
        Rows = session.Dimensions.Rows;
        lock (Buffer.SyncRoot)
        {
            Buffer.Resize(Columns, Rows);
        }
        Buffer.RequestRefresh();
        State = session.State;
        ExitCode = session.ExitCode;
        if (session.ProcessId != null)
        {
            ProcessId = session.ProcessId;
        }

        session.OutputReceived += OnSessionOutputReceived;
        session.StateChanged += OnSessionStateChanged;
        session.ProcessExited += OnSessionProcessExited;

        UpdateCommandStates();
    }

    private void DetachSession(ITerminalSession session)
    {
        session.OutputReceived -= OnSessionOutputReceived;
        session.StateChanged -= OnSessionStateChanged;
        session.ProcessExited -= OnSessionProcessExited;
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        if (!CanStart) return;

        // 若当前已有会话且已退出或失败，彻底释放旧会话实例
        if (Session != null && (Session.State == TerminalState.Exited || Session.State == TerminalState.Failed))
        {
            var oldSession = Session;
            DetachSession(oldSession);
            Session = null;

            try
            {
                await oldSession.StopAsync();
                await oldSession.DisposeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Error disposing old terminal session: {ex.Message}");
            }
        }

        // 若会话未初始化，使用工厂重新创建
        if (Session == null)
        {
            if (_sessionFactory != null)
            {
                var newSession = _sessionFactory(Profile);
                AttachSession(newSession);
            }
            else
            {
                StatusMessage = "未提供会话工厂，无法新建会话。";
                return;
            }
        }

        try
        {
            if (Session == null)
            {
                StatusMessage = "会话实例未初始化。";
                return;
            }

            StatusMessage = "正在启动会话...";
            await Session.StartAsync();
            if (Session.ProcessId != null)
            {
                ProcessId = Session.ProcessId;
            }
            StatusMessage = $"运行中 (PID: {ProcessId?.ToString() ?? "N/A"})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动失败: {ex.Message}";
        }
        finally
        {
            UpdateCommandStates();
        }
    }

    [RelayCommand]
    public async Task SendAsync()
    {
        if (!CanSend || Session == null) return;

        string commandToSend = InputText + "\r\n";
        InputText = string.Empty;

        try
        {
            await Session.WriteAsync(commandToSend);
        }
        catch (Exception ex)
        {
            AppendOutput($"\n[发送错误: {ex.Message}]\n");
        }
    }

    [RelayCommand]
    public async Task InterruptAsync()
    {
        if (!CanInterrupt || Session == null) return;

        try
        {
            // 发送 Ctrl+C (0x03)
            await Session.WriteAsync(new byte[] { 0x03 });
            AppendOutput("^C\n");
        }
        catch (Exception ex)
        {
            AppendOutput($"\n[中断失败: {ex.Message}]\n");
        }
    }

    [RelayCommand]
    public async Task ResizeAsync()
    {
        if (!CanResize || Session == null) return;

        try
        {
            lock (Buffer.SyncRoot)
            {
                Buffer.Resize(Columns, Rows);
            }
            Buffer.RequestRefresh();
            await Session.ResizeAsync(Columns, Rows);
            StatusMessage = $"视口已更新为 {Columns}x{Rows}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"调整尺寸失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task StopAsync()
    {
        if (!CanStop || Session == null) return;

        try
        {
            StatusMessage = "正在停止会话...";
            await Session.StopAsync();
            FlushOutput();
            StatusMessage = "会话已停止";
        }
        catch (Exception ex)
        {
            StatusMessage = $"停止失败: {ex.Message}";
        }
        finally
        {
            UpdateCommandStates();
        }
    }

    [RelayCommand]
    public void ClearOutput()
    {
        lock (_bufferLock)
        {
            lock (Buffer.SyncRoot)
            {
                Buffer.Clear();
            }
            _flushTimer?.Stop();
            _pendingBuffer.Clear();
            _outputBuffer.Clear();
            IsTruncated = false;
            OutputText = string.Empty;
            Buffer.RequestRefresh();
        }
    }

    [RelayCommand]
    public void ToggleDiagnosticMode()
    {
        IsDiagnosticMode = !IsDiagnosticMode;
    }

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void Minimize()
    {
        if (WindowState != MdiWindowState.Minimized)
        {
            SaveRestoreBounds();
            WindowState = MdiWindowState.Minimized;
        }
    }

    [RelayCommand]
    public void Maximize()
    {
        if (WindowState != MdiWindowState.Maximized)
        {
            SaveRestoreBounds();
            WindowState = MdiWindowState.Maximized;
        }
    }

    [RelayCommand]
    public void Restore()
    {
        if (WindowState != MdiWindowState.Normal)
        {
            WindowState = MdiWindowState.Normal;
            Left = _restoreLeft;
            Top = _restoreTop;
            Width = _restoreWidth;
            Height = _restoreHeight;
        }
    }

    public void SaveRestoreBounds()
    {
        if (WindowState == MdiWindowState.Normal)
        {
            _restoreLeft = Left;
            _restoreTop = Top;
            _restoreWidth = Width;
            _restoreHeight = Height;
        }
    }

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        lock (_bufferLock)
        {
            _pendingBuffer.Append(text);

            // 待刷新队列有界限制（1 MiB）
            if (_pendingBuffer.Length > MaxPendingBufferSize)
            {
                IsTruncated = true;
                int excess = _pendingBuffer.Length - MaxPendingBufferSize;
                _pendingBuffer.Remove(0, excess);
            }

            if (_flushTimer == null)
            {
                // 无 UI 调度器环境（如单元测试），同步刷新
                FlushPendingBufferLocked();
            }
            else
            {
                // UI 环境：启动 16ms 批量合流定时器
                if (!_flushTimer.IsEnabled)
                {
                    if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            lock (_bufferLock)
                            {
                                if (_pendingBuffer.Length > 0 && !_flushTimer.IsEnabled)
                                {
                                    _flushTimer.Start();
                                }
                            }
                        });
                    }
                    else
                    {
                        _flushTimer.Start();
                    }
                }
            }
        }
    }

    private void OnFlushTimerTick(object? sender, EventArgs e)
    {
        lock (_bufferLock)
        {
            if (_pendingBuffer.Length == 0)
            {
                _flushTimer?.Stop();
                return;
            }

            FlushPendingBufferLocked();
        }
    }

    public void FlushOutput()
    {
        lock (_bufferLock)
        {
            _flushTimer?.Stop();
            FlushPendingBufferLocked();
        }
    }

    private void FlushPendingBufferLocked()
    {
        if (_pendingBuffer.Length == 0) return;

        string pendingText = _pendingBuffer.ToString();
        _pendingBuffer.Clear();

        // 1. 在 UI 线程对累积文本解析 VT 序列，安全改写 Buffer
        lock (Buffer.SyncRoot)
        {
            Parser.Parse(pendingText);
        }

        // 2. 累加诊断输出缓冲
        _outputBuffer.Append(pendingText);

        // 显示缓冲有界限制（1 MiB）
        if (_outputBuffer.Length > MaxOutputBufferSize)
        {
            IsTruncated = true;
            int excess = _outputBuffer.Length - MaxOutputBufferSize;
            _outputBuffer.Remove(0, excess);
        }

        string fullText = IsTruncated
            ? TruncationNoticeHeader + _outputBuffer.ToString()
            : _outputBuffer.ToString();

        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                OutputText = fullText;
            });
        }
        else
        {
            OutputText = fullText;
        }

        // 3. 通知 Buffer 刷新，驱动 TerminalControl 重绘
        Buffer.RequestRefresh();
    }

    public async ValueTask DisposeAsync()
    {
        _flushTimer?.Stop();
        if (Session != null)
        {
            var session = Session;
            DetachSession(session);
            Session = null;
            try
            {
                await session.StopAsync();
                await session.DisposeAsync();
            }
            catch { }
        }
    }

    private void OnSessionOutputReceived(object? sender, string text)
    {
        AppendOutput(text);
    }

    private void OnSessionStateChanged(object? sender, TerminalState newState)
    {
        State = newState;
        if (Session?.ProcessId != null)
        {
            ProcessId = Session.ProcessId;
        }
        UpdateCommandStates();

        StatusMessage = newState switch
        {
            TerminalState.Created => "就绪 (Created)",
            TerminalState.Starting => "启动中 (Starting)...",
            TerminalState.Running => $"运行中 (PID: {ProcessId?.ToString() ?? "N/A"})",
            TerminalState.Stopping => "停止中 (Stopping)...",
            TerminalState.Exited => $"已退出 (PID: {ProcessId?.ToString() ?? "N/A"}, ExitCode: {ExitCode?.ToString() ?? "0"})",
            TerminalState.Failed => "运行失败 (Failed)",
            _ => newState.ToString()
        };
    }

    private void OnSessionProcessExited(object? sender, int code)
    {
        ExitCode = code;
        if (Session?.ProcessId != null)
        {
            ProcessId = Session.ProcessId;
        }
        UpdateCommandStates();
        StatusMessage = $"进程已退出 (PID: {ProcessId?.ToString() ?? "N/A"}, 代码: {code})";
        AppendOutput($"\n[进程已退出，代码: {code}]\n");
        FlushOutput();
    }

    private void UpdateCommandStates()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanStop));
        OnPropertyChanged(nameof(CanSend));
        OnPropertyChanged(nameof(CanInterrupt));
        OnPropertyChanged(nameof(CanResize));

        StartCommand.NotifyCanExecuteChanged();
        SendCommand.NotifyCanExecuteChanged();
        InterruptCommand.NotifyCanExecuteChanged();
        ResizeCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }
}
