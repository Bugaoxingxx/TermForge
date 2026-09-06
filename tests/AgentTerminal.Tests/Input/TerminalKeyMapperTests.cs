using System.Windows.Input;
using AgentTerminal.Terminal.Input;
using Xunit;

namespace AgentTerminal.Tests.Input;

public class TerminalKeyMapperTests
{
    [Fact]
    public void MapKeyToVtSequence_ReturnAndBackspace_ShouldReturnExpectedSequences()
    {
        Assert.Equal("\r", TerminalKeyMapper.MapKeyToVtSequence(Key.Return, ModifierKeys.None));
        Assert.Equal("\x7F", TerminalKeyMapper.MapKeyToVtSequence(Key.Back, ModifierKeys.None));
    }

    [Fact]
    public void MapKeyToVtSequence_TabAndShiftTab_ShouldReturnExpectedSequences()
    {
        Assert.Equal("\t", TerminalKeyMapper.MapKeyToVtSequence(Key.Tab, ModifierKeys.None));
        Assert.Equal("\x1B[Z", TerminalKeyMapper.MapKeyToVtSequence(Key.Tab, ModifierKeys.Shift));
    }

    [Fact]
    public void MapKeyToVtSequence_ArrowKeys_ShouldReturnCorrectSequences()
    {
        Assert.Equal("\x1B[A", TerminalKeyMapper.MapKeyToVtSequence(Key.Up, ModifierKeys.None));
        Assert.Equal("\x1B[B", TerminalKeyMapper.MapKeyToVtSequence(Key.Down, ModifierKeys.None));
        Assert.Equal("\x1B[C", TerminalKeyMapper.MapKeyToVtSequence(Key.Right, ModifierKeys.None));
        Assert.Equal("\x1B[D", TerminalKeyMapper.MapKeyToVtSequence(Key.Left, ModifierKeys.None));

        // Ctrl + Arrows
        Assert.Equal("\x1B[1;5A", TerminalKeyMapper.MapKeyToVtSequence(Key.Up, ModifierKeys.Control));
        Assert.Equal("\x1B[1;5B", TerminalKeyMapper.MapKeyToVtSequence(Key.Down, ModifierKeys.Control));
    }

    [Fact]
    public void MapKeyToVtSequence_CtrlCombinations_ShouldReturnAsciiControlCodes()
    {
        Assert.Equal("\x01", TerminalKeyMapper.MapKeyToVtSequence(Key.A, ModifierKeys.Control));
        Assert.Equal("\x03", TerminalKeyMapper.MapKeyToVtSequence(Key.C, ModifierKeys.Control));
        Assert.Equal("\x04", TerminalKeyMapper.MapKeyToVtSequence(Key.D, ModifierKeys.Control));
        Assert.Equal("\x1A", TerminalKeyMapper.MapKeyToVtSequence(Key.Z, ModifierKeys.Control));
        Assert.Equal("\0", TerminalKeyMapper.MapKeyToVtSequence(Key.Space, ModifierKeys.Control));
    }

    [Fact]
    public void MapKeyToVtSequence_FunctionKeys_ShouldReturnExpectedSequences()
    {
        Assert.Equal("\x1BOP", TerminalKeyMapper.MapKeyToVtSequence(Key.F1, ModifierKeys.None));
        Assert.Equal("\x1BOQ", TerminalKeyMapper.MapKeyToVtSequence(Key.F2, ModifierKeys.None));
        Assert.Equal("\x1B[15~", TerminalKeyMapper.MapKeyToVtSequence(Key.F5, ModifierKeys.None));
        Assert.Equal("\x1B[24~", TerminalKeyMapper.MapKeyToVtSequence(Key.F12, ModifierKeys.None));
    }

    [Fact]
    public void MapKeyToVtSequence_RegularKeyWithoutModifiers_ShouldReturnNull()
    {
        // Letters and numbers without Ctrl should return null so TextInput event processes them
        Assert.Null(TerminalKeyMapper.MapKeyToVtSequence(Key.A, ModifierKeys.None));
        Assert.Null(TerminalKeyMapper.MapKeyToVtSequence(Key.D1, ModifierKeys.None));
    }
}
