# **Windows 原生 Agent Terminal 工作台 详细开发计划**

*技术路线：.NET 8/9 + WPF + Docking + ConPTY + Native Terminal Renderer*

## 1. 项目概述

本项目目标是开发一款 Windows 原生的 .NET 运维/开发工作台，整体交互体验参考 macOS Terminal、Linux Terminal、Windows Terminal，同时提供类似 MMC、Visual Studio 的自由 Docking 能力。核心使用场景为 PowerShell、WSL 以及 Codex、Claude Code、Gemini CLI 等 Agent CLI。

核心原则：Shell/Agent 不作为 GUI 窗口嵌入，不使用 PuTTY + SetParent，不依赖 WebView2/xterm.js；通过 Windows ConPTY 将无窗口 Shell 作为 PTY 会话运行，由原生 TerminalControl 完成终端渲染。

## 2. 产品目标

* 支持 PowerShell 7、Windows PowerShell、CMD、WSL。
* 支持 Agent CLI 作为一等公民，包括 Codex、Claude Code、Gemini CLI 及自定义 Agent。
* 支持多个 Terminal Tab 同时运行。
* 支持 Tab 拖动、左右/上下拆分、浮动、停靠、关闭和 Layout 保存恢复。
* 提供接近 macOS/Linux Terminal 的键盘、复制粘贴、ANSI/VT、Unicode、滚动和 Resize 体验。
* Terminal 长时间运行稳定，并能承受 Agent CLI 的高频输出。
* 通过 Job Object 管理 Shell 及其子进程，避免关闭 Tab 后残留进程。
* 后续可扩展 SSH、服务器管理、Agent 管理、Workspace、任务和审计等能力。

## 3. 总体技术架构

```text
WPF MainWindow
 ├── DockingManager
 │    ├── Explorer
 │    ├── TerminalDocument × N
 │    ├── Properties
 │    └── Output / Logs
 │
 └── TerminalDocument
      └── TerminalSession
           ├── ConPTY
           ├── Process / Job Object
           ├── Async Input / Output
           ├── VT Parser
           ├── Terminal Buffer
           └── Native Renderer
                │
                ├── pwsh.exe
                ├── powershell.exe
                ├── wsl.exe
                └── cmd.exe
```

## 4. 技术选型

| 模块 | 推荐技术 | 说明 |
| :--- | :--- | :--- |
| 运行时 | .NET 8 LTS / .NET 9 | 优先 .NET 8 LTS |
| 桌面 UI | WPF | Windows 原生桌面体验 |
| 架构 | MVVM | UI 与 Terminal 核心解耦 |
| Docking | AvalonDock（MVP） | 后续可替换商业 Docking 框架 |
| PTY | Windows ConPTY | Windows 原生伪终端 |
| Shell | pwsh / powershell / cmd / wsl | 统一 TerminalSession 抽象 |
| Terminal Renderer | WPF Native | 避免 WebView2 多进程和内存开销 |
| Win32 API | CsWin32 / PInvoke | CreatePseudoConsole 等 |
| 配置 | JSON | Shell / Agent / Workspace |
| 日志 | Serilog | 诊断与故障排查 |
| 测试 | xUnit | 单元测试和集成测试 |
| 打包 | MSIX / 自包含 EXE | 根据企业发布要求选择 |

## 5. 项目目录

```text
AgentTerminal/
├── src/
│   ├── AgentTerminal.App/
│   ├── AgentTerminal.Core/
│   ├── AgentTerminal.Terminal/
│   │   ├── ConPty/
│   │   ├── VT/
│   │   ├── Buffer/
│   │   ├── Input/
│   │   └── Rendering/
│   ├── AgentTerminal.Docking/
│   ├── AgentTerminal.Infrastructure/
│   └── AgentTerminal.Tests/
├── tests/
├── docs/
└── build/
```

## 6. 核心对象设计

TerminalSession：负责一个 Shell/Agent 会话的完整生命周期。

```text
ITerminalSession
 ├── StartAsync()
 ├── WriteAsync(data)
 ├── ResizeAsync(columns, rows)
 ├── StopAsync()
 ├── OutputReceived
 └── ProcessExited
```

TerminalDocumentViewModel 持有 TerminalSession；Docking 只管理 Document/Pane，不负责 Shell 生命周期。

