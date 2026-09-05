using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Pagination;

/// <summary>
/// Captures the authoritative live editor into immutable generations and publishes only current
/// dedicated-STA results to the diagnostic compositor.
/// </summary>
internal sealed class WriterPaginatedDiagnosticController : IDisposable
{
    private readonly RichTextBox _editor;
    private readonly WriterPaginatedDiagnosticSurface _surface;
    private readonly WriterDedicatedPaginationEngine _engine;
    private readonly DispatcherTimer _captureTimer;
    private readonly DispatcherTimer _overlayTimer;
    private readonly Dictionary<TextElement, long> _objectIdentities =
        new(ReferenceEqualityComparer.Instance);
    private Dictionary<long, SourceObject> _currentObjects = new();
    private DocumentPageSettings _settings;
    private FlowDocument _document;
    private ImmutableArray<byte> _cachedPackage;
    private ImmutableArray<WriterPaginationObjectCapture> _cachedObjects;
    private long _cachedContentVersion = -1;
    private long _contentVersion;
    private long _nextObjectIdentity;
    private long _documentIdentity = 1;
    private long _layoutIdentity;
    private int _visiblePage;
    private int _scrollDirection = 1;
    private ActiveResize? _activeResize;
    private bool _handlingResizeRequest;
    private bool _disposed;

