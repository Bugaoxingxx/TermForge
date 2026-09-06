using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;
using System.Text;

namespace AgentTerminal.Terminal.VT;

/// <summary>
/// VT / ANSI 转义序列解析状态机。
/// 支持 Ground、ESC、CSI、OSC、DCS 状态，跨分块保持状态，
/// 消费 C0 控制码、光标移动、清屏/清行、SGR 颜色与样式、DEC 私有模式及备用屏。
/// </summary>
public class VtParser
{
    private enum ParserState
    {
        Ground,
        Escape,
        CsiEntry,
        CsiParam,
        CsiIntermediate,
        OscString,
        DcsPassthrough
    }

    private ParserState _state = ParserState.Ground;
    private ITerminalBuffer? _buffer;

    // CSI 参数累加
    private readonly List<int?> _csiParams = new(16);
    private int _currentParam;
    private bool _hasParam;
    private char _privateModePrefix;

    // OSC 字符串累加
    private readonly StringBuilder _oscBuffer = new(256);
    private const int MaxOscLength = 2048;

    // 当前绘制样式
    private TerminalColor _foreground = TerminalColor.Default;
    private TerminalColor _background = TerminalColor.Default;
    private CellAttributes _attributes = CellAttributes.None;

    public TerminalColor CurrentForeground => _foreground;
    public TerminalColor CurrentBackground => _background;
    public CellAttributes CurrentAttributes => _attributes;

    public event EventHandler<string>? TitleChanged;

    public VtParser(ITerminalBuffer? buffer = null)
    {
        _buffer = buffer;
    }

    public void SetBuffer(ITerminalBuffer buffer)
    {
        _buffer = buffer;
    }

    public void Parse(ReadOnlySpan<char> input)
    {
        if (_buffer == null) return;
        Parse(input, _buffer);
    }

