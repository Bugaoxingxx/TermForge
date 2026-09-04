using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;

namespace AgentTerminal.Terminal.Buffer;

/// <summary>
/// 终端视口与回滚历史缓冲区实现
/// </summary>
public class TerminalBuffer : ITerminalBuffer
{
    private int _scrollbackLineCount;

    public TerminalDimensions Dimensions { get; private set; }

    public int CursorX { get; private set; }

    public int CursorY { get; private set; }

    public bool IsCursorVisible { get; set; } = true;

    public int MaxScrollbackLines { get; }

    /// <summary>当前回滚区包含的行数</summary>
    public int ScrollbackLineCount => _scrollbackLineCount;

    /// <summary>总行数 = 视口行数 + 回滚历史行数</summary>
    public int TotalLines => Dimensions.Rows + _scrollbackLineCount;

    public TerminalBuffer(int columns = 120, int rows = 30, int maxScrollbackLines = 20000)
    {
        if (maxScrollbackLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxScrollbackLines),
                maxScrollbackLines,
                "MaxScrollbackLines must be non-negative.");
        }

        Dimensions = new TerminalDimensions(columns, rows);
        MaxScrollbackLines = maxScrollbackLines;
    }

    /// <summary>
    /// 追加回滚历史行（达到 MaxScrollbackLines 时自动上限截断）
    /// </summary>
    public void AppendScrollbackLines(int count = 1)
    {
        if (count <= 0) return;
        _scrollbackLineCount = Math.Min(MaxScrollbackLines, _scrollbackLineCount + count);
    }

    /// <summary>
    /// 调整终端行列尺寸，并校验合法性与调整光标位置
    /// </summary>
    public void Resize(int columns, int rows)
    {
        Dimensions = new TerminalDimensions(columns, rows);
        CursorX = Math.Min(CursorX, Dimensions.Columns - 1);
        CursorY = Math.Min(CursorY, Dimensions.Rows - 1);
    }

    /// <summary>
    /// 设置光标坐标（0-indexed）
    /// </summary>
    public void SetCursorPosition(int x, int y)
    {
        if (x < 0 || x >= Dimensions.Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"CursorX must be within [0, {Dimensions.Columns - 1}].");
        }

        if (y < 0 || y >= Dimensions.Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"CursorY must be within [0, {Dimensions.Rows - 1}].");
        }

        CursorX = x;
        CursorY = y;
    }

    /// <summary>
    /// 清空回滚历史和光标位置
    /// </summary>
    public void Clear()
    {
        _scrollbackLineCount = 0;
        CursorX = 0;
        CursorY = 0;
    }
}
