using System.IO;
using System.Text.Json;
using AgentTerminal.Core.Models;
using Serilog;

namespace AgentTerminal.Infrastructure.Configuration;

/// <summary>
/// Shell 与 Agent 配置管理与 JSON 持久化服务，支持原子写入与异常自动回退
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

    /// <summary>
    /// 原子写入配置文件（通过同目录临时文件写入并原子替换，防止断电或崩溃损坏）
    /// </summary>
    public void SaveToFile(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = filePath + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// 从文件加载配置。若文件缺失、损坏或无可用 Shell 配置，安全回退到默认配置并备份损坏文件
    /// </summary>
    public static ProfileManager LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var defaultManager = new ProfileManager();
            defaultManager.LoadDefaults();
            return defaultManager;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var manager = JsonSerializer.Deserialize<ProfileManager>(json, JsonOptions);

            // 若反序列化为 null，或内容为空列表，则安全填充默认配置
            if (manager == null || manager.ShellProfiles.Count == 0)
            {
                Log.Warning("Loaded profile configuration from {FilePath} contains no shell profiles. Reverting to defaults.", filePath);
                var fallbackManager = new ProfileManager();
                fallbackManager.LoadDefaults();
                return fallbackManager;
            }

            return manager;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to deserialize profile configuration from {FilePath}. Creating backup and falling back to defaults.", filePath);

            // 保留损坏文件备份供排查
            try
            {
                var backupPath = filePath + ".corrupt." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                File.Copy(filePath, backupPath, overwrite: true);
            }
            catch (Exception backupEx)
            {
                Log.Warning(backupEx, "Failed to create backup of corrupted file {FilePath}", filePath);
            }

            var fallbackManager = new ProfileManager();
            fallbackManager.LoadDefaults();
            return fallbackManager;
        }
    }
}
