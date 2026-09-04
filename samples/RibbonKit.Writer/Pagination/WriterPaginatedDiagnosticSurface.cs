using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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
    private readonly Dictionary<int, Border> _pageFrames = new();
    private readonly HashSet<int> _placeholderPages = new();
    private WriterPaginationLayoutResult? _result;
    private RichTextBox? _editor;
    private long _requestedGeneration;
    private long _layoutIdentity;
    private long _documentIdentity;
    private double _zoomPercent = 100;
    private int _requestedPage;
    private WriterPaginationPageInteraction? _dragAnchor;
    private ResizeDrag? _resizeDrag;
    private ResizeHandleDescriptor? _keyboardResizeTarget;
    private long? _selectedObjectIdentity;
    private WriterPaginationObjectKind? _selectedObjectKind;
    private DocumentPageSettings? _chromeSettings;
    private WriterRulerIndentation _rulerIndentation;
    private bool _showRuler;
    private bool _showMarginGuides;
    private Window? _hostWindow;
    private string? _interactionStatus;
    private ImmutableArray<int> _spellingCandidateOffsets;
    private readonly List<(int Start, int End)> _spellingRanges = new();
    private long _spellingGeneration;
    private long _spellingDocumentIdentity;
    private int _spellingCandidateIndex;
    private int _releasedPageFrameCount;

    internal WriterPaginatedDiagnosticSurface()
    {
        Background = new SolidColorBrush(Color.FromRgb(229, 232, 235));
        AutomationProperties.SetAutomationId(this, "PaginatedEditingDiagnostic");
        AutomationProperties.SetName(this, "Opt-in paginated editing diagnostic");
        AutomationProperties.SetHelpText(this,
            "After selecting a table or picture, press Control+Alt+R to enter keyboard resize, " +
            "Tab to choose a handle, Enter to start, arrows to resize, and Enter or Escape to finish.");

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
        AutomationProperties.SetAutomationId(_statusText, "PaginationDiagnosticStatus");
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
    internal IReadOnlyCollection<int> PlaceholderPages => _placeholderPages;
    internal int ReleasedPageFrameCount => _releasedPageFrameCount;
    internal int RulerElementCount => _rulerCanvas.Children.Count;
    internal int MarginGuideCount => _overlayCanvases.Values.Sum(canvas =>
        canvas.Children.OfType<FrameworkElement>().Count(element =>
            Equals(element.Tag, "pagination-margin-guide")));
    internal int ResizeHandleCount => _overlayCanvases.Values.Sum(canvas =>
        canvas.Children.OfType<FrameworkElement>().Count(element =>
            element.Tag is string tag && tag.StartsWith("pagination-resize-",
                StringComparison.Ordinal)));
    internal string StatusTextForTesting => _statusText.Text;
    internal IReadOnlyList<(int Start, int End)> SpellingRangesForTesting =>
        _spellingRanges;
    internal IReadOnlyList<(int PageNumber, Rect Bounds)> SpellingOverlayBoundsForTesting =>
        _overlayCanvases.SelectMany(entry => entry.Value.Children
            .OfType<FrameworkElement>()
            .Where(element => Equals(element.Tag, "pagination-spelling"))
            .Select(element => (entry.Key, new Rect(Canvas.GetLeft(element),
                Canvas.GetTop(element), element.Width, element.Height))))
            .ToArray();
    internal string? KeyboardResizeTargetNameForTesting =>
        _keyboardResizeTarget is { } target ? GetResizeHandleName(target) : null;

    internal bool IsPageInteractiveForTesting(int pageNumber) =>
        _result is { } result && result.Generation == _requestedGeneration &&
        result.LayoutIdentity == _layoutIdentity &&
        result.DocumentIdentity == _documentIdentity &&
        result.MappedPages.Contains(pageNumber) &&
        _overlayCanvases.ContainsKey(pageNumber) &&
        !_placeholderPages.Contains(pageNumber);

    internal bool CanCreateInteractionForTesting(int pageNumber)
    {
        if (_result is not { } result)
            return false;
        var scale = _zoomPercent / 100d;
        return TryCreateInteraction(pageNumber,
            new Point(result.PageSettings.WidthDip * scale / 2,
                result.PageSettings.HeightDip * scale / 2), out _);
    }

    internal void RaiseDpiScaleChangedForTesting() => DpiScaleChanged?.Invoke();

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
        WriterPaginationResizeHandleKind handle, int handleIndex = -1,
        int rowGroupIndex = -1)
    {
        var interaction = CaptureObjectInteractionForTesting(kind);
        ShowInteractionStatus(interaction.ObjectIdentity!.Value, kind, activated: true);
        if (!TryFindResizeHandle(interaction.ObjectIdentity.Value, kind, handle,
                handleIndex, rowGroupIndex, out var descriptor))
            return false;
        return TryBeginResize(descriptor.PageNumber, descriptor.Center, handle,
            handleIndex, rowGroupIndex);
    }

    internal bool UpdateResizeForTesting(double deltaX, double deltaY) =>
        UpdateResize(new Vector(deltaX, deltaY));

    internal bool CompleteResizeForTesting() => CompleteResize();

    internal bool BeginKeyboardResizeForTesting(WriterPaginationObjectKind kind,
        WriterPaginationResizeHandleKind handle, int handleIndex = -1,
        int rowGroupIndex = -1)
    {
        var interaction = CaptureObjectInteractionForTesting(kind);
        ShowInteractionStatus(interaction.ObjectIdentity!.Value, kind, activated: true);
        if (!TryFindResizeHandle(interaction.ObjectIdentity.Value, kind, handle,
                handleIndex, rowGroupIndex, out var descriptor))
            return false;
        return TryBeginResize(descriptor.PageNumber, descriptor.Center, handle,
            handleIndex, rowGroupIndex, isKeyboard: true);
    }

    internal bool ApplyKeyboardResizeKeyForTesting(Key key, ModifierKeys modifiers =
        ModifierKeys.None) => ApplyKeyboardResizeKey(key, modifiers);

    internal bool ApplyHostKeyForTesting(Key key, ModifierKeys modifiers =
        ModifierKeys.None) => TryHandleHostKey(key, modifiers);

    internal static Key GetEffectiveKeyForTesting(Key key, Key systemKey) =>
        key == Key.System ? systemKey : key;

    internal bool ApplyResizeRequestForTesting(WriterPaginationResizeInteraction request) =>
        ResizeRequested?.Invoke(request) == true;

    internal IReadOnlyList<AutomationPeer> ResizeHandlePeersForTesting() =>
        _overlayCanvases.Values.SelectMany(canvas => canvas.Children
                .OfType<ResizeHandleElement>())
            .Select(element => UIElementAutomationPeer.CreatePeerForElement(element))
            .Where(peer => peer is not null)
            .Cast<AutomationPeer>()
            .ToArray();

    internal IReadOnlyList<AutomationPeer> StructuredObjectPeersForTesting() =>
        _overlayCanvases.Values.SelectMany(canvas => canvas.Children
                .OfType<StructuredObjectElement>())
            .Select(element => UIElementAutomationPeer.CreatePeerForElement(element))
            .Where(peer => peer is not null)
            .Cast<AutomationPeer>()
            .ToArray();

    internal static Size GetLogicalHandleSizeForTesting(double screenSizeDip,
        double zoomPercent, DpiScale dpi) => GetLogicalHandleSize(
            screenSizeDip, zoomPercent, dpi);

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
                RelayoutPages();
                RefreshOverlays(_editor);
                ScrollToPage(centerPage);
            }
        }
    }

    internal void Invalidate(long generation, long layoutIdentity,
        long documentIdentity, bool preservePages, int requestedPage)
    {
        CancelActiveResize();
        _keyboardResizeTarget = null;
        if (documentIdentity != _documentIdentity)
        {
            _selectedObjectIdentity = null;
            _selectedObjectKind = null;
        }
        if (!preservePages || layoutIdentity != _layoutIdentity)
        {
            _result = null;
            ClearPageVisuals();
        }
        _requestedGeneration = generation;
        _layoutIdentity = layoutIdentity;
        _documentIdentity = documentIdentity;
        ResetSpellingScan();
        if (preservePages)
            EnsureLoadingPlaceholder(requestedPage);
        SetStatus($"Paginated diagnostic: updating generation {generation:N0}…");
        RefreshOverlays(null);
    }

    internal void Publish(WriterPaginationLayoutResult result,
        double captureMilliseconds, double endToEndMilliseconds,
        WriterPaginationWorkStatistics statistics,
        RichTextBox editor)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(editor);
        if (result.Generation != _requestedGeneration ||
            result.LayoutIdentity != _layoutIdentity ||
            result.DocumentIdentity != _documentIdentity)
            return;
        _result = result;
        _editor = editor;
        ResetSpellingScan();
        _statusBorder.Background = new SolidColorBrush(Color.FromArgb(220, 36, 43, 50));
        _requestedPage = result.VisiblePage;
        MergePages(result);
        SetStatus($"Diagnostic · document {result.DocumentIdentity:N0} · " +
            $"generation {result.Generation:N0} · " +
            $"page {result.VisiblePage + 1:N0}/{result.PageCount:N0} · " +
            $"{(result.RequestKind == WriterPaginationRequestKind.Prefetch ? "prefetch" : "visible")} · " +
            $"session {(result.ReusedLayoutSession ? "reused" : "new")} · " +
            $"cache {result.RetainedPages.Length:N0}/{WriterDedicatedPaginationEngine.DefaultPageCacheLimit:N0} " +
            $"({result.CachedBytes / 1024d / 1024d:0.0} MB total, " +
            $"{result.CachedDecodedBytes / 1024d / 1024d:0.0} MB decoded, " +
            $"hit/miss {result.CacheHitCount:N0}/{result.CacheMissCount:N0}, " +
            $"evicted {result.EvictedPageCount:N0}) · " +
            $"capture {captureMilliseconds:0.#} ms · layout {result.WorkerMilliseconds:0.#} ms · " +
            $"phases L/C/S/G/R {result.PhaseTimings.PackageLoadMilliseconds:0.#}/" +
            $"{result.PhaseTimings.PageCountMilliseconds:0.#}/" +
            $"{result.PhaseTimings.PageStartsMilliseconds:0.#}/" +
            $"{result.PhaseTimings.InsertionGeometryMilliseconds:0.#}/" +
            $"{result.PhaseTimings.RasterizationMilliseconds:0.#} ms · " +
            $"end {endToEndMilliseconds:0.#} ms · " +
            $"work {statistics.CompletedCount:N0}/{statistics.StartedCount:N0} · " +
            $"cancelled {statistics.CanceledActiveCount:N0} · " +
            $"coalesced {statistics.SupersededPendingCount:N0}");
        _interactionStatus = null;
        RefreshOverlays(editor);
    }

    internal void ShowWorkProgress(WriterPaginationWorkProgress progress,
        WriterPaginationWorkStatistics statistics)
    {
        if (progress.Phase == WriterPaginationWorkPhase.Idle ||
            progress.Generation <= 0 || _requestedGeneration <= 0)
            return;

        var generationText = progress.Generation == _requestedGeneration
            ? $"generation {progress.Generation:N0}"
            : $"active {progress.Generation:N0} → requested {_requestedGeneration:N0}";
        SetStatus($"Paginated diagnostic · {generationText} · " +
            $"{FormatPhase(progress.Phase)} {progress.PhaseElapsedMilliseconds:0.#} ms · " +
            $"work {statistics.CompletedCount:N0}/{statistics.StartedCount:N0} · " +
            $"cancelled {statistics.CanceledActiveCount:N0} · " +
            $"coalesced {statistics.SupersededPendingCount:N0}");
    }

    private static string FormatPhase(WriterPaginationWorkPhase phase) => phase switch
    {
        WriterPaginationWorkPhase.PackageLoad => "package load",
        WriterPaginationWorkPhase.Formatting => "formatting",
        WriterPaginationWorkPhase.PageCount => "page count",
        WriterPaginationWorkPhase.PageStarts => "page starts",
        WriterPaginationWorkPhase.ObjectMapping => "object mapping",
        WriterPaginationWorkPhase.ViewerRealization => "page realization",
        WriterPaginationWorkPhase.InsertionGeometry => "insertion geometry",
        WriterPaginationWorkPhase.Rasterization => "rasterization",
        WriterPaginationWorkPhase.StructuredGeometry => "structured geometry",
        _ => "idle"
    };

    internal void ShowInteractionStatus(long objectIdentity,
        WriterPaginationObjectKind kind, bool activated)
    {
        if (activated)
        {
            _selectedObjectIdentity = objectIdentity;
            _selectedObjectKind = kind;
            _keyboardResizeTarget = null;
            RefreshOverlays(_editor);
        }
        _interactionStatus = activated
            ? $"{kind.ToString().ToLowerInvariant()} selected"
            : $"{kind.ToString().ToLowerInvariant()} rejected";
        if (_result is { } result)
            SetStatus($"Diagnostic · generation {result.Generation:N0} · " +
                $"page {result.VisiblePage + 1:N0}/{result.PageCount:N0} · " +
                _interactionStatus);
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
            SetStatus($"Diagnostic · document {result.DocumentIdentity:N0} · " +
                $"generation {result.Generation:N0} · {_interactionStatus}");
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
        SetStatus($"Paginated diagnostic failed: {message}");
        _statusBorder.Background = new SolidColorBrush(Color.FromArgb(230, 145, 36, 36));
    }

    private void SetStatus(string status)
    {
        _statusText.Text = status;
        AutomationProperties.SetName(_statusText, status);
        WriterPaginationDiagnosticOptions.WriteTelemetry(status);
    }

    internal void Clear()
    {
        CancelActiveResize();
        _result = null;
        _editor = null;
        _selectedObjectIdentity = null;
        _selectedObjectKind = null;
        ClearPageVisuals();
        ResetSpellingScan();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (!oldDpi.Equals(newDpi))
            DpiScaleChanged?.Invoke();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new PaginationSurfaceAutomationPeer(this);

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
        e.Handled = TryHandleHostKey(GetEffectiveKeyForTesting(e.Key, e.SystemKey),
            Keyboard.Modifiers);
    }

    private bool TryHandleHostKey(Key key, ModifierKeys modifiers)
    {
        if (_resizeDrag is { IsKeyboard: true })
            return ApplyKeyboardResizeKey(key, modifiers);
        if (_resizeDrag is not null)
        {
            if (key != Key.Escape)
                return false;
            CancelActiveResize();
            return true;
        }
        if (_keyboardResizeTarget is { } target)
        {
            if (key == Key.Escape)
            {
                _keyboardResizeTarget = null;
                RefreshOverlays(_editor);
                RestoreEditorKeyboardFocus();
                return true;
            }
            if (key == Key.Tab)
                return CycleKeyboardResizeTarget(modifiers.HasFlag(ModifierKeys.Shift));
            if (key is Key.Enter or Key.Space)
                return BeginKeyboardResize(target);
            return false;
        }
        return key == Key.R && modifiers.HasFlag(ModifierKeys.Control) &&
            modifiers.HasFlag(ModifierKeys.Alt) && ActivateKeyboardResizeNavigation();
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

    private void MergePages(WriterPaginationLayoutResult result)
    {
        var retained = result.RetainedPages.ToHashSet();
        foreach (var pageNumber in _pageFrames.Keys
                     .Where(page => !retained.Contains(page)).ToArray())
            RemovePageFrame(pageNumber);

        foreach (var page in result.Pages)
        {
            if (_pageFrames.ContainsKey(page.PageNumber) &&
                !_placeholderPages.Contains(page.PageNumber))
                continue;
            RemovePageFrame(page.PageNumber);
            AddPageFrame(page.PageNumber, DecodePage(page.PngBytes));
        }
        RelayoutPages();
        RebuildRuler();
        RefreshOverlays(_editor);
    }

    private void EnsureLoadingPlaceholder(int pageNumber)
    {
        if (_result is not { } result || pageNumber < 0 || pageNumber >= result.PageCount ||
            _pageFrames.ContainsKey(pageNumber))
            return;
        AddPageFrame(pageNumber, null);
        RelayoutPages();
    }

    private void AddPageFrame(int pageNumber, BitmapSource? bitmap)
    {
        if (_result is not { } result)
            return;
        var pageGrid = new Grid
        {
            Tag = pageNumber,
            Background = Brushes.White,
            Cursor = bitmap is null ? Cursors.Arrow : Cursors.IBeam,
            IsHitTestVisible = bitmap is not null
        };
        if (bitmap is null)
        {
            _placeholderPages.Add(pageNumber);
            pageGrid.Children.Add(new TextBlock
            {
                Text = $"Loading page {pageNumber + 1:N0}…",
                Foreground = new SolidColorBrush(Color.FromRgb(105, 111, 118)),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            });
        }
        else
        {
            var overlay = new Canvas
            {
                Width = result.PageSettings.WidthDip,
                Height = result.PageSettings.HeightDip,
                IsHitTestVisible = false
            };
            _overlayCanvases[pageNumber] = overlay;
            pageGrid.Children.Add(new Image
            {
                Source = bitmap,
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
        }

        var frame = new Border
        {
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(176, 180, 184)),
            BorderThickness = new Thickness(1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.18
            },
            Child = pageGrid,
            Tag = bitmap is null ? "pagination-loading-placeholder" :
                "pagination-page"
        };
        _pageFrames[pageNumber] = frame;
        _pageCanvas.Children.Add(frame);
    }

    private void RemovePageFrame(int pageNumber)
    {
        var releasedRenderedPage = !_placeholderPages.Contains(pageNumber) &&
            _pageFrames.ContainsKey(pageNumber);
        if (_pageFrames.Remove(pageNumber, out var frame))
        {
            _pageCanvas.Children.Remove(frame);
            if (frame.Child is Panel panel)
            {
                foreach (var image in panel.Children.OfType<Image>())
                    image.Source = null;
                panel.Children.Clear();
            }
            frame.Child = null;
            frame.Effect = null;
        }
        if (_overlayCanvases.Remove(pageNumber, out var overlay))
            overlay.Children.Clear();
        _placeholderPages.Remove(pageNumber);
        if (releasedRenderedPage)
            _releasedPageFrameCount++;
    }

    private void ClearPageVisuals()
    {
        foreach (var pageNumber in _pageFrames.Keys.ToArray())
            RemovePageFrame(pageNumber);
        _placeholderPages.Clear();
        _overlayCanvases.Clear();
        _pageCanvas.Children.Clear();
        _pageCanvas.Width = 0;
        _pageCanvas.Height = 0;
    }

    private void RelayoutPages()
    {
        if (_result is not { } result)
            return;
        var scale = _zoomPercent / 100d;
        var pageWidth = result.PageSettings.WidthDip * scale;
        var pageHeight = result.PageSettings.HeightDip * scale;
        var pitch = pageHeight + PageGap;
        _pageCanvas.Width = Math.Max(_scrollViewer.ViewportWidth,
            pageWidth + PageGap * 2);
        _pageCanvas.Height = PageGap + result.PageCount * pitch;
        foreach (var (pageNumber, frame) in _pageFrames)
        {
            frame.Width = pageWidth;
            frame.Height = pageHeight;
            if (frame.Child is FrameworkElement child)
            {
                child.Width = pageWidth;
                child.Height = pageHeight;
            }
            Canvas.SetLeft(frame, Math.Max(PageGap,
                (_pageCanvas.Width - pageWidth) / 2));
            Canvas.SetTop(frame, PageGap + pageNumber * pitch);
        }
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
            AddStructuredObjectAutomationElement(canvas, result, item, rect);
        }
        AddResizeHandles(result);
        AddResizePreview();
    }

    private void AddStructuredObjectAutomationElement(Canvas canvas,
        WriterPaginationLayoutResult result, WriterPaginationObjectGeometry item, Rect rect)
    {
        var name = $"Select {item.Kind.ToString().ToLowerInvariant()} on page " +
            $"{item.PageNumber + 1}";
        var element = new StructuredObjectElement(
            () => InvokeStructuredObject(result.Generation, result.DocumentIdentity,
                item), name)
        {
            Width = Math.Max(1, rect.Width),
            Height = Math.Max(1, rect.Height),
            Tag = $"pagination-object-{item.Kind.ToString().ToLowerInvariant()}"
        };
        AutomationProperties.SetAutomationId(element,
            $"PaginationObject-{item.Kind}-{item.ObjectIdentity}-{item.PageNumber}");
        AutomationProperties.SetName(element, name);
        AutomationProperties.SetHelpText(element,
            "Invoking selects the authoritative object and returns commands to the live editor.");
        Canvas.SetLeft(element, rect.Left);
        Canvas.SetTop(element, rect.Top);
        canvas.Children.Add(element);
    }

    private void InvokeStructuredObject(long generation, long documentIdentity,
        WriterPaginationObjectGeometry item)
    {
        var rect = item.Rectangle.ToRect();
        InteractionRequested?.Invoke(new WriterPaginationPageInteraction(
            generation, documentIdentity, item.PageNumber,
            new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2),
            item.ObjectIdentity, item.Kind), null);
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
        if (drag.Request.Handle == WriterPaginationResizeHandleKind.TableColumn)
        {
            return new Rect(drag.OpeningRect.Location,
                new Size(Math.Max(WriterTableResizeGeometry.MinimumColumnWidth,
                    drag.OpeningRect.Width + drag.Delta.X), drag.OpeningRect.Height));
        }
        if (drag.Request.Handle == WriterPaginationResizeHandleKind.TableRow)
        {
            return new Rect(drag.OpeningRect.Location,
                new Size(drag.OpeningRect.Width,
                    Math.Max(WriterTableResizeGeometry.MinimumRowHeight,
                        drag.OpeningRect.Height + drag.Delta.Y)));
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
        foreach (var descriptor in BuildResizeHandleDescriptors(result, identity, kind))
            AddResizeHandle(descriptor);
    }

    private void AddResizeHandle(ResizeHandleDescriptor descriptor)
    {
        if (!_overlayCanvases.TryGetValue(descriptor.PageNumber, out var canvas))
            return;
        var name = GetResizeHandleName(descriptor);
        var shape = new ResizeHandleElement(
            () => InvokeResizeHandle(descriptor), name,
            _keyboardResizeTarget is { } target && SameHandle(target, descriptor))
        {
            Width = descriptor.Rectangle.Width,
            Height = descriptor.Rectangle.Height,
            Tag = $"pagination-resize-{descriptor.Handle.ToString().ToLowerInvariant()}"
        };
        AutomationProperties.SetAutomationId(shape,
            $"PaginationResize-{descriptor.ObjectIdentity}-{descriptor.PageNumber}-" +
            $"{descriptor.Handle}-{descriptor.RowGroupIndex}-{descriptor.HandleIndex}");
        AutomationProperties.SetName(shape, name);
        AutomationProperties.SetHelpText(shape,
            "Invoke grows by twelve DIPs. Use Control+Alt+R from the live editor for transactional " +
            "keyboard resize without moving native command focus.");
        Canvas.SetLeft(shape, descriptor.Rectangle.Left);
        Canvas.SetTop(shape, descriptor.Rectangle.Top);
        canvas.Children.Add(shape);
    }

    private Rect GetPictureHandleRect(Rect rect,
        WriterPaginationResizeHandleKind handle, double screenSizeDip)
    {
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
        return GetPointHandleRect(new Point(x, y), screenSizeDip);
    }

    private Rect GetPointHandleRect(Point point, double screenSizeDip)
    {
        var size = GetLogicalHandleSize(screenSizeDip, _zoomPercent,
            VisualTreeHelper.GetDpi(this));
        return new Rect(point.X - size.Width / 2, point.Y - size.Height / 2,
            size.Width, size.Height);
    }

    private static Size GetLogicalHandleSize(double screenSizeDip,
        double zoomPercent, DpiScale dpi)
    {
        if (!double.IsFinite(screenSizeDip) || screenSizeDip <= 0 ||
            !double.IsFinite(zoomPercent) || zoomPercent <= 0 ||
            !double.IsFinite(dpi.DpiScaleX) || dpi.DpiScaleX <= 0 ||
            !double.IsFinite(dpi.DpiScaleY) || dpi.DpiScaleY <= 0)
            throw new ArgumentOutOfRangeException(nameof(screenSizeDip));
        var scale = zoomPercent / 100d;
        var alignedWidth = Math.Max(1 / dpi.DpiScaleX,
            Math.Round(screenSizeDip * dpi.DpiScaleX) / dpi.DpiScaleX);
        var alignedHeight = Math.Max(1 / dpi.DpiScaleY,
            Math.Round(screenSizeDip * dpi.DpiScaleY) / dpi.DpiScaleY);
        return new Size(alignedWidth / scale, alignedHeight / scale);
    }

    private IEnumerable<ResizeHandleDescriptor> BuildResizeHandleDescriptors(
        WriterPaginationLayoutResult result, long identity, WriterPaginationObjectKind kind)
    {
        if (kind == WriterPaginationObjectKind.Picture)
        {
            foreach (var geometry in result.StructuredObjects.Where(item =>
                         item.ObjectIdentity == identity && item.Kind == kind &&
                         _overlayCanvases.ContainsKey(item.PageNumber)))
            foreach (var handle in Enum.GetValues<WriterPaginationResizeHandleKind>()
                         .Where(IsPictureHandle))
            {
                yield return new ResizeHandleDescriptor(geometry.PageNumber, identity, kind,
                    handle, -1, -1, GetPictureHandleRect(
                        geometry.Rectangle.ToRect(), handle,
                        WriterTableResizeGeometry.VisualHandleSize));
            }
            yield break;
        }
        if (kind != WriterPaginationObjectKind.Table)
            yield break;
        foreach (var table in result.Tables.Where(item => item.ObjectIdentity == identity &&
                     _overlayCanvases.ContainsKey(item.PageNumber)))
        {
            for (var column = 0; column + 1 < table.ColumnBoundaries.Length; column++)
            {
                yield return new ResizeHandleDescriptor(table.PageNumber, identity, kind,
                    WriterPaginationResizeHandleKind.TableColumn, column, -1,
                    GetPointHandleRect(new Point(table.ColumnBoundaries[column + 1],
                        table.Bounds.Y), WriterTableResizeGeometry.VisualHandleSize));
            }
            foreach (var row in table.RowBoundaries)
            {
                yield return new ResizeHandleDescriptor(table.PageNumber, identity, kind,
                    WriterPaginationResizeHandleKind.TableRow, row.RowIndex,
                    row.RowGroupIndex, GetPointHandleRect(new Point(table.Bounds.X,
                        row.PositionDip), WriterTableResizeGeometry.VisualHandleSize));
            }
            if (!table.IsLastFragment || !table.HasTrustedColumnBoundaries)
                continue;
            var bounds = table.Bounds.ToRect();
            yield return new ResizeHandleDescriptor(table.PageNumber, identity, kind,
                WriterPaginationResizeHandleKind.TableOverall, -1, -1,
                GetPointHandleRect(new Point(bounds.Right, bounds.Bottom),
                    WriterTableResizeGeometry.VisualHandleSize));
        }
    }

    private bool TryFindResizeHandle(long identity, WriterPaginationObjectKind kind,
        WriterPaginationResizeHandleKind handle, int handleIndex, int rowGroupIndex,
        out ResizeHandleDescriptor descriptor)
    {
        descriptor = default;
        if (_result is not { } result)
            return false;
        var match = BuildResizeHandleDescriptors(result, identity, kind)
            .FirstOrDefault(item => item.Handle == handle &&
                item.HandleIndex == handleIndex && item.RowGroupIndex == rowGroupIndex);
        if (match == default)
            return false;
        descriptor = match;
        return true;
    }

    private void InvokeResizeHandle(ResizeHandleDescriptor descriptor)
    {
        if (!TryBeginResize(descriptor.PageNumber, descriptor.Center,
                descriptor.Handle, descriptor.HandleIndex, descriptor.RowGroupIndex))
            return;
        var delta = descriptor.Handle switch
        {
            WriterPaginationResizeHandleKind.PictureTopLeft => new Vector(-12, -12),
            WriterPaginationResizeHandleKind.PictureTop => new Vector(0, -12),
            WriterPaginationResizeHandleKind.PictureTopRight => new Vector(12, -12),
            WriterPaginationResizeHandleKind.PictureLeft => new Vector(-12, 0),
            WriterPaginationResizeHandleKind.PictureRight or
                WriterPaginationResizeHandleKind.TableColumn => new Vector(12, 0),
            WriterPaginationResizeHandleKind.PictureBottomLeft => new Vector(-12, 12),
            WriterPaginationResizeHandleKind.PictureBottom => new Vector(0, 12),
            WriterPaginationResizeHandleKind.TableRow => new Vector(0, 12),
            _ => new Vector(12, 12)
        };
        if (!UpdateResize(delta))
        {
            CancelActiveResize();
            return;
        }
        CompleteResize();
    }

    private bool BeginKeyboardResize(ResizeHandleDescriptor descriptor) =>
        TryBeginResize(descriptor.PageNumber, descriptor.Center, descriptor.Handle,
            descriptor.HandleIndex, descriptor.RowGroupIndex, isKeyboard: true);

    private bool ActivateKeyboardResizeNavigation()
    {
        if (_result is not { } result || _selectedObjectIdentity is not { } identity ||
            _selectedObjectKind is not { } kind)
            return false;
        var handles = BuildResizeHandleDescriptors(result, identity, kind).ToArray();
        if (handles.Length == 0)
            return false;
        _keyboardResizeTarget = handles[0];
        ShowKeyboardResizeTargetStatus(handles[0]);
        RefreshOverlays(_editor);
        return true;
    }

    private bool CycleKeyboardResizeTarget(bool reverse)
    {
        if (_result is not { } result || _selectedObjectIdentity is not { } identity ||
            _selectedObjectKind is not { } kind || _keyboardResizeTarget is not { } current)
            return false;
        var handles = BuildResizeHandleDescriptors(result, identity, kind).ToArray();
        if (handles.Length == 0)
            return false;
        var index = Array.FindIndex(handles, candidate => SameHandle(candidate, current));
        index = index < 0 ? 0 : (index + (reverse ? -1 : 1) + handles.Length) % handles.Length;
        _keyboardResizeTarget = handles[index];
        ShowKeyboardResizeTargetStatus(handles[index]);
        RefreshOverlays(_editor);
        return true;
    }

    private void ShowKeyboardResizeTargetStatus(ResizeHandleDescriptor descriptor)
    {
        if (_result is not { } result)
            return;
        SetStatus($"Diagnostic · generation {result.Generation:N0} · keyboard target · " +
            GetResizeHandleName(descriptor));
    }

    private static bool SameHandle(ResizeHandleDescriptor first,
        ResizeHandleDescriptor second) => first.PageNumber == second.PageNumber &&
        first.ObjectIdentity == second.ObjectIdentity && first.ObjectKind == second.ObjectKind &&
        first.Handle == second.Handle && first.HandleIndex == second.HandleIndex &&
        first.RowGroupIndex == second.RowGroupIndex;

    private bool ApplyKeyboardResizeKey(Key key, ModifierKeys modifiers)
    {
        if (_resizeDrag is not { IsKeyboard: true } drag)
            return false;
        if (key == Key.Escape)
        {
            CancelActiveResize();
            _keyboardResizeTarget = null;
            RestoreEditorKeyboardFocus();
            return true;
        }
        if (key is Key.Enter or Key.Space)
        {
            CompleteResize();
            _keyboardResizeTarget = null;
            RestoreEditorKeyboardFocus();
            return true;
        }

        var step = modifiers.HasFlag(ModifierKeys.Shift) ? 12d : 1d;
        var increment = GetKeyboardResizeIncrement(drag.Request.Handle, key, step);
        if (increment is not { } value)
            return false;
        return UpdateResize(drag.Delta + value);
    }

    private static Vector? GetKeyboardResizeIncrement(
        WriterPaginationResizeHandleKind handle, Key key, double step)
    {
        var allowsHorizontal = handle is WriterPaginationResizeHandleKind.TableColumn or
            WriterPaginationResizeHandleKind.TableOverall or
            WriterPaginationResizeHandleKind.PictureTopLeft or
            WriterPaginationResizeHandleKind.PictureTopRight or
            WriterPaginationResizeHandleKind.PictureRight or
            WriterPaginationResizeHandleKind.PictureBottomRight or
            WriterPaginationResizeHandleKind.PictureBottomLeft or
            WriterPaginationResizeHandleKind.PictureLeft;
        var allowsVertical = handle is WriterPaginationResizeHandleKind.TableRow or
            WriterPaginationResizeHandleKind.TableOverall or
            WriterPaginationResizeHandleKind.PictureTopLeft or
            WriterPaginationResizeHandleKind.PictureTop or
            WriterPaginationResizeHandleKind.PictureTopRight or
            WriterPaginationResizeHandleKind.PictureBottomRight or
            WriterPaginationResizeHandleKind.PictureBottom or
            WriterPaginationResizeHandleKind.PictureBottomLeft;
        return key switch
        {
            Key.Left when allowsHorizontal => new Vector(-step, 0),
            Key.Right when allowsHorizontal => new Vector(step, 0),
            Key.Up when allowsVertical => new Vector(0, -step),
            Key.Down when allowsVertical => new Vector(0, step),
            _ => null
        };
    }

    private void RestoreEditorKeyboardFocus()
    {
        if (_editor is null)
            return;
        if (Window.GetWindow(_editor) is { } window)
            FocusManager.SetFocusedElement(window, _editor);
        _editor.Focus();
        Keyboard.Focus(_editor);
    }

    private static string GetResizeHandleName(ResizeHandleDescriptor descriptor) =>
        descriptor.Handle switch
        {
            WriterPaginationResizeHandleKind.TableColumn =>
                $"Resize table column {descriptor.HandleIndex + 1} on page {descriptor.PageNumber + 1}",
            WriterPaginationResizeHandleKind.TableRow =>
                $"Resize table row group {descriptor.RowGroupIndex + 1}, " +
                $"row {descriptor.HandleIndex + 1} on page {descriptor.PageNumber + 1}",
            WriterPaginationResizeHandleKind.TableOverall =>
                $"Resize entire table on page {descriptor.PageNumber + 1}",
            _ => $"Resize picture {descriptor.Handle.ToString()[7..].ToLowerInvariant()} " +
                $"on page {descriptor.PageNumber + 1}"
        };

    private static bool IsPictureHandle(WriterPaginationResizeHandleKind handle) =>
        handle is >= WriterPaginationResizeHandleKind.PictureTopLeft and
            <= WriterPaginationResizeHandleKind.PictureLeft;

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
        if (!SpellCheck.GetIsEnabled(editor))
            return;
        if (_spellingGeneration != result.Generation ||
            _spellingDocumentIdentity != result.DocumentIdentity ||
            _spellingCandidateOffsets.IsDefault)
        {
            _spellingGeneration = result.Generation;
            _spellingDocumentIdentity = result.DocumentIdentity;
            _spellingCandidateOffsets = BuildSpellingCandidateOffsets(result,
                editor.Document);
            _spellingCandidateIndex = 0;
            _spellingRanges.Clear();
        }

        var watch = Stopwatch.StartNew();
        var processed = 0;
        while (_spellingCandidateIndex < _spellingCandidateOffsets.Length &&
               processed++ < 64 && watch.ElapsedMilliseconds < 8)
        {
            var offset = _spellingCandidateOffsets[_spellingCandidateIndex++];
            var position = editor.Document.ContentStart.GetPositionAtOffset(offset,
                LogicalDirection.Forward);
            var range = position is null ? null : editor.GetSpellingErrorRange(position);
            if (range is null)
                continue;
            var start = editor.Document.ContentStart.GetOffsetToPosition(range.Start);
            var end = editor.Document.ContentStart.GetOffsetToPosition(range.End);
            if (!_spellingRanges.Contains((start, end)))
                _spellingRanges.Add((start, end));
        }

        foreach (var range in _spellingRanges)
            AddRangeOverlay(result, range.Start, range.End, "spelling", Brushes.Red, 1);
    }

    private static ImmutableArray<int> BuildSpellingCandidateOffsets(
        WriterPaginationLayoutResult result, FlowDocument document)
    {
        var candidates = ImmutableArray.CreateBuilder<int>();
        var position = document.ContentStart;
        while (position is not null && position.CompareTo(document.ContentEnd) < 0)
        {
            if (position.GetPointerContext(LogicalDirection.Forward) !=
                TextPointerContext.Text)
            {
                position = position.GetNextContextPosition(LogicalDirection.Forward);
                continue;
            }

            var text = position.GetTextInRun(LogicalDirection.Forward);
            var runOffset = document.ContentStart.GetOffsetToPosition(position);
            for (var index = 0; index < text.Length; index++)
            {
                if (!IsWordCharacter(text[index]) ||
                    index > 0 && IsWordCharacter(text[index - 1]))
                    continue;
                var sourceOffset = runOffset + index;
                if (IsMappedOffset(result, sourceOffset))
                    candidates.Add(sourceOffset);
            }
            position = position.GetPositionAtOffset(text.Length,
                LogicalDirection.Forward);
        }
        return candidates.ToImmutable();
    }

    private static bool IsMappedOffset(WriterPaginationLayoutResult result, int offset)
    {
        foreach (var page in result.MappedPages)
        {
            var start = result.PageStartOffsets[page];
            var end = page + 1 < result.PageStartOffsets.Length
                ? result.PageStartOffsets[page + 1]
                : int.MaxValue;
            if (offset >= start && offset < end)
                return true;
        }
        return false;
    }

    private static bool IsWordCharacter(char character)
    {
        if (char.IsLetterOrDigit(character) || character is '\'' or '\u2019')
            return true;
        return char.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or UnicodeCategory.ConnectorPunctuation;
    }

    private void ResetSpellingScan()
    {
        _spellingGeneration = 0;
        _spellingDocumentIdentity = 0;
        _spellingCandidateOffsets = default;
        _spellingCandidateIndex = 0;
        _spellingRanges.Clear();
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
        if (TryHitResizeHandle(pageNumber, scaledPoint, out var resizeHandle) &&
            TryBeginResize(pageNumber, ToPagePoint(scaledPoint), resizeHandle.Handle,
                resizeHandle.HandleIndex, resizeHandle.RowGroupIndex))
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
        out ResizeHandleDescriptor handle)
    {
        handle = default;
        if (_result is not { } result || _selectedObjectIdentity is not { } identity ||
            _selectedObjectKind is not { } kind)
            return false;
        var point = ToPagePoint(scaledPoint);
        var found = false;
        var nearest = double.PositiveInfinity;
        foreach (var candidate in BuildResizeHandleDescriptors(result, identity, kind)
                     .Where(item => item.PageNumber == pageNumber))
        {
            var center = candidate.Center;
            var hitSize = GetLogicalHandleSize(
                WriterTableResizeGeometry.HandleHitTargetSize, _zoomPercent,
                VisualTreeHelper.GetDpi(this));
            var rect = new Rect(center.X - hitSize.Width / 2,
                center.Y - hitSize.Height / 2, hitSize.Width, hitSize.Height);
            if (!rect.Contains(point))
                continue;
            var distance = (point - center).LengthSquared;
            if (distance >= nearest)
                continue;
            nearest = distance;
            handle = candidate;
            found = true;
        }
        return found;
    }

    private bool TryBeginResize(int pageNumber, Point point,
        WriterPaginationResizeHandleKind handle, int handleIndex = -1,
        int rowGroupIndex = -1, bool isKeyboard = false)
    {
        if (_resizeDrag is not null || _result is not { } result ||
            _selectedObjectIdentity is not { } identity ||
            _selectedObjectKind is not { } kind)
            return false;
        if (!TryFindResizeHandle(identity, kind, handle, handleIndex,
                rowGroupIndex, out var descriptor) || descriptor.PageNumber != pageNumber)
            return false;
        var geometry = result.StructuredObjects.FirstOrDefault(item =>
            item.PageNumber == pageNumber && item.ObjectIdentity == identity && item.Kind == kind);
        var openingRect = kind == WriterPaginationObjectKind.Table
            ? result.Tables.First(item => item.PageNumber == pageNumber &&
                item.ObjectIdentity == identity).Bounds.ToRect()
            : geometry.Rectangle.ToRect();
        var request = new WriterPaginationResizeInteraction(result.Generation,
            result.DocumentIdentity, pageNumber, identity, kind, handle,
            WriterPaginationResizePhase.Start, 0, 0, handleIndex, rowGroupIndex);
        if (ResizeRequested?.Invoke(request) != true)
            return false;
        _resizeDrag = new ResizeDrag(request, point, openingRect, default, isKeyboard);
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
        {
            RelayoutPages();
            RebuildRuler();
        }
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

    private readonly record struct ResizeHandleDescriptor(
        int PageNumber,
        long ObjectIdentity,
        WriterPaginationObjectKind ObjectKind,
        WriterPaginationResizeHandleKind Handle,
        int HandleIndex,
        int RowGroupIndex,
        Rect Rectangle)
    {
        internal Point Center => new(Rectangle.Left + Rectangle.Width / 2,
            Rectangle.Top + Rectangle.Height / 2);
    }

    private sealed class ResizeHandleElement : FrameworkElement
    {
        private readonly Action _invoke;
        private readonly bool _isKeyboardTarget;

        internal ResizeHandleElement(Action invoke, string name, bool isKeyboardTarget)
        {
            _invoke = invoke;
            _isKeyboardTarget = isKeyboardTarget;
            Focusable = false;
            IsHitTestVisible = false;
            AutomationProperties.SetName(this, name);
        }

        internal void Invoke() => _invoke();

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var dpi = VisualTreeHelper.GetDpi(this);
            var thickness = Math.Max(1 / dpi.DpiScaleX, 1);
            drawingContext.DrawRectangle(Brushes.White,
                new Pen(Brushes.DodgerBlue, thickness),
                new Rect(0, 0, ActualWidth, ActualHeight));
            if (_isKeyboardTarget)
                drawingContext.DrawRectangle(null,
                    new Pen(Brushes.Black, thickness),
                    new Rect(thickness, thickness,
                        Math.Max(0, ActualWidth - thickness * 2),
                        Math.Max(0, ActualHeight - thickness * 2)));
        }

        protected override AutomationPeer OnCreateAutomationPeer() =>
            new ResizeHandleAutomationPeer(this);
    }

    private sealed class StructuredObjectElement : FrameworkElement
    {
        private readonly Action _invoke;

        internal StructuredObjectElement(Action invoke, string name)
        {
            _invoke = invoke;
            Focusable = false;
            IsHitTestVisible = false;
            AutomationProperties.SetName(this, name);
        }

        internal void Invoke() => _invoke();

        protected override AutomationPeer OnCreateAutomationPeer() =>
            new StructuredObjectAutomationPeer(this);
    }

    private sealed class StructuredObjectAutomationPeer(StructuredObjectElement owner) :
        FrameworkElementAutomationPeer(owner), IInvokeProvider
    {
        private StructuredObjectElement ObjectOwner => (StructuredObjectElement)Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            AutomationControlType.Button;

        protected override string GetClassNameCore() => "WriterPaginationStructuredObject";

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;

        public override object? GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

        void IInvokeProvider.Invoke() => ObjectOwner.Dispatcher.BeginInvoke(ObjectOwner.Invoke);
    }

    private sealed class ResizeHandleAutomationPeer(ResizeHandleElement owner) :
        FrameworkElementAutomationPeer(owner), IInvokeProvider
    {
        private ResizeHandleElement HandleOwner => (ResizeHandleElement)Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() =>
            AutomationControlType.Button;

        protected override string GetClassNameCore() => "WriterPaginationResizeHandle";

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;

        public override object? GetPattern(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

        void IInvokeProvider.Invoke()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();
            if (HandleOwner.Dispatcher.CheckAccess())
                HandleOwner.Invoke();
            else
                HandleOwner.Dispatcher.Invoke(HandleOwner.Invoke);
        }
    }

    private sealed class PaginationSurfaceAutomationPeer(
        WriterPaginatedDiagnosticSurface owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() =>
            AutomationControlType.Pane;

        protected override string GetClassNameCore() =>
            nameof(WriterPaginatedDiagnosticSurface);

        protected override bool IsControlElementCore() => true;

        protected override bool IsContentElementCore() => true;
    }

    private sealed record ResizeDrag(
        WriterPaginationResizeInteraction Request,
        Point OpeningPoint,
        Rect OpeningRect,
        Vector Delta,
        bool IsKeyboard)
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
