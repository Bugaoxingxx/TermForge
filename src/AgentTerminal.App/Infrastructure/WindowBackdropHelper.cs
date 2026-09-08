using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace AgentTerminal.App.Infrastructure;

/// <summary>
/// Windows 11 DWM 系统 Backdrop (Mica / Acrylic) 与深浅色沉浸式标题栏辅助类。
/// 在 Windows 11 22000+ 启用系统圆角与沉浸式暗色，在 22621+ (22H2+) 启用原生 Mica 材质，
/// 低版本或调用失败时优雅回退纯色，杜绝黑边与资源泄漏。
/// </summary>
public static class WindowBackdropHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMWCP_ROUND = 2;
    private const int DWMSBT_MAINWINDOW = 2; // Mica

    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MARGINS(int left, int right, int top, int bottom)
    {
        public int cxLeftWidth = left;
        public int cxRightWidth = right;
        public int cyTopHeight = top;
        public int cyBottomHeight = bottom;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttributeId, ref int pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

    /// <summary>
    /// 检测当前系统是否为 Windows 11 初始版本 (Build 22000) 或更高版本。
    /// 此版本起支持 DWM 圆角与沉浸式暗色属性。
    /// </summary>
    public static bool IsWindows11OrNewer()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT &&
               Environment.OSVersion.Version.Major >= 10 &&
               Environment.OSVersion.Version.Build >= 22000;
    }

    /// <summary>
    /// 检测当前系统是否为 Windows 11 22621 (22H2) 或更高版本。
    /// 此版本起正式支持 DWMWA_SYSTEMBACKDROP_TYPE 原生 Mica 材质 API。
    /// </summary>
    public static bool IsWindows11_22H2OrNewer()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT &&
               Environment.OSVersion.Version.Major >= 10 &&
               Environment.OSVersion.Version.Build >= 22621;
    }

    /// <summary>
    /// 获取当前 Windows 系统的强调色 (Accent Color)。
    /// 优先从 Windows 10/11 注册表 AccentColor 获取，降级至 DwmGetColorizationColor 或默认 Fluent 蓝。
    /// </summary>
    public static Color GetAccentColor()
    {
        // 1. 优先读取 Windows 10/11 个性化设置强调色 (注册表 DWM\AccentColor)
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (key?.GetValue("AccentColor") is int accent && accent != 0)
            {
                // 注册表 AccentColor 格式通常为 0xAABBGGRR
                byte r = (byte)(accent & 0xFF);
                byte g = (byte)((accent >> 8) & 0xFF);
                byte b = (byte)((accent >> 16) & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch
        {
            // 注册表访问受限时降级
        }

        // 2. 降级读取 DWM 玻璃色彩
        try
        {
            if (DwmGetColorizationColor(out uint colorization, out _) == 0)
            {
                byte r = (byte)((colorization >> 16) & 0xFF);
                byte g = (byte)((colorization >> 8) & 0xFF);
                byte b = (byte)(colorization & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch
        {
            // DWM 调用失败时降级
        }

        // 3. 默认 Fluent 蓝
        return Color.FromRgb(0x00, 0x78, 0xD4);
    }

    /// <summary>
    /// 检测当前 Windows 用户首选应用主题是否为深色模式。
    /// </summary>
    public static bool IsDarkModePreferred()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int appsUseLightTheme)
            {
                return appsUseLightTheme == 0;
            }
        }
        catch
        {
            // 注册表读取受限时降级为依据窗口背景色亮度判断
        }

        var windowColor = SystemColors.WindowColor;
        var luminance = (0.299 * windowColor.R + 0.587 * windowColor.G + 0.114 * windowColor.B) / 255.0;
        return luminance < 0.5;
    }

    /// <summary>
    /// 为目标窗口配置 Windows 11 原生 Mica 背景材质与系统圆角。
    /// 若处于 Windows 10 或 DWM 材质不可用环境，自动应用纯色回退。
    /// </summary>
    /// <param name="window">目标 WPF 窗口</param>
    /// <returns>若成功应用系统 Backdrop 返回 true；若降级为纯色回退返回 false。</returns>
    public static bool ApplyBackdrop(Window window)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        if (hwnd == IntPtr.Zero)
        {
            ApplyFallbackSolidBackground(window);
            return false;
        }

        // 挂载 Win32 消息钩子以便实时响应系统模式与强调色变更；窗口关闭时自动卸载
        var source = HwndSource.FromHwnd(hwnd);
        if (source != null)
        {
            source.RemoveHook(WndProc); // 防止重复挂载
            source.AddHook(WndProc);

            window.Closed -= OnWindowClosed;
            window.Closed += OnWindowClosed;
        }

        if (!IsWindows11OrNewer())
        {
            ApplyFallbackSolidBackground(window);
            return false;
        }

        try
        {
            // 1. 设置系统圆角 (Win11 22000+)
            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            // 2. 设置沉浸式暗色模式 (Win11 22000+)
            UpdateDarkModeAttribute(hwnd);

            // 3. 设置 Mica 材质 (Win11 22621+ 22H2+)
            if (IsWindows11_22H2OrNewer())
            {
                int backdropType = DWMSBT_MAINWINDOW;
                int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

                if (hr == 0)
                {
                    var margins = new MARGINS(-1, -1, -1, -1);
                    _ = DwmExtendFrameIntoClientArea(hwnd, ref margins);

                    // Mica 生效：将窗口背景设为透明以令 DWM 材质透显
                    window.Background = Brushes.Transparent;
                    return true;
                }
            }
        }
        catch
        {
            // P/Invoke 失败时静默降级
        }

        ApplyFallbackSolidBackground(window);
        return false;
    }

    private static void OnWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.Closed -= OnWindowClosed;
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                var source = HwndSource.FromHwnd(hwnd);
                source?.RemoveHook(WndProc);
            }
        }
    }

    /// <summary>
    /// 更新窗口的沉浸式暗色模式属性
    /// </summary>
    public static void UpdateDarkModeAttribute(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindows11OrNewer())
        {
            return;
        }

        try
        {
            int darkMode = IsDarkModePreferred() ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        }
        catch
        {
            // 忽略非关键属性失败
        }
    }

    /// <summary>
    /// 获取深色或浅色模式对应的纯色回退画刷。
    /// </summary>
    public static SolidColorBrush GetFallbackSolidBackgroundBrush(bool isDark)
    {
        return isDark
            ? new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20))
            : new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    }

    /// <summary>
    /// 当 Mica 材质不可用时，应用优雅的纯色回退背景，避免黑色黑边。
    /// </summary>
    public static void ApplyFallbackSolidBackground(Window window)
    {
        bool isDark = IsDarkModePreferred();
        window.Background = GetFallbackSolidBackgroundBrush(isDark);
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SETTINGCHANGE || msg == WM_DWMCOLORIZATIONCOLORCHANGED)
        {
            UpdateDarkModeAttribute(hwnd);
            Application.Current?.Dispatcher?.BeginInvoke(() =>
            {
                if (Application.Current is App app)
                {
                    app.ApplyTheme();
                }
            });
        }
        return IntPtr.Zero;
    }
}
