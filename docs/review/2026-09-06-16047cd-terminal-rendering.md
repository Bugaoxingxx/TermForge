# 代码审阅：原生终端渲染管线

审阅日期：2026-09-06
审阅范围：`16047cd`（feat: 原生 VT 渲染与交互管线 Phase 2/3/4/5）、`f4dc0dd`（test: AC07 文档关闭进程清理测试）
审阅方式：人工逐文件审阅 + 本地构建 + 单元测试运行
验证结果：`dotnet build` 通过（0 警告 0 错误）；Buffer/VT/Rendering/Input 单元测试 42 项全部通过。
关联规格：[openspec/changes/add-terminal-rendering](../../openspec/changes/add-terminal-rendering)

## 总体结论：💬 Comment（架构与实现质量高，但有 2 个必须修复的阻断问题）

实现质量相当高：Cell/颜色的位打包、环形回滚缓冲、GlyphRun 批量绘制、备用屏隔离、去抖 resize、键位映射，测试覆盖扎实（含 10 万行压测）。但有两个 🔴 阻断级问题会让「原生终端」在真实运行时要么不刷新、要么在高频输出下崩溃，且两者都不易被现有单线程单测覆盖到。

---

## 🔴 Blocking（合并/上线前必须修）

### 1. 原生控件在有新输出时不会重绘 —— 实时终端实际不刷新

`TerminalControl` 仅在以下时机调用 `InvalidateVisual`：Buffer 引用变化、鼠标滚轮/按键、resize 去抖。而 `Buffer` 为构造时一次性绑定、引用不再变化。

`BufferProperty` 的 `FrameworkPropertyMetadataOptions.AffectsRender` 只在 **DP 值（Buffer 引用）** 变化时触发重绘，**不会因为 buffer 内部单元格被 `Parser.Parse` 改写而触发**。ViewModel 持有 `_flushTimer`，但只更新调试用的 `OutputText`，**没有任何路径通知 `TerminalControl` 重绘**（VM 不持有控件引用）。

- 位置：`src/AgentTerminal.Terminal/Rendering/TerminalControl.cs`（`BufferProperty` 注册、`OnBufferChanged`）；`src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs`（`_flushTimer` / `FlushPendingBufferLocked`）
- 后果：原生控件首次渲染后，Shell 后续输出与键盘输入都不会显示，直到鼠标移动或窗口缩放才「蹦」出来；主渲染路径肉眼近似「死屏」。现有单测直接断言 buffer，绕过了重绘触发，因此未暴露此问题。
- 建议：让拥有 parser + buffer + 16ms flush timer 的 VM 在每个刷新周期驱动控件失效。可暴露 `event Action? Invalidated;`（或让控件订阅 buffer 的「脏」通知），在 flush tick（UI 线程）触发 `InvalidateVisual`。该修复可与问题 2 合并解决。

### 2. 缓冲区跨线程数据竞争（高频输出下会崩溃/花屏）

写入方在**后台线程**：`ConPtyTerminalSession.ReadOutputLoopAsync`（`Task.Run`）→ `OutputReceived` → `OnSessionOutputReceived` → `AppendOutput`，其中 `Parser.Parse(text)` 在 `_bufferLock` 内改写 `TerminalBuffer`。

读取方在 **UI 线程**：`TerminalControl.OnRender` 遍历 `Buffer.GetCell / Buffer.Dimensions`，**完全没有加锁**。此外还有两个**未加锁的 UI 线程写入方**动同一 buffer：

- `TerminalControl.OnResizeDebounceTick` 中的 `Buffer.Resize(newCols, newRows)`（未持 `_bufferLock`）
- `TerminalDocumentViewModel.ResizeAsync` 命令中的 `Buffer.Resize(Columns, Rows)`（未在 `_bufferLock` 内）

`Resize` 会整体替换 `ViewportRows` 数组并修改 `Dimensions`。若 `OnRender` 正读到一半，极易 `IndexOutOfRangeException` 或读到半更新状态（`ViewportHead` / `_scrollbackCount` 撕裂）。`_bufferLock` 仅保护 VM 中的一处写，控件侧的读与写都在锁外，等于没有统一同步。

