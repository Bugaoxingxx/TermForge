using System.IO;
using Serilog;

namespace AgentTerminal.Infrastructure.Logging;

/// <summary>
/// Serilog 日志初始化与基础设施服务
/// </summary>
public static class LoggingService
{
    public static void Initialize(string? logDirectory = null)
    {
        var targetDir = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TermForge",
            "logs");

        Directory.CreateDirectory(targetDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(targetDir, "termforge-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        Log.Information("TermForge Logging Initialized. Log directory: {LogDirectory}", targetDir);
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
