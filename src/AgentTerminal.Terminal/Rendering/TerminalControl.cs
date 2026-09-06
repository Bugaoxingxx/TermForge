using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AgentTerminal.Core.Abstractions;
using AgentTerminal.Core.Models;
using AgentTerminal.Terminal.Input;

namespace AgentTerminal.Terminal.Rendering;

/// <summary>
/// WPF 原生高性能字符网格终端呈现控件。
/// 基于 DrawingContext 与 GlyphRun 批量绘制，杜绝海量 Visual 开销；
/// 内置键盘输入直连、鼠标选区与复制粘贴、尺寸联动去抖及历史回滚回看。
/// </summary>
public class TerminalControl : Control
{
    public static readonly DependencyProperty BufferProperty =
        DependencyProperty.Register(
            nameof(Buffer),
            typeof(ITerminalBuffer),
            typeof(TerminalControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnBufferChanged));

    public static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(
            nameof(Session),
            typeof(ITerminalSession),
            typeof(TerminalControl),
            new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty ScrollOffsetProperty =
        DependencyProperty.Register(
            nameof(ScrollOffset),
            typeof(int),
            typeof(TerminalControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    public ITerminalBuffer? Buffer
    {
        get => (ITerminalBuffer?)GetValue(BufferProperty);
        set => SetValue(BufferProperty, value);
    }

    public ITerminalSession? Session
    {
        get => (ITerminalSession?)GetValue(SessionProperty);
        set => SetValue(SessionProperty, value);
    }

    public int ScrollOffset
    {
        get => (int)GetValue(ScrollOffsetProperty);
        set => SetValue(ScrollOffsetProperty, value);
    }

    // 字体度量与字形
    private GlyphTypeface? _normalGlyphTypeface;
    private GlyphTypeface? _boldGlyphTypeface;
    private GlyphTypeface? _italicGlyphTypeface;
    private double _charWidth = 8.5;
    private double _lineHeight = 18.0;
    private double _baseline = 14.0;
    private bool _metricsInitialized;

    public double CharWidth => _charWidth;
    public double LineHeight => _lineHeight;

    // 画刷缓存（设 1024 上限，防 TrueColor 内存无界增长）
    private const int MaxBrushCacheSize = 1024;
    private static readonly Dictionary<uint, Brush> _brushCache = new();
    private static readonly Brush _cursorBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
    private static readonly Brush _selectionBrush = new SolidColorBrush(Color.FromArgb(100, 51, 153, 255));

    // 渲染临时缓冲复用（消除高帧率每行 List 分配）
    private readonly List<ushort> _renderGlyphIndices = new(256);
    private readonly List<double> _renderAdvanceWidths = new(256);

    // 鼠标选区
    private Point? _selectionStart;
    private Point? _selectionEnd;
    private bool _isSelecting;

    public bool HasSelection => _selectionStart.HasValue && _selectionEnd.HasValue && _selectionStart.Value != _selectionEnd.Value;

    // 尺寸联动去抖
    private readonly DispatcherTimer _resizeDebounceTimer;

    static TerminalControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(TerminalControl),
            new FrameworkPropertyMetadata(typeof(TerminalControl)));

        _cursorBrush.Freeze();
        _selectionBrush.Freeze();
    }

    public TerminalControl()
    {
        Focusable = true;
        Background = Brushes.Black;
        FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New");
        FontSize = 14.0;

        _resizeDebounceTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _resizeDebounceTimer.Tick += OnResizeDebounceTick;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnControlSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureFontMetrics();
        if (Buffer != null)
        {
            Buffer.RefreshRequested -= OnBufferRefreshRequested;
            Buffer.RefreshRequested += OnBufferRefreshRequested;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _resizeDebounceTimer.Stop();
        if (Buffer != null)
        {
            Buffer.RefreshRequested -= OnBufferRefreshRequested;
        }
    }

    private static void OnBufferChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TerminalControl control)
        {
            if (e.OldValue is ITerminalBuffer oldBuffer)
            {
                oldBuffer.RefreshRequested -= control.OnBufferRefreshRequested;
            }
            if (e.NewValue is ITerminalBuffer newBuffer)
            {
                newBuffer.RefreshRequested += control.OnBufferRefreshRequested;
            }

            control.InvalidateVisual();
        }
    }

