using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AgentTerminal.Terminal.Rendering;

/// <summary>
/// WPF 原生高性能终端呈现控件（Phase 4 重点实现）
/// 基于 DrawingVisual / DrawingContext 实现字符网格绘制，杜绝 TextBlock 海量对象开销
/// </summary>
public class TerminalControl : Control
{
    static TerminalControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TerminalControl),
            new FrameworkPropertyMetadata(typeof(TerminalControl)));
    }

    public TerminalControl()
    {
        Focusable = true;
        Background = Brushes.Black;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(Background, null, new Rect(RenderSize));
    }
}
