using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Editing;

/// <summary>Event data for a temporary or committed Writer page-setting change.</summary>
public sealed class WriterRulerPageSettingsEventArgs : EventArgs
{
    internal WriterRulerPageSettingsEventArgs(DocumentPageSettings settings) => Settings = settings;

    /// <summary>Gets the validated page settings represented by the drag.</summary>
    public DocumentPageSettings Settings { get; }
}

/// <summary>
/// An app-owned, non-printing horizontal ruler for Writer Paper view.
/// </summary>
/// <remarks>
/// The control deliberately receives the actual editor viewport and paper canvas. It therefore
/// follows the same centring, zoom and horizontal-scroll geometry as the live RichTextBox. Page
/// margins are previewed until release; paragraph marker changes are delegated to the editor's
/// native undoable formatting path.
/// </remarks>
public sealed class WriterRuler : FrameworkElement, IDisposable
{
    private const double RulerHeight = 31d;
    private const double HitTolerance = 8d;
    private const double KeyboardStepDip = 1d;
    // Keep the page-margin hit band between the paragraph-marker bands. At the default zero-indent
    // state the first-line, hanging/body and left markers share one X coordinate, so their Y bands
    // are part of the input contract rather than a visual-only distinction.
    private const double FirstLineMarkerBandEnd = 10d;
    private const double MarginEdgeBandStart = 10d;
    private const double MarginEdgeBandEnd = 18d;
    private const double LeftMarkerBandStart = 18d;
    private const double LeftMarkerBandEnd = 24d;
    private const double HangingMarkerBandStart = 24d;
    private readonly Typeface _typeface = new("Segoe UI");
    private RichTextBox? _editor;
    private ScrollViewer? _viewport;
    private Border? _paperCanvas;
    private WriterEditingAdapter? _editing;
    private DocumentPageSettings _pageSettings = DocumentPageSettings.Letter();
    private DocumentPageSettings? _previewPageSettings;
    private double _zoomPercent = 100d;
    private bool _isPaperView;
    private bool _isRulerVisible = true;
    private bool _isSurfaceTransparent;
    private bool _canEditMargins = true;
    private bool _canEditParagraphs = true;
    private WriterRulerLayout _layout = WriterRulerGeometry.Create(
        DocumentPageSettings.Letter(), 100, 0);
    private MarginDragState? _marginDrag;
    private WriterParagraphIndentDrag? _indentDrag;
    private bool _endingCapture;
    private double _lastPointerX;
    private double _lastPointerY;
    private double _lastGeometryOrigin = double.NaN;
    private double _lastGeometryWidth = double.NaN;
    private double _lastGeometryActualWidth = double.NaN;
    private DocumentPageSettings? _lastGeometrySettings;
    private WriterRulerIndentation _lastGeometryIndentation;
    private bool _hasGeometrySignature;
    private bool _disposed;

    /// <summary>Initializes the Writer ruler.</summary>
    public WriterRuler()
    {
        Height = RulerHeight;
        MinHeight = RulerHeight;
        MaxHeight = RulerHeight;
        Focusable = true;
        Cursor = Cursors.Arrow;
        AutomationProperties.SetAutomationId(this, "WriterRuler");
        AutomationProperties.SetName(this, "Horizontal ruler");
        Loaded += (_, _) => RefreshGeometry();
        IsVisibleChanged += OnIsVisibleChanged;
        Unloaded += OnUnloaded;
    }

    /// <summary>Raised while a page-margin drag is previewing a validated candidate.</summary>
    public event EventHandler<WriterRulerPageSettingsEventArgs>? PageSettingsPreviewChanged;

    /// <summary>Raised once when a page-margin drag commits on mouse release.</summary>
    public event EventHandler<WriterRulerPageSettingsEventArgs>? PageSettingsCommitted;

    /// <summary>Raised when Escape or capture loss cancels a page-margin drag.</summary>
    public event EventHandler? PageSettingsDragCancelled;

    /// <summary>Raised after a paragraph-indent drag closes its native undo scope.</summary>
    public event EventHandler? ParagraphIndentDragCompleted;

    /// <summary>Gets whether a page-margin drag is currently previewing.</summary>
    public bool IsMarginDragActive => _marginDrag is not null;

    /// <summary>Gets whether a paragraph marker drag is currently active.</summary>
    public bool IsParagraphIndentDragActive => _indentDrag is not null;

    /// <summary>Gets the current ruler layout in this control's coordinate space.</summary>
    public WriterRulerLayout Layout => _layout;

