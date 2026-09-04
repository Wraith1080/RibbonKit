using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RibbonKit.Writer.Pagination;

/// <summary>
/// Owns Writer's disposable pagination clones on one persistent STA. No WPF object created here
/// is returned to the editor dispatcher.
/// </summary>
internal sealed class WriterDedicatedPaginationEngine : IDisposable
{
    internal const int DefaultPageCacheLimit = 8;
    internal const long DefaultCacheByteLimit = 64L * 1024 * 1024;

    private readonly object _sync = new();
    private readonly AutoResetEvent _workAvailable = new(false);
    private readonly ManualResetEventSlim _ready = new();
    private readonly Thread _thread;
    private readonly int _pageCacheLimit;
    private readonly long _cacheByteLimit;
    private WorkerRequest? _active;
    private WorkerRequest? _pending;
    private LayoutSession? _session;
    private bool _stopping;
    private bool _disposed;
    private int _startedCount;
    private int _completedCount;
    private int _canceledActiveCount;
    private int _supersededPendingCount;
    private int _sessionsCreatedCount;
    private int _sessionsDisposedCount;
    private int _cacheHitCount;
    private int _cacheMissCount;
    private int _evictedPageCount;
    private int _cachedPageCount;
    private long _cachedBytes;
    private long _cachedEncodedBytes;
    private long _cachedDecodedBytes;
    private long _activeGeneration;
    private long _phaseStartedTimestamp;
    private int _activePhase;

