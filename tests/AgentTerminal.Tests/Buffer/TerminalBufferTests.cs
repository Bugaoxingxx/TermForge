using AgentTerminal.Terminal.Buffer;
using FluentAssertions;
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
        buffer.Dimensions.Columns.Should().Be(100);
        buffer.Dimensions.Rows.Should().Be(40);
        buffer.MaxScrollbackLines.Should().Be(15000);
        buffer.CursorX.Should().Be(0);
        buffer.CursorY.Should().Be(0);
        buffer.IsCursorVisible.Should().BeTrue();
    }

    [Fact]
    public void Resize_ShouldUpdateDimensions()
    {
        // Arrange
        var buffer = new TerminalBuffer(columns: 80, rows: 25);

        // Act
        buffer.Resize(120, 50);

        // Assert
        buffer.Dimensions.Columns.Should().Be(120);
        buffer.Dimensions.Rows.Should().Be(50);
    }
}
