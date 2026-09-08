using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgentTerminal.App.ViewModels;
using AgentTerminal.Core.Models;
using AgentTerminal.Docking.ViewModels;

namespace AgentTerminal.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (s, e) => SystemCommands.CloseWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (s, e) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (s, e) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (s, e) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, OnShowSystemMenu));
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        AgentTerminal.App.Infrastructure.WindowBackdropHelper.ApplyBackdrop(this);
    }

    private void OnShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
    {
        double captionHeight = System.Windows.Shell.WindowChrome.GetWindowChrome(this)?.CaptionHeight ?? 30.0;
        var point = PointToScreen(new Point(0, captionHeight));
        SystemCommands.ShowSystemMenu(this, point);
    }



    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Tab：在各个 MDI 子文档间循环切换
        if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ViewModel.SwitchToNextDocument();
            e.Handled = true;
            return;
        }

        // Ctrl+F4：关闭当前活动文档
        if (e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (ViewModel.CloseTerminalCommand.CanExecute(null))
            {
                ViewModel.CloseTerminalCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        // Ctrl+N：新建终端文档
        if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (ViewModel.NewTerminalCommand.CanExecute(null))
            {
                ViewModel.NewTerminalCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }
    }

    private async void OnWindowClosing(object sender, CancelEventArgs e)
    {
        // 窗体关闭前回收所有会话进程
        await ViewModel.CleanupAllSessionsAsync();
    }

    private void OnMdiContainerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ViewModel.ContainerWidth = e.NewSize.Width;
        ViewModel.ContainerHeight = e.NewSize.Height;
    }

    private void OnWindowMenuOpened(object sender, RoutedEventArgs e)
    {
        WindowsListMenuItem.Items.Clear();

        if (ViewModel.Documents.Count == 0)
        {
            WindowsListMenuItem.Items.Add(new MenuItem { Header = "(无打开文档)", IsEnabled = false });
            return;
        }

        for (int i = 0; i < ViewModel.Documents.Count; i++)
        {
            var doc = ViewModel.Documents[i];
            var item = new MenuItem
            {
                Header = $"{i + 1} {doc.Title}",
                IsCheckable = true,
                IsChecked = (doc == ViewModel.ActiveDocument)
            };

            item.Click += (s, args) =>
            {
                ViewModel.ActivateDocument(doc);
                if (doc.WindowState == MdiWindowState.Minimized)
                {
                    doc.Restore();
                }
            };

            WindowsListMenuItem.Items.Add(item);
        }
    }

    private void OnNavigationItemClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TerminalDocumentViewModel doc)
        {
            ViewModel.ActivateDocument(doc);
            if (doc.WindowState == MdiWindowState.Minimized)
            {
                doc.Restore();
            }
            e.Handled = true;
        }
    }

    private void OnProfileNavigationItemClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ShellProfile profile)
        {
            ViewModel.NewTerminal(profile);
            e.Handled = true;
        }
    }

    private void OnMenuExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnMenuAboutClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "TermForge - Windows Native Agent Terminal\n" +
            "Windows 7 Aero MMC 风格 MDI 工作台与 PowerShell 会话\n" +
            "Version 0.3 | 2026-09-05",
            "关于 TermForge",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}