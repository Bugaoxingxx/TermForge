# TermForge

> 终端之锻造台 / The Terminal Workspace & Agent CLI for Windows

**TermForge** 是一个专业级的终端工作台，为开发者和运维工程师打造 Windows 原生的 Shell + AI Agent 集成工作区。它不是简单的终端模拟器，而是一个强大的多工具协作平台。

## ✨ 核心特性

### 🖥️ 多 Shell 原生支持
- **PowerShell** – Windows 原生脚本语言，任务自动化
- **WSL (Windows Subsystem for Linux)** – Linux 环境无缝集成
- **CMD** – 传统 Windows 命令行兼容
- **自定义 Shell** – 支持扩展其他 Shell 环境

### 🤖 AI Agent 工作台
- **Claude / Codex 集成** – 直接在终端调用 AI 代码助手
- **Agent CLI** – 构建和运行自动化任务代理
- **上下文感知** – Agent 理解当前工作目录、环境变量和历史命令
- **一键执行** – 从自然语言生成并执行脚本

### 🎯 自由 Docking 布局
- **灵活的窗格管理** – 自定义分割、浮动、标签式终端布局
- **工作区快照** – 保存和恢复完整的工作环境配置
- **多监视器支持** – 跨屏幕无缝分布式工作

### 🛠️ 专业运维/开发工具链
- **内置任务运行器** – 快速执行常用命令、脚本、构建命令
- **命令历史和搜索** – 模糊搜索、上下文感知的命令补全
- **环境管理** – 快速切换项目环境、变量、配置文件
- **集成开发工具** – 支持 Git、Docker、Kubernetes 操作
- **输出美化** – 彩色输出、日志过滤、结构化数据展示

## 📦 架构

```
TermForge
├── Core Runtime
│   ├── Shell Host (PowerShell, WSL, CMD)
│   └── Terminal Renderer (ANSI/VT100 支持)
├── Docking System
│   ├── Layout Engine
│   ├── Workspace Manager
│   └── Theme & Customization
├── Agent Framework
│   ├── CLI Parser
│   ├── AI Integration (Claude, Codex, etc.)
│   └── Task Executor
├── Tools & Plugins
│   ├── Git Wrapper
│   ├── Docker CLI
│   ├── Kubernetes Client
│   └── Custom Extensions
└── Configuration
    ├── Profiles
    ├── Keybindings
    └── Workspace Layouts
```

## 🚀 快速开始

### 系统要求
- **Windows 10** 或更高版本
- **.NET 6.0+** 运行时
- **PowerShell 7.0+**（推荐）
- **WSL2**（可选，用于 Linux 工作负载）

### 安装

```bash
# 使用 Scoop（推荐）
scoop install termforge

# 或者使用 Chocolatey
choco install termforge

# 或从源代码构建
git clone https://github.com/Bugaoxingxx/TermForge.git
cd TermForge
dotnet build
dotnet run
```

### 基础用法

#### 启动 TermForge
```powershell
termforge
```

#### 创建新工作区
```powershell
termforge new-workspace --name "MyProject"
```

#### 使用 Agent 执行任务
```powershell
termforge agent "创建一个 React 项目骨架"
termforge agent "列出当前目录中所有 Docker 容器并显示它们的状态"
termforge agent "分析这个脚本并建议优化"
```

#### 快速切换 Shell
```powershell
# 在 PowerShell 和 WSL 之间切换
termforge shell --switch wsl
termforge shell --switch powershell
```

#### 导入/导出工作区配置
```powershell
termforge workspace --export --output ./my-workspace.json
termforge workspace --import ./my-workspace.json
```

## 🎮 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+T` | 打开新终端标签页 |
| `Ctrl+Shift+V` | 垂直分割窗格 |
| `Ctrl+Shift+H` | 水平分割窗格 |
| `Ctrl+Shift+X` | 关闭当前窗格 |
| `Ctrl+Tab` | 切换终端 |
| `Ctrl+,` | 打开设置 |
| `Ctrl+Shift+P` | 打开命令面板 |
| `Ctrl+Shift+A` | 打开 Agent 对话框 |
| `F1` | 显示帮助 |

## 📝 配置示例

### `termforge.json` 配置文件

