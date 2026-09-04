namespace AgentTerminal.Terminal.ConPty;

/// <summary>
/// Windows ConPTY (PseudoConsole) 相关 Win32 常量定义
/// </summary>
public static class PseudoConsoleConstants
{
    public const uint PSEUDOCONSOLE_INHERIT_CURSOR = 1;
    public const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
}
