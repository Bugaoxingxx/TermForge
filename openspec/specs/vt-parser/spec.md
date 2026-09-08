# vt-parser Specification

## Purpose
TBD - created by archiving change add-terminal-rendering. Update Purpose after archive.
## Requirements
### Requirement: VT 解析状态机
解析器 SHALL 实现一个覆盖 Ground / Escape / CSI / OSC / DCS 状态的状态机，逐字符消费 PTY 文本流，并将其翻译为对终端缓冲区的操作；解析 SHALL 是有状态的，能跨读取分块（chunk）保持中间状态而不丢失或错解序列。

#### Scenario: 转义序列跨分块保持状态
- **WHEN** 一个 CSI 序列 `ESC[31m` 被拆成两个分块 `ESC[3` 与 `1m` 先后送入解析器
- **THEN** 解析器正确识别为设置前景红色，而不会把任何残片当作可打印文本输出

### Requirement: 转义序列不作为文本渲染
解析器 SHALL 消费所有控制序列，普通可打印文本以外的 ESC/CSI/OSC/C0 控制字节不得出现在缓冲区单元格中。

#### Scenario: 不再出现裸控制码
- **WHEN** 输入包含 `ESC[?25l`、`ESC[2J`、`ESC]0;title\x07` 及可打印文本 `hello`
- **THEN** 缓冲区中仅出现 `hello` 对应的单元格，不出现 `[`, `?25l`, `]0;` 等序列残片

### Requirement: 光标移动与擦除
解析器 SHALL 支持常见 CSI 光标控制（CUP/HVP 定位、CUU/CUD/CUF/CUB 相对移动）与擦除（ED 清屏、EL 清行）序列。

#### Scenario: 光标定位
- **WHEN** 解析 `ESC[5;10H`
- **THEN** 光标被定位到第 5 行第 10 列（转换为 0 基后的 4,9）

#### Scenario: 清屏
- **WHEN** 解析 `ESC[2J`
- **THEN** 整个可见视口被清空为空白单元格

### Requirement: SGR 颜色与属性
解析器 SHALL 支持 SGR 序列，包括重置（0）、粗体/下划线/反显等属性、标准 16 色、256 色（`38;5;n` / `48;5;n`）与 TrueColor（`38;2;r;g;b` / `48;2;r;g;b`）。

#### Scenario: TrueColor 前景
- **WHEN** 解析 `ESC[38;2;10;20;30m` 后写入字符 'X'
- **THEN** 该字符单元格前景色为 RGB(10,20,30)

#### Scenario: SGR 重置
- **WHEN** 在已设置颜色/属性后解析 `ESC[0m`
- **THEN** 后续写入字符恢复为默认前景、默认背景、无附加属性

### Requirement: 私有模式与备用屏
解析器 SHALL 支持常见 DEC 私有模式，至少包括光标显隐（`?25h`/`?25l`）与备用屏缓冲（`?1049h`/`?1049l`）。

#### Scenario: 隐藏光标
- **WHEN** 解析 `ESC[?25l`
- **THEN** 缓冲区 `IsCursorVisible` 为假

#### Scenario: 进入备用屏
- **WHEN** 解析 `ESC[?1049h`
- **THEN** 缓冲区切换到备用屏，主屏内容与回滚历史被保留

