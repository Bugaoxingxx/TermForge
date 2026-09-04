using AgentTerminal.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AgentTerminal.Docking.ViewModels;

/// <summary>
/// 终端文档视图模型，作为 AvalonDock LayoutDocument 的数据上下文
/// 持有 ITerminalSession 实例，实现 UI 停靠与底层会话解耦
/// </summary>
public partial class TerminalDocumentViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Terminal";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isActive;

    public ITerminalSession? Session { get; }

    public TerminalDocumentViewModel(ITerminalSession? session = null, string title = "Terminal")
    {
        Session = session;
        Title = title;
    }
}
