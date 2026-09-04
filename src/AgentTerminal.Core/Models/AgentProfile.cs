namespace AgentTerminal.Core.Models;

/// <summary>
/// Agent 命令行运行配置描述（如 Claude Code, Codex, Gemini CLI 等）
/// </summary>
public class AgentProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>基础宿主 Shell 配置 ID（例如 WSL、PowerShell）</summary>
    public string? BaseShellProfileId { get; set; }

    /// <summary>待执行的 Agent CLI 命令（例如 claude, codex, gemini）</summary>
    public string Command { get; set; } = string.Empty;

    public string? WorkingDirectory { get; set; }

    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    public string? IconPath { get; set; }
}
