namespace AgentTerminal.Core.Models;

/// <summary>
/// MDI 子文档窗口在工作区内的状态
/// </summary>
public enum MdiWindowState
{
    /// <summary>正常浮动窗口状态，可自由移动和缩放</summary>
    Normal = 0,

    /// <summary>最小化状态，收拢到工作区底部托盘</summary>
    Minimized = 1,

    /// <summary>最大化状态，填充整个 MDI 工作区容器</summary>
    Maximized = 2
}
