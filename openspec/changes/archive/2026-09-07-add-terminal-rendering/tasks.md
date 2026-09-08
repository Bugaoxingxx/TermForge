## 1. Phase 2 — Terminal Buffer（单元格网格）

- [x] 1.1 定义 `Cell` 结构（char + 打包 fg/bg + 属性 flags）与 `TerminalColor`（16/256/TrueColor 表示）
- [x] 1.2 扩展 `ITerminalBuffer`：暴露按行读取单元格、写字符、擦除、滚动、备用屏切换等能力
- [x] 1.3 用环形缓冲重写 `TerminalBuffer`：视口网格 + 回滚历史（默认 20000/最大 100000），O(1) 滚动
- [x] 1.4 实现写字符/自动换行、光标定位收敛、清屏/清行、滚动区
- [x] 1.5 实现主屏/备用屏隔离与 `Resize` 内容保留 + 光标收敛
- [x] 1.6 单元测试：写入、换行滚动、回滚上限淘汰、备用屏隔离、resize 收敛

## 2. Phase 3 — VT Parser（解析状态机）

- [x] 2.1 实现 Ground/ESC/CSI/OSC/DCS 状态机骨架，跨分块保持状态
- [x] 2.2 C0 控制码（CR/LF/BS/TAB/BEL）与可打印字符写入 buffer
- [x] 2.3 CSI 光标（CUP/HVP/CUU/CUD/CUF/CUB）与擦除（ED/EL）
- [x] 2.4 SGR：重置、粗体/下划线/反显、16 色、256 色（38/48;5;n）、TrueColor（38/48;2;r;g;b）
- [x] 2.5 DEC 私有模式：`?25`（光标显隐）、`?1049`（备用屏），OSC 标题吞掉
- [x] 2.6 未识别序列安全丢弃；单元测试覆盖跨分块、无裸控制码、颜色、清屏、备用屏

## 3. Phase 4 — Native Renderer（渲染控件）

- [x] 3.1 `TerminalControl` 度量：加载等宽 `GlyphTypeface`、缓存 advance width / 行高
- [x] 3.2 `OnRender` 遍历可见行，按背景色合并矩形填充，GlyphRun 批量绘制文本
- [x] 3.3 前景/背景/属性（粗体/下划线/反显）呈现，反显互换前景背景
- [x] 3.4 光标块绘制（受 `IsCursorVisible` 控制）
- [x] 3.5 接入 16ms 批量刷新（复用 ViewModel DispatcherTimer），脏行标记 + `InvalidateVisual`
- [x] 3.6 像素尺寸 ↔ 行列换算 API；滚动回看垂直偏移
- [x] 3.7 高频输出与 10 万行压测，确认 Visual 数量不随输出增长

## 4. Phase 5 — Interaction（输入/剪贴板/尺寸）

- [x] 4.1 实现 `TerminalKeyMapper.MapKeyToVtSequence`（可打印/Enter/BS/Tab/方向/功能/Ctrl 组合）
- [x] 4.2 控件键盘事件直连 `session.WriteAsync`，聚焦管理
- [x] 4.3 鼠标拖拽选区 + 复制；粘贴写入会话；Ctrl+C 依选区分流（复制 vs 0x03）
- [x] 4.4 `SizeChanged` 去抖 → `buffer.Resize` + `session.ResizeAsync`

## 5. 集成（MDI-T06）与收尾

- [x] 5.1 数据流改造：`TerminalDocumentViewModel` 持有 `VtParser`+`TerminalBuffer`，输出事件驱动解析而非字符串拼接
- [x] 5.2 `MainWindow.xaml` DataTemplate 改为呈现 `TerminalControl`；调试视图降级为可选诊断模式
- [x] 5.3 关闭文档/主窗口回收会话验证（无残留进程）
- [x] 5.4 真实 CLI 兼容性验证（pwsh、vim、进度条、Claude/Codex CLI），更新 `docs/validation`
- [x] 5.5 更新 TODO（勾选 MDI-T06）、README 进度；`openspec validate --strict` 通过后 `openspec archive`
