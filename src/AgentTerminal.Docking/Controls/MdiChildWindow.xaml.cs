using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using AgentTerminal.Core.Models;
using AgentTerminal.Docking.ViewModels;

namespace AgentTerminal.Docking.Controls;

/// <summary>
/// MdiChildWindow 交互逻辑：支持标题栏拖拽位移、双击最大化/还原、8向拉伸及焦点置顶
/// </summary>
public partial class MdiChildWindow : UserControl
{
    private Point _dragStartPoint;
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
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            TitleBar.CaptureMouse();
            TitleBar.MouseMove += OnTitleBarMouseMove;
            TitleBar.MouseLeftButtonUp += OnTitleBarMouseUp;
            e.Handled = true;
        }
    }

    private void OnTitleBarMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || DataContext is not TerminalDocumentViewModel vm) return;

        Point currentPoint = e.GetPosition(Parent as IInputElement);
        if (Parent is FrameworkElement parentElement)
        {
            double newLeft = currentPoint.X - _dragStartPoint.X;
            double newTop = currentPoint.Y - _dragStartPoint.Y;

            // 保持在合理可视范围内
            vm.Left = Math.Max(0, Math.Min(newLeft, parentElement.ActualWidth - 60));
            vm.Top = Math.Max(0, Math.Min(newTop, parentElement.ActualHeight - 40));
        }
    }

    private void OnTitleBarMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            TitleBar.ReleaseMouseCapture();
            TitleBar.MouseMove -= OnTitleBarMouseMove;
            TitleBar.MouseLeftButtonUp -= OnTitleBarMouseUp;
        }
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
