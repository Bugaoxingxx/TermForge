## Context

Phase 1 的 `ConPtyTerminalSession` 已稳定提供双向 IO、UTF-8 有状态解码、Job Object 生命周期与尺寸调整。其 `OutputReceived(string)` 事件目前直接被 `TerminalDocumentViewModel.AppendOutput` 拼接进 `OutputText` 并绑定到只读 TextBox。本设计在不动 ConPTY 会话底层的前提下，插入「Buffer + VT Parser + Renderer」渲染管线，并把键盘/鼠标/尺寸交互直连会话。

约束（AGENTS.md）：NativeAOT 友好（禁反射、禁 dynamic）、异步 IO + ~16ms 批量刷新防雪崩、Scrollback 有上限、优先 `DrawingContext`/`GlyphRun`、MVVM 分层。

## Goals / Non-Goals

**Goals:**
- 消灭转义序列「乱码」，正确呈现颜色、光标、清屏、滚动。
- 支持常见 Agent CLI / TUI 的 VT 输出（vim、进度条、备用屏）。
- 高频输出下 UI 不卡死，Visual 数量不随输出量增长。
- 键盘直连、鼠标选区复制粘贴、尺寸联动，达到接近现代终端的交互。

**Non-Goals:**
- 不实现完整 xterm 全部私有序列（只覆盖常见集，未知序列安全吞掉）。
- 不实现连字（ligature）、复杂双向文本（BiDi）、字形整形。
- 不在本变更内做布局保存/恢复（MDI-T07）、SSH、Agent Profile 等后续阶段。
- 不引入第三方终端/渲染库（xterm.js、WebView2 一律排除）。

## Decisions

- **数据流改造**：新增 `BinaryOutputReceived`（已存在）或复用 `OutputReceived(string)` 作为解析器输入。选择：`TerminalDocumentViewModel` 持有 `VtParser` + `TerminalBuffer`，在输出事件里 `parser.Parse(text)` 更新 buffer，然后通知渲染控件在下一刷新周期重绘。VtParser 接口由 `Parse(ReadOnlySpan<char>)` 扩展为携带目标 buffer（`VtParser(TerminalBuffer)` 构造注入，或 `Parse(span, buffer)`）。
- **单元格模型**：`Cell` 用 `struct`（字符 char + fg/bg 打包为 int/uint + 属性 flags byte），行用 `Cell[]`，视口 + 回滚用环形缓冲（ring buffer）以 O(1) 滚动、避免大数组搬移。TrueColor 用 24 位 + 调色板索引标志位区分。
- **VT 状态机**：显式 enum 状态（Ground/Escape/CsiEntry/CsiParam/OscString/DcsPassthrough…），参数用小型 `int` 栈缓冲，避免正则与反射。未识别序列进入对应终止态后丢弃。
- **渲染**：`TerminalControl : Control`，`OnRender` 遍历可见行，用 `GlyphRun`（缓存 `GlyphTypeface` 与 advance width）批量绘制；按背景色分段填充矩形。刷新由 ViewModel 的 16ms `DispatcherTimer`（已有）触发 `InvalidateVisual`（或脏行标记）。滚动回看用垂直偏移量选择起始行。
- **输入**：实现 `TerminalKeyMapper.MapKeyToVtSequence`，控件 `OnTextInput` 处理可打印字符、`OnKeyDown` 处理特殊键；Ctrl+C 依据是否有选区分流复制 / 0x03。
- **尺寸联动**：控件 `SizeChanged` → 按字体度量换算行列 → `buffer.Resize` + `session.ResizeAsync`，去抖动。
- **AOT**：全部纯逻辑与 WPF 原生类型，无反射/无 `dynamic`；System.Text.Json 若涉及配置用源生成器。

## Risks / Trade-offs

- **渲染性能（最高风险）**：GlyphRun 构造与字体度量开销大。缓解：缓存字形与 advance、仅重绘脏行、按背景色合并矩形、压测 10 万行 / 高频输出。
- **VT 兼容性（中）**：不同 CLI 使用序列差异大。缓解：以真实 CLI（pwsh、vim、Claude/Codex CLI）建兼容性测试集，未知序列安全降级。
- **宽字符/Emoji 网格对齐（中）**：CJK 全角占两列、Emoji 复杂。缓解：东亚宽度表判定 2 列，组合字符占位；本轮先保证 BMP + 常见全角正确。
- **接口 BREAKING**：`ITerminalBuffer` 升级会影响既有测试。缓解：同步更新 `tests`，保留 `TotalLines`/`Dimensions` 等既有成员语义。
- **ConPTY 换行歧义**：ConPTY 会重绘整行。缓解：严格按 VT 语义处理，不自作聪明地做软换行猜测。
