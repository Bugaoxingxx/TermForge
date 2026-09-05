using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AgentTerminal.Core.Models;
using AgentTerminal.Docking.ViewModels;

namespace AgentTerminal.Docking.Controls;

/// <summary>
/// MdiChildWindow 交互逻辑：支持标题栏拖拽位移、双击最大化/还原、8向拉伸及焦点置顶
/// </summary>
public partial class MdiChildWindow : UserControl
{
    private Point _dragStartMousePos;
    private double _dragStartLeft;
    private double _dragStartTop;
    private bool _isDragging;

    public MdiChildWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is TerminalDocumentViewModel vm)
        {
            UpdateVisualState(vm.WindowState);
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(TerminalDocumentViewModel.WindowState))
                {
                    UpdateVisualState(vm.WindowState);
                }
            };
        }
    }

    private void UpdateVisualState(MdiWindowState state)
    {
        if (ResizeGrid != null)
        {
            ResizeGrid.Visibility = state == MdiWindowState.Normal ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private IInputElement? GetReferenceElement()
    {
        return (Window.GetWindow(this) as IInputElement) ?? (FindParentCanvas() as IInputElement);
    }

    private Canvas? FindParentCanvas()
    {
        DependencyObject? current = this;
        while (current != null)
        {
            current = VisualTreeHelper.GetParent(current);
            if (current is Canvas canvas)
            {
                return canvas;
            }
        }
        return null;
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is TerminalDocumentViewModel vm)
        {
            vm.IsActive = true;
        }
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not TerminalDocumentViewModel vm) return;

        vm.IsActive = true;

        if (e.ClickCount == 2)
        {
            // 双击标题栏在 Normal 和 Maximized 之间切换
            if (vm.WindowState == MdiWindowState.Maximized)
            {
                vm.Restore();
            }
            else
            {
                vm.Maximize();
            }
            e.Handled = true;
            return;
        }

        // 仅在 Normal 状态支持拖拽
        if (vm.WindowState == MdiWindowState.Normal && e.LeftButton == MouseButtonState.Pressed)
        {
            var refElement = GetReferenceElement();
            if (refElement == null) return;

            _isDragging = true;
            _dragStartMousePos = e.GetPosition(refElement);
            _dragStartLeft = vm.Left;
            _dragStartTop = vm.Top;
            TitleBar.CaptureMouse();
            e.Handled = true;
        }
    }

    private void OnTitleBarMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || DataContext is not TerminalDocumentViewModel vm) return;

        var refElement = GetReferenceElement();
        if (refElement == null) return;

        Point currentMousePos = e.GetPosition(refElement);
        double deltaX = currentMousePos.X - _dragStartMousePos.X;
        double deltaY = currentMousePos.Y - _dragStartMousePos.Y;

        double newLeft = _dragStartLeft + deltaX;
        double newTop = _dragStartTop + deltaY;

        var canvas = FindParentCanvas();
        double maxLeft = canvas != null && canvas.ActualWidth > 60 ? canvas.ActualWidth - 60 : 3000;
        double maxTop = canvas != null && canvas.ActualHeight > 40 ? canvas.ActualHeight - 40 : 2000;

        vm.Left = Math.Max(0, Math.Min(newLeft, maxLeft));
        vm.Top = Math.Max(0, Math.Min(newTop, maxTop));
    }

    private void OnTitleBarMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            TitleBar.ReleaseMouseCapture();
        }
    }

    private void OnTitleBarLostMouseCapture(object sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void OnMaximizeRestoreClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TerminalDocumentViewModel vm) return;

        if (vm.WindowState == MdiWindowState.Maximized)
        {
            vm.Restore();
        }
        else
        {
            vm.Maximize();
        }
    }

    // 8 向拉伸手柄处理
    private void OnResizeLeft(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not TerminalDocumentViewModel vm || vm.WindowState != MdiWindowState.Normal) return;

        double newWidth = vm.Width - e.HorizontalChange;
        if (newWidth >= MinWidth && newWidth >= 320)
        {
            vm.Left += e.HorizontalChange;
            vm.Width = newWidth;
        }
    }

    private void OnResizeRight(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not TerminalDocumentViewModel vm || vm.WindowState != MdiWindowState.Normal) return;

        double newWidth = vm.Width + e.HorizontalChange;
        if (newWidth >= MinWidth && newWidth >= 320)
        {
            vm.Width = newWidth;
        }
    }

    private void OnResizeTop(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not TerminalDocumentViewModel vm || vm.WindowState != MdiWindowState.Normal) return;

        double newHeight = vm.Height - e.VerticalChange;
        if (newHeight >= MinHeight && newHeight >= 220)
        {
            vm.Top += e.VerticalChange;
            vm.Height = newHeight;
        }
    }

    private void OnResizeBottom(object sender, DragDeltaEventArgs e)
    {
        if (DataContext is not TerminalDocumentViewModel vm || vm.WindowState != MdiWindowState.Normal) return;

        double newHeight = vm.Height + e.VerticalChange;
        if (newHeight >= MinHeight && newHeight >= 220)
        {
            vm.Height = newHeight;
        }
    }

    private void OnResizeTopLeft(object sender, DragDeltaEventArgs e)
    {
        OnResizeTop(sender, e);
        OnResizeLeft(sender, e);
    }

    private void OnResizeTopRight(object sender, DragDeltaEventArgs e)
    {
        OnResizeTop(sender, e);
        OnResizeRight(sender, e);
    }

    private void OnResizeBottomLeft(object sender, DragDeltaEventArgs e)
    {
        OnResizeBottom(sender, e);
        OnResizeLeft(sender, e);
    }

    private void OnResizeBottomRight(object sender, DragDeltaEventArgs e)
    {
        OnResizeBottom(sender, e);
        OnResizeRight(sender, e);
    }
}
