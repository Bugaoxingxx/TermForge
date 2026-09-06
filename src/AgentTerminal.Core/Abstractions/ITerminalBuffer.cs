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

    /// <summary>
    /// 多线程并发访问同步锁对象
    /// </summary>
    object SyncRoot { get; }

    /// <summary>
    /// 缓冲区内容或光标变更时触发的重绘请求事件
    /// </summary>
    event EventHandler? RefreshRequested;

    /// <summary>
    /// 请求触发重绘通知
    /// </summary>
    void RequestRefresh();

    /// <summary>
    /// 在当前光标处插入指定数量的空白字符（ICH），右侧字符向右顺移
    /// </summary>
    void InsertCharacters(int count = 1);

    /// <summary>
    /// 从当前光标处删除指定数量的字符（DCH），右侧字符向左顺移，行尾补空白
    /// </summary>
    void DeleteCharacters(int count = 1);

    /// <summary>
    /// 从当前光标处擦除指定数量的字符（ECH），替换为空白，光标位置不变
    /// </summary>
    void EraseCharacters(int count = 1);

    /// <summary>
    /// 在当前光标行插入指定数量的空白行（IL），下方行向下顺移
    /// </summary>
    void InsertLines(int count = 1);

    /// <summary>
    /// 从当前光标行删除指定数量的行（DL），下方行向上顺移，滚动区底部补空白行
    /// </summary>
    void DeleteLines(int count = 1);

    /// <summary>
    /// 保存当前光标位置（DECSC / SCP）
    /// </summary>
    void SaveCursor();

    /// <summary>
    /// 恢复之前保存的光标位置（DECRC / RCP）
    /// </summary>
    void RestoreCursor();
}
