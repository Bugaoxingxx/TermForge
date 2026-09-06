using System.Windows.Input;

namespace AgentTerminal.Terminal.Input;

/// <summary>
/// 键盘事件到 VT / ANSI 输入序列映射器。
/// 支持可打印字符、Enter、Backspace、Tab、方向键、功能键及 Ctrl 组合键。
/// </summary>
public static class TerminalKeyMapper
{
    public static string? MapKeyToVtSequence(Key key, ModifierKeys modifiers)
    {
        bool hasCtrl = modifiers.HasFlag(ModifierKeys.Control);
        bool hasShift = modifiers.HasFlag(ModifierKeys.Shift);
        bool hasAlt = modifiers.HasFlag(ModifierKeys.Alt);

        // 1. 处理 Ctrl 字母组合 (Ctrl+A .. Ctrl+Z)
        if (hasCtrl && !hasAlt && key is >= Key.A and <= Key.Z)
        {
            char ctrlChar = (char)(key - Key.A + 1);
            return ctrlChar.ToString();
        }

        // Ctrl + @ / Space
        if (hasCtrl && !hasAlt && key is Key.Space)
        {
            return "\0";
        }

        // 2. 特殊键与控制键
        switch (key)
        {
            case Key.Return:
                return "\r";

            case Key.Back:
                return "\x7F";

            case Key.Tab:
                return hasShift ? "\x1B[Z" : "\t";

            case Key.Escape:
                return "\x1B";

            case Key.Up:
                return GetCursorSequence('A', hasShift, hasAlt, hasCtrl);

            case Key.Down:
                return GetCursorSequence('B', hasShift, hasAlt, hasCtrl);

            case Key.Right:
                return GetCursorSequence('C', hasShift, hasAlt, hasCtrl);

            case Key.Left:
                return GetCursorSequence('D', hasShift, hasAlt, hasCtrl);

            case Key.Home:
                return hasCtrl ? "\x1B[1;5H" : "\x1B[H";

            case Key.End:
                return hasCtrl ? "\x1B[1;5F" : "\x1B[F";

            case Key.PageUp:
                return hasCtrl ? "\x1B[5;5~" : "\x1B[5~";

            case Key.PageDown:
                return hasCtrl ? "\x1B[6;5~" : "\x1B[6~";

            case Key.Insert:
                return "\x1B[2~";

            case Key.Delete:
                return "\x1B[3~";

            // 功能键 F1..F12
            case Key.F1: return "\x1BOP";
            case Key.F2: return "\x1BOQ";
            case Key.F3: return "\x1BOR";
            case Key.F4: return "\x1BOS";
            case Key.F5: return "\x1B[15~";
            case Key.F6: return "\x1B[17~";
            case Key.F7: return "\x1B[18~";
            case Key.F8: return "\x1B[19~";
            case Key.F9: return "\x1B[20~";
            case Key.F10: return "\x1B[21~";
            case Key.F11: return "\x1B[23~";
            case Key.F12: return "\x1B[24~";

            default:
                return null;
        }
    }

    private static string GetCursorSequence(char direction, bool shift, bool alt, bool ctrl)
    {
        if (!shift && !alt && !ctrl)
        {
            return $"\x1B[{direction}";
        }

        int modParam = 1 + (shift ? 1 : 0) + (alt ? 2 : 0) + (ctrl ? 4 : 0);
        return $"\x1B[1;{modParam}{direction}";
    }
}
