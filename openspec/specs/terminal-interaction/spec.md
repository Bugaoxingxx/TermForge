# terminal-interaction Specification

## Purpose
TBD - created by archiving change add-terminal-rendering. Update Purpose after archive.
## Requirements
### Requirement: 键盘直连会话
获得焦点的 `TerminalControl` SHALL 将键盘输入经 `TerminalKeyMapper` 转换为 VT/ANSI 输入序列并直接写入会话，包括可打印字符、Enter、Backspace、Tab、方向键、功能键及 Ctrl 组合键。

#### Scenario: 方向键映射
- **WHEN** 焦点在终端控件时按下向上方向键
- **THEN** 会话收到 `ESC[A`（或对应应用光标键模式序列）

#### Scenario: Ctrl+C 中断
- **WHEN** 无选区状态下按下 Ctrl+C
- **THEN** 会话收到 0x03（ETX/中断），而非执行复制

### Requirement: 鼠标选区与剪贴板
终端 SHALL 支持鼠标拖拽选择文本区域，并支持复制选中文本与粘贴。有选区时 Ctrl+C SHALL 执行复制而非发送中断。

#### Scenario: 有选区时复制
- **WHEN** 存在鼠标选区并按下 Ctrl+C
- **THEN** 选中文本被复制到剪贴板，且不向会话发送中断字节

#### Scenario: 粘贴写入会话
- **WHEN** 触发粘贴且剪贴板含文本
- **THEN** 该文本被写入会话输入

### Requirement: 尺寸联动
终端控件像素尺寸变化 SHALL 换算为行列并调用会话 `ResizeAsync`，使 Shell 感知新尺寸；仅在会话运行中执行。

#### Scenario: 控件缩放触发 PTY resize
- **WHEN** 运行中的终端控件尺寸变化导致可容纳行列改变
- **THEN** 会话 `ResizeAsync(cols, rows)` 被调用，且缓冲区与 ConPTY 尺寸随之更新

### Requirement: 替换调试视图接入工作台
终端文档 SHALL 以 `TerminalControl` 为主视图呈现于 MDI 工作台，取代原 `TerminalDebugView` 的只读 TextBox 输出模式；原始调试输出模式 MAY 作为可选诊断视图保留。

#### Scenario: 文档默认呈现原生终端
- **WHEN** 新建一个终端文档并激活
- **THEN** 文档内容区呈现 `TerminalControl` 原生终端，而非只读调试 TextBox

#### Scenario: 关闭文档回收会话
- **WHEN** 关闭承载 `TerminalControl` 的终端文档
- **THEN** 关联会话被停止并释放，无残留进程

