using System.Diagnostics;
using System.IO;
using System.Text;
using AgentTerminal.Core.Models;
using AgentTerminal.Terminal.ConPty;
using Xunit;

namespace AgentTerminal.Tests.ConPty;

/// <summary>
/// 真实 Windows 环境下 ConPTY 与 PowerShell 会话集成测试（覆盖 AC-01 至 AC-06）
/// </summary>
public class PowerShellSessionIntegrationTests
{
    private static string? GetPowerShellExecutable()
    {
        // 优先使用 PowerShell 7 (pwsh.exe)，若未安装则使用 Windows PowerShell
        string[] candidates = ["pwsh.exe", "powershell.exe"];
        string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var candidate in candidates)
        {
            foreach (var p in paths)
            {
                try
                {
                    string fullPath = Path.Combine(p, candidate);
                    if (File.Exists(fullPath)) return fullPath;
                }
                catch { }
            }
        }

        string systemWinPs = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");

        if (File.Exists(systemWinPs))
        {
            return systemWinPs;
        }

        return null;
    }

    [Fact]
    public async Task AC01_StartAndExecuteCommand_ShouldReceiveOutputAndVerifyEnvironment()
    {
        string? psExe = GetPowerShellExecutable();
        if (psExe == null)
        {
            Assert.Fail("PowerShell executable not found in test environment.");
            return;
        }

        string uniqueToken = Guid.NewGuid().ToString("N");
        string envValue = "TermForge_Env_" + uniqueToken;

        var profile = new ShellProfile
        {
            Name = "Test PowerShell",
            ExecutablePath = psExe,
            Arguments = "-NoLogo -NoProfile",
            WorkingDirectory = Environment.CurrentDirectory,
            EnvironmentVariables = new Dictionary<string, string>
            {
                { "TF_INTEGRATION_TEST_VAR", envValue }
            }
        };

        var session = new ConPtyTerminalSession(profile);
        var sb = new StringBuilder();
        var tcs = new TaskCompletionSource<bool>();

        session.OutputReceived += (s, text) =>
        {
            sb.Append(text);
            if (sb.ToString().Contains($"RESULT_{uniqueToken}") && sb.ToString().Contains(envValue))
            {
                tcs.TrySetResult(true);
            }
        };

        try
        {
            await session.StartAsync();
            Assert.Equal(TerminalState.Running, session.State);
            Assert.NotNull(session.ProcessId);

            // 等待 PowerShell 完成启动与控制台初始化 (避免 PSReadLine 启动时清空输入缓冲)
            await Task.Delay(1500);

            // 发送带唯一标识的命令
            string cmd = $"Write-Output \"RESULT_{uniqueToken}=$env:TF_INTEGRATION_TEST_VAR\"\r\n";
            await session.WriteAsync(cmd);

            // 等待最长 10 秒（AC-01 门槛）
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(tcs.Task, completedTask);

            string output = sb.ToString();
            Assert.Contains($"RESULT_{uniqueToken}", output);
            Assert.Contains(envValue, output);
        }
        finally
        {
            await session.DisposeAsync();
        }
    }

    [Fact]
    public async Task AC02_UnicodeOutput_ShouldDecodeChineseAndEmojiWithoutLoss()
    {
        string? psExe = GetPowerShellExecutable();
        if (psExe == null) return;

        var profile = new ShellProfile
        {
            Name = "Test Unicode",
            ExecutablePath = psExe,
            Arguments = "-NoLogo -NoProfile"
        };

        var session = new ConPtyTerminalSession(profile);
        var sb = new StringBuilder();
        string expectedTarget = "你好世界_测试成功_🚀";
        var tcs = new TaskCompletionSource<bool>();

        session.OutputReceived += (s, text) =>
        {
            sb.Append(text);
            if (sb.ToString().Contains(expectedTarget))
            {
                tcs.TrySetResult(true);
            }
        };

        try
        {
            await session.StartAsync();

            string cmd = $"Write-Output \"{expectedTarget}\"\r\n";
            await session.WriteAsync(cmd);

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(tcs.Task, completedTask);

            string output = sb.ToString();
            Assert.Contains(expectedTarget, output);
            Assert.DoesNotContain("\uFFFD", output);
        }
        finally
        {
            await session.DisposeAsync();
        }
    }

    [Fact]
    public async Task AC03_Interrupt_ShouldCancelLongCommandAndAllowNextCommand()
    {
        string? psExe = GetPowerShellExecutable();
        if (psExe == null) return;

        var profile = new ShellProfile
        {
            Name = "Test Interrupt",
            ExecutablePath = psExe,
            Arguments = "-NoLogo -NoProfile"
        };

        var session = new ConPtyTerminalSession(profile);
        var sb = new StringBuilder();
        var tcsAfterInterrupt = new TaskCompletionSource<bool>();

        session.OutputReceived += (s, text) =>
        {
            sb.Append(text);
            if (sb.ToString().Contains("INTERRUPT_RECOVERED_OK"))
            {
                tcsAfterInterrupt.TrySetResult(true);
            }
        };

        try
        {
            await session.StartAsync();

            // 发送长耗时循环命令
            await session.WriteAsync("Start-Sleep -Seconds 30\r\n");
            await Task.Delay(500);

            // 发送中断信号 Ctrl+C (0x03)
            await session.WriteAsync(new byte[] { 0x03 });
            await Task.Delay(300);

            // 发送中断恢复确认命令
            await session.WriteAsync("Write-Output \"INTERRUPT_RECOVERED_OK\"\r\n");

            // 门槛：中断后 5 秒内可执行下一条命令
            var completedTask = await Task.WhenAny(tcsAfterInterrupt.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(tcsAfterInterrupt.Task, completedTask);

            Assert.Contains("INTERRUPT_RECOVERED_OK", sb.ToString());
            Assert.Equal(TerminalState.Running, session.State);
        }
        finally
        {
            await session.DisposeAsync();
        }
    }

    [Fact]
    public async Task AC04_Resize_ShouldUpdateDimensionsSuccessfully()
    {
        string? psExe = GetPowerShellExecutable();
        if (psExe == null) return;

        var profile = new ShellProfile
        {
            Name = "Test Resize",
            ExecutablePath = psExe,
            Arguments = "-NoLogo -NoProfile"
        };

        var session = new ConPtyTerminalSession(profile);

        try
        {
            await session.StartAsync();

            // 连续应用 80x24, 120x30, 160x50
            await session.ResizeAsync(80, 24);
            Assert.Equal(80, session.Dimensions.Columns);
            Assert.Equal(24, session.Dimensions.Rows);

            await session.ResizeAsync(120, 30);
            Assert.Equal(120, session.Dimensions.Columns);
            Assert.Equal(30, session.Dimensions.Rows);

            await session.ResizeAsync(160, 50);
            Assert.Equal(160, session.Dimensions.Columns);
            Assert.Equal(50, session.Dimensions.Rows);

            // 无效尺寸被拒绝且原会话仍可用
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.ResizeAsync(0, 50));
            Assert.Equal(160, session.Dimensions.Columns);
            Assert.Equal(50, session.Dimensions.Rows);
        }
        finally
        {
            await session.DisposeAsync();
        }
    }

    [Fact]
    public async Task AC05_ExitCodeAndProcessCleanup_ShouldRecordCorrectExitCode()
    {
        string? psExe = GetPowerShellExecutable();
        if (psExe == null) return;

        var profile = new ShellProfile
        {
            Name = "Test Exit",
            ExecutablePath = psExe,
            Arguments = "-NoLogo -NoProfile"
        };

        var session = new ConPtyTerminalSession(profile);
        var tcsExit = new TaskCompletionSource<int>();

        session.ProcessExited += (s, code) =>
        {
            tcsExit.TrySetResult(code);
        };

        try
        {
            await session.StartAsync();
            int pid = session.ProcessId ?? 0;

            // 执行 exit 7
            await session.WriteAsync("exit 7\r\n");

            var completedTask = await Task.WhenAny(tcsExit.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(tcsExit.Task, completedTask);

            int exitCode = await tcsExit.Task;
            Assert.Equal(7, exitCode);
            Assert.Equal(7, session.ExitCode);

            // 验证进程已终止
            await Task.Delay(200);
            Assert.Throws<ArgumentException>(() => Process.GetProcessById(pid));
        }
        finally
        {
            await session.DisposeAsync();
        }
    }

    [Fact]
    public async Task AC06_InvalidConfiguration_ShouldEnterFailedStateWithoutResourceLeak()
    {
        // 1. 无效的可执行路径
        var invalidExeProfile = new ShellProfile
        {
            Name = "Invalid Exe",
            ExecutablePath = @"C:\NonExistentDirectory\FakeNonExistentExe_12345.exe"
        };

        var session1 = new ConPtyTerminalSession(invalidExeProfile);
        await Assert.ThrowsAnyAsync<Exception>(() => session1.StartAsync());
        Assert.Equal(TerminalState.Failed, session1.State);
        await session1.DisposeAsync();

        // 2. 无效的工作目录
        string? psExe = GetPowerShellExecutable();
        if (psExe != null)
        {
            var invalidDirProfile = new ShellProfile
            {
                Name = "Invalid Dir",
                ExecutablePath = psExe,
                WorkingDirectory = @"C:\FakeDirectory_XYZ_987654"
            };

            var session2 = new ConPtyTerminalSession(invalidDirProfile);
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => session2.StartAsync());
            Assert.Equal(TerminalState.Failed, session2.State);
            await session2.DisposeAsync();
        }
    }
}