    private void OnBufferRefreshRequested(object? sender, EventArgs e)
    {
        if (Dispatcher.CheckAccess())
        {
            InvalidateVisual();
        }
        else
        {
            Dispatcher.BeginInvoke(InvalidateVisual);
        }
    }

    public void EnsureFontMetrics()
    {
        if (_metricsInitialized) return;

        var baseTypeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        if (!baseTypeface.TryGetGlyphTypeface(out _normalGlyphTypeface))
        {
            var fallbackTypeface = new Typeface(new FontFamily("Consolas, Courier New"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            fallbackTypeface.TryGetGlyphTypeface(out _normalGlyphTypeface);
        }

        var boldTypeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        boldTypeface.TryGetGlyphTypeface(out _boldGlyphTypeface);

        var italicTypeface = new Typeface(FontFamily, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);
        italicTypeface.TryGetGlyphTypeface(out _italicGlyphTypeface);

        double emSize = FontSize;
        if (_normalGlyphTypeface != null)
        {
            if (_normalGlyphTypeface.CharacterToGlyphMap.TryGetValue('X', out ushort glyphIndex))
            {
                _charWidth = _normalGlyphTypeface.AdvanceWidths[glyphIndex] * emSize;
            }
            else
            {
                _charWidth = emSize * 0.6;
            }
            _lineHeight = Math.Ceiling(_normalGlyphTypeface.Height * emSize);
            _baseline = Math.Ceiling(_normalGlyphTypeface.Baseline * emSize);
        }
        else
        {
            _charWidth = FontSize * 0.6;
            _lineHeight = FontSize * 1.3;
            _baseline = FontSize;
        }

        _metricsInitialized = true;
    }

    public (int Columns, int Rows) GetDimensionsFromPixelSize(double pixelWidth, double pixelHeight)
    {
        EnsureFontMetrics();
        int cols = Math.Max(1, (int)Math.Floor(pixelWidth / _charWidth));
        int rows = Math.Max(1, (int)Math.Floor(pixelHeight / _lineHeight));
        return (cols, rows);
    }

    public (double Width, double Height) GetPixelSizeFromDimensions(int columns, int rows)
    {
        EnsureFontMetrics();
        return (columns * _charWidth, rows * _lineHeight);
    }

    private static Brush GetBrush(TerminalColor color, bool isForeground)
    {
        uint key = color.Value | (isForeground ? 0x80000000U : 0U);
        if (!_brushCache.TryGetValue(key, out var brush))
        {
            if (_brushCache.Count >= MaxBrushCacheSize)
            {
                _brushCache.Clear();
            }

            var (r, g, b) = color.ToRgb(isForeground);
            var solid = new SolidColorBrush(Color.FromRgb(r, g, b));
            solid.Freeze();
            brush = solid;
            _brushCache[key] = brush;
        }
        return brush;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        EnsureFontMetrics();

        dc.DrawRectangle(Background ?? Brushes.Black, null, new Rect(RenderSize));

        if (Buffer == null || _normalGlyphTypeface == null) return;

        lock (Buffer.SyncRoot)
        {
            int cols = Buffer.Dimensions.Columns;
            int rows = Buffer.Dimensions.Rows;

            double actualW = RenderSize.Width;
            double actualH = RenderSize.Height;
            int visibleCols = Math.Min(cols, Math.Max(1, (int)(actualW / _charWidth)));
            int visibleRows = Math.Min(rows, Math.Max(1, (int)(actualH / _lineHeight)));

            float pixelsPerDip = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double emSize = FontSize;

            for (int vr = 0; vr < visibleRows; vr++)
            {
                int bufferRow = vr - ScrollOffset;
                double rowY = vr * _lineHeight;

                // 背景批量矩形合并填充
                int bgStartCol = 0;
                TerminalColor currentBg = TerminalColor.Default;
                bool hasBgSpan = false;

                for (int c = 0; c < visibleCols; c++)
                {
                    var cell = Buffer.GetCell(c, bufferRow);
                    bool inverse = (cell.Attributes & CellAttributes.Inverse) != 0;
                    var effBg = inverse ? (cell.Foreground.IsDefault ? TerminalColor.From16Color(7) : cell.Foreground)
                                        : cell.Background;

                    if (!hasBgSpan)
                    {
                        currentBg = effBg;
                        bgStartCol = c;
                        hasBgSpan = true;
                    }
                    else if (effBg != currentBg)
                    {
                        if (!currentBg.IsDefault)
                        {
                            var bgBrush = GetBrush(currentBg, false);
                            dc.DrawRectangle(bgBrush, null, new Rect(bgStartCol * _charWidth, rowY, (c - bgStartCol) * _charWidth, _lineHeight));
                        }
                        currentBg = effBg;
                        bgStartCol = c;
                    }
                }
                if (hasBgSpan && !currentBg.IsDefault)
                {
                    var bgBrush = GetBrush(currentBg, false);
                    dc.DrawRectangle(bgBrush, null, new Rect(bgStartCol * _charWidth, rowY, (visibleCols - bgStartCol) * _charWidth, _lineHeight));
                }

                // 选区高亮绘制
                if (HasSelection && IsRowInSelection(vr, out int selStartCol, out int selEndCol))
                {
                    int sCol = Math.Clamp(selStartCol, 0, visibleCols);
                    int eCol = Math.Clamp(selEndCol, 0, visibleCols);
                    if (eCol > sCol)
                    {
                        dc.DrawRectangle(_selectionBrush, null, new Rect(sCol * _charWidth, rowY, (eCol - sCol) * _charWidth, _lineHeight));
                    }
                }

                // 前景文本 GlyphRun 批量绘制（复用 _renderGlyphIndices 与 _renderAdvanceWidths）
                int runStartCol = -1;
                TerminalColor currentFg = TerminalColor.Default;
                CellAttributes currentAttr = CellAttributes.None;
                _renderGlyphIndices.Clear();
                _renderAdvanceWidths.Clear();

                void FlushGlyphRun(GlyphTypeface gtf, Brush fgBrush)
                {
                    if (_renderGlyphIndices.Count == 0 || runStartCol < 0) return;
                    var origin = new Point(runStartCol * _charWidth, rowY + _baseline);
                    var glyphRun = new GlyphRun(
                        gtf,
                        0,
                        false,
                        emSize,
                        pixelsPerDip,
                        _renderGlyphIndices.ToArray(),
                        origin,
                        _renderAdvanceWidths.ToArray(),
                        null, null, null, null, null, null);
                    dc.DrawGlyphRun(fgBrush, glyphRun);

                    double runWidth = 0;
                    for (int k = 0; k < _renderAdvanceWidths.Count; k++) runWidth += _renderAdvanceWidths[k];

                    if ((currentAttr & CellAttributes.Underline) != 0)
                    {
                        dc.DrawRectangle(fgBrush, null, new Rect(runStartCol * _charWidth, rowY + _baseline + 2, runWidth, 1));
                    }
                    if ((currentAttr & CellAttributes.Strikethrough) != 0)
                    {
                        dc.DrawRectangle(fgBrush, null, new Rect(runStartCol * _charWidth, rowY + _baseline - _lineHeight * 0.3, runWidth, 1));
                    }

                    _renderGlyphIndices.Clear();
                    _renderAdvanceWidths.Clear();
                    runStartCol = -1;
                }

                for (int c = 0; c < visibleCols; c++)
                {
                    var cell = Buffer.GetCell(c, bufferRow);
                    if ((cell.Attributes & CellAttributes.WideContinuation) != 0)
                    {
                        continue;
                    }

                    char ch = cell.Character == '\0' ? ' ' : cell.Character;
                    bool inverse = (cell.Attributes & CellAttributes.Inverse) != 0;
                    var effFg = inverse ? (cell.Background.IsDefault ? TerminalColor.From16Color(0) : cell.Background)
                                        : cell.Foreground;
                    var effAttr = cell.Attributes;

                    bool isBold = (effAttr & CellAttributes.Bold) != 0;
                    bool isItalic = (effAttr & CellAttributes.Italic) != 0;

                    GlyphTypeface gtf = (isBold && _boldGlyphTypeface != null) ? _boldGlyphTypeface
                                      : (isItalic && _italicGlyphTypeface != null) ? _italicGlyphTypeface
                                      : _normalGlyphTypeface;

                    if (runStartCol >= 0 && (effFg != currentFg || effAttr != currentAttr))
                    {
                        FlushGlyphRun(gtf, GetBrush(currentFg, true));
                    }

                    if (runStartCol < 0)
                    {
                        runStartCol = c;
                        currentFg = effFg;
                        currentAttr = effAttr;
                    }

                    if (!gtf.CharacterToGlyphMap.TryGetValue(ch, out ushort glyphIndex))
                    {
                        if (!gtf.CharacterToGlyphMap.TryGetValue(' ', out glyphIndex))
                        {
                            glyphIndex = 0;
                        }
                    }

                    bool isWideLeading = (effAttr & CellAttributes.WideLeading) != 0;
                    double advance = isWideLeading ? _charWidth * 2 : _charWidth;

                    _renderGlyphIndices.Add(glyphIndex);
                    _renderAdvanceWidths.Add(advance);
                }

                if (runStartCol >= 0)
                {
                    bool isBold = (currentAttr & CellAttributes.Bold) != 0;
                    bool isItalic = (currentAttr & CellAttributes.Italic) != 0;
                    GlyphTypeface gtf = (isBold && _boldGlyphTypeface != null) ? _boldGlyphTypeface
                                      : (isItalic && _italicGlyphTypeface != null) ? _italicGlyphTypeface
                                      : _normalGlyphTypeface;
                    FlushGlyphRun(gtf, GetBrush(currentFg, true));
                }
            }

            // 光标绘制
            if (Buffer.IsCursorVisible && ScrollOffset == 0)
            {
                int cx = Buffer.CursorX;
                int cy = Buffer.CursorY;
                if (cx >= 0 && cx < visibleCols && cy >= 0 && cy < visibleRows)
                {
                    var cursorRect = new Rect(cx * _charWidth, cy * _lineHeight, _charWidth, _lineHeight);
                    dc.DrawRectangle(_cursorBrush, null, cursorRect);
                }
            }
        }
    }

    #region 鼠标交互与选区复制

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Buffer == null) return;

        int lines = e.Delta > 0 ? 3 : -3;
        int newOffset = Math.Clamp(ScrollOffset + lines, 0, Buffer.ScrollbackLineCount);
        if (newOffset != ScrollOffset)
        {
            ScrollOffset = newOffset;
            InvalidateVisual();
        }
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.ChangedButton == MouseButton.Left)
        {
            _isSelecting = true;
            _selectionStart = e.GetPosition(this);
            _selectionEnd = _selectionStart;
            CaptureMouse();
            InvalidateVisual();
            e.Handled = true;
        }
        else if (e.ChangedButton == MouseButton.Right)
        {
            // 右键粘贴
            _ = PasteAsync();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isSelecting)
        {
            _selectionEnd = e.GetPosition(this);
            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_isSelecting && e.ChangedButton == MouseButton.Left)
        {
            _isSelecting = false;
            ReleaseMouseCapture();

            if (_selectionStart.HasValue && _selectionEnd.HasValue)
            {
                var p1 = _selectionStart.Value;
                var p2 = _selectionEnd.Value;
                if (Math.Abs(p1.X - p2.X) < 3 && Math.Abs(p1.Y - p2.Y) < 3)
                {
                    // 仅点击，清除选区
                    _selectionStart = null;
                    _selectionEnd = null;
                }
            }

            InvalidateVisual();
            e.Handled = true;
        }
    }

    private bool IsRowInSelection(int visibleRow, out int startCol, out int endCol)
    {
        startCol = 0;
        endCol = 0;
        if (!HasSelection || Buffer == null) return false;

        var p1 = _selectionStart!.Value;
        var p2 = _selectionEnd!.Value;

        int r1 = (int)(Math.Min(p1.Y, p2.Y) / _lineHeight);
        int r2 = (int)(Math.Max(p1.Y, p2.Y) / _lineHeight);

        if (visibleRow < r1 || visibleRow > r2) return false;

        int c1 = (int)(p1.X / _charWidth);
        int c2 = (int)(p2.X / _charWidth);

        if (r1 == r2)
        {
            startCol = Math.Min(c1, c2);
            endCol = Math.Max(c1, c2);
            return endCol > startCol;
        }

        if (visibleRow == r1)
        {
            startCol = p1.Y <= p2.Y ? c1 : c2;
            endCol = Buffer.Dimensions.Columns;
        }
        else if (visibleRow == r2)
        {
            startCol = 0;
            endCol = p1.Y <= p2.Y ? c2 : c1;
        }
        else
        {
            startCol = 0;
            endCol = Buffer.Dimensions.Columns;
        }

        return true;
    }

    public string? GetSelectedText()
    {
        if (!HasSelection || Buffer == null) return null;

        lock (Buffer.SyncRoot)
        {
            var p1 = _selectionStart!.Value;
            var p2 = _selectionEnd!.Value;

            int r1 = (int)(Math.Min(p1.Y, p2.Y) / _lineHeight);
            int r2 = (int)(Math.Max(p1.Y, p2.Y) / _lineHeight);

            var sb = new System.Text.StringBuilder();

            for (int vr = r1; vr <= r2; vr++)
            {
                int bufferRow = vr - ScrollOffset;
                if (IsRowInSelection(vr, out int sc, out int ec))
                {
                    var lineChars = new List<char>();
                    for (int c = sc; c < ec; c++)
                    {
                        var cell = Buffer.GetCell(c, bufferRow);
                        lineChars.Add(cell.Character == '\0' ? ' ' : cell.Character);
                    }
                    string line = new string(lineChars.ToArray()).TrimEnd();
                    sb.AppendLine(line);
                }
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }
    }

    public void CopySelection()
    {
        string? text = GetSelectedText();
        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Clipboard error: {ex.Message}");
            }
        }
    }

    public async Task PasteAsync()
    {
        if (Session == null) return;
        try
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    await Session.WriteAsync(text);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Clipboard paste error: {ex.Message}");
        }
    }

    #endregion

    #region 键盘输入直连会话

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Ctrl+C 依选区分流：有选区复制，无选区发 0x03 中断
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (HasSelection)
            {
                CopySelection();
            }
            else
            {
                _ = Session?.WriteAsync(new byte[] { 0x03 });
            }
            e.Handled = true;
            return;
        }

        // Ctrl+V 粘贴
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _ = PasteAsync();
            e.Handled = true;
            return;
        }

        // 键盘映射至 VT 序列
        string? vtSequence = TerminalKeyMapper.MapKeyToVtSequence(e.Key, Keyboard.Modifiers);
        if (vtSequence != null && Session != null)
        {
            _ = Session.WriteAsync(vtSequence);
            e.Handled = true;
            return;
        }
    }

    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        base.OnTextInput(e);
        if (!string.IsNullOrEmpty(e.Text) && Session != null)
        {
            _ = Session.WriteAsync(e.Text);
            e.Handled = true;
        }
    }

    #endregion

    #region 尺寸联动与去抖

    private void OnControlSizeChanged(object sender, SizeChangedEventArgs e)
    {
        EnsureFontMetrics();
        if (RenderSize.Width < _charWidth || RenderSize.Height < _lineHeight) return;

        _resizeDebounceTimer.Stop();
        _resizeDebounceTimer.Start();
    }

    private async void OnResizeDebounceTick(object? sender, EventArgs e)
    {
        _resizeDebounceTimer.Stop();

        int newCols = Math.Max(1, (int)(RenderSize.Width / _charWidth));
        int newRows = Math.Max(1, (int)(RenderSize.Height / _lineHeight));

        if (Buffer != null)
        {
            lock (Buffer.SyncRoot)
            {
                if (Buffer.Dimensions.Columns != newCols || Buffer.Dimensions.Rows != newRows)
                {
                    Buffer.Resize(newCols, newRows);
                }
            }
            InvalidateVisual();
        }

        if (Session != null && Session.State == TerminalState.Running &&
            (Session.Dimensions.Columns != newCols || Session.Dimensions.Rows != newRows))
        {
            try
            {
                await Session.ResizeAsync(newCols, newRows);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Error resizing session: {ex.Message}");
            }
        }
    }

    #endregion
}
