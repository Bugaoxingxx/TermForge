using System;
using System.Threading;
using AgentTerminal.UITests.Infrastructure;
using AgentTerminal.UITests.Pages;
using Xunit;

namespace AgentTerminal.UITests.Specs;

public class MdiWorkbenchLayoutTests
{
    [Fact]
    public void CreateThreeDocuments_ShouldDisplayInWorkspace()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var mainPage = new MainWindowPage(window);
        var workspace = mainPage.GetMdiWorkspace();

        var initialCount = workspace.GetChildWindows().Count;

        mainPage.ClickNewTerminal();
        mainPage.ClickNewTerminal();
        
        workspace.WaitForWindowCount(initialCount + 2, timeout: TimeSpan.FromSeconds(5));
        var docs = workspace.GetChildWindows();
        Assert.True(docs.Count >= 2);

        fixture.CaptureScreenshot("01_CreateThreeDocuments_Displayed");
    }

    [Fact]
    public void TileAndCascade_ShouldRearrangeWindowBoundsCorrectly()
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
        Assert.Equal(3, docs.Count);

        // 1. Test Tile Vertical (Left to Right)
        mainPage.ClickTileVertical();
        Thread.Sleep(500);
        docs = workspace.GetChildWindows();
        Assert.True(docs[0].BoundingRectangle.Left < docs[1].BoundingRectangle.Left);
        Assert.True(docs[1].BoundingRectangle.Left < docs[2].BoundingRectangle.Left);
        fixture.CaptureScreenshot("02_TileVertical_SideBySide");

        // 2. Test Tile Horizontal (Top to Bottom)
        mainPage.ClickTileHorizontal();
        Thread.Sleep(500);
        docs = workspace.GetChildWindows();
        Assert.True(docs[0].BoundingRectangle.Top < docs[1].BoundingRectangle.Top);
        Assert.True(docs[1].BoundingRectangle.Top < docs[2].BoundingRectangle.Top);
        fixture.CaptureScreenshot("03_TileHorizontal_Stacked");

        // 3. Test Cascade (Diagonal step)
        mainPage.ClickCascade();
        Thread.Sleep(500);
        docs = workspace.GetChildWindows();
        Assert.True(docs[0].BoundingRectangle.Left <= docs[1].BoundingRectangle.Left);
        Assert.True(docs[1].BoundingRectangle.Left <= docs[2].BoundingRectangle.Left);
        fixture.CaptureScreenshot("04_Cascade_Diagonal");
    }

    [Fact]
    public void MaximizeAndRestore_ShouldSwitchWindowSizes()
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
        var initialBounds = doc.BoundingRectangle;

        doc.MaximizeOrRestore();
        Thread.Sleep(400);

        var maxBounds = doc.BoundingRectangle;
        Assert.True(maxBounds.Width > initialBounds.Width || maxBounds.Height > initialBounds.Height);
        fixture.CaptureScreenshot("05_Window_Maximized");

        doc.MaximizeOrRestore();
        Thread.Sleep(400);

        var restoredBounds = doc.BoundingRectangle;
        Assert.True(Math.Abs(restoredBounds.Width - initialBounds.Width) <= 5);
        Assert.True(Math.Abs(restoredBounds.Height - initialBounds.Height) <= 5);
        fixture.CaptureScreenshot("06_Window_Restored");
    }
}
