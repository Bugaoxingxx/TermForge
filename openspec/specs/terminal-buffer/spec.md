# terminal-buffer Specification

## Purpose
TBD - created by archiving change add-terminal-rendering. Update Purpose after archive.
## Requirements
### Requirement: 单元格网格模型
终端缓冲区 SHALL 以二维单元格网格表示可见视口，每个单元格包含一个字符（支持 BMP 及组合宽字符占位）、前景色、背景色与文本属性（粗体、下划线、反显等）。缓冲区 SHALL 独立于 WPF，可在无 UI 环境下构造与断言。

#### Scenario: 写入字符填充单元格
- **WHEN** 向缓冲区在光标位置写入字符 'A' 并附带前景色红、粗体属性
- **THEN** 对应单元格的字符为 'A'，前景色为红，粗体标志为真，且光标列前移一格

#### Scenario: 行尾自动换行
- **WHEN** 光标位于最后一列且写入一个字符
- **THEN** 该字符写入当前行最后一列后，光标换行到下一行首列（若在视口底部则触发滚动）

### Requirement: 光标定位与边界约束
缓冲区 SHALL 提供以 0 为基的光标定位，坐标越界时按终端语义收敛到合法范围而非抛出。

#### Scenario: 越界定位收敛
- **WHEN** 请求将光标设置到超出当前列/行数的坐标
- **THEN** 光标被收敛到最大合法列/行，且不抛出异常

### Requirement: 滚动区与滚动
缓冲区 SHALL 支持在视口内向上滚动：当内容超出视口底部时，最顶部的视口行迁移进回滚历史，视口底部出现空行。

#### Scenario: 底部换行触发滚动
- **WHEN** 光标位于视口最后一行且发生换行
- **THEN** 顶部一行进入回滚历史，视口整体上移一行，底部新增空行，回滚历史行数加一

### Requirement: 回滚历史上限
缓冲区 SHALL 维护有上限的回滚历史（默认 20000 行，最大 100000 行）；超过上限时按 FIFO 丢弃最旧行。

#### Scenario: 达到上限后淘汰最旧行
- **WHEN** 回滚历史已达 `MaxScrollbackLines` 且再有一行滚出视口
- **THEN** 最旧一行被丢弃，回滚历史行数保持等于上限

### Requirement: 主屏与备用屏
缓冲区 SHALL 支持主屏（primary）与备用屏（alternate）两套视口；切换到备用屏时不影响主屏内容与回滚历史，切回主屏时原内容完整恢复。

#### Scenario: 备用屏切换隔离
- **WHEN** 从主屏切换到备用屏、写入内容、再切回主屏
- **THEN** 主屏内容与回滚历史与切换前一致，备用屏写入不进入主屏回滚历史

### Requirement: 尺寸调整保留内容
缓冲区 `Resize` SHALL 调整行列并将光标收敛到新边界内，尽力保留既有可见内容。

#### Scenario: 缩小列数收敛光标
- **WHEN** 缓冲区列数由 120 调整为 80 且光标原列为 100
- **THEN** 光标列被收敛到 79（新最大合法列），既有单元格内容不因调整而损坏

