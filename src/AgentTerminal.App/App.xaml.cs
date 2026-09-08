using System;
using System.Windows;
using System.Windows.Media;
using AgentTerminal.App.Infrastructure;
using AgentTerminal.Infrastructure.Logging;
using Microsoft.Win32;

namespace AgentTerminal.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ResourceDictionary? _highContrastDictionary;
    private ResourceDictionary? _darkThemeDictionary;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LoggingService.Initialize();

        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        LoggingService.CloseAndFlush();
        base.OnExit(e);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.Accessibility ||
            e.Category == UserPreferenceCategory.Color ||
            e.Category == UserPreferenceCategory.General)
        {
            Dispatcher.BeginInvoke(ApplyTheme);
        }
    }

    /// <summary>
    /// 依据系统辅助功能与深浅色模式首选项统一应用主题字典。
    /// </summary>
    public void ApplyTheme()
    {
        if (SystemParameters.HighContrast)
        {
            if (_highContrastDictionary == null)
            {
                _highContrastDictionary = new ResourceDictionary
                {
                    Source = new Uri("/AgentTerminal.App;component/Themes/HighContrast.xaml", UriKind.RelativeOrAbsolute)
                };
                Resources.MergedDictionaries.Add(_highContrastDictionary);
            }

            if (_darkThemeDictionary != null)
            {
                Resources.MergedDictionaries.Remove(_darkThemeDictionary);
                _darkThemeDictionary = null;
            }
            return;
        }

        if (_highContrastDictionary != null)
        {
            Resources.MergedDictionaries.Remove(_highContrastDictionary);
            _highContrastDictionary = null;
        }

        bool isDark = WindowBackdropHelper.IsDarkModePreferred();
        if (isDark)
        {
            if (_darkThemeDictionary == null)
            {
                _darkThemeDictionary = new ResourceDictionary
                {
                    Source = new Uri("/AgentTerminal.App;component/Themes/DarkColors.xaml", UriKind.RelativeOrAbsolute)
                };
                Resources.MergedDictionaries.Add(_darkThemeDictionary);
            }
        }
        else if (_darkThemeDictionary != null)
        {
            Resources.MergedDictionaries.Remove(_darkThemeDictionary);
            _darkThemeDictionary = null;
        }

        // 动态覆盖系统强调色画刷，与 Windows 任务栏/设置 AccentColor 联动
        var accentColor = WindowBackdropHelper.GetAccentColor();
        var accentBrush = new SolidColorBrush(accentColor);
        accentBrush.Freeze();
        Resources["FluentAccentBrush"] = accentBrush;
        Resources["AeroFocusBorderBrush"] = accentBrush;
    }
}
