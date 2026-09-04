namespace AgentTerminal.Core.Models;

/// <summary>
/// 终端会话生命周期状态
/// </summary>
public enum TerminalState
{
    /// <summary>已创建但未启动</summary>
    Created = 0,

    /// <summary>启动中（创建 ConPTY、绑定 Pipe、创建进程）</summary>
    Starting = 1,

    /// <summary>正常运行中，支持输入输出交互</summary>
    Running = 2,

    /// <summary>正在停止或关闭会话</summary>
    Stopping = 3,

    /// <summary>进程已正常或外部退出</summary>
    Exited = 4,

    /// <summary>启动或运行发生异常失败</summary>
    Failed = 5
}
