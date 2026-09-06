namespace AgentTerminal.Core.Models;

/// <summary>
/// 终端颜色类型
/// </summary>
public enum TerminalColorType : byte
{
    Default = 0,
    Indexed = 1,
    Rgb = 2
}

/// <summary>
/// 紧凑 32 位打包的终端颜色（支持 Default、ANSI 16 色、256 色及 24 位 TrueColor）
/// </summary>
public readonly record struct TerminalColor : IEquatable<TerminalColor>
{
    // 打包布局：
    // Bits 0..7:   R 或 调色板索引
    // Bits 8..15:  G
    // Bits 16..23: B
    // Bits 24..31: Type (0=Default, 1=Indexed, 2=Rgb)
    public uint Value { get; }

    public TerminalColorType Type => (TerminalColorType)((Value >> 24) & 0xFF);
    public byte R => (byte)(Value & 0xFF);
    public byte G => (byte)((Value >> 8) & 0xFF);
    public byte B => (byte)((Value >> 16) & 0xFF);
    public byte Index => R;

    public bool IsDefault => Type == TerminalColorType.Default;
    public bool IsIndexed => Type == TerminalColorType.Indexed;
    public bool IsRgb => Type == TerminalColorType.Rgb;

    public TerminalColor(TerminalColorType type, byte rOrIndex, byte g, byte b)
    {
        Value = (uint)rOrIndex | ((uint)g << 8) | ((uint)b << 16) | ((uint)type << 24);
    }

    public static TerminalColor Default => new(TerminalColorType.Default, 0, 0, 0);

    public static TerminalColor FromIndex(byte index) => new(TerminalColorType.Indexed, index, 0, 0);

    public static TerminalColor FromRgb(byte r, byte g, byte b) => new(TerminalColorType.Rgb, r, g, b);

    public static TerminalColor From16Color(int ansiIndex) => FromIndex((byte)(ansiIndex & 0x0F));

    private static readonly (byte R, byte G, byte B)[] Standard16 =
    [
        (12, 12, 12),       // 0: Black
        (197, 15, 31),      // 1: Red
        (19, 161, 14),      // 2: Green
        (193, 156, 0),      // 3: Yellow
        (0, 55, 218),       // 4: Blue
        (136, 23, 152),     // 5: Magenta
        (58, 150, 221),     // 6: Cyan
        (204, 204, 204),    // 7: White / Light Gray
        (118, 118, 118),    // 8: Bright Black / Gray
        (231, 72, 86),      // 9: Bright Red
        (22, 198, 12),      // 10: Bright Green
        (249, 241, 165),    // 11: Bright Yellow
        (59, 120, 255),     // 12: Bright Blue
        (180, 0, 158),      // 13: Bright Magenta
        (97, 214, 214),     // 14: Bright Cyan
        (242, 242, 242)     // 15: Bright White
    ];

    public static readonly (byte R, byte G, byte B) DefaultForegroundRgb = (204, 204, 204);
    public static readonly (byte R, byte G, byte B) DefaultBackgroundRgb = (12, 12, 12);

    /// <summary>
    /// 解析为 (R, G, B) 像素值
    /// </summary>
    public (byte R, byte G, byte B) ToRgb(bool isForeground = true)
    {
        return Type switch
        {
            TerminalColorType.Rgb => (R, G, B),
            TerminalColorType.Indexed => ResolveIndexedRgb(Index),
            _ => isForeground ? DefaultForegroundRgb : DefaultBackgroundRgb
        };
    }

    private static (byte R, byte G, byte B) ResolveIndexedRgb(byte index)
    {
        if (index < 16)
        {
            return Standard16[index];
        }

        if (index <= 231)
        {
            int cube = index - 16;
            int rIdx = cube / 36;
            int gIdx = (cube % 36) / 6;
            int bIdx = cube % 6;
            byte r = (byte)(rIdx == 0 ? 0 : 55 + rIdx * 40);
            byte g = (byte)(gIdx == 0 ? 0 : 55 + gIdx * 40);
            byte b = (byte)(bIdx == 0 ? 0 : 55 + bIdx * 40);
            return (r, g, b);
        }

        // 232-255: 灰阶
        byte gray = (byte)(8 + (index - 232) * 10);
        return (gray, gray, gray);
    }
}
