using System;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
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

    private MenuItem? FindMenuItem(string automationId) => Find(automationId)?.AsMenuItem();

    public MenuItem? MenuFile => FindMenuItem("Menu.File");
    public MenuItem? MenuWindow => FindMenuItem("Menu.Window");
    public MenuItem? MenuView => FindMenuItem("Menu.View");
    public MenuItem? MenuHelp => FindMenuItem("Menu.Help");

    public AutomationElement? MainMenu => Find("MainMenu");
    public AutomationElement? StatusBar => Find("Workbench.StatusBar");

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
        => ClickMenuPath("Menu.File", "Menu.File.New", "Menu.File.NewTerminal");

    public void ClickCascade()
        => ClickMenuPath("Menu.Window", "Menu.Window.Arrange", "Menu.Window.Cascade");

    public void ClickTileHorizontal()
        => ClickMenuPath("Menu.Window", "Menu.Window.Arrange", "Menu.Window.TileHorizontal");

    public void ClickTileVertical()
        => ClickMenuPath("Menu.Window", "Menu.Window.Arrange", "Menu.Window.TileVertical");

    public void ClickRestoreAll()
        => ClickMenuPath("Menu.Window", "Menu.Window.Arrange", "Menu.Window.RestoreAll");

    public void ClickToggleDiagnostic()
        => ClickMenuPath("Menu.View", "Menu.View.ToggleDiagnostic");

    private void ClickMenuPath(params string[] automationIds)
    {
        for (int i = 0; i < automationIds.Length; i++)
        {
            var id = automationIds[i];
            var item = UiaWait.Until(
                () => FindMenuItem(id),
                timeout: TimeSpan.FromSeconds(8),
                message: $"Menu item {id} not found");

            bool isLeaf = i == automationIds.Length - 1;
            if (isLeaf)
            {
                InvokeMenuItem(item);
            }
            else
            {
                ExpandMenuItem(item);
            }
        }
    }

    private static void ExpandMenuItem(MenuItem item)
    {
        if (item.Patterns.ExpandCollapse.IsSupported)
        {
            var pattern = item.Patterns.ExpandCollapse.Pattern;
            if (pattern.ExpandCollapseState.Value != ExpandCollapseState.Expanded)
            {
                pattern.Expand();
            }
            return;
        }

        InvokeMenuItem(item);
    }

    private static void InvokeMenuItem(MenuItem item)
    {
        if (item.Patterns.Invoke.IsSupported)
        {
            item.Invoke();
            return;
        }

        if (item.Patterns.Toggle.IsSupported)
        {
            item.Patterns.Toggle.Pattern.Toggle();
            return;
        }

        item.Click();
    }
}
