using AgentTerminal.Core.Models;

namespace AgentTerminal.Core.Abstractions;

/// <summary>
/// 终端缓冲区抽象，解耦 PTY 数据流与 WPF 渲染呈现
/// </summary>
public interface ITerminalBuffer
{
    /// <summary>当前可见视口尺寸</summary>
    TerminalDimensions Dimensions { get; }

    /// <summary>当前光标列坐标（0-indexed）</summary>
    int CursorX { get; }

    /// <summary>当前光标行坐标（0-indexed）</summary>
    int CursorY { get; }

    /// <summary>光标是否可见</summary>
    bool IsCursorVisible { get; }

    /// <summary>滚动回滚区行数上限</summary>
    int MaxScrollbackLines { get; }

    /// <summary>当前回滚区包含的历史总行数</summary>
    int TotalLines { get; }

    /// <summary>
    /// 调整缓冲区几何尺寸
    /// </summary>
    void Resize(int columns, int rows);

    /// <summary>
    /// 清空缓冲区内容
    /// </summary>
    void Clear();
}
