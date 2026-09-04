namespace AgentTerminal.Terminal.VT;

/// <summary>
/// ANSI / VT 转义序列解析器骨架（Phase 3 重点实现）
/// </summary>
public class VtParser
{
    public void Parse(ReadOnlySpan<char> input)
    {
        // TODO: Phase 3 - VT State Machine (ESC, CSI, OSC, SGR)
    }
}
