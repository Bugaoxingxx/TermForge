using System.Windows.Controls;
using System.Windows.Input;
using AgentTerminal.Docking.ViewModels;

namespace AgentTerminal.App.Views;

/// <summary>
/// TerminalDebugView 单文档调试视图交互逻辑
/// </summary>
public partial class TerminalDebugView : UserControl
{
    public TerminalDebugView()
    {
        InitializeComponent();
    }

    private void OnCommandInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is TerminalDocumentViewModel vm)
        {
            if (vm.SendCommand.CanExecute(null))
            {
                vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void OnOutputTextChanged(object sender, TextChangedEventArgs e)
    {
        OutputTextBox.ScrollToEnd();
    }
}
