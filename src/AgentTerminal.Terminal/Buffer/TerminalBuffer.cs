using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;

namespace AgentTerminal.Terminal.Buffer;

/// <summary>
/// 终端视口与回滚历史缓冲区实现（Phase 2 重点实现）
/// </summary>
public class TerminalBuffer : ITerminalBuffer
{
    public TerminalDimensions Dimensions { get; private set; }

    public int CursorX { get; private set; }

    public int CursorY { get; private set; }

    public bool IsCursorVisible { get; private set; } = true;

    public int MaxScrollbackLines { get; }

    public int TotalLines => Dimensions.Rows;

    public TerminalBuffer(int columns = 120, int rows = 30, int maxScrollbackLines = 20000)
    {
        Dimensions = new TerminalDimensions(columns, rows);
        MaxScrollbackLines = maxScrollbackLines;
    }

    public void Resize(int columns, int rows)
    {
        Dimensions = new TerminalDimensions(columns, rows);
    }

    public void Clear()
    {
        CursorX = 0;
        CursorY = 0;
    }
}
