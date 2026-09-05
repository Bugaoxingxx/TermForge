# 最近两次提交代码审阅

审阅日期：2026-09-05  
审阅范围：`f54bde0`（feat ConPTY/MDI 工作台）、`0a8e40b`（MDI 标题栏拖拽与边界钳制修复）  
对照文档：[PRD-Phase1-ConPTY.md](../PRD-Phase1-ConPTY.md)（FR-01～FR-09、AC-01～AC-09、MDI-01～MDI-08、第 13 节架构约束）  
审阅方式：Bugbot，相对 `54f3ff74`（HEAD~2）的分支差异

## 结论

**建议：请求修改（Request changes）**

两次提交交付了 Phase 1 会话闭环与 MMC 风格 MDI 外壳的主体实现，但启动失败清理、Job 绑定失败回收、停止路径协调关闭，以及重新启动时的旧会话释放均未满足 PRD。后续阶段的完整终端渲染（字符网格、VT、原位编辑）未按缺陷计。

## 发现

### [P0] 启动失败未关闭 ConPTY

- 位置：`src/AgentTerminal.Terminal/ConPty/ConPtyTerminalSession.cs:259-266`
- 问题：`StartAsync` 在 `catch` 中只调用 `CleanupResourcesAsync`，而 `ClosePseudoConsole` 仅在 `StopAsync` 里执行。ConPTY 创建成功后若后续步骤失败，伪控制台句柄不会被释放。
- 影响：违反 PRD 失败路径完整清理与 FR-07；重复失败启动会泄漏伪控制台句柄。
- 建议：启动失败、取消与 `StopAsync` 共用同一清理顺序（排空输出、关闭管道、关闭 ConPTY、释放 Job/进程句柄），保证任意中间步骤失败都能回收此前资源。

### [P0] Job 绑定失败遗留进程

- 位置：`src/AgentTerminal.Terminal/ConPty/ConPtyTerminalSession.cs:226-230`
- 问题：`CreateProcessW` 成功后若 `AssignProcess` 失败会抛异常，但清理逻辑仅 `CloseHandle` 进程句柄，不会终止已启动的 Shell，且该进程未纳入 Job Object。
- 影响：违反 PRD「创建进程、加入 Job 与开始执行之间不得留下未受管理子进程」及 AC-06；失败后本机残留不受控 pwsh。
- 建议：Job 绑定失败时立即终止该进程并走完整回收；绑定成功前不得视为会话已进入可运行状态。

### [P0] StopAsync 与退出等待竞态

- 位置：`src/AgentTerminal.Terminal/ConPty/ConPtyTerminalSession.cs:326-353`
- 问题：`StopAsync` 在 `Task.Run(WaitForProcessExitAsync)` 仍可能阻塞于 `WaitForSingleObject` 时，就在 `CleanupResourcesAsync` 中关闭 `_hProcess`，且未 `await` 读取或退出后台任务。
- 影响：退出码可能不可靠，`ProcessExited` 与状态转换可能竞态，违反 FR-06/FR-07 的协调关闭约定（输出排空、管道关闭与 ConPTY 关闭）。
- 建议：先发出停止信号，等待读取排空与进程退出（带超时），再关闭句柄；保证 `ProcessExited` 每个已创建进程至多触发一次，退出码仅在确实取得进程退出结果后赋值。

### [P0] 重建会话未释放旧实例

- 位置：`src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs:145-152`
- 问题：在 `Exited/Failed` 后重新启动时，`_sessionFactory` 创建新会话并 `AttachSession`，但旧的 `ITerminalSession` 未调用 `DisposeAsync`/`StopAsync`。
- 影响：重复启动会泄漏 ConPTY、Job 和 Shell 进程，违反 FR-07「结束后可新建会话重新启动」及 T09 完成标准中的「重新启动创建新实例」。
- 建议：切换到新实例前对旧会话执行停止与异步释放，并取消旧事件订阅；失败时仍保证旧资源被回收。

### [P1] StartAsync 忽略取消令牌

- 位置：`src/AgentTerminal.Terminal/ConPty/ConPtyTerminalSession.cs:84-95`
- 问题：`StartAsync(CancellationToken cancellationToken)` 接受取消令牌，但整个启动流程从未检查或链接该令牌。
- 影响：启动中无法被取消，与 PRD「Starting 期间取消也必须回收已分配资源」及 FR-07、AC-06 不一致。
- 建议：在创建 ConPTY、创建进程、绑定 Job 等步骤检查取消；取消后走与失败相同的完整清理路径，状态进入 Exited 而非残留 Starting。

### [P1] 高频输出未批次刷新

- 位置：`src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs:315-347`
- 问题：`AppendOutput` 在每次 `OutputReceived` 时立即 `ToString` 整个缓冲并通过 `Dispatcher.BeginInvoke` 更新 UI，没有 PRD/T10 要求的约 16ms 批次刷新，也没有独立有界待刷新队列。
- 影响：大量输出时 Dispatcher 队列可无限增长，停止操作反馈可能超过 AC-07 的 1 秒门槛；显示与待刷新数据缺少 1 MiB 上限与截断提示。
- 建议：读取与 UI 更新解耦；待刷新队列与显示缓冲均设界（建议各 1 MiB），超限丢弃最旧显示数据并提示截断；按约 16ms 批次刷新。截断仅影响调试显示。

### [P2] Restore All 未还原尺寸

- 位置：`src/AgentTerminal.Docking/Layout/MdiLayoutManager.cs:106-113`
- 问题：`RestoreAll` 仅将 `WindowState` 设为 `Normal`，不会像 `TerminalDocumentViewModel.Restore()` 那样恢复 `_restoreLeft/Top/Width/Height`。
- 影响：曾最大化的文档会保持容器满屏尺寸，与 MDI-03「全部还原」及窗口菜单「全部还原」的预期不符。
- 建议：全部还原时复用各文档已保存的还原矩形，而不是只改 `WindowState`。

### [P2] ViewModel 未同步 ProcessId

- 位置：`src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs:113-131`
- 问题：`AttachSession` 与 `OnSessionStateChanged` 从未从 `ITerminalSession.ProcessId` 赋值到 ViewModel 的 `ProcessId`。
- 影响：调试 UI 和状态栏在 Running 时始终显示 `PID: N/A`，不符合 FR-08/FR-09 对进程 ID 可见性的要求，也不利于对照 AC-05/AC-08 做人工验收。
- 建议：在会话进入 Running 或 `ProcessId` 可用时同步到 ViewModel；退出后保留最后一次进程 ID 或明确显示已退出，避免继续显示 N/A。

## 未计入缺陷

- 字符网格、完整 VT/ANSI 解析、颜色、光标、Alternate Screen。
- 终端原位编辑、方向键、Tab 补全、Backspace、完整快捷键及鼠标选区。
- CMD、Windows PowerShell、WSL 的正式兼容性验收。
- 浮动到主窗口之外、自由 Docking 与标签模式。

上述均属 PRD 明确的后续阶段，两次提交未宣称已完成。

## 审阅备注

本次只写入审阅意见，未修改产品代码。`0a8e40b` 的标题栏拖拽计算与边界钳制修复未发现新的阻断级回归；MDI 相关问题集中在全部还原与会话/进程信息同步。
