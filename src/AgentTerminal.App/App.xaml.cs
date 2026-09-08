using System;
using System.Windows;
using AgentTerminal.Infrastructure.Logging;
using Microsoft.Win32;

namespace AgentTerminal.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ResourceDictionary? _highContrastDictionary;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
#pragma warning disable WPF0001
        ThemeMode = ThemeMode.System;
#pragma warning restore WPF0001
        LoggingService.Initialize();

        ApplyHighContrastThemeIfNeeded();
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
        if (e.Category == UserPreferenceCategory.Accessibility || e.Category == UserPreferenceCategory.Color)
        {
            Dispatcher.BeginInvoke(ApplyHighContrastThemeIfNeeded);
        }
    }

    public void ApplyHighContrastThemeIfNeeded()
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
        }
        else if (_highContrastDictionary != null)
        {
            Resources.MergedDictionaries.Remove(_highContrastDictionary);
            _highContrastDictionary = null;
        }
    }
}
