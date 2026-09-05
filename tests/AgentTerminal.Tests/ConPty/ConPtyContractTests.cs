using System.Text;
using AgentTerminal.Core.Models;
using AgentTerminal.Docking.ViewModels;
using AgentTerminal.Terminal.ConPty;
using Xunit;

namespace AgentTerminal.Tests.ConPty;

/// <summary>
/// ConPTY 会话契约与边界条件单元测试
/// </summary>
public class ConPtyContractTests
{
    [Fact]
    public async Task WriteAsync_WhenNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var profile = ShellProfile.CreatePowerShellCore();
        var session = new ConPtyTerminalSession(profile);

        // Act & Assert (Created 状态)
        Assert.Equal(TerminalState.Created, session.State);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.WriteAsync("test command\n"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.WriteAsync(new byte[] { 0x01 }));
    }

    [Fact]
    public async Task ResizeAsync_WhenNotRunning_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var profile = ShellProfile.CreatePowerShellCore();
        var session = new ConPtyTerminalSession(profile);

        // Act & Assert
        Assert.Equal(TerminalState.Created, session.State);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.ResizeAsync(80, 24));
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(-10, 24)]
    [InlineData(80, 0)]
    [InlineData(80, -5)]
    [InlineData(40000, 24)]
    public void TerminalDimensions_WithInvalidBounds_ShouldThrowArgumentOutOfRangeException(int cols, int rows)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalDimensions(cols, rows));
    }

    [Fact]
    public void Utf8Decoder_SplitAcrossChunkBoundary_ShouldDecodeWithoutReplacementCharacters()
    {
        // Arrange: "你好，TermForge！🚀"
        string expectedText = "你好，TermForge！🚀";
        byte[] fullBytes = Encoding.UTF8.GetBytes(expectedText);

        var decoder = Encoding.UTF8.GetDecoder();
        char[] charBuffer = new char[64];
        var sb = new StringBuilder();

        // 人为在第一个汉字 "你" (3字节) 的第2个字节处进行拆分
        // Act
        for (int i = 0; i < fullBytes.Length; i += 3)
        {
            int count = Math.Min(3, fullBytes.Length - i);
            int charsUsed = decoder.GetChars(fullBytes, i, count, charBuffer, 0, flush: false);
            sb.Append(charBuffer, 0, charsUsed);
        }

        // Flush
        int finalChars = decoder.GetChars(Array.Empty<byte>(), 0, 0, charBuffer, 0, flush: true);
        sb.Append(charBuffer, 0, finalChars);

        // Assert
        Assert.Equal(expectedText, sb.ToString());
    }

    [Fact]
    public void StartupInfoEx_OffsetsAndSize_ShouldMatchWin32Expected()
    {
        int size = System.Runtime.InteropServices.Marshal.SizeOf<PseudoConsoleApi.STARTUPINFOEX>();
        int cbOffset = (int)System.Runtime.InteropServices.Marshal.OffsetOf<PseudoConsoleApi.STARTUPINFOEX>("StartupInfo");
        int attrListOffset = (int)System.Runtime.InteropServices.Marshal.OffsetOf<PseudoConsoleApi.STARTUPINFOEX>("lpAttributeList");

        int hStdInputOffset = (int)System.Runtime.InteropServices.Marshal.OffsetOf<PseudoConsoleApi.STARTUPINFO>("hStdInput");
        int hStdOutputOffset = (int)System.Runtime.InteropServices.Marshal.OffsetOf<PseudoConsoleApi.STARTUPINFO>("hStdOutput");
        int hStdErrorOffset = (int)System.Runtime.InteropServices.Marshal.OffsetOf<PseudoConsoleApi.STARTUPINFO>("hStdError");

        Assert.Equal(112, size);
        Assert.Equal(0, cbOffset);
        Assert.Equal(104, attrListOffset);
        Assert.Equal(80, hStdInputOffset);
        Assert.Equal(88, hStdOutputOffset);
        Assert.Equal(96, hStdErrorOffset);
    }

    [Fact]
    public async Task ConPty_WithCmdExe_ShouldReceiveOutput()
    {
        var profile = new ShellProfile
        {
            Name = "CMD",
            ExecutablePath = System.IO.Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            Arguments = "/c echo HELLO_CONPTY_WORLD",
            WorkingDirectory = Environment.CurrentDirectory
        };

        var session = new ConPtyTerminalSession(profile);
        var sb = new StringBuilder();
        var tcs = new TaskCompletionSource<bool>();

        session.OutputReceived += (s, text) =>
        {
            sb.Append(text);
            if (sb.ToString().Contains("HELLO_CONPTY_WORLD"))
            {
                tcs.TrySetResult(true);
            }
        };

        await session.StartAsync();
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
        Assert.Same(tcs.Task, completed);
    }

    [Fact]
    public void TerminalDocumentViewModel_BoundedOutputBuffer_ShouldTruncateWhenExceedingLimit()
    {
        // Arrange
        var profile = ShellProfile.CreatePowerShellCore();
        var vm = new TerminalDocumentViewModel(profile);

        // Act: 写入超过 1 MiB 的文本 (例如 1.2 MiB)
        string chunk = new string('A', 100_000); // 100K
        for (int i = 0; i < 12; i++)
        {
            vm.AppendOutput(chunk);
        }

        // Assert
        Assert.True(vm.IsTruncated);
        Assert.True(vm.OutputText.Length <= TerminalDocumentViewModel.MaxOutputBufferSize + 200);
        Assert.Contains("输出已截断", vm.OutputText);

        // Act: 清空输出
        vm.ClearOutput();
        Assert.False(vm.IsTruncated);
        Assert.Empty(vm.OutputText);
    }
}
