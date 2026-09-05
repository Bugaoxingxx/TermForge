using FlaUI.Core.AutomationElements;
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

    public Button? BtnNewTerminal => _window.FindFirstDescendant(cf => cf.ByAutomationId("Toolbar.BtnNewTerminal"))?.AsButton();
    public Button? BtnStopSession => _window.FindFirstDescendant(cf => cf.ByAutomationId("Toolbar.BtnStopSession"))?.AsButton();
    public Button? BtnCascade => _window.FindFirstDescendant(cf => cf.ByAutomationId("Toolbar.BtnCascade"))?.AsButton();
    public Button? BtnTileHorizontal => _window.FindFirstDescendant(cf => cf.ByAutomationId("Toolbar.BtnTileHorizontal"))?.AsButton();
    public Button? BtnTileVertical => _window.FindFirstDescendant(cf => cf.ByAutomationId("Toolbar.BtnTileVertical"))?.AsButton();
    public Button? BtnRestoreAll => _window.FindFirstDescendant(cf => cf.ByAutomationId("Toolbar.BtnRestoreAll"))?.AsButton();
    public Button? BtnClearOutput => _window.FindFirstDescendant(cf => cf.ByAutomationId("Toolbar.BtnClearOutput"))?.AsButton();

    public MenuItem? MenuFile => _window.FindFirstDescendant(cf => cf.ByAutomationId("Menu.File"))?.AsMenuItem();
    public MenuItem? MenuWindow => _window.FindFirstDescendant(cf => cf.ByAutomationId("Menu.Window"))?.AsMenuItem();
    public MenuItem? MenuView => _window.FindFirstDescendant(cf => cf.ByAutomationId("Menu.View"))?.AsMenuItem();

    public AutomationElement? NavigationPane => _window.FindFirstDescendant(cf => cf.ByAutomationId("Pane.Navigation"));
    public AutomationElement? PropertiesPane => _window.FindFirstDescendant(cf => cf.ByAutomationId("Pane.Properties"));
    public AutomationElement? DiagnosticsPane => _window.FindFirstDescendant(cf => cf.ByAutomationId("Pane.Diagnostics"));

    public string GetStatusBarMessage()
    {
        var elem = _window.FindFirstDescendant(cf => cf.ByAutomationId("Workbench.StatusBar.StatusMessage"));
        return elem?.Name ?? elem?.AsLabel()?.Text ?? string.Empty;
    }

    public string GetStatusBarActiveDocTitle()
    {
        var elem = _window.FindFirstDescendant(cf => cf.ByAutomationId("Workbench.StatusBar.ActiveDocTitle"));
        return elem?.Name ?? elem?.AsLabel()?.Text ?? string.Empty;
    }

    public MdiWorkspacePage GetMdiWorkspace()
    {
        var container = UiaWait.Until(() => _window.FindFirstDescendant(cf => cf.ByAutomationId("Workbench.MdiContainer")),
            message: "MdiContainer not found in MainWindow");
        return new MdiWorkspacePage(container);
    }

    public void ClickNewTerminal()
    {
        UiaWait.Until(() => BtnNewTerminal, message: "Toolbar NewTerminal button not found").Invoke();
    }

    public void ClickCascade()
    {
        UiaWait.Until(() => BtnCascade, message: "Toolbar Cascade button not found").Invoke();
    }

    public void ClickTileHorizontal()
    {
        UiaWait.Until(() => BtnTileHorizontal, message: "Toolbar TileHorizontal button not found").Invoke();
    }

    public void ClickTileVertical()
    {
        UiaWait.Until(() => BtnTileVertical, message: "Toolbar TileVertical button not found").Invoke();
    }

    public void ClickRestoreAll()
    {
        UiaWait.Until(() => BtnRestoreAll, message: "Toolbar RestoreAll button not found").Invoke();
    }
}
