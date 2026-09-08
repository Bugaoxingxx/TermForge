# TermForge — .NET 10 升级与 Windows 11 Fluent 原生迁移设计方案

> 目标：将 UI 从「Windows 7 Aero 拟物模拟」迁移为「Windows 11 Fluent 原生风格」，获得真正的系统材质（Mica/Acrylic）、系统强调色与深浅色跟随、系统级窗口手感（圆角、投影、Snap Layouts）。
> 前置：框架由 .NET 8 升级至 **.NET 10（LTS）**。
> 立项日期：2026-09-07

---

## 1. 背景与动机

此前外壳刻意采用「非透明 `WindowChrome` + 纯 XAML 渐变模拟 Aero」，规避真 DWM 毛玻璃的兼容性问题。该决策工程上合理，但导致「不够 Windows」的三个根因：

1. **玻璃是"画"的不是"透"的**：`AeroGlassBrush` 等为静态 `LinearGradientBrush`，无法透出桌面/下层窗口，与环境割裂。
2. **颜色全硬编码**：`Colors.xaml` 全为写死色值，不跟随系统强调色与浅色/深色模式，气质偏"贴皮仿品"。
3. **自绘标题栏丢系统手感**：缺 Snap Layouts、贴靠动画、系统投影、最大化正确留边。

Windows 11 Fluent 原生能力（Mica 材质、系统色、ThemeMode）自 **.NET 9 引入、.NET 10 延续强化**，故迁移的前提是升级框架。

---

## 2. 版本决策：目标 .NET 10（LTS）

- **不选 .NET 9**：STS 标准支持期，2024-11 GA，18 个月支持，**2026-05 已 EOL**，不可作为新基线。
- **选 .NET 10**：LTS，2025-11 GA，支持至 **2028-11**；Fluent/`ThemeMode`/Mica 能力完整延续。
- **环境现状**：本机已安装 `Microsoft.NETCore.App 10.0.11` 与 `Microsoft.WindowsDesktop.App 10.0.11` 运行时，`Microsoft.Extensions.*` 包已是 `10.0.11`；**仅缺 .NET 10 SDK**（当前仅 `8.0.424`）。

---

## 3. 现状快照

| 项 | 当前值 |
| :-- | :-- |
| 目标框架 | 7 个项目均 `net8.0` / `net8.0-windows` |
| `global.json` | 固定 `sdk.version = 8.0.424`，`rollForward = latestFeature` |
| WPF | `UseWPF=true`，无 `PublishAot`（AOT 与 WPF 不兼容，不适用） |
| 主要依赖 | CommunityToolkit.Mvvm 8.4.2、Dirkster.AvalonDock 4.72.1、Serilog 4.x、Microsoft.Extensions.* 10.0.11、FlaUI.UIA3 4.0.0、xunit 2.5.3 |
| 主窗口 chrome | `MainWindowChrome.xaml` 自绘不透明标题栏 + `Background=AeroGlassBrush` |
| 测试 | 85 单元测试 + 18 FlaUI UI 自动化测试 |

---

## 4. 两步走路线图（降低风险）

### 第一步：纯升级到 .NET 10，视觉零变化（低风险，可独立提交）

目标：`ThemeMode` 保持 `None`（仍走 Aero2 旧主题），现有 Aero 视觉与行为完全不变，测试全绿。

改动清单：
1. 安装 **.NET 10 SDK**。
2. `global.json`：
   ```json
   {
     "sdk": {
       "rollForward": "latestFeature",
       "version": "10.0.100"
     }
   }
   ```
   （`version` 以实际安装的 10.0.1xx SDK 为准。）
3. 7 个 `.csproj`：`net8.0` → `net10.0`，`net8.0-windows` → `net10.0-windows`。
4. 重新还原 + 编译，跑通 85 + 18 测试。

依赖兼容性（均无阻塞，需重编译验证）：

| 包 | 版本 | 结论 |
| :-- | :-- | :-- |
| CommunityToolkit.Mvvm | 8.4.2 | ✅ 兼容 |
| Dirkster.AvalonDock | 4.72.1 | ✅ 多目标，重编译验证 |
| Microsoft.Extensions.* | 10.0.11 | ✅ 升级后更匹配（消除超前将就） |
| Serilog / Sinks | 4.x / 6.x / 7.x | ✅ 兼容 |
| FlaUI.UIA3 | 4.0.0 | ✅ 兼容 |
| xunit / Test.Sdk | 2.5.3 / 17.8.0 | ✅ 兼容（可顺带升级） |

