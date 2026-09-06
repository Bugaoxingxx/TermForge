using System.Drawing;
using FlaUI.Core.AutomationElements;
using AgentTerminal.UITests.Infrastructure;

namespace AgentTerminal.UITests.Pages;

public class MdiChildWindowPage
{
    private readonly AutomationElement _element;

    public MdiChildWindowPage(AutomationElement element)
    {
        _element = element;
    }

    public AutomationElement Element => _element;

    public string Title
    {
        get
        {
            var titleElem = _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.Title"));
            if (titleElem != null)
            {
                var text = titleElem.Name;
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
                return titleElem.AsLabel()?.Text ?? string.Empty;
            }
            return _element.Name;
        }
    }

    public Rectangle BoundingRectangle => _element.BoundingRectangle;

    public AutomationElement? TitleBarElement =>
        _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.TitleBar"))
        ?? _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.Title"));

    public Button? MinimizeButton => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.BtnMinimize"))?.AsButton();
    public Button? MaxRestoreButton => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.BtnMaxRestore"))?.AsButton();
    public Button? CloseButton => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.BtnClose"))?.AsButton();

    public void ClickTitleBar()
    {
        var titleElem = _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.Title"));
        titleElem?.Click();
    }

    public void DragBy(int deltaX, int deltaY)
    {
        if (_element.Patterns.Transform.IsSupported)
        {
            var bounds = _element.BoundingRectangle;
            _element.Patterns.Transform.Pattern.Move(bounds.Left + deltaX, bounds.Top + deltaY);
            return;
        }

        var titleBar = TitleBarElement ?? throw new System.InvalidOperationException("TitleBar element not found");
        InputSimulator.Drag(titleBar, deltaX, deltaY);
    }

    public void DoubleClickTitleBar()
    {
        if (_element.Patterns.Window.IsSupported)
        {
            var win = _element.Patterns.Window.Pattern;
            if (win.WindowVisualState.Value == FlaUI.Core.Definitions.WindowVisualState.Maximized)
            {
                win.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Normal);
            }
            else
            {
                win.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
            }
            return;
        }

        var titleBar = TitleBarElement ?? throw new System.InvalidOperationException("TitleBar element not found");
        InputSimulator.DoubleClick(titleBar);
    }

    public AutomationElement? BottomRightThumb => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.Thumb.BottomRight"));

    public void ResizeBottomRight(int deltaX, int deltaY)
    {
        if (_element.Patterns.Transform.IsSupported)
        {
            var bounds = _element.BoundingRectangle;
            _element.Patterns.Transform.Pattern.Resize(bounds.Width + deltaX, bounds.Height + deltaY);
            return;
        }

        var thumb = BottomRightThumb ?? throw new System.InvalidOperationException("BottomRightThumb element not found");
        InputSimulator.Drag(thumb, deltaX, deltaY);
    }

    public void MaximizeOrRestore()
    {
        UiaWait.Until(() => MaxRestoreButton, message: "MaxRestore button not found").Invoke();
    }

    public void Minimize()
    {
        UiaWait.Until(() => MinimizeButton, message: "Minimize button not found").Invoke();
    }

    public void Close()
    {
        UiaWait.Until(() => CloseButton, message: "Close button not found").Invoke();
    }

    public TerminalDebugViewPage GetDebugView()
    {
        return new TerminalDebugViewPage(_element);
    }
}