    internal WriterDedicatedPaginationEngine(
        int pageCacheLimit = DefaultPageCacheLimit,
        long cacheByteLimit = DefaultCacheByteLimit)
    {
        if (pageCacheLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCacheLimit));
        if (cacheByteLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(cacheByteLimit));
        _pageCacheLimit = pageCacheLimit;
        _cacheByteLimit = cacheByteLimit;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "RibbonKit Writer pagination layout"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("The Writer pagination STA did not start.");
    }

    internal int StartedCount => Volatile.Read(ref _startedCount);
    internal int CompletedCount => Volatile.Read(ref _completedCount);
    internal int CanceledActiveCount => Volatile.Read(ref _canceledActiveCount);
    internal int SupersededPendingCount => Volatile.Read(ref _supersededPendingCount);
    internal int SessionsCreatedCount => Volatile.Read(ref _sessionsCreatedCount);
    internal int SessionsDisposedCount => Volatile.Read(ref _sessionsDisposedCount);
    internal int CacheHitCount => Volatile.Read(ref _cacheHitCount);
    internal int CacheMissCount => Volatile.Read(ref _cacheMissCount);
    internal int EvictedPageCount => Volatile.Read(ref _evictedPageCount);
    internal int CachedPageCount => Volatile.Read(ref _cachedPageCount);
    internal long CachedBytes => Interlocked.Read(ref _cachedBytes);
    internal long CachedEncodedBytes => Interlocked.Read(ref _cachedEncodedBytes);
    internal long CachedDecodedBytes => Interlocked.Read(ref _cachedDecodedBytes);

    internal WriterPaginationWorkStatistics Statistics => new(
        StartedCount, CompletedCount, CanceledActiveCount, SupersededPendingCount,
        SessionsCreatedCount, SessionsDisposedCount, CacheHitCount, CacheMissCount,
        EvictedPageCount, CachedPageCount, CachedBytes, CachedEncodedBytes,
        CachedDecodedBytes);

    internal WriterPaginationWorkProgress Progress
    {
        get
        {
            while (true)
            {
                var phase = (WriterPaginationWorkPhase)Volatile.Read(ref _activePhase);
                var generation = Volatile.Read(ref _activeGeneration);
                var started = Volatile.Read(ref _phaseStartedTimestamp);
                if (phase != (WriterPaginationWorkPhase)Volatile.Read(ref _activePhase))
                    continue;
                var elapsed = phase == WriterPaginationWorkPhase.Idle || started == 0
                    ? 0
                    : ElapsedMilliseconds(started);
                return new WriterPaginationWorkProgress(generation, phase, elapsed);
            }
        }
    }

    internal Task<WriterPaginationCompletion> Queue(WriterPaginationCapture capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        var source = new TaskCompletionSource<WriterPaginationCompletion>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new WorkerRequest(capture, source, new CancellationTokenSource());
        WorkerRequest? superseded;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (capture.RequestKind == WriterPaginationRequestKind.Prefetch &&
                (_active is { IsCompleting: false } || _pending is not null))
            {
                Interlocked.Increment(ref _supersededPendingCount);
                source.TrySetResult(new WriterPaginationCompletion(
                    WriterPaginationCompletionKind.SupersededBeforeStart, null, 0));
                request.Cancellation.Dispose();
                return source.Task;
            }
            superseded = _pending;
            _pending = request;
            if (superseded is not null)
                Interlocked.Increment(ref _supersededPendingCount);
            if (capture.RequestKind == WriterPaginationRequestKind.Visible)
                _active?.Cancellation.Cancel();
        }

        if (superseded is not null)
        {
            superseded.Completion.TrySetResult(new WriterPaginationCompletion(
                WriterPaginationCompletionKind.SupersededBeforeStart, null, 0));
            superseded.Cancellation.Dispose();
        }
        _workAvailable.Set();
        return source.Task;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        WorkerRequest? active;
        WorkerRequest? pending;
        lock (_sync)
        {
            _disposed = true;
            _stopping = true;
            active = _active;
            pending = _pending;
            _pending = null;
            active?.Cancellation.Cancel();
        }
        if (pending is not null)
        {
            pending.Completion.TrySetResult(new WriterPaginationCompletion(
                WriterPaginationCompletionKind.SupersededBeforeStart, null, 0));
            pending.Cancellation.Dispose();
        }
        _workAvailable.Set();
        if (!_thread.Join(TimeSpan.FromSeconds(8)))
            throw new TimeoutException("The Writer pagination STA did not stop.");
        _workAvailable.Dispose();
        _ready.Dispose();
    }

    private void Run()
    {
        _ = Dispatcher.CurrentDispatcher;
        _ready.Set();
        while (true)
        {
            _workAvailable.WaitOne();
            WorkerRequest? request;
            lock (_sync)
            {
                if (_stopping && _pending is null)
                    break;
                request = _pending;
                _pending = null;
                if (request is not null)
                    _active = request;
            }
            if (request is null)
                continue;

            Interlocked.Increment(ref _startedCount);
            try
            {
                request.Cancellation.Token.ThrowIfCancellationRequested();
                var result = Build(request, request.Cancellation.Token);
                request.Cancellation.Token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref _completedCount);
                lock (_sync)
                    request.IsCompleting = true;
                request.Completion.TrySetResult(new WriterPaginationCompletion(
                    WriterPaginationCompletionKind.Completed, result,
                    request.CompletedMappedPages));
            }
            catch (OperationCanceledException) when (request.Cancellation.IsCancellationRequested)
            {
                Interlocked.Increment(ref _canceledActiveCount);
                request.Completion.TrySetResult(new WriterPaginationCompletion(
                    WriterPaginationCompletionKind.CanceledAfterStart, null,
                    request.CompletedMappedPages));
            }
            catch (Exception exception)
            {
                request.Completion.TrySetException(exception);
            }
            finally
            {
                SetProgress(0, WriterPaginationWorkPhase.Idle);
                lock (_sync)
                {
                    if (ReferenceEquals(_active, request))
                        _active = null;
                }
                request.Cancellation.Dispose();
            }

            lock (_sync)
            {
                if (_pending is not null)
                    _workAvailable.Set();
            }
        }
        DisposeSession();
        Dispatcher.CurrentDispatcher.InvokeShutdown();
    }

    private WriterPaginationLayoutResult Build(
        WorkerRequest request,
        CancellationToken cancellationToken)
    {
        var capture = request.Capture;
        var watch = Stopwatch.StartNew();
        var packageLoadMilliseconds = 0d;
        var formattingMilliseconds = 0d;
        var pageCountMilliseconds = 0d;
        var pageStartsMilliseconds = 0d;
        var objectMappingMilliseconds = 0d;
        var viewerRealizationMilliseconds = 0d;
        var insertionGeometryMilliseconds = 0d;
        var rasterizationMilliseconds = 0d;
        var structuredGeometryMilliseconds = 0d;
        var sessionReused = _session is not null &&
            _session.LayoutIdentity == capture.LayoutIdentity &&
            _session.DocumentIdentity == capture.DocumentIdentity;
        if (!sessionReused)
        {
            DisposeSession();
            _session = CreateSession(capture, cancellationToken,
                ref packageLoadMilliseconds, ref formattingMilliseconds,
                ref pageCountMilliseconds, ref pageStartsMilliseconds,
                ref objectMappingMilliseconds, ref viewerRealizationMilliseconds);
            Interlocked.Increment(ref _sessionsCreatedCount);
        }
        var session = _session ?? throw new InvalidOperationException(
            "The pagination layout session was not created.");
        cancellationToken.ThrowIfCancellationRequested();

        var visiblePage = Math.Clamp(capture.VisiblePage, 0, session.PageCount - 1);
        var mappedPages = capture.InteractivePages
            .Where(page => page >= 0 && page < session.PageCount)
            .Distinct()
            .ToImmutableArray();
        if (mappedPages.IsEmpty)
            mappedPages = ImmutableArray.Create(visiblePage);
        var requestedPages = capture.RequestedPages
            .Concat(mappedPages)
            .Where(page => page >= 0 && page < session.PageCount)
            .Distinct()
            .ToImmutableArray();
        if (requestedPages.IsEmpty)
            requestedPages = mappedPages;

        var requestHits = 0;
        var requestMisses = 0;
        foreach (var pageNumber in requestedPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (session.TryGetPage(pageNumber, out _))
            {
                requestHits++;
                Interlocked.Increment(ref _cacheHitCount);
                request.CompletedMappedPages++;
                continue;
            }

            requestMisses++;
            Interlocked.Increment(ref _cacheMissCount);
            SetProgress(capture.Generation, WriterPaginationWorkPhase.ViewerRealization);
            var phaseStarted = Stopwatch.GetTimestamp();
            session.Viewer.GoToPage(pageNumber + 1);
            UpdateLayout(session.Host);
            var pageView = session.Viewer.PageViews.Single(view =>
                view.PageNumber == pageNumber);
            using var documentPage = session.Paginator.GetPage(pageNumber);
            viewerRealizationMilliseconds += ElapsedMilliseconds(phaseStarted);

            SetProgress(capture.Generation, WriterPaginationWorkPhase.InsertionGeometry);
            phaseStarted = Stopwatch.GetTimestamp();
            var pageInsertions = BuildPageInsertions(session.Document, session.Paginator,
                session.PageStartOffsets, pageNumber, pageView, session.PageSettings,
                cancellationToken);
            insertionGeometryMilliseconds += ElapsedMilliseconds(phaseStarted);

            SetProgress(capture.Generation, WriterPaginationWorkPhase.Rasterization);
            phaseStarted = Stopwatch.GetTimestamp();
            var page = new WriterPaginationPage(pageNumber,
                RenderPage(documentPage, session.PageSettings,
                    session.PixelScaleX, session.PixelScaleY, cancellationToken));
            rasterizationMilliseconds += ElapsedMilliseconds(phaseStarted);

            SetProgress(capture.Generation, WriterPaginationWorkPhase.StructuredGeometry);
            phaseStarted = Stopwatch.GetTimestamp();
            var structured = ImmutableArray.CreateBuilder<WriterPaginationObjectGeometry>();
            var tables = ImmutableArray.CreateBuilder<WriterPaginationTableGeometry>();
            AddStructuredGeometry(structured, tables, session.StructuredObjects,
                session.CloneObjects, pageInsertions, pageView, session.PageSettings,
                session.Paginator, pageNumber, cancellationToken);
            structuredGeometryMilliseconds += ElapsedMilliseconds(phaseStarted);

            var structuredValues = structured.ToImmutable();
            var tableValues = tables.ToImmutable();
            var footprint = EstimatePageFootprint(page, pageInsertions,
                structuredValues, tableValues, session.PageSettings,
                session.PixelScaleX, session.PixelScaleY);
            session.AddPage(new CachedPage(page, pageInsertions,
                structuredValues, tableValues, 0, footprint.TotalBytes,
                footprint.EncodedBytes, footprint.DecodedBytes));
            UpdateCacheSnapshot(session);
            request.CompletedMappedPages++;
            cancellationToken.ThrowIfCancellationRequested();
        }

        var evicted = session.Evict(_pageCacheLimit, _cacheByteLimit,
            mappedPages.ToHashSet(), visiblePage);
        if (evicted > 0)
            Interlocked.Add(ref _evictedPageCount, evicted);
        UpdateCacheSnapshot(session);

        var retainedPages = session.CachedPageNumbers;
        var resultPages = ImmutableArray.CreateBuilder<WriterPaginationPage>();
        var insertions = ImmutableArray.CreateBuilder<WriterPaginationInsertionGeometry>();
        var resultStructured = ImmutableArray.CreateBuilder<WriterPaginationObjectGeometry>();
        var resultTables = ImmutableArray.CreateBuilder<WriterPaginationTableGeometry>();
        foreach (var pageNumber in requestedPages)
        {
            if (!session.TryGetPage(pageNumber, out var cached))
                continue;
            resultPages.Add(cached.Page);
            insertions.AddRange(cached.Insertions);
            resultStructured.AddRange(cached.StructuredObjects);
            resultTables.AddRange(cached.Tables);
        }

        watch.Stop();
        var timings = new WriterPaginationPhaseTimings(packageLoadMilliseconds,
            formattingMilliseconds, pageCountMilliseconds, pageStartsMilliseconds,
            objectMappingMilliseconds, viewerRealizationMilliseconds,
            insertionGeometryMilliseconds, rasterizationMilliseconds,
            structuredGeometryMilliseconds);
        return new WriterPaginationLayoutResult(capture.Generation, capture.LayoutIdentity,
            capture.DocumentIdentity, visiblePage, session.PageCount, session.PageStartOffsets,
            mappedPages, retainedPages, resultPages.ToImmutable(), insertions.ToImmutable(),
            resultStructured.ToImmutable(), resultTables.ToImmutable(), session.PageSettings,
            capture.RequestKind, sessionReused, requestHits, requestMisses, evicted,
            session.CachedBytes, session.CachedEncodedBytes, session.CachedDecodedBytes,
            Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState(),
            timings,
            watch.Elapsed.TotalMilliseconds);
    }

    private LayoutSession CreateSession(WriterPaginationCapture capture,
        CancellationToken cancellationToken,
        ref double packageLoadMilliseconds,
        ref double formattingMilliseconds,
        ref double pageCountMilliseconds,
        ref double pageStartsMilliseconds,
        ref double objectMappingMilliseconds,
        ref double viewerRealizationMilliseconds)
    {
        SetProgress(capture.Generation, WriterPaginationWorkPhase.PackageLoad);
        var phaseStarted = Stopwatch.GetTimestamp();
        var clone = new FlowDocument();
        using (var stream = new MemoryStream(capture.XamlPackage.ToArray(), writable: false))
        {
            new TextRange(clone.ContentStart, clone.ContentEnd)
                .Load(stream, DataFormats.XamlPackage);
        }
        packageLoadMilliseconds += ElapsedMilliseconds(phaseStarted);

        SetProgress(capture.Generation, WriterPaginationWorkPhase.Formatting);
        phaseStarted = Stopwatch.GetTimestamp();
        ApplyFormatting(clone, capture.Formatting);
        ApplyPageSettings(clone, capture.PageSettings);
        formattingMilliseconds += ElapsedMilliseconds(phaseStarted);
        cancellationToken.ThrowIfCancellationRequested();

        SetProgress(capture.Generation, WriterPaginationWorkPhase.PageCount);
        phaseStarted = Stopwatch.GetTimestamp();
        var paginator = (DynamicDocumentPaginator)
            ((IDocumentPaginatorSource)clone).DocumentPaginator;
        paginator.PageSize = new Size(capture.PageSettings.WidthDip,
            capture.PageSettings.HeightDip);
        paginator.ComputePageCount();
        pageCountMilliseconds += ElapsedMilliseconds(phaseStarted);
        cancellationToken.ThrowIfCancellationRequested();
        if (paginator.PageCount <= 0)
            throw new InvalidOperationException("The pagination clone did not produce a page.");

        SetProgress(capture.Generation, WriterPaginationWorkPhase.PageStarts);
        phaseStarted = Stopwatch.GetTimestamp();
        var pageStarts = GetPageStartOffsets(clone, paginator, cancellationToken);
        pageStartsMilliseconds += ElapsedMilliseconds(phaseStarted);

        SetProgress(capture.Generation, WriterPaginationWorkPhase.ObjectMapping);
        phaseStarted = Stopwatch.GetTimestamp();
        var cloneObjects = FindCloneObjects(clone, capture.StructuredObjects);
        objectMappingMilliseconds += ElapsedMilliseconds(phaseStarted);

        var viewer = new FlowDocumentPageViewer
        {
            Document = clone,
            Zoom = 100
        };
        var host = new Window
        {
            Content = viewer,
            Width = capture.PageSettings.WidthDip + 120,
            Height = Math.Min(900, capture.PageSettings.HeightDip + 80),
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            ShowActivated = false,
            Opacity = 0.01
        };
        try
        {
            SetProgress(capture.Generation, WriterPaginationWorkPhase.ViewerRealization);
            phaseStarted = Stopwatch.GetTimestamp();
            host.Show();
            UpdateLayout(host);
            viewerRealizationMilliseconds += ElapsedMilliseconds(phaseStarted);
            return new LayoutSession(capture.LayoutIdentity, capture.DocumentIdentity,
                clone, paginator, pageStarts, capture.StructuredObjects, cloneObjects,
                capture.PageSettings, capture.PixelScaleX, capture.PixelScaleY, viewer, host);
        }
        catch
        {
            viewer.Document = null;
            if (host.IsVisible)
                host.Close();
            throw;
        }
    }

    private void SetProgress(long generation, WriterPaginationWorkPhase phase)
    {
        if (phase == WriterPaginationWorkPhase.Idle)
        {
            Volatile.Write(ref _activePhase, (int)phase);
            Volatile.Write(ref _phaseStartedTimestamp, 0);
            Volatile.Write(ref _activeGeneration, 0);
            WriterPaginationDiagnosticOptions.WriteTelemetry("worker idle");
            return;
        }

        Volatile.Write(ref _activeGeneration, generation);
        Volatile.Write(ref _phaseStartedTimestamp, Stopwatch.GetTimestamp());
        Volatile.Write(ref _activePhase, (int)phase);
        WriterPaginationDiagnosticOptions.WriteTelemetry(
            $"worker generation {generation:N0} phase {phase}");
    }

    private static double ElapsedMilliseconds(long startedTimestamp) =>
        (Stopwatch.GetTimestamp() - startedTimestamp) * 1000d / Stopwatch.Frequency;

    private static ImmutableArray<int> GetPageStartOffsets(FlowDocument document,
        DynamicDocumentPaginator paginator, CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<int>(paginator.PageCount);
        for (var page = 0; page < paginator.PageCount; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var documentPage = paginator.GetPage(page);
            if (paginator.GetPagePosition(documentPage) is not TextPointer position)
                throw new InvalidOperationException($"Page {page + 1} has no text position.");
            builder.Add(document.ContentStart.GetOffsetToPosition(position));
        }
        return builder.ToImmutable();
    }

    private static ImmutableArray<WriterPaginationInsertionGeometry> BuildPageInsertions(
        FlowDocument document,
        DynamicDocumentPaginator paginator,
        ImmutableArray<int> pageStartOffsets,
        int pageNumber,
        DocumentPageView pageView,
        WriterPaginationPageSettings pageSettings,
        CancellationToken cancellationToken)
    {
        var pageStart = document.ContentStart.GetPositionAtOffset(
            pageStartOffsets[pageNumber], LogicalDirection.Forward) ??
            throw new InvalidOperationException($"Page {pageNumber + 1} has no start position.");
        var pageEnd = pageNumber + 1 < pageStartOffsets.Length
            ? document.ContentStart.GetPositionAtOffset(pageStartOffsets[pageNumber + 1],
                LogicalDirection.Forward) ?? throw new InvalidOperationException(
                    $"Page {pageNumber + 2} has no start position.")
            : document.ContentEnd;
        var builder = ImmutableArray.CreateBuilder<WriterPaginationInsertionGeometry>();
        var batch = 0;
        for (var position = pageStart.GetInsertionPosition(LogicalDirection.Forward);
             position is not null && position.CompareTo(pageEnd) < 0;
             position = position.GetNextInsertionPosition(LogicalDirection.Forward))
        {
            if (++batch % 128 == 0)
                cancellationToken.ThrowIfCancellationRequested();
            if (paginator.GetPageNumber(position) != pageNumber)
                continue;
            var rect = position.GetCharacterRect(LogicalDirection.Forward);
            if (!IsFinite(rect) || rect.Height <= 0 || rect.Left < -1 || rect.Top < -1 ||
                rect.Right > pageView.ActualWidth + 1 || rect.Bottom > pageView.ActualHeight + 1)
                continue;
            var normalized = NormalizePageRect(rect, pageView, pageSettings);
            builder.Add(new WriterPaginationInsertionGeometry(
                document.ContentStart.GetOffsetToPosition(position), pageNumber,
                new WriterPaginationRectangle(normalized.X, normalized.Y,
                    normalized.Width, normalized.Height)));
        }
        return builder.ToImmutable();
    }

    private static ImmutableArray<byte> RenderPage(DocumentPage documentPage,
        WriterPaginationPageSettings settings, double pixelScaleX, double pixelScaleY,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var width = Math.Max(1, (int)Math.Ceiling(settings.WidthDip * pixelScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(settings.HeightDip * pixelScaleY));
        var bitmap = new RenderTargetBitmap(width, height,
            96 * pixelScaleX, 96 * pixelScaleY, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0,
                settings.WidthDip, settings.HeightDip));
            var pageBrush = new VisualBrush(documentPage.Visual)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, settings.WidthDip, settings.HeightDip),
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, settings.WidthDip, settings.HeightDip),
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
            drawing.DrawRectangle(pageBrush, null, new Rect(0, 0,
                settings.WidthDip, settings.HeightDip));
        }
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray().ToImmutableArray();
    }

    private static Dictionary<long, TextElement> FindCloneObjects(
        FlowDocument clone,
        ImmutableArray<WriterPaginationObjectCapture> captures)
    {
        var byKey = captures.ToDictionary(item => (item.Kind, item.StartOffset));
        var result = new Dictionary<long, TextElement>();
        foreach (var element in EnumerateTextElements(clone))
        {
            var kind = element switch
            {
                Table => WriterPaginationObjectKind.Table,
                InlineUIContainer { Child: Image } => WriterPaginationObjectKind.Picture,
                Hyperlink => WriterPaginationObjectKind.Hyperlink,
                _ => (WriterPaginationObjectKind?)null
            };
            if (kind is null)
                continue;
            var offset = clone.ContentStart.GetOffsetToPosition(element.ElementStart);
            if (byKey.TryGetValue((kind.Value, offset), out var capture))
                result[capture.ObjectIdentity] = element;
        }
        return result;
    }

    private static IEnumerable<TextElement> EnumerateTextElements(FlowDocument document)
    {
        for (var position = document.ContentStart;
             position is not null && position.CompareTo(document.ContentEnd) < 0;
             position = position.GetNextContextPosition(LogicalDirection.Forward))
        {
            if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart &&
                position.GetAdjacentElement(LogicalDirection.Forward) is TextElement element)
                yield return element;
        }
    }

    private static void AddStructuredGeometry(
        ImmutableArray<WriterPaginationObjectGeometry>.Builder output,
        ImmutableArray<WriterPaginationTableGeometry>.Builder tableOutput,
        ImmutableArray<WriterPaginationObjectCapture> captures,
        IReadOnlyDictionary<long, TextElement> cloneObjects,
        ImmutableArray<WriterPaginationInsertionGeometry> pageInsertions,
        DocumentPageView pageView,
        WriterPaginationPageSettings pageSettings,
        DynamicDocumentPaginator paginator,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        foreach (var capture in captures)
        {
            if (!cloneObjects.TryGetValue(capture.ObjectIdentity, out var element))
                continue;
            Rect bounds;
            if (element is InlineUIContainer { Child: Image image } container)
            {
                if (paginator.GetPageNumber(element.ContentStart) != pageNumber)
                    continue;
                if (!TryFindImageBounds(pageView, container, image, cancellationToken, out bounds))
                    continue;
                bounds = NormalizePageRect(bounds, pageView, pageSettings);
            }
            else
            {
                var matching = pageInsertions.Where(entry =>
                    entry.SourceOffset >= capture.StartOffset &&
                    entry.SourceOffset <= capture.EndOffset).ToArray();
                if (matching.Length == 0)
                    continue;
                var left = matching.Min(entry => entry.Rectangle.X);
                var top = matching.Min(entry => entry.Rectangle.Y);
                var right = matching.Max(entry => entry.Rectangle.X +
                    Math.Max(1, entry.Rectangle.Width));
                var bottom = matching.Max(entry => entry.Rectangle.Y + entry.Rectangle.Height);
                bounds = new Rect(left, top, Math.Max(1, right - left),
                    Math.Max(1, bottom - top));
            }
            output.Add(new WriterPaginationObjectGeometry(capture.ObjectIdentity,
                capture.Kind, capture.StartOffset, pageNumber,
                new WriterPaginationRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height)));
            if (element is Table table && TryBuildTableGeometry(table,
                    capture.ObjectIdentity, bounds, pageView, pageSettings, paginator,
                    pageNumber, out var tableGeometry))
                tableOutput.Add(tableGeometry);
        }
    }

    private static bool TryBuildTableGeometry(Table table, long objectIdentity,
        Rect fragmentBounds, DocumentPageView pageView,
        WriterPaginationPageSettings pageSettings, DynamicDocumentPaginator paginator,
        int pageNumber, out WriterPaginationTableGeometry geometry)
    {
        geometry = null!;
        var cells = EnumerateLogicalTableCells(table).ToArray();
        if (cells.Length == 0)
            return false;
        var realizedEndings = new List<(CloneTableCell Cell, double Bottom)>();
        foreach (var cell in cells)
        {
            if (TryGetCellBottom(cell.Cell, pageView, pageSettings, paginator,
                    pageNumber, out var bottom))
                realizedEndings.Add((cell, bottom));
        }
        if (realizedEndings.Count == 0)
            return false;

        var columnCount = Math.Max(table.Columns.Count,
            cells.Max(cell => cell.LastColumn) + 1);
        if (columnCount <= 0)
            return false;
        var spacing = double.IsFinite(table.CellSpacing)
            ? Math.Max(0, table.CellSpacing)
            : 0;
        var marginLeft = double.IsFinite(table.Margin.Left)
            ? Math.Max(0, table.Margin.Left)
            : 0;
        var left = pageSettings.LeftMarginDip + marginLeft + spacing;
        var hasTrustedColumns = table.Columns.Count >= columnCount &&
            table.Columns.Take(columnCount).All(column => column.Width.IsAbsolute &&
                double.IsFinite(column.Width.Value) && column.Width.Value > 0);
        var columns = ImmutableArray.CreateBuilder<double>(
            hasTrustedColumns ? columnCount + 1 : 0);
        if (hasTrustedColumns)
        {
            columns.Add(left);
            foreach (var column in table.Columns.Take(columnCount))
                columns.Add(columns[^1] + column.Width.Value + spacing);
        }

        var rows = ImmutableArray.CreateBuilder<WriterPaginationTableRowBoundary>();
        foreach (var group in realizedEndings.GroupBy(item =>
                     (item.Cell.RowGroupIndex, item.Cell.LastRow)))
        {
            var bottom = group.Max(item => item.Bottom);
            if (double.IsFinite(bottom))
                rows.Add(new WriterPaginationTableRowBoundary(group.Key.RowGroupIndex,
                    group.Key.LastRow, bottom));
        }
        if (rows.Count == 0)
            return false;
        var top = fragmentBounds.Top;
        var bottomEdge = Math.Max(fragmentBounds.Bottom,
            rows.Max(row => row.PositionDip));
        var contentRight = Math.Max(left + 1,
            pageSettings.WidthDip - pageSettings.RightMarginDip);
        var right = hasTrustedColumns
            ? columns[^1]
            : Math.Clamp(fragmentBounds.Right, left + 1, contentRight);
        var boundsValue = new WriterPaginationRectangle(left, top,
            Math.Max(1, right - left), Math.Max(1, bottomEdge - top));
        geometry = new WriterPaginationTableGeometry(objectIdentity, pageNumber,
            boundsValue, columns.ToImmutable(), hasTrustedColumns,
            rows.OrderBy(row => row.PositionDip)
                .ThenBy(row => row.RowGroupIndex)
                .ThenBy(row => row.RowIndex)
                .ToImmutableArray(),
            GetPageNumber(paginator, table.ContentStart, LogicalDirection.Forward) == pageNumber,
            GetPageNumber(paginator, table.ContentEnd, LogicalDirection.Backward) == pageNumber);
        return true;
    }

    private static bool TryGetCellBottom(TableCell cell, DocumentPageView pageView,
        WriterPaginationPageSettings pageSettings, DynamicDocumentPaginator paginator,
        int pageNumber, out double bottom)
    {
        bottom = double.NaN;
        var end = cell.ContentEnd.GetInsertionPosition(LogicalDirection.Backward);
        if (end is null || paginator.GetPageNumber(end) != pageNumber)
            return false;
        var endRect = end.GetCharacterRect(LogicalDirection.Backward);
        if (!IsFinite(endRect) || endRect.Height <= 0 ||
            endRect.Left < -1 || endRect.Top < -1 ||
            endRect.Right > pageView.ActualWidth + 1 || endRect.Bottom > pageView.ActualHeight + 1)
            return false;
        var padding = cell.Padding;
        var border = cell.BorderThickness;
        var fitted = new Rect(endRect.Left, endRect.Top,
            Math.Max(1, endRect.Width),
            Math.Max(1, endRect.Height + padding.Bottom + border.Bottom));
        var normalized = NormalizePageRect(fitted, pageView, pageSettings);
        bottom = normalized.Bottom;
        return IsFinite(normalized) && double.IsFinite(bottom);
    }

    private static int GetPageNumber(DynamicDocumentPaginator paginator,
        TextPointer position, LogicalDirection direction)
    {
        var insertion = position.GetInsertionPosition(direction) ?? position;
        return paginator.GetPageNumber(insertion);
    }

    private static IEnumerable<CloneTableCell> EnumerateLogicalTableCells(Table table)
    {
        for (var groupIndex = 0; groupIndex < table.RowGroups.Count; groupIndex++)
        {
            var group = table.RowGroups[groupIndex];
            var occupiedUntil = new List<int>();
            for (var rowIndex = 0; rowIndex < group.Rows.Count; rowIndex++)
            {
                var column = 0;
                foreach (var cell in group.Rows[rowIndex].Cells)
                {
                    while (column < occupiedUntil.Count && occupiedUntil[column] > rowIndex)
                        column++;
                    var columnSpan = Math.Max(1, cell.ColumnSpan);
                    var rowSpan = Math.Max(1, cell.RowSpan);
                    while (occupiedUntil.Count < column + columnSpan)
                        occupiedUntil.Add(0);
                    var lastColumn = column + columnSpan - 1;
                    yield return new CloneTableCell(cell, groupIndex, rowIndex,
                        rowIndex + rowSpan - 1, column, lastColumn);
                    for (var index = column; index <= lastColumn; index++)
                        occupiedUntil[index] = Math.Max(occupiedUntil[index], rowIndex + rowSpan);
                    column = lastColumn + 1;
                }
            }
        }
    }

    private readonly record struct CloneTableCell(TableCell Cell, int RowGroupIndex,
        int RowIndex, int LastRow, int Column, int LastColumn);

    private static bool TryFindImageBounds(DocumentPageView pageView,
        InlineUIContainer container, Image image,
        CancellationToken cancellationToken, out Rect bounds)
    {
        var leading = container.ElementStart.GetCharacterRect(LogicalDirection.Forward);
        var trailing = container.ElementEnd.GetCharacterRect(LogicalDirection.Backward);
        if (IsFinite(leading) && leading.Height > 0 &&
            leading.Left >= -1 && leading.Top >= -1 &&
            leading.Right <= pageView.ActualWidth + 1 &&
            leading.Bottom <= pageView.ActualHeight + 1)
        {
            var width = IsFinite(trailing) && trailing.X > leading.X
                ? trailing.X - leading.X
                : image.RenderSize.Width;
            var height = Math.Max(leading.Height, image.RenderSize.Height);
            if (width > 0 && height > 0)
            {
                bounds = new Rect(leading.X, leading.Y, width, height);
                return true;
            }
        }

        foreach (var candidate in EnumerateVisualDescendants(pageView).OfType<Image>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReferenceEquals(candidate, image) &&
                !ReferenceEquals(candidate.Source, image.Source))
                continue;
            try
            {
                var candidateBounds = candidate.TransformToAncestor(pageView).TransformBounds(
                    new Rect(new Point(), candidate.RenderSize));
                if (!candidateBounds.IsEmpty &&
                    candidateBounds.Width > 0 && candidateBounds.Height > 0 &&
                    double.IsFinite(candidateBounds.X) && double.IsFinite(candidateBounds.Y) &&
                    double.IsFinite(candidateBounds.Width) && double.IsFinite(candidateBounds.Height))
                {
                    bounds = candidateBounds;
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
                // A paginator may expose a logical image that is not the visual hosted by this
                // page. Continue to the public hit-test fallback below.
            }
        }

        const double step = 4;
        var left = double.PositiveInfinity;
        var top = double.PositiveInfinity;
        var right = double.NegativeInfinity;
        var bottom = double.NegativeInfinity;
        for (var y = 0d; y <= pageView.ActualHeight; y += step)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0d; x <= pageView.ActualWidth; x += step)
            {
                if (!ReferenceEquals(pageView.InputHitTest(new Point(x, y)), image))
                    continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x + step);
                bottom = Math.Max(bottom, y + step);
            }
        }
        if (!double.IsFinite(left) || !double.IsFinite(top))
        {
            bounds = Rect.Empty;
            return false;
        }
        bounds = new Rect(left, top, Math.Max(step, right - left),
            Math.Max(step, bottom - top));
        return true;
    }

    private static IEnumerable<DependencyObject> EnumerateVisualDescendants(
        DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in EnumerateVisualDescendants(child))
                yield return descendant;
        }
    }

    private static Rect NormalizePageRect(Rect rect, DocumentPageView pageView,
        WriterPaginationPageSettings pageSettings)
    {
        if (pageView.ActualWidth <= 0 || pageView.ActualHeight <= 0)
            throw new InvalidOperationException("The realized page view has no layout size.");
        var scaleX = pageSettings.WidthDip / pageView.ActualWidth;
        var scaleY = pageSettings.HeightDip / pageView.ActualHeight;
        return new Rect(rect.X * scaleX, rect.Y * scaleY,
            rect.Width * scaleX, rect.Height * scaleY);
    }

    private static void ApplyFormatting(FlowDocument document,
        WriterPaginationFormatting formatting)
    {
        document.FontFamily = new FontFamily(formatting.FontFamily);
        document.FontSize = formatting.FontSize;
        document.FontWeight = FontWeight.FromOpenTypeWeight(formatting.FontWeight);
        document.FontStretch = FontStretch.FromOpenTypeStretch(formatting.FontStretch);
        document.Language = XmlLanguage.GetLanguage(formatting.Language);
        document.FlowDirection = formatting.FlowDirection;
        document.TextAlignment = formatting.TextAlignment;
        document.LineHeight = formatting.LineHeight;
        document.LineStackingStrategy = formatting.LineStackingStrategy;
        document.IsHyphenationEnabled = formatting.IsHyphenationEnabled;
        document.IsOptimalParagraphEnabled = formatting.IsOptimalParagraphEnabled;
        document.Background = ParseXaml<Brush>(formatting.BackgroundXaml);
        document.Foreground = ParseXaml<Brush>(formatting.ForegroundXaml);
        document.TextEffects = ParseXaml<TextEffectCollection>(formatting.TextEffectsXaml);
        document.ColumnRuleBrush = ParseXaml<Brush>(formatting.ColumnRuleBrushXaml);
        document.ColumnRuleWidth = formatting.ColumnRuleWidth;
    }

    private static T? ParseXaml<T>(string? xaml) where T : class =>
        string.IsNullOrWhiteSpace(xaml) ? null : XamlReader.Parse(xaml) as T;

    private static void ApplyPageSettings(FlowDocument document,
        WriterPaginationPageSettings settings)
    {
        document.PageWidth = settings.WidthDip;
        document.PageHeight = settings.HeightDip;
        document.PagePadding = new Thickness(settings.LeftMarginDip, settings.TopMarginDip,
            settings.RightMarginDip, settings.BottomMarginDip);
        document.ColumnWidth = settings.ContentWidthDip;
        document.ColumnGap = 0;
        document.IsColumnWidthFlexible = false;
        document.ColumnRuleWidth = 0;
    }

    private static bool IsFinite(Rect rect) => !rect.IsEmpty &&
        double.IsFinite(rect.X) && double.IsFinite(rect.Y) &&
        double.IsFinite(rect.Width) && double.IsFinite(rect.Height);

    private static void UpdateLayout(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
    }

    private static PageFootprint EstimatePageFootprint(WriterPaginationPage page,
        ImmutableArray<WriterPaginationInsertionGeometry> insertions,
        ImmutableArray<WriterPaginationObjectGeometry> structuredObjects,
        ImmutableArray<WriterPaginationTableGeometry> tables,
        WriterPaginationPageSettings settings,
        double pixelScaleX,
        double pixelScaleY)
    {
        var tableBytes = tables.Sum(table => 128L +
            table.ColumnBoundaries.Length * sizeof(double) +
            table.RowBoundaries.Length * 24L);
        var geometryBytes = insertions.Length * 48L +
            structuredObjects.Length * 64L + tableBytes;
        var pixelWidth = Math.Max(1L,
            (long)Math.Ceiling(settings.WidthDip * pixelScaleX));
        var pixelHeight = Math.Max(1L,
            (long)Math.Ceiling(settings.HeightDip * pixelScaleY));
        var decodedBytes = checked(pixelWidth * pixelHeight * 4L);
        var encodedBytes = (long)page.PngBytes.Length;
        return new PageFootprint(checked(encodedBytes + decodedBytes + geometryBytes),
            encodedBytes, decodedBytes);
    }

    private void UpdateCacheSnapshot(LayoutSession session)
    {
        Volatile.Write(ref _cachedPageCount, session.CachedPageCount);
        Interlocked.Exchange(ref _cachedBytes, session.CachedBytes);
        Interlocked.Exchange(ref _cachedEncodedBytes, session.CachedEncodedBytes);
        Interlocked.Exchange(ref _cachedDecodedBytes, session.CachedDecodedBytes);
    }

    private void DisposeSession()
    {
        if (_session is null)
            return;
        _session.Dispose();
        _session = null;
        Interlocked.Increment(ref _sessionsDisposedCount);
        Volatile.Write(ref _cachedPageCount, 0);
        Interlocked.Exchange(ref _cachedBytes, 0);
        Interlocked.Exchange(ref _cachedEncodedBytes, 0);
        Interlocked.Exchange(ref _cachedDecodedBytes, 0);
    }

    private readonly record struct PageFootprint(
        long TotalBytes,
        long EncodedBytes,
        long DecodedBytes);

    private sealed class LayoutSession : IDisposable
    {
        private readonly Dictionary<int, CachedPage> _pages = new();
        private long _accessSequence;

        internal LayoutSession(long layoutIdentity, long documentIdentity,
            FlowDocument document, DynamicDocumentPaginator paginator,
            ImmutableArray<int> pageStartOffsets,
            ImmutableArray<WriterPaginationObjectCapture> structuredObjects,
            Dictionary<long, TextElement> cloneObjects,
            WriterPaginationPageSettings pageSettings,
            double pixelScaleX, double pixelScaleY,
            FlowDocumentPageViewer viewer, Window host)
        {
            LayoutIdentity = layoutIdentity;
            DocumentIdentity = documentIdentity;
            Document = document;
            Paginator = paginator;
            PageStartOffsets = pageStartOffsets;
            StructuredObjects = structuredObjects;
            CloneObjects = cloneObjects;
            PageSettings = pageSettings;
            PixelScaleX = pixelScaleX;
            PixelScaleY = pixelScaleY;
            Viewer = viewer;
            Host = host;
        }

        internal long LayoutIdentity { get; }
        internal long DocumentIdentity { get; }
        internal FlowDocument Document { get; }
        internal DynamicDocumentPaginator Paginator { get; }
        internal int PageCount => Paginator.PageCount;
        internal ImmutableArray<int> PageStartOffsets { get; }
        internal ImmutableArray<WriterPaginationObjectCapture> StructuredObjects { get; }
        internal Dictionary<long, TextElement> CloneObjects { get; }
        internal WriterPaginationPageSettings PageSettings { get; }
        internal double PixelScaleX { get; }
        internal double PixelScaleY { get; }
        internal FlowDocumentPageViewer Viewer { get; }
        internal Window Host { get; }
        internal int CachedPageCount => _pages.Count;
        internal long CachedBytes { get; private set; }
        internal long CachedEncodedBytes { get; private set; }
        internal long CachedDecodedBytes { get; private set; }
        internal ImmutableArray<int> CachedPageNumbers => _pages.Keys
            .OrderBy(page => page).ToImmutableArray();

        internal bool TryGetPage(int pageNumber, out CachedPage page)
        {
            if (!_pages.TryGetValue(pageNumber, out page!))
                return false;
            page.LastAccess = ++_accessSequence;
            return true;
        }

        internal void AddPage(CachedPage page)
        {
            if (_pages.Remove(page.Page.PageNumber, out var replaced))
            {
                CachedBytes -= replaced.EstimatedBytes;
                CachedEncodedBytes -= replaced.EncodedBytes;
                CachedDecodedBytes -= replaced.DecodedBytes;
            }
            page.LastAccess = ++_accessSequence;
            _pages.Add(page.Page.PageNumber, page);
            CachedBytes += page.EstimatedBytes;
            CachedEncodedBytes += page.EncodedBytes;
            CachedDecodedBytes += page.DecodedBytes;
        }

        internal int Evict(int pageLimit, long byteLimit,
            HashSet<int> protectedPages, int visiblePage)
        {
            var evicted = 0;
            var effectivePageLimit = Math.Max(pageLimit, protectedPages.Count);
            while (_pages.Count > effectivePageLimit || CachedBytes > byteLimit)
            {
                var candidates = _pages
                    .Where(item => !protectedPages.Contains(item.Key))
                    .OrderByDescending(item => Math.Abs(item.Key - visiblePage))
                    .ThenBy(item => item.Value.LastAccess)
                    .ToArray();
                if (candidates.Length == 0)
                    break;
                var candidate = candidates[0];
                _pages.Remove(candidate.Key);
                CachedBytes -= candidate.Value.EstimatedBytes;
                CachedEncodedBytes -= candidate.Value.EncodedBytes;
                CachedDecodedBytes -= candidate.Value.DecodedBytes;
                evicted++;
            }
            return evicted;
        }

        public void Dispose()
        {
            _pages.Clear();
            CachedBytes = 0;
            CachedEncodedBytes = 0;
            CachedDecodedBytes = 0;
            Viewer.Document = null;
            if (Host.IsVisible)
                Host.Close();
        }
    }

    private sealed class CachedPage(
        WriterPaginationPage page,
        ImmutableArray<WriterPaginationInsertionGeometry> insertions,
        ImmutableArray<WriterPaginationObjectGeometry> structuredObjects,
        ImmutableArray<WriterPaginationTableGeometry> tables,
        long lastAccess,
        long estimatedBytes,
        long encodedBytes,
        long decodedBytes)
    {
        internal WriterPaginationPage Page { get; } = page;
        internal ImmutableArray<WriterPaginationInsertionGeometry> Insertions { get; } = insertions;
        internal ImmutableArray<WriterPaginationObjectGeometry> StructuredObjects { get; } =
            structuredObjects;
        internal ImmutableArray<WriterPaginationTableGeometry> Tables { get; } = tables;
        internal long LastAccess { get; set; } = lastAccess;
        internal long EstimatedBytes { get; } = estimatedBytes;
        internal long EncodedBytes { get; } = encodedBytes;
        internal long DecodedBytes { get; } = decodedBytes;
    }

    private sealed class WorkerRequest(
        WriterPaginationCapture capture,
        TaskCompletionSource<WriterPaginationCompletion> completion,
        CancellationTokenSource cancellation)
    {
        internal WriterPaginationCapture Capture { get; } = capture;
        internal TaskCompletionSource<WriterPaginationCompletion> Completion { get; } = completion;
        internal CancellationTokenSource Cancellation { get; } = cancellation;
        internal int CompletedMappedPages { get; set; }
        internal bool IsCompleting { get; set; }
    }
}