纯升级的行为影响：
- WPF net8→net10 高度兼容；`ThemeMode=None` 时默认 Aero2 主题不变。
- C# 版本随之到 14，现有 C# 12 语法向后兼容。
- 无 AOT 相关风险。

**验收标准**：功能零回归、85 + 18 测试全绿。此步不引入任何视觉改动。

### 第二步：Fluent 原生迁移（大工程，含设计与测试改动）

在第一步绿之后进行，见 §5～§8。

---

## 5. Fluent 迁移技术方案

### 5.1 启用 Fluent 主题与深浅色跟随

- 通过 `ThemeMode` 启用（应用级或窗口级），值：`Light` / `Dark` / `System` / `None`。推荐 **`System`**（跟随用户 Windows 设置）。
- 代码访问 `ThemeMode` 在 .NET 9/10 仍为 **experimental**，会触发编译错误 `WPF0001`，需显式抑制：
  ```csharp
  #pragma warning disable WPF0001
  Application.Current.ThemeMode = ThemeMode.System;
  #pragma warning restore WPF0001
  ```
  （或在 XAML 的 `Application`/`Window` 上设置 `ThemeMode="System"`。）
- 系统强调色：Fluent 主题自动引入 accent 资源；如需在自定义模板中主动取色，用 `Windows.UI.ViewManagement.UISettings.GetColorValue(UIColorType.Accent)`，并监听变化刷新 `DynamicResource`。

### 5.2 Mica 材质与自绘 Chrome 的冲突（关键）

> Mica 要求窗口背景透明并将 frame 扩展进客户区。当前 `MainWindowChrome.xaml` 的**不透明自绘标题栏 + `Background=AeroGlassBrush`** 与之互斥。

处理路径二选一：

- **路径 A（推荐，交给框架）**：启用 Fluent 主题后，框架自动对窗口应用 Mica（`DWMSBT_MAINWINDOW`）。需**放弃自绘玻璃标题栏**，改用系统标题栏或 Fluent chrome，并移除 `Background=AeroGlassBrush`（改透明/扩展 frame）。
  - 若暂不想要默认 backdrop，可用运行时开关关闭后再自行精调：
    ```xml
    <ItemGroup>
      <RuntimeHostConfigurationOption
        Include="Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop" Value="True" />
    </ItemGroup>
    ```
- **路径 B（自行 P/Invoke 精调）**：保留自定义 chrome 骨架但让客户区透明，手动调用 `DwmSetWindowAttribute`（见 §5.3）。灵活但需自行处理标题栏命中测试、Snap Layouts。

### 5.3 DWM 窗口属性参考（`DwmSetWindowAttribute`，Win11 22621+）

在 `Window.SourceInitialized` 之后（或 `WindowInteropHelper.EnsureHandle()`）调用：

| 属性 | 常量值 | pvAttribute 类型 | 用途 |
| :-- | :-- | :-- | :-- |
| `DWMWA_USE_IMMERSIVE_DARK_MODE` | 20 | `BOOL` | 标题栏深色 |
| `DWMWA_WINDOW_CORNER_PREFERENCE` | 33 | `DWM_WINDOW_CORNER_PREFERENCE` | 圆角：`DWMWCP_ROUND=2` / `DWMWCP_ROUNDSMALL=3` |
| `DWMWA_SYSTEMBACKDROP_TYPE` | 38 | `DWM_SYSTEMBACKDROP_TYPE` | 材质：Mica=`DWMSBT_MAINWINDOW(2)`、Acrylic=`DWMSBT_TRANSIENTWINDOW(3)`、Tabbed=`DWMSBT_TABBEDWINDOW(4)` |

> 注意：旧的 `DWMWA_MICA_EFFECT=1029` 在 22H2 前为未文档化，**不要用**；统一走 `38`。

签名示例：
```csharp
[DllImport("dwmapi.dll")]
private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
```

### 5.4 恢复系统窗口投影（低成本高收益）

自绘非透明窗口常丢系统投影，用 `DwmExtendFrameIntoClientArea` 扩展 1px frame 即可拿回系统级阴影与边缘。

### 5.5 图标体系迁移

- 现状：自画 `Path Geometry` / `DrawingImage` 线框图标（`Icons.xaml`）。
- 目标：改用 Windows 视觉语言 **Segoe Fluent Icons**（Win11）字体图标，标题栏控制键使用系统 caption 语义。
- 过渡：保留 DPI 无损优势，非必要图元可暂留，优先替换工具栏/标题栏关键图标。

### 5.6 颜色 Token 重构

- 将 `Colors.xaml` 的写死色值改为**语义 token**，底层绑定到 Fluent/系统资源（accent、`SystemColors`），而非固定 HEX。
- 深浅色两套值随 `ThemeMode` 自动切换。
- `HighContrast.xaml` 的系统色回退机制可复用为「无 Mica 时回退纯色」的降级基础。

