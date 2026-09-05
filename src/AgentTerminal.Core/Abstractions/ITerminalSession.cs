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
    /// 异步启动终端进程与 ConPTY 管道。
    /// 仅允许在 <see cref="TerminalState.Created"/> 状态下调用一次；并发或重复调用将抛出 <see cref="InvalidOperationException"/>。
    /// 成功启动进入 <see cref="TerminalState.Running"/>；配置无效或基础设施错误进入 <see cref="TerminalState.Failed"/> 并完成资源清理。
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 向终端输入流发送文本指令。
    /// 必须在 <see cref="TerminalState.Running"/> 状态下调用，否则抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    Task WriteAsync(string data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 向终端输入流发送原始二进制字节（如 Ctrl+C 信号 0x03）。
    /// 必须在 <see cref="TerminalState.Running"/> 状态下调用，否则抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 调整终端视口行列尺寸并同步通知底层 ConPTY。
    /// 仅在合法尺寸且处于 <see cref="TerminalState.Running"/> 状态时生效；非法尺寸将被拒绝并抛出 <see cref="ArgumentOutOfRangeException"/>，原会话保持可用。
    /// </summary>
    Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止终端会话并安全回收相关进程树与所有原生句柄。
    /// 支持幂等重复调用；若启动中被取消或遇到错误，均完成安全回收。
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
