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

    /// <summary>
    /// 移动子窗口（使用 UI 自动化标准 ITransformProvider.Move）。
    /// </summary>
    public void DragBy(int deltaX, int deltaY)
    {
        if (!_element.Patterns.Transform.IsSupported)
        {
            throw new System.InvalidOperationException("Transform pattern is not supported on this MDI child window");
        }

        var bounds = _element.BoundingRectangle;
        _element.Patterns.Transform.Pattern.Move(bounds.Left + deltaX, bounds.Top + deltaY);
    }

    /// <summary>
    /// 双击标题栏切换最大化/还原（使用 UI 自动化标准 IWindowProvider.SetWindowVisualState）。
    /// </summary>
    public void DoubleClickTitleBar()
    {
        if (!_element.Patterns.Window.IsSupported)
        {
            throw new System.InvalidOperationException("Window pattern is not supported on this MDI child window");
        }

        var win = _element.Patterns.Window.Pattern;
        if (win.WindowVisualState.Value == FlaUI.Core.Definitions.WindowVisualState.Maximized)
        {
            win.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Normal);
        }
        else
        {
            win.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
        }
    }

    public AutomationElement? BottomRightThumb => _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.Thumb.BottomRight"));

    /// <summary>
    /// 拉伸右下角（使用 UI 自动化标准 ITransformProvider.Resize）。
    /// </summary>
    public void ResizeBottomRight(int deltaX, int deltaY)
    {
        if (!_element.Patterns.Transform.IsSupported)
        {
            throw new System.InvalidOperationException("Transform pattern is not supported on this MDI child window");
        }

        var bounds = _element.BoundingRectangle;
        _element.Patterns.Transform.Pattern.Resize(bounds.Width + deltaX, bounds.Height + deltaY);
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

    public void Activate()
    {
        var btn = _element.FindFirstDescendant(cf => cf.ByAutomationId("MdiWindow.BtnActivate"))?.AsButton();
        if (btn != null && btn.Patterns.Invoke.IsSupported)
        {
            btn.Invoke();
            return;
        }

        if (_element.Patterns.Invoke.IsSupported)
        {
            _element.Patterns.Invoke.Pattern.Invoke();
        }
        else if (_element.Patterns.SelectionItem.IsSupported)
        {
            _element.Patterns.SelectionItem.Pattern.Select();
        }
        else
        {
            try
            {
                _element.Focus();
            }
            catch { }
        }
    }

    public void EnsureDiagnosticMode()
    {
        if (_element.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.BtnStart")) == null)
        {
            Activate();
            System.Threading.Thread.Sleep(300);

            var menuView = _element.Automation.GetDesktop().FindFirstDescendant(cf => cf.ByAutomationId("Menu.View"))?.AsMenuItem();
            if (menuView != null)
            {
                if (menuView.Patterns.ExpandCollapse.IsSupported)
                {
                    menuView.Patterns.ExpandCollapse.Pattern.Expand();
                }
                else if (menuView.Patterns.Invoke.IsSupported)
                {
                    menuView.Invoke();
                }

                var toggle = _element.Automation.GetDesktop().FindFirstDescendant(cf => cf.ByAutomationId("Menu.View.ToggleDiagnostic"))?.AsMenuItem();
                if (toggle != null)
                {
                    if (toggle.Patterns.Invoke.IsSupported)
                    {
                        toggle.Invoke();
                    }
                    else if (toggle.Patterns.Toggle.IsSupported)
                    {
                        toggle.Patterns.Toggle.Pattern.Toggle();
                    }
                    else
                    {
                        toggle.Click();
                    }
                }
            }
            System.Threading.Thread.Sleep(500);
        }
    }

    public TerminalDebugViewPage GetDebugView()
    {
        EnsureDiagnosticMode();
        return new TerminalDebugViewPage(_element);
    }
}
