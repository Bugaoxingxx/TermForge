# 最近三次提交代码审阅

审阅日期：2026-09-06  
审阅范围：`4f5e3d0`（Windows 7 Aero UI 设计规范与原型）、`c9d67f7`（MDI 拖动和缩放的物理鼠标测试）、`08ebfcb`（Windows 7 Aero 主题与 MDI 自动化支持）  
审阅方式：审查 `HEAD~3..HEAD` 差异，并执行相关 UI 自动化测试。

## 结论

**建议：请求修改（Request changes）**

Aero 主题和 MDI 自动化支持已能构建，新增的交互测试在当前环境中也能通过。但输入模拟器会在物理输入成功后重复发送 Win32 回退消息，导致测试一次操作实际注入两次手势；同时，边界钳制测试没有验证窗口位置，无法捕获窗口移出工作区的回归。

## 发现

### [P1] 物理拖动成功后仍执行 Win32 回退拖动

- 位置：`tests/AgentTerminal.UITests/Infrastructure/InputSimulator.cs:55-71`
- 问题：`FlaUI.Core.Input.Mouse` 的拖动操作在 `try` 块成功后没有 `return`，代码仍继续向顶层窗口发送 `WM_LBUTTONDOWN`、`WM_MOUSEMOVE` 和 `WM_LBUTTONUP`。
- 影响：每次测试会注入两次拖动。第二次操作仍使用初始坐标，而窗口可能已移动，因而可能命中工作区或其他控件；测试结果将依赖窗口布局与桌面环境，且不再代表一次真实用户操作。
- 建议：物理输入成功后直接返回。若需要处理 `SendInput` 静默失效的场景，应先验证预期的窗口位置变化，再有条件地执行回退，而不是无条件注入第二次手势。

### [P1] 物理双击成功后仍执行 Win32 回退双击

- 位置：`tests/AgentTerminal.UITests/Infrastructure/InputSimulator.cs:104-114`
- 问题：`DoubleClick` 与拖动路径相同：物理双击成功后仍发送 `WM_LBUTTONDOWN`、`WM_LBUTTONDBLCLK` 和 `WM_LBUTTONUP`。
- 影响：第一次双击可能已经触发最大化；随后按旧坐标发送的第二次双击会落在状态变化后的不同元素上，造成与环境相关的假阳性或额外副作用。
- 建议：在物理双击成功后返回；回退路径只在物理输入失败，或经状态验证确认未生效时执行。

### [P2] 边界钳制测试未验证窗口仍位于工作区内

- 位置：`tests/AgentTerminal.UITests/Specs/MdiWorkbenchLayoutTests.cs:197-202`
- 问题：`DragChildWindow_BeyondLeftAndTop_ShouldClampWithinWorkspace` 仅检查窗口宽高是否大于最小值，没有检查拖动后的 `Left`、`Top` 或子窗口与工作区的相对边界。
- 影响：即使钳制逻辑失效、窗口完全被拖出工作区，该测试依然会通过，无法保护本次提交要验证的边界行为。
- 建议：获取工作区与子窗口的屏幕矩形，断言子窗口左、上边界不小于工作区左、上边界；同时增加右、下边界场景，并按产品规则验证至少保留的可见区域。

## 验证

- `dotnet test tests/AgentTerminal.UITests/AgentTerminal.UITests.csproj --no-build --filter 'FullyQualifiedName~DoubleClickTitleBar_ShouldToggleMaximizeRestore'`：通过（1/1）。
- `dotnet test tests/AgentTerminal.UITests/AgentTerminal.UITests.csproj --no-build --filter 'FullyQualifiedName~DragChildWindow_BeyondLeftAndTop_ShouldClampWithinWorkspace|FullyQualifiedName~DragWindowBorder_ShouldResizeWindowDimensions'`：通过（2/2）。
- `dotnet test AgentTerminal.sln --no-restore`：未完全通过。既有的 `AgentTerminal.Tests.ConPty.PowerShellSessionIntegrationTests.AC03_Interrupt_ShouldCancelLongCommandAndAllowNextCommand` 失败；该测试文件不在本次三次提交的改动范围内。
- `git diff HEAD~3..HEAD --check`：发现文档和界面文件中的尾随空白；未作为审阅缺陷计入。

## 审阅备注

本次只写入审阅意见，未修改产品代码或测试实现。
