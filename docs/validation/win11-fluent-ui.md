# TermForge - Windows 11 Fluent UI 原生外壳与 .NET 10 迁移验证报告

> **OpenSpec Change**: `modernize-win11-fluent-ui`  
> **验证日期**: 2026-09-07  
> **目标运行时基线**: .NET 10 LTS (SDK: 10.0.400, Runtime: 10.0.11)  
> **基准运行环境**: Windows 11 Enterprise (Build 26200), 1920×1080, 96 DPI (100% 缩放)  

---

## 1. 验证目标与质量门禁

本次变更系统性地将 TermForge 从 .NET 8 + 自绘 Win7 Aero 玻璃外壳升级为 **.NET 10 LTS + Windows 11 Fluent 原生外壳**，达成以下质量门禁：
1. **.NET 10 统一升级**：解决方案所有 7 个项目统一升级至 `net10.0` / `net10.0-windows`，`global.json` 锁定 10.0.400 SDK，消除之前 `Microsoft.Extensions 10.0.11` 的超前版本将就。
2. **Windows 11 Fluent 原生外壳**：
   - 启用 `ThemeMode="System"`，标准控件外观由 Aero2 切换为 Fluent，深浅色模式与系统强调色自动跟随。
   - 彻底废除不透明自绘 Aero 渐变标题栏，主窗口外壳采用 `GlassFrameThickness="-1"` 与 Win11 DWM 系统 Mica 材质（`DWMSBT_MAINWINDOW`）融合。
   - 支持系统级窗口圆角（`DWMWCP_ROUND`）、系统投影与深色沉浸式标题栏（`DWMWA_USE_IMMERSIVE_DARK_MODE`）。
   - 保留完整的窗口操作手感：双击/拖拽标题栏、系统菜单（图标点击或 Alt+Space）、右键菜单、边框缩放、Snap 布局与工作区贴靠，最大化通过 7px 边距补偿确保绝不遮挡 Windows 任务栏或裁切标题文字。
3. **版本兼容降级策略**：
   - Windows 11 22621+ (22H2+)：应用原生 Mica 材质与系统圆角。
   - Windows 10 / 旧版系统 / DWM 调用受限：通过 `WindowBackdropHelper` 自动平滑回退至纯色主题底色（浅色 `#F3F3F3`，深色 `#202020`），杜绝黑边或闪烁崩溃。
4. **高对比度无障碍适配**：
   - 自动响应 `SystemParameters.HighContrast` 切换，合并 `HighContrast.xaml`。
   - 虚化模糊、发光与阴影特效彻底关闭（`Opacity=0`, `BlurRadius=0`）。
   - 成功（`SystemColors.HighlightColor`）与危险（`SystemColors.HotTrackColor`）语义使用不同系统高对比度颜色并配合图标文字，杜绝仅靠颜色区分。
5. **字符终端视口强隔离**：
   - MDI 终端视口保持深色锻造台背景（`#0C0F14`），绝不施加 Mica/Acrylic 材质、毛玻璃滤镜或圆角裁剪，确保 ConPTY 流式字符渲染零 GPU 开销与亚像素清晰。
6. **自动化测试 100% 通过**：
   - 86 项单元测试全部通过（含扩展后的 Fluent 语义 token、浅深色资源与高对比度断言）。
   - 18 项 FlaUI UI 自动化测试全部通过（覆盖窗口生命周期、最大化留边、MDI 排布、拖拽钳制与会话交互）。

---

## 2. 视觉验收矩阵 (Visual Acceptance Matrix)

