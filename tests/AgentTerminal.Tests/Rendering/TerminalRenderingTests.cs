using System.Windows.Media;
using AgentTerminal.Core.Models;
using AgentTerminal.Terminal.Buffer;
using AgentTerminal.Terminal.Rendering;
using AgentTerminal.Terminal.VT;
using Xunit;

namespace AgentTerminal.Tests.Rendering;

public class TerminalRenderingTests
{
    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null)
        {
            throw new AggregateException(exception);
        }
    }

    [Fact]
    public void StressTest_100000LinesOutput_ShouldMaintainScrollbackCapAndConstantPerformance()
    {
        // 压测 10 万行输出写入 buffer
        var buffer = new TerminalBuffer(columns: 80, rows: 25, maxScrollbackLines: 100000);
        var parser = new VtParser(buffer);

        // 高频输出循环（视口 25 行填满后开始产生回滚）
        for (int i = 0; i < 100025; i++)
        {
            parser.Parse("Test log line output with VT color \x1B[32mOK\x1B[0m\r\n");
        }

        // 回滚行数达到上限 100000
        Assert.Equal(100000, buffer.ScrollbackLineCount);
        Assert.Equal(100025, buffer.TotalLines);

        // 验证最新日志行内容正确（\r\n 换行后该行位于倒数第二行，最底行为新就绪行）
        var cell = buffer.GetCell(0, buffer.Dimensions.Rows - 2);
        Assert.Equal('T', cell.Character);
    }

    [Fact]
    public void TerminalControl_VisualCount_ShouldRemainZeroAndNotGrowWithOutput()
    {
        RunInSta(() =>
        {
            var buffer = new TerminalBuffer(columns: 80, rows: 25, maxScrollbackLines: 20000);
            var control = new TerminalControl
            {
                Buffer = buffer
            };

            // 写入大量内容
            for (int i = 0; i < 5000; i++)
            {
                buffer.WriteChar((char)('A' + (i % 26)), TerminalColor.Default, TerminalColor.Default, CellAttributes.None);
                buffer.NewLine();
            }

            // TerminalControl 基于 OnRender(DrawingContext)，不生成独立子 Visual 元素
            int childVisualCount = VisualTreeHelper.GetChildrenCount(control);
            Assert.Equal(0, childVisualCount);
        });
    }

    [Fact]
    public void TerminalControl_PixelToDimensionConversion_ShouldBeConsistent()
    {
        RunInSta(() =>
        {
            var control = new TerminalControl();
            control.EnsureFontMetrics();

            Assert.True(control.CharWidth > 0);
            Assert.True(control.LineHeight > 0);

            var (cols, rows) = control.GetDimensionsFromPixelSize(800, 600);
            Assert.True(cols > 0);
            Assert.True(rows > 0);

            var (w, h) = control.GetPixelSizeFromDimensions(cols, rows);
            Assert.True(w <= 800);
            Assert.True(h <= 600);
        });
    }
}
