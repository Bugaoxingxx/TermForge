# TermForge 品牌 LOGO 与视觉识别设计系统 (Logo & Brand Identity System)

> **TermForge（终端之锻造台）**  
> *The Terminal Workspace & Agent CLI for Windows*  
> 视觉规范版本：v1.0 (2026)

---

## 🌟 1. 设计核心理念 (Design Philosophy)

**TermForge** 的定位是为 Windows 平台打造的原生级 Shell 与 AI Agent 深度集成的终端工作台。  
项目的视觉风格传承了 **Windows 7 Aero Glass** 的经典晶莹透光质感，同时承载高强度的现代字符终端交互。

因此，LOGO 的设计紧扣三大核心意象的深度融合：

```text
    ┌─────────────────────────────────────────────────────────────┐
    │                        TermForge                            │
    ├───────────────────┬─────────────────────┬───────────────────┤
    │  Term (终端命令)  │   Forge (锻造熔炉)   │ Aero Glass (经典) │
    │   • 提示符 >_     │    • 工业铁砧 (Anvil)│    • 晶莹圆角毛玻璃│
    │   • 极客字符光标  │    • 炽热熔光 (Flame)│    • 45° 镜面高光带  │
    │   • 电光天青蓝    │    • 飞溅星火 (Sparks)│    • 双线立体发光轮廓│
    └───────────────────┴─────────────────────┴───────────────────┘
```

1. **铁砧 (Anvil)**：沉稳、坚实、工业级品质。象征着高稳定性的 ConPTY 底座与严格的 Windows Job Object 进程管控。
2. **终端光标与提示符 (`>_`)**：极客与开发者的灵魂。雕刻于铁砧核心，电光青蓝如同凝聚的智能算力与指令流。
3. **锻造之火与星火 (Molten Fire & Sparks)**：象征 AI Agent 实时交互、高频流式输出以及代码构建的蓬勃生命力。
4. **Aero Glass 水晶质感 (Crystal Glass Squircle)**：致敬 Windows 巅峰时期的拟物晶莹美学，透光、高光折射与现代深色终端视口形成“外透内敛”的独特辨识度。

---

## 🎨 2. 四套设计方案与应用场景

| 方案编号 | 方案名称 | 核心特征 | 推荐最佳应用场景 | 对应文件路径 |
| :--- | :--- | :--- | :--- | :--- |
| **方案 1 (推荐)** | **Aero Glass 晶莹拟物图标** | 3D 水晶毛玻璃胶囊、熔火铁砧、荧光 `>_` | Windows 桌面快捷方式、安装包、启动屏 (Splash) | `docs/design/images/termforge_app_icon_aero.jpg` |
| **方案 2** | **Aero Console 视窗桌面图标** | 毛玻璃终端视窗轮廓、深邃黑底、金色锻造火花 | MDI 标题栏徽标、任务栏窗口预览、托盘图标 | `docs/design/images/termforge_app_icon_glass_window.jpg` |
| **方案 3** | **极简现代矢量徽标 (Minimal Vector)** | 极简硬核几何线条、双色渐变、极高辨识度 | 网页 Favicon (16x16)、文档导航标、单色贴纸 | `docs/design/images/termforge_logo_minimal.jpg` / `.svg` |
| **方案 4** | **官方全景品牌横幅 (Brand Banner)** | 16:9 全画幅、Aero 水晶发光标 + Segoe UI 标准字 | GitHub README 顶部大图、官网首页、技术演示 | `docs/design/images/termforge_brand_banner.jpg` |

---

## 📐 3. 矢量资产与多阶尺寸规范 (Scalable Assets)

为了确保 LOGO 在任何 DPI 与尺寸下均清晰锐利，设计系统提供纯矢量 SVG 文件：

* **单标矢量**：[`docs/design/images/termforge-logo-mark.svg`](images/termforge-logo-mark.svg)
* **全幅横版**：[`docs/design/images/termforge-logo-horizontal.svg`](images/termforge-logo-horizontal.svg)
* **扁平极简标**：[`docs/design/images/termforge-logo-flat.svg`](images/termforge-logo-flat.svg)

### 像素级尺寸适配表现：

* **512 × 512 px**：全细节展示，包括毛玻璃镜面高光、网格辅助线、星火粒子轨道与铁砧倒角。
* **128 × 128 px**：Windows 开始菜单中大磁贴、控制面板卸载程序列表。
* **48 × 48 px**：Windows 桌面快捷方式标准尺寸，保证外圈天青玻璃框与中央金光对比鲜明。
* **32 × 32 px**：MMC 3 栏工作台树状导航图标、Alt+Tab 多任务切换视图。
* **16 × 16 px**：系统托盘 (Tray)、网页 Favicon，使用 `termforge-logo-flat.svg` 保证高对比度辨识。

---

## 🌈 4. 品牌色彩系统 (Color Tokens)

| 色彩角色 | 变量名称 | HEX 色值 | RGBA 色值 | 视觉含义与使用规范 |
| :--- | :--- | :--- | :--- | :--- |
| **Aero 天青蓝** | `AeroSkyCyan` | `#38BDF8` | `rgba(56, 189, 248, 0.9)` | 玻璃外框高光、主活动窗口、`>_` 终端命令提示符 |
| **锻造炽烈橙** | `ForgeAmber` | `#FF7A00` | `rgba(255, 122, 0, 1.0)` | 锻造烈火、冲击星花、AI Agent 活力火花 |
| **纯阳亮金** | `ForgeGold` | `#FFF275` | `rgba(255, 242, 117, 1.0)` | 冲击焦点核星花、字标渐变高光 |
| **深邃黑曜** | `ObsidianDark`| `#0C0F14` | `rgba(12, 15, 20, 1.0)` | 终端字符画布底色、铁砧沉稳基底色 |
| **镜面高光** | `AeroSheen` | `#FFFFFF` | `rgba(255, 255, 255, 0.45)`| 顶部 45° 半月形镜面反光带、双层倒角线 |

---

## 💻 5. 工程与代码集成示例

### 5.1 在 README.md 中引入品牌横幅
```markdown
<p align="center">
  <img src="docs/design/images/termforge_brand_banner.jpg" width="720" alt="TermForge Brand Banner" />
</p>
```

### 5.2 在 WPF `MainWindow.xaml` 中绑定窗口图标
```xml
<Window x:Class="AgentTerminal.App.MainWindow"
        ...
        Icon="Assets/termforge_app_icon_aero.jpg"
        Title="TermForge">
```

### 5.3 在 HTML / 浏览器中交互预览
直接双击打开仓库内自带的无依赖交互原型页面：  
👉 [本地交互原型: docs/design/termforge_logo_preview.html](termforge_logo_preview.html)
