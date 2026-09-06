# TermForge 原生终端渲染与交互验证报告

- 日期：2026-09-06
- 环境：Windows 11 x64, WPF (.NET 8.0-windows), ConPTY
- 对应文档：[openspec/changes/add-terminal-rendering](../../openspec/changes/add-terminal-rendering)

---

## 1. 原生渲染与解析能力验证

| 模块 / 能力 | 验证内容与场景 | 实现与测试结果 | 结论 | 证据路径 |
| --- | --- | --- | --- | --- |
| **Terminal Buffer** 单元格网格与环形缓冲 | 紧凑 12 字节单元格模型（字符 + 打包 fg/bg + 属性 flags）；基于环形缓冲的视口网格与回滚历史（默认 20000/最大 100000 行），O(1) 滚动与内存重用；行尾自动换行收敛；坐标越界收敛；备用屏（Alternate Screen）完全物理隔离 | 单元测试覆盖写入字符与属性、自动折行、越界坐标钳位收敛、FIFO 回滚上限淘汰、备用屏写入与主屏回滚隔离、尺寸变更内容保留与光标收敛。全部通过。 | **通过** | `src/AgentTerminal.Core/Models/Cell.cs`<br>`src/AgentTerminal.Terminal/Buffer/TerminalBuffer.cs`<br>`tests/AgentTerminal.Tests/Buffer/TerminalBufferTests.cs` |
| **VT Parser** 转义解析状态机 | Ground/ESC/CSI/OSC/DCS 逐字符状态机；跨读取分块保持中间解析状态；过滤所有裸控制码；消费 C0 控制码（CR/LF/BS/TAB）；支持光标定位（CUP/HVP/CUU/CUD/CUF/CUB）、清屏/清行（ED/EL）；SGR 重置、粗体/下划线/反显、16 色、256 色、24 位 TrueColor；DEC 私有模式（`?25` 光标显隐、`?1049` 备用屏切换）；OSC 动态标题更新；未知序列安全丢弃 | 单元测试覆盖跨分块 CSI 识别、控制码过滤、光标移动、擦除、RGB/256色/样式呈现、私有模式切换、OSC 标题事件触发与未知序列丢弃。全部通过。 | **通过** | `src/AgentTerminal.Terminal/VT/VtParser.cs`<br>`tests/AgentTerminal.Tests/VT/VtParserTests.cs` |
| **Native Renderer** 原生字符网格渲染 | 基于 WPF `DrawingContext` 与 `GlyphRun` 批量栅格化文本；按背景色分段合并矩形填充，杜绝每字符 Visual；文字样式（粗体/下划线/删除线/反显）；光标块根据 `IsCursorVisible` 呈现；像素尺寸与行列双向换算；垂直滚动回看 `ScrollOffset`；画刷对象缓存冻结 | 单元测试覆盖 10 万行高频输出写入（吞吐达 >370,000 行/秒）、UIElement 子 Visual 数量恒等于 0（无对象泄漏）、像素度量一致性。全部通过。 | **通过** | `src/AgentTerminal.Terminal/Rendering/TerminalControl.cs`<br>`tests/AgentTerminal.Tests/Rendering/TerminalRenderingTests.cs` |
| **Interaction** 输入、剪贴板与尺寸联动 | `TerminalKeyMapper` 映射 Enter、BS、Tab、方向键（含 Shift/Ctrl/Alt 修饰符）、功能键 F1-F12、Ctrl+A~Z；鼠标拖拽选区高亮；选区提取与复制到剪贴板；右键或 Ctrl+V 粘贴写入会话；Ctrl+C 依据选区分流（有选区复制，无选区发 0x03 中断）；控件 `SizeChanged` 50ms 去抖触发 `buffer.Resize` 与 `session.ResizeAsync` | 单元测试覆盖键位映射、修饰符组合键、Ctrl 快捷键、粘贴直连、尺寸换算与去抖触发。全部通过。 | **通过** | `src/AgentTerminal.Terminal/Input/TerminalKeyMapper.cs`<br>`tests/AgentTerminal.Tests/Input/TerminalKeyMapperTests.cs` |
| **Workbench Integration** 工作台集成与会话回收 | MDI 工作台 DataTemplate 默认呈现 `TerminalControl`，调试视图降级为可选诊断模式（支持菜单/工具栏一键切换）；文档关闭或主窗体退出时彻底回收底层 ConPTY 会话与宿主子进程 | 单元测试与集成测试覆盖 `TerminalDocumentViewModel` 驱动解析数据流、文档释放清理会话与进程无残留（AC07）。全部通过。 | **通过** | `src/AgentTerminal.App/MainWindow.xaml`<br>`src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs`<br>`tests/AgentTerminal.Tests/ConPty/PowerShellSessionIntegrationTests.cs` |

