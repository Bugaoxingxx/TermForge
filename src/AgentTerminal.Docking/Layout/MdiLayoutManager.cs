using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;

namespace AgentTerminal.Docking.Layout;

/// <summary>
/// MDI 经典布局管理器，提供 Win32 风格的子窗口层叠、水平平铺、垂直平铺及全部还原算法
/// </summary>
public static class MdiLayoutManager
{
    public const double CascadeOffsetStep = 26.0;
    public const double DefaultMinWidth = 320.0;
    public const double DefaultMinHeight = 220.0;

    /// <summary>
    /// 层叠排列（Cascade）：子窗口按对角阶梯偏移展开排布
    /// </summary>
    public static void Cascade(IEnumerable<IMdiDocument> documents, double containerWidth, double containerHeight)
    {
        if (documents == null || containerWidth <= 0 || containerHeight <= 0) return;

        var list = documents.ToList();
        if (list.Count == 0) return;

        double targetWidth = Math.Max(DefaultMinWidth, containerWidth * 0.65);
        double targetHeight = Math.Max(DefaultMinHeight, containerHeight * 0.65);

        double currentX = 0;
        double currentY = 0;

        for (int i = 0; i < list.Count; i++)
        {
            var doc = list[i];
            doc.WindowState = MdiWindowState.Normal;
            doc.Width = targetWidth;
            doc.Height = targetHeight;
            doc.Left = currentX;
            doc.Top = currentY;
            doc.ZIndex = i + 1;

            currentX += CascadeOffsetStep;
            currentY += CascadeOffsetStep;

            // 超出边界时环绕回退
            if (currentX + targetWidth > containerWidth || currentY + targetHeight > containerHeight)
            {
                currentX = 0;
                currentY = 0;
            }
        }
    }

    /// <summary>
    /// 水平平铺（Tile Horizontally）：子窗口在垂直方向自上而下等高堆叠展开
    /// </summary>
    public static void TileHorizontal(IEnumerable<IMdiDocument> documents, double containerWidth, double containerHeight)
    {
        if (documents == null || containerWidth <= 0 || containerHeight <= 0) return;

        var list = documents.ToList();
        int count = list.Count;
        if (count == 0) return;

        double itemHeight = containerHeight / count;

        for (int i = 0; i < count; i++)
        {
            var doc = list[i];
            doc.WindowState = MdiWindowState.Normal;
            doc.Left = 0;
            doc.Top = i * itemHeight;
            doc.Width = containerWidth;
            doc.Height = itemHeight;
            doc.ZIndex = i + 1;
        }
    }

    /// <summary>
    /// 垂直平铺（Tile Vertically）：子窗口在水平方向自左向右等宽并排展开
    /// </summary>
    public static void TileVertical(IEnumerable<IMdiDocument> documents, double containerWidth, double containerHeight)
    {
        if (documents == null || containerWidth <= 0 || containerHeight <= 0) return;

        var list = documents.ToList();
        int count = list.Count;
        if (count == 0) return;

        double itemWidth = containerWidth / count;

        for (int i = 0; i < count; i++)
        {
            var doc = list[i];
            doc.WindowState = MdiWindowState.Normal;
            doc.Left = i * itemWidth;
            doc.Top = 0;
            doc.Width = itemWidth;
            doc.Height = containerHeight;
            doc.ZIndex = i + 1;
        }
    }

    /// <summary>
    /// 全部还原（Restore All）：将所有最小化或最大化的文档还原为正常浮动状态
    /// </summary>
    public static void RestoreAll(IEnumerable<IMdiDocument> documents)
    {
        if (documents == null) return;

        foreach (var doc in documents)
        {
            doc.WindowState = MdiWindowState.Normal;
        }
    }
}
