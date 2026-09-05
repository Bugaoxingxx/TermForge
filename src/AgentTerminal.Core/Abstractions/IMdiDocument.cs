using AgentTerminal.Core.Models;

namespace AgentTerminal.Core.Abstractions;

/// <summary>
/// MDI 文档接口，定义工作区内子文档窗口的核心数据与窗口状态契约
/// </summary>
public interface IMdiDocument
{
    /// <summary>文档唯一标识符</summary>
    string Id { get; }

    /// <summary>文档标题</summary>
    string Title { get; set; }

    /// <summary>是否为当前活动文档</summary>
    bool IsActive { get; set; }

    /// <summary>子窗口状态（Normal / Minimized / Maximized）</summary>
    MdiWindowState WindowState { get; set; }

    /// <summary>子窗口在工作区内的 X 坐标</summary>
    double Left { get; set; }

    /// <summary>子窗口在工作区内的 Y 坐标</summary>
    double Top { get; set; }

    /// <summary>子窗口宽度</summary>
    double Width { get; set; }

    /// <summary>子窗口高度</summary>
    double Height { get; set; }

    /// <summary>窗口层叠 Z 序</summary>
    int ZIndex { get; set; }

    /// <summary>绑定的终端底层会话</summary>
    ITerminalSession? Session { get; }
}
