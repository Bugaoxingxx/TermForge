namespace AgentTerminal.Core.Models;

/// <summary>
/// 终端单元格模型，表示网格中的一个字符位置及其样式
/// </summary>
public readonly record struct Cell : IEquatable<Cell>
{
    public char Character { get; init; }
    public TerminalColor Foreground { get; init; }
    public TerminalColor Background { get; init; }
    public CellAttributes Attributes { get; init; }

    public static Cell Empty => new(' ', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);

    public Cell(char character, TerminalColor foreground, TerminalColor background, CellAttributes attributes = CellAttributes.None)
    {
        Character = character;
        Foreground = foreground;
        Background = background;
        Attributes = attributes;
    }

    public bool IsEmpty => Character == ' ' && Foreground.IsDefault && Background.IsDefault && Attributes == CellAttributes.None;
}
