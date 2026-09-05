using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AgentTerminal.UITests.Infrastructure;
using AgentTerminal.UITests.Pages;
using Xunit;

namespace AgentTerminal.UITests.Specs;

public class AppLifecycleTests
{
    [Fact]
    public void CloseMainWindow_ShouldCleanUpAllShellProcesses()
    {
        var fixture = new TestAppFixture();
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

        var appPid = fixture.App!.ProcessId;
        fixture.CaptureScreenshot("13_AppLifecycle_SessionRunning_BeforeExit");

        fixture.Dispose();

        Thread.Sleep(1000);
        var procs = Process.GetProcesses().Where(p => p.Id == appPid).ToList();
        Assert.Empty(procs);
    }
}
