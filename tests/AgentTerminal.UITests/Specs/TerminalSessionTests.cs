using System;
using System.Threading;
using AgentTerminal.UITests.Infrastructure;
using AgentTerminal.UITests.Pages;
using Xunit;

namespace AgentTerminal.UITests.Specs;

public class TerminalSessionTests
{
    [Fact]
    public void StartAndSendCommand_ShouldReceiveCorrectOutput()
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

        UiaWait.UntilTrue(() => debugView.IsRunning(),
            timeout: TimeSpan.FromSeconds(10),
            message: "Terminal session did not reach Running state");

        var uniqueToken = $"TF_E2E_{Guid.NewGuid().ToString("N")[..8]}";
        debugView.SendCommand($"Write-Host \"TOKEN:{uniqueToken}\"");

        debugView.WaitForOutput($"TOKEN:{uniqueToken}", timeout: TimeSpan.FromSeconds(8));
        Assert.Contains($"TOKEN:{uniqueToken}", debugView.GetOutputText());
        fixture.CaptureScreenshot("09_TerminalSession_OutputReceived");
    }

    [Fact]
    public void InterruptLongRunningCommand_ShouldStopOutputAndRecover()
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
        UiaWait.UntilTrue(() => debugView.IsRunning(),
            timeout: TimeSpan.FromSeconds(10));

        debugView.SendCommand("for($i=1;$i -le 50;$i++){ Start-Sleep -Milliseconds 200; Write-Host \"LOOP_$i\" }");
        
        debugView.WaitForOutput("LOOP_", timeout: TimeSpan.FromSeconds(5));

        debugView.ClickInterrupt();
        Thread.Sleep(500);

        var recoveryToken = $"REC_{Guid.NewGuid().ToString("N")[..8]}";
        debugView.SendCommand($"Write-Host \"{recoveryToken}\"");
        debugView.WaitForOutput(recoveryToken, timeout: TimeSpan.FromSeconds(8));
        Assert.Contains(recoveryToken, debugView.GetOutputText());
        fixture.CaptureScreenshot("10_TerminalSession_InterruptedAndRecovered");
    }

    [Fact]
    public void ResizeDimensions_ShouldUpdateViewportAndSettings()
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

        debugView.SetDimensions(140, 45);
        Thread.Sleep(300);

        Assert.Equal("140", debugView.ColumnsBox?.Text);
        Assert.Equal("45", debugView.RowsBox?.Text);
        fixture.CaptureScreenshot("11_TerminalSession_ResizeApplied");
    }
}
