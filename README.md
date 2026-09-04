# TermForge (AgentTerminal)

Windows 原生 Agent Terminal 工作台，基于 **.NET 8 LTS + WPF + ConPTY + AvalonDock + Native Terminal Renderer** 构建。

本项目专为 Windows 原生环境打造，深度支持 PowerShell 7、Windows PowerShell、CMD、WSL 以及 Codex、Claude Code、Gemini CLI 等 Agent CLI。

## 🌟 核心特性与技术路线

* **原生 Windows ConPTY**：不使用 PuTTY + SetParent，不依赖 WebView2/xterm.js，无额外多进程及高内存消耗。
* **原生 WPF 自定义渲染**：基于 `DrawingVisual` / `DrawingContext` / `GlyphRun` 进行终端栅格字符渲染，避免海量 WPF 控件树开销。
* **多 Tab 与自由停靠**：采用 AvalonDock 实现 Visual Studio / MMC 风格的 Tab 拖拽、多屏浮动、左右/上下拆分停靠，且停靠操作不中断 Shell 会话。
* **Job Object 进程生命周期管理**：严格管控 Shell 及 Agent CLI 派生出的子进程树，防止 Tab 或主程序关闭后残留孤儿进程。
* **高频 I/O 缓冲防卡顿**：异步流读取 + 线程安全缓冲区 + 批量渲染刷新（~16ms 周期），保障 Agent CLI 高频大量输出时 UI 丝滑流畅。

## 📁 目录架构

```text
TermForge/
├── .gitignore                      # Git 忽略配置
├── .editorconfig                   # 代码格式与规范
├── global.json                     # 锁定 .NET SDK 8.0
├── Directory.Build.props           # 全局构建属性
├── AgentTerminal.sln               # 解决方案
├── src/
│   ├── AgentTerminal.App/          # 主程序入口与主窗口
│   ├── AgentTerminal.Core/         # 核心领域模型与抽象接口
│   ├── AgentTerminal.Terminal/     # ConPTY、VT 解析、字符 Buffer、输入映射、原生渲染器
│   ├── AgentTerminal.Docking/      # 停靠与工作区布局管理 (AvalonDock)
│   └── AgentTerminal.Infrastructure/ # Win32 (JobObject)、Serilog 日志、JSON 配置
├── tests/
│   └── AgentTerminal.Tests/        # 单元测试 (xUnit + FluentAssertions)
├── docs/                           # 架构与开发文档
└── build/                          # 构建与自动化脚本
```

## 🛠️ 构建与测试

### 前置要求
* Windows 10 (1903+) / Windows 11
* .NET 8.0 SDK (8.0.400+)

### 快速构建
```powershell
# 使用 dotnet CLI 构建
dotnet build

# 执行单元测试
dotnet test
```

或使用内置构建脚本：
```powershell
.\build\build.ps1
```

## 🗺️ 开发路线与规划

详细开发路线请参阅：[Windows 原生 Agent Terminal 工作台 详细开发计划](Windows原生Agent_Terminal开发计划.md)。
* [x] **环境初始化**：工程骨架、依赖引入、代码规范、核心领域模型与接口
* [ ] **Phase 1**：ConPTY POC（双向管道通信与 pwsh.exe 交互）
* [ ] **Phase 2**：Terminal Buffer（行列网格、Cell、Cursor、Scrollback 缓冲区）
* [ ] **Phase 3**：VT Parser（ANSI 转义序列、SGR、256/TrueColor 色彩）
* [ ] **Phase 4**：Native Renderer（WPF DrawingVisual 高性能原生渲染控件）
* [ ] **Phase 5**：Input / Clipboard / Resize（按键映射、选区、复制粘贴、Resize 联动）
* [ ] **Phase 6**：Shell Profile（PowerShell、CMD、WSL 配置化）
* [ ] **Phase 7**：Docking（AvalonDock 布局集成与状态持久化）
* [ ] **Phase 8**：Multi-Terminal / 生命周期与 Job Object 进程管控
* [ ] **Phase 9**：Agent Profile（Codex、Claude Code、Gemini CLI 配置与一键运行）
* [ ] **Phase 10**：Workspace（工作区项目环境持久化与恢复）
