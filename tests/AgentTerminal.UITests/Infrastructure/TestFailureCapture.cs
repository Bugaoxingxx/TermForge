using System;
using System.IO;

namespace AgentTerminal.UITests.Infrastructure;

public static class TestFailureCapture
{
    public static string CaptureScreen(string testName)
    {
        try
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults", "Screenshots");
            Directory.CreateDirectory(dir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = Path.Combine(dir, $"{testName}_{timestamp}.png");
            using var image = FlaUI.Core.Capturing.Capture.Screen();
            image.ToFile(filename);
            return filename;
        }
        catch
        {
            return string.Empty;
        }
    }
}