### 5.7 现有主题字典去留

| 文件 | 处置 |
| :-- | :-- |
| `Colors.xaml` | 重构为语义 token（浅/深 + accent 驱动） |
| `Typography.xaml` | 保留，对齐 Segoe UI Variable 度量 |
| `Icons.xaml` | 逐步迁移至 Segoe Fluent Icons |
| `Controls.xaml` | 梳理，避免与 Fluent 默认样式冲突/混搭 |
| `MainWindowChrome.xaml` | **重构**：放弃自绘玻璃标题栏，改系统/Fluent chrome + Mica |
| `HighContrast.xaml` | 保留，扩展为无 Mica 降级回退 |
| `AeroDockingTheme.xaml` | 保留为 MDI 工作区**应用皮肤**（品牌辨识度） |

> 定位：外壳走系统 Fluent（真原生感），MDI 工作区可保留品牌化皮肤，兼顾原生与辨识度。

---

## 6. 兼容性与降级策略

| 场景 | 能力 | 降级 |
| :-- | :-- | :-- |
| Windows 11 22621+ | Mica / 圆角 / 深色标题栏全支持 | — |
| Windows 11 22000 | 深色/圆角支持，backdrop 视版本 | 无 Mica → Acrylic 或纯色 |
| Windows 10 | 无 Mica | Acrylic（旧 API）或纯色主题 |
| 高对比度模式 | — | 复用 `HighContrast.xaml`，关闭材质与阴影 |

运行时需按 OS 版本检测（如 `IsWindows11_22H2OrNewer`）择优应用，失败静默回退纯色，保证不黑边、不崩溃。

---

## 7. 测试回归清单

- **单元测试（85）**：框架升级基本不受影响，第一步后应全绿。
- **UI 自动化测试（18，FlaUI）**：chrome/标题栏结构变化会改变 UIA 元素树，需同步更新页面对象与 `AutomationId`（注意修复既有重复 `AutomationId` 问题，见 `docs/review/2026-09-06-6fe0b84.md`）。
- **主题冒烟测试**：扩展 `ThemeResourceSmokeTests`，覆盖 Fluent 资源键存在性与浅/深/高对比度三态。
- **多环境验证**：Win11 22621+ / Win10 / 高对比度 三档手动截图核对材质与降级。

---

## 8. 风险与注意事项

1. `ThemeMode` 代码访问为 experimental（`WPF0001`），API 后续可能调整。
2. Mica 与自绘 chrome 互斥，`MainWindowChrome.xaml` 需实质重构——这是第二步主要工作量。
3. 启用 Fluent 后，未显式模板化的标准控件外观整体变化，需统一梳理避免新旧混搭。
4. Snap Layouts 需最大化按钮正确响应 `HTMAXBUTTON` 命中；路径 A 由系统处理，路径 B 需自行实现。
5. 终端视口（`#0C0F14`）须继续隔离，**不套用任何材质/圆角/滤镜**，保障 ConPTY 渲染零损耗（沿用既有约束）。

---

## 9. 可选加速器：WPF-UI（lepoco/wpfui）

成熟第三方库，已封装 Mica/Acrylic、Fluent 控件、`FluentWindow` 标题栏与 backdrop 管理，可显著减少 P/Invoke 与 chrome 重构工作量。代价：引入一个较重的 UI 依赖（与 `AGENTS.md`「最小化依赖」原则需权衡）。作为路径 A 的替代评估项。

---

## 10. 实施顺序小结

1. [ ] 装 .NET 10 SDK；改 `global.json` + 7 个 TFM；还原编译；跑通 85+18（**第一步，独立提交**）。
2. [ ] 启用 `ThemeMode=System`，抑制 `WPF0001`，验证 Aero2→Fluent 切换。
3. [ ] 重构 `MainWindowChrome.xaml`：系统/Fluent chrome + Mica（路径 A）或 P/Invoke 精调（路径 B）。
4. [ ] 颜色 token 重构 + 深浅色/accent 跟随。
5. [ ] 图标迁移 Segoe Fluent Icons。
6. [ ] 梳理 `Controls.xaml` 避免样式混搭；MDI 皮肤保留。
7. [ ] Win10/高对比度降级与多环境验证。
8. [ ] 更新 18 项 UI 测试与主题冒烟测试。
9. [x] 按 [细菜单栏命令面](./UI-Fluent-Command-Surface.md) 去掉工具栏，只保留 22 DIP 菜单栏，不改 ViewModel 命令。
