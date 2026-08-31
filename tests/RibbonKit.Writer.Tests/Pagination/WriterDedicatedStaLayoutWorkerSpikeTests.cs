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
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Tests.Preview;
using Xunit;
using Xunit.Abstractions;

namespace RibbonKit.Writer.Tests.Pagination;

/// <summary>
/// Bounded W2-G spike proving whether isolated WPF pagination and public geometry can run on a
/// dedicated STA without transferring dispatcher-owned objects back to Writer's UI thread.
/// </summary>
[Collection(WriterPreviewTestCollection.Name)]
public sealed class WriterDedicatedStaLayoutWorkerSpikeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DedicatedStaResultMatchesTheAcceptedPaginatorAndReturnsImmutableGeometry()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = WorkerWorkspace.Create();
            var uiThreadId = Environment.CurrentManagedThreadId;
            var request = workspace.Controller.SetVisiblePage(2);

            Assert.True(await request.Publication);
            var result = Assert.IsType<WorkerLayoutResult>(workspace.Controller.Current);
            Assert.NotEqual(uiThreadId, result.WorkerThreadId);
            Assert.Equal(ApartmentState.STA, result.WorkerApartment);
            Assert.Equal(request.Generation, result.Generation);
            Assert.Equal(new[] { 1, 2, 3 }, result.MappedPages);
            Assert.NotEmpty(result.Geometry);
            Assert.All(result.Geometry, entry =>
            {
                Assert.Contains(entry.PageNumber, result.MappedPages);
                Assert.True(double.IsFinite(entry.Rectangle.X));
                Assert.True(double.IsFinite(entry.Rectangle.Y));
                Assert.True(entry.Rectangle.Height > 0);
            });

            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                workspace.Editor.Document, workspace.Settings);
            var acceptedPaginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            Assert.Equal(acceptedPaginator.PageCount, result.PageCount);
            Assert.Equal(GetPageStartOffsets(accepted.SourceClone, acceptedPaginator),
                result.PageStartOffsets);
            Assert.Equal(DocumentText(workspace.Editor.Document), result.ContentText);

            output.WriteLine($"Dedicated STA: UI thread={uiThreadId}; worker={result.WorkerThreadId}; " +
                $"pages={result.PageCount}; mapped={string.Join(',', result.MappedPages.Select(page => page + 1))}; " +
                $"entries={result.Geometry.Length}; capture={request.CaptureMilliseconds:0.###} ms; " +
                $"worker={result.WorkerMilliseconds:0.###} ms");
        });
    }

    [Fact]
    public async Task NewTypingStaysResponsiveAndRejectsTheOlderWorkerGeneration()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = WorkerWorkspace.Create();
            using var gate = new WorkerGate();
            var staleRequest = workspace.Controller.SetVisiblePage(2, gate);
            await gate.Started;
            Assert.False(staleRequest.WorkerCompletion.IsCompleted);

            var uiCallbackRan = false;
            await workspace.Editor.Dispatcher.InvokeAsync(() => uiCallbackRan = true,
                DispatcherPriority.Input);
            Assert.True(uiCallbackRan);

            var typingWatch = Stopwatch.StartNew();
            workspace.Editor.AppendText(" newest worker generation");
            typingWatch.Stop();
            var currentRequest = Assert.IsType<LayoutRequest>(workspace.Controller.LatestRequest);
            Assert.True(currentRequest.Generation > staleRequest.Generation);
            Assert.False(currentRequest.WorkerCompletion.IsCompleted);
            Assert.True(typingWatch.Elapsed < TimeSpan.FromMilliseconds(250),
                $"Typing plus immutable capture blocked the UI dispatcher for " +
                $"{typingWatch.Elapsed.TotalMilliseconds:0.###} ms.");

            gate.Release();
            Assert.False(await staleRequest.Publication);
            Assert.True(await currentRequest.Publication);

            var published = Assert.IsType<WorkerLayoutResult>(workspace.Controller.Current);
            Assert.Equal(currentRequest.Generation, published.Generation);
            Assert.Contains("newest worker generation", published.ContentText);
            Assert.Equal(DocumentText(workspace.Editor.Document), published.ContentText);
            Assert.Equal(new[] { 1, 2, 3 }, published.MappedPages);

            output.WriteLine($"Worker race: stale={staleRequest.Generation}; " +
                $"published={published.Generation}; typing+capture={typingWatch.Elapsed.TotalMilliseconds:0.###} ms; " +
                $"capture={currentRequest.CaptureMilliseconds:0.###} ms; " +
                $"worker={published.WorkerMilliseconds:0.###} ms");
        });
    }

    [Fact]
    public async Task PageSettingReflowMatchesAcceptedPaginatorAndPreservesLiveRangeFocusAndDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = WorkerWorkspace.Create();
            var authoritativeDocument = workspace.Editor.Document;
            var openingRequest = workspace.Controller.SetVisiblePage(1);
            Assert.True(await openingRequest.Publication);
            var openingResult = Assert.IsType<WorkerLayoutResult>(workspace.Controller.Current);

            var livePaginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                ((IDocumentPaginatorSource)authoritativeDocument).DocumentPaginator);
            livePaginator.PageSize = new Size(workspace.Settings.WidthDip,
                workspace.Settings.HeightDip);
            livePaginator.ComputePageCount();
            var pageTwoStart = Assert.IsType<TextPointer>(
                livePaginator.GetPagePosition(livePaginator.GetPage(1)));
            var anchor = FindInsertionPositionOnPage(livePaginator, pageTwoStart,
                LogicalDirection.Backward, 0);
            var moving = FindInsertionPositionOnPage(livePaginator, pageTwoStart,
                LogicalDirection.Forward, 1);
            workspace.Editor.Selection.Select(anchor, moving);
            FocusManager.SetFocusedElement(workspace.Window, workspace.Editor);
            workspace.Editor.Focus();
            Keyboard.Focus(workspace.Editor);
            var anchorOffset = authoritativeDocument.ContentStart.GetOffsetToPosition(
                workspace.Editor.Selection.Start);
            var movingOffset = authoritativeDocument.ContentStart.GetOffsetToPosition(
                workspace.Editor.Selection.End);

            var reflowSettings = DocumentPageSettings.A4(
                DocumentPageOrientation.Landscape,
                new DocumentPageMargins(48, 72, 60, 84));
            var reflowRequest = workspace.Controller.SetPageSettings(reflowSettings);
            Assert.True(await reflowRequest.Publication);
            var reflowResult = Assert.IsType<WorkerLayoutResult>(workspace.Controller.Current);

            Assert.Same(authoritativeDocument, workspace.Editor.Document);
            Assert.Equal(anchorOffset, authoritativeDocument.ContentStart.GetOffsetToPosition(
                workspace.Editor.Selection.Start));
            Assert.Equal(movingOffset, authoritativeDocument.ContentStart.GetOffsetToPosition(
                workspace.Editor.Selection.End));
            Assert.True(workspace.Editor.IsKeyboardFocusWithin || ReferenceEquals(
                FocusManager.GetFocusedElement(workspace.Window), workspace.Editor));
            Assert.Equal(CapturePageSettings(reflowSettings), reflowResult.PageSettings);
            Assert.False(openingResult.PageStartOffsets.SequenceEqual(
                reflowResult.PageStartOffsets));

            using var accepted = new WriterPreviewCloneService().CreateSnapshot(
                authoritativeDocument, reflowSettings);
            var acceptedPaginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                accepted.PrintPaginator);
            Assert.Equal(acceptedPaginator.PageCount, reflowResult.PageCount);
            Assert.Equal(GetPageStartOffsets(accepted.SourceClone, acceptedPaginator),
                reflowResult.PageStartOffsets);

            output.WriteLine($"Settings reflow: Letter pages={openingResult.PageCount}; " +
                $"A4 landscape pages={reflowResult.PageCount}; anchors={anchorOffset}-{movingOffset}; " +
                $"capture={reflowRequest.CaptureMilliseconds:0.###} ms; " +
                $"worker={reflowResult.WorkerMilliseconds:0.###} ms");
        });
    }

    [Fact]
    public async Task RapidEditBurstCancelsTheActiveMapAndCoalescesToOnlyTheLatestPendingGeneration()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var workspace = WorkerWorkspace.Create();
            using var pageGate = new PageCompletionGate();
            var activeRequest = workspace.Controller.SetVisiblePage(2, pageGate: pageGate);
            await pageGate.Reached;
            Assert.Equal(1, workspace.Worker.StartedCount);

            var burstWatch = Stopwatch.StartNew();
            var burstRequests = new List<LayoutRequest>();
            for (var index = 0; index < 12; index++)
            {
                workspace.Editor.AppendText($" burst-{index:D2}");
                burstRequests.Add(Assert.IsType<LayoutRequest>(
                    workspace.Controller.LatestRequest));
            }
            burstWatch.Stop();
            var finalRequest = burstRequests[^1];

            Assert.Equal(1, workspace.Worker.StartedCount);
            Assert.Equal(1, workspace.Worker.PendingCount);
            Assert.Equal(11, workspace.Worker.SupersededPendingCount);
            Assert.All(burstRequests, request => Assert.True(
                request.CaptureMilliseconds < 250,
                $"Generation {request.Generation} capture took " +
                $"{request.CaptureMilliseconds:0.###} ms."));
            Assert.True(burstWatch.Elapsed < TimeSpan.FromSeconds(2),
                $"The edit burst blocked the UI for {burstWatch.Elapsed.TotalMilliseconds:0.###} ms.");

            pageGate.Release();
            Assert.False(await activeRequest.Publication);
            foreach (var superseded in burstRequests.Take(burstRequests.Count - 1))
                Assert.False(await superseded.Publication);
            Assert.True(await finalRequest.Publication);

            var activeCompletion = await activeRequest.WorkerCompletion;
            Assert.Equal(WorkerCompletionKind.CanceledAfterStart, activeCompletion.Kind);
            Assert.Equal(1, activeCompletion.CompletedMappedPages);
            Assert.Equal(2, workspace.Worker.StartedCount);
            Assert.Equal(1, workspace.Worker.CompletedCount);
            Assert.Equal(1, workspace.Worker.CanceledActiveCount);
            Assert.Equal(11, workspace.Worker.SupersededPendingCount);

            var published = Assert.IsType<WorkerLayoutResult>(workspace.Controller.Current);
            Assert.Equal(finalRequest.Generation, published.Generation);
            Assert.Contains("burst-11", published.ContentText);
            Assert.Equal(DocumentText(workspace.Editor.Document), published.ContentText);

            output.WriteLine($"Latest-only burst: requests={burstRequests.Count + 1}; " +
                $"started={workspace.Worker.StartedCount}; completed={workspace.Worker.CompletedCount}; " +
                $"canceledActive={workspace.Worker.CanceledActiveCount}; " +
                $"supersededPending={workspace.Worker.SupersededPendingCount}; " +
                $"burst+capture={burstWatch.Elapsed.TotalMilliseconds:0.###} ms; " +
                $"finalWorker={published.WorkerMilliseconds:0.###} ms");
        });
    }

    private sealed class WorkerWorkspace : IDisposable
    {
        private readonly Window _window;

        private WorkerWorkspace(DocumentPageSettings settings, RichTextBox editor,
            DedicatedStaLayoutWorker worker, StaWorkerPaginationController controller,
            Window window)
        {
            Settings = settings;
            Editor = editor;
            Worker = worker;
            Controller = controller;
            _window = window;
        }

        public DocumentPageSettings Settings { get; }
        public RichTextBox Editor { get; }
        public Window Window => _window;
        public DedicatedStaLayoutWorker Worker { get; }
        public StaWorkerPaginationController Controller { get; }

        public static WorkerWorkspace Create()
        {
            var settings = DocumentPageSettings.Letter();
            var document = CreateParagraphDocument(settings, 180);
            var editor = new RichTextBox
            {
                Document = document,
                IsUndoEnabled = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var window = new Window
            {
                Content = editor,
                Width = settings.WidthDip + 100,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                Opacity = 0.01
            };
            window.Show();
            UpdateLayout(window);
            ApplyPageSettings(document, settings);
            UpdateLayout(window);

            var worker = new DedicatedStaLayoutWorker();
            var controller = new StaWorkerPaginationController(editor, settings, worker);
            return new WorkerWorkspace(settings, editor, worker, controller, window);
        }

        public void Dispose()
        {
            Controller.Dispose();
            Worker.Dispose();
            if (_window.IsVisible)
                _window.Close();
        }
    }

    private sealed class StaWorkerPaginationController : IDisposable
    {
        private readonly RichTextBox _editor;
        private DocumentPageSettings _settings;
        private readonly DedicatedStaLayoutWorker _worker;
        private int _visiblePage;
        private bool _disposed;

        public StaWorkerPaginationController(RichTextBox editor, DocumentPageSettings settings,
            DedicatedStaLayoutWorker worker)
        {
            _editor = editor;
            _settings = settings;
            _worker = worker;
            _editor.TextChanged += OnTextChanged;
        }

        public long RequestedGeneration { get; private set; }
        public WorkerLayoutResult? Current { get; private set; }
        public LayoutRequest? LatestRequest { get; private set; }

        public LayoutRequest SetVisiblePage(int pageNumber, WorkerGate? gate = null,
            PageCompletionGate? pageGate = null)
        {
            _visiblePage = pageNumber;
            return RequestRebuild(gate, pageGate);
        }

        public LayoutRequest SetPageSettings(DocumentPageSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            return RequestRebuild();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _editor.TextChanged -= OnTextChanged;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e) => RequestRebuild();

        private LayoutRequest RequestRebuild(WorkerGate? gate = null,
            PageCompletionGate? pageGate = null)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var generation = ++RequestedGeneration;
            var sourceDocument = _editor.Document;
            var captureWatch = Stopwatch.StartNew();
            byte[] package;
            using (var stream = new MemoryStream())
            {
                new TextRange(sourceDocument.ContentStart, sourceDocument.ContentEnd)
                    .Save(stream, DataFormats.XamlPackage);
                package = stream.ToArray();
            }
            var capture = new ImmutableLayoutCapture(generation, _visiblePage,
                package.ToImmutableArray(),
                DocumentText(sourceDocument), CaptureFormatting(sourceDocument),
                CapturePageSettings(_settings));
            captureWatch.Stop();

            var workerCompletion = _worker.Queue(capture, gate, pageGate);
            var publication = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new LayoutRequest(generation, captureWatch.Elapsed.TotalMilliseconds,
                sourceDocument, workerCompletion, publication.Task);
            LatestRequest = request;
            _ = PublishWhenReadyAsync(request, publication);
            return request;
        }

        private async Task PublishWhenReadyAsync(LayoutRequest request,
            TaskCompletionSource<bool> publication)
        {
            try
            {
                var completion = await request.WorkerCompletion.ConfigureAwait(false);
                await _editor.Dispatcher.InvokeAsync(() =>
                {
                    var isCurrent = completion.Kind == WorkerCompletionKind.Completed &&
                        completion.Result is not null && !_disposed &&
                        request.Generation == RequestedGeneration &&
                        ReferenceEquals(request.SourceDocument, _editor.Document);
                    if (isCurrent)
                        Current = completion.Result;
                    publication.TrySetResult(isCurrent);
                }, DispatcherPriority.DataBind);
            }
            catch (Exception exception)
            {
                publication.TrySetException(exception);
            }
        }
    }

    private sealed class DedicatedStaLayoutWorker : IDisposable
    {
        private readonly object _sync = new();
        private readonly AutoResetEvent _workAvailable = new(false);
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _ready = new();
        private WorkerRequest? _active;
        private WorkerRequest? _pending;
        private bool _stopping;
        private bool _disposed;
        private int _startedCount;
        private int _completedCount;
        private int _canceledActiveCount;
        private int _supersededPendingCount;

        public DedicatedStaLayoutWorker()
        {
            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "Writer W2-G pagination spike"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            if (!_ready.Wait(TimeSpan.FromSeconds(2)))
                throw new TimeoutException("The dedicated pagination STA did not start.");
        }

        public int StartedCount => Volatile.Read(ref _startedCount);
        public int CompletedCount => Volatile.Read(ref _completedCount);
        public int CanceledActiveCount => Volatile.Read(ref _canceledActiveCount);
        public int SupersededPendingCount => Volatile.Read(ref _supersededPendingCount);
        public int PendingCount
        {
            get
            {
                lock (_sync)
                    return _pending is null ? 0 : 1;
            }
        }

        public Task<WorkerCompletion> Queue(ImmutableLayoutCapture capture,
            WorkerGate? gate = null, PageCompletionGate? pageGate = null)
        {
            var completion = new TaskCompletionSource<WorkerCompletion>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new WorkerRequest(capture, gate, pageGate, completion,
                new CancellationTokenSource());
            WorkerRequest? superseded;
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                superseded = _pending;
                _pending = request;
                if (superseded is not null)
                    Interlocked.Increment(ref _supersededPendingCount);
                _active?.Cancellation.Cancel();
            }
            if (superseded is not null)
            {
                superseded.Completion.TrySetResult(new WorkerCompletion(
                    WorkerCompletionKind.SupersededBeforeStart, null, 0));
                superseded.Cancellation.Dispose();
            }
            _workAvailable.Set();
            return completion.Task;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            WorkerRequest? pending;
            WorkerRequest? active;
            lock (_sync)
            {
                _disposed = true;
                _stopping = true;
                pending = _pending;
                _pending = null;
                active = _active;
                active?.Cancellation.Cancel();
            }
            active?.Gate?.Release();
            active?.PageGate?.Release();
            if (pending is not null)
            {
                pending.Completion.TrySetResult(new WorkerCompletion(
                    WorkerCompletionKind.SupersededBeforeStart, null, 0));
                pending.Cancellation.Dispose();
            }
            _workAvailable.Set();
            if (!_thread.Join(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("The dedicated pagination STA did not stop.");
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
                    request.Gate?.WaitUntilReleased();
                    request.Cancellation.Token.ThrowIfCancellationRequested();
                    var result = Build(request, request.Cancellation.Token);
                    request.Cancellation.Token.ThrowIfCancellationRequested();
                    Interlocked.Increment(ref _completedCount);
                    request.Completion.TrySetResult(new WorkerCompletion(
                        WorkerCompletionKind.Completed, result, request.CompletedMappedPages));
                }
                catch (OperationCanceledException)
                    when (request.Cancellation.IsCancellationRequested)
                {
                    Interlocked.Increment(ref _canceledActiveCount);
                    request.Completion.TrySetResult(new WorkerCompletion(
                        WorkerCompletionKind.CanceledAfterStart, null,
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

        private static WorkerLayoutResult Build(WorkerRequest request,
            CancellationToken cancellationToken)
        {
            var capture = request.Capture;
            var watch = Stopwatch.StartNew();
            cancellationToken.ThrowIfCancellationRequested();
            var clone = new FlowDocument();
            using (var stream = new MemoryStream(capture.XamlPackage.ToArray(), writable: false))
            {
                new TextRange(clone.ContentStart, clone.ContentEnd)
                    .Load(stream, DataFormats.XamlPackage);
            }
            ApplyFormatting(clone, capture.Formatting);
            ApplyPageSettings(clone, capture.PageSettings);
            cancellationToken.ThrowIfCancellationRequested();

            var paginator = Assert.IsAssignableFrom<DynamicDocumentPaginator>(
                ((IDocumentPaginatorSource)clone).DocumentPaginator);
            paginator.PageSize = new Size(capture.PageSettings.WidthDip,
                capture.PageSettings.HeightDip);
            paginator.ComputePageCount();
            cancellationToken.ThrowIfCancellationRequested();
            if (capture.VisiblePage < 0 || capture.VisiblePage >= paginator.PageCount)
                throw new ArgumentOutOfRangeException(nameof(capture.VisiblePage));

            var pageStarts = GetPageStartOffsets(clone, paginator).ToImmutableArray();
            var mappedPages = Enumerable.Range(Math.Max(0, capture.VisiblePage - 1),
                    Math.Min(paginator.PageCount - 1, capture.VisiblePage + 1) -
                    Math.Max(0, capture.VisiblePage - 1) + 1)
                .ToImmutableArray();
            var viewer = new FlowDocumentPageViewer { Document = clone };
            var window = new Window
            {
                Content = viewer,
                Width = capture.PageSettings.WidthDip + 120,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                Opacity = 0.01
            };
            var geometry = ImmutableArray.CreateBuilder<WorkerPageGeometry>();
            try
            {
                window.Show();
                UpdateLayout(window);
                foreach (var pageNumber in mappedPages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    viewer.GoToPage(pageNumber + 1);
                    UpdateLayout(window);
                    var pageView = Assert.Single(viewer.PageViews,
                        page => page.PageNumber == pageNumber);
                    var pageStart = Assert.IsType<TextPointer>(
                        paginator.GetPagePosition(paginator.GetPage(pageNumber)));
                    var pageEnd = pageNumber + 1 < paginator.PageCount
                        ? Assert.IsType<TextPointer>(
                            paginator.GetPagePosition(paginator.GetPage(pageNumber + 1)))
                        : clone.ContentEnd;
                    var insertionBatch = 0;
                    for (var position = pageStart.GetInsertionPosition(LogicalDirection.Forward);
                         position is not null && position.CompareTo(pageEnd) < 0;
                         position = position.GetNextInsertionPosition(LogicalDirection.Forward))
                    {
                        if (++insertionBatch % 128 == 0)
                            cancellationToken.ThrowIfCancellationRequested();
                        if (paginator.GetPageNumber(position) != pageNumber)
                            continue;
                        var rect = position.GetCharacterRect(LogicalDirection.Forward);
                        if (rect.IsEmpty || !double.IsFinite(rect.X) ||
                            !double.IsFinite(rect.Y) || rect.Height <= 0)
                            continue;
                        Assert.InRange(rect.Left, -0.5, pageView.ActualWidth + 0.5);
                        Assert.InRange(rect.Bottom, -0.5, pageView.ActualHeight + 0.5);
                        geometry.Add(new WorkerPageGeometry(
                            clone.ContentStart.GetOffsetToPosition(position), pageNumber,
                            new ImmutablePageRectangle(rect.X, rect.Y, rect.Width, rect.Height)));
                    }
                    request.CompletedMappedPages++;
                    request.PageGate?.WaitAfterPage(request.CompletedMappedPages);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            finally
            {
                viewer.Document = null;
                if (window.IsVisible)
                    window.Close();
            }
            watch.Stop();
            return new WorkerLayoutResult(capture.Generation, capture.ContentText,
                capture.PageSettings, paginator.PageCount, pageStarts, mappedPages,
                geometry.ToImmutable(),
                Environment.CurrentManagedThreadId, Thread.CurrentThread.GetApartmentState(),
                watch.Elapsed.TotalMilliseconds);
        }
    }

    private sealed class WorkerGate : IDisposable
    {
        private readonly TaskCompletionSource _started = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _release = new();

        public Task Started => _started.Task;

        public void Release() => _release.Set();

        public void WaitUntilReleased()
        {
            _started.TrySetResult();
            if (!_release.Wait(TimeSpan.FromSeconds(3)))
                throw new TimeoutException("The test did not release the pagination worker.");
        }

        public void Dispose()
        {
            _release.Set();
            _release.Dispose();
        }
    }

    private sealed class PageCompletionGate : IDisposable
    {
        private readonly TaskCompletionSource _reached = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ManualResetEventSlim _release = new();

        public Task Reached => _reached.Task;

        public void Release() => _release.Set();

        public void WaitAfterPage(int completedPageCount)
        {
            if (completedPageCount != 1)
                return;
            _reached.TrySetResult();
            if (!_release.Wait(TimeSpan.FromSeconds(3)))
                throw new TimeoutException("The test did not release the page checkpoint.");
        }

        public void Dispose()
        {
            _release.Set();
            _release.Dispose();
        }
    }

    private sealed class WorkerRequest(ImmutableLayoutCapture capture, WorkerGate? gate,
        PageCompletionGate? pageGate, TaskCompletionSource<WorkerCompletion> completion,
        CancellationTokenSource cancellation)
    {
        public ImmutableLayoutCapture Capture { get; } = capture;
        public WorkerGate? Gate { get; } = gate;
        public PageCompletionGate? PageGate { get; } = pageGate;
        public TaskCompletionSource<WorkerCompletion> Completion { get; } = completion;
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public int CompletedMappedPages { get; set; }
    }

    private sealed record ImmutableLayoutCapture(long Generation, int VisiblePage,
        ImmutableArray<byte> XamlPackage, string ContentText, ImmutableFlowFormatting Formatting,
        ImmutablePageSettings PageSettings);

    private sealed record ImmutableFlowFormatting(string FontFamily, double FontSize,
        int FontWeight, int FontStretch, string Language, FlowDirection FlowDirection,
        TextAlignment TextAlignment, double LineHeight, LineStackingStrategy LineStackingStrategy,
        bool IsHyphenationEnabled, bool IsOptimalParagraphEnabled);

    private sealed record ImmutablePageSettings(double WidthDip, double HeightDip,
        double ContentWidthDip, double LeftMarginDip, double TopMarginDip,
        double RightMarginDip, double BottomMarginDip);

    private sealed record LayoutRequest(long Generation, double CaptureMilliseconds,
        FlowDocument SourceDocument, Task<WorkerCompletion> WorkerCompletion,
        Task<bool> Publication);

    private enum WorkerCompletionKind
    {
        Completed,
        SupersededBeforeStart,
        CanceledAfterStart
    }

    private sealed record WorkerCompletion(WorkerCompletionKind Kind,
        WorkerLayoutResult? Result, int CompletedMappedPages);

    private sealed record WorkerLayoutResult(long Generation, string ContentText,
        ImmutablePageSettings PageSettings, int PageCount,
        ImmutableArray<int> PageStartOffsets, ImmutableArray<int> MappedPages,
        ImmutableArray<WorkerPageGeometry> Geometry, int WorkerThreadId,
        ApartmentState WorkerApartment, double WorkerMilliseconds);

    private readonly record struct WorkerPageGeometry(int SourceOffset, int PageNumber,
        ImmutablePageRectangle Rectangle);

    private readonly record struct ImmutablePageRectangle(double X, double Y,
        double Width, double Height);

    private static ImmutableFlowFormatting CaptureFormatting(FlowDocument document) =>
        new(document.FontFamily.Source, document.FontSize, document.FontWeight.ToOpenTypeWeight(),
            document.FontStretch.ToOpenTypeStretch(), document.Language.IetfLanguageTag,
            document.FlowDirection, document.TextAlignment, document.LineHeight,
            document.LineStackingStrategy, document.IsHyphenationEnabled,
            document.IsOptimalParagraphEnabled);

    private static ImmutablePageSettings CapturePageSettings(DocumentPageSettings settings) =>
        new(settings.WidthDip, settings.HeightDip, settings.ContentWidthDip,
            settings.Margins.LeftDip, settings.Margins.TopDip,
            settings.Margins.RightDip, settings.Margins.BottomDip);

    private static void ApplyFormatting(FlowDocument document, ImmutableFlowFormatting formatting)
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
    }

    private static void ApplyPageSettings(FlowDocument document, DocumentPageSettings settings) =>
        ApplyPageSettings(document, CapturePageSettings(settings));

    private static void ApplyPageSettings(FlowDocument document, ImmutablePageSettings settings)
    {
        document.PageWidth = settings.WidthDip;
        document.PageHeight = settings.HeightDip;
        document.PagePadding = new Thickness(settings.LeftMarginDip, settings.TopMarginDip,
            settings.RightMarginDip, settings.BottomMarginDip);
        document.ColumnWidth = settings.ContentWidthDip;
        document.ColumnGap = 0;
        document.IsColumnWidthFlexible = false;
    }

    private static FlowDocument CreateParagraphDocument(DocumentPageSettings settings, int count)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14
        };
        ApplyPageSettings(document, settings);
        for (var index = 0; index < count; index++)
        {
            document.Blocks.Add(new Paragraph(new Run(
                $"Paragraph {index:D3}: dedicated STA pagination worker corpus."))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });
        }
        return document;
    }

    private static int[] GetPageStartOffsets(FlowDocument document,
        DynamicDocumentPaginator paginator)
    {
        var offsets = new int[paginator.PageCount];
        for (var pageNumber = 0; pageNumber < paginator.PageCount; pageNumber++)
        {
            var position = Assert.IsType<TextPointer>(
                paginator.GetPagePosition(paginator.GetPage(pageNumber)));
            offsets[pageNumber] = document.ContentStart.GetOffsetToPosition(position);
        }
        return offsets;
    }

    private static TextPointer FindInsertionPositionOnPage(DynamicDocumentPaginator paginator,
        TextPointer origin, LogicalDirection direction, int pageNumber)
    {
        for (var position = origin.GetInsertionPosition(direction);
             position is not null;
             position = position.GetNextInsertionPosition(direction))
        {
            if (paginator.GetPageNumber(position) == pageNumber)
                return position;
        }
        throw new Xunit.Sdk.XunitException(
            $"No insertion position was found on page {pageNumber + 1}.");
    }

    private static string DocumentText(FlowDocument document) =>
        new TextRange(document.ContentStart, document.ContentEnd).Text;

    private static void UpdateLayout(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
    }
}