- 位置：`src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs`（`AppendOutput` / `ResizeAsync`）；`src/AgentTerminal.Terminal/Rendering/TerminalControl.cs`（`OnRender` / `OnResizeDebounceTick`）
- 建议（择一）：
  - 首选：将 `Parser.Parse` 也移到 UI 线程（在 flush tick 里对累积的原始文本解析），使 buffer 变为单线程访问，同时天然解决问题 1；或
  - 在 `TerminalBuffer` 内部对所有读写（含 `GetCell/CopyRow/Resize`）加同一把锁，`OnRender` 与控件侧 `Buffer.Resize` 均走同一把锁。

---

## 🟡 Important（建议本轮或紧接着处理）

- **缺少常见 CSI 编辑序列**：`VtParser.ExecuteCsi` 未实现 `@`(ICH)、`P`(DCH)、`L`(IL)、`M`(DL)、`X`(ECH) 及光标保存/恢复 `ESC 7/8`、`CSI s/u`。PSReadLine（命令行内编辑、历史）与 vim/less 大量使用；缺失不产生乱码，但会导致行内编辑、插入/删除行时错位。补 `L/M/@/P/X` + `s/u` 收益最大。
- **`_brushCache` 为无界 static 字典**：TrueColor 场景可增长到百万级，长会话内存只增不减。建议限容（LRU）或对 RGB 直接构造不缓存。
- **`Resize` 内容保留策略是从顶部保留**（`ResizeScreen` 复制 `0..copyRows-1`）：终端惯例应保留**底部**（提示符/光标处）；缩小行数时会丢掉带提示符的底部并把光标 clamp 上移，体感是内容「跳走」。属可接受首版取舍，但建议记入已知限制。

## 🟢 Nits（可选）

- `VtParser`：`_currentParam = _currentParam*10 + …` 无上限，超长数字串会整型溢出成负数，建议加上限 clamp。
- `OnRender` 每行 `new List<ushort>() / List<double>()` + `.ToArray()`，每帧按行分配，高刷新率下有 GC 压力，可复用成员缓冲数组。
- 存在两套「默认色」来源：`OnRender` 反显回退用 index 7/0，另有 `TerminalColor.DefaultForegroundRgb(204,204,204)` / `DefaultBackgroundRgb(12,12,12)`，建议统一避免漂移。
- `f4dc0dd` 的 `AC07` 测试：`Assert.NotNull(Process.GetProcessById(pid))` 冗余（该方法要么返回非空要么抛异常）；`await Task.Delay(300)` 固定等待偏脆，建议轮询直到进程消失或超时；缺 `try/finally` 兜底释放，断言中途失败会漏杀会话。

## 🎉 Praise

- `Cell`(12B) + `TerminalColor` 32 位打包 + `CellAttributes` 位标志，紧凑清晰。
- 环形回滚 + `O(1)` `ScrollUp` + FIFO 淘汰 + 主/备用屏隔离，设计干净。
- 渲染层按背景色合并矩形 + GlyphRun 批绘、`VisualTreeHelper` 零子 Visual 断言，方向正确、有性能意识。
- 测试覆盖到位（跨 chunk 解析、SGR、备用屏、100k 压测、STA 渲染），符合项目 xUnit 规范。

---

## 与 openspec 规格的对照

- `terminal-renderer` spec 的「批量刷新防雪崩 / 高频输出不逐字符调度」：buffer 侧满足，但**渲染侧实时刷新链路缺失（问题 1）**。
- `terminal-interaction` 的 resize 联动与**问题 2 的并发写冲突**相关。
- 建议两条阻断项修复后，再在 `tasks.md` 勾选 3.5 / 4.4 并执行 `openspec archive`。

## 建议修复顺序

1. 合并修复问题 1 + 2：将 `Parser.Parse` 移至 UI 线程 flush tick，并在同一 tick 触发控件 `InvalidateVisual`（改动集中在 `TerminalDocumentViewModel` 与 `TerminalControl` 的失效钩子）。
2. 补齐 🟡 的 CSI 编辑序列与 brush 缓存限容。
3. 真机手动验证：pwsh 启动、命令回显、vim/进度条、备用屏、窗口缩放联动。
