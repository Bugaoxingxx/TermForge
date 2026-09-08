using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace AgentTerminal.App.Infrastructure;

/// <summary>
/// Windows 11 DWM 系统 Backdrop (Mica / Acrylic) 与深浅色沉浸式标题栏辅助类。
/// 仅在 Windows 11 22621+ (22H2+) 启用原生 Mica 材质，低版本或调用失败时优雅回退纯色，杜绝黑边。
/// </summary>
public static class WindowBackdropHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    private const int DWMWCP_ROUND = 2;
    private const int DWMSBT_NONE = 1;
    private const int DWMSBT_MAINWINDOW = 2; // Mica
    private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic

    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
    private const int WM_GETMINMAXINFO = 0x0024;
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttributeId, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    /// <summary>
    /// 检测当前系统是否为 Windows 11 22621 (22H2) 或更高版本。
    /// </summary>
    public static bool IsWindows11_22H2OrNewer()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT &&
               Environment.OSVersion.Version.Major >= 10 &&
               Environment.OSVersion.Version.Build >= 22621;
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

        // 挂载 Win32 消息钩子以便实时响应系统模式与强调色变更
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        if (!IsWindows11_22H2OrNewer())
        {
            ApplyFallbackSolidBackground(window);
            return false;
        }

        try
        {
            // 1. 设置圆角
            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            // 2. 设置沉浸式暗色模式
            UpdateDarkModeAttribute(hwnd);

            // 3. 应用系统 Mica 材质 (DWMSBT_MAINWINDOW = 2)
            int backdropType = DWMSBT_MAINWINDOW;
            int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

            if (hr == 0)
            {
                // Mica 生效：将窗口背景设为透明以令 DWM 材质透显
                window.Background = Brushes.Transparent;
                return true;
            }
        }
        catch
        {
            // P/Invoke 失败时静默降级
        }

        ApplyFallbackSolidBackground(window);
        return false;
    }

    /// <summary>
    /// 更新窗口的沉浸式暗色模式属性
    /// </summary>
    public static void UpdateDarkModeAttribute(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindows11_22H2OrNewer())
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
    /// 当 Mica 材质不可用时，应用优雅的纯色回退背景，避免黑色黑边。
    /// </summary>
    public static void ApplyFallbackSolidBackground(Window window)
    {
        bool isDark = IsDarkModePreferred();
        window.Background = isDark
            ? new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20))
            : new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    }

    private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SETTINGCHANGE || msg == WM_DWMCOLORIZATIONCOLORCHANGED)
        {
            UpdateDarkModeAttribute(hwnd);
        }
        else if (msg == WM_GETMINMAXINFO)
        {
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hMonitor != IntPtr.Zero)
            {
                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    mmi.ptMaxPosition.x = Math.Abs(mi.rcWork.Left - mi.rcMonitor.Left);
                    mmi.ptMaxPosition.y = Math.Abs(mi.rcWork.Top - mi.rcMonitor.Top);
                    mmi.ptMaxSize.x = Math.Abs(mi.rcWork.Right - mi.rcWork.Left);
                    mmi.ptMaxSize.y = Math.Abs(mi.rcWork.Bottom - mi.rcWork.Top);
                    mmi.ptMaxTrackSize.x = mmi.ptMaxSize.x;
                    mmi.ptMaxTrackSize.y = mmi.ptMaxSize.y;
                    Marshal.StructureToPtr(mmi, lParam, true);
                    handled = true;
                }
            }
        }
        return IntPtr.Zero;
    }
}
