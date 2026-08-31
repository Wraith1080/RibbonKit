using System.Collections.Immutable;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Pagination;

/// <summary>
/// Opt-in clone-page compositor. Its pages and overlays are presentation-only; all mutations are
/// requested against the separately realized authoritative RichTextBox.
/// </summary>
internal sealed class WriterPaginatedDiagnosticSurface : Grid
{
    private const double PageGap = 24;
    private readonly ScrollViewer _scrollViewer;
    private readonly Canvas _pageCanvas;
    private readonly Canvas _rulerCanvas;
    private readonly Border _statusBorder;
    private readonly TextBlock _statusText;
    private readonly Dictionary<int, Canvas> _overlayCanvases = new();
    private WriterPaginationLayoutResult? _result;
    private RichTextBox? _editor;
    private long _requestedGeneration;
    private long _documentIdentity;
    private double _zoomPercent = 100;
    private int _requestedPage;
    private WriterPaginationPageInteraction? _dragAnchor;
    private ResizeDrag? _resizeDrag;
    private long? _selectedObjectIdentity;
    private WriterPaginationObjectKind? _selectedObjectKind;
    private DocumentPageSettings? _chromeSettings;
    private WriterRulerIndentation _rulerIndentation;
    private bool _showRuler;
    private bool _showMarginGuides;
    private Window? _hostWindow;
    private string? _interactionStatus;

