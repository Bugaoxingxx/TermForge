# TermForge MDI 工作台验证报告

- 日期：2026-09-05
- 环境：Windows 11 x64, WPF (.NET 8.0-windows)
- 对应文档：[PRD-Phase1-ConPTY.md](../PRD-Phase1-ConPTY.md), [TODO-Phase1-ConPTY.md](../TODO-Phase1-ConPTY.md)

---

## 1. MDI 交互行为与规格验证（MDI-01 ~ MDI-08）

| 需求项 | 验证功能与交互点 | 实际实现与验证结果 | 结论 | 证据路径 |
| --- | --- | --- | --- | --- |
| **MDI-01** MMC 风格工作台外壳 | 顶部主菜单栏（文件/视图/窗口/帮助）、标准工具栏、左侧可折叠导航树、中央 MDI 文档宿主容器、右侧属性面板、底部诊断输出面板、底部状态栏 | 完整 WPF 布局实现，响应式自适应缩放，所有菜单项与工具栏绑定 `RelayCommand` | **通过** | `src/AgentTerminal.App/MainWindow.xaml`<br>`src/AgentTerminal.App/ViewModels/MainWindowViewModel.cs` |
| **MDI-02** 多文档管理与上下文 | 动态文档集合管理、唯一 ID、标题绑定、活动文档上下文跟随、导航树与主菜单实时联动 | 3个文档独立创建、激活、关闭；关闭一个文档后自动回退激活前一个文档；快捷键 `Ctrl+Tab` 正向轮换，`Ctrl+Shift+Tab` 反向轮换，`Ctrl+F4` 关闭当前文档 | **通过** | `src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs`<br>`tests/AgentTerminal.Tests/Docking/MdiWorkbenchTests.cs` |
| **MDI-03** 子窗口布局与层叠平铺 | 窗口自由拖拽移动、8 向边缘缩放控制、激活置顶 Z-Index 提升、层叠 (Cascade)、水平平铺 (Tile Horizontal)、垂直平铺 (Tile Vertical)、全部还原 (Restore All) | 纯几何布局管理器严格按 PRD 计算：<br>- 层叠：等距对角线偏移 (dx=26, dy=26)<br>- 水平平铺：N 等分高度纵向堆叠<br>- 垂直平铺：N 等分宽度横向排列<br>- 全部还原：恢复所有最小化与最大化窗口至 Normal 状态并恢复原 Bounds | **通过** | `src/AgentTerminal.Docking/Layout/MdiLayoutManager.cs`<br>`tests/AgentTerminal.Tests/Docking/MdiWorkbenchTests.cs` (5 项用例 100% 通过) |
| **MDI-04** 子窗口最大化与还原 | 双击标题栏或点击最大化按钮充满工作区、隐藏自身边框控制、保留并恢复进入最大化前的原始坐标 Bounds | 双击标题栏触发 Maximize/Restore 切换；进入最大化时自动保存 `Left/Top/Width/Height`，还原后坐标与尺寸 100% 恢复 | **通过** | `src/AgentTerminal.Docking/Controls/MdiChildWindow.xaml.cs`<br>`tests/AgentTerminal.Tests/Docking/MdiWorkbenchTests.cs#L66-L84` |
| **MDI-05** 子窗口最小化托盘 | 最小化至 MDI 容器底部托盘栏 (160x28 紧凑胶囊按钮)，点击托盘按钮一键还原至原位置并设为活动窗口 | 最小化窗口折叠并沉底至 MdiContainer 底部专用托盘栏，不占用工作区显示区域，点击即恢复 | **通过** | `src/AgentTerminal.Docking/Controls/MdiContainer.xaml`<br>`src/AgentTerminal.Docking/Controls/MdiChildWindow.xaml` |
| **MDI-06** 独立会话生命周期隔离 | 布局操作（层叠、平铺、缩放、最大化、最小化、Tab 切换）不影响底层 ConPTY 进程运行；关闭文档才触发会话终止与 Job Object 回收 | 视觉树隐藏与移动与 `ITerminalSession` 完全解耦，底层流式读取持续进行，不受任何 UI 布局变换影响 | **通过** | `src/AgentTerminal.Docking/ViewModels/TerminalDocumentViewModel.cs#L220-L245` |
| **MDI-07** 1 MiB 有界缓冲保护 | 终端显示缓冲区达到 1 MiB 上限时自动截断最早的历史数据，并在 UI 顶部展示截断告警提示条，防止海量文本导致 WPF 内存溢出 | 单元测试模拟 1.2 MiB 数据注入，成功将缓冲区裁剪至上限并在前部增加截断标记，UI 响应无卡顿 | **通过** | `tests/AgentTerminal.Tests/ConPty/ConPtyContractTests.cs#L132-L155` |
| **MDI-08** 快捷键与键盘导航 | 窗口与文档键盘快捷键：`Ctrl+N` 新建会话，`Ctrl+F4` 关闭文档，`Ctrl+Tab` 切换文档，`Ctrl+Shift+Tab` 反向切换文档 | 窗体路由快捷键与菜单快捷键对齐，全局快捷键直达 ViewModel 命令执行 | **通过** | `src/AgentTerminal.App/MainWindow.xaml` |
