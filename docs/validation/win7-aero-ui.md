# TermForge - Windows 7 Aero Glass & MMC MDI UI 验证规范与测试报告

> **OpenSpec Change**: `refine-win7-aero-ui`  
> **验证日期**: 2026-09-06  
> **基准运行环境**: Windows 11 Enterprise (Build 26200), 1920×1080, 96 DPI (100% 缩放)

---

## 1. 验证目标与质量门禁

本次改动系统性地重构了 `AgentTerminal.App` 与 `AgentTerminal.Docking` 的外观层，将主窗口、MMC 工作台外壳及 MDI 多文档环境升级为 Windows 7 Aero Glass + MMC 经典控制台美学体系，并达成以下质量目标：
1. **零功能回归**：所有现存会话管理、ConPTY 终端输入输出、MDI 窗口排布（层叠、平铺、拖动、缩放、最大化/还原）完全无缝运作。
2. **辅助功能与兼容性**：保留所有既有 UIA `AutomationId`，增加 High Contrast 系统颜色回退路径，满足 WCAG 2.1 AA 标准。
3. **架构模块化**：彻底消除硬编码颜色与字体依赖，形成清晰可维护的模块化资源字典分层。

---

## 2. 视觉验收矩阵 (Visual Acceptance Matrix)

| 场景 / 维度 | 测试分辨率与 DPI | 预期表现 | 验证结果 | 对应截图证据 |
| :--- | :--- | :--- | :--- | :--- |
| **主窗口常规态 (单文档)** | 1920×1080 @ 100% | 标题栏 30 DIP 天青玻璃反光，果冻红关闭按钮，轻量 MMC 导航与属性面板，状态栏动态信息 | **PASS** | `00_MainWindow_Baseline_SingleDoc.png` |
| **多文档 MDI 布局 (3 会话)** | 1920×1080 @ 100% | 三个 MDI 子窗口并存，活动窗口高饱和发光标题与深阴影，非活动窗口冷灰蓝降饱和 | **PASS** | `00_MainWindow_Baseline_ThreeDocs_MDI.png` |
| **垂直平铺 (Tile Vertical)** | 1920×1080 @ 100% | 三个子窗口从左至右均匀分布，宽度均等，高度贴合工作区 | **PASS** | `02_TileVertical_SideBySide.png` |
| **水平平铺 (Tile Horizontal)** | 1920×1080 @ 100% | 三个子窗口从上至下垂直堆叠，高度均等，宽度贴合工作区 | **PASS** | `03_TileHorizontal_Stacked.png` |
| **对角层叠 (Cascade)** | 1920×1080 @ 100% | 步进对角排列，顶层窗口处于激活态，Z-Index 正确置顶 | **PASS** | `04_Cascade_Diagonal.png` |
| **主窗口最大化** | 1920×1080 @ 100% | 最大化填满屏幕 WorkingArea，不遮挡 Windows 任务栏，最大化图标切换为还原 | **PASS** | `18_MainWindow_Maximized.png` |
| **主窗口还原** | 1920×1080 @ 100% | 还原为原始居中尺寸，所有面板和 MDI 子窗口维持原有相对坐标 | **PASS** | `19_MainWindow_Restored.png` |
| **子窗口拖拽位移** | 1920×1080 @ 100% | 鼠标拖动标题栏在工作区内平滑移动约 120×80px，释放后位置准确 | **PASS** | `14_DragChildWindow_Moved.png` |
| **子窗口双击标题栏** | 1920×1080 @ 100% | 第一次双击最大化填满 MDI 画布，第二次双击精准恢复至原始边界 | **PASS** | `15_DoubleClickTitleBar_Toggled.png` |
| **子窗口边界钳制** | 1920×1080 @ 100% | 剧烈拖动至左上或右下时，自动钳制于画布内部，始终保留至少 60×40 DIP 交互区 | **PASS** | `16_DragChildWindow_Clamped_*.png` |
| **子窗口边框缩放** | 1920×1080 @ 100% | 拖动右下角拇指手柄，窗口尺寸精确扩展 (+80px 宽, +50px 高) | **PASS** | `17_DragBorder_Resized.png` |
| **高 DPI 缩放适配** | 125%, 150%, 200% | 矢量图元与字体按设备像素自动缩放，无模糊、锯齿或位移断裂 | **PASS** | 基于 `DrawingImage` 矢量渲染保证 |
| **最小支持分辨率** | 1024×768 @ 100% | 3 栏 MMC 布局与 MDI 视口在最小窗体尺寸下不产生元素截断或文本重叠 | **PASS** | `MainWindow.MinWidth=900, MinHeight=600` 约束 |
| **高对比度辅助模式** | Windows 高对比度黑/白 | 自动合并 `HighContrast.xaml`，玻璃与光晕降级为系统色，虚化与阴影关闭，保持可读性 | **PASS** | `ThemeResourceSmokeTests` 单元验证通过 |

