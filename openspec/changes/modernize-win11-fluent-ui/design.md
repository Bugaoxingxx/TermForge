## Context

TermForge 当前在 `.NET 8` + 自绘 Win7 Aero Chrome 上交付 MMC/MDI 工作台。`refine-win7-aero-ui` 已完成主题字典拆分、非透明 `WindowChrome`、矢量图标和高对比度回退，但「玻璃是画出来的、颜色全硬编码、标题栏丢失系统手感」使外壳仍不像原生 Windows。产品已决定把外壳迁到 Windows 11 Fluent，并把运行时基线升到 `.NET 10` LTS（`.NET 9` 已 EOL）。

约束：
- WPF 不支持 NativeAOT；本变更不引入 `PublishAot`。
- 终端视口必须保持不透明、无材质、无圆角裁剪，避免 ConPTY 渲染损耗。
- Mica 与当前不透明自绘 Chrome 互斥，必须先改外壳再谈真实材质。
- `ThemeMode` 代码访问仍是 experimental（`WPF0001`），需显式抑制。
- 本机已有 `.NET 10` 桌面运行时，缺的是 SDK；`global.json` 仍锁 `8.0.424`。

## Goals / Non-Goals

**Goals:**
- 将 7 个项目统一到 `net10.0` / `net10.0-windows`，`global.json` 锁定 .NET 10 SDK，升级后视觉可先零变化。
- 主窗口改为 Fluent 原生外壳：系统/Fluent chrome、Mica（或等价系统 backdrop）、圆角、投影、深浅色与强调色跟随、Snap Layouts。
- 颜色改为语义 token；关键图标改 Segoe Fluent Icons；高对比度与 Win10 可降级。
- 产品文档（README / PRD / 架构 / 设计 / 品牌）明确写出基线变更，避免入口文档仍写 .NET 8 + Win7 Aero。
- 归档已完成的 `refine-win7-aero-ui`。

**Non-Goals:**
- 不重做 ConPTY、VT、Buffer、会话生命周期或公开业务 API。
- 不把 MDI 工作区改成 WinUI 3 / WebView；MDI 仍是 WPF 自研容器。
- 不强制引入 `WPF-UI`；仅在自研 chrome 成本过高时再评估。
- 不承诺 Win10 上出现 Mica；不逐像素复刻 Win11 Settings。
- 不改写 Phase 1 PRD 的历史 FR/AC，只追加后续基线说明。

## Decisions

### 1. 目标运行时用 .NET 10 LTS，不用 .NET 9

Fluent/`ThemeMode`/Mica 从 .NET 9 引入，但 .NET 9 已于 2026-05 EOL。`.NET 10` 是当前仍受支持的 LTS，且仓库里的 `Microsoft.Extensions.*` 已是 `10.0.11`。

替代：停在 .NET 8 用 P/Invoke 补 Mica。能做，但拿不到官方 Fluent 控件基线，后续还是要升级。

### 2. 分两步落地，先升框架再改视觉

第一步只改 TFM 与 `global.json`，`ThemeMode` 保持 `None`，验收 85+18 测试全绿。第二步再开 Fluent 并重构 chrome。这样框架回归与视觉回归可分开定位。

替代：一次提交同时升框架和换外壳。风险高，FlaUI 元素树与编译失败会缠在一起。

### 3. 外壳走框架 Fluent + 系统 backdrop（路径 A），不保留假玻璃标题栏

启用 `ThemeMode=System` 后，框架会对窗口套 Mica（`DWMSBT_MAINWINDOW`）。自绘不透明 `AeroGlassBrush` 标题栏必须放弃，否则材质透不出来，Snap Layouts 也会继续缺失。

替代：路径 B 自绘 chrome + `DwmSetWindowAttribute(38/20/33)`。更灵活，但要自己做 `HTMAXBUTTON` 与最大化留边。默认不选。

若默认 Mica 不合适用 `Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop` 关掉后再精调。

### 4. 颜色与图标跟系统，MDI 皮肤可保留品牌

`Colors.xaml` 改为浅/深语义 token，绑定 accent / `SystemColors`。工具栏与标题栏图标迁 Segoe Fluent Icons。`AeroDockingTheme.xaml` 继续给 MDI 子窗做应用皮肤，避免工作区变成纯系统控件堆砌。

替代：MDI 一并改成系统 Caption。会更「系统」，但丢失锻造台辨识度。

### 5. 产品文档现在就写清方向，代码完成后再改「当前要求」措辞

入口文档必须说明「已决定迁 .NET 10 + Fluent」。在第一步合并前，README 的 SDK 要求写成「当前构建仍为 .NET 8；目标基线 .NET 10，见本变更」。第一步落地后改成「需要 .NET 10 SDK」。

Phase 1 PRD 第 6 节「复用 .NET 8」保留为当时范围，文首增加后续基线，避免把历史验收改成好像当时就要求 .NET 10。

### 6. 默认不引入 WPF-UI

与 `AGENTS.md`「最小化依赖」一致。自研 `ThemeMode` + 必要时少量 DWM P/Invoke 即可。若 chrome 命中测试或 Snap Layouts 卡住，再单独立项评估 `lepoco/wpfui`。

## Risks / Trade-offs

- **[Mica 与自绘 Chrome 互斥]** → 第二步必须重构 `MainWindowChrome.xaml`；第一步禁止碰视觉。
- **[`ThemeMode` 仍是 experimental]** → 抑制 `WPF0001`，并在设计/README 标明 API 可能变。
- **[Fluent 默认样式与现有 Controls.xaml 混搭]** → 梳理未模板化控件；冲突处用产品覆盖，而不是再叠一层 Aero。
- **[Win10 无 Mica]** → OS 版本检测后回退 Acrylic 或纯色；失败静默，不黑边。
- **[18 项 UI 测试随 chrome 失效]** → 第二步同步改页面对象，并去掉重复 `AutomationId`（见 `docs/review/2026-09-06-6fe0b84.md`）。
- **[入口文档超前于代码]** → 文档区分「已决定」与「已落地」；第一步合并当天改 SDK 要求。

## Migration Plan

1. 安装 .NET 10 SDK；改 `global.json` 与 7 个 TFM；还原编译；跑通单元与 UI 测试。
2. 同步 README / 架构文档中的「当前 SDK」表述。
3. 启用 `ThemeMode=System`，重构主窗口 chrome，接入 backdrop 与系统色。
4. 更新 FlaUI 页面对象与主题冒烟测试；Win11 / Win10 / 高对比度抽检。
5. 归档 `refine-win7-aero-ui`（其任务已全部勾完）。

回滚：第一步可把 TFM 与 `global.json` 退回 .NET 8。第二步按主题字典与 chrome 文件回退；终端与会话代码不在本变更触碰面。

## Open Questions

- 主窗口最大化按钮是否必须在第一期就接好 Snap Layouts（路径 A 由系统处理；若改路径 B 则必须自做命中测试）。
- Win10 回退选旧 Acrylic 还是纯色，等真机观感后再定，默认纯色更稳。
- 是否在第二步中期引入 WPF-UI，仅当自研 chrome 成本明显过高时重开评估。