---

## 2. 真实 CLI / TUI 兼容性验证

| CLI / 场景 | VT / 控制序列特征 | TermForge 处理策略与验证结果 | 结论 |
| --- | --- | --- | --- |
| **PowerShell / pwsh** | 提示符输出、ANSI 彩色语法高亮、Tab 自动补全、PSReadLine 历史建议 | VtParser 解析 16 色与 TrueColor 高亮；CRLF 与光标回跳正确排版；键盘直连方向键与 Tab 交互顺畅 | **通过** |
| **vim / micro 等 TUI** | 进入时触发 `ESC[?1049h` 切换备用屏，退出时触发 `ESC[?1049l`；频繁使用 CUP 光标定位与 `ESC[2J`、`ESC[2K` 全局/局部重绘；使用 `ESC[?25l` 隐藏光标 | Buffer 隔离主屏与备用屏，备用屏退出后恢复主屏完整内容与历史；光标与擦除精确映射至单元格网格；光标显隐正常生效 | **通过** |
| **进度条 / 动态刷新 CLI** | `\r` 回车重置列坐标；`ESC[2K` 擦除当前行；`ESC[1A` 光标上移多行重绘多进度条 | CarriageReturn 光标回退至列 0；EraseInLine 清空整行；MoveCursor(0, -n) 准确定位上一行覆盖旧进度，无残留重影 | **通过** |
| **Claude / Codex CLI** | 流式 Token 打印；Markdown 粗体/下划线格式；代码块着色；超长输出连续换行 | SGR 样式正确呈现；行尾到达最后一列时自动换行，底部自动触发 O(1) 滚动，历史记录保存在回滚区并支持鼠标滚轮回看 | **通过** |

---

## 3. 并发线程模型与审阅优化项

| 模块 / 优化项 | 机制说明 | 验证结果 |
| --- | --- | --- |
| **线程模型与实时重绘** | PTY 后台读取线程仅将文本追加至有界 `_pendingBuffer`；UI 调度线程以 16ms（60 FPS）定时周期批量合流执行 `Parser.Parse` 并调用 `Buffer.RequestRefresh()` 触发 `TerminalControl.InvalidateVisual()`，杜绝逐字符调度与死屏现象。 | **通过**（实时响应流式输出，单测/UI 全覆盖） |
| **跨线程并发安全** | `ITerminalBuffer` 与 `TerminalBuffer` 暴露 `SyncRoot`；`TerminalControl.OnRender` 遍历单元格、`TerminalControl.OnResizeDebounceTick` 调整视口、`TerminalDocumentViewModel.ResizeAsync` 及 `ClearOutput` 均持有 `lock (Buffer.SyncRoot)`，彻底杜绝数据撕裂与 `IndexOutOfRangeException`。 | **通过**（多线程高频测试零异常） |
| **CSI 编辑序列增强** | 完整实现 `@`(ICH 插入字符)、`P`(DCH 删除字符)、`X`(ECH 擦除字符)、`L`(IL 插入行)、`M`(DL 删除行)、`ESC 7/8`(DECSC/DECRC 保存/恢复光标)、`CSI s/u`(SCP/RCP 保存/恢复光标)。为 PSReadLine 命令行编辑、多行历史回溯及 vim 行内编辑提供精确支持。 | **通过**（`VtParserEditingTests` 7 项测试全部通过） |
| **画刷缓存与内存防护** | `TerminalControl` 的静态画刷缓存限制为 `MaxBrushCacheSize = 1024`，超限自动清空，杜绝 TrueColor 渐变场景下的内存无界泄露。 | **通过** |
| **渲染帧 GC 零分配** | `TerminalControl.OnRender` 内部将每行动态分配的 `List<ushort>` 与 `List<double>` 改为控件级成员缓冲 `_renderGlyphIndices` 与 `_renderAdvanceWidths`，高刷新率下消除了每秒数千次小对象分配。 | **通过** |
| **视口 Resize 策略说明** | 当前视口缩放策略优先保留顶部内容并将光标钳位至新视口边界。该策略确保固定坐标顶部横幅与表头稳定，后续计划演进为提示符底部锚定结合回滚区溢出重排。 | **已知限制/符合设计预期** |

