using System;
using System.Threading;
using AgentTerminal.UITests.Infrastructure;
using AgentTerminal.UITests.Pages;
using Xunit;

namespace AgentTerminal.UITests.Specs;

public class KeyboardNavigationTests
{
    [Fact]
    public void CloseDocumentViaWindowButton_ShouldPreserveRemainingWindows()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var mainPage = new MainWindowPage(window);
        var workspace = mainPage.GetMdiWorkspace();

        while (workspace.GetChildWindows().Count < 3)
        {
            mainPage.ClickNewTerminal();
            Thread.Sleep(300);
        }

        workspace.WaitForWindowCount(3);
        var docs = workspace.GetChildWindows();
        var secondDoc = docs[1];

        secondDoc.Close();
        Thread.Sleep(500);

        workspace.WaitForWindowCount(2);
        var remainingDocs = workspace.GetChildWindows();
        Assert.Equal(2, remainingDocs.Count);

        fixture.CaptureScreenshot("12_KeyboardNav_DocumentClosed_RemainingTwo");
    }
}
