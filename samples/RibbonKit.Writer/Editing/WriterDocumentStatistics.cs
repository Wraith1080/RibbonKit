using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace RibbonKit.Writer.Editing;

/// <summary>Immutable word and character counts for one FlowDocument snapshot.</summary>
/// <remarks>
/// Characters are Unicode text elements and include spaces, tabs, soft line breaks and other text
/// whitespace. FlowDocument paragraph separators and its terminal structural break are excluded.
/// Embedded UI/content objects are word separators and contribute no character. They are never
/// concatenated with adjacent text while counting.
/// Words are maximal runs of Unicode letters, numbers or combining marks; an apostrophe joins two
/// such runs when it is between word characters. Other punctuation, symbols and whitespace separate
/// words. Counts are computed on the dispatcher that owns the editor.
/// </remarks>
public readonly record struct WriterDocumentStatisticsSnapshot(int Words, int Characters)
{
    /// <summary>Gets the number of Unicode word tokens.</summary>
    public int WordCount => Words;

    /// <summary>Gets the number of Unicode text elements, including spaces.</summary>
    public int CharacterCount => Characters;

    /// <summary>An empty statistics value.</summary>
    public static WriterDocumentStatisticsSnapshot Empty => new(0, 0);

    internal static WriterDocumentStatisticsSnapshot Calculate(WriterDocumentTextSnapshot snapshot)
    {
        var wordsText = new StringBuilder();
        var segment = new StringBuilder();
        var characterCount = 0L;

        foreach (var unit in snapshot.Units)
        {
            if (unit.IsStructuralBoundary)
            {
                characterCount += CountTextElements(segment);
                segment.Clear();
                wordsText.Append(' ');
                continue;
            }

            if (unit.IsNonTextBarrier)
            {
                characterCount += CountTextElements(segment);
                segment.Clear();
                wordsText.Append(' ');
                continue;
            }

            segment.Append(unit.Character);
            wordsText.Append(unit.Character);
        }

        characterCount += CountTextElements(segment);
        var wordCount = CountWords(wordsText.ToString());
        return new WriterDocumentStatisticsSnapshot(ToInt32(wordCount), ToInt32(characterCount));
    }

    private static int CountTextElements(StringBuilder value) =>
        value.Length == 0 ? 0 : StringInfo.ParseCombiningCharacters(value.ToString()).Length;

    private static long CountWords(string value)
    {
        if (value.Length == 0)
            return 0;

        var wordFlags = new bool[value.Length];
        var apostrophes = new bool[value.Length];
        for (var index = 0; index < value.Length;)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            var isWord = IsWordCategory(category);
            wordFlags[index] = isWord;
            apostrophes[index] = value[index] is '\'' or '\u2019';
            if (char.IsHighSurrogate(value[index]) && index + 1 < value.Length &&
                char.IsLowSurrogate(value[index + 1]))
            {
                wordFlags[index + 1] = isWord;
                index += 2;
            }
            else
            {
                index++;
            }
        }

        var inWord = false;
        long count = 0;
        for (var index = 0; index < value.Length; index++)
        {
            if (wordFlags[index])
            {
                if (!inWord)
                {
                    count++;
                    inWord = true;
                }
                continue;
            }

            if (apostrophes[index] && index > 0 && index + 1 < value.Length &&
                IsWordAt(wordFlags, index - 1) && IsWordAt(wordFlags, index + 1))
                continue;

            inWord = false;
        }

        return count;
    }

    private static bool IsWordAt(bool[] flags, int index) =>
        index >= 0 && index < flags.Length && flags[index];

    private static bool IsWordCategory(UnicodeCategory category) => category switch
    {
        UnicodeCategory.UppercaseLetter or
        UnicodeCategory.LowercaseLetter or
        UnicodeCategory.TitlecaseLetter or
        UnicodeCategory.ModifierLetter or
        UnicodeCategory.OtherLetter or
        UnicodeCategory.NonSpacingMark or
        UnicodeCategory.SpacingCombiningMark or
        UnicodeCategory.EnclosingMark or
        UnicodeCategory.DecimalDigitNumber or
        UnicodeCategory.LetterNumber or
        UnicodeCategory.OtherNumber => true,
        _ => false
    };

    private static int ToInt32(long value) => value >= int.MaxValue ? int.MaxValue : (int)value;
}

/// <summary>Schedules one dispatcher-affine statistics callback after a debounce delay.</summary>
public interface IWriterDocumentStatisticsScheduler
{
    /// <summary>Schedules a callback. Implementations must not run it on a worker thread.</summary>
    IDisposable Schedule(TimeSpan delay, Action callback);
}

/// <summary>Schedules Writer statistics work through a WPF dispatcher.</summary>
public sealed class WriterDispatcherDocumentStatisticsScheduler : IWriterDocumentStatisticsScheduler
{
    private readonly Dispatcher _dispatcher;

    /// <summary>Creates a scheduler for a dispatcher.</summary>
    public WriterDispatcherDocumentStatisticsScheduler(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "The debounce delay cannot be negative.");
        if (!_dispatcher.CheckAccess())
            throw new InvalidOperationException("Statistics scheduling must occur on the editor dispatcher.");

        if (delay == TimeSpan.Zero)
        {
            var operation = _dispatcher.BeginInvoke(DispatcherPriority.Background, callback);
            return new DispatcherOperationRegistration(operation);
        }

