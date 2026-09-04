namespace AgentTerminal.Core.Models;

/// <summary>
/// 表示终端视口的行列网格尺寸
/// </summary>
/// <param name="Columns">列数（字符宽度，默认 80 或 120）</param>
/// <param name="Rows">行数（字符高度，默认 24 或 30）</param>
public readonly record struct TerminalDimensions(int Columns, int Rows)
{
    public static TerminalDimensions Default => new(120, 30);

    public override string ToString() => $"{Columns}x{Rows}";
}
