using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace AgentTerminal.UITests.Infrastructure;

public class TestAppFixture : IDisposable
{
    private readonly string _appPath;
    public Application? App { get; private set; }
    public UIA3Automation? Automation { get; private set; }

    public TestAppFixture()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidatePaths = new[]
        {
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\src\AgentTerminal.App\bin\Debug\net10.0-windows\AgentTerminal.App.exe")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\src\AgentTerminal.App\bin\Debug\net10.0-windows\AgentTerminal.App.exe")),
            Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\src\AgentTerminal.App\bin\Debug\net10.0-windows\AgentTerminal.App.exe")),
            Path.GetFullPath(Path.Combine(baseDir, "AgentTerminal.App.exe"))
        };

        _appPath = candidatePaths.FirstOrDefault(File.Exists) 
            ?? throw new FileNotFoundException($"Cannot find AgentTerminal.App.exe in candidate paths. Checked: {string.Join(", ", candidatePaths)}");
    }

    public Window Launch()
    {
        Automation = new UIA3Automation();
        App = Application.Launch(_appPath);

        var mainWindow = UiaWait.Until(() =>
        {
            try
            {
                var w = App.GetMainWindow(Automation);
                if (w != null && (!string.IsNullOrEmpty(w.Title) || w.AutomationId == "MainWindow"))
                {
                    return w;
                }

                // Fallback: search desktop top-level windows by process ID
                var desktop = Automation.GetDesktop();
                var topWindows = desktop.FindAllChildren(cf => cf.ByProcessId(App.ProcessId));
                foreach (var child in topWindows)
                {
                    var win = child.AsWindow();
                    if (win != null && (!string.IsNullOrEmpty(win.Title) || win.AutomationId == "MainWindow"))
                    {
                        return win;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }, timeout: TimeSpan.FromSeconds(25), message: "Failed to locate MainWindow within 25s of launch");

        try
        {
            mainWindow.SetForeground();
        }
        catch { }

        try
        {
            App.WaitWhileBusy(TimeSpan.FromSeconds(5));
        }
        catch { }

        System.Threading.Thread.Sleep(400);

        return mainWindow;
    }

    public string CaptureScreenshot(string stepOrTestName, AutomationElement? targetElement = null)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var targetDir = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\TestResults\Screenshots"));
            Directory.CreateDirectory(targetDir);

            var safeName = string.Join("_", stepOrTestName.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(targetDir, $"{safeName}.png");

            if (targetElement != null)
            {
                try
                {
                    using var elemImg = FlaUI.Core.Capturing.Capture.Element(targetElement);
                    elemImg.ToFile(filePath);
                    return filePath;
                }
                catch { }
            }

            using var image = FlaUI.Core.Capturing.Capture.Screen();
            image.ToFile(filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Screenshot] Failed to capture: {ex.Message}");
            return string.Empty;
        }
    }

    public void Dispose()
    {
        try
        {
            if (App != null && !App.HasExited)
            {
                var pid = App.ProcessId;
                try
                {
                    App.Close();
                }
                catch { }

                try
                {
                    using var proc = Process.GetProcessById(pid);
                    if (!proc.WaitForExit(3000))
                    {
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(3000);
                    }
                }
                catch { }
            }
        }
        catch { }
        finally
        {
            Automation?.Dispose();
            Automation = null;
            App = null;
            System.Threading.Thread.Sleep(500);
        }
    }
}
