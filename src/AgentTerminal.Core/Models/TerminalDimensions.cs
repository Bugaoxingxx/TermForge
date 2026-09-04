namespace AgentTerminal.Core.Models;

/// <summary>
/// 表示终端视口的行列网格尺寸，保证符合 Windows ConPTY (COORD) 规格
/// </summary>
public readonly record struct TerminalDimensions
{
    public const int MinColumns = 1;
    public const int MaxColumns = short.MaxValue;
    public const int MinRows = 1;
    public const int MaxRows = short.MaxValue;

    public int Columns { get; init; }
    public int Rows { get; init; }

    public TerminalDimensions(int columns, int rows)
    {
        if (columns < MinColumns || columns > MaxColumns)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                columns,
                $"Columns must be between {MinColumns} and {MaxColumns}.");
        }

        if (rows < MinRows || rows > MaxRows)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rows),
                rows,
                $"Rows must be between {MinRows} and {MaxRows}.");
        }

        Columns = columns;
        Rows = rows;
    }

    public static TerminalDimensions Default => new(120, 30);

    public override string ToString() => $"{Columns}x{Rows}";
}
