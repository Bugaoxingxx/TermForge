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

    public Button? MinimizeButton => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.BtnMinimize"))?.AsButton();
    public Button? MaxRestoreButton => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.BtnMaxRestore"))?.AsButton();
    public Button? CloseButton => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.BtnClose"))?.AsButton();

    public void ClickTitleBar()
    {
        var titleElem = _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.Title"));
        titleElem?.Click();
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
