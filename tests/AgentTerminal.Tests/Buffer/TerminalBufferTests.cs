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
}
