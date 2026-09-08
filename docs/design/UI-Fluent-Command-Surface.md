# TermForge — 命令面：细菜单栏

> 日期：2026-09-08  
> 状态：已实施  
> 外壳方向：[UI-Win11-Fluent-Migration.md](./UI-Win11-Fluent-Migration.md)  
> 交互原型：[win11_fluent_preview.html](./win11_fluent_preview.html)

## 1. 决策

命令面只保留 **22 DIP 细菜单栏**（文件 / 视图 / 窗口 / 帮助）。不再提供工具栏或 Command Bar：同一动作不出现两次。

## 2. 菜单信息架构

- **文件**：新建（子菜单：默认终端 `Ctrl+N` + 各 Profile）、停止会话、关闭当前文档 `Ctrl+F4`、退出 `Alt+F4`
- **视图**：左侧导航树、右侧属性面板、底部诊断日志（勾选）；分隔；原始输出诊断模式（勾选）；清空输出
- **窗口**：**排列**子菜单（层叠 / 水平平铺 / 垂直平铺 / 全部还原）；打开的文档列表（动态）
- **帮助**：关于 TermForge

不改 `MainWindowViewModel` / 文档命令实现，只改入口绑定。

## 3. AutomationId

| 入口 | Id |
| --- | --- |
| 菜单栏 | `MainMenu` |
| 文件 → 新建子菜单 | `Menu.File.New` |
| 默认终端 | `Menu.File.NewTerminal` |
| 排列子项 | `Menu.Window.Cascade` / `TileHorizontal` / `TileVertical` / `RestoreAll` |
| 诊断模式 | `Menu.View.ToggleDiagnostic` |

FlaUI 页面对象走菜单路径，不再依赖 `Toolbar.*`。

## 4. 非目标

- 不引入 Ribbon，不把菜单栏去掉。
- 不改 MDI 层叠/平铺算法。
