using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace AgentTerminal.UITests.Infrastructure;

public static class InputSimulator
{
    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const int MK_LBUTTON = 0x0001;

    private static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private static Window? FindWindow(AutomationElement element)
    {
        var current = element;
        while (current != null)
        {
            if (current.ControlType == ControlType.Window)
            {
                try
                {
                    if (current.Properties.NativeWindowHandle.IsSupported)
                    {
                        var handle = current.Properties.NativeWindowHandle.ValueOrDefault;
                        if (handle != IntPtr.Zero)
                        {
                            return current.AsWindow();
                        }
                    }
                }
                catch
                {
                    // 若当前 Window 节点（如 MdiChildWindow）无 HWND，继续向上查找到顶层原生 Window
                }
            }
            current = current.Parent;
        }
        return null;
    }

    public static void Drag(AutomationElement targetElement, int deltaX, int deltaY)
    {
        var rect = targetElement.BoundingRectangle;
        var startX = rect.Width > 80 ? rect.Left + 50 : rect.Left + rect.Width / 2;
        var startY = rect.Top + rect.Height / 2;
        var startScreen = new Point(startX, startY);
        var endScreen = new Point(startScreen.X + deltaX, startScreen.Y + deltaY);

        var initialBounds = targetElement.BoundingRectangle;

        try
        {
            // 1. 优先使用标准 OS SendInput 物理鼠标驱动（在交互式真实桌面下生效）
            FlaUI.Core.Input.Mouse.Position = startScreen;
            Thread.Sleep(30);
            FlaUI.Core.Input.Mouse.Down(FlaUI.Core.Input.MouseButton.Left);
            Thread.Sleep(30);
            FlaUI.Core.Input.Mouse.MoveTo(endScreen);
            Thread.Sleep(30);
            FlaUI.Core.Input.Mouse.Up(FlaUI.Core.Input.MouseButton.Left);
            Thread.Sleep(80);

            // 成功判定：
            // a) 若元素实际发生位移，或调用方请求位移为 0，物理拖拽确定生效
            var currentBounds = targetElement.BoundingRectangle;
            if (currentBounds.Left != initialBounds.Left || currentBounds.Top != initialBounds.Top || (deltaX == 0 && deltaY == 0))
            {
                return;
            }

            // b) 若元素未位移（例如在边界处触发了产品合法钳制），检查光标是否成功到达终点附近。
            //    若光标实际到达终点，说明物理输入已完整派发且生效，避免因边界钳制误判为失败而触发 Win32 二次注入
            if (GetCursorPos(out POINT curPos))
            {
                int distSq = (curPos.X - endScreen.X) * (curPos.X - endScreen.X) + (curPos.Y - endScreen.Y) * (curPos.Y - endScreen.Y);
                if (distSq <= 100) // 10px 容差
                {
                    return;
                }
            }
        }
        catch
        {
            // 物理鼠标调用抛出异常（如 CI / 锁屏无交互权限），回退至 Win32 消息模拟
        }

        var window = FindWindow(targetElement)
            ?? throw new InvalidOperationException("Could not find parent Window for target element");

        var hWnd = window.Properties.NativeWindowHandle.Value;
        var ptStart = new POINT { X = startScreen.X, Y = startScreen.Y };
        ScreenToClient(hWnd, ref ptStart);

        var ptEnd = new POINT { X = endScreen.X, Y = endScreen.Y };
        ScreenToClient(hWnd, ref ptEnd);

        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, MakeLParam(ptStart.X, ptStart.Y));
        Thread.Sleep(30);

        int steps = 10;
        for (int i = 1; i <= steps; i++)
        {
            int curX = ptStart.X + (ptEnd.X - ptStart.X) * i / steps;
            int curY = ptStart.Y + (ptEnd.Y - ptStart.Y) * i / steps;
            SendMessage(hWnd, WM_MOUSEMOVE, (IntPtr)MK_LBUTTON, MakeLParam(curX, curY));
            Thread.Sleep(15);
        }

        SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, MakeLParam(ptEnd.X, ptEnd.Y));
        Thread.Sleep(100);
    }

    public static void DoubleClick(AutomationElement targetElement)
    {
        var rect = targetElement.BoundingRectangle;
        var startX = rect.Width > 80 ? rect.Left + 50 : rect.Left + rect.Width / 2;
        var startY = rect.Top + rect.Height / 2;
        var screenPt = new Point(startX, startY);

        var window = FindWindow(targetElement);
        var initialBounds = window?.BoundingRectangle ?? targetElement.BoundingRectangle;

        try
        {
            FlaUI.Core.Input.Mouse.Position = screenPt;
            Thread.Sleep(30);
            FlaUI.Core.Input.Mouse.DoubleClick(screenPt);
            Thread.Sleep(120);

            // 成功判定：
            // a) 若宿主窗口或目标元素尺寸/位置发生改变（如最大化/还原切换），物理双击生效
            var currentBounds = window?.BoundingRectangle ?? targetElement.BoundingRectangle;
            bool hasChanged = currentBounds.Width != initialBounds.Width
                || currentBounds.Height != initialBounds.Height
                || currentBounds.Left != initialBounds.Left
                || currentBounds.Top != initialBounds.Top;
            if (hasChanged)
            {
                return;
            }

            // b) 若状态未变（如窗口本来就不可最大化/或已处于目标状态），检查光标是否在目标点附近。
            //    若光标成功落位且物理双击未抛异常，表明输入已完整注入，避免误回退产生二次点击
            if (GetCursorPos(out POINT curPos))
            {
                int distSq = (curPos.X - screenPt.X) * (curPos.X - screenPt.X) + (curPos.Y - screenPt.Y) * (curPos.Y - screenPt.Y);
                if (distSq <= 100)
                {
                    return;
                }
            }
        }
        catch
        {
            // 物理鼠标调用抛出异常，回退至 Win32 消息模拟
        }

        if (window == null)
            throw new InvalidOperationException("Could not find parent Window for target element");

        var hWnd = window.Properties.NativeWindowHandle.Value;
        var pt = new POINT { X = screenPt.X, Y = screenPt.Y };
        ScreenToClient(hWnd, ref pt);

        // WPF 根据双击间隔内的两次 WM_LBUTTONDOWN 识别 ClickCount = 2（WPF 窗口无 CS_DBLCLKS，不接收 WM_LBUTTONDBLCLK）
        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, MakeLParam(pt.X, pt.Y));
        SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, MakeLParam(pt.X, pt.Y));
        Thread.Sleep(40);
        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, MakeLParam(pt.X, pt.Y));
        SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, MakeLParam(pt.X, pt.Y));
        Thread.Sleep(150);
    }
}
