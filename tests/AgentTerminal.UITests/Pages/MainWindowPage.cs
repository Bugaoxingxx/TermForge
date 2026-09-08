using System;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Exceptions;
using AgentTerminal.UITests.Infrastructure;

namespace AgentTerminal.UITests.Pages;

public class MainWindowPage
{
    private readonly Window _window;

    public MainWindowPage(Window window)
    {
        _window = window;
    }

    public Window Window => _window;

    private AutomationElement? Find(string automationId)
    {
        try
        {
            var elem = _window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            if (elem != null) return elem;

            int pid = _window.Properties.ProcessId.Value;
            var desktop = _window.Automation.GetDesktop();
            var appWindow = desktop.FindFirstDescendant(cf => cf.ByProcessId(pid).And(cf.ByAutomationId("MainWindow")));
            var found = appWindow?.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
            if (found != null) return found;

            // Also check popup/context menu windows within current process
            return desktop.FindFirstDescendant(cf => cf.ByProcessId(pid).And(cf.ByAutomationId(automationId)));
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private Button? FindButton(string automationId) => Find(automationId)?.AsButton();
    private MenuItem? FindMenuItem(string automationId) => Find(automationId)?.AsMenuItem();

    public Button? BtnNewTerminal => FindButton("Toolbar.BtnNewTerminal");
    public Button? BtnStopSession => FindButton("Toolbar.BtnStopSession");
    public Button? BtnArrange => FindButton("Toolbar.BtnArrange");
    public Button? BtnArrangeDropdown => FindButton("Toolbar.BtnArrangeDropdown");
    public Button? BtnClearOutput => FindButton("Toolbar.BtnClearOutput");

    // Compatibility aliases
    public Button? BtnCascade => BtnArrange;
    public Button? BtnTileHorizontal => FindButton("Toolbar.BtnTileHorizontal") ?? FindButton("Toolbar.Arrange.TileHorizontal");
    public Button? BtnTileVertical => FindButton("Toolbar.BtnTileVertical") ?? FindButton("Toolbar.Arrange.TileVertical");
    public Button? BtnRestoreAll => FindButton("Toolbar.BtnRestoreAll") ?? FindButton("Toolbar.Arrange.RestoreAll");

    public MenuItem? MenuFile => FindMenuItem("Menu.File");
    public MenuItem? MenuWindow => FindMenuItem("Menu.Window");
    public MenuItem? MenuView => FindMenuItem("Menu.View");

    public AutomationElement? MainMenu => Find("MainMenu");
    public AutomationElement? MainToolBar => Find("MainToolBar");
    public AutomationElement? StatusBar => Find("Workbench.StatusBar");
    public Button? BtnToggleDiagnostic => FindButton("Toolbar.BtnToggleDiagnostic");

    public AutomationElement? NavigationPane => Find("Pane.Navigation");
    public AutomationElement? PropertiesPane => Find("Pane.Properties");
    public AutomationElement? DiagnosticsPane => Find("Pane.Diagnostics");

    public string GetStatusBarMessage()
    {
        var elem = Find("Workbench.StatusBar.StatusMessage");
        return elem?.Name ?? elem?.AsLabel()?.Text ?? string.Empty;
    }

    public string GetStatusBarActiveDocTitle()
    {
        var elem = Find("Workbench.StatusBar.ActiveDocTitle");
        return elem?.Name ?? elem?.AsLabel()?.Text ?? string.Empty;
    }

    public MdiWorkspacePage GetMdiWorkspace()
    {
        var container = UiaWait.Until(() =>
        {
            var elem = _window.FindFirstDescendant(cf => cf.ByAutomationId("Workbench.MdiContainer"))
                    ?? _window.FindFirstDescendant(cf => cf.ByAutomationId("Workbench.MdiCanvasArea"));
            if (elem != null) return elem;

            try
            {
                int pid = _window.Properties.ProcessId.Value;
                var desktop = _window.Automation.GetDesktop();
                var freshWindow = desktop.FindFirstDescendant(cf => cf.ByProcessId(pid).And(cf.ByAutomationId("MainWindow")));
                if (freshWindow != null)
                {
                    return freshWindow.FindFirstDescendant(cf => cf.ByAutomationId("Workbench.MdiContainer"))
                        ?? freshWindow.FindFirstDescendant(cf => cf.ByAutomationId("Workbench.MdiCanvasArea"));
                }
            }
            catch { }
            return null;
        },
        timeout: TimeSpan.FromSeconds(30),
        message: "MdiContainer not found in MainWindow");

        return new MdiWorkspacePage(container);
    }

    public void ClickNewTerminal()
    {
        UiaWait.Until(() => BtnNewTerminal, timeout: TimeSpan.FromSeconds(15), message: "Toolbar NewTerminal button not found").Invoke();
    }

    public void ClickCascade()
    {
        UiaWait.Until(() => BtnArrange, timeout: TimeSpan.FromSeconds(15), message: "Toolbar Arrange button not found").Invoke();
    }

    public void ClickTileHorizontal()
        => ClickArrangeVariant("Toolbar.Arrange.TileHorizontal", "Menu.Window.TileHorizontal");

    public void ClickTileVertical()
        => ClickArrangeVariant("Toolbar.Arrange.TileVertical", "Menu.Window.TileVertical");

    public void ClickRestoreAll()
        => ClickArrangeVariant("Toolbar.Arrange.RestoreAll", "Menu.Window.RestoreAll");

    private void ClickArrangeVariant(string toolbarItemId, string windowMenuItemId)
    {
        try
        {
            var dropBtn = BtnArrangeDropdown;
            if (dropBtn != null)
            {
                dropBtn.Invoke();
                UiaWait.Until(
                    () => FindMenuItem(toolbarItemId),
                    timeout: TimeSpan.FromSeconds(5),
                    message: $"Arrange dropdown item {toolbarItemId} not found").Invoke();
                return;
            }
        }
        catch (TimeoutException)
        {
            // 下拉 Popup 未出现时改走窗口菜单
        }

        UiaWait.Until(() => MenuWindow, timeout: TimeSpan.FromSeconds(5), message: "Window menu not found").Invoke();
        UiaWait.Until(() => FindMenuItem("Menu.Window.Arrange"), timeout: TimeSpan.FromSeconds(5), message: "Arrange submenu not found").Invoke();
        UiaWait.Until(() => FindMenuItem(windowMenuItemId), timeout: TimeSpan.FromSeconds(5), message: $"{windowMenuItemId} not found").Invoke();
    }
}
