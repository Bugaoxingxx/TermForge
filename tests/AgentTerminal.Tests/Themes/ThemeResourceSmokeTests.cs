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
                "FluentAccentBrush",
                "FluentWindowBackgroundBrush",
                "FluentSubtleBrush",
                "FluentCardBackgroundBrush",
                "FluentTextPrimaryBrush",
                "FluentTextSecondaryBrush",
                "FluentBorderBrush",
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
                "AeroMenuBarHeight",
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
                "AeroCompactMenuBarStyle",
                "AeroCompactMenuItemStyle",
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
                "FluentAccentBrush",
                "FluentWindowBackgroundBrush",
                "FluentCardBackgroundBrush",
                "FluentTextPrimaryBrush",
                "FluentBorderBrush",
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

            // Verify success and danger are distinct colors in high contrast mode
            var successBrush = dict["AeroTextSuccessBrush"] as System.Windows.Media.SolidColorBrush;
            var dangerBrush = dict["AeroTextDangerBrush"] as System.Windows.Media.SolidColorBrush;
            Assert.NotNull(successBrush);
            Assert.NotNull(dangerBrush);
            Assert.NotEqual(successBrush.Color, dangerBrush.Color);
        });
    }

    [Fact]
    public void FluentThemeTokens_ShouldSupportLightAndDarkSemantics()
    {
        RunInSta(() =>
        {
            if (Application.Current == null)
            {
                _ = new Application();
            }

            var lightDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/AgentTerminal.App;component/Themes/Colors.xaml", UriKind.Absolute)
            };

            var darkDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/AgentTerminal.App;component/Themes/DarkColors.xaml", UriKind.Absolute)
            };

            string[] semanticTokens =
            [
                "FluentAccentBrush",
                "FluentWindowBackgroundBrush",
                "FluentSubtleBrush",
                "FluentCardBackgroundBrush",
                "FluentTextPrimaryBrush",
                "FluentTextSecondaryBrush",
                "FluentBorderBrush",
                "AeroStatusBarBrush",
                "AeroPanelBackgroundBrush",
                "AeroPropertiesBackgroundBrush",
                "AeroDiagnosticsBackgroundBrush",
                "AeroSplitterBrush",
                "AeroTextDarkBrush",
                "AeroTextNormalBrush",
                "AeroTextMutedBrush",
                "AeroTextSuccessBrush",
                "AeroTextDangerBrush"
            ];

            foreach (var token in semanticTokens)
            {
                Assert.True(lightDict.Contains(token), $"Light dictionary missing semantic token '{token}'");
                Assert.True(darkDict.Contains(token), $"Dark dictionary missing semantic token '{token}'");
                Assert.NotNull(lightDict[token]);
                Assert.NotNull(darkDict[token]);
            }

            // Verify Light vs Dark contrast semantics
            var lightText = (System.Windows.Media.SolidColorBrush)lightDict["FluentTextPrimaryBrush"];
            var darkText = (System.Windows.Media.SolidColorBrush)darkDict["FluentTextPrimaryBrush"];
            double lightTextLum = 0.299 * lightText.Color.R + 0.587 * lightText.Color.G + 0.114 * lightText.Color.B;
            double darkTextLum = 0.299 * darkText.Color.R + 0.587 * darkText.Color.G + 0.114 * darkText.Color.B;
            Assert.True(lightTextLum < darkTextLum, "Text primary in light mode should be darker than in dark mode.");

            var lightCard = (System.Windows.Media.SolidColorBrush)lightDict["FluentCardBackgroundBrush"];
            var darkCard = (System.Windows.Media.SolidColorBrush)darkDict["FluentCardBackgroundBrush"];
            double lightCardLum = 0.299 * lightCard.Color.R + 0.587 * lightCard.Color.G + 0.114 * lightCard.Color.B;
            double darkCardLum = 0.299 * darkCard.Color.R + 0.587 * darkCard.Color.G + 0.114 * darkCard.Color.B;
            Assert.True(lightCardLum > darkCardLum, "Card background in light mode should be brighter than in dark mode.");

            // Verify title glow effect is zeroed out for clean Fluent typography in both
            var lightGlow = lightDict["AeroTitleGlowEffect"] as System.Windows.Media.Effects.DropShadowEffect;
            var darkGlow = darkDict["AeroTitleGlowEffect"] as System.Windows.Media.Effects.DropShadowEffect;
            Assert.NotNull(lightGlow);
            Assert.NotNull(darkGlow);
            Assert.Equal(0, lightGlow.Opacity);
            Assert.Equal(0, darkGlow.Opacity);
        });
    }
}
