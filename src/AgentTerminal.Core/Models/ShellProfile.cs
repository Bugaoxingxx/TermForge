namespace AgentTerminal.Core.Models;

/// <summary>
/// Shell 启动配置描述（如 PowerShell 7、CMD、WSL 等）
/// </summary>
public class ShellProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public string Arguments { get; set; } = string.Empty;

    public string? WorkingDirectory { get; set; }

    public string? IconPath { get; set; }

    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    public bool IsDefault { get; set; }

    public static ShellProfile CreatePowerShellCore() => new()
    {
        Name = "PowerShell 7",
        ExecutablePath = "pwsh.exe",
        Arguments = "-NoLogo",
        IsDefault = true
    };

    public static ShellProfile CreateWindowsPowerShell() => new()
    {
        Name = "Windows PowerShell",
        ExecutablePath = "powershell.exe",
        Arguments = "-NoLogo"
    };

    public static ShellProfile CreateCmd() => new()
    {
        Name = "Command Prompt",
        ExecutablePath = "cmd.exe"
    };

    public static ShellProfile CreateWsl(string? distro = null) => new()
    {
        Name = string.IsNullOrEmpty(distro) ? "WSL" : $"WSL ({distro})",
        ExecutablePath = "wsl.exe",
        Arguments = string.IsNullOrEmpty(distro) ? string.Empty : $"-d {distro}"
    };
}
