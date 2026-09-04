using System.IO;
using AgentTerminal.Core.Models;
using AgentTerminal.Infrastructure.Configuration;
using Xunit;

namespace AgentTerminal.Tests.Models;

public class ProfileModelTests
{
    [Fact]
    public void ShellProfile_CreatePowerShellCore_ShouldHaveCorrectDefaults()
    {
        // Act
        var profile = ShellProfile.CreatePowerShellCore();

        // Assert
        Assert.Equal("PowerShell 7", profile.Name);
        Assert.Equal("pwsh.exe", profile.ExecutablePath);
        Assert.Equal("-NoLogo", profile.Arguments);
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public void ProfileManager_LoadDefaults_ShouldPopulateProfiles()
    {
        // Arrange
        var manager = new ProfileManager();

        // Act
        manager.LoadDefaults();

        // Assert
        Assert.NotEmpty(manager.ShellProfiles);
        Assert.Contains(manager.ShellProfiles, p => p.Name == "PowerShell 7");
        Assert.NotEmpty(manager.AgentProfiles);
        Assert.Contains(manager.AgentProfiles, p => p.Name == "Claude Code");
    }

    [Fact]
    public void ProfileManager_SaveAndLoad_ShouldPersistProfilesAtomically()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"termforge_test_{Guid.NewGuid():N}.json");
        try
        {
            var manager = new ProfileManager();
            manager.LoadDefaults();

            // Act
            manager.SaveToFile(tempFile);
            var loaded = ProfileManager.LoadFromFile(tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            Assert.Equal(manager.ShellProfiles.Count, loaded.ShellProfiles.Count);
            Assert.Equal(manager.AgentProfiles.Count, loaded.AgentProfiles.Count);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ProfileManager_LoadFromFile_WithCorruptedJson_ShouldFallbackToDefaults()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"termforge_corrupt_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, "{ invalid json corrupt content... }}}");

            // Act
            var loaded = ProfileManager.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(loaded);
            Assert.NotEmpty(loaded.ShellProfiles);
            Assert.Contains(loaded.ShellProfiles, p => p.Name == "PowerShell 7");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
            // Cleanup any backup files
            var parentDir = Path.GetDirectoryName(tempFile)!;
            var prefix = Path.GetFileName(tempFile);
            foreach (var bak in Directory.GetFiles(parentDir, $"{prefix}.corrupt.*"))
            {
                try { File.Delete(bak); } catch { }
            }
        }
    }

    [Fact]
    public void ProfileManager_LoadFromFile_WithEmptyJson_ShouldFallbackToDefaults()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"termforge_empty_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, "{}");

            // Act
            var loaded = ProfileManager.LoadFromFile(tempFile);

            // Assert
            Assert.NotNull(loaded);
            Assert.NotEmpty(loaded.ShellProfiles);
            Assert.Contains(loaded.ShellProfiles, p => p.Name == "PowerShell 7");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
