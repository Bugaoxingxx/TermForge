# TermForge (AgentTerminal)

> 终端之锻造台 / The Terminal Workspace & Agent CLI for Windows

**TermForge** 是一款 Windows 原生的专业级终端工作台，为开发者和运维工程师打造原生的 Shell + AI Agent 集成工作区。
本项目基于 **.NET 8 LTS + WPF + ConPTY + AvalonDock + Native Terminal Renderer** 深度构建，提供极致原生的终端交互与现代化 IDE 级的自由停靠体验。

---

## ✨ 核心特性与技术路线

### 🖥️ 原生 Windows ConPTY & 多 Shell 支持
* **拒绝无头嵌入**：不使用 PuTTY + SetParent，不依赖 WebView2 / xterm.js，无额外多进程及高内存消耗。
* **原生 Shell 全覆盖**：深度支持 PowerShell 7、Windows PowerShell、CMD、WSL。统一抽象为 `TerminalSession`。

### 🎨 原生 WPF 高性能渲染 (Native Renderer)
* **字符网格渲染**：基于 `DrawingVisual` / `DrawingContext` / `GlyphRun` 自研终端绘制，彻底避免海量 WPF 控件树与 TextBlock 带来的内存与排版开销。
* **现代终端体验**：支持 ANSI / 256 色 / TrueColor、Unicode、光标、平滑滚动与灵活选区。

### 🤖 AI Agent 工作台一等公民
* **Agent CLI 深度集成**：直接支持 Codex、Claude Code、Gemini CLI 及自定义 Agent。
* **独立 PTY 运行环境**：Agent 运行于独立的标准 PTY 会话中，与 UI 停靠解耦。
* **高频 I/O 缓冲防卡顿**：异步流读取 + 线程安全缓冲区 + 批量渲染刷新（~16ms 周期），保障 Agent CLI 高频大量输出时 UI 丝滑流畅。

### 🎯 自由 Docking 布局 (AvalonDock)
* **自由停靠与拆分**：支持 Visual Studio / MMC 风格的 Tab 拖拽、多屏浮动、左右/上下自由拆分停靠。
* **无感知会话保持**：窗口拖动、拆分与浮动操作绝不中断或重启底层的 Shell / Agent 进程。
* **工作区快照与恢复**：支持完整保存与恢复包含终端会话、布局与环境变量的 Workspace。

### 🛡️ 进程生命周期与安全
* **Windows Job Object 管控**：通过 `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` 严格管控 Shell 及其派生的所有子进程树，杜绝关闭 Tab 或退出程序后的孤儿进程残留。

---

## 📁 目录架构

```text
TermForge/
├── .gitignore                      # Git 忽略配置
├── .editorconfig                   # 代码格式与规范
├── global.json                     # 锁定 .NET SDK 8.0
├── Directory.Build.props           # 全局构建属性
├── AgentTerminal.sln               # 解决方案
├── src/
│   ├── AgentTerminal.App/          # WPF 桌面主程序入口与主窗口
│   ├── AgentTerminal.Core/         # 核心领域模型与抽象接口 (ITerminalSession, Profiles)
│   ├── AgentTerminal.Terminal/     # ConPTY、VT 解析、字符 Buffer、输入映射、原生渲染器
│   ├── AgentTerminal.Docking/      # 停靠与工作区布局管理 (AvalonDock)
│   └── AgentTerminal.Infrastructure/ # Win32 (JobObject)、Serilog 日志、JSON 配置
├── tests/
│   └── AgentTerminal.Tests/        # 单元测试与集成测试 (xUnit + FluentAssertions)
├── docs/                           # 架构与开发设计文档
└── build/                          # 构建与自动化脚本
```

---

## 🚀 快速开始

### 系统要求
* Windows 10 (1903+) / Windows 11
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.400+)
* PowerShell 7.0+（推荐）
* WSL2（可选，用于 Linux 工作负载）

### 构建与测试

```powershell
# 使用 dotnet CLI 构建解决方案
dotnet build

# 执行单元测试
dotnet test
```

或使用内置构建脚本：
```powershell
.\build\build.ps1
```

---

## 🗺️ 开发阶段与路线规划

详细技术设计与阶段规范请参阅：[Windows 原生 Agent Terminal 工作台 详细开发计划](Windows原生Agent_Terminal开发计划.md) 与 [架构设计文档](docs/ARCHITECTURE.md)。

* [x] **环境初始化**：工程多模块分层、依赖配置、代码规范、核心领域模型与测试套件
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

---

## 🎮 快捷键规划

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+T` | 打开新终端标签页 |
| `Ctrl+Shift+V` | 垂直分割窗格 |
| `Ctrl+Shift+H` | 水平分割窗格 |
| `Ctrl+Shift+W` | 关闭当前终端窗格 |
| `Ctrl+Tab` | 切换终端标签页 |
| `Ctrl+Shift+C` / `Ctrl+C` (有选区时) | 复制 |
| `Ctrl+Shift+V` / `Ctrl+V` | 粘贴 |
| `Ctrl+C` (无选区时) | 向终端发送 SIGINT 中断信号 |

---

## 📝 配置示例 (`profiles.json`)

```json
{
  "shellProfiles": [
    {
      "name": "PowerShell 7",
      "executablePath": "pwsh.exe",
      "arguments": "-NoLogo",
      "isDefault": true
    },
    {
      "name": "WSL",
      "executablePath": "wsl.exe"
    }
  ],
  "agentProfiles": [
    {
      "name": "Claude Code",
      "command": "claude"
    },
    {
      "name": "Codex CLI",
      "command": "codex"
    }
  ]
}
```

---

## 🤝 贡献与反馈

欢迎提交 Issue、Pull Request 和功能建议！

- **GitHub Issues** – 报告 Bug 和请求功能
- **Discussions** – 讨论设计和最佳实践

---

**TermForge** – 让终端成为你的锻造台 🔨⚡
