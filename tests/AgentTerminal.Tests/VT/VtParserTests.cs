using AgentTerminal.Core.Models;
using AgentTerminal.Terminal.Buffer;
using AgentTerminal.Terminal.VT;
using Xunit;

namespace AgentTerminal.Tests.VT;

public class VtParserTests
{
    [Fact]
    public void Parse_ChunkedCsiSequence_ShouldPreserveStateAcrossChunks()
    {
        // Arrange: "ESC[3" then "1mA"
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);

        // Act
        parser.Parse("\x1B[3");
        parser.Parse("1mA");

        // Assert: SGR 31 = Red, 'A' written with Red foreground
        var cell = buffer.GetCell(0, 0);
        Assert.Equal('A', cell.Character);
        Assert.Equal(TerminalColor.FromIndex(1), cell.Foreground);
        Assert.Equal(1, buffer.CursorX);
    }

    [Fact]
    public void Parse_ControlSequences_ShouldNotProduceRawControlCodeFragments()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);

        // Act: Input contains cursor hide, clear screen, OSC title, then "hello"
        parser.Parse("\x1B[?25l\x1B[2J\x1B]0;title\x07hello");

        // Assert: Buffer only has 'h','e','l','l','o'
        Assert.False(buffer.IsCursorVisible);
        Assert.Equal('h', buffer.GetCell(0, 0).Character);
        Assert.Equal('e', buffer.GetCell(1, 0).Character);
        Assert.Equal('l', buffer.GetCell(2, 0).Character);
        Assert.Equal('l', buffer.GetCell(3, 0).Character);
        Assert.Equal('o', buffer.GetCell(4, 0).Character);
        Assert.Equal(' ', buffer.GetCell(5, 0).Character); // No trailing raw characters
        Assert.Equal(5, buffer.CursorX);
    }

    [Fact]
    public void Parse_CursorPosition_ShouldSetCorrectCoordinates()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);

        // Act: ESC[5;10H -> row 5, col 10 (1-based) -> 4, 9 (0-based)
        parser.Parse("\x1B[5;10H");

        // Assert
        Assert.Equal(9, buffer.CursorX);
        Assert.Equal(4, buffer.CursorY);
    }

    [Fact]
    public void Parse_CursorRelativeMovements_ShouldUpdatePosition()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);
        buffer.SetCursorPosition(10, 10);

        // Act: Up 3, Right 5, Down 2, Left 4
        parser.Parse("\x1B[3A\x1B[5C\x1B[2B\x1B[4D");

        // Assert:
        // Y: 10 - 3 + 2 = 9
        // X: 10 + 5 - 4 = 11
        Assert.Equal(11, buffer.CursorX);
        Assert.Equal(9, buffer.CursorY);
    }

    [Fact]
    public void Parse_EraseInDisplayAndLine_ShouldClearCorrectAreas()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 20, rows: 5);
        var parser = new VtParser(buffer);
        parser.Parse("Line1\r\nLine2\r\nLine3");

        // Act: Clear line 2 entirely
        parser.Parse("\x1B[2;1H\x1B[2K");

        // Assert
        Assert.Equal(' ', buffer.GetCell(0, 1).Character);
        Assert.Equal(' ', buffer.GetCell(4, 1).Character);
        // Line 1 still intact
        Assert.Equal('L', buffer.GetCell(0, 0).Character);

        // Act: Clear entire screen
        parser.Parse("\x1B[2J");
        Assert.Equal(' ', buffer.GetCell(0, 0).Character);
        Assert.Equal(' ', buffer.GetCell(0, 2).Character);
    }

    [Fact]
    public void Parse_SgrTrueColorAnd256Color_ShouldApplyColorsAndStyles()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);

        // Act: TrueColor foreground (10, 20, 30), 256-color background (42), Bold + Underline
        parser.Parse("\x1B[38;2;10;20;30;48;5;42;1;4mX");

        // Assert
        var cell = buffer.GetCell(0, 0);
        Assert.Equal('X', cell.Character);
        Assert.Equal(TerminalColor.FromRgb(10, 20, 30), cell.Foreground);
        Assert.Equal(TerminalColor.FromIndex(42), cell.Background);
        Assert.True((cell.Attributes & CellAttributes.Bold) != 0);
        Assert.True((cell.Attributes & CellAttributes.Underline) != 0);

        // Reset
        parser.Parse("\x1B[0mY");
        var cellY = buffer.GetCell(1, 0);
        Assert.Equal('Y', cellY.Character);
        Assert.Equal(TerminalColor.Default, cellY.Foreground);
        Assert.Equal(TerminalColor.Default, cellY.Background);
        Assert.Equal(CellAttributes.None, cellY.Attributes);
    }

    [Fact]
    public void Parse_DecPrivateModes_ShouldToggleCursorAndAlternateScreen()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 40, rows: 10);
        var parser = new VtParser(buffer);

        // Act: Hide cursor
        parser.Parse("\x1B[?25l");
        Assert.False(buffer.IsCursorVisible);

        // Act: Show cursor
        parser.Parse("\x1B[?25h");
        Assert.True(buffer.IsCursorVisible);

        // Act: Enter alternate screen
        parser.Parse("\x1B[?1049h");
        Assert.True(buffer.IsAlternateScreen);

        // Act: Exit alternate screen
        parser.Parse("\x1B[?1049l");
        Assert.False(buffer.IsAlternateScreen);
    }

    [Fact]
    public void Parse_OscTitle_ShouldTriggerTitleChangedEvent()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);
        string? capturedTitle = null;
        parser.TitleChanged += (_, title) => capturedTitle = title;

        // Act
        parser.Parse("\x1B]0;TermForge Window\x07");

        // Assert
        Assert.Equal("TermForge Window", capturedTitle);
    }

    [Fact]
    public void Parse_UnknownEscapeSequences_ShouldBeSafelyDiscarded()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);
        var parser = new VtParser(buffer);

        // Act: Unknown sequence ESC [ 999 z followed by "Safe"
        parser.Parse("\x1B[999zSafe");

        // Assert: "Safe" appears starting at column 0
        Assert.Equal('S', buffer.GetCell(0, 0).Character);
        Assert.Equal('a', buffer.GetCell(1, 0).Character);
        Assert.Equal('f', buffer.GetCell(2, 0).Character);
        Assert.Equal('e', buffer.GetCell(3, 0).Character);
        Assert.Equal(4, buffer.CursorX);
    }
}
