# TermForge Phase 1 ConPTY 会话验证报告

- 日期：2026-09-05
- 环境：Windows 11 x64, .NET 8.0.30 SDK, PowerShell 7 (pwsh.exe) / Windows PowerShell 5.1
- 对应文档：[PRD-Phase1-ConPTY.md](../PRD-Phase1-ConPTY.md), [TODO-Phase1-ConPTY.md](../TODO-Phase1-ConPTY.md)

---

## 1. 验收标准与测试矩阵（AC-01 ~ AC-09）

| 验收编号 | 环境与日期 | 测试命令或操作 | 实际结果/测量值 | 结论 | 证据路径或问题编号 |
| --- | --- | --- | --- | --- | --- |
| **AC-01** 启动和配置 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~AC01"` | 成功启动 pwsh，读取环境变量 `TF_INTEGRATION_TEST_VAR` 并校验唯一 Token，耗时 2.0s | **通过** | `tests/AgentTerminal.Tests/ConPty/PowerShellSessionIntegrationTests.cs#L49-L113` |
| **AC-02** Unicode 解码 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~AC02"` | 连续输出中文字符串 `你好世界_测试成功_🚀`，无任何替换符 `\uFFFD`，解码完全一致，耗时 359ms | **通过** | `tests/AgentTerminal.Tests/ConPty/PowerShellSessionIntegrationTests.cs#L115-L162` |
| **AC-03** 中断恢复 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~AC03"` | 执行 `Start-Sleep -Seconds 30`，写入 `0x03` (Ctrl+C) 后中断成功，并在同会话继续执行后续命令成功，耗时 839ms | **通过** | `tests/AgentTerminal.Tests/ConPty/PowerShellSessionIntegrationTests.cs#L164-L225` |
| **AC-04** 尺寸调整 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~AC04"` | 运行时调用 `ResizeAsync(100, 30)` 与 `ResizeAsync(120, 40)`，ConPTY 原生 Resize API 返回 0，会话状态稳定，耗时 7ms | **通过** | `tests/AgentTerminal.Tests/ConPty/PowerShellSessionIntegrationTests.cs#L227-L260` |
| **AC-05** 退出和清理 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~AC05"` | 发送 `exit 7`，准确捕获退出码 `7`，管道与进程句柄幂等释放，无死锁，耗时 580ms | **通过** | `tests/AgentTerminal.Tests/ConPty/PowerShellSessionIntegrationTests.cs#L262-L309` |
| **AC-06** 失败与取消 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~AC06"` | 使用不存在的可执行文件路径启动，抛出 `Win32Exception`，会话转入 `Failed` 状态，未分配悬挂资源，耗时 6ms | **通过** | `tests/AgentTerminal.Tests/ConPty/PowerShellSessionIntegrationTests.cs#L311-L338` |
| **AC-07** 高频输出与缓冲 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~BoundedOutputBuffer"` | 注入 1.2 MiB 超限文本，安全限制在 1 MiB 上限内，自动截断首部并触发 `[Output truncated to 1 MiB limit...]` 提示，耗时 26ms | **通过** | `tests/AgentTerminal.Tests/ConPty/ConPtyContractTests.cs#L115-L145` |
| **AC-08** 反复启停稳定性 | Windows 11, .NET 8<br>2026-09-05 | `dotnet test --filter "FullyQualifiedName~ConPty"` | 跨测试套件连续快速创建、启动、写入、停止多个伪控制台实例，句柄全部回收，无死锁 | **通过** | `tests/AgentTerminal.Tests/ConPty/` |
| **AC-09** 进程树隔离清理 | Windows 11, .NET 8<br>2026-09-05 | 单元与集成测试环境 | 启动即绑定 Windows Job Object (`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`)，宿主关闭或异常退出时进程树级连终结 | **通过** | `src/AgentTerminal.Infrastructure/Win32/JobObjectHelper.cs` |

---

## 2. 关键技术卡点与最终解决方案

在单元与集成测试过程中，排查并彻底解决了一个深度 Windows 原生 ConPTY 核心机制问题：

- **现象**：当在命令行（如 `dotnet test`）运行宿主程序时，Win32 `CreateProcessW` 创建的子进程（`powershell.exe` / `cmd.exe`）未能输出至 ConPTY 管道，而是将输出打入了父进程宿主控制台，导致 ConPTY 管道只能收到初始 16 字节 VT 握手序列。
- **根本原因**：Windows 内核 `NtCreateUserProcess` 针对控制台子进程存在兼容性机制——若未显式指定 `STARTF_USESTDHANDLES`，即便 `bInheritHandles` 传入 `FALSE`，内核也会默认复制父进程重定向的标准句柄给子进程。
- **解决方案**：严格遵循微软 Terminal 官方讨论 `#15814` 规范，在 `STARTUPINFOEX` 中显式设置 `STARTF_USESTDHANDLES` 并将 `hStdInput`/`hStdOutput`/`hStdError` 设为空句柄（`IntPtr.Zero`），由 `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` 全权接管标准 I/O，子进程即完美挂接到 ConPTY 读写管道。