    /// <summary>Gets or sets whether the ruler is visible.</summary>
    public bool IsRulerVisible
    {
        get => _isRulerVisible;
        set
        {
            if (_isRulerVisible == value)
                return;
            if (!value)
                CancelActiveDrags();
            _isRulerVisible = value;
            Visibility = value && _isPaperView ? Visibility.Visible : Visibility.Collapsed;
            InvalidateVisual();
        }
    }

    /// <summary>Gets or sets whether the ruler's base surface reveals its host material.</summary>
    public bool IsSurfaceTransparent
    {
        get => _isSurfaceTransparent;
        set
        {
            if (_isSurfaceTransparent == value)
                return;
            _isSurfaceTransparent = value;
            InvalidateVisual();
        }
    }

    /// <summary>Redraws theme-resource-backed ruler chrome after the host changes appearance.</summary>
    /// <remarks>
    /// The ruler resolves its brushes while drawing rather than through dependency-property resource
    /// references, so replacing theme or accent dictionaries does not otherwise invalidate it.
    /// </remarks>
    public void RefreshAppearance()
    {
        if (_disposed)
            return;
        InvalidateVisual();
    }

    /// <summary>Gets or sets whether the editor is currently in Paper view.</summary>
    public bool IsPaperView
    {
        get => _isPaperView;
        set
        {
            if (_isPaperView == value)
                return;
            if (!value)
                CancelActiveDrags();
            _isPaperView = value;
            Visibility = value && _isRulerVisible ? Visibility.Visible : Visibility.Collapsed;
            RefreshGeometry();
        }
    }

    /// <summary>Gets or sets whether page-margin handles may commit document page settings.</summary>
    public bool CanEditMargins
    {
        get => _canEditMargins;
        set
        {
            if (_canEditMargins == value)
                return;
            if (!value)
                CancelActiveDrags();
            _canEditMargins = value;
        }
    }

    /// <summary>Gets or sets whether paragraph-indent marker mutation is available.</summary>
    /// <remarks>
    /// Writer profile capabilities are enforced at this app-owned boundary. A plain-text profile
    /// may still show a calibrated paper ruler, but it must never mutate paragraph formatting.
    /// </remarks>
    public bool CanEditParagraphs
    {
        get => _canEditParagraphs;
        set
        {
            if (_canEditParagraphs == value)
                return;
            if (!value)
                CancelActiveDrags();
            _canEditParagraphs = value;
            RefreshGeometry();
        }
    }

