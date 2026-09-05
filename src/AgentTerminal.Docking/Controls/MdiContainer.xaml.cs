using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AgentTerminal.Core.Models;
using AgentTerminal.Docking.ViewModels;

namespace AgentTerminal.Docking.Controls;

/// <summary>
/// MdiContainer 容器逻辑：负责子窗口布局调度、最大化自适应、最小化托盘及 Z-Index 置顶管理
/// </summary>
public partial class MdiContainer : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(MdiContainer),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public MdiContainer()
    {
        InitializeComponent();
        MinimizedTray.Visibility = Visibility.Collapsed;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MdiContainer container)
        {
            container.BindItemsSource(e.OldValue as IEnumerable, e.NewValue as IEnumerable);
        }
    }

    private void BindItemsSource(IEnumerable? oldSource, IEnumerable? newSource)
    {
        if (oldSource is INotifyCollectionChanged oldNcc)
        {
            oldNcc.CollectionChanged -= OnCollectionChanged;
        }

        if (oldSource != null)
        {
            foreach (var item in oldSource)
            {
                if (item is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged -= OnItemPropertyChanged;
                }
            }
        }

        MdiItemsControl.ItemsSource = newSource;

        if (newSource is INotifyCollectionChanged newNcc)
        {
            newNcc.CollectionChanged += OnCollectionChanged;
        }

        if (newSource != null)
        {
            foreach (var item in newSource)
            {
                if (item is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += OnItemPropertyChanged;
                }
            }
        }

        RefreshMinimizedTray();
        UpdateMaximizedWindows();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += OnItemPropertyChanged;
                }
            }
        }

        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged -= OnItemPropertyChanged;
                }
            }
        }

        RefreshMinimizedTray();
        UpdateMaximizedWindows();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TerminalDocumentViewModel.WindowState))
        {
            RefreshMinimizedTray();
            UpdateMaximizedWindows();
        }
        else if (e.PropertyName == nameof(TerminalDocumentViewModel.IsActive))
        {
            if (sender is TerminalDocumentViewModel activeVm && activeVm.IsActive)
            {
                BringToFront(activeVm);
            }
        }
    }

    private void BringToFront(TerminalDocumentViewModel targetVm)
    {
        if (ItemsSource == null) return;

        int highestZ = 0;
        foreach (var item in ItemsSource)
        {
            if (item is TerminalDocumentViewModel vm)
            {
                if (vm != targetVm)
                {
                    vm.IsActive = false;
                }
                if (vm.ZIndex > highestZ)
                {
                    highestZ = vm.ZIndex;
                }
            }
        }

        targetVm.ZIndex = highestZ + 1;
    }

    private void UpdateMaximizedWindows()
    {
        if (ItemsSource == null || ActualWidth <= 0 || ActualHeight <= 0) return;

        double trayHeight = MinimizedTray.Visibility == Visibility.Visible ? MinimizedTray.ActualHeight : 0;
        double availableHeight = Math.Max(0, ActualHeight - trayHeight);

        foreach (var item in ItemsSource)
        {
            if (item is TerminalDocumentViewModel vm && vm.WindowState == MdiWindowState.Maximized)
            {
                vm.Left = 0;
                vm.Top = 0;
                vm.Width = ActualWidth;
                vm.Height = availableHeight;
            }
        }
    }

    private void RefreshMinimizedTray()
    {
        if (ItemsSource == null)
        {
            MinimizedTray.Visibility = Visibility.Collapsed;
            return;
        }

        var list = new List<TerminalDocumentViewModel>();
        foreach (var item in ItemsSource)
        {
            if (item is TerminalDocumentViewModel vm && vm.WindowState == MdiWindowState.Minimized)
            {
                list.Add(vm);
            }
        }

        MinimizedItemsControl.ItemsSource = list;
        MinimizedTray.Visibility = list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnContainerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateMaximizedWindows();
    }
}
