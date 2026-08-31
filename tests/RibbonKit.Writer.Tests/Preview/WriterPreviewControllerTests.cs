using System.Windows.Controls;
using System.Windows.Documents;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Preview;

[Collection(WriterPreviewTestCollection.Name)]
public sealed class WriterPreviewControllerTests
{
    [Fact]
    public void RejectsMismatchedEditorAndWriterDocumentContent()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox { Document = new FlowDocument() };
            var document = new WriterDocument(new FlowDocument());
            Assert.Throws<ArgumentException>(() => new WriterPreviewController(editor, document));
        });
    }

    [Fact]
    public void ContentAndPageSettingsChangesUseOneTrailingEdgeRebuild()
    {
        StaTestHelper.Run(() =>
        {
            var document = new WriterDocument(new FlowDocument(new Paragraph(new Run("one"))));
            var editor = new RichTextBox { Document = document.Content };
            var scheduler = new ManualScheduler();
            using var controller = new WriterPreviewController(editor, document,
                TimeSpan.FromMilliseconds(50), scheduler);
            var events = 0;
            controller.SnapshotChanged += (_, _) => events++;

            scheduler.RunNext();
            Assert.Equal("one", SnapshotText(controller).TrimEnd());
            Assert.Equal(1, events);
            Assert.True(controller.TryGetCurrentSnapshot(out var current));
            Assert.Same(controller.Snapshot, current);

            new TextRange(editor.Document.ContentEnd, editor.Document.ContentEnd)
                .Text = " two";
            document.SetPageSettings(DocumentPageSettings.A4());
            Assert.True(controller.IsPending);
            Assert.False(controller.TryGetCurrentSnapshot(out _));
            Assert.Equal(1, scheduler.PendingCount);
            Assert.Equal(1, scheduler.CanceledCount);
            scheduler.RunNext();
            Assert.Equal(1, events);
            scheduler.RunNext();
            Assert.Equal(2, events);
            Assert.True(controller.TryGetCurrentSnapshot(out _));
            Assert.Contains("two", SnapshotText(controller));
            Assert.Equal(DocumentPageSettings.A4().WidthDip, controller.Snapshot!.SourceClone.PageWidth, 4);
        });
    }

    [Fact]
    public void StaleCallbacksAreIgnoredAfterRefreshAndDispose()
    {
        StaTestHelper.Run(() =>
        {
            var document = new WriterDocument(new FlowDocument(new Paragraph(new Run("first"))));
            var editor = new RichTextBox { Document = document.Content };
            var scheduler = new ManualScheduler();
            var controller = new WriterPreviewController(editor, document,
                TimeSpan.FromMilliseconds(1), scheduler);
            scheduler.RunNext();
            var events = 0;
            controller.SnapshotChanged += (_, _) => events++;

            controller.Refresh();
            controller.Refresh();
            Assert.Equal(1, scheduler.PendingCount);
            Assert.Equal(1, scheduler.CanceledCount);
            scheduler.RunAllIgnoringDisposal();
            Assert.Equal(1, events);

            controller.Refresh();
            controller.Dispose();
            scheduler.RunAllIgnoringDisposal();
            Assert.Equal(1, events);
            Assert.Throws<ObjectDisposedException>(() => controller.Refresh());
        });
    }

    [Fact]
    public void SetDocumentPublishesOnlyTheReplacementContentAndSettings()
    {
        StaTestHelper.Run(() =>
        {
            var first = new WriterDocument(new FlowDocument(new Paragraph(new Run("first"))));
            var editor = new RichTextBox { Document = first.Content };
            var scheduler = new ManualScheduler();
            using var controller = new WriterPreviewController(editor, first,
                TimeSpan.FromMilliseconds(10), scheduler);
            scheduler.RunNext();

            var second = new WriterDocument(new FlowDocument(new Paragraph(new Run("second"))),
                pageSettings: DocumentPageSettings.A4());
            editor.Document = second.Content;
            controller.SetDocument(second);
            scheduler.RunAllIgnoringDisposal();

            Assert.Same(second, controller.Document);
            Assert.Contains("second", SnapshotText(controller));
            Assert.Equal(second.PageSettings, controller.Snapshot!.PageSettings);
        });
    }

    [Fact]
    public void CloneFailureKeepsOlderSnapshotStaleUntilARefreshSucceeds()
    {
        StaTestHelper.Run(() =>
        {
            var document = new WriterDocument(new FlowDocument(new Paragraph(new Run("content"))));
            var editor = new RichTextBox { Document = document.Content };
            var scheduler = new ManualScheduler();
            var cloneService = new WriterPreviewCloneService();
            var attempts = 0;
            using var controller = new WriterPreviewController(editor, document,
                TimeSpan.Zero, scheduler, (content, settings) =>
                {
                    attempts++;
                    if (attempts == 2)
                        throw new InvalidOperationException("Synthetic clone failure.");
                    return cloneService.CreateSnapshot(content, settings);
                });

            scheduler.RunNext();
            var firstSnapshot = controller.Snapshot;
            Assert.True(controller.TryGetCurrentSnapshot(out _));

            controller.Refresh();
            scheduler.RunNext();
            Assert.True(controller.IsPending);
            Assert.Same(firstSnapshot, controller.Snapshot);
            Assert.False(controller.TryGetCurrentSnapshot(out _));

            controller.Refresh();
            scheduler.RunNext();
            Assert.False(controller.IsPending);
            Assert.True(controller.TryGetCurrentSnapshot(out var current));
            Assert.NotSame(firstSnapshot, current);
        });
    }

    [Fact]
    public void SuspendedControllerCoalescesTypingWithoutSchedulingPagination()
    {
        StaTestHelper.Run(() =>
        {
            var document = new WriterDocument(new FlowDocument(new Paragraph(new Run("start"))));
            var editor = new RichTextBox { Document = document.Content };
            var scheduler = new ManualScheduler();
            using var controller = new WriterPreviewController(editor, document,
                TimeSpan.FromMilliseconds(250), scheduler);
            scheduler.RunNext();
            var openingSnapshot = controller.Snapshot;

            controller.SetRebuildEnabled(false);
            editor.AppendText(" one");
            editor.AppendText(" two");
            editor.AppendText(" three");

            Assert.False(controller.IsRebuildEnabled);
            Assert.True(controller.IsPending);
            Assert.Equal(0, scheduler.PendingCount);
            Assert.Same(openingSnapshot, controller.Snapshot);
            Assert.False(controller.TryGetCurrentSnapshot(out _));

            controller.SetRebuildEnabled(true);
            Assert.True(controller.IsRebuildEnabled);
            Assert.Equal(1, scheduler.PendingCount);
            scheduler.RunNext();
            Assert.True(controller.TryGetCurrentSnapshot(out var current));
            Assert.NotSame(openingSnapshot, current);
            Assert.Contains("three", SnapshotText(controller));
        });
    }

    private static string SnapshotText(WriterPreviewController controller) =>
        new TextRange(controller.Snapshot!.SourceClone.ContentStart,
                controller.Snapshot.SourceClone.ContentEnd).Text;

    private sealed class ManualScheduler : IWriterPreviewScheduler
    {
        private readonly List<Scheduled> _scheduled = new();

        public int PendingCount => _scheduled.Count(item => !item.Canceled);
        public int CanceledCount => _scheduled.Count(item => item.Canceled);

        public IDisposable Schedule(TimeSpan delay, Action callback)
        {
            var item = new Scheduled(callback);
            _scheduled.Add(item);
            return item;
        }

        public void RunNext()
        {
            if (_scheduled.Count == 0)
                throw new InvalidOperationException("No scheduled callback.");
            var item = _scheduled[0];
            _scheduled.RemoveAt(0);
            item.Callback();
        }

        public void RunAllIgnoringDisposal()
        {
            while (_scheduled.Count > 0)
                RunNext();
        }

        private sealed class Scheduled(Action callback) : IDisposable
        {
            public Action Callback { get; } = callback;
            public bool Canceled { get; private set; }
            public void Dispose() => Canceled = true;
        }
    }
}