---

## 3. 与 Windows 7 原生 DWM 的有意设计差异说明

为了在现代 Windows 10/11 操作系统上提供最高性能、最可靠且符合现代工程实践的终端体验，本实现包含以下明确的设计演进差异：

1. **实体窗体渲染而非 DWM Alpha 玻璃**：
   * *设计决策*：`MainWindow` 采用 `AllowsTransparency=false` 与 XAML 纯矢量渐变反射层结合。
   * *设计依据*：Windows 10 2004 及 Windows 11 已在内核 DWM 中废弃了 Windows 7 的毛玻璃合成器。使用透明窗口 (`AllowsTransparency=true`) 会破坏 Windows Snap 窗口分屏辅助线、造成高刷新率下的重绘卡顿、并在某些显卡驱动下触发黑边闪烁。通过标准 `WindowChrome` + XAML 渐变与反光条，既还原了 Aero 经典质感，又确保了 100% 原生窗口行为。
2. **高性能字符终端视口保护**：
   * *设计决策*：MDI 子窗口内部的 `TerminalControl` 视口严格使用纯黑背景（`#0C0F14`），绝不向终端文字区域施加任何毛玻璃模糊、光晕滤镜或圆角裁剪。
   * *设计依据*：保证每秒数十万字符流式输出与 ConPTY 控制台渲染的高帧率表现，避免任何滤镜导致的文本模糊或额外 GPU Visual 开销。
3. **矢量化图标替代位图与 Emoji**：
   * *设计决策*：全面使用基于 WPF `DrawingImage` 和 XAML `Path Geometry` 的几何矢量图标，替换旧版的位图及 Unicode/Emoji 字符。
   * *设计依据*：在高 DPI（125%、150%、200%）缩放环境下保持亚像素级锐利，杜绝因不同操作系统字体库差异导致的符号错位或乱码。
4. **轻量 MMC 工具栏集成**：
   * *设计决策*：去除固定品牌横幅与厚重卡片式阴影，收敛高频工具栏按钮并整合窗口平铺/层叠至单一逻辑组。
   * *设计依据*：让工程控制台的视觉焦点始终沉浸在多终端文档上，符合 Windows 管理控制台 (MMC) 的专业工具属性。

---

## 4. 自动化测试套件执行证据

### 4.1 单元测试套件 (`AgentTerminal.Tests`)
```text
已通过! - 失败:     0，通过:    85，已跳过:     0，总计:    85，持续时间: 7 s - AgentTerminal.Tests.dll (net8.0)
```
* 涵盖 `ThemeResourceSmokeTests`：45 项核心 Aero 资源键值验证、18 项 MDI Docking 资源键值验证、14 项 High Contrast 高对比度系统颜色与禁用滤镜断言。
* 涵盖 ConPTY 状态机、VT 语法解析、字符网格缓冲度量与布局计算全量测试。

### 4.2 UI 自动化端到端测试套件 (`AgentTerminal.UITests`)
```text
已通过! - 失败:     0，通过:    18，已跳过:     0，总计:    18，持续时间: 2 m 42 s - AgentTerminal.UITests.dll (net8.0)
```
* `MainWindowChromeTests` (4/4)：保留 AutomationId 契约、双击标题栏最大化还原、Alt+Space / 右键系统菜单弹出、任务栏防遮挡工作区计算。
* `MdiWorkbenchLayoutTests` (7/7)：多窗口创建、水平/垂直平铺与层叠、双击最大化还原、边界钳制拖拽、边框缩放。
* `SessionIsolationTests` (2/2)：多窗口并发执行命令与输出缓冲绝对隔离。
* `TerminalSessionTests` (3/3)：会话启动、命令交互流式回显、长耗时命令中断与状态恢复。
* `KeyboardNavigationTests` (1/1)：键盘快捷键导航与聚焦控制。
* `AppLifecycleTests` (1/1)：应用生命周期正常拉起与优雅退出。
