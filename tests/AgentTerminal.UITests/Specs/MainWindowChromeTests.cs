using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using AgentTerminal.UITests.Infrastructure;
using AgentTerminal.UITests.Pages;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using Xunit;

namespace AgentTerminal.UITests.Specs;

public class MainWindowChromeTests
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [Fact]
    public void MainWindow_RetainedAutomationIds_MustExist()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var mainPage = new MainWindowPage(window);

        Assert.NotNull(mainPage.MainMenu);
        Assert.NotNull(mainPage.MainToolBar);
        Assert.NotNull(mainPage.BtnNewTerminal);
        Assert.NotNull(mainPage.BtnStopSession);
        Assert.NotNull(mainPage.BtnArrange);
        Assert.NotNull(mainPage.BtnArrangeDropdown);
        Assert.NotNull(mainPage.BtnClearOutput);
        Assert.NotNull(mainPage.BtnToggleDiagnostic);
        Assert.NotNull(mainPage.NavigationPane);
        Assert.NotNull(mainPage.PropertiesPane);
        Assert.NotNull(mainPage.DiagnosticsPane);
        Assert.NotNull(mainPage.StatusBar);
        Assert.NotNull(mainPage.GetMdiWorkspace().Container);

        // Verify menu surface items
        Assert.NotNull(mainPage.MenuFile);
        Assert.NotNull(mainPage.MenuView);
        Assert.NotNull(mainPage.MenuWindow);

        // Verify native window provider capabilities (native OS caption)
        var winPattern = window.Patterns.Window.Pattern;
        Assert.NotNull(winPattern);
        Assert.True(winPattern.CanMinimize);
        Assert.True(winPattern.CanMaximize);
        Assert.False(string.IsNullOrWhiteSpace(window.Title));
        Assert.NotNull(window.TitleBar);
        Assert.True(window.TitleBar.BoundingRectangle.Height > 0, "Native OS title bar should have positive height");
    }

    [Fact]
    public void MainWindow_MaximizeAndRestore_ShouldRespectWorkingArea()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var winPattern = window.Patterns.Window.Pattern;

        var hWnd = window.Properties.NativeWindowHandle.Value;
        var hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(hMonitor, ref mi);

        // Maximize
        winPattern.SetWindowVisualState(WindowVisualState.Maximized);
        Thread.Sleep(500);

        Assert.Equal(WindowVisualState.Maximized, winPattern.WindowVisualState.Value);
        var maxBounds = window.BoundingRectangle;

        // Verify taskbar-safe: maximized bounds should align with rcWork within standard DWM sizing border tolerance (typically ~10px)
        int tolerance = 15;
        Assert.True(Math.Abs(maxBounds.Left - mi.rcWork.Left) <= tolerance,
            $"Maximized Left ({maxBounds.Left}) should align near WorkingArea Left ({mi.rcWork.Left})");
        Assert.True(Math.Abs(maxBounds.Top - mi.rcWork.Top) <= tolerance,
            $"Maximized Top ({maxBounds.Top}) should align near WorkingArea Top ({mi.rcWork.Top})");
        Assert.True(Math.Abs(maxBounds.Right - mi.rcWork.Right) <= tolerance,
            $"Maximized Right ({maxBounds.Right}) should align near WorkingArea Right ({mi.rcWork.Right})");
        Assert.True(Math.Abs(maxBounds.Bottom - mi.rcWork.Bottom) <= tolerance,
            $"Maximized Bottom ({maxBounds.Bottom}) should align near WorkingArea Bottom ({mi.rcWork.Bottom})");

        fixture.CaptureScreenshot("18_MainWindow_Maximized");

        // Restore
        winPattern.SetWindowVisualState(WindowVisualState.Normal);
        Thread.Sleep(500);

        Assert.Equal(WindowVisualState.Normal, winPattern.WindowVisualState.Value);
        fixture.CaptureScreenshot("19_MainWindow_Restored");
    }

    [Fact]
    public void MainWindow_DocumentLifecycle_NewAndClose_ShouldMaintainWorkspace()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var mainPage = new MainWindowPage(window);
        var workspace = mainPage.GetMdiWorkspace();

        int initialCount = workspace.GetChildWindows().Count;

        // 1. Create new terminal document via Toolbar
        mainPage.ClickNewTerminal();
        workspace.WaitForWindowCount(initialCount + 1, TimeSpan.FromSeconds(5));
        Assert.Equal(initialCount + 1, workspace.GetChildWindows().Count);

        // 2. Close document via child window close button
        var docs = workspace.GetChildWindows();
        docs.Last().Close();
        workspace.WaitForWindowCount(initialCount, TimeSpan.FromSeconds(5));
        Assert.Equal(initialCount, workspace.GetChildWindows().Count);
    }

    [Fact]
    public void MainWindow_CaptureBaselines_AndEnvironmentInfo()
    {
        using var fixture = new TestAppFixture();
        var window = fixture.Launch();
        var mainPage = new MainWindowPage(window);
        var workspace = mainPage.GetMdiWorkspace();

        fixture.CaptureScreenshot("00_MainWindow_Baseline_SingleDoc");

        while (workspace.GetChildWindows().Count < 3)
        {
            mainPage.ClickNewTerminal();
            Thread.Sleep(300);
        }
        workspace.WaitForWindowCount(3);
        mainPage.ClickTileVertical();
        Thread.Sleep(400);

        fixture.CaptureScreenshot("00_MainWindow_Baseline_ThreeDocs_MDI");
    }
}
