using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;

namespace AgentTerminal.Terminal.Buffer;

/// <summary>
/// 终端视口与回滚历史缓冲区的高性能环形缓冲实现。
/// 支持单元格网格、光标定位收敛、行尾自动换行、清屏/清行、滚动区、主屏/备用屏隔离与尺寸调整内容保留。
/// </summary>
public class TerminalBuffer : ITerminalBuffer
{
    private sealed class ScreenState
    {
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int CursorX { get; set; }
        public int CursorY { get; set; }
        public int ScrollTop { get; set; }
        public int ScrollBottom { get; set; }

        public Cell[][] ViewportRows { get; set; }
        public int ViewportHead { get; set; }

        public ScreenState(int columns, int rows)
        {
            Columns = columns;
            Rows = rows;
            CursorX = 0;
            CursorY = 0;
            ScrollTop = 0;
            ScrollBottom = rows - 1;
            ViewportHead = 0;
            ViewportRows = new Cell[rows][];
            for (int i = 0; i < rows; i++)
            {
                ViewportRows[i] = CreateEmptyRow(columns);
            }
        }

        public Cell[] GetViewportRow(int r)
        {
            return ViewportRows[(ViewportHead + r) % Rows];
        }

        public void SetViewportRow(int r, Cell[] row)
        {
            ViewportRows[(ViewportHead + r) % Rows] = row;
        }
    }

    private readonly ScreenState _mainScreen;
    private ScreenState? _alternateScreen;
    private ScreenState _activeScreen;

    // 主屏专用的回滚历史环形缓冲
    private readonly Cell[][] _scrollback;
    private int _scrollbackHead; // 指向最旧历史行
    private int _scrollbackCount;

    public TerminalDimensions Dimensions { get; private set; }

    public int CursorX => _activeScreen.CursorX;
    public int CursorY => _activeScreen.CursorY;

    public bool IsCursorVisible { get; set; } = true;

    public int MaxScrollbackLines { get; }

    public int ScrollbackLineCount => _scrollbackCount;

    public int TotalLines => Dimensions.Rows + _scrollbackCount;

    public bool IsAlternateScreen => _activeScreen == _alternateScreen;

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
        MaxScrollbackLines = Math.Min(maxScrollbackLines, 100000);

        _mainScreen = new ScreenState(columns, rows);
        _activeScreen = _mainScreen;

