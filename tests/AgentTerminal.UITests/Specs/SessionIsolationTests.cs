using System;
using System.Threading;
using AgentTerminal.UITests.Infrastructure;
using AgentTerminal.UITests.Pages;
using Xunit;

namespace AgentTerminal.UITests.Specs;

public class SessionIsolationTests
{
    [Fact]
    public void MultipleSessions_CommandsAndOutputs_MustBeIsolated()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var mainPage = new MainWindowPage(window);
        var workspace = mainPage.GetMdiWorkspace();

        while (workspace.GetChildWindows().Count < 2)
        {
            mainPage.ClickNewTerminal();
            Thread.Sleep(300);
        }

        workspace.WaitForWindowCount(2);
        var docs = workspace.GetChildWindows();
        var docA = docs[0];
        var docB = docs[1];

        var debugA = docA.GetDebugView();
        var debugB = docB.GetDebugView();

        debugA.ClickStart();
        debugB.ClickStart();

        UiaWait.UntilTrue(() => debugA.IsRunning());
        UiaWait.UntilTrue(() => debugB.IsRunning());

        var tagA = $"TAG_AAA_{Guid.NewGuid().ToString("N")[..6]}";
        var tagB = $"TAG_BBB_{Guid.NewGuid().ToString("N")[..6]}";

        debugA.SendCommand($"Write-Host \"{tagA}\"");
        debugB.SendCommand($"Write-Host \"{tagB}\"");

        debugA.WaitForOutput(tagA);
        debugB.WaitForOutput(tagB);

        Assert.Contains(tagA, debugA.GetOutputText());
        Assert.DoesNotContain(tagB, debugA.GetOutputText());

        Assert.Contains(tagB, debugB.GetOutputText());
        Assert.DoesNotContain(tagA, debugB.GetOutputText());

        fixture.CaptureScreenshot("07_MultipleSessions_IsolatedOutputs");
    }

    [Fact]
    public void LayoutChanges_ShouldNotInterruptRunningProcesses()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var mainPage = new MainWindowPage(window);
        var workspace = mainPage.GetMdiWorkspace();

        if (workspace.GetChildWindows().Count == 0)
        {
            mainPage.ClickNewTerminal();
            workspace.WaitForWindowCount(1);
        }

        var doc = workspace.GetChildWindows()[0];
        var debugView = doc.GetDebugView();

        debugView.ClickStart();
        UiaWait.UntilTrue(() => debugView.IsRunning());

        mainPage.ClickTileVertical();
        Thread.Sleep(300);
        mainPage.ClickTileHorizontal();
        Thread.Sleep(300);
        mainPage.ClickCascade();
        Thread.Sleep(300);

        var aliveTag = $"STILL_ALIVE_{Guid.NewGuid().ToString("N")[..6]}";
        debugView.SendCommand($"Write-Host \"{aliveTag}\"");
        debugView.WaitForOutput(aliveTag);

        Assert.Contains(aliveTag, debugView.GetOutputText());
        fixture.CaptureScreenshot("08_LayoutChanges_StillAlive");
    }
}
