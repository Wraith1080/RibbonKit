using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Editing;

/// <summary>
/// A non-hit-testable, non-printing content-boundary guide for Writer Paper view.
/// </summary>
/// <remarks>
/// The guide is an overlay over the live editor. It measures the actual paper canvas so centring,
/// zoom and horizontal scrolling are inherited from the one native editor surface rather than
/// duplicated in a second layout model. A preview page setting can be supplied during ruler drag
/// without changing the document or the editor's page padding.
/// </remarks>
public sealed class WriterMarginGuide : FrameworkElement, IDisposable
{
    private const double NormalGuideOpacity = 0.58;
    private RichTextBox? _editor;
    private ScrollViewer? _viewport;
    private Border? _paperCanvas;
    private DocumentPageSettings _pageSettings = DocumentPageSettings.Letter();
    private DocumentPageSettings? _previewSettings;
    private double _zoomPercent = 100d;
    private bool _isPaperView;
    private bool _isGuideVisible = true;
    private double _lastOriginX = double.NaN;
    private double _lastOriginY = double.NaN;
    private double _lastWidth = double.NaN;
    private double _lastHeight = double.NaN;
    private double _lastScale = double.NaN;
    private DocumentPageSettings? _lastGeometrySettings;
    private bool _hasGeometrySignature;
    private bool _disposed;

    /// <summary>Initializes a non-interactive margin-guide overlay.</summary>
    public WriterMarginGuide()
    {
        IsHitTestVisible = false;
        SizeChanged += OnSizeChanged;
        Loaded += (_, _) => InvalidateVisual();
    }

    /// <summary>Gets or sets whether the guide is drawn.</summary>
    public bool IsGuideVisible
    {
        get => _isGuideVisible;
        set
        {
            if (_isGuideVisible == value)
                return;
            _isGuideVisible = value;
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets whether the live editor is in Paper view.</summary>
    public bool IsPaperView
    {
        get => _isPaperView;
        set
        {
            if (_isPaperView == value)
                return;
            _isPaperView = value;
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets the committed page settings.</summary>
    public DocumentPageSettings PageSettings
    {
        get => _pageSettings;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _pageSettings = value;
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets the temporary ruler-drag page settings.</summary>
    public DocumentPageSettings? PreviewPageSettings
    {
        get => _previewSettings;
        set
        {
            _previewSettings = value;
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets the logical editor zoom.</summary>
    public double ZoomPercent
    {
        get => _zoomPercent;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Zoom must be finite and positive.");
            _zoomPercent = value;
            InvalidateVisual();
        }
    }

    /// <summary>Attaches the guide to the existing editor, viewport and paper canvas.</summary>
    public void Attach(RichTextBox editor, ScrollViewer viewport, Border paperCanvas)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(paperCanvas);
        if (_editor is not null && !ReferenceEquals(_editor, editor))
            throw new InvalidOperationException("A Writer margin guide cannot switch native editors.");

        if (_viewport is not null)
            _viewport.ScrollChanged -= OnViewportChanged;
        if (_paperCanvas is not null)
            _paperCanvas.LayoutUpdated -= OnPaperLayoutUpdated;
        _editor = editor;
        _viewport = viewport;
        _paperCanvas = paperCanvas;
        _viewport.ScrollChanged += OnViewportChanged;
        _paperCanvas.LayoutUpdated += OnPaperLayoutUpdated;
        InvalidateVisual();
    }

    /// <summary>Detaches native editor and paper-layout event handlers without disposing the guide.</summary>
    public void Detach()
    {
        if (_disposed)
            return;
        DetachInputs();
        _editor = null;
        _viewport = null;
        _paperCanvas = null;
        _previewSettings = null;
        InvalidateVisual();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        Detach();
        _disposed = true;
    }

    /// <summary>Clears a temporary preview without changing committed settings.</summary>
    public void ClearPreview() => PreviewPageSettings = null;

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (!IsGuideVisible || !IsPaperView || _paperCanvas is null)
            return;

        var settings = _previewSettings ?? _pageSettings;
        if (!TryGetPaperOrigin(out var origin))
            return;

        var scale = _zoomPercent / 100d;
        var left = origin.X + settings.Margins.LeftDip * scale;
        var top = origin.Y + settings.Margins.TopDip * scale;
        var width = settings.ContentWidthDip * scale;
        var height = settings.ContentHeightDip * scale;
        if (width <= 0 || height <= 0)
            return;

        var highContrast = SystemParameters.HighContrast;
        var brush = highContrast
            ? SystemColors.WindowTextBrush
            : ResolveBrush("RibbonKit.Brushes.Text.Secondary", SystemColors.GrayTextBrush);
        var pen = new Pen(brush, highContrast ? 1.5 : 1)
        {
            DashStyle = DashStyles.Dot,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        if (pen.CanFreeze)
            pen.Freeze();
        if (!highContrast)
            drawingContext.PushOpacity(NormalGuideOpacity);
        try
        {
            drawingContext.DrawRectangle(null, pen,
                new Rect(left, top, width, height));
        }
        finally
        {
            if (!highContrast)
                drawingContext.Pop();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // WPF does not clip FrameworkElement descendants to their layout slot by default. Keep
        // this non-hit-testable overlay inside its own EditorSurface row so a page-sized guide
        // cannot paint into the sibling status bar below it.
        var clip = new RectangleGeometry(new Rect(0, 0,
            Math.Max(0, ActualWidth), Math.Max(0, ActualHeight)));
        if (clip.CanFreeze)
            clip.Freeze();
        Clip = clip;
    }

    private bool TryGetPaperOrigin(out Point origin)
    {
        origin = default;
        if (_paperCanvas is null || !IsLoaded)
            return false;
        try
        {
            var border = _paperCanvas.BorderThickness;
            origin = _paperCanvas.TranslatePoint(new Point(border.Left, border.Top), this);
            return double.IsFinite(origin.X) && double.IsFinite(origin.Y);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private Brush ResolveBrush(object key, Brush fallback) =>
        TryFindResource(key) as Brush ?? fallback;

    private void OnViewportChanged(object sender, ScrollChangedEventArgs e) => InvalidateVisual();

    private void OnPaperLayoutUpdated(object? sender, EventArgs e)
    {
        if (!TryGetPaperOrigin(out var origin))
            return;
        var settings = _previewSettings ?? _pageSettings;
        var scale = _zoomPercent / 100d;
        var left = origin.X + settings.Margins.LeftDip * scale;
        var top = origin.Y + settings.Margins.TopDip * scale;
        var width = settings.ContentWidthDip * scale;
        var height = settings.ContentHeightDip * scale;
        if (_hasGeometrySignature && Equals(_lastGeometrySettings, settings) &&
            Math.Abs(_lastOriginX - left) < 0.01 && Math.Abs(_lastOriginY - top) < 0.01 &&
            Math.Abs(_lastWidth - width) < 0.01 && Math.Abs(_lastHeight - height) < 0.01 &&
            Math.Abs(_lastScale - scale) < 0.0001)
            return;
        _lastGeometrySettings = settings;
        _lastOriginX = left;
        _lastOriginY = top;
        _lastWidth = width;
        _lastHeight = height;
        _lastScale = scale;
        _hasGeometrySignature = true;
        InvalidateVisual();
    }

    private void DetachInputs()
    {
        if (_viewport is not null)
            _viewport.ScrollChanged -= OnViewportChanged;
        if (_paperCanvas is not null)
            _paperCanvas.LayoutUpdated -= OnPaperLayoutUpdated;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WriterMarginGuide));
    }
}
