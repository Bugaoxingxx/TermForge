using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using AgentTerminal.App.Infrastructure;
using Xunit;

namespace AgentTerminal.Tests.Infrastructure;

public class WindowBackdropHelperTests
{
    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null)
        {
            throw new AggregateException(exception);
        }
    }

    [Fact]
    public void WindowsVersionChecks_ShouldFollowLogicalHierarchy()
    {
        bool isWin11_22H2 = WindowBackdropHelper.IsWindows11_22H2OrNewer();
        bool isWin11 = WindowBackdropHelper.IsWindows11OrNewer();

        if (isWin11_22H2)
        {
            Assert.True(isWin11, "Windows 11 22H2 or newer logically implies Windows 11 or newer.");
        }
    }

    [Fact]
    public void IsDarkModePreferred_ShouldExecuteWithoutThrowing()
    {
        var ex = Record.Exception(() => WindowBackdropHelper.IsDarkModePreferred());
        Assert.Null(ex);
    }

    [Fact]
    public void GetAccentColor_ShouldReturnValidOpaqueRgbColor()
    {
        Color accent = WindowBackdropHelper.GetAccentColor();
        Assert.Equal(255, accent.A);
    }

    [Fact]
    public void GetFallbackSolidBackgroundBrush_ShouldReturnDistinctBrushesForDarkAndLight()
    {
        var darkBrush = WindowBackdropHelper.GetFallbackSolidBackgroundBrush(true);
        var lightBrush = WindowBackdropHelper.GetFallbackSolidBackgroundBrush(false);

        Assert.NotNull(darkBrush);
        Assert.NotNull(lightBrush);
        Assert.NotEqual(darkBrush.Color, lightBrush.Color);

        // Dark brush should have lower luminance than light brush
        double darkLuminance = 0.299 * darkBrush.Color.R + 0.587 * darkBrush.Color.G + 0.114 * darkBrush.Color.B;
        double lightLuminance = 0.299 * lightBrush.Color.R + 0.587 * lightBrush.Color.G + 0.114 * lightBrush.Color.B;
        Assert.True(darkLuminance < lightLuminance);
    }

    [Fact]
    public void ApplyFallbackSolidBackground_ShouldApplyBrushToWindow()
    {
        RunInSta(() =>
        {
            var win = new Window();
            WindowBackdropHelper.ApplyFallbackSolidBackground(win);

            Assert.NotNull(win.Background);
            Assert.IsType<SolidColorBrush>(win.Background);
        });
    }

    [Fact]
    public void ApplyBackdrop_OnWindow_ShouldNotThrow()
    {
        RunInSta(() =>
        {
            var win = new Window
            {
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            // Call ApplyBackdrop multiple times to verify hook deduplication and safety
            bool result1 = WindowBackdropHelper.ApplyBackdrop(win);
            bool result2 = WindowBackdropHelper.ApplyBackdrop(win);

            Assert.Equal(result1, result2);
            Assert.NotNull(win.Background);

            // Close window to verify Closed hook cleanup
            win.Close();
        });
    }
}
