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

    [Fact]
    public void DragChildWindow_ShouldMoveToNewPositionWithinWorkspace()
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

        // Perform physical mouse drag: deltaX = 120, deltaY = 80
        doc.DragBy(120, 80);
        Thread.Sleep(300);

        var newBounds = doc.BoundingRectangle;
        fixture.CaptureScreenshot("14_DragChildWindow_Moved");

        // Verify that the child window actually moved by the dragged delta
        var deltaX = newBounds.Left - initialBounds.Left;
        var deltaY = newBounds.Top - initialBounds.Top;

        Assert.True(Math.Abs(deltaX - 120) <= 20, $"Window expected to move horizontally by ~120px, but moved by {deltaX}px (from {initialBounds.Left} to {newBounds.Left})");
        Assert.True(Math.Abs(deltaY - 80) <= 20, $"Window expected to move vertically by ~80px, but moved by {deltaY}px (from {initialBounds.Top} to {newBounds.Top})");
    }

    [Fact]
    public void DoubleClickTitleBar_ShouldToggleMaximizeRestore()
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

        // 1. Double click to maximize
        doc.DoubleClickTitleBar();
        Thread.Sleep(500);

        var maxBounds = doc.BoundingRectangle;
        Assert.True(maxBounds.Width > initialBounds.Width || maxBounds.Height > initialBounds.Height,
            $"Expected maximized width/height ({maxBounds.Width}x{maxBounds.Height}) to be larger than initial ({initialBounds.Width}x{initialBounds.Height})");

        // 2. Double click to restore
        doc.DoubleClickTitleBar();
        Thread.Sleep(500);

        var restoredBounds = doc.BoundingRectangle;
        Assert.True(Math.Abs(restoredBounds.Width - initialBounds.Width) <= 10,
            $"Restored width {restoredBounds.Width} does not match initial {initialBounds.Width}");
        Assert.True(Math.Abs(restoredBounds.Height - initialBounds.Height) <= 10,
            $"Restored height {restoredBounds.Height} does not match initial {initialBounds.Height}");

        fixture.CaptureScreenshot("15_DoubleClickTitleBar_Toggled");
    }

    [Fact]
    public void DragChildWindow_BeyondBoundaries_ShouldClampWithinWorkspace()
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
        var canvasBounds = workspace.CanvasBounds;

        // 1. 针对左上边界：剧烈向左上方拖拽 (-800px, -800px)
        doc.DragBy(-800, -800);
        Thread.Sleep(300);

        var tlClampedBounds = doc.BoundingRectangle;
        fixture.CaptureScreenshot("16_DragChildWindow_Clamped_TopLeft");

        // 验证：子窗口左上角不得逃逸出工作区 Canvas 左侧及顶部（允许少量 DPI / 边框容差）
        Assert.True(tlClampedBounds.Left >= canvasBounds.Left - 5,
            $"Window left ({tlClampedBounds.Left}) should clamp to canvas left ({canvasBounds.Left})");
        Assert.True(tlClampedBounds.Top >= canvasBounds.Top - 5,
            $"Window top ({tlClampedBounds.Top}) should clamp to canvas top ({canvasBounds.Top})");
        Assert.True(tlClampedBounds.Width > 200, "Window width should remain valid after clamp drag");
        Assert.True(tlClampedBounds.Height > 100, "Window height should remain valid after clamp drag");

        // 2. 针对右下边界：剧烈向右下方拖拽 (+3000px, +3000px)
        doc.DragBy(3000, 3000);
        Thread.Sleep(300);

        var brClampedBounds = doc.BoundingRectangle;
        fixture.CaptureScreenshot("16_DragChildWindow_Clamped_BottomRight");

        // 验证 2.1：断言第二次拖拽切实发生了位移（排除前次居左上后二次拖动为空操作的假阳性）
        Assert.True(brClampedBounds.Left > tlClampedBounds.Left + 100,
            $"Window left ({brClampedBounds.Left}) must move significantly right from top-left clamped position ({tlClampedBounds.Left})");
        Assert.True(brClampedBounds.Top > tlClampedBounds.Top + 100,
            $"Window top ({brClampedBounds.Top}) must move significantly down from top-left clamped position ({tlClampedBounds.Top})");

        // 验证 2.2：按照产品规则（Left 钳制于 canvas.Width - 60，Top 钳制于 canvas.Height - 40），
        // 窗口左上角必须停留在 Canvas 右/下内侧约 60/40 像素处（留 15px 容差）
        var expectedLeft = canvasBounds.Right - 60;
        var expectedTop = canvasBounds.Bottom - 40;
        Assert.True(Math.Abs(brClampedBounds.Left - expectedLeft) <= 15,
            $"Window left ({brClampedBounds.Left}) should clamp near canvas.Right - 60 ({expectedLeft})");
        Assert.True(Math.Abs(brClampedBounds.Top - expectedTop) <= 15,
            $"Window top ({brClampedBounds.Top}) should clamp near canvas.Bottom - 40 ({expectedTop})");

        // 验证 2.3：计算窗口与工作区 Canvas 的实际交集矩形，确认至少保留 50px 宽和 30px 高的可视交互区域（产品标称保留 60x40）
        var intersection = System.Drawing.Rectangle.Intersect(brClampedBounds, canvasBounds);
        Assert.False(intersection.IsEmpty, "Clamped window must intersect with canvas workspace");
        Assert.True(intersection.Width >= 50,
            $"Visible intersection width ({intersection.Width}) must be at least 50px (product target 60px)");
        Assert.True(intersection.Height >= 30,
            $"Visible intersection height ({intersection.Height}) must be at least 30px (product target 40px)");
    }

    [Fact]
    public void DragWindowBorder_ShouldResizeWindowDimensions()
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

        // Drag bottom-right thumb outward by 80px width and 50px height
        doc.ResizeBottomRight(80, 50);
        Thread.Sleep(300);

        var resizedBounds = doc.BoundingRectangle;
        fixture.CaptureScreenshot("17_DragBorder_Resized");

        var deltaW = resizedBounds.Width - initialBounds.Width;
        var deltaH = resizedBounds.Height - initialBounds.Height;

        Assert.True(Math.Abs(deltaW - 80) <= 20, $"Expected width to increase by ~80px, but changed by {deltaW}px (from {initialBounds.Width} to {resizedBounds.Width})");
        Assert.True(Math.Abs(deltaH - 50) <= 20, $"Expected height to increase by ~50px, but changed by {deltaH}px (from {initialBounds.Height} to {resizedBounds.Height})");
    }
}
