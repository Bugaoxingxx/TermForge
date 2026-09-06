using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace AgentTerminal.Docking.Controls;

/// <summary>
/// MMC 工作台窗格容器控件，为停靠面板（导航、诊断、属性）提供标准的 UI 自动化对等体支持。
/// </summary>
public class WorkbenchPane : ContentControl
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new WorkbenchPaneAutomationPeer(this);
    }
}

public class WorkbenchPaneAutomationPeer : FrameworkElementAutomationPeer
{
    public WorkbenchPaneAutomationPeer(WorkbenchPane owner) : base(owner)
    {
    }

    protected override string GetClassNameCore() => nameof(WorkbenchPane);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override bool IsControlElementCore() => true;
}