        _scrollback = new Cell[MaxScrollbackLines][];
        _scrollbackHead = 0;
        _scrollbackCount = 0;
    }

    public static bool IsWideChar(char c)
    {
        return c switch
        {
            >= '\u1100' and <= '\u115F' => true, // Hangul Jamo
            >= '\u2E80' and <= '\uA4CF' and not '\u303F' => true, // CJK Radicals, Symbols, Ideographs
            >= '\uAC00' and <= '\uD7A3' => true, // Hangul Syllables
            >= '\uF900' and <= '\uFAFF' => true, // CJK Compatibility Ideographs
            >= '\uFE30' and <= '\uFE6F' => true, // CJK Compatibility Forms
            >= '\uFF01' and <= '\uFF60' => true, // Fullwidth Forms
            >= '\uFFE0' and <= '\uFFE6' => true,
            _ => false
        };
    }

    private static Cell[] CreateEmptyRow(int columns)
    {
        var row = new Cell[columns];
        Array.Fill(row, Cell.Empty);
        return row;
    }

    public Cell GetCell(int column, int row)
    {
        if (column < 0 || column >= Dimensions.Columns)
        {
            return Cell.Empty;
        }

        if (row >= 0 && row < Dimensions.Rows)
        {
            return _activeScreen.GetViewportRow(row)[column];
        }

        if (row < 0)
        {
            // 回滚历史：-1 为最近历史行，-k 为倒数第 k 行
            int historyIndex = -row;
            if (historyIndex <= _scrollbackCount && !IsAlternateScreen)
            {
                int physicalIndex = (_scrollbackHead + _scrollbackCount - historyIndex) % MaxScrollbackLines;
                if (physicalIndex < 0) physicalIndex += MaxScrollbackLines;
                var histRow = _scrollback[physicalIndex];
                if (histRow != null && column < histRow.Length)
                {
                    return histRow[column];
                }
            }
        }

        return Cell.Empty;
    }

    public ReadOnlySpan<Cell> GetRowSpan(int row)
    {
        if (row >= 0 && row < Dimensions.Rows)
        {
            return _activeScreen.GetViewportRow(row);
        }

        if (row < 0 && !IsAlternateScreen)
        {
            int historyIndex = -row;
            if (historyIndex <= _scrollbackCount)
            {
                int physicalIndex = (_scrollbackHead + _scrollbackCount - historyIndex) % MaxScrollbackLines;
                if (physicalIndex < 0) physicalIndex += MaxScrollbackLines;
                var histRow = _scrollback[physicalIndex];
                if (histRow != null)
                {
                    return histRow;
                }
            }
        }

        return ReadOnlySpan<Cell>.Empty;
    }

    public void CopyRow(int row, Span<Cell> destination)
    {
        var span = GetRowSpan(row);
        if (span.IsEmpty)
        {
            destination.Fill(Cell.Empty);
            return;
        }

        int copyLen = Math.Min(span.Length, destination.Length);
        span[..copyLen].CopyTo(destination);
        if (destination.Length > copyLen)
        {
            destination[copyLen..].Fill(Cell.Empty);
        }
    }

    public void SetCursorPosition(int x, int y)
    {
        _activeScreen.CursorX = Math.Clamp(x, 0, Dimensions.Columns - 1);
        _activeScreen.CursorY = Math.Clamp(y, 0, Dimensions.Rows - 1);
    }

    public void MoveCursor(int deltaX, int deltaY)
    {
        SetCursorPosition(_activeScreen.CursorX + deltaX, _activeScreen.CursorY + deltaY);
    }

    public void UseAlternateScreenBuffer()
    {
        if (IsAlternateScreen) return;
        _alternateScreen ??= new ScreenState(Dimensions.Columns, Dimensions.Rows);
        _activeScreen = _alternateScreen;
        _activeScreen.CursorX = 0;
        _activeScreen.CursorY = 0;
        EraseInDisplay(2);
    }

    public void UseMainScreenBuffer()
    {
        if (!IsAlternateScreen) return;
        _activeScreen = _mainScreen;
    }

    public void WriteChar(char c, TerminalColor foreground, TerminalColor background, CellAttributes attributes)
    {
        bool isWide = IsWideChar(c);

        // 若当前在最后一列且写入全角字符，需先行折行
        if (isWide && _activeScreen.CursorX >= Dimensions.Columns - 1)
        {
            var fillCell = new Cell(' ', foreground, background, attributes);
            SetViewportCell(_activeScreen.CursorX, _activeScreen.CursorY, fillCell);
            CarriageReturn();
            NewLine();
        }
        else if (_activeScreen.CursorX >= Dimensions.Columns)
        {
            CarriageReturn();
            NewLine();
        }

        if (isWide)
        {
            var leadCell = new Cell(c, foreground, background, attributes | CellAttributes.WideLeading);
            SetViewportCell(_activeScreen.CursorX, _activeScreen.CursorY, leadCell);
            _activeScreen.CursorX++;

            if (_activeScreen.CursorX < Dimensions.Columns)
            {
                var contCell = new Cell(' ', foreground, background, attributes | CellAttributes.WideContinuation);
                SetViewportCell(_activeScreen.CursorX, _activeScreen.CursorY, contCell);
                _activeScreen.CursorX++;
            }
        }
        else
        {
            var cell = new Cell(c, foreground, background, attributes);
            SetViewportCell(_activeScreen.CursorX, _activeScreen.CursorY, cell);
            _activeScreen.CursorX++;
        }

        // 行尾自动换行收敛
        if (_activeScreen.CursorX >= Dimensions.Columns)
        {
            CarriageReturn();
            NewLine();
        }
    }

    private void SetViewportCell(int x, int y, Cell cell)
    {
        if (x >= 0 && x < Dimensions.Columns && y >= 0 && y < Dimensions.Rows)
        {
            _activeScreen.GetViewportRow(y)[x] = cell;
        }
    }

    public void CarriageReturn()
    {
        _activeScreen.CursorX = 0;
    }

    public void NewLine()
    {
        if (_activeScreen.CursorY < _activeScreen.ScrollBottom)
        {
            _activeScreen.CursorY++;
        }
        else
        {
            ScrollUp(1);
        }
    }

    public void Backspace()
    {
        if (_activeScreen.CursorX > 0)
        {
            _activeScreen.CursorX--;
        }
    }

    public void Tab(int tabSize = 8)
    {
        if (tabSize <= 0) tabSize = 8;
        int nextTab = ((_activeScreen.CursorX / tabSize) + 1) * tabSize;
        _activeScreen.CursorX = Math.Min(nextTab, Dimensions.Columns - 1);
    }

    public void EraseInDisplay(int mode)
    {
        switch (mode)
        {
            case 0: // 光标至屏尾
                ClearLineSpan(_activeScreen.CursorY, _activeScreen.CursorX, Dimensions.Columns - _activeScreen.CursorX);
                for (int y = _activeScreen.CursorY + 1; y < Dimensions.Rows; y++)
                {
                    ClearLineSpan(y, 0, Dimensions.Columns);
                }
                break;

            case 1: // 屏首至光标
                for (int y = 0; y < _activeScreen.CursorY; y++)
                {
                    ClearLineSpan(y, 0, Dimensions.Columns);
                }
                ClearLineSpan(_activeScreen.CursorY, 0, _activeScreen.CursorX + 1);
                break;

            case 2: // 清空整屏
                for (int y = 0; y < Dimensions.Rows; y++)
                {
                    ClearLineSpan(y, 0, Dimensions.Columns);
                }
                break;

            case 3: // 清空整屏及回滚历史
                for (int y = 0; y < Dimensions.Rows; y++)
                {
                    ClearLineSpan(y, 0, Dimensions.Columns);
                }
                _scrollbackCount = 0;
                _scrollbackHead = 0;
                break;
        }
    }

    public void EraseInLine(int mode)
    {
        switch (mode)
        {
            case 0: // 光标至行尾
                ClearLineSpan(_activeScreen.CursorY, _activeScreen.CursorX, Dimensions.Columns - _activeScreen.CursorX);
                break;

            case 1: // 行首至光标
                ClearLineSpan(_activeScreen.CursorY, 0, _activeScreen.CursorX + 1);
                break;

            case 2: // 清空整行
                ClearLineSpan(_activeScreen.CursorY, 0, Dimensions.Columns);
                break;
        }
    }

    private void ClearLineSpan(int y, int startX, int length)
    {
        if (y < 0 || y >= Dimensions.Rows) return;
        var row = _activeScreen.GetViewportRow(y);
        int end = Math.Min(startX + length, row.Length);
        for (int x = Math.Max(0, startX); x < end; x++)
        {
            row[x] = Cell.Empty;
        }
    }

    public void ScrollUp(int lines = 1)
    {
        if (lines <= 0) return;

        bool isFullRegion = _activeScreen.ScrollTop == 0 && _activeScreen.ScrollBottom == Dimensions.Rows - 1;

        for (int i = 0; i < lines; i++)
        {
            if (isFullRegion)
            {
                var topRow = _activeScreen.GetViewportRow(0);

                // 仅主屏维护回滚历史
                if (!IsAlternateScreen && MaxScrollbackLines > 0)
                {
                    PushScrollback(topRow);
                }

                // 环形推进视口 Head，重用或新建行
                _activeScreen.ViewportHead = (_activeScreen.ViewportHead + 1) % Dimensions.Rows;
                var bottomRow = _activeScreen.GetViewportRow(Dimensions.Rows - 1);
                Array.Fill(bottomRow, Cell.Empty);
            }
            else
            {
                // 局部滚动区：仅滚动区内行上移，最顶行丢弃，底部补空白行，不进回滚历史
                var topRow = _activeScreen.GetViewportRow(_activeScreen.ScrollTop);
                for (int y = _activeScreen.ScrollTop; y < _activeScreen.ScrollBottom; y++)
                {
                    _activeScreen.SetViewportRow(y, _activeScreen.GetViewportRow(y + 1));
                }
                Array.Fill(topRow, Cell.Empty);
                _activeScreen.SetViewportRow(_activeScreen.ScrollBottom, topRow);
            }
        }
    }

    public void ScrollDown(int lines = 1)
    {
        if (lines <= 0) return;

        for (int i = 0; i < lines; i++)
        {
            var bottomRow = _activeScreen.GetViewportRow(_activeScreen.ScrollBottom);
            for (int y = _activeScreen.ScrollBottom; y > _activeScreen.ScrollTop; y--)
            {
                _activeScreen.SetViewportRow(y, _activeScreen.GetViewportRow(y - 1));
            }
            Array.Fill(bottomRow, Cell.Empty);
            _activeScreen.SetViewportRow(_activeScreen.ScrollTop, bottomRow);
        }
    }

    private void PushScrollback(Cell[] row)
    {
        // 制作或复用独立行副本
        var clone = new Cell[row.Length];
        row.CopyTo(clone, 0);

        if (_scrollbackCount < MaxScrollbackLines)
        {
            int targetIndex = (_scrollbackHead + _scrollbackCount) % MaxScrollbackLines;
            _scrollback[targetIndex] = clone;
            _scrollbackCount++;
        }
        else
        {
            // 已达上限：覆盖最旧行（FIFO淘汰）
            _scrollback[_scrollbackHead] = clone;
            _scrollbackHead = (_scrollbackHead + 1) % MaxScrollbackLines;
        }
    }

    public void AppendScrollbackLines(int count = 1)
    {
        if (count <= 0 || MaxScrollbackLines <= 0) return;
        for (int i = 0; i < count; i++)
        {
            PushScrollback(CreateEmptyRow(Dimensions.Columns));
        }
    }

    public void SetScrollRegion(int topRow, int bottomRow)
    {
        int clampedTop = Math.Clamp(topRow, 0, Dimensions.Rows - 1);
        int clampedBottom = Math.Clamp(bottomRow, 0, Dimensions.Rows - 1);

        if (clampedTop < clampedBottom)
        {
            _activeScreen.ScrollTop = clampedTop;
            _activeScreen.ScrollBottom = clampedBottom;
        }
        else
        {
            ResetScrollRegion();
        }
    }

    public void ResetScrollRegion()
    {
        _activeScreen.ScrollTop = 0;
        _activeScreen.ScrollBottom = Dimensions.Rows - 1;
    }

    public void Resize(int columns, int rows)
    {
        var newDimensions = new TerminalDimensions(columns, rows);
        if (newDimensions == Dimensions) return;

        int oldColumns = Dimensions.Columns;
        int oldRows = Dimensions.Rows;

        ResizeScreen(_mainScreen, columns, rows, oldColumns, oldRows);
        if (_alternateScreen != null)
        {
            ResizeScreen(_alternateScreen, columns, rows, oldColumns, oldRows);
        }

        // 调整回滚区各行尺寸
        if (columns != oldColumns)
        {
            for (int i = 0; i < _scrollbackCount; i++)
            {
                int idx = (_scrollbackHead + i) % MaxScrollbackLines;
                var oldRow = _scrollback[idx];
                if (oldRow != null)
                {
                    var newRow = new Cell[columns];
                    int copyLen = Math.Min(oldRow.Length, columns);
                    Array.Copy(oldRow, newRow, copyLen);
                    for (int c = copyLen; c < columns; c++)
                    {
                        newRow[c] = Cell.Empty;
                    }
                    _scrollback[idx] = newRow;
                }
            }
        }

        Dimensions = newDimensions;
    }

    private static void ResizeScreen(ScreenState screen, int newCols, int newRows, int oldCols, int oldRows)
    {
        var newViewport = new Cell[newRows][];
        int copyRows = Math.Min(oldRows, newRows);

        for (int r = 0; r < copyRows; r++)
        {
            var oldRow = screen.GetViewportRow(r);
            var newRow = new Cell[newCols];
            int copyCols = Math.Min(oldRow.Length, newCols);
            Array.Copy(oldRow, newRow, copyCols);
            for (int c = copyCols; c < newCols; c++)
            {
                newRow[c] = Cell.Empty;
            }
            newViewport[r] = newRow;
        }

        for (int r = copyRows; r < newRows; r++)
        {
            newViewport[r] = CreateEmptyRow(newCols);
        }

        screen.ViewportRows = newViewport;
        screen.ViewportHead = 0;
        screen.Columns = newCols;
        screen.Rows = newRows;
        screen.ScrollTop = 0;
        screen.ScrollBottom = newRows - 1;

        screen.CursorX = Math.Clamp(screen.CursorX, 0, newCols - 1);
        screen.CursorY = Math.Clamp(screen.CursorY, 0, newRows - 1);
    }

    public void Clear()
    {
        _scrollbackCount = 0;
        _scrollbackHead = 0;
        _mainScreen.CursorX = 0;
        _mainScreen.CursorY = 0;
        _mainScreen.ViewportHead = 0;
        for (int i = 0; i < _mainScreen.Rows; i++)
        {
            Array.Fill(_mainScreen.ViewportRows[i], Cell.Empty);
        }
        ResetScrollRegion();

        if (_alternateScreen != null)
        {
            _alternateScreen.CursorX = 0;
            _alternateScreen.CursorY = 0;
            _alternateScreen.ViewportHead = 0;
            for (int i = 0; i < _alternateScreen.Rows; i++)
            {
                Array.Fill(_alternateScreen.ViewportRows[i], Cell.Empty);
            }
        }
    }
}
