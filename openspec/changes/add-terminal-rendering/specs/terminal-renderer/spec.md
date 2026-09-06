## ADDED Requirements

### Requirement: 字符网格渲染
`TerminalControl` SHALL 基于 WPF `DrawingContext` 绘制终端缓冲区的单元格网格，使用等宽字体按行列栅格定位字符；SHALL NOT 为每个字符创建独立的 `TextBlock`/WPF 元素。

#### Scenario: 网格对齐绘制
- **WHEN** 缓冲区包含若干行文本且控件重绘
- **THEN** 每个单元格按 (列×字符宽, 行×行高) 栅格定位绘制，字符纵横对齐不折断

### Requirement: 颜色与属性呈现
渲染器 SHALL 呈现每个单元格的前景色、背景色与属性（粗体、下划线、反显）；反显时前景背景互换。

#### Scenario: 反显单元格
- **WHEN** 某单元格设置了反显属性，前景白背景黑
- **THEN** 该单元格绘制为前景黑、背景白

### Requirement: 光标呈现
渲染器 SHALL 在 `IsCursorVisible` 为真时于光标坐标处绘制光标块；为假时不绘制。

#### Scenario: 光标隐藏
- **WHEN** 缓冲区 `IsCursorVisible` 为假
- **THEN** 渲染输出中不出现光标标记

### Requirement: 批量刷新防雪崩
渲染刷新 SHALL 与 PTY 读取解耦，采用约 16ms 的批量合流刷新；高频输出下 SHALL NOT 每字符触发一次 UI 调度，且不随输出总量线性增长 Visual 数量。

#### Scenario: 高频输出不逐字符调度
- **WHEN** 短时间内到达大量输出分块
- **THEN** UI 在约 16ms 周期内合并刷新一次，而非每个字符/分块各触发一次 Dispatcher 调用

### Requirement: 视口尺寸与行列换算
渲染器 SHALL 根据控件像素尺寸与字体度量换算出可容纳的行列数，供尺寸联动使用。

#### Scenario: 像素尺寸换算行列
- **WHEN** 控件像素尺寸与等宽字体的字符宽/行高已知
- **THEN** 渲染器计算出的列数=floor(宽/字符宽)、行数=floor(高/行高)
