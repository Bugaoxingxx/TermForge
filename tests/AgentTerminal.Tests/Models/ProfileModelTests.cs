using AgentTerminal.Core.Models;
using AgentTerminal.Infrastructure.Configuration;
using FluentAssertions;
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
        profile.Name.Should().Be("PowerShell 7");
        profile.ExecutablePath.Should().Be("pwsh.exe");
        profile.Arguments.Should().Be("-NoLogo");
        profile.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ProfileManager_LoadDefaults_ShouldPopulateProfiles()
    {
        // Arrange
        var manager = new ProfileManager();

        // Act
        manager.LoadDefaults();

        // Assert
        manager.ShellProfiles.Should().NotBeEmpty();
        manager.ShellProfiles.Should().Contain(p => p.Name == "PowerShell 7");
        manager.AgentProfiles.Should().NotBeEmpty();
        manager.AgentProfiles.Should().Contain(p => p.Name == "Claude Code");
    }
}
