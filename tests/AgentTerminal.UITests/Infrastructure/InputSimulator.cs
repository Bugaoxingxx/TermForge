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

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint WM_MOUSEMOVE = 0x0200;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP = 0x0202;
    private const uint WM_LBUTTONDBLCLK = 0x0203;
    private const int MK_LBUTTON = 0x0001;

    private static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private static Window? FindWindow(AutomationElement element)
    {
        var current = element;
        while (current != null)
        {
            if (current.ControlType == ControlType.Window)
            {
                return current.AsWindow();
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

        try
        {
            FlaUI.Core.Input.Mouse.Position = startScreen;
            var curPos = FlaUI.Core.Input.Mouse.Position;
            if (Math.Abs(curPos.X - startScreen.X) <= 5 && Math.Abs(curPos.Y - startScreen.Y) <= 5)
            {
                Thread.Sleep(30);
                FlaUI.Core.Input.Mouse.Down(FlaUI.Core.Input.MouseButton.Left);
                Thread.Sleep(30);
                FlaUI.Core.Input.Mouse.MoveTo(endScreen);
                Thread.Sleep(30);
                FlaUI.Core.Input.Mouse.Up(FlaUI.Core.Input.MouseButton.Left);
                Thread.Sleep(50);
                return;
            }
        }
        catch
        {
            // 物理鼠标调用失败，回退至 Win32 消息模拟
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

        try
        {
            FlaUI.Core.Input.Mouse.Position = screenPt;
            var curPos = FlaUI.Core.Input.Mouse.Position;
            if (Math.Abs(curPos.X - screenPt.X) <= 5 && Math.Abs(curPos.Y - screenPt.Y) <= 5)
            {
                FlaUI.Core.Input.Mouse.DoubleClick(screenPt);
                Thread.Sleep(100);
                return;
            }
        }
        catch
        {
            // 物理鼠标调用失败，回退至 Win32 消息模拟
        }

        var window = FindWindow(targetElement)
            ?? throw new InvalidOperationException("Could not find parent Window for target element");

        var hWnd = window.Properties.NativeWindowHandle.Value;
        var pt = new POINT { X = screenPt.X, Y = screenPt.Y };
        ScreenToClient(hWnd, ref pt);

        // 标准 Win32 双击序列：WM_LBUTTONDOWN -> UP -> WM_LBUTTONDBLCLK -> UP
        SendMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, MakeLParam(pt.X, pt.Y));
        SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, MakeLParam(pt.X, pt.Y));
        Thread.Sleep(30);
        SendMessage(hWnd, WM_LBUTTONDBLCLK, (IntPtr)MK_LBUTTON, MakeLParam(pt.X, pt.Y));
        SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, MakeLParam(pt.X, pt.Y));
        Thread.Sleep(150);
    }
}
