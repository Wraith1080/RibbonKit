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
    private readonly object _sync = new();
    private readonly AutoResetEvent _workAvailable = new(false);
    private readonly ManualResetEventSlim _ready = new();
    private readonly Thread _thread;
    private WorkerRequest? _active;
    private WorkerRequest? _pending;
    private bool _stopping;
    private bool _disposed;

    internal WriterDedicatedPaginationEngine()
    {
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

    internal int StartedCount { get; private set; }
    internal int CompletedCount { get; private set; }
    internal int CanceledActiveCount { get; private set; }
    internal int SupersededPendingCount { get; private set; }

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
            superseded = _pending;
            _pending = request;
            if (superseded is not null)
                SupersededPendingCount++;
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

            StartedCount++;
            try
            {
                request.Cancellation.Token.ThrowIfCancellationRequested();
                var result = Build(request, request.Cancellation.Token);
                request.Cancellation.Token.ThrowIfCancellationRequested();
                CompletedCount++;
                request.Completion.TrySetResult(new WriterPaginationCompletion(
                    WriterPaginationCompletionKind.Completed, result,
                    request.CompletedMappedPages));
            }
            catch (OperationCanceledException) when (request.Cancellation.IsCancellationRequested)
            {
                CanceledActiveCount++;
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
        Dispatcher.CurrentDispatcher.InvokeShutdown();
    }

    private static WriterPaginationLayoutResult Build(
        WorkerRequest request,
        CancellationToken cancellationToken)
    {
        var capture = request.Capture;
        var watch = Stopwatch.StartNew();
        var clone = new FlowDocument();
        using (var stream = new MemoryStream(capture.XamlPackage.ToArray(), writable: false))
        {
            new TextRange(clone.ContentStart, clone.ContentEnd)
                .Load(stream, DataFormats.XamlPackage);
        }
        ApplyFormatting(clone, capture.Formatting);
        ApplyPageSettings(clone, capture.PageSettings);
        cancellationToken.ThrowIfCancellationRequested();

        var paginator = (DynamicDocumentPaginator)
            ((IDocumentPaginatorSource)clone).DocumentPaginator;
        paginator.PageSize = new Size(capture.PageSettings.WidthDip,
            capture.PageSettings.HeightDip);
        paginator.ComputePageCount();
        cancellationToken.ThrowIfCancellationRequested();
        if (paginator.PageCount <= 0)
            throw new InvalidOperationException("The pagination clone did not produce a page.");

        var visiblePage = Math.Clamp(capture.VisiblePage, 0, paginator.PageCount - 1);
        var firstMappedPage = Math.Max(0, visiblePage - 1);
        var lastMappedPage = Math.Min(paginator.PageCount - 1, visiblePage + 1);
        var mappedPages = Enumerable.Range(firstMappedPage,
                lastMappedPage - firstMappedPage + 1)
            .ToImmutableArray();
        var pageStarts = GetPageStartOffsets(clone, paginator, cancellationToken);
        var cloneObjects = FindCloneObjects(clone, capture.StructuredObjects);

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
        var pages = ImmutableArray.CreateBuilder<WriterPaginationPage>();
        var insertions = ImmutableArray.CreateBuilder<WriterPaginationInsertionGeometry>();
        var structured = ImmutableArray.CreateBuilder<WriterPaginationObjectGeometry>();
        try
        {
            host.Show();
            UpdateLayout(host);
            foreach (var pageNumber in mappedPages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                viewer.GoToPage(pageNumber + 1);
                UpdateLayout(host);
                var pageView = viewer.PageViews.Single(view => view.PageNumber == pageNumber);
                var documentPage = paginator.GetPage(pageNumber);
                var pageInsertions = BuildPageInsertions(clone, paginator, pageNumber,
                    pageView, capture.PageSettings, cancellationToken);
                insertions.AddRange(pageInsertions);
                pages.Add(new WriterPaginationPage(pageNumber,
                    RenderPage(documentPage, capture.PageSettings,
                        capture.PixelScaleX, capture.PixelScaleY, cancellationToken)));
                AddStructuredGeometry(structured, capture.StructuredObjects, cloneObjects,
                    pageInsertions, pageView, capture.PageSettings, paginator, pageNumber,
                    cancellationToken);
                request.CompletedMappedPages++;
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            viewer.Document = null;
            if (host.IsVisible)
                host.Close();
        }

        watch.Stop();
        return new WriterPaginationLayoutResult(capture.Generation, capture.DocumentIdentity,
            visiblePage, paginator.PageCount, pageStarts, mappedPages, pages.ToImmutable(),
            insertions.ToImmutable(), structured.ToImmutable(), capture.PageSettings,
            Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState(),
            watch.Elapsed.TotalMilliseconds);
    }

    private static ImmutableArray<int> GetPageStartOffsets(FlowDocument document,
        DynamicDocumentPaginator paginator, CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<int>(paginator.PageCount);
        for (var page = 0; page < paginator.PageCount; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (paginator.GetPagePosition(paginator.GetPage(page)) is not TextPointer position)
                throw new InvalidOperationException($"Page {page + 1} has no text position.");
            builder.Add(document.ContentStart.GetOffsetToPosition(position));
        }
        return builder.ToImmutable();
    }

    private static ImmutableArray<WriterPaginationInsertionGeometry> BuildPageInsertions(
        FlowDocument document,
        DynamicDocumentPaginator paginator,
        int pageNumber,
        DocumentPageView pageView,
        WriterPaginationPageSettings pageSettings,
        CancellationToken cancellationToken)
    {
        if (paginator.GetPagePosition(paginator.GetPage(pageNumber)) is not TextPointer pageStart)
            throw new InvalidOperationException($"Page {pageNumber + 1} has no start position.");
        var pageEnd = pageNumber + 1 < paginator.PageCount
            ? paginator.GetPagePosition(paginator.GetPage(pageNumber + 1)) as TextPointer
                ?? throw new InvalidOperationException($"Page {pageNumber + 2} has no start position.")
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
        }
    }

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

    private sealed class WorkerRequest(
        WriterPaginationCapture capture,
        TaskCompletionSource<WriterPaginationCompletion> completion,
        CancellationTokenSource cancellation)
    {
        internal WriterPaginationCapture Capture { get; } = capture;
        internal TaskCompletionSource<WriterPaginationCompletion> Completion { get; } = completion;
        internal CancellationTokenSource Cancellation { get; } = cancellation;
        internal int CompletedMappedPages { get; set; }
    }
}
