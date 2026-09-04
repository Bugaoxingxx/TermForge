using System.IO;
using AvalonDock;
using AvalonDock.Layout.Serialization;

namespace AgentTerminal.Docking.Layout;

/// <summary>
/// AvalonDock 布局持久化与恢复管理器（Phase 7 / Phase 10）
/// </summary>
public class DockingLayoutManager
{
    public void SaveLayout(DockingManager dockingManager, string filePath)
    {
        var serializer = new XmlLayoutSerializer(dockingManager);
        serializer.Serialize(filePath);
    }

    public void LoadLayout(DockingManager dockingManager, string filePath)
    {
        if (!File.Exists(filePath)) return;

        var serializer = new XmlLayoutSerializer(dockingManager);
        serializer.Deserialize(filePath);
    }
}
