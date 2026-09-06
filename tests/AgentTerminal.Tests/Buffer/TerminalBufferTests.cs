using AgentTerminal.Core.Models;
using AgentTerminal.Terminal.Buffer;
using Xunit;

namespace AgentTerminal.Tests.Buffer;

public class TerminalBufferTests
{
    [Fact]
    public void Constructor_ShouldInitializeDimensionsAndDefaults()
    {
        // Arrange & Act
        var buffer = new TerminalBuffer(columns: 100, rows: 40, maxScrollbackLines: 15000);

        // Assert
        Assert.Equal(100, buffer.Dimensions.Columns);
        Assert.Equal(40, buffer.Dimensions.Rows);
        Assert.Equal(15000, buffer.MaxScrollbackLines);
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
        Assert.True(buffer.IsCursorVisible);
        Assert.Equal(0, buffer.ScrollbackLineCount);
        Assert.Equal(40, buffer.TotalLines);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(-5, 30)]
    [InlineData(100, 0)]
    [InlineData(100, -10)]
    [InlineData(40000, 30)]
    [InlineData(100, 40000)]
    public void Constructor_WithInvalidDimensions_ShouldThrowArgumentOutOfRangeException(int columns, int rows)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalBuffer(columns, rows));
    }

    [Fact]
    public void Constructor_WithNegativeMaxScrollbackLines_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalBuffer(80, 24, maxScrollbackLines: -1));
    }

    [Fact]
    public void Resize_ShouldUpdateDimensions()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);

        // Act
        buffer.Resize(120, 50);

        // Assert
        Assert.Equal(120, buffer.Dimensions.Columns);
        Assert.Equal(50, buffer.Dimensions.Rows);
        Assert.Equal(50, buffer.TotalLines);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-1, 25)]
    [InlineData(80, 0)]
    [InlineData(80, -5)]
    public void Resize_WithInvalidDimensions_ShouldThrowArgumentOutOfRangeException(int columns, int rows)
    {
        var buffer = new TerminalBuffer(80, 25);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Resize(columns, rows));
    }

    [Fact]
    public void AppendScrollbackLines_ShouldIncreaseTotalLines_AndRespectMaxLimit()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 24, maxScrollbackLines: 50);

        // Act: Append 20 lines
        buffer.AppendScrollbackLines(20);

        // Assert
        Assert.Equal(20, buffer.ScrollbackLineCount);
        Assert.Equal(44, buffer.TotalLines); // 24 + 20

        // Act: Append 50 more lines (should cap at maxScrollbackLines = 50)
        buffer.AppendScrollbackLines(50);

        // Assert
        Assert.Equal(50, buffer.ScrollbackLineCount);
        Assert.Equal(74, buffer.TotalLines); // 24 + 50
    }

    [Fact]
    public void Clear_ShouldResetScrollbackLinesAndCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 24);
        buffer.AppendScrollbackLines(100);
        buffer.SetCursorPosition(10, 5);

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.ScrollbackLineCount);
        Assert.Equal(24, buffer.TotalLines);
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
    }

    [Fact]
    public void WriteChar_ShouldFillCell_AdvanceCursor_AndPreserveAttributes()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var fg = TerminalColor.From16Color(1); // Red
        var bg = TerminalColor.Default;
        var attr = CellAttributes.Bold;

        // Act
        buffer.WriteChar('A', fg, bg, attr);

        // Assert
        var cell = buffer.GetCell(0, 0);
        Assert.Equal('A', cell.Character);
        Assert.Equal(fg, cell.Foreground);
        Assert.Equal(bg, cell.Background);
        Assert.True((cell.Attributes & CellAttributes.Bold) != 0);
        Assert.Equal(1, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
    }

    [Fact]
    public void WriteChar_AtEndOfLine_ShouldAutoWrap()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 5);
        buffer.SetCursorPosition(9, 0); // last column

        // Act
        buffer.WriteChar('Z', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);

        // Assert: char is written at last column, cursor wraps to next line column 0
        var cell = buffer.GetCell(9, 0);
        Assert.Equal('Z', cell.Character);
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(1, buffer.CursorY);
    }

    [Fact]
    public void WriteChar_AtBottomRight_ShouldAutoWrapAndScroll()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 3, maxScrollbackLines: 10);
        buffer.SetCursorPosition(9, 2); // bottom right

        // Act
        buffer.WriteChar('!', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);

        // Assert: triggered scroll up, cursor is at (0, 2)
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(2, buffer.CursorY);
        Assert.Equal(1, buffer.ScrollbackLineCount);

        // Cell '!' was at row 2, now scrolled up to row 1
        var cell = buffer.GetCell(9, 1);
        Assert.Equal('!', cell.Character);
    }

    [Fact]
    public void SetCursorPosition_OutOfBounds_ShouldClampWithoutThrowing()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);

        // Act & Assert: Upper bounds
        buffer.SetCursorPosition(150, 80);
        Assert.Equal(79, buffer.CursorX);
        Assert.Equal(24, buffer.CursorY);

        // Act & Assert: Lower bounds
        buffer.SetCursorPosition(-10, -5);
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
    }

    [Fact]
    public void ScrollUp_BeyondMaxScrollback_ShouldEvictOldestFifo()
    {
        // Arrange: 3 rows viewport, max 2 scrollback lines
        var buffer = new TerminalBuffer(columns: 10, rows: 3, maxScrollbackLines: 2);

        // Write unique line identifiers
        buffer.WriteChar('1', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.CarriageReturn();
        buffer.NewLine();
        buffer.WriteChar('2', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.CarriageReturn();
        buffer.NewLine();
        buffer.WriteChar('3', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.CarriageReturn();
        buffer.NewLine(); // Scroll 1: line '1' enters scrollback. Viewport has [2, 3, empty]
        buffer.WriteChar('4', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.CarriageReturn();
        buffer.NewLine(); // Scroll 2: line '2' enters scrollback. Scrollback has [1, 2]
        buffer.WriteChar('5', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.CarriageReturn();
        buffer.NewLine(); // Scroll 3: line '3' enters scrollback. Scrollback max=2, '1' evicted, scrollback has [2, 3]

        // Assert
        Assert.Equal(2, buffer.ScrollbackLineCount);

        // -1 is newest history ('3'), -2 is oldest history ('2')
        Assert.Equal('3', buffer.GetCell(0, -1).Character);
        Assert.Equal('2', buffer.GetCell(0, -2).Character);
    }

    [Fact]
    public void AlternateScreen_ShouldBeIsolatedFromMainScreen()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 20, rows: 5, maxScrollbackLines: 10);
        buffer.WriteChar('M', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.CarriageReturn();
        buffer.NewLine();
        buffer.WriteChar('1', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);

        // Act: Switch to alternate screen
        buffer.UseAlternateScreenBuffer();
        Assert.True(buffer.IsAlternateScreen);
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);
        // Alternate screen is blank
        Assert.Equal(' ', buffer.GetCell(0, 0).Character);

        // Write in alternate screen and scroll
        buffer.WriteChar('A', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        for (int i = 0; i < 10; i++)
        {
            buffer.NewLine();
        }
        // Alternate screen should NOT add to scrollback
        Assert.Equal(0, buffer.ScrollbackLineCount);

        // Act: Switch back to main screen
        buffer.UseMainScreenBuffer();
        Assert.False(buffer.IsAlternateScreen);

        // Assert: Main screen contents and cursor are intact
        Assert.Equal('M', buffer.GetCell(0, 0).Character);
        Assert.Equal('1', buffer.GetCell(0, 1).Character);
    }

    [Fact]
    public void Resize_ShouldPreserveContent_AndClampCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 120, rows: 30);
        buffer.WriteChar('H', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.WriteChar('i', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
        buffer.SetCursorPosition(100, 25);

        // Act: Shrink to 80x20
        buffer.Resize(80, 20);

        // Assert
        Assert.Equal(80, buffer.Dimensions.Columns);
        Assert.Equal(20, buffer.Dimensions.Rows);
        Assert.Equal(79, buffer.CursorX);
        Assert.Equal(19, buffer.CursorY);
        Assert.Equal('H', buffer.GetCell(0, 0).Character);
        Assert.Equal('i', buffer.GetCell(1, 0).Character);
    }

    [Fact]
    public void WriteChar_WideCharacter_ShouldTakeTwoColumns()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 20, rows: 5);

        // Act: Write Chinese character '你'
        buffer.WriteChar('你', TerminalColor.Default, TerminalColor.Default, CellAttributes.None);

        // Assert
        var lead = buffer.GetCell(0, 0);
        var cont = buffer.GetCell(1, 0);
        Assert.Equal('你', lead.Character);
        Assert.True((lead.Attributes & CellAttributes.WideLeading) != 0);
        Assert.True((cont.Attributes & CellAttributes.WideContinuation) != 0);
        Assert.Equal(2, buffer.CursorX);
    }
}
