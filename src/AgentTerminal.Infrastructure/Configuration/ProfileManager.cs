using System.IO;
using System.Text.Json;
using AgentTerminal.Core.Models;

namespace AgentTerminal.Infrastructure.Configuration;

/// <summary>
/// Shell 与 Agent 配置管理与 JSON 持久化服务
/// </summary>
public class ProfileManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public List<ShellProfile> ShellProfiles { get; set; } = new();
    public List<AgentProfile> AgentProfiles { get; set; } = new();

    public void LoadDefaults()
    {
        ShellProfiles = new List<ShellProfile>
        {
            ShellProfile.CreatePowerShellCore(),
            ShellProfile.CreateWindowsPowerShell(),
            ShellProfile.CreateCmd(),
            ShellProfile.CreateWsl()
        };

        AgentProfiles = new List<AgentProfile>
        {
            new()
            {
                Name = "Claude Code",
                Description = "Anthropic Claude Code CLI",
                Command = "claude"
            },
            new()
            {
                Name = "Codex CLI",
                Description = "Codex Autonomous Coding Agent",
                Command = "codex"
            },
            new()
            {
                Name = "Gemini CLI",
                Description = "Google Gemini Command Line Interface",
                Command = "gemini"
            }
        };
    }

    public void SaveToFile(string filePath)
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(filePath, json);
    }

    public static ProfileManager LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var manager = new ProfileManager();
            manager.LoadDefaults();
            return manager;
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ProfileManager>(json, JsonOptions) ?? new ProfileManager();
    }
}