    public void Parse(ReadOnlySpan<char> input, ITerminalBuffer buffer)
    {
        _buffer = buffer;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            switch (_state)
            {
                case ParserState.Ground:
                    HandleGround(c, buffer);
                    break;

                case ParserState.Escape:
                    HandleEscape(c, buffer);
                    break;

                case ParserState.CsiEntry:
                case ParserState.CsiParam:
                    HandleCsi(c, buffer);
                    break;

                case ParserState.CsiIntermediate:
                    // 消费中间字符（0x20..0x2F），等待最终字符
                    if (c >= 0x40 && c <= 0x7E)
                    {
                        // 忽略未知带中间字符的 CSI 序列
                        _state = ParserState.Ground;
                    }
                    else if (c == '\x1B')
                    {
                        _state = ParserState.Escape;
                    }
                    break;

                case ParserState.OscString:
                    HandleOsc(c);
                    break;

                case ParserState.DcsPassthrough:
                    if (c == '\a')
                    {
                        _state = ParserState.Ground;
                    }
                    else if (c == '\x1B')
                    {
                        _state = ParserState.Escape;
                    }
                    break;
            }
        }
    }

    private void HandleGround(char c, ITerminalBuffer buffer)
    {
        switch (c)
        {
            case '\x1B': // ESC
                _state = ParserState.Escape;
                break;

            case '\r': // CR
                buffer.CarriageReturn();
                break;

            case '\n': // LF
            case '\v': // VT
            case '\f': // FF
                buffer.NewLine();
                break;

            case '\b': // BS
                buffer.Backspace();
                break;

            case '\t': // TAB
                buffer.Tab();
                break;

            case '\a': // BEL
            case '\0': // NUL
                // 忽略
                break;

            default:
                if (c >= 0x20 && c != 0x7F)
                {
                    buffer.WriteChar(c, _foreground, _background, _attributes);
                }
                break;
        }
    }

    private void HandleEscape(char c, ITerminalBuffer buffer)
    {
        switch (c)
        {
            case '[': // CSI
                _state = ParserState.CsiEntry;
                ResetCsi();
                break;

            case ']': // OSC
                _state = ParserState.OscString;
                _oscBuffer.Clear();
                break;

            case 'P': // DCS
                _state = ParserState.DcsPassthrough;
                break;

            case '\\': // ST (String Terminator)
                _state = ParserState.Ground;
                break;

            case 'c': // RIS (Reset to Initial State)
                ResetAll(buffer);
                _state = ParserState.Ground;
                break;

            case '7': // DECSC (Save Cursor)
                buffer.SaveCursor();
                _state = ParserState.Ground;
                break;

            case '8': // DECRC (Restore Cursor)
                buffer.RestoreCursor();
                _state = ParserState.Ground;
                break;

            case '\x1B': // 连续 ESC
                _state = ParserState.Escape;
                break;

            default:
                // 未识别的双字符转义序列，安全丢弃并返回 Ground
                _state = ParserState.Ground;
                break;
        }
    }

    private void HandleCsi(char c, ITerminalBuffer buffer)
    {
        if (_state == ParserState.CsiEntry && (c == '?' || c == '>' || c == '='))
        {
            _privateModePrefix = c;
            _state = ParserState.CsiParam;
            return;
        }

        _state = ParserState.CsiParam;

        if (c >= '0' && c <= '9')
        {
            _currentParam = Math.Min(_currentParam * 10 + (c - '0'), 100_000);
            _hasParam = true;
            return;
        }

        if (c == ';')
        {
            _csiParams.Add(_hasParam ? _currentParam : null);
            _currentParam = 0;
            _hasParam = false;
            return;
        }

        if (c >= 0x20 && c <= 0x2F)
        {
            _state = ParserState.CsiIntermediate;
            return;
        }

        if (c >= 0x40 && c <= 0x7E)
        {
            // 终止字符，执行 CSI 命令
            if (_hasParam)
            {
                _csiParams.Add(_currentParam);
            }
            else if (_csiParams.Count > 0)
            {
                _csiParams.Add(null);
            }

            ExecuteCsi(c, buffer);
            _state = ParserState.Ground;
            return;
        }

        if (c == '\x1B')
        {
            _state = ParserState.Escape;
            return;
        }

        // 其他未知控制字符，重置回 Ground
        _state = ParserState.Ground;
    }

    private void HandleOsc(char c)
    {
        if (c == '\a') // BEL 终止
        {
            ProcessOscString();
            _state = ParserState.Ground;
        }
        else if (c == '\x1B') // 可能接 '\\' 组成 ST
        {
            ProcessOscString();
            _state = ParserState.Escape;
        }
        else
        {
            if (_oscBuffer.Length < MaxOscLength)
            {
                _oscBuffer.Append(c);
            }
        }
    }

    private void ProcessOscString()
    {
        string osc = _oscBuffer.ToString();
        _oscBuffer.Clear();

        // 格式通常为: "0;title" 或 "2;title"
        int semicolon = osc.IndexOf(';');
        if (semicolon > 0 && int.TryParse(osc.AsSpan(0, semicolon), out int cmd))
        {
            if (cmd is 0 or 2)
            {
                string title = osc[(semicolon + 1)..];
                TitleChanged?.Invoke(this, title);
            }
        }
    }

    private void ExecuteCsi(char finalChar, ITerminalBuffer buffer)
    {
        if (_privateModePrefix == '?')
        {
            ExecuteDecPrivateMode(finalChar, buffer);
            return;
        }

        switch (finalChar)
        {
            case 'm': // SGR
                ExecuteSgr();
                break;

            case 'H': // CUP
            case 'f': // HVP
                int row = GetParam(0, 1) - 1;
                int col = GetParam(1, 1) - 1;
                buffer.SetCursorPosition(col, row);
                break;

            case 'A': // CUU (Cursor Up)
                buffer.MoveCursor(0, -GetParam(0, 1));
                break;

            case 'B': // CUD (Cursor Down)
                buffer.MoveCursor(0, GetParam(0, 1));
                break;

            case 'C': // CUF (Cursor Forward)
                buffer.MoveCursor(GetParam(0, 1), 0);
                break;

            case 'D': // CUB (Cursor Back)
                buffer.MoveCursor(-GetParam(0, 1), 0);
                break;

            case 'E': // CNL (Cursor Next Line)
                buffer.SetCursorPosition(0, buffer.CursorY + GetParam(0, 1));
                break;

            case 'F': // CPL (Cursor Previous Line)
                buffer.SetCursorPosition(0, buffer.CursorY - GetParam(0, 1));
                break;

            case 'G': // CHA (Cursor Horizontal Absolute)
                buffer.SetCursorPosition(GetParam(0, 1) - 1, buffer.CursorY);
                break;

            case 'd': // VPA (Line Position Absolute)
                buffer.SetCursorPosition(buffer.CursorX, GetParam(0, 1) - 1);
                break;

            case 'J': // ED (Erase in Display)
                buffer.EraseInDisplay(GetParam(0, 0));
                break;

            case 'K': // EL (Erase in Line)
                buffer.EraseInLine(GetParam(0, 0));
                break;

            case 'S': // SU (Scroll Up)
                buffer.ScrollUp(GetParam(0, 1));
                break;

            case 'T': // SD (Scroll Down)
                buffer.ScrollDown(GetParam(0, 1));
                break;

            case 'r': // DECSTBM (Set Scroll Region)
                int top = GetParam(0, 1) - 1;
                int bottom = GetParam(1, buffer.Dimensions.Rows) - 1;
                buffer.SetScrollRegion(top, bottom);
                buffer.SetCursorPosition(0, 0);
                break;

            case '@': // ICH (Insert Character)
                buffer.InsertCharacters(GetParam(0, 1));
                break;

            case 'P': // DCH (Delete Character)
                buffer.DeleteCharacters(GetParam(0, 1));
                break;

            case 'L': // IL (Insert Line)
                buffer.InsertLines(GetParam(0, 1));
                break;

            case 'M': // DL (Delete Line)
                buffer.DeleteLines(GetParam(0, 1));
                break;

            case 'X': // ECH (Erase Character)
                buffer.EraseCharacters(GetParam(0, 1));
                break;

            case 's': // SCP (Save Cursor Position)
                buffer.SaveCursor();
                break;

            case 'u': // RCP (Restore Cursor Position)
                buffer.RestoreCursor();
                break;
        }
    }

    private void ExecuteDecPrivateMode(char finalChar, ITerminalBuffer buffer)
    {
        int mode = GetParam(0, 0);
        bool enable = finalChar == 'h';

        switch (mode)
        {
            case 25: // 光标显隐
                buffer.IsCursorVisible = enable;
                break;

            case 47:
            case 1047:
            case 1049: // 备用屏切换
                if (enable)
                {
                    buffer.UseAlternateScreenBuffer();
                }
                else
                {
                    buffer.UseMainScreenBuffer();
                }
                break;
        }
    }

    private void ExecuteSgr()
    {
        if (_csiParams.Count == 0)
        {
            ResetSgr();
            return;
        }

        for (int i = 0; i < _csiParams.Count; i++)
        {
            int p = _csiParams[i] ?? 0;

            switch (p)
            {
                case 0:
                    ResetSgr();
                    break;
                case 1:
                    _attributes |= CellAttributes.Bold;
                    break;
                case 2:
                    _attributes |= CellAttributes.Dim;
                    break;
                case 3:
                    _attributes |= CellAttributes.Italic;
                    break;
                case 4:
                    _attributes |= CellAttributes.Underline;
                    break;
                case 5:
                    _attributes |= CellAttributes.Blink;
                    break;
                case 7:
                    _attributes |= CellAttributes.Inverse;
                    break;
                case 8:
                    _attributes |= CellAttributes.Hidden;
                    break;
                case 9:
                    _attributes |= CellAttributes.Strikethrough;
                    break;
                case 22:
                    _attributes &= ~(CellAttributes.Bold | CellAttributes.Dim);
                    break;
                case 23:
                    _attributes &= ~CellAttributes.Italic;
                    break;
                case 24:
                    _attributes &= ~CellAttributes.Underline;
                    break;
                case 25:
                    _attributes &= ~CellAttributes.Blink;
                    break;
                case 27:
                    _attributes &= ~CellAttributes.Inverse;
                    break;
                case 28:
                    _attributes &= ~CellAttributes.Hidden;
                    break;
                case 29:
                    _attributes &= ~CellAttributes.Strikethrough;
                    break;

                // 标准前景色 (30..37, 39, 90..97)
                case >= 30 and <= 37:
                    _foreground = TerminalColor.FromIndex((byte)(p - 30));
                    break;
                case 39:
                    _foreground = TerminalColor.Default;
                    break;
                case >= 90 and <= 97:
                    _foreground = TerminalColor.FromIndex((byte)(p - 90 + 8));
                    break;

                // 标准背景色 (40..47, 49, 100..107)
                case >= 40 and <= 47:
                    _background = TerminalColor.FromIndex((byte)(p - 40));
                    break;
                case 49:
                    _background = TerminalColor.Default;
                    break;
                case >= 100 and <= 107:
                    _background = TerminalColor.FromIndex((byte)(p - 100 + 8));
                    break;

                // 扩展前景色 (38;5;n / 38;2;r;g;b)
                case 38:
                    if (i + 1 < _csiParams.Count)
                    {
                        int mode = _csiParams[++i] ?? 0;
                        if (mode == 5 && i + 1 < _csiParams.Count) // 256 色
                        {
                            int index = _csiParams[++i] ?? 0;
                            _foreground = TerminalColor.FromIndex((byte)Math.Clamp(index, 0, 255));
                        }
                        else if (mode == 2 && i + 3 < _csiParams.Count) // TrueColor
                        {
                            byte r = (byte)Math.Clamp(_csiParams[++i] ?? 0, 0, 255);
                            byte g = (byte)Math.Clamp(_csiParams[++i] ?? 0, 0, 255);
                            byte b = (byte)Math.Clamp(_csiParams[++i] ?? 0, 0, 255);
                            _foreground = TerminalColor.FromRgb(r, g, b);
                        }
                    }
                    break;

                // 扩展背景色 (48;5;n / 48;2;r;g;b)
                case 48:
                    if (i + 1 < _csiParams.Count)
                    {
                        int mode = _csiParams[++i] ?? 0;
                        if (mode == 5 && i + 1 < _csiParams.Count) // 256 色
                        {
                            int index = _csiParams[++i] ?? 0;
                            _background = TerminalColor.FromIndex((byte)Math.Clamp(index, 0, 255));
                        }
                        else if (mode == 2 && i + 3 < _csiParams.Count) // TrueColor
                        {
                            byte r = (byte)Math.Clamp(_csiParams[++i] ?? 0, 0, 255);
                            byte g = (byte)Math.Clamp(_csiParams[++i] ?? 0, 0, 255);
                            byte b = (byte)Math.Clamp(_csiParams[++i] ?? 0, 0, 255);
                            _background = TerminalColor.FromRgb(r, g, b);
                        }
                    }
                    break;
            }
        }
    }

    private void ResetSgr()
    {
        _foreground = TerminalColor.Default;
        _background = TerminalColor.Default;
        _attributes = CellAttributes.None;
    }

    private void ResetAll(ITerminalBuffer buffer)
    {
        ResetSgr();
        buffer.Clear();
    }

    private int GetParam(int index, int defaultValue)
    {
        if (index < _csiParams.Count && _csiParams[index].HasValue)
        {
            return _csiParams[index]!.Value;
        }
        return defaultValue;
    }

    private void ResetCsi()
    {
        _csiParams.Clear();
        _currentParam = 0;
        _hasParam = false;
        _privateModePrefix = '\0';
    }
}
