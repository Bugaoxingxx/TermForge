using System;
using System.Threading;
using System.Windows;
using Xunit;

namespace AgentTerminal.Tests.Themes;

public class ThemeResourceSmokeTests
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
    public void CriticalThemeResources_ShouldAllResolveSuccessfully()
    {
        RunInSta(() =>
        {
            if (Application.Current == null)
            {
                _ = new Application();
            }

            var dict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/AgentTerminal.App;component/Themes/AeroTheme.xaml", UriKind.Absolute)
            };

            string[] requiredKeys =
            [
                // Colors & Brushes
                "AeroGlassBrush",
                "AeroActiveGlassBrush",
                "AeroInactiveGlassBrush",
                "AeroGlassSheenBrush",
                "AeroBorderHighlightBrush",
                "AeroBorderOuterBrush",
                "AeroMenuBarBrush",
                "AeroToolBarBrush",
                "AeroStatusBarBrush",
                "AeroWorkspaceBackgroundBrush",
                "AeroPanelBackgroundBrush",
                "AeroTextDarkBrush",
                "AeroTextNormalBrush",
                "AeroTextMutedBrush",
                "AeroFocusBorderBrush",
                "AeroSelectionHighlightBrush",
                "AeroTitleGlowEffect",

                // Typography & Metrics
                "AeroFontFamily",
                "AeroCodeFontFamily",
                "AeroFontSizeTitle",
                "AeroFontSizeNormal",
                "AeroFontSizeSmall",
                "AeroCommandButtonHeight",
                "AeroCaptionButtonHeight",
                "AeroCaptionButtonWidth",
                "AeroCloseButtonWidth",
                "AeroMarginTight",
                "AeroMarginMedium",
                "AeroMarginLarge",
                "AeroPaddingTight",
                "AeroPaddingNormal",

                // Icons & Geometries
                "AeroMinimizeGeometry",
                "AeroMaximizeGeometry",
                "AeroRestoreGeometry",
                "AeroCloseGeometry",
                "NewTerminalIcon",
                "StopSessionIcon",
                "CascadeIcon",
                "TileHorizontalIcon",
                "TileVerticalIcon",
                "RestoreAllIcon",
                "ClearOutputIcon",
                "DiagnosticIcon",
                "SessionIcon",
                "ProfileIcon",

                // Control Styles
                "AeroTitleTextBlockStyle",
                "AeroInactiveTitleTextBlockStyle",
                "AeroToolBarButtonStyle",
                "AeroToolBarSeparatorStyle",
                "AeroCaptionButtonStyle",
                "AeroCloseButtonStyle",
                "AeroTreeViewItemStyle",

                // Window Chrome
                "AeroMainWindowChromeStyle"
            ];

            foreach (var key in requiredKeys)
            {
                Assert.True(dict.Contains(key), $"Critical theme resource key '{key}' was not found in AeroTheme.xaml hierarchy.");
                var resource = dict[key];
                Assert.NotNull(resource);
            }
        });
    }

    [Fact]
    public void AeroDockingThemeResources_ShouldAllResolveSuccessfully()
    {
        RunInSta(() =>
        {
            if (Application.Current == null)
            {
                _ = new Application();
            }

            var dict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/AgentTerminal.Docking;component/Themes/AeroDockingTheme.xaml", UriKind.Absolute)
            };

            string[] requiredKeys =
            [
                "MdiActiveGlassBrush",
                "MdiInactiveGlassBrush",
                "MdiSheenBrush",
                "MdiActiveBorderBrush",
                "MdiInactiveBorderBrush",
                "MdiHeaderSeparatorBrush",
                "MdiViewportBackgroundBrush",
                "MdiWorkspaceBackgroundBrush",
                "MdiTrayBackgroundBrush",
                "MdiTrayBorderBrush",
                "MdiTaskButtonBackgroundBrush",
                "MdiTaskButtonBorderBrush",
                "MdiTitleGlowEffect",
                "MdiInactiveTitleGlowEffect",
                "MdiActiveShadowEffect",
                "MdiInactiveShadowEffect",
                "MdiMinimizeGeometry",
                "MdiMaximizeGeometry",
                "MdiRestoreGeometry",
                "MdiCloseGeometry"
            ];

            foreach (var key in requiredKeys)
            {
                Assert.True(dict.Contains(key), $"MDI Docking theme resource key '{key}' was not found in AeroDockingTheme.xaml.");
                var resource = dict[key];
                Assert.NotNull(resource);
            }
        });
    }

    [Fact]
    public void HighContrastResources_ShouldAllResolveAndDisableEffects()
    {
        RunInSta(() =>
        {
            if (Application.Current == null)
            {
                _ = new Application();
            }

            var dict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/AgentTerminal.App;component/Themes/HighContrast.xaml", UriKind.Absolute)
            };

            string[] requiredKeys =
            [
                "AeroGlassBrush",
                "AeroActiveGlassBrush",
                "AeroInactiveGlassBrush",
                "AeroTextDarkBrush",
                "AeroTextNormalBrush",
                "AeroTextMutedBrush",
                "AeroWorkspaceBackgroundBrush",
                "AeroPanelBackgroundBrush",
                "MdiActiveGlassBrush",
                "MdiInactiveGlassBrush",
                "MdiViewportBackgroundBrush",
                "MdiWorkspaceBackgroundBrush",
                "MdiTrayBackgroundBrush",
                "MdiTaskButtonBackgroundBrush"
            ];

            foreach (var key in requiredKeys)
            {
                Assert.True(dict.Contains(key), $"High Contrast theme resource key '{key}' was not found in HighContrast.xaml.");
                var resource = dict[key];
                Assert.NotNull(resource);
            }

            // Verify effects are disabled in high contrast mode
            var mdiShadow = dict["MdiActiveShadowEffect"] as System.Windows.Media.Effects.DropShadowEffect;
            Assert.NotNull(mdiShadow);
            Assert.Equal(0, mdiShadow.Opacity);

            var aeroGlow = dict["AeroTitleGlowEffect"] as System.Windows.Media.Effects.DropShadowEffect;
            Assert.NotNull(aeroGlow);
            Assert.Equal(0, aeroGlow.Opacity);
        });
    }
}
