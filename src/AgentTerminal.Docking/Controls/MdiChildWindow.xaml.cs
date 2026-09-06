using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new MdiChildWindowAutomationPeer(this);
    }

    private IInputElement? GetReferenceElement()
    {
        return (Window.GetWindow(this) as IInputElement) ?? (FindParentCanvas() as IInputElement);
    }

    internal Canvas? FindParentCanvas()
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

    private void OnActivateClicked(object sender, RoutedEventArgs e)
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

/// <summary>
/// MdiChildWindow 的 UI 自动化对等体，提供 ITransformProvider（窗口拖动与尺寸拉伸）及 IWindowProvider（窗口状态切换）支持，
/// 确保无物理交互桌面/CI 环境下 UI 自动化测试及辅助技术的标准化控制。
/// </summary>
public class MdiChildWindowAutomationPeer : FrameworkElementAutomationPeer, ITransformProvider, IWindowProvider, ISelectionItemProvider, IInvokeProvider
{
    public MdiChildWindowAutomationPeer(MdiChildWindow owner) : base(owner)
    {
    }

    protected override string GetClassNameCore() => nameof(MdiChildWindow);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    protected override bool IsControlElementCore() => true;

    public override object? GetPattern(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Transform || 
            patternInterface == PatternInterface.Window ||
            patternInterface == PatternInterface.SelectionItem ||
            patternInterface == PatternInterface.Invoke)
        {
            return this;
        }
        return base.GetPattern(patternInterface);
    }

    // --- IInvokeProvider ---
    public void Invoke()
    {
        if (Owner is MdiChildWindow window && window.DataContext is TerminalDocumentViewModel vm)
        {
            window.Dispatcher.Invoke(() => vm.IsActive = true);
        }
    }

    // --- ISelectionItemProvider ---
    public bool IsSelected => (Owner as MdiChildWindow)?.DataContext is TerminalDocumentViewModel vm && vm.IsActive;
    public IRawElementProviderSimple? SelectionContainer => null;
    public void AddToSelection() => Select();
    public void RemoveFromSelection()
    {
        if (Owner is MdiChildWindow window && window.DataContext is TerminalDocumentViewModel vm)
        {
            window.Dispatcher.Invoke(() => vm.IsActive = false);
        }
    }
    public void Select()
    {
        if (Owner is MdiChildWindow window && window.DataContext is TerminalDocumentViewModel vm)
        {
            window.Dispatcher.Invoke(() => vm.IsActive = true);
        }
    }

    // --- ITransformProvider ---
    public bool CanMove => true;
    public bool CanResize => true;
    public bool CanRotate => false;

    public void Move(double x, double y)
    {
        if (Owner is not MdiChildWindow window) return;

        window.Dispatcher.Invoke(() =>
        {
            if (window.DataContext is TerminalDocumentViewModel vm)
            {
                var canvas = window.FindParentCanvas();
                if (canvas != null)
                {
                    Point canvasPt = canvas.PointFromScreen(new Point(x, y));
                    double maxLeft = canvas.ActualWidth > 60 ? canvas.ActualWidth - 60 : 3000;
                    double maxTop = canvas.ActualHeight > 40 ? canvas.ActualHeight - 40 : 2000;

                    vm.Left = Math.Max(0, Math.Min(canvasPt.X, maxLeft));
                    vm.Top = Math.Max(0, Math.Min(canvasPt.Y, maxTop));
                }
            }
        });
    }

    public void Resize(double width, double height)
    {
        if (Owner is not MdiChildWindow window) return;

        window.Dispatcher.Invoke(() =>
        {
            if (window.DataContext is TerminalDocumentViewModel vm && vm.WindowState == MdiWindowState.Normal)
            {
                vm.Width = Math.Max(320, width);
                vm.Height = Math.Max(220, height);
            }
        });
    }

    public void Rotate(double degrees) => throw new InvalidOperationException("Rotation is not supported for MDI window.");

    // --- IWindowProvider ---
    public bool IsModal => false;
    public bool IsTopmost => (Owner as MdiChildWindow)?.DataContext is TerminalDocumentViewModel vm && vm.IsActive;
    public bool Maximizable => true;
    public bool Minimizable => true;
    public WindowInteractionState InteractionState => WindowInteractionState.ReadyForUserInteraction;

    public WindowVisualState VisualState
    {
        get
        {
            if (Owner is MdiChildWindow window && window.DataContext is TerminalDocumentViewModel vm)
            {
                return vm.WindowState switch
                {
                    MdiWindowState.Maximized => WindowVisualState.Maximized,
                    MdiWindowState.Minimized => WindowVisualState.Minimized,
                    _ => WindowVisualState.Normal
                };
            }
            return WindowVisualState.Normal;
        }
    }

    public void Close()
    {
        if (Owner is MdiChildWindow window && window.DataContext is TerminalDocumentViewModel vm)
        {
            window.Dispatcher.Invoke(() => vm.CloseCommand.Execute(null));
        }
    }

    public void SetVisualState(WindowVisualState state)
    {
        if (Owner is not MdiChildWindow window) return;

        window.Dispatcher.Invoke(() =>
        {
            if (window.DataContext is not TerminalDocumentViewModel vm) return;

            switch (state)
            {
                case WindowVisualState.Maximized:
                    vm.Maximize();
                    break;
                case WindowVisualState.Minimized:
                    vm.Minimize();
                    break;
                case WindowVisualState.Normal:
                    vm.Restore();
                    break;
            }
        });
    }

    public bool WaitForInputIdle(int milliseconds) => true;
}
