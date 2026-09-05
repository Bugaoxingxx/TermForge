using FlaUI.Core.AutomationElements;
using AgentTerminal.UITests.Infrastructure;

namespace AgentTerminal.UITests.Pages;

public class TerminalDebugViewPage
{
    private readonly AutomationElement _container;

    public TerminalDebugViewPage(AutomationElement container)
    {
        _container = container;
    }

    public Button? StartButton => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.BtnStart"))?.AsButton();
    public Button? StopButton => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.BtnStop"))?.AsButton();
    public Button? ClearButton => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.BtnClear"))?.AsButton();
    public Button? SendButton => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.BtnSend"))?.AsButton();
    public Button? InterruptButton => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.BtnInterrupt"))?.AsButton();
    public Button? ApplyResizeButton => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.BtnApplyResize"))?.AsButton();

    public TextBox? InputBox => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.InputBox"))?.AsTextBox();
    public TextBox? OutputBox => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.TxtOutput"))?.AsTextBox();
    public TextBox? ColumnsBox => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.TxtColumns"))?.AsTextBox();
    public TextBox? RowsBox => _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.TxtRows"))?.AsTextBox();

    public string GetStateText()
    {
        var elem = _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.TxtState"));
        return elem?.Name ?? elem?.AsLabel()?.Text ?? string.Empty;
    }

    public bool IsRunning()
    {
        var text = GetStateText();
        return text.Contains("运行", StringComparison.OrdinalIgnoreCase) || text.Contains("Running", StringComparison.OrdinalIgnoreCase);
    }

    public string GetExitCodeText()
    {
        var elem = _container.FindFirstDescendant(cf => cf.ByAutomationId("DebugView.TxtExitCode"));
        return elem?.Name ?? elem?.AsLabel()?.Text ?? string.Empty;
    }

    public string GetOutputText()
    {
        return OutputBox?.Text ?? string.Empty;
    }

    public void ClickStart()
    {
        UiaWait.Until(() => StartButton, message: "Start button not found").Invoke();
    }

    public void ClickStop()
    {
        UiaWait.Until(() => StopButton, message: "Stop button not found").Invoke();
    }

    public void ClickClear()
    {
        UiaWait.Until(() => ClearButton, message: "Clear button not found").Invoke();
    }

    public void SendCommand(string command)
    {
        var input = UiaWait.Until(() => InputBox, message: "Command input box not found");
        input.Text = command;
        UiaWait.Until(() => SendButton, message: "Send button not found").Invoke();
    }

    public void ClickInterrupt()
    {
        UiaWait.Until(() => InterruptButton, message: "Interrupt button not found").Invoke();
    }

    public void SetDimensions(int columns, int rows)
    {
        var cols = UiaWait.Until(() => ColumnsBox, message: "Columns box not found");
        var rws = UiaWait.Until(() => RowsBox, message: "Rows box not found");
        cols.Text = columns.ToString();
        rws.Text = rows.ToString();
        UiaWait.Until(() => ApplyResizeButton, message: "Apply resize button not found").Invoke();
    }

    public void WaitForOutput(string expectedSubstring, TimeSpan? timeout = null)
    {
        UiaWait.UntilTrue(() => GetOutputText().Contains(expectedSubstring), 
            timeout: timeout ?? TimeSpan.FromSeconds(8),
            message: $"Timed out waiting for output containing: '{expectedSubstring}'");
    }
}