```json
{
  "profiles": [
    {
      "name": "PowerShell Core",
      "shell": "pwsh",
      "startingDirectory": "$HOME",
      "colorScheme": "Dracula",
      "fontFace": "Cascadia Code",
      "fontSize": 10
    },
    {
      "name": "WSL Ubuntu",
      "shell": "wsl",
      "distribution": "Ubuntu",
      "startingDirectory": "~",
      "colorScheme": "One Dark",
      "fontFace": "JetBrains Mono",
      "fontSize": 11
    }
  ],
  "agent": {
    "provider": "claude",
    "apiKey": "${CLAUDE_API_KEY}",
    "model": "claude-3-5-sonnet",
    "context": {
      "includeHistory": true,
      "maxHistoryLines": 100,
      "includeEnvironment": true
    }
  },
  "layout": {
    "defaultTheme": "dark",
    "dockingBehavior": "float",
    "rememberLayout": true
  }
}
```

### 自定义命令快捷方式

```json
{
  "commands": [
    {
      "alias": "gst",
      "command": "git status",
      "description": "Show git status"
    },
    {
      "alias": "dcu",
      "command": "docker compose up -d",
      "description": "Start Docker containers"
    },
    {
      "alias": "k9s",
      "command": "k9s",
      "description": "Launch Kubernetes dashboard"
    }
  ]
}
```

## 🔌 扩展和插件

TermForge 支持通过插件系统扩展功能：

```powershell
# 安装插件
termforge plugin install git-enhanced
termforge plugin install docker-tools
termforge plugin install k8s-helper

# 列出已安装插件
termforge plugin list

# 创建自定义插件
termforge plugin create --template agent-custom
```

## 📚 使用案例

### 场景 1：快速项目初始化
```powershell
# 使用 Agent 创建项目结构
termforge agent "初始化一个 Node.js + Express + TypeScript 项目"

# Agent 自动执行：
# - npm init -y
# - npm install express typescript ts-node
# - 生成 tsconfig.json
# - 创建项目目录结构
```

### 场景 2：跨平台开发
```powershell
# 在同一工作区中并行运行 Windows 和 Linux 任务
# PowerShell 窗格：运行 .NET 构建
# WSL 窗格：运行 Docker 容器
# 两个环境共享项目文件系统
```

### 场景 3：Kubernetes 运维
```powershell
termforge agent "查看集群中所有失败的 Pod 并显示日志"
# Agent 自动执行 kubectl 命令并格式化输出

termforge agent "滚动更新 my-app 部署并等待完成"
# Agent 理解 K8s 操作并安全执行
```

### 场景 4：AI 辅助脚本开发
```powershell
termforge agent "写一个 PowerShell 脚本，备份 SQL Server 数据库到 Azure"
# Agent 生成脚本 → 展示预览 → 用户确认 → 执行
```

## 🔐 安全特性

- **环境变量隔离** – 不同工作区可以有独立的环境变量
- **命令审计** – 记录所有执行的命令和输出
- **Agent 权限控制** – 限制 Agent 可以执行的操作范围
- **密钥管理** – 安全存储 API 密钥和凭证

## 🐛 故障排除

### Agent 响应缓慢
```powershell
termforge config set agent.timeout 30  # 增加超时时间
termforge config set agent.model "claude-3-haiku"  # 切换更快的模型
```

### WSL 集成问题
```powershell
# 检查 WSL 状态
termforge diagnose --wsl

# 重新初始化 WSL 集成
termforge wsl --reinitialize
```

### 显示问题
```powershell
# 更新终端渲染器
termforge update --renderer

# 重置主题为默认值
termforge theme --reset
```

## 📖 文档

- [完整用户指南](./docs/user-guide.md)
- [Agent CLI 文档](./docs/agent-cli.md)
- [插件开发指南](./docs/plugin-development.md)
- [配置参考](./docs/configuration-reference.md)
- [快捷键自定义](./docs/keybindings.md)

## 🤝 贡献

欢迎提交 Issue、Pull Request 和功能建议！

```bash
git clone https://github.com/Bugaoxingxx/TermForge.git
cd TermForge
git checkout -b feature/your-feature
# ... 开发 ...
git push origin feature/your-feature
```

## 📄 许可证

MIT License – 详见 [LICENSE](LICENSE) 文件

## 🙋 反馈与支持

- **GitHub Issues** – 报告 Bug 和请求功能
- **Discussions** – 讨论设计和最佳实践
- **Discord Community** – [加入社区](https://discord.gg/termforge)

---

**TermForge** – 让终端成为你的锻造台 🔨⚡
