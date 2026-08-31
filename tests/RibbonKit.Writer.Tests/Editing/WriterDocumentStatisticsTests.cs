using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.Editing;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

public sealed class WriterDocumentStatisticsTests
{
    [Fact]
    public void InitialAndTextChangedScansAreDeferredAndCoalesced()
    {
        StaTestHelper.Run(() =>
        {
            var scheduler = new ManualScheduler();
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("one two"))), scheduler);
            Assert.True(fixture.Statistics.IsPending);
            Assert.Equal(0, fixture.Statistics.Statistics.Words);
            Assert.Equal(1, scheduler.ScheduledCount);

            scheduler.RunNext();
            Assert.Equal(2, fixture.Statistics.Statistics.Words);
            Assert.Equal(7, fixture.Statistics.Statistics.Characters);

            fixture.Editor.Selection.Select(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentEnd);
            fixture.Editor.Selection.Text = "one two three";
            fixture.Editor.Selection.Select(fixture.Editor.Document.ContentStart,
                fixture.Editor.Document.ContentEnd);
            fixture.Editor.Selection.Text = "one two three four";
            Assert.True(scheduler.CanceledCount >= 1);
            Assert.Equal(1, scheduler.PendingCount);
            Assert.Equal(2, fixture.Statistics.Statistics.Words);
            while (fixture.Statistics.IsPending)
                scheduler.RunNext();
            Assert.Equal(4, fixture.Statistics.Statistics.Words);
        });
    }

    [Fact]
    public void WordsIncludeUnicodeTextAndSpacesButExcludeParagraphStructure()
    {
        StaTestHelper.Run(() =>
        {
            var scheduler = new ManualScheduler();
            var document = new FlowDocument();
            document.Blocks.Add(new Paragraph(new Run("Hello   world")));
            document.Blocks.Add(new Paragraph(new Run("next\tline")));
            using var fixture = CreateFixture(document, scheduler);
            scheduler.RunNext();

            Assert.Equal(4, fixture.Statistics.Statistics.Words);
            Assert.Equal(22, fixture.Statistics.Statistics.Characters);
        });
    }

    [Fact]
    public void UnicodeCombiningTextAndApostropheHaveExplicitTokenSemantics()
    {
        StaTestHelper.Run(() =>
        {
            var scheduler = new ManualScheduler();
            using var fixture = CreateFixture(
                new FlowDocument(new Paragraph(new Run("can't café 😀"))), scheduler);
            scheduler.RunNext();

            Assert.Equal(2, fixture.Statistics.Statistics.Words);
            Assert.Equal(12, fixture.Statistics.Statistics.Characters);
        });
    }

    [Fact]
    public void StaleDocumentCallbackIsDroppedAndReplacementCanBeRefreshed()
    {
        StaTestHelper.Run(() =>
        {
            var scheduler = new ManualScheduler();
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("old"))), scheduler);
            var oldDocument = fixture.Editor.Document;
            fixture.Editor.Document = new FlowDocument(new Paragraph(new Run("new text")));
            fixture.Statistics.Refresh();

            Assert.Equal(1, scheduler.PendingCount);
            scheduler.RunNext();
            Assert.Equal(0, fixture.Statistics.Statistics.Words);
            Assert.True(fixture.Statistics.IsPending);
            scheduler.RunLatest();
            Assert.Equal(2, fixture.Statistics.Statistics.Words);
            Assert.NotSame(oldDocument, fixture.Editor.Document);
        });
    }

    [Fact]
    public void EmptyDocumentPublishesZeroCounts()
    {
        StaTestHelper.Run(() =>
        {
            var scheduler = new ManualScheduler();
            using var fixture = CreateFixture(new FlowDocument(), scheduler);
            scheduler.RunNext();
            Assert.Equal(0, fixture.Statistics.Statistics.Words);
            Assert.Equal(0, fixture.Statistics.Statistics.Characters);
        });
    }

    [Fact]
    public void EachEditResetsTheTrailingEdgeAndStaleCallbacksCannotClearPendingState()
    {
        StaTestHelper.Run(() =>
        {
            var scheduler = new ManualScheduler();
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("one"))), scheduler);
            scheduler.RunNext();

            fixture.Statistics.Refresh();
            fixture.Statistics.Refresh();
            fixture.Statistics.Refresh();

            Assert.Equal(2, scheduler.CanceledCount);
            Assert.Equal(1, scheduler.PendingCount);
            scheduler.RunNext();
            Assert.True(fixture.Statistics.IsPending);
            scheduler.RunNext();
            Assert.True(fixture.Statistics.IsPending);
            scheduler.RunLatest();
            Assert.False(fixture.Statistics.IsPending);
            Assert.Equal(1, fixture.Statistics.Statistics.Words);
            Assert.Equal(3, fixture.Statistics.Statistics.Characters);
        });
    }

    [Fact]
    public void EmbeddedObjectSeparatesWordsAndIsNotACharacter()
    {
        StaTestHelper.Run(() =>
        {
            var document = new FlowDocument();
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run("a"));
            paragraph.Inlines.Add(new InlineUIContainer(new Button { Content = "object" }));
            paragraph.Inlines.Add(new Run("b"));
            document.Blocks.Add(paragraph);
            var scheduler = new ManualScheduler();
            using var fixture = CreateFixture(document, scheduler);
            scheduler.RunNext();

            Assert.Equal(2, fixture.Statistics.Statistics.Words);
            Assert.Equal(2, fixture.Statistics.Statistics.Characters);
        });
    }

    [Fact]
    public void DefaultDispatcherSchedulerPublishesWithoutBlockingSleep()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(new Run("default scheduler")))
            };
            var window = new Window { Content = editor, Width = 220, Height = 140, ShowInTaskbar = false };
            window.Show();
            try
            {
                using var statistics = new WriterDocumentStatistics(editor,
                    TimeSpan.FromMilliseconds(1));
                var frame = new DispatcherFrame();
                var timeout = new DispatcherTimer(DispatcherPriority.ApplicationIdle, editor.Dispatcher)
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timeout.Tick += (_, _) => frame.Continue = false;
                timeout.Start();
                statistics.StatisticsChanged += (_, _) => frame.Continue = false;
                Dispatcher.PushFrame(frame);
                timeout.Stop();

                Assert.Equal(2, statistics.Statistics.Words);
                Assert.False(statistics.IsPending);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void DisposeDropsQueuedCallbackAndPublishesNoStaleResult()
    {
        StaTestHelper.Run(() =>
        {
            var scheduler = new ManualScheduler();
            using var fixture = CreateFixture(new FlowDocument(new Paragraph(new Run("text"))), scheduler);
            var events = 0;
            fixture.Statistics.StatisticsChanged += (_, _) => events++;
            fixture.Statistics.Dispose();
            scheduler.RunNext();
            Assert.Equal(0, events);
            Assert.Throws<ObjectDisposedException>(() => fixture.Statistics.Refresh());
        });
    }

    private static Fixture CreateFixture(FlowDocument document, ManualScheduler scheduler)
    {
        var editor = new RichTextBox { Document = document };
        var window = new Window { Content = editor, Width = 220, Height = 140, ShowInTaskbar = false };
        window.Show();
        editor.Focus();
        return new Fixture(window, editor,
            new WriterDocumentStatistics(editor, TimeSpan.FromMilliseconds(10), scheduler));
    }

    private sealed class Fixture(Window window, RichTextBox editor, WriterDocumentStatistics statistics) : IDisposable
    {
        public RichTextBox Editor { get; } = editor;
        public WriterDocumentStatistics Statistics { get; } = statistics;

        public void Dispose()
        {
            Statistics.Dispose();
            window.Close();
        }
    }

    private sealed class ManualScheduler : IWriterDocumentStatisticsScheduler
    {
        private readonly List<Scheduled> _scheduled = new();

        public int ScheduledCount => _scheduled.Count;
        public int PendingCount => _scheduled.Count(item => !item.Cancelled && !item.Ran);
        public int CanceledCount => _scheduled.Count(item => item.Cancelled);

        public IDisposable Schedule(TimeSpan delay, Action callback)
        {
            var item = new Scheduled(callback);
            _scheduled.Add(item);
            return item;
        }

        public void RunNext()
        {
            var item = _scheduled.FirstOrDefault(candidate => !candidate.Ran);
            Assert.NotNull(item);
            item!.Ran = true;
            item.Callback();
        }

        public void RunLatest()
        {
            var item = _scheduled.LastOrDefault(candidate => !candidate.Ran);
            Assert.NotNull(item);
            item!.Ran = true;
            item.Callback();
        }

        private sealed class Scheduled(Action callback) : IDisposable
        {
            public Action Callback { get; } = callback;
            public bool Cancelled { get; private set; }
            public bool Ran { get; set; }
            public void Dispose() => Cancelled = true;
        }
    }
}
