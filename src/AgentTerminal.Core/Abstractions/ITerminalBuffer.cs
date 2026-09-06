using AgentTerminal.Core.Models;

namespace AgentTerminal.Core.Abstractions;

/// <summary>
/// 终端缓冲区抽象，解耦 PTY 数据流与 WPF 渲染呈现
/// </summary>
public interface ITerminalBuffer
{
    /// <summary>当前可见视口尺寸</summary>
    TerminalDimensions Dimensions { get; }

    /// <summary>当前光标列坐标（0-indexed）</summary>
    int CursorX { get; }

    /// <summary>当前光标行坐标（0-indexed）</summary>
    int CursorY { get; }

    /// <summary>光标是否可见</summary>
    bool IsCursorVisible { get; set; }

    /// <summary>滚动回滚区行数上限</summary>
    int MaxScrollbackLines { get; }

    /// <summary>当前回滚区包含的历史总行数</summary>
    int ScrollbackLineCount { get; }

    /// <summary>总行数 = 视口行数 + 回滚历史行数</summary>
    int TotalLines { get; }

    /// <summary>当前是否处于备用屏模式</summary>
    bool IsAlternateScreen { get; }

    /// <summary>
    /// 读取指定单元格。
    /// row 在 [0, Rows - 1] 为视口可见行；row 为负数 [-1, -ScrollbackLineCount] 为回滚历史行（-1 为最近历史行）。
    /// </summary>
    Cell GetCell(int column, int row);

    /// <summary>
    /// 读取指定整行单元格至目标 Span。
    /// row 在 [0, Rows - 1] 为视口可见行；row 为负数 [-1, -ScrollbackLineCount] 为回滚历史行。
    /// </summary>
    void CopyRow(int row, Span<Cell> destination);

    /// <summary>
    /// 设置光标坐标（0-indexed，越界自动收敛至合法边界）
    /// </summary>
    void SetCursorPosition(int x, int y);

    /// <summary>
    /// 相对移动光标（越界自动收敛）
    /// </summary>
    void MoveCursor(int deltaX, int deltaY);

    /// <summary>
    /// 切换到备用屏缓冲（隔离主屏与回滚历史）
    /// </summary>
    void UseAlternateScreenBuffer();

    /// <summary>
    /// 切回主屏缓冲（恢复原内容）
    /// </summary>
    void UseMainScreenBuffer();

    /// <summary>
    /// 在当前光标处写入一个字符并应用样式，自动处理行尾折行或全角占位
    /// </summary>
    void WriteChar(char c, TerminalColor foreground, TerminalColor background, CellAttributes attributes);

    /// <summary>
    /// 执行回车（CR）：光标移至行首
    /// </summary>
    void CarriageReturn();

    /// <summary>
    /// 执行换行（LF）：光标移至下一行，超出视口/滚动区底部时向上滚动
    /// </summary>
    void NewLine();

    /// <summary>
    /// 执行退格（BS）：光标左移一格
    /// </summary>
    void Backspace();

    /// <summary>
    /// 执行制表（Tab）：光标前进到下一个制表停靠点（默认每 8 格）
    /// </summary>
    void Tab(int tabSize = 8);

    /// <summary>
    /// 擦除屏幕内容
    /// 0: 光标至屏尾, 1: 屏首至光标, 2: 清空整屏, 3: 清空整屏及回滚历史
    /// </summary>
    void EraseInDisplay(int mode);

    /// <summary>
    /// 擦除行内容
    /// 0: 光标至行尾, 1: 行首至光标, 2: 清空整行
    /// </summary>
    void EraseInLine(int mode);

    /// <summary>
    /// 视口向上滚动指定行数（最顶行进入回滚历史）
    /// </summary>
    void ScrollUp(int lines = 1);

    /// <summary>
    /// 视口向下滚动指定行数
    /// </summary>
    void ScrollDown(int lines = 1);

    /// <summary>
    /// 设置滚动区域边距（0-indexed）
    /// </summary>
    void SetScrollRegion(int topRow, int bottomRow);

    /// <summary>
    /// 重置滚动区域为整个视口
    /// </summary>
    void ResetScrollRegion();

    /// <summary>
    /// 调整缓冲区几何尺寸
    /// </summary>
    void Resize(int columns, int rows);

    /// <summary>
    /// 清空缓冲区内容与回滚历史
    /// </summary>
    void Clear();
}
