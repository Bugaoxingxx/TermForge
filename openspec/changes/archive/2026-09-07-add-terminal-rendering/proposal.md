## Why

当前终端处于 Phase 1「原始输出调试模式」：`ConPtyTerminalSession` 把 ConPTY 原始字节直接经 `OutputReceived` 塞进一个只读 `TextBox`（`TerminalDebugView`）显示。ConPTY 必然输出 VT/ANSI 控制序列（如 `ESC[H`、`ESC[2J`、`ESC[?25l`、`ESC[0m`、`ESC]0;title BEL`），而 `VtParser`、`TerminalBuffer`、`TerminalControl` 目前都是空骨架，导致：

- 转义序列被当作文本原样显示，出现 `[1900`、`[?25l`、`[0m` 等「乱码」。
- 光标定位 / 清屏 / 行内重绘 / 滚动全部失效，任何 TUI 程序（vim、进度条、`cls`）都会错乱。
- SGR 颜色全部丢失。
- `TextWrapping=Wrap` 折断超宽行，破坏字符网格对齐。
- 交互是「输入框 + 发送按钮」，不是直连键盘的真实终端。

这使终端「几乎不可用」。本变更补全开发计划中的 Phase 2/3/4/5，交付一个真正可用的原生终端。

## What Changes

- 实现真正的**单元格网格缓冲区**（Cell = 字符 + 前景/背景/属性），含视口 + 回滚历史（scrollback）、光标、主/备用屏（Alternate Screen）。**BREAKING**：`ITerminalBuffer` / `TerminalBuffer` 由「仅记录行数」升级为完整网格模型。
- 实现 **VT/ANSI 解析状态机**（Ground/ESC/CSI/OSC/DCS），支持 C0 控制码、光标移动、清屏/清行、滚动区、SGR（16 色 / 256 色 / TrueColor）、Alternate Screen、常见私有模式（如 `?25` 光标显隐、`?1049` 备用屏）。
- 实现**原生渲染控件** `TerminalControl`：基于 `DrawingContext` + `GlyphRun` 绘制字符网格，禁止一字符一 `TextBlock`；绘制背景、文本、颜色、光标、选区；保持 ~16ms 批量刷新、不随内容量增长创建海量 Visual。
- 实现**交互**：键盘经 `TerminalKeyMapper` 直连会话（方向键、功能键、Ctrl 组合），鼠标选区 + 复制/粘贴，控件尺寸变化映射到 `ResizePseudoConsole`。
- **集成（MDI-T06）**：用 `TerminalControl` 替换 `TerminalDebugView` 成为终端文档的主视图；保留调试视图为可选诊断模式。

## Capabilities

### New Capabilities
- `terminal-buffer`: 终端屏幕状态模型——单元格网格、光标、属性、滚动区、回滚历史与主/备用屏切换，独立于 WPF 可测试。
- `vt-parser`: VT/ANSI 转义序列解析状态机，将 PTY 字节流解释为对 `terminal-buffer` 的一系列操作。
- `terminal-renderer`: 基于 WPF `DrawingContext`/`GlyphRun` 的高性能字符网格渲染控件及其刷新调度。
- `terminal-interaction`: 键盘映射、鼠标选区、复制/粘贴、焦点与尺寸联动，以及替换调试视图接入 MDI 工作台。

### Modified Capabilities
<!-- openspec/specs 目前为空，无既有 spec-level 需求变更。ITerminalBuffer 的接口升级作为 terminal-buffer 新能力的一部分记录于其 spec。 -->

## Impact

- 代码：`src/AgentTerminal.Terminal/`（`Buffer/TerminalBuffer.cs`、`VT/VtParser.cs`、`Rendering/TerminalControl.cs`、`Input/TerminalKeyMapper.cs`）、`src/AgentTerminal.Core/Abstractions/ITerminalBuffer.cs`；`src/AgentTerminal.App`（`MainWindow.xaml` 的 DataTemplate、`Views/`）；`src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs`（输出改为喂给 buffer/parser 而非字符串拼接）。
- 约束：遵循 AGENTS.md 的 NativeAOT 友好（禁反射）、异步 I/O + 批量刷新防雪崩、Scrollback 上限。
- 测试：新增 `tests/AgentTerminal.Tests` 下 VT 解析器与 Buffer 单元测试；渲染性能与高频输出压测。
- 依赖：不新增第三方依赖，纯 WPF 原生实现。
