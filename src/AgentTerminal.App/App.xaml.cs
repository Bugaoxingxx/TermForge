using System.Windows;
using AgentTerminal.Infrastructure.Logging;

namespace AgentTerminal.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LoggingService.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggingService.CloseAndFlush();
        base.OnExit(e);
    }
}
