using System.Collections.ObjectModel;
using AgentTerminal.Core.Models;
using AgentTerminal.Docking.Layout;
using AgentTerminal.Docking.ViewModels;
using AgentTerminal.Infrastructure.Configuration;
using AgentTerminal.Terminal.ConPty;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AgentTerminal.App.ViewModels;

/// <summary>
/// MMC 风格 MDI 工作台主视图模型。
/// 统一管理终端多文档集合、活动上下文、经典 MDI 布局调度、侧边栏面板显隐及键盘加速导航。
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ProfileManager _profileManager;

    [ObservableProperty]
    private string _title = "TermForge - MMC MDI Agent Terminal";

    [ObservableProperty]
    private TerminalDocumentViewModel? _activeDocument;

    [ObservableProperty]
    private bool _isNavigationPaneVisible = true;

    [ObservableProperty]
    private bool _isPropertiesPaneVisible = true;

    [ObservableProperty]
    private bool _isLogPaneVisible = true;

    [ObservableProperty]
    private double _containerWidth = 1000;

    [ObservableProperty]
    private double _containerHeight = 700;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public ObservableCollection<TerminalDocumentViewModel> Documents { get; } = new();

    public ObservableCollection<ShellProfile> AvailableProfiles { get; } = new();

    public MainWindowViewModel()
    {
        _profileManager = new ProfileManager();
        _profileManager.LoadDefaults();

        foreach (var profile in _profileManager.ShellProfiles)
        {
            AvailableProfiles.Add(profile);
        }

        // 启动时默认创建第一个 PowerShell 7 调试终端文档
        var defaultProfile = AvailableProfiles.FirstOrDefault(p => p.IsDefault) ?? ShellProfile.CreatePowerShellCore();
        CreateNewDocument(defaultProfile, autoStart: false);
    }

    [RelayCommand]
    public void NewTerminal(ShellProfile? profile = null)
    {
        var targetProfile = profile ?? AvailableProfiles.FirstOrDefault(p => p.IsDefault) ?? ShellProfile.CreatePowerShellCore();
        CreateNewDocument(targetProfile, autoStart: true);
    }

    [RelayCommand]
    public async Task CloseTerminalAsync(TerminalDocumentViewModel? doc)
    {
        var target = doc ?? ActiveDocument;
        if (target == null || !Documents.Contains(target)) return;

        // 关闭文档时安全清理其绑定的会话与原生进程
        if (target.Session != null)
        {
            try
            {
                await target.Session.StopAsync();
                await target.Session.DisposeAsync();
            }
            catch { }
        }

        Documents.Remove(target);

        if (ActiveDocument == target)
        {
            ActiveDocument = Documents.LastOrDefault();
            if (ActiveDocument != null)
            {
                ActiveDocument.IsActive = true;
            }
        }

        StatusMessage = $"已关闭文档：{target.Title}";
    }

    [RelayCommand]
    public void Cascade()
    {
        if (Documents.Count == 0) return;
        MdiLayoutManager.Cascade(Documents, ContainerWidth, ContainerHeight);
        StatusMessage = "执行窗口层叠排布 (Cascade)";
    }

    [RelayCommand]
    public void TileHorizontal()
    {
        if (Documents.Count == 0) return;
        MdiLayoutManager.TileHorizontal(Documents, ContainerWidth, ContainerHeight);
        StatusMessage = "执行窗口水平平铺 (Tile Horizontal)";
    }

    [RelayCommand]
    public void TileVertical()
    {
        if (Documents.Count == 0) return;
        MdiLayoutManager.TileVertical(Documents, ContainerWidth, ContainerHeight);
        StatusMessage = "执行窗口垂直平铺 (Tile Vertical)";
    }

    [RelayCommand]
    public void RestoreAll()
    {
        if (Documents.Count == 0) return;
        MdiLayoutManager.RestoreAll(Documents);
        StatusMessage = "还原所有窗口 (Restore All)";
    }

    [RelayCommand]
    public void ToggleNavigationPane() => IsNavigationPaneVisible = !IsNavigationPaneVisible;

    [RelayCommand]
    public void TogglePropertiesPane() => IsPropertiesPaneVisible = !IsPropertiesPaneVisible;

    [RelayCommand]
    public void ToggleLogPane() => IsLogPaneVisible = !IsLogPaneVisible;

    /// <summary>
    /// Ctrl+Tab 循环切换下一个活动文档
    /// </summary>
    public void SwitchToNextDocument()
    {
        if (Documents.Count <= 1) return;

        int currentIndex = ActiveDocument != null ? Documents.IndexOf(ActiveDocument) : -1;
        int nextIndex = (currentIndex + 1) % Documents.Count;
        ActivateDocument(Documents[nextIndex]);
    }

    public void ActivateDocument(TerminalDocumentViewModel doc)
    {
        if (doc == null || !Documents.Contains(doc)) return;

        foreach (var d in Documents)
        {
            d.IsActive = (d == doc);
        }

        ActiveDocument = doc;
        StatusMessage = $"当前活动文档：{doc.Title}";
    }

    public async Task CleanupAllSessionsAsync()
    {
        foreach (var doc in Documents.ToList())
        {
            if (doc.Session != null)
            {
                try
                {
                    await doc.Session.StopAsync();
                    await doc.Session.DisposeAsync();
                }
                catch { }
            }
        }
    }

    private void CreateNewDocument(ShellProfile profile, bool autoStart)
    {
        int index = Documents.Count + 1;
        string title = $"{profile.Name} #{index}";

        var doc = new TerminalDocumentViewModel(
            profile: profile,
            sessionFactory: p => new ConPtyTerminalSession(p),
            id: Guid.NewGuid().ToString("N"))
        {
            Title = title,
            Left = 30 + ((Documents.Count % 8) * 28),
            Top = 30 + ((Documents.Count % 8) * 28),
            Width = Math.Max(550, ContainerWidth * 0.65),
            Height = Math.Max(380, ContainerHeight * 0.65),
            ZIndex = Documents.Count + 1
        };

        doc.RequestClose += async (s, e) => await CloseTerminalAsync(doc);

        Documents.Add(doc);
        ActivateDocument(doc);

        if (autoStart)
        {
            _ = doc.StartAsync();
        }

        StatusMessage = $"已新建终端文档：{title}";
    }
}
