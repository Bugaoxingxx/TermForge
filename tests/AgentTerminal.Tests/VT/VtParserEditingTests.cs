using AgentTerminal.Terminal.Buffer;
using AgentTerminal.Terminal.VT;
using Xunit;

namespace AgentTerminal.Tests.VT;

public class VtParserEditingTests
{
    [Fact]
    public void InsertCharacters_ShouldShiftCharactersRight()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 3);
        var parser = new VtParser(buffer);

        // Write "ABCDE" at line 0
        parser.Parse("ABCDE");
        // Move cursor to column 1 ('B')
        parser.Parse("\x1B[1;2H");
        Assert.Equal(1, buffer.CursorX);

        // Act: Insert 2 characters (CSI 2 @)
        parser.Parse("\x1B[2@");

        // Assert: Line 0 should be 'A', ' ', ' ', 'B', 'C', 'D', 'E', ' '...
        Assert.Equal('A', buffer.GetCell(0, 0).Character);
        Assert.Equal(' ', buffer.GetCell(1, 0).Character);
        Assert.Equal(' ', buffer.GetCell(2, 0).Character);
        Assert.Equal('B', buffer.GetCell(3, 0).Character);
        Assert.Equal('C', buffer.GetCell(4, 0).Character);
        Assert.Equal('D', buffer.GetCell(5, 0).Character);
        Assert.Equal('E', buffer.GetCell(6, 0).Character);
        // Cursor remains at column 1
        Assert.Equal(1, buffer.CursorX);
    }

    [Fact]
    public void DeleteCharacters_ShouldShiftCharactersLeftAndFillEndWithBlanks()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 3);
        var parser = new VtParser(buffer);

        // Write "ABCDEFGHIJ"
        parser.Parse("ABCDEFGHIJ");
        // Move cursor to column 2 ('C')
        parser.Parse("\x1B[1;3H");
        Assert.Equal(2, buffer.CursorX);

        // Act: Delete 3 characters (CSI 3 P) -> deletes C, D, E
        parser.Parse("\x1B[3P");

        // Assert: Line 0 should be 'A', 'B', 'F', 'G', 'H', 'I', 'J', ' ', ' ', ' '
        Assert.Equal('A', buffer.GetCell(0, 0).Character);
        Assert.Equal('B', buffer.GetCell(1, 0).Character);
        Assert.Equal('F', buffer.GetCell(2, 0).Character);
        Assert.Equal('G', buffer.GetCell(3, 0).Character);
        Assert.Equal('H', buffer.GetCell(4, 0).Character);
        Assert.Equal('I', buffer.GetCell(5, 0).Character);
        Assert.Equal('J', buffer.GetCell(6, 0).Character);
        Assert.Equal(' ', buffer.GetCell(7, 0).Character);
        Assert.Equal(' ', buffer.GetCell(8, 0).Character);
        Assert.Equal(' ', buffer.GetCell(9, 0).Character);
        Assert.Equal(2, buffer.CursorX);
    }

    [Fact]
    public void EraseCharacters_ShouldReplaceWithBlanksWithoutMovingCursor()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 3);
        var parser = new VtParser(buffer);

        // Write "ABCDEFGHIJ"
        parser.Parse("ABCDEFGHIJ");
        // Move cursor to column 2 ('C')
        parser.Parse("\x1B[1;3H");

        // Act: Erase 3 characters (CSI 3 X) -> replaces C, D, E with spaces
        parser.Parse("\x1B[3X");

        // Assert: Line 0 should be 'A', 'B', ' ', ' ', ' ', 'F', 'G', 'H', 'I', 'J'
        Assert.Equal('A', buffer.GetCell(0, 0).Character);
        Assert.Equal('B', buffer.GetCell(1, 0).Character);
        Assert.Equal(' ', buffer.GetCell(2, 0).Character);
        Assert.Equal(' ', buffer.GetCell(3, 0).Character);
        Assert.Equal(' ', buffer.GetCell(4, 0).Character);
        Assert.Equal('F', buffer.GetCell(5, 0).Character);
        Assert.Equal(2, buffer.CursorX);
    }

    [Fact]
    public void InsertLines_ShouldShiftLinesDown()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 4);
        var parser = new VtParser(buffer);

        // Write 3 lines: "Row0", "Row1", "Row2"
        parser.Parse("Row0\r\nRow1\r\nRow2");
        Assert.Equal('R', buffer.GetCell(0, 0).Character);
        Assert.Equal('R', buffer.GetCell(0, 1).Character);
        Assert.Equal('R', buffer.GetCell(0, 2).Character);

        // Move cursor to line 1
        parser.Parse("\x1B[2;1H");
        Assert.Equal(1, buffer.CursorY);

        // Act: Insert 1 line (CSI 1 L)
        parser.Parse("\x1B[1L");

        // Assert: Row 0 is "Row0", Row 1 is empty, Row 2 is "Row1", Row 3 is "Row2"
        Assert.Equal('0', buffer.GetCell(3, 0).Character);
        Assert.Equal(' ', buffer.GetCell(0, 1).Character);
        Assert.Equal('1', buffer.GetCell(3, 2).Character);
        Assert.Equal('2', buffer.GetCell(3, 3).Character);
    }

    [Fact]
    public void DeleteLines_ShouldShiftLinesUp()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 4);
        var parser = new VtParser(buffer);

        // Write 4 lines: "Row0", "Row1", "Row2", "Row3"
        parser.Parse("Row0\r\nRow1\r\nRow2\r\nRow3");

        // Move cursor to line 1
        parser.Parse("\x1B[2;1H");

        // Act: Delete 1 line (CSI 1 M) -> deletes Row1
        parser.Parse("\x1B[1M");

        // Assert: Row 0 is "Row0", Row 1 is "Row2", Row 2 is "Row3", Row 3 is empty
        Assert.Equal('0', buffer.GetCell(3, 0).Character);
        Assert.Equal('2', buffer.GetCell(3, 1).Character);
        Assert.Equal('3', buffer.GetCell(3, 2).Character);
        Assert.Equal(' ', buffer.GetCell(0, 3).Character);
    }

    [Fact]
    public void InsertLines_WithCountExceedingScrollRegion_ShouldClearFromCursorDown()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 4);
        var parser = new VtParser(buffer);

        parser.Parse("Row0\r\nRow1\r\nRow2\r\nRow3");
        // Move cursor to line 1
        parser.Parse("\x1B[2;1H");

        // Act: Insert far more lines than the scroll region can hold（等价于从光标行向下整体清空）
        parser.Parse("\x1B[99999L");

        // Assert: Row 0 保留，Row 1..3 全部清空，且不越界
        Assert.Equal('0', buffer.GetCell(3, 0).Character);
        Assert.Equal(' ', buffer.GetCell(0, 1).Character);
        Assert.Equal(' ', buffer.GetCell(0, 2).Character);
        Assert.Equal(' ', buffer.GetCell(0, 3).Character);
    }

    [Fact]
    public void DeleteLines_WithCountExceedingScrollRegion_ShouldClearFromCursorDown()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 10, rows: 4);
        var parser = new VtParser(buffer);

        parser.Parse("Row0\r\nRow1\r\nRow2\r\nRow3");
        // Move cursor to line 1
        parser.Parse("\x1B[2;1H");

        // Act: Delete far more lines than the scroll region can hold（等价于从光标行向下整体清空）
        parser.Parse("\x1B[99999M");

        // Assert: Row 0 保留，Row 1..3 全部清空，且不越界
        Assert.Equal('0', buffer.GetCell(3, 0).Character);
        Assert.Equal(' ', buffer.GetCell(0, 1).Character);
        Assert.Equal(' ', buffer.GetCell(0, 2).Character);
        Assert.Equal(' ', buffer.GetCell(0, 3).Character);
    }

    [Fact]
    public void CursorSaveAndRestore_Esc7Esc8_ShouldRestorePosition()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 20, rows: 10);
        var parser = new VtParser(buffer);

        // Move to (5, 4) and save with ESC 7
        parser.Parse("\u001B[5;6H\u001B7");
        Assert.Equal(5, buffer.CursorX);
        Assert.Equal(4, buffer.CursorY);

        // Move elsewhere (0, 0)
        parser.Parse("\u001B[1;1H");
        Assert.Equal(0, buffer.CursorX);
        Assert.Equal(0, buffer.CursorY);

        // Act: Restore with ESC 8
        parser.Parse("\u001B8");

        // Assert
        Assert.Equal(5, buffer.CursorX);
        Assert.Equal(4, buffer.CursorY);
    }

    [Fact]
    public void CursorSaveAndRestore_CsiSCsiU_ShouldRestorePosition()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 20, rows: 10);
        var parser = new VtParser(buffer);

        // Move to (7, 3) and save with CSI s
        parser.Parse("\u001B[4;8H\u001B[s");
        Assert.Equal(7, buffer.CursorX);
        Assert.Equal(3, buffer.CursorY);

        // Move elsewhere
        parser.Parse("\u001B[1;1H");

        // Act: Restore with CSI u
        parser.Parse("\u001B[u");

        // Assert
        Assert.Equal(7, buffer.CursorX);
        Assert.Equal(3, buffer.CursorY);
    }

    [Fact]
    public void ParameterAccumulation_ShouldClampWithoutOverflow()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);

        // Act: Parse huge number that would overflow 32-bit int if unclamped
        parser.Parse("\x1B[99999999999999999999C");

        // Assert: Cursor was moved forward by clamped count (clamped to 79)
        Assert.Equal(79, buffer.CursorX);
    }
}