        var timer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher)
        {
            Interval = delay
        };
        TimerRegistration? registration = null;
        EventHandler handler = (_, _) =>
        {
            registration!.Dispose();
            callback();
        };
        timer.Tick += handler;
        registration = new TimerRegistration(timer, handler);
        timer.Start();
        return registration;
    }

    private sealed class TimerRegistration(DispatcherTimer timer, EventHandler handler) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            timer.Stop();
            timer.Tick -= handler;
        }
    }

    private sealed class DispatcherOperationRegistration(DispatcherOperation operation) : IDisposable
    {
        public void Dispose() => operation.Abort();
    }
}

/// <summary>
/// Observes a native editor and coalesces text/document changes into dispatcher-affine statistics
/// scans.
/// </summary>
/// <remarks>
/// TextChanged and document replacement only increment a generation and reset one trailing-edge
/// callback; they never synchronously rescan the FlowDocument. A callback captures its pending
/// operation identity, document reference and generation and discards stale results after edits,
/// replacement or disposal. The default debounce delay is 250 ms. Inject
/// <see cref="IWriterDocumentStatisticsScheduler"/> for deterministic tests.
/// </remarks>
public sealed class WriterDocumentStatistics : IDisposable, INotifyPropertyChanged
{
    private readonly IWriterDocumentStatisticsScheduler _scheduler;
    private readonly Dispatcher _dispatcher;
    private readonly TimeSpan _debounce;
    private FlowDocument _observedDocument;
    private PendingScan? _pending;
    private long _nextScheduleIdentity;
    private long _generation;
    private bool _disposed;
    private bool _hasPublished;

    /// <summary>Creates a debounced statistics observer with the default WPF dispatcher scheduler.</summary>
    public WriterDocumentStatistics(RichTextBox editor, TimeSpan? debounce = null,
        IWriterDocumentStatisticsScheduler? scheduler = null)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _dispatcher = editor.Dispatcher;
        _debounce = debounce ?? TimeSpan.FromMilliseconds(250);
        if (_debounce < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(debounce), _debounce,
                "The debounce delay cannot be negative.");
        _scheduler = scheduler ?? new WriterDispatcherDocumentStatisticsScheduler(_dispatcher);
        Statistics = WriterDocumentStatisticsSnapshot.Empty;
        _observedDocument = Editor.Document;
        Editor.TextChanged += OnTextChanged;
        ScheduleScan(resetPending: false);
    }

    /// <summary>Gets the native editor observed by this service.</summary>
    public RichTextBox Editor { get; }

    /// <summary>Gets the latest published statistics snapshot.</summary>
    public WriterDocumentStatisticsSnapshot Statistics { get; private set; }

    /// <summary>Gets whether a debounced scan is waiting to publish.</summary>
    public bool IsPending => _pending != null;

    /// <summary>Gets the number of scheduled document generations.</summary>
    public long Generation => _generation;

    /// <summary>Raised only when a non-stale snapshot is published.</summary>
    public event EventHandler? StatisticsChanged;

    /// <summary>Raised when observable state changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Requests one coalesced scan without touching the document synchronously.</summary>
    public void Refresh()
    {
        ThrowIfDisposed();
        if (!ReferenceEquals(_observedDocument, Editor.Document))
            _observedDocument = Editor.Document;
        _generation++;
        ScheduleScan(resetPending: true);
    }

    /// <summary>Stops callbacks and unsubscribes without changing the native document.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var pending = _pending;
        _pending = null;
        pending?.Dispose();
        Editor.TextChanged -= OnTextChanged;
        OnPropertyChanged(nameof(IsPending));
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_disposed)
            return;
        if (!ReferenceEquals(_observedDocument, Editor.Document))
            _observedDocument = Editor.Document;
        _generation++;
        ScheduleScan(resetPending: true);
    }

    private void ScheduleScan(bool resetPending)
    {
        if (_disposed)
            return;

        if (resetPending && _pending is not null)
        {
            var previous = _pending;
            _pending = null;
            previous.Dispose();
        }

        if (_pending is not null)
            return;

        var generation = _generation;
        var document = Editor.Document;
        var pending = new PendingScan(++_nextScheduleIdentity, generation, document);
        _pending = pending;
        pending.Registration = _scheduler.Schedule(_debounce, () => CompleteScan(pending));
        OnPropertyChanged(nameof(IsPending));
    }

    private void CompleteScan(PendingScan pending)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => CompleteScan(pending)));
            return;
        }

        if (_disposed || _pending is null || _pending.Identity != pending.Identity ||
            !ReferenceEquals(_pending, pending))
            return;

        _pending = null;
        OnPropertyChanged(nameof(IsPending));
        if (pending.Generation != _generation || !ReferenceEquals(pending.Document, Editor.Document))
        {
            _observedDocument = Editor.Document;
            ScheduleScan(resetPending: false);
            return;
        }

        var value = WriterDocumentStatisticsSnapshot.Calculate(
            WriterDocumentTextSnapshot.Create(pending.Document));
        if (!_hasPublished || value != Statistics)
        {
            _hasPublished = true;
            Statistics = value;
            OnPropertyChanged(nameof(Statistics));
            StatisticsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private sealed class PendingScan(long identity, long generation, FlowDocument document) : IDisposable
    {
        public long Identity { get; } = identity;
        public long Generation { get; } = generation;
        public FlowDocument Document { get; } = document;
        public IDisposable? Registration { get; set; }

        public void Dispose() => Registration?.Dispose();
    }
}