## 7. 分阶段开发计划

### Phase 1：ConPTY POC（1～2 天）

* 实现 CreatePseudoConsole、Pipe、CreateProcess。
* 启动 pwsh.exe -NoLogo。
* 完成双向读写。
* 验证中文、方向键、Backspace、Tab、Ctrl+C、Ctrl+D。
* 完成高频输出压力测试。
**验收：**可以在自研窗口中稳定交互 PowerShell。

### Phase 2：Terminal Buffer（3～5 天）

* 建立 Terminal State、Cell、Cursor、Selection、Scrollback。
* 将 PTY 输出与 UI 解耦。
* 支持固定行列终端模型。
* 设置默认 Scrollback 20,000 行，最大 100,000 行。
**验收：**输出数据可以独立于 WPF Renderer 保存和测试。

### Phase 3：VT Parser（5～10 天）

* 实现 ESC/CSI/OSC/C0。
* 支持光标移动、清屏、清行、滚动。
* 支持 SGR、16 色、256 色、TrueColor。
* 支持 Alternate Screen。
* 建立 VT Parser 单元测试。
**验收：**常见 Agent CLI 的 ANSI/VT 输出正确。

### Phase 4：Native Renderer（5～10 天）

* 实现 WPF CustomControl。
* 优先使用 DrawingVisual/DrawingContext/GlyphRun。
* 禁止一个字符一个 WPF TextBlock 的方案。
* 实现背景、文本、颜色、光标、Selection。
* 验证大输出时 UI 不明显卡顿。
**验收：**形成可实际使用的原生 TerminalControl。

### Phase 5：Input / Clipboard / Resize（3～5 天）

* 键盘映射到 VT 输入序列。
* 实现 Ctrl+C、Ctrl+D、Ctrl+Z、Ctrl+L 等。
* 实现鼠标选区、复制、粘贴。
* 无选区 Ctrl+C 发送中断；有选区 Ctrl+C 执行复制。
* 窗口尺寸变化调用 ResizePseudoConsole。
**验收：**操作体验接近现代 Terminal。

### Phase 6：Shell Profile（2～3 天）

* 统一 PowerShell、Windows PowerShell、CMD、WSL。
* 支持默认工作目录和环境变量。
* 支持 WSL Distribution。
* Shell Profile JSON 化。
**验收：**New Terminal 可以选择不同 Shell。

### Phase 7：Docking（3～5 天）

* 接入 AvalonDock。
* Terminal 作为 LayoutDocument。
* 实现 Tab、Split、Float、Dock。
* 保存和恢复 Layout。
* Docking 操作不得重启 TerminalSession。
**验收：**形成 MMC/Visual Studio 风格工作区。

### Phase 8：Multi-Terminal / 生命周期（3～5 天）

* 支持多个 TerminalSession。
* 加入 Job Object。
* 实现 Created/Starting/Running/Stopping/Exited/Failed 状态。
* 关闭 Tab 时正确释放 Pipe、ConPTY、Process、Job Handle。
* 测试 Shell 异常退出和主程序异常退出。
**验收：**20～50 个 TerminalSession 的基础压力测试通过。

### Phase 9：Agent Profile（2～4 天）

* 定义 Agent Profile。
* 支持 command、shell、workingDirectory、environment。
* 一键启动 Codex、Claude Code、Gemini CLI 等。
* Tab 标题显示 Agent/项目/状态。
**验收：**右键项目即可打开指定 Agent Terminal。

### Phase 10：Workspace（3～5 天）

* 保存 Terminal、项目目录、Agent Profile 和 Docking Layout。
* 支持 Workspace 新建、打开、保存。
* 支持程序重启后的 Layout 恢复。
**验收：**可以恢复完整 Agent 工作环境。

## 8. Agent 场景重点设计

Agent CLI 的关键不是图形化 Agent UI，而是稳定提供一个真正的 PTY 环境。Agent 应运行在普通 TerminalSession 中。

例如：

```text
Workspace
 └── Project A
      ├── Codex → WSL → codex
      ├── Claude → WSL → claude
      └── PowerShell → pwsh
```

用户可以把三个 Terminal 任意拆分、拖动或浮动，而 Agent 进程本身不感知 Docking。

