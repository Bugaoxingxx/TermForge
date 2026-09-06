namespace AgentTerminal.Core.Models;

/// <summary>
/// 终端单元格样式属性标志位
/// </summary>
[Flags]
public enum CellAttributes : ushort
{
    None = 0,
    Bold = 1 << 0,
    Dim = 1 << 1,
    Italic = 1 << 2,
    Underline = 1 << 3,
    Blink = 1 << 4,
    Inverse = 1 << 5,          // 反显（前景背景互换）
    Hidden = 1 << 6,           // 隐藏文本
    Strikethrough = 1 << 7,    // 删除线
    WideLeading = 1 << 8,      // 全角字符首列
    WideContinuation = 1 << 9  // 全角字符后继占位列
}
