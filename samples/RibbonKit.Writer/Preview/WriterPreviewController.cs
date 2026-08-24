using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Documents;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Preview;

/// <summary>
/// Observes a live editor and its document page settings, rebuilding an isolated preview snapshot
/// at the trailing edge of a debounce interval.
/// </summary>
/// <remarks>
/// Text changes and page-setting changes share one pending operation.  A generation and operation
/// identity guard every callback, including callbacks from schedulers that cannot physically abort
/// an already queued action.
/// </remarks>
public sealed class WriterPreviewController : IDisposable
{
    private readonly IWriterPreviewScheduler _scheduler;
    private readonly Func<FlowDocument, DocumentPageSettings, WriterPreviewSnapshot> _snapshotFactory;
    private readonly TimeSpan _debounce;
    private WriterDocument _document;
    private PendingRebuild? _pending;
    private long _generation;
    private long _nextIdentity;
    private bool _disposed;

    /// <summary>Creates a debounced preview controller for a live editor/document pair.</summary>
    public WriterPreviewController(RichTextBox editor, WriterDocument document,
        TimeSpan? debounce = null, IWriterPreviewScheduler? scheduler = null,
        WriterPreviewCloneService? cloneService = null)
        : this(editor, document, debounce,
            scheduler ?? new WriterDispatcherPreviewScheduler(editor?.Dispatcher ??
                throw new ArgumentNullException(nameof(editor))),
            (cloneService ?? new WriterPreviewCloneService()).CreateSnapshot)
    {
    }

    internal WriterPreviewController(RichTextBox editor, WriterDocument document,
        TimeSpan? debounce, IWriterPreviewScheduler scheduler,
        Func<FlowDocument, DocumentPageSettings, WriterPreviewSnapshot> snapshotFactory)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _document = document ?? throw new ArgumentNullException(nameof(document));
        ValidateDocumentPair(Editor, _document);
        _debounce = debounce ?? TimeSpan.FromMilliseconds(250);
        if (_debounce < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(debounce), _debounce,
                "The preview debounce delay cannot be negative.");
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _snapshotFactory = snapshotFactory ?? throw new ArgumentNullException(nameof(snapshotFactory));
        Editor.TextChanged += OnEditorTextChanged;
        _document.PropertyChanged += OnDocumentPropertyChanged;
        RequestRebuild();
    }

    /// <summary>Gets the native editor observed by this controller.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Gets the Writer document whose page settings are observed.</summary>
    public WriterDocument Document => _document;

    /// <summary>Gets the latest non-stale preview snapshot.</summary>
    public WriterPreviewSnapshot? Snapshot { get; private set; }

    /// <summary>Gets whether a trailing-edge rebuild is pending.</summary>
    public bool IsPending => _pending is not null;

    /// <summary>Gets the current rebuild generation.</summary>
    public long Generation => _generation;

    /// <summary>Raised when a fresh preview snapshot is published.</summary>
    public event EventHandler? SnapshotChanged;

    /// <summary>Replaces the observed Writer document and requests one debounced rebuild.</summary>
    public void SetDocument(WriterDocument document)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(document);
        ValidateDocumentPair(Editor, document);
        if (ReferenceEquals(_document, document))
        {
            RequestRebuild();
            return;
        }

        _document.PropertyChanged -= OnDocumentPropertyChanged;
        _document = document;
        _document.PropertyChanged += OnDocumentPropertyChanged;
        RequestRebuild();
    }

    /// <summary>Requests a coalesced rebuild without synchronously cloning the live document.</summary>
    public void Refresh() => RequestRebuild();

    /// <summary>
    /// Gets the latest snapshot only when no newer content or page-setting rebuild is pending.
    /// </summary>
    public bool TryGetCurrentSnapshot([NotNullWhen(true)] out WriterPreviewSnapshot? snapshot)
    {
        ThrowIfDisposed();
        snapshot = Snapshot;
        return _pending is null && snapshot is not null;
    }

    /// <summary>Stops observing, releases the current snapshot, and ignores queued callbacks.</summary>
    /// <remarks>A bound preview view must synchronously detach its snapshot before this call.</remarks>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Editor.TextChanged -= OnEditorTextChanged;
        _document.PropertyChanged -= OnDocumentPropertyChanged;
        var pending = _pending;
        _pending = null;
        pending?.Dispose();
        Snapshot?.Dispose();
        Snapshot = null;
    }

    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e) => RequestRebuild();

    private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(WriterDocument.PageSettings))
            RequestRebuild();
    }

    private void RequestRebuild()
    {
        ThrowIfDisposed();
        _generation++;
        if (_pending is not null)
        {
            var previous = _pending;
            _pending = null;
            previous.Dispose();
        }

        var pending = new PendingRebuild(++_nextIdentity, _generation);
        _pending = pending;
        pending.Registration = _scheduler.Schedule(_debounce, () => CompleteRebuild(pending));
    }

    private void CompleteRebuild(PendingRebuild pending)
    {
        if (_disposed || _pending is null || !ReferenceEquals(_pending, pending) ||
            pending.Identity != _pending.Identity || pending.Generation != _generation)
            return;

        var currentDocument = _document;
        var currentContent = Editor.Document;
        WriterPreviewSnapshot snapshot;
        try
        {
            snapshot = _snapshotFactory(currentContent, currentDocument.PageSettings);
        }
        catch
        {
            // Keep this generation pending so TryGetCurrentSnapshot cannot expose the older
            // snapshot. A later content/settings change or explicit Refresh can retry safely.
            return;
        }
        _pending = null;
        if (_disposed || pending.Generation != _generation || !ReferenceEquals(_document, currentDocument) ||
            !ReferenceEquals(Editor.Document, currentContent))
        {
            snapshot.Dispose();
            return;
        }

        var previousSnapshot = Snapshot;
        Snapshot = snapshot;
        try
        {
            SnapshotChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            previousSnapshot?.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static void ValidateDocumentPair(RichTextBox editor, WriterDocument document)
    {
        if (!ReferenceEquals(editor.Document, document.Content))
            throw new ArgumentException(
                "The preview controller requires the editor and Writer document to share content.",
                nameof(document));
    }

    private sealed class PendingRebuild(long identity, long generation) : IDisposable
    {
        public long Identity { get; } = identity;
        public long Generation { get; } = generation;
        public IDisposable? Registration { get; set; }

        public void Dispose() => Registration?.Dispose();
    }
}