## 9. 性能与并发设计

* 禁止每收到一个字符就 Dispatcher.Invoke。
* PTY Reader 使用异步读取。
* 输出进入线程安全 Buffer 后按批次刷新 UI。
* 建议以约 16ms 的刷新周期作为初始策略，并根据压力测试调整。
* Renderer 不创建海量 WPF Visual/Control。
* Scrollback 必须有上限。
* Terminal 关闭后必须验证 Process、Pipe、ConPTY、Job 等资源均释放。
* 压力目标：单 Terminal 高输出场景稳定；10～20 个 Terminal 同时运行不应因 Terminal 层导致 UI 卡顿；进一步目标是 50 个 Session 基础压力测试。

## 10. 测试矩阵

| 测试项 | 要求 |
| :--- | :--- |
| PowerShell / WSL / CMD | 全部通过 |
| 中文 / Emoji / Unicode | 显示及输入正常 |
| ANSI / 256 色 / TrueColor | 显示正常 |
| Ctrl+C / Ctrl+D / Ctrl+Z | 行为正确 |
| Copy / Paste / Selection | 行为正确 |
| Resize | Shell 能正确感知 |
| 高频输出 | UI 不被 Dispatcher 队列拖死 |
| Tab Drag / Split / Float | 不导致 Session 重启 |
| Shell 崩溃 | 状态正确、资源释放 |
| Agent 长时间运行 | Session 稳定 |
| 关闭 Tab | 无残留进程 |
| 关闭主程序 | Job Object 清理子进程 |

## 11. MVP 范围

第一版严格控制范围，只包含：

* .NET 8 + WPF
* ConPTY
* Native TerminalControl
* PowerShell
* WSL
* Terminal Tab
* Docking / Split / Float
* Copy / Paste
* Resize
* 基础 Shell Profile
* 基础 Agent Profile
* Job Object
* 基础日志
第一版暂不实现 SSH 协议栈、复杂服务器资产管理、Agent 编排、权限审计等高级功能。

## 12. 后续版本路线

* SSH：优先复用 Windows ssh.exe + ConPTY，不改变 Terminal UI 架构。
* 服务器资产管理：服务器树、凭据引用、连接 Profile。
* Agent Manager：统一管理多个 Agent Session。
* 任务中心：查看 Agent/脚本执行状态。
* Workspace：项目级完整工作环境。
* 日志/审计：根据企业需求增加。
* 插件系统：允许扩展 Shell、Agent、工具栏和面板。

## 13. 主要技术风险

| 风险 | 等级 | 应对 |
| :--- | :--- | :--- |
| Native Terminal Renderer | 最高 | VT/Unicode/光标/Selection/高性能渲染复杂。第一阶段就建立原型。 |
| UI Backpressure | 高 | Agent 高输出可能拖垮 Dispatcher。必须采用异步读取+批量刷新。 |
| Session 生命周期 | 高 | Shell 会继续派生子进程。必须使用 Job Object 和统一生命周期管理。 |
| DPI/字体 | 中 | Terminal 字符栅格和 WPF DPI 变化需要专门测试。 |
| VT 兼容性 | 中 | 不同 Agent CLI 使用的 VT 特性可能不同。通过真实 CLI 建立兼容性测试集。 |

## 14. 建议工期

* ConPTY POC：3～5 个工作日。
* 可用 Terminal MVP：约 2～3 周。
* Docking + Multi-Terminal：约 1 周。
* Agent Profile + Workspace：约 1 周。
* 综合 MVP：约 3～5 周（单人、熟悉 C#/WPF/Win32）。
* 产品化版本：约 6～10 周，取决于测试、安装、更新、兼容性和企业功能范围。

## 15. 最终架构结论

本项目不采用 PuTTY + SetParent，也不采用 WebView2 + xterm.js。推荐以 Windows ConPTY 为终端后端，以 Native TerminalControl 为终端前端，以 WPF + Docking Framework 构建桌面工作台。Shell 和 Agent 都被抽象为 TerminalSession，从而实现 macOS/Linux Terminal 风格的原生终端体验，以及 MMC/Visual Studio 风格的自由拖拽、拆分和浮动。

最终技术路线：WPF + Docking + Native TerminalControl + ConPTY + Shell/Agent Profile。