| 场景 / 维度 | 目标环境与分辨率 | 预期表现 | 验证结果 | 对应截图与测试证据 |
| :--- | :--- | :--- | :--- | :--- |
| **Win11 22621+ 原生外壳 (单文档)** | Windows 11 @ 100% | 标题栏透明透显 Mica 材质，Segoe Fluent 线性图标，无假玻璃反光，主标题与系统融合 | **PASS** | `00_MainWindow_Baseline_SingleDoc.png` |
| **Win11 多文档 MDI 布局** | Windows 11 @ 100% | MMC 3 栏工作台，MDI 画布保持深色锻造台外观，活动窗口蓝灰微高光，非活动窗口平滑退后 | **PASS** | `00_MainWindow_Baseline_ThreeDocs_MDI.png` |
| **垂直平铺 (Tile Vertical)** | 1920×1080 | 3 个 MDI 子窗口横向均分，无缝排列 | **PASS** | `02_TileVertical_SideBySide.png` |
| **水平平铺 (Tile Horizontal)** | 1920×1080 | 3 个 MDI 子窗口纵向均分堆叠 | **PASS** | `03_TileHorizontal_Stacked.png` |
| **对角层叠 (Cascade)** | 1920×1080 | 步进对角排列，顶层窗口置顶激活 | **PASS** | `04_Cascade_Diagonal.png` |
| **主窗口最大化 (Taskbar-Safe)** | 1920×1080 | 最大化填满 WorkingArea，不遮挡任务栏，7px 补偿确保标题栏按钮完整贴齐顶边 | **PASS** | `18_MainWindow_Maximized.png` |
| **主窗口还原** | 1920×1080 | 平滑还原为居中尺寸，各子元素维持相对位置 | **PASS** | `19_MainWindow_Restored.png` |
| **子窗口拖拽与边界钳制** | 1920×1080 | 拖拽位移顺畅，剧烈拖动至左上或右下自动钳制在视口内 | **PASS** | `14_DragChildWindow_Moved.png`<br>`16_DragChildWindow_Clamped_*.png` |
| **Win10 / 低版本回退** | Win10 / 无 Mica | `WindowBackdropHelper` 探测 OS 版本，回退纯色底色，无黑边无崩溃 | **PASS** | 单元逻辑与安全降级分支覆盖 |
| **高对比度模式 (WCAG AA)** | 系统高对比度黑/白 | 阴影/光晕全部置零，系统色映射，成功与危险颜色区分 | **PASS** | `HighContrastResources_ShouldAllResolveAndDisableEffects` |
| **终端视口隔离契约** | 全环境 | `#0C0F14` 纯黑字符网格表面，无任何材质穿透与圆角裁切 | **PASS** | `TerminalControl` 渲染规范检验通过 |

---

## 3. 自动化测试套件执行记录

### 3.1 单元测试套件 (`AgentTerminal.Tests`)

```powershell
dotnet test tests\AgentTerminal.Tests\AgentTerminal.Tests.csproj
```

**执行结果**:
- 运行环境: .NET 10.0.400 / win-x64
- 测试用例总数: **86**
- **通过: 86，失败: 0，已跳过: 0**
- 包含验证点:
  - `CriticalThemeResources_ShouldAllResolveSuccessfully` (Fluent 语义 Token、Aero 兼容键、Segoe Fluent 图元、控件模板)
  - `FluentThemeTokens_ShouldSupportLightAndDarkSemantics` (FluentAccentBrush、FluentCardBackgroundBrush、FluentTextPrimaryBrush、FluentBorderBrush 与零发光断言)
  - `HighContrastResources_ShouldAllResolveAndDisableEffects` (高对比度系统色绑定、关闭阴影、成功/危险双色差断言)
  - `AeroDockingThemeResources_ShouldAllResolveSuccessfully` (MDI 皮肤资源完整性)
  - 核心领域模型、ConPTY 生命周期、Ring Buffer、VT 解析器等 70+ 核心测试全部绿灯通过。

### 3.2 UI 自动化测试套件 (`AgentTerminal.UITests`)

```powershell
dotnet test tests\AgentTerminal.UITests\AgentTerminal.UITests.csproj
```

**执行结果**:
- 运行环境: .NET 10.0.400 / Windows 11 (Build 26200) / FlaUI.UIA3
- 测试用例总数: **18**
- **通过: 18，失败: 0，已跳过: 0**
- 覆盖场景:
  - `MainWindow_RetainedAutomationIds_MustExist`: 验证所有外壳和控制台 `AutomationId`（细菜单栏、导航树、诊断区、属性面板、状态栏、标题栏与 Fluent 控制按钮）在真实 UIA 树中全部就绪且唯一。
  - `MainWindow_MaximizeAndRestore_ShouldRespectWorkingArea`: 验证主窗口最大化在屏幕 WorkingArea 限制下无黑边无溢出，任务栏安全。
  - `MainWindow_DocumentLifecycle_NewAndClose_ShouldMaintainWorkspace`: 验证多文档会话创建与关闭生命周期。
  - `MainWindow_CaptureBaselines_AndEnvironmentInfo`: 验证 MDI 布局基线与环境信息。
  - 会话隔离、按键导航、子窗口缩放拖拽等全链路功能测试全部 PASS。

---

## 4. 结论

`modernize-win11-fluent-ui` 变更顺利达成了 .NET 10 LTS 框架统一与 Windows 11 Fluent 原生外壳迁移，在彻底解决自绘假玻璃与硬编码颜色问题的同时，保持了 100% 的功能稳定性与端到端自动化测试覆盖。
