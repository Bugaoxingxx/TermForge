## Why

TermForge 已具备 Win7 色彩、渐变和 MDI 窗口雏形，但主窗口仍继承当前操作系统标题栏，标准控件、内部窗口和调试视图也使用了不一致的视觉语言，导致整体更像“现代 WPF 界面叠加 Aero 装饰”而不是统一的 Windows 7 桌面工作台。当前终端渲染能力即将接入 MDI 外壳，正适合在不重做业务交互的前提下建立稳定、可复用、可验证的 Win7 Aero 视觉系统。

## What Changes

- 显式采用 WPF 内置 Windows 7-era Aero 主题作为标准控件基线，并用分层资源字典集中管理 TermForge 的颜色、尺寸、字体、控件状态和窗口模板。
- 为主窗口提供 Win7 Aero 风格的自定义非客户区，同时保留拖动、缩放、双击最大化、系统菜单、Snap 和正确的最大化工作区行为。
- 统一菜单栏、工具栏、导航树、属性面板、状态栏和分隔条的 MMC/Explorer 视觉层级，减少常驻胶囊、圆角卡片、过强发光和固定品牌信息。
- 统一 MDI 子窗口的活动/非活动标题栏、边框、阴影、标题按钮和最小化托盘，并将局部重复资源收敛到共享主题。
- 用像素对齐的矢量或位图资源替换界面中的 Emoji/Unicode 控件图标；将应用图标替换为包含多尺寸图像的 `.ico`。
- 统一 Segoe UI 与中文 UI 字体回退、9pt 对应字号、Win7 间距节奏以及 Normal/Hover/Pressed/Focus/Disabled/Inactive 状态。
- 增加 100%～200% DPI、高对比度、键盘导航和多种窗口尺寸下的视觉与行为验收。
- 在 `add-terminal-rendering` 接入 `TerminalControl` 后完成终端宿主区域的最终视觉整合，不对即将退居诊断用途的 `TerminalDebugView` 做高成本重绘。

## Capabilities

### New Capabilities

- `win7-aero-shell`: 主窗口 Aero 窗框及菜单、工具栏、导航、属性和状态区域的统一 Win7/MMC 视觉与交互契约。
- `mdi-aero-windows`: MDI 子窗口、窗口状态视觉反馈、标题按钮和最小化托盘的统一 Aero 表现。
- `desktop-ui-compatibility`: DPI、像素对齐、高对比度、键盘焦点、图标资源和跨 Windows 版本降级行为的验收要求。

### Modified Capabilities

无。当前主规格目录尚无已归档的 UI 视觉能力；本变更不改变 `add-terminal-rendering` 中终端缓冲、VT、渲染和输入语义。

## Impact

- 主要影响 `src/AgentTerminal.App/App.xaml`、`MainWindow.xaml`、`Themes/`、应用图标资源，以及 `src/AgentTerminal.Docking/Controls/MdiChildWindow.xaml` 和 `MdiContainer.xaml`。
- 可能新增窗口外框辅助行为、主题资源字典和视觉回归/高 DPI 验收测试，但不改变公开业务 API、终端协议或会话生命周期。
- 依赖 `add-terminal-rendering` 完成 `TerminalControl` 到 MDI 文档宿主的接入后再进行终端区域最终收口；其余外壳与主题工作可独立推进。
- 优先复用 WPF 自带 `PresentationFramework.Aero`，不引入第三方 UI 框架；如目标运行时未自动提供主题程序集，则仅增加对应框架程序集引用。
