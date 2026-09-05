# AgentTerminal (TermForge) 架构设计文档

本文档基于 [Windows 原生 Agent Terminal 工作台 详细开发计划](../Windows原生Agent_Terminal开发计划.md) 编写，指导系统设计与模块演进。

界面目标已明确为 Windows MMC 风格的 MDI 多文档工作台，交互要求以 [统一 PRD](PRD-Phase1-ConPTY.md) 为准。文档区需支持内部子窗口重叠、层叠、平铺和窗口状态控制；AvalonDock 作为现有候选方案，须经原型验证后确定它与 MDI 容器的分工，不能将标签停靠视为完整 MDI 验收。

## 1. 核心架构原则

1. **Native Terminal & ConPTY**：严禁采用 PuTTY + SetParent 或 WebView2 + xterm.js。使用 Windows 原生 ConPTY 伪终端 API，配合原生 WPF 自定义控件（DrawingVisual / GlyphRun）完成字符网格渲染。
2. **Tab / Docking 解耦**：Docking（AvalonDock）仅负责窗口的停靠、拆分、浮动及布局序列化；`TerminalSession` 不受 Docking 拖拽影响，避免重新建立 Shell 会话。
3. **进程生命周期与 Job Object**：每个 Terminal 及其派生子进程必须受 Windows Job Object (`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`) 管辖，杜绝关闭 Tab 或退出程序后的僵尸进程。
4. **异步 I/O 与 UI 防雪崩**：禁止每个字符触发 `Dispatcher.Invoke`。PTY 异步流由线程安全 Buffer 缓冲，采用 ~16ms 批量刷新机制应对 Agent CLI 高频大量输出。

## 2. 模块层次与依赖结构

```text
AgentTerminal.App (WPF Executable)
 ├── AgentTerminal.Docking (WPF Lib: AvalonDock + Layout VM)
 ├── AgentTerminal.Terminal (WPF Lib: ConPTY + VT Parser + Buffer + Native Renderer)
 ├── AgentTerminal.Infrastructure (Win32 P/Invoke + Job Object + Serilog + Profile JSON)
 └── AgentTerminal.Core (Abstractions & Models: ITerminalSession, Profiles, State)
```

## 3. 目录与分工

| 目录/项目 | 说明 |
| :--- | :--- |
| `src/AgentTerminal.Core` | 核心领域模型与抽象接口（`ITerminalSession`, `ITerminalBuffer`, `TerminalDimensions`, `TerminalState`, `ShellProfile`, `AgentProfile`） |
| `src/AgentTerminal.Terminal` | 终端核心组件：`ConPty/`、`VT/`、`Buffer/`、`Input/`、`Rendering/` |
| `src/AgentTerminal.Docking` | 停靠与工作台布局：`Layout/`、`ViewModels/`（基于 `Dirkster.AvalonDock`） |
| `src/AgentTerminal.Infrastructure` | 操作系统基础设施与通用服务：`Win32/`（Job Object 等）、`Logging/`（Serilog）、`Configuration/`（JSON） |
| `src/AgentTerminal.App` | WPF 应用程序宿主与主界面：`App.xaml`、`MainWindow.xaml`、DI 配置 |
| `tests/AgentTerminal.Tests` | xUnit 自动化测试套件：VT 解析器测试、Buffer 测试、Profile 测试 |
| `docs/` | 架构、API 与开发文档 |
| `build/` | 自动化编译与打包脚本 |
