## Why

现方案此前刻意用「非透明 WindowChrome + 纯 XAML 渐变」模拟 Win7 Aero，导致「玻璃是画出来的、颜色全硬编码、自绘标题栏丢失系统手感」，整体不够原生。Windows 11 Fluent 原生能力（Mica 材质、系统强调色、深浅色跟随、`ThemeMode`）自 .NET 9 引入、.NET 10 延续强化，且当前基线 .NET 8 亦需升级到受支持的 LTS（.NET 9 已于 2026-05 EOL），因此以「升级 .NET 10 + Fluent 原生化」一并推进。

## What Changes

- **BREAKING**：目标框架从 `net8.0[-windows]` 升级到 `net10.0[-windows]`（7 个项目），更新 `global.json` 固定版本；运行时要求变为 .NET 10。
- **BREAKING**：主窗口外壳从自绘 Aero 玻璃 Chrome 迁移为 Windows 11 Fluent 原生外壳（Mica/Acrylic 材质、系统投影、圆角、Snap Layouts、深色标题栏），放弃 `MainWindowChrome.xaml` 现有的不透明自绘玻璃标题栏。
- 启用 WPF Fluent 主题（`ThemeMode=System`），标准控件基线由 Aero2 切换为 Fluent，随系统浅色/深色与强调色自动切换。
- 颜色系统从写死 HEX 重构为语义 token，底层绑定系统强调色/`SystemColors`，支持浅/深两套值。
- 关键图标由自绘 `Path`/`DrawingImage` 迁移为 Segoe Fluent Icons 视觉语言。
- 兼容性降级：Win11 22621+ 用 Mica，Win10/旧版回退 Acrylic 或纯色，高对比度复用并扩展 `HighContrast.xaml`。
- 终端视口（`#0C0F14`）继续隔离，不套用任何材质/圆角/滤镜（沿用既有约束）。
- 文档与流程同步：更新产品入口文档（`README.md`、`docs/ARCHITECTURE.md`、`docs/PRD-Phase1-ConPTY.md`、品牌/设计文档）说明运行时与外壳方向变更；归档已完成的 `refine-win7-aero-ui` 变更。

## Capabilities

### New Capabilities
- `dotnet10-runtime-baseline`: 将解决方案统一升级到 .NET 10 LTS 的构建/运行时基线契约，含目标框架、`global.json`、依赖兼容与「视觉零回归」验收。
- `win11-fluent-shell`: 主窗口 Windows 11 Fluent 原生外壳的视觉与交互契约——Fluent 主题、Mica/Acrylic 材质、系统强调色与深浅色跟随、系统圆角/投影/Snap Layouts，以及跨 Windows 版本降级与高对比度回退。

### Modified Capabilities
<!-- openspec/specs/ 主规格目录暂无已归档能力（refine-win7-aero-ui 的 win7-aero-shell 等尚未归档）；本变更不修改已归档主规格。 -->

## Impact

- **代码**：7 个 `.csproj`（`TargetFramework`）、`global.json`；`src/AgentTerminal.App/App.xaml(.cs)`（`ThemeMode`/DWM 应用）、`Themes/MainWindowChrome.xaml`（重构）、`Themes/Colors.xaml`、`Themes/Icons.xaml`、`Themes/Controls.xaml`、`Themes/HighContrast.xaml`；`src/AgentTerminal.Docking/Themes/AeroDockingTheme.xaml`（MDI 皮肤保留）。
- **依赖**：`Microsoft.Extensions.*` 已是 10.0.11（升级后消除版本超前将就）；`Dirkster.AvalonDock`/`CommunityToolkit.Mvvm`/`Serilog`/`FlaUI` 均需在 net10 下重编译验证；可选引入 `WPF-UI (lepoco/wpfui)` 作为 backdrop/Fluent 加速器（与 `AGENTS.md` 最小化依赖原则需权衡）。
- **环境**：需安装 .NET 10 SDK（本机已具备 10.0.11 运行时，仅缺 SDK）。仅 Win11 22621+ 支持 Mica；Win10 降级。
- **测试**：18 项 FlaUI UI 自动化测试需随 Chrome 结构变化更新页面对象与 `AutomationId`（并修复 `docs/review/2026-09-06-6fe0b84.md` 指出的重复 `AutomationId`）；扩展 `ThemeResourceSmokeTests` 覆盖 Fluent/浅/深/高对比度资源；85 项单元测试预期不受影响。
- **文档/流程**：更新 `README.md`（技术栈、SDK 前置、设计规范链接）；`docs/ARCHITECTURE.md` 文档索引；`docs/PRD-Phase1-ConPTY.md` 追加后续基线说明（不改写 Phase 1 FR/AC 历史事实）；`docs/design/LOGO_DESIGN_SYSTEM.md` 标明外壳方向已迁 Fluent。详细方案见 `docs/design/UI-Win11-Fluent-Migration.md`。归档 `refine-win7-aero-ui`。