    /// <summary>Gets or sets the committed page settings.</summary>
    public DocumentPageSettings PageSettings
    {
        get => _pageSettings;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            CancelActiveDrags();
            _pageSettings = value;
            _previewPageSettings = null;
            RefreshGeometry();
        }
    }

    /// <summary>Gets or sets the logical zoom percentage used by the paper surface.</summary>
    public double ZoomPercent
    {
        get => _zoomPercent;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "Zoom must be finite and positive.");
            if (Math.Abs(_zoomPercent - value) < 0.0001)
                return;
            CancelActiveDrags();
            _zoomPercent = value;
            RefreshGeometry();
        }
    }

    /// <summary>Attaches the ruler to the existing native editor and paper viewport.</summary>
    public void Attach(RichTextBox editor, ScrollViewer viewport, Border paperCanvas,
        WriterEditingAdapter? editing = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(paperCanvas);
        if (_editor is not null && !ReferenceEquals(_editor, editor))
            throw new InvalidOperationException("A Writer ruler cannot switch native editors.");

        CancelActiveDrags();
        DetachInputs();
        _editor = editor;
        _viewport = viewport;
        _paperCanvas = paperCanvas;
        _editing = editing;
        _viewport.ScrollChanged += OnViewportChanged;
        _paperCanvas.LayoutUpdated += OnPaperLayoutUpdated;
        _editor.SelectionChanged += OnEditorSelectionChanged;
        _editor.TextChanged += OnEditorTextChanged;
        RefreshGeometry();
    }

    /// <summary>Clears an uncommitted margin preview.</summary>
    public void CancelPageSettingsPreview()
    {
        if (_marginDrag is not null)
            CancelMarginDrag();
        else
        {
            _previewPageSettings = null;
            RefreshGeometry();
        }
    }

    /// <summary>
    /// Cancels any active page-margin or paragraph-indent transaction before the host changes the
    /// document, view, capability or control lifetime.
    /// </summary>
    internal void CancelActiveDrags()
    {
        if (_marginDrag is not null)
            CancelMarginDrag();
        if (_indentDrag is not null)
            CancelIndentDrag();
    }

    /// <summary>Begins a margin transaction for deterministic keyboard/UI tests or host input.</summary>
    public bool TryBeginMarginDrag(WriterRulerMarginEdge edge)
    {
        ThrowIfDisposed();
        if (!_canEditMargins || !Enum.IsDefined(edge) || _marginDrag is not null || _indentDrag is not null)
            return false;
        _marginDrag = new MarginDragState(_pageSettings, edge);
        _previewPageSettings = _pageSettings;
        RefreshGeometry();
        return true;
    }

    /// <summary>Begins a paragraph-marker transaction through the deterministic app test seam.</summary>
    internal bool TryBeginParagraphIndentDrag(WriterRulerIndentMarker marker)
    {
        ThrowIfDisposed();
        if (!Enum.IsDefined(marker))
            throw new ArgumentOutOfRangeException(nameof(marker), marker, "Unknown ruler marker.");
        if (!CanRenderParagraphMarkers || _editing is null ||
            _marginDrag is not null || _indentDrag is not null)
            return false;
        _indentDrag = _editing.BeginParagraphIndentDrag(marker,
            _layout.ContentWidthDip / _layout.Scale);
        return _indentDrag is not null;
    }

    /// <summary>Updates an active margin transaction from a rendered ruler coordinate.</summary>
    public void UpdateMarginDragPosition(double renderedX)
    {
        ThrowIfDisposed();
        UpdateMarginDrag(renderedX);
    }

    /// <summary>Commits an active margin transaction as one page-settings event.</summary>
    public void CommitMarginDragPosition()
    {
        ThrowIfDisposed();
        CommitMarginDrag();
    }

    /// <summary>Cancels an active margin transaction without changing committed page settings.</summary>
    public void CancelMarginDragPosition()
    {
        ThrowIfDisposed();
        if (_marginDrag is not null)
            CancelMarginDrag();
    }

    /// <summary>Recomputes page and marker geometry after a host layout or selection change.</summary>
    public void RefreshGeometry()
    {
        if (_disposed)
            return;

        var settings = _previewPageSettings ?? _pageSettings;
        var pageWidth = settings.WidthDip * _zoomPercent / 100d;
        var origin = double.IsFinite(ActualWidth) && ActualWidth > 0
            ? (ActualWidth - pageWidth) / 2d
            : 0d;
        if (TryGetPaperOrigin(out var point))
            origin = point.X;

        var indentation = ReadIndentation();
        if (_hasGeometrySignature &&
            Equals(_lastGeometrySettings, settings) &&
            Math.Abs(_lastGeometryOrigin - origin) < 0.01 &&
            Math.Abs(_lastGeometryWidth - pageWidth) < 0.01 &&
            Math.Abs(_lastGeometryActualWidth - ActualWidth) < 0.01 &&
            _lastGeometryIndentation == indentation)
            return;

        _lastGeometrySettings = settings;
        _lastGeometryOrigin = origin;
        _lastGeometryWidth = pageWidth;
        _lastGeometryActualWidth = ActualWidth;
        _lastGeometryIndentation = indentation;
        _hasGeometrySignature = true;
        _layout = WriterRulerGeometry.Create(settings, _zoomPercent, origin, indentation);
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new WriterRulerAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (!_isRulerVisible || !_isPaperView)
            return;

        var highContrast = SystemParameters.HighContrast;
        var background = highContrast
            ? SystemColors.WindowBrush
            : _isSurfaceTransparent
                ? Brushes.Transparent
                : ResolveBrush("RibbonKit.Brushes.Control.SurfaceBackground", SystemColors.ControlBrush);
        var border = highContrast
            ? SystemColors.WindowTextBrush
            : ResolveBrush("RibbonKit.Brushes.Ribbon.Border", SystemColors.ControlDarkBrush);
        var tick = highContrast
            ? SystemColors.WindowTextBrush
            : ResolveBrush("RibbonKit.Brushes.Text.Secondary", SystemColors.ControlTextBrush);
        var marker = highContrast
            ? SystemColors.HighlightBrush
            : ResolveBrush("RibbonKit.Brushes.Accent", SystemColors.HighlightBrush);
        var margin = highContrast
            ? SystemColors.ControlBrush
            : ResolveBrush("RibbonKit.Brushes.Control.CompanionBackground", SystemColors.ControlBrush);
        drawingContext.DrawRectangle(background, new Pen(border, 1),
            new Rect(0, 0, Math.Max(0, ActualWidth), RulerHeight));

        foreach (var zone in _layout.MarginZones)
        {
            var start = Math.Max(0, zone.StartDip);
            var end = Math.Min(ActualWidth, zone.EndDip);
            if (end > start)
                drawingContext.DrawRectangle(margin, null, new Rect(start, 1, end - start, RulerHeight - 2));
        }

        foreach (var rulerTick in _layout.Ticks)
        {
            if (rulerTick.PositionDip < -1 || rulerTick.PositionDip > ActualWidth + 1)
                continue;
            var y = RulerHeight - rulerTick.LengthDip;
            drawingContext.DrawLine(new Pen(tick, rulerTick.IsMajor ? 1 : 0.8),
                new Point(rulerTick.PositionDip, y), new Point(rulerTick.PositionDip, RulerHeight - 2));
            if (!string.IsNullOrEmpty(rulerTick.Label))
            {
                var text = new FormattedText(rulerTick.Label, CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight, _typeface, 9, tick,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);
                drawingContext.DrawText(text,
                    new Point(rulerTick.PositionDip + 2, 1));
            }
        }

        if (CanRenderParagraphMarkers)
        {
            DrawMarker(drawingContext, GetRenderedMarkerPosition(WriterRulerIndentMarker.FirstLine), marker,
                WriterRulerIndentMarker.FirstLine);
            DrawMarker(drawingContext, GetRenderedMarkerPosition(WriterRulerIndentMarker.Hanging), marker,
                WriterRulerIndentMarker.Hanging);
            DrawMarker(drawingContext, GetRenderedMarkerPosition(WriterRulerIndentMarker.Left), marker,
                WriterRulerIndentMarker.Left);
            DrawMarker(drawingContext, GetRenderedMarkerPosition(WriterRulerIndentMarker.Right), marker,
                WriterRulerIndentMarker.Right);
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (!_isRulerVisible || !_isPaperView || _disposed)
            return;

        var pointer = e.GetPosition(this);
        _lastPointerX = pointer.X;
        _lastPointerY = pointer.Y;
        var marker = HitTestIndentMarker(_lastPointerX, _lastPointerY);
        if (marker is not null && TryBeginParagraphIndentDrag(marker.Value))
        {
            Focus();
            CaptureMouse();
            e.Handled = true;
            return;
        }

        if (_canEditMargins && TryHitMarginEdge(_lastPointerX, _lastPointerY, out var edge) &&
            TryBeginMarginDrag(edge))
        {
            Focus();
            CaptureMouse();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_marginDrag is null && _indentDrag is null)
            return;
        _lastPointerX = e.GetPosition(this).X;
        if (_marginDrag is not null)
            UpdateMarginDrag(_lastPointerX);
        else if (_indentDrag is not null)
            UpdateIndentDrag(_lastPointerX);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_marginDrag is not null)
        {
            var pointer = e.GetPosition(this);
            _lastPointerX = pointer.X;
            _lastPointerY = pointer.Y;
            UpdateMarginDrag(_lastPointerX);
            CommitMarginDrag();
            e.Handled = true;
        }
        else if (_indentDrag is not null)
        {
            var pointer = e.GetPosition(this);
            _lastPointerX = pointer.X;
            _lastPointerY = pointer.Y;
            UpdateIndentDrag(_lastPointerX);
            var drag = _indentDrag;
            _indentDrag = null;
            drag.Commit();
            ReleaseCaptureSafely();
            ParagraphIndentDragCompleted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_endingCapture)
            return;
        if (_marginDrag is not null)
            CancelMarginDrag();
        else if (_indentDrag is not null)
            CancelIndentDrag();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            if (_marginDrag is not null)
                CancelMarginDrag();
            else if (_indentDrag is not null)
                CancelIndentDrag();
            e.Handled = true;
            return;
        }

        if (_marginDrag is null && _indentDrag is null)
            return;
        if (e.Key is Key.Left or Key.Right)
        {
            _lastPointerX += e.Key == Key.Left ? -KeyboardStepDip : KeyboardStepDip;
            if (_marginDrag is not null)
                UpdateMarginDrag(_lastPointerX);
            else if (_indentDrag is not null)
                UpdateIndentDrag(_lastPointerX);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;
        CancelActiveDrags();
        _disposed = true;
        DetachInputs();
        _editor = null;
        _viewport = null;
        _paperCanvas = null;
        _editing = null;
    }

    private void UpdateMarginDrag(double renderedX)
    {
        if (_marginDrag is null)
            return;
        var settings = _marginDrag.OriginalSettings;
        var margins = settings.Margins;
        try
        {
            margins = _marginDrag.Edge == WriterRulerMarginEdge.Left
                ? margins with
                {
                    LeftDip = WriterRulerGeometry.LeftMarginFromRenderedX(_layout, renderedX,
                        margins.RightDip)
                }
                : margins with
                {
                    RightDip = WriterRulerGeometry.RightMarginFromRenderedX(_layout, renderedX,
                        margins.LeftDip)
                };
            var preview = settings.WithMargins(margins);
            _previewPageSettings = preview;
            RefreshGeometry();
            PageSettingsPreviewChanged?.Invoke(this,
                new WriterRulerPageSettingsEventArgs(preview));
        }
        catch (ArgumentException)
        {
            // The geometry is already bounded, but retain the last valid preview if a custom
            // page model rejects a pathological DPI-rounded candidate.
        }
    }

    private void CommitMarginDrag()
    {
        var preview = _previewPageSettings;
        _marginDrag = null;
        _previewPageSettings = null;
        ReleaseCaptureSafely();
        if (preview is null || preview == _pageSettings)
        {
            RefreshGeometry();
            return;
        }

        _pageSettings = preview;
        RefreshGeometry();
        PageSettingsCommitted?.Invoke(this, new WriterRulerPageSettingsEventArgs(preview));
    }

    private void CancelMarginDrag()
    {
        _marginDrag = null;
        _previewPageSettings = null;
        ReleaseCaptureSafely();
        RefreshGeometry();
        PageSettingsDragCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void CancelIndentDrag()
    {
        var drag = _indentDrag;
        _indentDrag = null;
        if (drag is null)
            return;
        drag.Cancel();
        ReleaseCaptureSafely();
        ParagraphIndentDragCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateIndentDrag(double renderedX)
    {
        if (_indentDrag is null)
            return;
        var logical = WriterRulerGeometry.ToLogicalContentDip(_layout, renderedX);
        _indentDrag.Update(logical);
        RefreshGeometry();
        InvalidateVisual();
    }

    private WriterRulerIndentation ReadIndentation() =>
        CanRenderParagraphMarkers ? _editing?.ReadRulerIndentation() ?? WriterRulerIndentation.Empty
        : WriterRulerIndentation.Empty;

    private bool CanRenderParagraphMarkers =>
        _canEditParagraphs && (_editing is null || !_editing.HasMixedRulerIndentation);

    private double? GetRenderedMarkerPosition(WriterRulerIndentMarker marker)
    {
        if (_indentDrag?.Marker == marker && _indentDrag.HasPreview)
        {
            var preview = _layout.ContentStartDip +
                _indentDrag.PreviewMarkerPositionDip * _layout.Scale;
            return preview >= _layout.PageOriginDip - 0.5 && preview <= _layout.PageEndDip + 0.5
                ? preview
                : null;
        }
        return _layout.GetMarkerPosition(marker);
    }

    internal WriterRulerIndentMarker? HitTestIndentMarkerAt(double x, double y) =>
        HitTestIndentMarker(x, y);

    internal bool HitTestMarginEdgeAt(double x, double y, out WriterRulerMarginEdge edge) =>
        TryHitMarginEdge(x, y, out edge);

    private WriterRulerIndentMarker? HitTestIndentMarker(double x, double y)
    {
        if (!CanRenderParagraphMarkers || !double.IsFinite(x) || !double.IsFinite(y) ||
            y < 0 || y > RulerHeight)
            return null;

        if (y < FirstLineMarkerBandEnd && IsMarkerNear(WriterRulerIndentMarker.FirstLine, x))
            return WriterRulerIndentMarker.FirstLine;
        if (y < FirstLineMarkerBandEnd && IsMarkerNear(WriterRulerIndentMarker.Right, x))
            return WriterRulerIndentMarker.Right;
        if (y >= LeftMarkerBandStart && y < LeftMarkerBandEnd &&
            IsMarkerNear(WriterRulerIndentMarker.Left, x))
            return WriterRulerIndentMarker.Left;
        if (y >= HangingMarkerBandStart && IsMarkerNear(WriterRulerIndentMarker.Hanging, x))
            return WriterRulerIndentMarker.Hanging;
        return null;
    }

    private bool IsMarkerNear(WriterRulerIndentMarker marker, double x)
    {
        var position = _layout.GetMarkerPosition(marker);
        return position is not null && Math.Abs(position.Value - x) <= HitTolerance;
    }

    private bool TryHitMarginEdge(double x, double y, out WriterRulerMarginEdge edge)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) ||
            y < MarginEdgeBandStart || y >= MarginEdgeBandEnd)
        {
            edge = default;
            return false;
        }

        var leftDistance = Math.Abs(_layout.ContentStartDip - x);
        var rightDistance = Math.Abs(_layout.ContentEndDip - x);
        if (leftDistance <= HitTolerance && leftDistance <= rightDistance)
        {
            edge = WriterRulerMarginEdge.Left;
            return true;
        }
        if (rightDistance <= HitTolerance)
        {
            edge = WriterRulerMarginEdge.Right;
            return true;
        }
        edge = default;
        return false;
    }

    private bool TryGetPaperOrigin(out Point point)
    {
        point = default;
        if (_paperCanvas is null || !IsLoaded)
            return false;
        try
        {
            var border = _paperCanvas.BorderThickness;
            point = _paperCanvas.TranslatePoint(new Point(border.Left, border.Top), this);
            return double.IsFinite(point.X) && double.IsFinite(point.Y);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private Brush ResolveBrush(object key, Brush fallback) =>
        TryFindResource(key) as Brush ?? fallback;

    private void DrawMarker(DrawingContext drawingContext, double? position, Brush brush,
        WriterRulerIndentMarker marker)
    {
        if (position is null || position < -HitTolerance || position > ActualWidth + HitTolerance)
            return;
        var x = position.Value;
        switch (marker)
        {
            case WriterRulerIndentMarker.FirstLine:
                DrawTriangle(drawingContext, x, apexY: 1, baseY: 8, brush);
                break;
            case WriterRulerIndentMarker.Hanging:
                DrawTriangle(drawingContext, x, apexY: RulerHeight - 1, baseY: 24, brush);
                break;
            case WriterRulerIndentMarker.Left:
                drawingContext.DrawRectangle(brush, null, new Rect(x - 4, 18, 8, 6));
                break;
            case WriterRulerIndentMarker.Right:
                DrawDiamond(drawingContext, x, 5, 4, brush);
                break;
        }
    }

    private static void DrawTriangle(DrawingContext drawingContext, double x, double apexY,
        double baseY, Brush brush)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(x, apexY), true, true);
            context.LineTo(new Point(x - 5, baseY), true, false);
            context.LineTo(new Point(x + 5, baseY), true, false);
        }
        if (geometry.CanFreeze)
            geometry.Freeze();
        drawingContext.DrawGeometry(brush, null, geometry);
    }

    private static void DrawDiamond(DrawingContext drawingContext, double x, double centerY,
        double halfSize, Brush brush)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(x, centerY - halfSize), true, true);
            context.LineTo(new Point(x + halfSize, centerY), true, false);
            context.LineTo(new Point(x, centerY + halfSize), true, false);
            context.LineTo(new Point(x - halfSize, centerY), true, false);
        }
        if (geometry.CanFreeze)
            geometry.Freeze();
        drawingContext.DrawGeometry(brush, null, geometry);
    }

    private void ReleaseCaptureSafely()
    {
        if (!IsMouseCaptured)
            return;
        _endingCapture = true;
        try { ReleaseMouseCapture(); }
        finally { _endingCapture = false; }
    }

    private void DetachInputs()
    {
        if (_viewport is not null)
            _viewport.ScrollChanged -= OnViewportChanged;
        if (_paperCanvas is not null)
            _paperCanvas.LayoutUpdated -= OnPaperLayoutUpdated;
        if (_editor is not null)
        {
            _editor.SelectionChanged -= OnEditorSelectionChanged;
            _editor.TextChanged -= OnEditorTextChanged;
        }
    }

    private void OnViewportChanged(object sender, ScrollChangedEventArgs e) => RefreshGeometry();

    private void OnPaperLayoutUpdated(object? sender, EventArgs e) => RefreshGeometry();

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e) => RefreshGeometry();

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e) => RefreshGeometry();

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible)
            CancelActiveDrags();
        InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => CancelActiveDrags();

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WriterRuler));
    }

    private sealed record MarginDragState(DocumentPageSettings OriginalSettings,
        WriterRulerMarginEdge Edge);

    private sealed class WriterRulerAutomationPeer(WriterRuler owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(WriterRuler);

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            AutomationControlType.Custom;
    }
}