    internal WriterPaginatedDiagnosticController(
        RichTextBox editor,
        WriterPaginatedDiagnosticSurface surface,
        DocumentPageSettings settings,
        int pageCacheLimit = WriterDedicatedPaginationEngine.DefaultPageCacheLimit,
        long cacheByteLimit = WriterDedicatedPaginationEngine.DefaultCacheByteLimit)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _document = editor.Document;
        _engine = new WriterDedicatedPaginationEngine(pageCacheLimit, cacheByteLimit);
        _surface.PageCacheLimit = pageCacheLimit;
        _captureTimer = new DispatcherTimer(DispatcherPriority.Background, editor.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(90)
        };
        _captureTimer.Tick += OnCaptureTimerTick;
        _overlayTimer = new DispatcherTimer(DispatcherPriority.ContextIdle, editor.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(750)
        };
        _overlayTimer.Tick += OnOverlayTimerTick;
        _overlayTimer.Start();
        _editor.TextChanged += OnEditorTextChanged;
        _editor.SelectionChanged += OnEditorSelectionChanged;
        _surface.PageWindowRequested += OnPageWindowRequested;
        _surface.InteractionRequested += OnInteractionRequested;
        _surface.ResizeRequested += OnResizeRequested;
        _surface.DpiScaleChanged += OnDpiScaleChanged;
        InvalidateLayoutAndSchedule(immediate: true);
    }

    internal long RequestedGeneration { get; private set; }
    internal long PublishedGeneration { get; private set; }
    internal long LayoutIdentity => _layoutIdentity;
    internal WriterPaginationLayoutResult? Current { get; private set; }
    internal WriterPaginationLayoutResult? LastVisible { get; private set; }
    internal WriterPaginationLayoutResult? LastNewSession { get; private set; }
    internal long PrefetchSettledGeneration { get; private set; }
    internal double LastCaptureMilliseconds { get; private set; }
    internal double LastEndToEndMilliseconds { get; private set; }
    internal WriterPaginationWorkStatistics WorkStatistics => _engine.Statistics;
    internal WriterPaginationWorkProgress WorkProgress => _engine.Progress;
    internal Func<TextElement, bool>? StructuredObjectActivator { get; set; }
    internal Func<TextElement, WriterPaginationResizeHandleKind, int, int, bool>?
        StructuredResizeStarter { get; set; }
    internal Action<WriterPaginationResizeHandleKind, double, double>?
        StructuredResizeUpdater { get; set; }
    internal Func<bool>? StructuredResizeCommitter { get; set; }
    internal Action? StructuredResizeCanceler { get; set; }

    internal void SetPageSettings(DocumentPageSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (_settings == settings)
            return;
        _settings = settings;
        InvalidateLayoutAndSchedule(immediate: true);
    }

    internal void SetZoom(double zoomPercent) => _surface.ZoomPercent = zoomPercent;

    internal void SetChrome(bool showRuler, bool showMarginGuides,
        Editing.WriterRulerIndentation indentation) =>
        _surface.SetChrome(_settings, showRuler, showMarginGuides, indentation);

    internal void RefreshFormatting()
    {
        _contentVersion++;
        InvalidateLayoutAndSchedule(immediate: false);
    }

    internal void ReplaceDocument(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!ReferenceEquals(document, _editor.Document))
            throw new InvalidOperationException(
                "The diagnostic can only follow the RichTextBox's authoritative document.");
        if (ReferenceEquals(document, _document))
            return;

        CancelActiveResize();
        _document = document;
        _documentIdentity++;
        _contentVersion++;
        _cachedContentVersion = -1;
        _cachedPackage = default;
        _cachedObjects = default;
        _currentObjects.Clear();
        _objectIdentities.Clear();
        _visiblePage = 0;
        Current = null;
        LastVisible = null;
        LastNewSession = null;
        PublishedGeneration = 0;
        InvalidateLayoutAndSchedule(immediate: true);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _captureTimer.Stop();
        _captureTimer.Tick -= OnCaptureTimerTick;
        _overlayTimer.Stop();
        _overlayTimer.Tick -= OnOverlayTimerTick;
        _editor.TextChanged -= OnEditorTextChanged;
        _editor.SelectionChanged -= OnEditorSelectionChanged;
        _surface.PageWindowRequested -= OnPageWindowRequested;
        _surface.InteractionRequested -= OnInteractionRequested;
        _surface.ResizeRequested -= OnResizeRequested;
        _surface.DpiScaleChanged -= OnDpiScaleChanged;
        CancelActiveResize();
        _surface.Clear();
        _engine.Dispose();
    }

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        _contentVersion++;
        if (_activeResize is not null || _handlingResizeRequest)
            return;
        InvalidateLayoutAndSchedule(immediate: false);
    }

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e) =>
        _surface.RefreshOverlays(_editor);

    private void OnDpiScaleChanged() => InvalidateLayoutAndSchedule(immediate: true);

    private void OnOverlayTimerTick(object? sender, EventArgs e)
    {
        _surface.ShowWorkProgress(_engine.Progress, _engine.Statistics);
        _surface.RefreshOverlays(_editor);
    }

    private void OnPageWindowRequested(int pageNumber)
    {
        if (_disposed || pageNumber < 0 || pageNumber == _visiblePage)
            return;
        _scrollDirection = Math.Sign(pageNumber - _visiblePage);
        if (_scrollDirection == 0)
            _scrollDirection = 1;
        _visiblePage = pageNumber;
        ScheduleViewportRequest(immediate: true);
    }

    private void InvalidateLayoutAndSchedule(bool immediate)
    {
        _layoutIdentity++;
        LastVisible = null;
        LastNewSession = null;
        ScheduleRequest(immediate, preservePages: false);
    }

    private void ScheduleViewportRequest(bool immediate) =>
        ScheduleRequest(immediate, preservePages: true);

    private void ScheduleRequest(bool immediate, bool preservePages)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        RequestedGeneration++;
        _surface.Invalidate(RequestedGeneration, _layoutIdentity, _documentIdentity,
            preservePages, _visiblePage);
        _captureTimer.Stop();
        if (immediate)
        {
            CaptureAndQueue(WriterPaginationRequestKind.Visible);
            return;
        }
        _captureTimer.Start();
    }

    private void OnCaptureTimerTick(object? sender, EventArgs e)
    {
        _captureTimer.Stop();
        CaptureAndQueue(WriterPaginationRequestKind.Visible);
    }

    private void CaptureAndQueue(WriterPaginationRequestKind requestKind)
    {
        if (_disposed || !ReferenceEquals(_document, _editor.Document))
            return;
        var generation = RequestedGeneration;
        var documentIdentity = _documentIdentity;
        var document = _document;
        var requestStartedTimestamp = Stopwatch.GetTimestamp();
        var watch = Stopwatch.StartNew();
        if (_cachedContentVersion != _contentVersion || _cachedPackage.IsDefault)
            CaptureTrustedContent(document);
        var dpi = VisualTreeHelper.GetDpi(_surface);
        var interactivePages = BuildInteractivePages(_visiblePage, _scrollDirection,
            Current?.LayoutIdentity == _layoutIdentity ? Current.PageCount : null);
        var requestedPages = requestKind == WriterPaginationRequestKind.Prefetch
            ? BuildPrefetchPages(interactivePages, _visiblePage, _scrollDirection,
                Current?.PageCount)
            : interactivePages;
        if (requestKind == WriterPaginationRequestKind.Prefetch &&
            requestedPages.Length == interactivePages.Length)
        {
            PrefetchSettledGeneration = generation;
            return;
        }
        var capture = new WriterPaginationCapture(generation, _layoutIdentity,
            documentIdentity, _visiblePage, requestKind, interactivePages, requestedPages,
            _cachedPackage, CaptureFormatting(document),
            CapturePageSettings(_settings), dpi.DpiScaleX, dpi.DpiScaleY, _cachedObjects);
        watch.Stop();
        var captureMilliseconds = watch.Elapsed.TotalMilliseconds;
        LastCaptureMilliseconds = captureMilliseconds;
        var completion = _engine.Queue(capture);
        _ = PublishWhenReadyAsync(completion, generation, _layoutIdentity,
            documentIdentity, document, captureMilliseconds, requestStartedTimestamp);
    }

    private async Task PublishWhenReadyAsync(
        Task<WriterPaginationCompletion> completionTask,
        long generation,
        long layoutIdentity,
        long documentIdentity,
        FlowDocument sourceDocument,
        double captureMilliseconds,
        long requestStartedTimestamp)
    {
        try
        {
            var completion = await completionTask.ConfigureAwait(false);
            await _editor.Dispatcher.InvokeAsync(() =>
            {
                if (_disposed || completion.Kind != WriterPaginationCompletionKind.Completed ||
                    completion.Result is not { } result ||
                    generation != RequestedGeneration ||
                    result.Generation != RequestedGeneration ||
                    layoutIdentity != _layoutIdentity ||
                    result.LayoutIdentity != _layoutIdentity ||
                    documentIdentity != _documentIdentity ||
                    result.DocumentIdentity != _documentIdentity ||
                    !ReferenceEquals(sourceDocument, _editor.Document))
                    return;

                Current = result;
                if (result.RequestKind == WriterPaginationRequestKind.Visible)
                    LastVisible = result;
                if (!result.ReusedLayoutSession)
                    LastNewSession = result;
                if (result.RequestKind == WriterPaginationRequestKind.Prefetch)
                    PrefetchSettledGeneration = result.Generation;
                PublishedGeneration = result.Generation;
                _visiblePage = result.VisiblePage;
                LastCaptureMilliseconds = captureMilliseconds;
                LastEndToEndMilliseconds = ElapsedMilliseconds(requestStartedTimestamp);
                _surface.Publish(result, captureMilliseconds, LastEndToEndMilliseconds,
                    _engine.Statistics, _editor);
                if (result.RequestKind == WriterPaginationRequestKind.Visible)
                    CaptureAndQueue(WriterPaginationRequestKind.Prefetch);
            }, DispatcherPriority.DataBind);
        }
        catch (Exception exception)
        {
            if (_disposed)
                return;
            await _editor.Dispatcher.InvokeAsync(() =>
                _surface.ShowFailure(exception.Message), DispatcherPriority.DataBind);
        }
    }

    private static double ElapsedMilliseconds(long startedTimestamp) =>
        (Stopwatch.GetTimestamp() - startedTimestamp) * 1000d / Stopwatch.Frequency;

    private static ImmutableArray<int> BuildInteractivePages(int visiblePage,
        int direction, int? pageCount)
    {
        var normalizedDirection = direction < 0 ? -1 : 1;
        var candidates = new[]
        {
            visiblePage,
            visiblePage + normalizedDirection,
            visiblePage - normalizedDirection
        };
        return candidates.Where(page => page >= 0 &&
                (pageCount is null || page < pageCount.Value))
            .Distinct().ToImmutableArray();
    }

    private static ImmutableArray<int> BuildPrefetchPages(
        ImmutableArray<int> interactivePages, int visiblePage, int direction,
        int? pageCount)
    {
        var normalizedDirection = direction < 0 ? -1 : 1;
        var furthest = normalizedDirection > 0
            ? interactivePages.DefaultIfEmpty(visiblePage).Max()
            : interactivePages.DefaultIfEmpty(visiblePage).Min();
        return interactivePages.Concat(new[]
            {
                furthest + normalizedDirection,
                furthest + normalizedDirection * 2
            })
            .Where(page => page >= 0 &&
                (pageCount is null || page < pageCount.Value))
            .Distinct().ToImmutableArray();
    }

    private void CaptureTrustedContent(FlowDocument document)
    {
        using var stream = new MemoryStream();
        new TextRange(document.ContentStart, document.ContentEnd)
            .Save(stream, DataFormats.XamlPackage);
        _cachedPackage = stream.ToArray().ToImmutableArray();

        var objects = ImmutableArray.CreateBuilder<WriterPaginationObjectCapture>();
        var currentObjects = new Dictionary<long, SourceObject>();
        foreach (var element in EnumerateStructuredObjects(document))
        {
            if (!_objectIdentities.TryGetValue(element, out var identity))
            {
                identity = ++_nextObjectIdentity;
                _objectIdentities[element] = identity;
            }
            var kind = GetObjectKind(element);
            var start = document.ContentStart.GetOffsetToPosition(element.ElementStart);
            var end = document.ContentStart.GetOffsetToPosition(element.ElementEnd);
            objects.Add(new WriterPaginationObjectCapture(identity, kind, start, end));
            currentObjects[identity] = new SourceObject(element, kind, start, end);
        }
        _cachedObjects = objects.ToImmutable();
        _currentObjects = currentObjects;
        _cachedContentVersion = _contentVersion;
    }

    private void OnInteractionRequested(
        WriterPaginationPageInteraction anchor,
        WriterPaginationPageInteraction? moving)
    {
        if (!IsCurrent(anchor) || moving is { } current && !IsCurrent(current))
            return;
        if (moving is null && anchor.ObjectIdentity is { } identity)
        {
            if (!TryGetCurrentStructuredObject(anchor, identity, out var source))
                return;
            var activated = StructuredObjectActivator?.Invoke(source.Element) == true;
            _surface.ShowInteractionStatus(identity, source.Kind, activated);
            if (activated)
            {
                RestoreEditorFocus();
                return;
            }
        }

        var anchorOffset = HitTest(anchor.PageNumber, anchor.PagePoint);
        var movingOffset = moving is { } endpoint
            ? HitTest(endpoint.PageNumber, endpoint.PagePoint)
            : anchorOffset;
        var anchorPosition = GetLiveInsertionPosition(anchorOffset);
        var movingPosition = GetLiveInsertionPosition(movingOffset);
        _editor.Selection.Select(anchorPosition, movingPosition);
        RestoreEditorFocus();
        _surface.RefreshOverlays(_editor);
    }

    private bool OnResizeRequested(WriterPaginationResizeInteraction request)
    {
        if (_disposed)
            return false;
        if (request.Phase == WriterPaginationResizePhase.Start)
        {
            if (_activeResize is not null || !IsCurrent(request) ||
                !_currentObjects.TryGetValue(request.ObjectIdentity, out var source) ||
                source.Kind != request.ObjectKind || source.Element.Parent is null ||
                StructuredResizeStarter is null)
                return false;
            _handlingResizeRequest = true;
            try
            {
                if (!StructuredResizeStarter(source.Element, request.Handle,
                        request.HandleIndex, request.RowGroupIndex))
                    return false;
                _activeResize = new ActiveResize(request.Generation,
                    request.DocumentIdentity, request.ObjectIdentity,
                    request.ObjectKind, request.Handle, request.HandleIndex,
                    request.RowGroupIndex);
                return true;
            }
            finally
            {
                _handlingResizeRequest = false;
            }
        }

        if (_activeResize is not { } active || !active.Matches(request))
            return false;
        _handlingResizeRequest = true;
        try
        {
            switch (request.Phase)
            {
                case WriterPaginationResizePhase.Update:
                    StructuredResizeUpdater?.Invoke(request.Handle,
                        request.DeltaX, request.DeltaY);
                    return true;
                case WriterPaginationResizePhase.Commit:
                {
                    _activeResize = null;
                    var committed = StructuredResizeCommitter?.Invoke() == true;
                    _surface.ShowResizeStatus(request.ObjectKind, committed);
                    RestoreEditorFocus();
                    if (committed)
                        InvalidateLayoutAndSchedule(immediate: true);
                    return committed;
                }
                case WriterPaginationResizePhase.Cancel:
                    _activeResize = null;
                    StructuredResizeCanceler?.Invoke();
                    _surface.ShowResizeStatus(request.ObjectKind, committed: false,
                        cancelled: true);
                    RestoreEditorFocus();
                    return true;
                default:
                    return false;
            }
        }
        finally
        {
            _handlingResizeRequest = false;
        }
    }

    private bool IsCurrent(WriterPaginationResizeInteraction interaction)
    {
        if (Current is not { } result || interaction.Generation != RequestedGeneration ||
            interaction.Generation != PublishedGeneration ||
            interaction.DocumentIdentity != _documentIdentity ||
            !result.MappedPages.Contains(interaction.PageNumber) ||
            !result.StructuredObjects.Any(item => item.PageNumber == interaction.PageNumber &&
                item.ObjectIdentity == interaction.ObjectIdentity &&
                item.Kind == interaction.ObjectKind))
            return false;
        if (interaction.ObjectKind != WriterPaginationObjectKind.Table)
            return interaction.HandleIndex == -1 && interaction.RowGroupIndex == -1;
        var table = result.Tables.FirstOrDefault(item =>
            item.PageNumber == interaction.PageNumber &&
            item.ObjectIdentity == interaction.ObjectIdentity);
        if (table is null)
            return false;
        return interaction.Handle switch
        {
            WriterPaginationResizeHandleKind.TableColumn =>
                interaction.RowGroupIndex == -1 && interaction.HandleIndex >= 0 &&
                interaction.HandleIndex + 1 < table.ColumnBoundaries.Length,
            WriterPaginationResizeHandleKind.TableRow =>
                interaction.HandleIndex >= 0 && interaction.RowGroupIndex >= 0 &&
                table.RowBoundaries.Any(row => row.RowGroupIndex == interaction.RowGroupIndex &&
                    row.RowIndex == interaction.HandleIndex),
            WriterPaginationResizeHandleKind.TableOverall => table.IsLastFragment &&
                table.HasTrustedColumnBoundaries &&
                interaction.HandleIndex == -1 && interaction.RowGroupIndex == -1,
            _ => false
        };
    }

    private void CancelActiveResize()
    {
        if (_activeResize is null)
            return;
        _activeResize = null;
        _handlingResizeRequest = true;
        try
        {
            StructuredResizeCanceler?.Invoke();
        }
        finally
        {
            _handlingResizeRequest = false;
        }
    }

    private bool TryGetCurrentStructuredObject(
        WriterPaginationPageInteraction interaction,
        long objectIdentity,
        out SourceObject source)
    {
        source = null!;
        if (interaction.ObjectKind is not { } kind ||
            !_currentObjects.TryGetValue(objectIdentity, out var candidate) ||
            candidate.Kind != kind || candidate.Element.Parent is null ||
            !ReferenceEquals(_document, _editor.Document) ||
            _document.ContentStart.GetOffsetToPosition(candidate.Element.ElementStart) != candidate.StartOffset)
            return false;
        source = candidate;
        return true;
    }

    private bool IsCurrent(WriterPaginationPageInteraction interaction) =>
        Current is { } result && interaction.Generation == RequestedGeneration &&
        interaction.Generation == PublishedGeneration &&
        interaction.DocumentIdentity == _documentIdentity &&
        result.MappedPages.Contains(interaction.PageNumber);

    private int HitTest(int pageNumber, Point point)
    {
        var entries = Current!.Insertions.Where(item => item.PageNumber == pageNumber);
        var nearest = entries.MinBy(item => DistanceSquared(item.Rectangle, point));
        if (nearest == default && !entries.Any())
            throw new InvalidOperationException($"Page {pageNumber + 1} has no insertion geometry.");
        return nearest.SourceOffset;
    }

    private TextPointer GetLiveInsertionPosition(int sourceOffset)
    {
        var position = _document.ContentStart.GetPositionAtOffset(sourceOffset,
            LogicalDirection.Forward) ?? throw new InvalidOperationException(
                $"Live offset {sourceOffset} is outside the authoritative document.");
        var insertion = position.GetInsertionPosition(LogicalDirection.Forward);
        if (insertion is null || _document.ContentStart.GetOffsetToPosition(insertion) != sourceOffset)
            throw new InvalidOperationException(
                $"Clone offset {sourceOffset} is not the same live insertion position.");
        return insertion;
    }

    private void RestoreEditorFocus()
    {
        if (Window.GetWindow(_editor) is { } window)
            FocusManager.SetFocusedElement(window, _editor);
        _editor.Focus();
        Keyboard.Focus(_editor);
    }

    private static double DistanceSquared(WriterPaginationRectangle rect, Point point)
    {
        var x = Math.Abs(point.X - rect.X);
        var y = point.Y < rect.Y
            ? rect.Y - point.Y
            : point.Y > rect.Y + rect.Height
                ? point.Y - rect.Y - rect.Height
                : 0;
        return x * x + y * y * 16;
    }

    private static IEnumerable<TextElement> EnumerateStructuredObjects(FlowDocument document)
    {
        for (var position = document.ContentStart;
             position is not null && position.CompareTo(document.ContentEnd) < 0;
             position = position.GetNextContextPosition(LogicalDirection.Forward))
        {
            if (position.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.ElementStart ||
                position.GetAdjacentElement(LogicalDirection.Forward) is not TextElement element)
                continue;
            if (element is Table or Hyperlink or InlineUIContainer { Child: Image })
                yield return element;
        }
    }

    private static WriterPaginationObjectKind GetObjectKind(TextElement element) => element switch
    {
        Table => WriterPaginationObjectKind.Table,
        InlineUIContainer { Child: Image } => WriterPaginationObjectKind.Picture,
        Hyperlink => WriterPaginationObjectKind.Hyperlink,
        _ => throw new ArgumentOutOfRangeException(nameof(element))
    };

    private static WriterPaginationFormatting CaptureFormatting(FlowDocument document) =>
        new(document.FontFamily.Source, document.FontSize,
            document.FontWeight.ToOpenTypeWeight(), document.FontStretch.ToOpenTypeStretch(),
            document.Language.IetfLanguageTag, document.FlowDirection, document.TextAlignment,
            document.LineHeight, document.LineStackingStrategy,
            document.IsHyphenationEnabled, document.IsOptimalParagraphEnabled,
            SaveXaml(document.Background), SaveXaml(document.Foreground),
            SaveXaml(document.TextEffects), SaveXaml(document.ColumnRuleBrush),
            document.ColumnRuleWidth);

    private static string? SaveXaml(object? value) => value is null ? null : XamlWriter.Save(value);

    private static WriterPaginationPageSettings CapturePageSettings(DocumentPageSettings settings) =>
        new(settings.WidthDip, settings.HeightDip, settings.ContentWidthDip,
            settings.Margins.LeftDip, settings.Margins.TopDip,
            settings.Margins.RightDip, settings.Margins.BottomDip);

    private sealed record SourceObject(
        TextElement Element,
        WriterPaginationObjectKind Kind,
        int StartOffset,
        int EndOffset);

    private sealed record ActiveResize(
        long Generation,
        long DocumentIdentity,
        long ObjectIdentity,
        WriterPaginationObjectKind ObjectKind,
        WriterPaginationResizeHandleKind Handle,
        int HandleIndex,
        int RowGroupIndex)
    {
        internal bool Matches(WriterPaginationResizeInteraction interaction) =>
            interaction.Generation == Generation &&
            interaction.DocumentIdentity == DocumentIdentity &&
            interaction.ObjectIdentity == ObjectIdentity &&
            interaction.ObjectKind == ObjectKind &&
            interaction.Handle == Handle && interaction.HandleIndex == HandleIndex &&
            interaction.RowGroupIndex == RowGroupIndex;
    }
}
