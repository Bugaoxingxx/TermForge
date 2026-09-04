using AgentTerminal.Core.Models;

namespace AgentTerminal.Core.Abstractions;

/// <summary>
/// 终端底层会话抽象接口，负责 Shell / Agent 会话的完整生命周期与输入输出交互
/// </summary>
public interface ITerminalSession : IAsyncDisposable
{
    /// <summary>会话唯一标识符</summary>
    string Id { get; }

    /// <summary>会话显示标题</summary>
    string Title { get; }

    /// <summary>当前会话生命周期状态</summary>
    TerminalState State { get; }

    /// <summary>当前视口行列尺寸</summary>
    TerminalDimensions Dimensions { get; }

    /// <summary>终端进程退出代码（若已退出）</summary>
    int? ExitCode { get; }

    /// <summary>接收到终端输出数据事件（包含 ANSI/VT 原始数据流）</summary>
    event EventHandler<string>? OutputReceived;

    /// <summary>接收到原始二进制输出数据事件</summary>
    event EventHandler<byte[]>? BinaryOutputReceived;

    /// <summary>终端进程退出事件</summary>
    event EventHandler<int>? ProcessExited;

    /// <summary>会话状态变更事件</summary>
    event EventHandler<TerminalState>? StateChanged;

    /// <summary>
    /// 异步启动终端进程与 ConPTY 管道
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 向终端管道发送文本指令或击键字符
    /// </summary>
    Task WriteAsync(string data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 向终端管道发送原始二进制数据
    /// </summary>
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 调整终端视口行列大小并通知底层的 ConPTY
    /// </summary>
    Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止终端会话并安全回收所有相关进程与句柄
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