    internal WriterPaginatedDiagnosticSurface()
    {
        Background = new SolidColorBrush(Color.FromRgb(229, 232, 235));
        AutomationProperties.SetAutomationId(this, "PaginatedEditingDiagnostic");
        AutomationProperties.SetName(this, "Opt-in paginated editing diagnostic");

        _pageCanvas = new Canvas { Background = Brushes.Transparent };
        _scrollViewer = new ScrollViewer
        {
            Content = _pageCanvas,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            CanContentScroll = false,
            Focusable = false
        };
        _scrollViewer.ScrollChanged += OnScrollChanged;
        _scrollViewer.SizeChanged += OnViewportSizeChanged;
        Children.Add(_scrollViewer);

        _rulerCanvas = new Canvas
        {
            Height = 24,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromRgb(246, 247, 248)),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(_rulerCanvas, 15);
        Children.Add(_rulerCanvas);

        _statusText = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 11,
            Text = "Paginated diagnostic: waiting for layout"
        };
        _statusBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 36, 43, 50)),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Child = _statusText,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(_statusBorder, 20);
        Children.Add(_statusBorder);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    internal event Action<int>? PageWindowRequested;
    internal event Action<WriterPaginationPageInteraction,
        WriterPaginationPageInteraction?>? InteractionRequested;
    internal event Func<WriterPaginationResizeInteraction, bool>? ResizeRequested;
    internal event Action? DpiScaleChanged;

    internal IReadOnlyCollection<int> RenderedPages => _overlayCanvases.Keys;
    internal int RulerElementCount => _rulerCanvas.Children.Count;
    internal int MarginGuideCount => _overlayCanvases.Values.Sum(canvas =>
        canvas.Children.OfType<FrameworkElement>().Count(element =>
            Equals(element.Tag, "pagination-margin-guide")));
    internal int ResizeHandleCount => _overlayCanvases.Values.Sum(canvas =>
        canvas.Children.OfType<FrameworkElement>().Count(element =>
            element.Tag is string tag && tag.StartsWith("pagination-resize-",
                StringComparison.Ordinal)));

    internal WriterPaginationPageInteraction CaptureInteractionForTesting(
        int pageNumber, int sourceOffset)
    {
        var result = _result ?? throw new InvalidOperationException(
            "No diagnostic generation is published.");
        var entry = result.Insertions
            .Where(item => item.PageNumber == pageNumber)
            .MinBy(item => Math.Abs((long)item.SourceOffset - sourceOffset));
        if (entry == default)
            throw new InvalidOperationException($"Page {pageNumber + 1} has no geometry.");
        var rect = entry.Rectangle.ToRect();
        return new WriterPaginationPageInteraction(result.Generation,
            result.DocumentIdentity, pageNumber,
            new Point(rect.Left, rect.Top + rect.Height / 2), null, null);
    }

    internal WriterPaginationPageInteraction CaptureObjectInteractionForTesting(
        WriterPaginationObjectKind kind)
    {
        var result = _result ?? throw new InvalidOperationException(
            "No diagnostic generation is published.");
        var item = result.StructuredObjects.First(entry => entry.Kind == kind);
        var rect = item.Rectangle.ToRect();
        return new WriterPaginationPageInteraction(result.Generation,
            result.DocumentIdentity, item.PageNumber,
            new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2),
            item.ObjectIdentity, item.Kind);
    }

    internal void ApplyInteractionForTesting(
        WriterPaginationPageInteraction anchor,
        WriterPaginationPageInteraction? moving = null) =>
        InteractionRequested?.Invoke(anchor, moving);

    internal bool BeginResizeForTesting(WriterPaginationObjectKind kind,
        WriterPaginationResizeHandleKind handle)
    {
        var interaction = CaptureObjectInteractionForTesting(kind);
        ShowInteractionStatus(interaction.ObjectIdentity!.Value, kind, activated: true);
        return TryBeginResize(interaction.PageNumber, interaction.PagePoint, handle);
    }

    internal bool UpdateResizeForTesting(double deltaX, double deltaY) =>
        UpdateResize(new Vector(deltaX, deltaY));

    internal bool CompleteResizeForTesting() => CompleteResize();

    internal void RequestPageForTesting(int pageNumber)
    {
        _requestedPage = pageNumber;
        ScrollToPage(pageNumber);
        PageWindowRequested?.Invoke(pageNumber);
    }

    internal double ZoomPercent
    {
        get => _zoomPercent;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Math.Abs(_zoomPercent - value) < 0.001)
                return;
            var centerPage = GetViewportPage();
            _zoomPercent = value;
            if (_result is not null)
            {
                RebuildPages();
                ScrollToPage(centerPage);
            }
        }
    }

    internal void Invalidate(long generation, long documentIdentity)
    {
        CancelActiveResize();
        if (documentIdentity != _documentIdentity)
        {
            _selectedObjectIdentity = null;
            _selectedObjectKind = null;
        }
        _requestedGeneration = generation;
        _documentIdentity = documentIdentity;
        _statusText.Text = $"Paginated diagnostic: updating generation {generation:N0}…";
        RefreshOverlays(null);
    }

    internal void Publish(WriterPaginationLayoutResult result,
        double captureMilliseconds, RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(editor);
        if (result.Generation != _requestedGeneration ||
            result.DocumentIdentity != _documentIdentity)
            return;
        _result = result;
        _editor = editor;
        _statusBorder.Background = new SolidColorBrush(Color.FromArgb(220, 36, 43, 50));
        _requestedPage = result.VisiblePage;
        RebuildPages();
        _statusText.Text = $"Diagnostic · document {result.DocumentIdentity:N0} · " +
            $"generation {result.Generation:N0} · " +
            $"page {result.VisiblePage + 1:N0}/{result.PageCount:N0} · " +
            $"capture {captureMilliseconds:0.#} ms · layout {result.WorkerMilliseconds:0.#} ms";
        _interactionStatus = null;
        RefreshOverlays(editor);
    }

    internal void ShowInteractionStatus(long objectIdentity,
        WriterPaginationObjectKind kind, bool activated)
    {
        if (activated)
        {
            _selectedObjectIdentity = objectIdentity;
            _selectedObjectKind = kind;
            RefreshOverlays(_editor);
        }
        _interactionStatus = activated
            ? $"{kind.ToString().ToLowerInvariant()} selected"
            : $"{kind.ToString().ToLowerInvariant()} rejected";
        if (_result is { } result)
            _statusText.Text = $"Diagnostic · generation {result.Generation:N0} · " +
                $"page {result.VisiblePage + 1:N0}/{result.PageCount:N0} · " +
                _interactionStatus;
    }

    internal void ShowResizeStatus(WriterPaginationObjectKind kind, bool committed,
        bool cancelled = false)
    {
        _interactionStatus = cancelled
            ? $"{kind.ToString().ToLowerInvariant()} resize cancelled"
            : committed
                ? $"{kind.ToString().ToLowerInvariant()} resized"
                : $"{kind.ToString().ToLowerInvariant()} resize rejected";
        if (_result is { } result)
            _statusText.Text = $"Diagnostic · document {result.DocumentIdentity:N0} · " +
                $"generation {result.Generation:N0} · {_interactionStatus}";
    }

    internal void SetChrome(DocumentPageSettings settings, bool showRuler,
        bool showMarginGuides, WriterRulerIndentation indentation)
    {
        _chromeSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        _showRuler = showRuler;
        _showMarginGuides = showMarginGuides;
        _rulerIndentation = indentation;
        _rulerCanvas.Visibility = showRuler ? Visibility.Visible : Visibility.Collapsed;
        _scrollViewer.Margin = showRuler ? new Thickness(0, 24, 0, 0) : default;
        RebuildRuler();
        RefreshOverlays(_editor);
    }

    internal void CancelActiveResize()
    {
        if (_resizeDrag is not { } drag)
            return;
        _resizeDrag = null;
        ResizeRequested?.Invoke(drag.ToInteraction(WriterPaginationResizePhase.Cancel,
            default));
        RefreshOverlays(_editor);
    }

    internal void ShowFailure(string message)
    {
        _statusText.Text = $"Paginated diagnostic failed: {message}";
        _statusBorder.Background = new SolidColorBrush(Color.FromArgb(230, 145, 36, 36));
    }

    internal void Clear()
    {
        CancelActiveResize();
        _result = null;
        _editor = null;
        _selectedObjectIdentity = null;
        _selectedObjectKind = null;
        _overlayCanvases.Clear();
        _pageCanvas.Children.Clear();
        _pageCanvas.Width = 0;
        _pageCanvas.Height = 0;
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (!oldDpi.Equals(newDpi))
            DpiScaleChanged?.Invoke();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (ReferenceEquals(window, _hostWindow))
            return;
        if (_hostWindow is not null)
            _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;
        _hostWindow = window;
        if (_hostWindow is not null)
            _hostWindow.PreviewKeyDown += OnHostPreviewKeyDown;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_hostWindow is not null)
            _hostWindow.PreviewKeyDown -= OnHostPreviewKeyDown;
        _hostWindow = null;
        CancelActiveResize();
    }

    private void OnHostPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _resizeDrag is null)
            return;
        CancelActiveResize();
        e.Handled = true;
    }

    internal void RefreshOverlays(RichTextBox? editor)
    {
        foreach (var canvas in _overlayCanvases.Values)
            canvas.Children.Clear();
        if (_result is not { } result || editor is null ||
            result.Generation != _requestedGeneration ||
            result.DocumentIdentity != _documentIdentity)
            return;

        AddMarginGuideOverlays(result);
        AddStructuredObjectOverlays(result);
        var document = editor.Document;
        var start = document.ContentStart.GetOffsetToPosition(editor.Selection.Start);
        var end = document.ContentStart.GetOffsetToPosition(editor.Selection.End);
        if (start != end)
            AddRangeOverlay(result, start, end, "selection",
                new SolidColorBrush(Color.FromArgb(85, 0, 120, 215)), 0);
        else
            AddCaretOverlay(result, document.ContentStart.GetOffsetToPosition(
                editor.CaretPosition));
        AddSpellingOverlays(result, editor);
    }

    private void RebuildPages()
    {
        if (_result is not { } result)
            return;
        _overlayCanvases.Clear();
        _pageCanvas.Children.Clear();
        var scale = _zoomPercent / 100d;
        var pageWidth = result.PageSettings.WidthDip * scale;
        var pageHeight = result.PageSettings.HeightDip * scale;
        var pitch = pageHeight + PageGap;
        _pageCanvas.Width = Math.Max(_scrollViewer.ViewportWidth,
            pageWidth + PageGap * 2);
        _pageCanvas.Height = PageGap + result.PageCount * pitch;

        foreach (var page in result.Pages)
        {
            var overlay = new Canvas
            {
                Width = result.PageSettings.WidthDip,
                Height = result.PageSettings.HeightDip,
                IsHitTestVisible = false
            };
            _overlayCanvases[page.PageNumber] = overlay;
            var pageGrid = new Grid
            {
                Width = pageWidth,
                Height = pageHeight,
                Tag = page.PageNumber,
                Background = Brushes.White,
                Cursor = Cursors.IBeam
            };
            pageGrid.Children.Add(new Image
            {
                Source = DecodePage(page.PngBytes),
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            });
            pageGrid.Children.Add(new Viewbox
            {
                Stretch = Stretch.Fill,
                IsHitTestVisible = false,
                Child = overlay
            });
            pageGrid.MouseLeftButtonDown += OnPageMouseLeftButtonDown;
            pageGrid.MouseMove += OnPageMouseMove;
            pageGrid.MouseLeftButtonUp += OnPageMouseLeftButtonUp;
            pageGrid.LostMouseCapture += OnPageLostMouseCapture;

            var frame = new Border
            {
                Width = pageWidth,
                Height = pageHeight,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(176, 180, 184)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 8,
                    ShadowDepth = 2,
                    Opacity = 0.18
                },
                Child = pageGrid
            };
            Canvas.SetLeft(frame, Math.Max(PageGap,
                (_pageCanvas.Width - pageWidth) / 2));
            Canvas.SetTop(frame, PageGap + page.PageNumber * pitch);
            _pageCanvas.Children.Add(frame);
        }
        RebuildRuler();
        RefreshOverlays(_editor);
    }

    private void RebuildRuler()
    {
        _rulerCanvas.Children.Clear();
        if (!_showRuler || _result is not { } result || _chromeSettings is null)
            return;
        var scale = _zoomPercent / 100d;
        var pageWidth = result.PageSettings.WidthDip * scale;
        var pageLeft = Math.Max(PageGap, (_pageCanvas.Width - pageWidth) / 2)
            - _scrollViewer.HorizontalOffset;
        var layout = WriterRulerGeometry.Create(_chromeSettings, _zoomPercent,
            pageLeft, _rulerIndentation);
        _rulerCanvas.Width = Math.Max(0, ActualWidth);
        foreach (var zone in layout.MarginZones)
        {
            var margin = new Rectangle
            {
                Width = zone.WidthDip,
                Height = 24,
                Fill = new SolidColorBrush(Color.FromRgb(224, 227, 230)),
                Tag = "pagination-ruler-margin"
            };
            Canvas.SetLeft(margin, zone.StartDip);
            _rulerCanvas.Children.Add(margin);
        }
        foreach (var tick in layout.Ticks)
        {
            var line = new Line
            {
                X1 = tick.PositionDip,
                X2 = tick.PositionDip,
                Y1 = 24 - tick.LengthDip,
                Y2 = 24,
                Stroke = Brushes.DimGray,
                StrokeThickness = 1,
                Tag = "pagination-ruler-tick"
            };
            _rulerCanvas.Children.Add(line);
            if (!tick.IsMajor || string.IsNullOrEmpty(tick.Label))
                continue;
            var label = new TextBlock
            {
                Text = tick.Label,
                FontSize = 9,
                Foreground = Brushes.DimGray,
                Tag = "pagination-ruler-label"
            };
            Canvas.SetLeft(label, tick.PositionDip + 2);
            Canvas.SetTop(label, 0);
            _rulerCanvas.Children.Add(label);
        }
        foreach (var marker in Enum.GetValues<WriterRulerIndentMarker>())
        {
            if (layout.GetMarkerPosition(marker) is not { } x)
                continue;
            var triangle = new Polygon
            {
                Points = marker is WriterRulerIndentMarker.FirstLine
                    ? new PointCollection { new(0, 0), new(8, 0), new(4, 5) }
                    : new PointCollection { new(0, 5), new(8, 5), new(4, 0) },
                Fill = Brushes.SteelBlue,
                Tag = $"pagination-ruler-{marker.ToString().ToLowerInvariant()}"
            };
            Canvas.SetLeft(triangle, x - 4);
            Canvas.SetTop(triangle, marker is WriterRulerIndentMarker.FirstLine ? 1 : 17);
            _rulerCanvas.Children.Add(triangle);
        }
    }

    private void AddMarginGuideOverlays(WriterPaginationLayoutResult result)
    {
        if (!_showMarginGuides)
            return;
        var settings = result.PageSettings;
        foreach (var canvas in _overlayCanvases.Values)
        {
            var guide = new Rectangle
            {
                Width = Math.Max(1, settings.ContentWidthDip),
                Height = Math.Max(1, settings.HeightDip - settings.TopMarginDip -
                    settings.BottomMarginDip),
                Stroke = new SolidColorBrush(Color.FromArgb(150, 75, 115, 155)),
                StrokeThickness = 0.75,
                StrokeDashArray = new DoubleCollection { 3, 3 },
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
                Tag = "pagination-margin-guide"
            };
            Canvas.SetLeft(guide, settings.LeftMarginDip);
            Canvas.SetTop(guide, settings.TopMarginDip);
            canvas.Children.Add(guide);
        }
    }

    private void AddStructuredObjectOverlays(WriterPaginationLayoutResult result)
    {
        foreach (var item in result.StructuredObjects)
        {
            if (!_overlayCanvases.TryGetValue(item.PageNumber, out var canvas))
                continue;
            var rect = item.Rectangle.ToRect();
            var shape = new Rectangle
            {
                Width = Math.Max(1, rect.Width),
                Height = Math.Max(1, rect.Height),
                Fill = item.Kind == WriterPaginationObjectKind.Picture
                    ? new SolidColorBrush(Color.FromArgb(24, 30, 144, 255))
                    : Brushes.Transparent,
                StrokeThickness = item.Kind switch
                {
                    WriterPaginationObjectKind.Picture => 2,
                    WriterPaginationObjectKind.Hyperlink => 1,
                    _ => 1.25
                },
                StrokeDashArray = item.Kind == WriterPaginationObjectKind.Table
                    ? new DoubleCollection { 3, 2 }
                    : null,
                Stroke = item.Kind switch
                {
                    WriterPaginationObjectKind.Picture => Brushes.DodgerBlue,
                    WriterPaginationObjectKind.Table => Brushes.SteelBlue,
                    _ => Brushes.Transparent
                },
                Tag = $"pagination-{item.Kind.ToString().ToLowerInvariant()}"
            };
            Canvas.SetLeft(shape, rect.Left);
            Canvas.SetTop(shape, rect.Top);
            canvas.Children.Add(shape);
        }
        AddResizeHandles(result);
        AddResizePreview();
    }

    private void AddResizePreview()
    {
        if (_resizeDrag is not { } drag ||
            !_overlayCanvases.TryGetValue(drag.Request.PageNumber, out var canvas))
            return;
        var rect = GetPreviewRect(drag);
        var preview = new Rectangle
        {
            Width = Math.Max(1, rect.Width),
            Height = Math.Max(1, rect.Height),
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = Math.Max(0.5, 1 / (_zoomPercent / 100d)),
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(16, 30, 144, 255)),
            IsHitTestVisible = false,
            Tag = "pagination-resize-preview"
        };
        Canvas.SetLeft(preview, rect.Left);
        Canvas.SetTop(preview, rect.Top);
        canvas.Children.Add(preview);
    }

    private Rect GetPreviewRect(ResizeDrag drag)
    {
        if (drag.Request.ObjectKind == WriterPaginationObjectKind.Picture)
        {
            var handle = ToPictureHandle(drag.Request.Handle);
            var size = WriterPictureResizeGeometry.Resize(drag.OpeningRect.Size,
                drag.Delta, handle, new Size(
                    _result!.PageSettings.ContentWidthDip,
                    _result.PageSettings.HeightDip - _result.PageSettings.TopMarginDip -
                    _result.PageSettings.BottomMarginDip));
            return new Rect(drag.OpeningRect.Location, size);
        }
        return new Rect(drag.OpeningRect.Location,
            new Size(Math.Max(WriterTableResizeGeometry.MinimumColumnWidth,
                    drag.OpeningRect.Width + drag.Delta.X),
                Math.Max(WriterTableResizeGeometry.MinimumRowHeight,
                    drag.OpeningRect.Height + drag.Delta.Y)));
    }

    private void AddResizeHandles(WriterPaginationLayoutResult result)
    {
        if (_selectedObjectIdentity is not { } identity ||
            _selectedObjectKind is not { } kind)
            return;
        var geometries = result.StructuredObjects
            .Where(item => item.ObjectIdentity == identity && item.Kind == kind &&
                _overlayCanvases.ContainsKey(item.PageNumber))
            .ToArray();
        if (geometries.Length == 0)
            return;
        if (kind == WriterPaginationObjectKind.Picture)
        {
            foreach (var geometry in geometries)
            foreach (var handle in Enum.GetValues<WriterPaginationResizeHandleKind>()
                         .Where(value => value != WriterPaginationResizeHandleKind.TableOverall))
                AddResizeHandle(geometry, handle, GetHandleRect(geometry.Rectangle.ToRect(), handle));
        }
        else if (kind == WriterPaginationObjectKind.Table)
        {
            var geometry = geometries.MaxBy(item => item.PageNumber);
            if (geometry != default)
            {
                var rect = geometry.Rectangle.ToRect();
                var size = 8 / (_zoomPercent / 100d);
                AddResizeHandle(geometry, WriterPaginationResizeHandleKind.TableOverall,
                    new Rect(rect.Right - size / 2, rect.Bottom - size / 2, size, size));
            }
        }
    }

    private void AddResizeHandle(WriterPaginationObjectGeometry geometry,
        WriterPaginationResizeHandleKind handle, Rect rect)
    {
        if (!_overlayCanvases.TryGetValue(geometry.PageNumber, out var canvas))
            return;
        var shape = new Rectangle
        {
            Width = rect.Width,
            Height = rect.Height,
            Fill = Brushes.White,
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = Math.Max(0.5, 1 / (_zoomPercent / 100d)),
            Tag = $"pagination-resize-{handle.ToString().ToLowerInvariant()}"
        };
        Canvas.SetLeft(shape, rect.Left);
        Canvas.SetTop(shape, rect.Top);
        canvas.Children.Add(shape);
    }

    private Rect GetHandleRect(Rect rect, WriterPaginationResizeHandleKind handle)
    {
        var size = 8 / (_zoomPercent / 100d);
        var x = handle switch
        {
            WriterPaginationResizeHandleKind.PictureTopLeft or
                WriterPaginationResizeHandleKind.PictureLeft or
                WriterPaginationResizeHandleKind.PictureBottomLeft => rect.Left,
            WriterPaginationResizeHandleKind.PictureTop or
                WriterPaginationResizeHandleKind.PictureBottom => rect.Left + rect.Width / 2,
            _ => rect.Right
        };
        var y = handle switch
        {
            WriterPaginationResizeHandleKind.PictureTopLeft or
                WriterPaginationResizeHandleKind.PictureTop or
                WriterPaginationResizeHandleKind.PictureTopRight => rect.Top,
            WriterPaginationResizeHandleKind.PictureLeft or
                WriterPaginationResizeHandleKind.PictureRight => rect.Top + rect.Height / 2,
            _ => rect.Bottom
        };
        return new Rect(x - size / 2, y - size / 2, size, size);
    }

    private void AddCaretOverlay(WriterPaginationLayoutResult result, int offset)
    {
        var nearest = result.Insertions
            .Where(item => _overlayCanvases.ContainsKey(item.PageNumber))
            .OrderBy(item => Math.Abs((long)item.SourceOffset - offset))
            .FirstOrDefault();
        if (nearest == default || Math.Abs((long)nearest.SourceOffset - offset) > 1 ||
            !_overlayCanvases.TryGetValue(nearest.PageNumber, out var canvas))
            return;
        var rect = nearest.Rectangle.ToRect();
        var caret = new Rectangle
        {
            Width = 1,
            Height = Math.Max(1, rect.Height),
            Fill = Brushes.Black,
            Tag = "pagination-caret"
        };
        Canvas.SetLeft(caret, rect.Left);
        Canvas.SetTop(caret, rect.Top);
        canvas.Children.Add(caret);
    }

    private void AddSpellingOverlays(WriterPaginationLayoutResult result, RichTextBox editor)
    {
        var cursor = editor.Document.ContentStart;
        var visited = 0;
        while (visited++ < 256 &&
               editor.GetNextSpellingErrorPosition(cursor, LogicalDirection.Forward) is { } error)
        {
            var range = editor.GetSpellingErrorRange(error);
            if (range is null)
                break;
            var start = editor.Document.ContentStart.GetOffsetToPosition(range.Start);
            var end = editor.Document.ContentStart.GetOffsetToPosition(range.End);
            AddRangeOverlay(result, start, end, "spelling", Brushes.Red, 1);
            cursor = range.End.GetNextInsertionPosition(LogicalDirection.Forward)
                ?? editor.Document.ContentEnd;
            if (cursor.CompareTo(editor.Document.ContentEnd) >= 0)
                break;
        }
    }

    private void AddRangeOverlay(WriterPaginationLayoutResult result,
        int firstOffset, int secondOffset, string tag, Brush brush, double underlineHeight)
    {
        var start = Math.Min(firstOffset, secondOffset);
        var end = Math.Max(firstOffset, secondOffset);
        foreach (var page in result.Insertions
                     .Where(item => item.SourceOffset >= start && item.SourceOffset <= end)
                     .GroupBy(item => item.PageNumber))
        {
            if (!_overlayCanvases.TryGetValue(page.Key, out var canvas))
                continue;
            foreach (var line in page.GroupBy(item => Math.Round(item.Rectangle.Y, 1)))
            {
                var left = line.Min(item => item.Rectangle.X);
                var top = line.Min(item => item.Rectangle.Y);
                var right = line.Max(item => item.Rectangle.X +
                    Math.Max(1, item.Rectangle.Width));
                var bottom = line.Max(item => item.Rectangle.Y + item.Rectangle.Height);
                var shape = new Rectangle
                {
                    Width = Math.Max(2, right - left),
                    Height = underlineHeight > 0 ? underlineHeight : Math.Max(1, bottom - top),
                    Fill = brush,
                    Tag = $"pagination-{tag}"
                };
                Canvas.SetLeft(shape, left);
                Canvas.SetTop(shape, underlineHeight > 0 ? Math.Max(top, bottom - 1) : top);
                canvas.Children.Add(shape);
            }
        }
    }

    private void OnPageMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Grid { Tag: int pageNumber } page)
            return;
        var scaledPoint = e.GetPosition(page);
        if (TryHitResizeHandle(pageNumber, scaledPoint, out var handle) &&
            TryBeginResize(pageNumber, ToPagePoint(scaledPoint), handle))
        {
            page.CaptureMouse();
            e.Handled = true;
            return;
        }
        if (!TryCreateInteraction(pageNumber, scaledPoint, out var interaction))
            return;
        _dragAnchor = interaction;
        page.CaptureMouse();
        InteractionRequested?.Invoke(interaction, null);
        e.Handled = true;
    }

    private void OnPageMouseMove(object sender, MouseEventArgs e)
    {
        if (_resizeDrag is { } resize && e.LeftButton == MouseButtonState.Pressed &&
            sender is Grid page)
        {
            var point = ToPagePoint(e.GetPosition(page));
            UpdateResize(point - resize.OpeningPoint);
            e.Handled = true;
            return;
        }
        if (_dragAnchor is not { } anchor || e.LeftButton != MouseButtonState.Pressed ||
            !TryCreateInteractionFromCanvasPoint(e.GetPosition(_pageCanvas), out var moving))
            return;
        InteractionRequested?.Invoke(anchor, moving);
        e.Handled = true;
    }

    private void OnPageMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_resizeDrag is not null)
            CompleteResize();
        if (sender is UIElement element && element.IsMouseCaptured)
            element.ReleaseMouseCapture();
        _dragAnchor = null;
        e.Handled = true;
    }

    private void OnPageLostMouseCapture(object sender, MouseEventArgs e)
    {
        _dragAnchor = null;
        CancelActiveResize();
    }

    private bool TryHitResizeHandle(int pageNumber, Point scaledPoint,
        out WriterPaginationResizeHandleKind handle)
    {
        handle = default;
        if (_result is not { } result || _selectedObjectIdentity is not { } identity ||
            _selectedObjectKind is not { } kind)
            return false;
        var point = ToPagePoint(scaledPoint);
        var geometry = result.StructuredObjects.FirstOrDefault(item =>
            item.PageNumber == pageNumber && item.ObjectIdentity == identity && item.Kind == kind);
        if (geometry == default)
            return false;
        var candidates = kind == WriterPaginationObjectKind.Picture
            ? Enum.GetValues<WriterPaginationResizeHandleKind>()
                .Where(value => value != WriterPaginationResizeHandleKind.TableOverall)
            : new[] { WriterPaginationResizeHandleKind.TableOverall };
        var hitPadding = 5 / (_zoomPercent / 100d);
        foreach (var candidate in candidates)
        {
            var rect = candidate == WriterPaginationResizeHandleKind.TableOverall
                ? new Rect(geometry.Rectangle.X + geometry.Rectangle.Width,
                    geometry.Rectangle.Y + geometry.Rectangle.Height, 0, 0)
                : GetHandleRect(geometry.Rectangle.ToRect(), candidate);
            rect.Inflate(hitPadding, hitPadding);
            if (!rect.Contains(point))
                continue;
            handle = candidate;
            return true;
        }
        return false;
    }

    private bool TryBeginResize(int pageNumber, Point point,
        WriterPaginationResizeHandleKind handle)
    {
        if (_resizeDrag is not null || _result is not { } result ||
            _selectedObjectIdentity is not { } identity ||
            _selectedObjectKind is not { } kind)
            return false;
        var geometry = result.StructuredObjects.FirstOrDefault(item =>
            item.PageNumber == pageNumber && item.ObjectIdentity == identity && item.Kind == kind);
        if (geometry == default)
            return false;
        var request = new WriterPaginationResizeInteraction(result.Generation,
            result.DocumentIdentity, pageNumber, identity, kind, handle,
            WriterPaginationResizePhase.Start, 0, 0);
        if (ResizeRequested?.Invoke(request) != true)
            return false;
        _resizeDrag = new ResizeDrag(request, point, geometry.Rectangle.ToRect(), default);
        RefreshOverlays(_editor);
        return true;
    }

    private bool UpdateResize(Vector delta)
    {
        if (_resizeDrag is not { } drag || !double.IsFinite(delta.X) ||
            !double.IsFinite(delta.Y))
            return false;
        var request = drag.ToInteraction(WriterPaginationResizePhase.Update, delta);
        if (ResizeRequested?.Invoke(request) != true)
        {
            _resizeDrag = null;
            RefreshOverlays(_editor);
            return false;
        }
        _resizeDrag = drag with { Delta = delta };
        RefreshOverlays(_editor);
        return true;
    }

    private bool CompleteResize()
    {
        if (_resizeDrag is not { } drag)
            return false;
        _resizeDrag = null;
        var committed = ResizeRequested?.Invoke(drag.ToInteraction(
            WriterPaginationResizePhase.Commit, drag.Delta)) == true;
        RefreshOverlays(_editor);
        return committed;
    }

    private Point ToPagePoint(Point scaledPoint)
    {
        if (_result is not { } result)
            return default;
        var scale = _zoomPercent / 100d;
        return new Point(Math.Clamp(scaledPoint.X / scale, 0, result.PageSettings.WidthDip),
            Math.Clamp(scaledPoint.Y / scale, 0, result.PageSettings.HeightDip));
    }

    private bool TryCreateInteraction(int pageNumber, Point scaledPoint,
        out WriterPaginationPageInteraction interaction)
    {
        interaction = default;
        if (_result is not { } result || result.Generation != _requestedGeneration ||
            result.DocumentIdentity != _documentIdentity ||
            !result.MappedPages.Contains(pageNumber))
            return false;
        var pagePoint = ToPagePoint(scaledPoint);
        var objectHit = result.StructuredObjects
            .Where(item => item.PageNumber == pageNumber &&
                item.Rectangle.ToRect().Contains(pagePoint))
            .OrderBy(item => item.Kind == WriterPaginationObjectKind.Picture ? 0 :
                item.Kind == WriterPaginationObjectKind.Hyperlink ? 1 : 2)
            .ThenBy(item => item.Rectangle.Width * item.Rectangle.Height)
            .Cast<WriterPaginationObjectGeometry?>()
            .FirstOrDefault();
        interaction = new WriterPaginationPageInteraction(result.Generation,
            result.DocumentIdentity, pageNumber, pagePoint,
            objectHit?.ObjectIdentity, objectHit?.Kind);
        return true;
    }

    private bool TryCreateInteractionFromCanvasPoint(Point point,
        out WriterPaginationPageInteraction interaction)
    {
        interaction = default;
        if (_result is not { } result)
            return false;
        var scale = _zoomPercent / 100d;
        var scaledWidth = result.PageSettings.WidthDip * scale;
        var scaledHeight = result.PageSettings.HeightDip * scale;
        var pitch = scaledHeight + PageGap;
        var pageNumber = Math.Clamp(
            (int)Math.Floor(Math.Max(0, point.Y - PageGap) / pitch),
            0, Math.Max(0, result.PageCount - 1));
        if (!result.MappedPages.Contains(pageNumber))
            return false;
        var pageLeft = Math.Max(PageGap, (_pageCanvas.Width - scaledWidth) / 2);
        var pageTop = PageGap + pageNumber * pitch;
        return TryCreateInteraction(pageNumber,
            new Point(Math.Clamp(point.X - pageLeft, 0, scaledWidth),
                Math.Clamp(point.Y - pageTop, 0, scaledHeight)), out interaction);
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        RebuildRuler();
        if (_result is null || e.VerticalChange == 0 && e.ExtentHeightChange != 0)
            return;
        var page = GetViewportPage();
        if (page == _requestedPage)
            return;
        _requestedPage = page;
        PageWindowRequested?.Invoke(page);
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_result is not null)
            RebuildPages();
    }

    private int GetViewportPage()
    {
        if (_result is not { } result)
            return 0;
        var pageHeight = result.PageSettings.HeightDip * _zoomPercent / 100d;
        var pitch = pageHeight + PageGap;
        var center = _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight / 2;
        return Math.Clamp((int)Math.Floor(Math.Max(0, center - PageGap) / pitch),
            0, Math.Max(0, result.PageCount - 1));
    }

    private void ScrollToPage(int pageNumber)
    {
        if (_result is not { } result)
            return;
        var pitch = result.PageSettings.HeightDip * _zoomPercent / 100d + PageGap;
        _scrollViewer.ScrollToVerticalOffset(PageGap + pageNumber * pitch);
    }

    private static BitmapSource DecodePage(ImmutableArray<byte> bytes)
    {
        using var stream = new MemoryStream(bytes.ToArray(), writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static WriterPictureResizeHandle ToPictureHandle(
        WriterPaginationResizeHandleKind handle) => handle switch
    {
        WriterPaginationResizeHandleKind.PictureTopLeft => WriterPictureResizeHandle.TopLeft,
        WriterPaginationResizeHandleKind.PictureTop => WriterPictureResizeHandle.Top,
        WriterPaginationResizeHandleKind.PictureTopRight => WriterPictureResizeHandle.TopRight,
        WriterPaginationResizeHandleKind.PictureRight => WriterPictureResizeHandle.Right,
        WriterPaginationResizeHandleKind.PictureBottomRight => WriterPictureResizeHandle.BottomRight,
        WriterPaginationResizeHandleKind.PictureBottom => WriterPictureResizeHandle.Bottom,
        WriterPaginationResizeHandleKind.PictureBottomLeft => WriterPictureResizeHandle.BottomLeft,
        WriterPaginationResizeHandleKind.PictureLeft => WriterPictureResizeHandle.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(handle), handle,
            "The resize handle is not a picture handle.")
    };

    private sealed record ResizeDrag(
        WriterPaginationResizeInteraction Request,
        Point OpeningPoint,
        Rect OpeningRect,
        Vector Delta)
    {
        internal WriterPaginationResizeInteraction ToInteraction(
            WriterPaginationResizePhase phase, Vector delta) => Request with
        {
            Phase = phase,
            DeltaX = delta.X,
            DeltaY = delta.Y
        };
    }
}
