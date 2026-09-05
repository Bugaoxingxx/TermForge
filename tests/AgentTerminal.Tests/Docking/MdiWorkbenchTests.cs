using AgentTerminal.Core.Models;
using AgentTerminal.Docking.Layout;
using AgentTerminal.Docking.ViewModels;
using Xunit;

namespace AgentTerminal.Tests.Docking;

/// <summary>
/// MMC 风格 MDI 布局算法与文档生命周期单元测试
/// </summary>
public class MdiWorkbenchTests
{
    [Fact]
    public void Cascade_WithThreeWindows_ShouldStaggerPositionsDiagonally()
    {
        // Arrange
        var docs = new List<TerminalDocumentViewModel>
        {
            new(title: "Doc 1"),
            new(title: "Doc 2"),
            new(title: "Doc 3")
        };

        double containerWidth = 1000;
        double containerHeight = 800;

        // Act
        MdiLayoutManager.Cascade(docs, containerWidth, containerHeight);

        // Assert
        Assert.Equal(MdiWindowState.Normal, docs[0].WindowState);
        Assert.Equal(0.0, docs[0].Left);
        Assert.Equal(0.0, docs[0].Top);
        Assert.Equal(1, docs[0].ZIndex);

        Assert.Equal(MdiWindowState.Normal, docs[1].WindowState);
        Assert.Equal(MdiLayoutManager.CascadeOffsetStep, docs[1].Left);
        Assert.Equal(MdiLayoutManager.CascadeOffsetStep, docs[1].Top);
        Assert.Equal(2, docs[1].ZIndex);

        Assert.Equal(MdiWindowState.Normal, docs[2].WindowState);
        Assert.Equal(MdiLayoutManager.CascadeOffsetStep * 2, docs[2].Left);
        Assert.Equal(MdiLayoutManager.CascadeOffsetStep * 2, docs[2].Top);
        Assert.Equal(3, docs[2].ZIndex);
    }

    [Fact]
    public void TileHorizontal_WithThreeWindows_ShouldStackVerticallyWithEqualHeight()
    {
        // Arrange
        var docs = new List<TerminalDocumentViewModel>
        {
            new(title: "Doc 1"),
            new(title: "Doc 2"),
            new(title: "Doc 3")
        };

        double containerWidth = 900;
        double containerHeight = 600;

        // Act
        MdiLayoutManager.TileHorizontal(docs, containerWidth, containerHeight);

        // Assert
        double expectedHeight = 200; // 600 / 3

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(MdiWindowState.Normal, docs[i].WindowState);
            Assert.Equal(0.0, docs[i].Left);
            Assert.Equal(i * expectedHeight, docs[i].Top);
            Assert.Equal(containerWidth, docs[i].Width);
            Assert.Equal(expectedHeight, docs[i].Height);
        }
    }

    [Fact]
    public void TileVertical_WithThreeWindows_ShouldArrangeSideBySideWithEqualWidth()
    {
        // Arrange
        var docs = new List<TerminalDocumentViewModel>
        {
            new(title: "Doc 1"),
            new(title: "Doc 2"),
            new(title: "Doc 3")
        };

        double containerWidth = 900;
        double containerHeight = 600;

        // Act
        MdiLayoutManager.TileVertical(docs, containerWidth, containerHeight);

        // Assert
        double expectedWidth = 300; // 900 / 3

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(MdiWindowState.Normal, docs[i].WindowState);
            Assert.Equal(i * expectedWidth, docs[i].Left);
            Assert.Equal(0.0, docs[i].Top);
            Assert.Equal(expectedWidth, docs[i].Width);
            Assert.Equal(containerHeight, docs[i].Height);
        }
    }

    [Fact]
    public void MaximizeAndRestore_ShouldPreserveAndRecoverOriginalBounds()
    {
        // Arrange
        var doc = new TerminalDocumentViewModel(title: "TestDoc")
        {
            Left = 120,
            Top = 80,
            Width = 500,
            Height = 350,
            WindowState = MdiWindowState.Normal
        };

        // Act: 最大化
        doc.Maximize();
        Assert.Equal(MdiWindowState.Maximized, doc.WindowState);

        // Act: 还原
        doc.Restore();

        // Assert
        Assert.Equal(MdiWindowState.Normal, doc.WindowState);
        Assert.Equal(120, doc.Left);
        Assert.Equal(80, doc.Top);
        Assert.Equal(500, doc.Width);
        Assert.Equal(350, doc.Height);
    }

    [Fact]
    public void RestoreAll_ShouldResetMinimizedAndMaximizedWindowsToNormalAndRecoverBounds()
    {
        // Arrange
        var doc1 = new TerminalDocumentViewModel(title: "Doc 1")
        {
            Left = 50, Top = 60, Width = 400, Height = 300, WindowState = MdiWindowState.Normal
        };
        doc1.Minimize();

        var doc2 = new TerminalDocumentViewModel(title: "Doc 2")
        {
            Left = 150, Top = 160, Width = 500, Height = 350, WindowState = MdiWindowState.Normal
        };
        doc2.Maximize();

        var doc3 = new TerminalDocumentViewModel(title: "Doc 3")
        {
            Left = 200, Top = 200, Width = 600, Height = 400, WindowState = MdiWindowState.Normal
        };

        var docs = new List<TerminalDocumentViewModel> { doc1, doc2, doc3 };

        // Act
        MdiLayoutManager.RestoreAll(docs);

        // Assert
        Assert.All(docs, d => Assert.Equal(MdiWindowState.Normal, d.WindowState));

        // 验证各窗口恢复到最大化/最小化前的尺寸位置
        Assert.Equal(50, doc1.Left);
        Assert.Equal(60, doc1.Top);
        Assert.Equal(400, doc1.Width);
        Assert.Equal(300, doc1.Height);

        Assert.Equal(150, doc2.Left);
        Assert.Equal(160, doc2.Top);
        Assert.Equal(500, doc2.Width);
        Assert.Equal(350, doc2.Height);
    }
}
