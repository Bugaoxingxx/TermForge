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
            return appWindow?.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
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
    public Button? BtnCascade => FindButton("Toolbar.BtnCascade");
    public Button? BtnTileHorizontal => FindButton("Toolbar.BtnTileHorizontal");
    public Button? BtnTileVertical => FindButton("Toolbar.BtnTileVertical");
    public Button? BtnRestoreAll => FindButton("Toolbar.BtnRestoreAll");
    public Button? BtnClearOutput => FindButton("Toolbar.BtnClearOutput");

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
        UiaWait.Until(() => BtnCascade, timeout: TimeSpan.FromSeconds(15), message: "Toolbar Cascade button not found").Invoke();
    }

    public void ClickTileHorizontal()
    {
        UiaWait.Until(() => BtnTileHorizontal, timeout: TimeSpan.FromSeconds(15), message: "Toolbar TileHorizontal button not found").Invoke();
    }

    public void ClickTileVertical()
    {
        UiaWait.Until(() => BtnTileVertical, timeout: TimeSpan.FromSeconds(15), message: "Toolbar TileVertical button not found").Invoke();
    }

    public void ClickRestoreAll()
    {
        UiaWait.Until(() => BtnRestoreAll, timeout: TimeSpan.FromSeconds(15), message: "Toolbar RestoreAll button not found").Invoke();
    }
}
