# 最近三次提交代码审阅

审阅日期：2026-09-04  
审阅范围：`1371d72`、`23abefa`、`55aa16f`

## 结论

**建议：请求修改（Request changes）**

`1371d72` 为合并提交，实际解决的差异是 README；`23abefa` 也仅修改 README。下面的问题均由 `55aa16f` 引入。

## 发现

### [P2] `TotalLines` 未反映回滚历史

- 位置：`src/AgentTerminal.Terminal/Buffer/TerminalBuffer.cs:21`
- 问题：接口将 `TotalLines` 定义为回滚区的历史总行数，但实现始终返回 `Dimensions.Rows`，即当前视口高度。`MaxScrollbackLines` 也未参与任何状态维护。
- 影响：滚动条和历史渲染等调用方会始终认为没有额外的回滚内容。
- 建议：实现行历史及其总数；如果该功能仍属于后续 Phase 2，则暂时不要对外暴露这一完成态契约。

### [P2] 终端尺寸缺少边界验证

- 位置：`src/AgentTerminal.Terminal/Buffer/TerminalBuffer.cs:23-31`
- 问题：构造函数和 `Resize` 接受零或负数的行列数。
- 影响：对象会进入无效状态，并在 WPF 布局、缓冲区分配或后续 ConPTY 调用时以更难定位的方式失败。
- 建议：在 `TerminalDimensions` 或两个入口统一验证正值，并限制到 ConPTY 支持的范围；对无效输入抛出 `ArgumentOutOfRangeException`。

### [P2] 配置写入可能损坏，读取路径无法恢复

- 位置：`src/AgentTerminal.Infrastructure/Configuration/ProfileManager.cs:62-75`
- 问题：`File.WriteAllText` 会直接覆盖目标文件；若写入时进程或系统中断，可能留下截断 JSON。读取路径未处理 `JsonException`，而合法的 JSON `null` 又会返回一个没有默认 Profile 的空管理器。
- 影响：一次异常写入后，下一次应用启动可能失败，或出现无任何 Shell/Agent 配置的不可用状态。
- 建议：先写入同目录临时文件并原子替换；读取失败时保留备份、记录错误并回退到默认配置；反序列化结果为空时同样加载默认配置。

### [P2] 确认 FluentAssertions 的商业使用许可

- 位置：`tests/AgentTerminal.Tests/AgentTerminal.Tests.csproj:15`
- 问题：实际测试运行已提示 Fluent Assertions 8.x 受 Xceed 许可约束，免费使用限于非商业场景。
- 影响：若项目用于商业开发、分发或内部商业用途，可能存在许可合规风险。
- 建议：在合并前确认并取得适当许可，或替换为许可兼容的断言库（或可合法使用的版本）。

## 验证

- `dotnet build AgentTerminal.sln -c Debug`：成功，0 警告、0 错误。
- `dotnet test tests/AgentTerminal.Tests/AgentTerminal.Tests.csproj -c Debug --no-build`：4/4 通过。

## 审阅备注

本次未修改产品代码或既有文件。工作区中已有未提交的 `README.md` 改动，审阅过程中未触及。
