using System.Windows.Documents;
using System.ComponentModel;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using Xunit;

namespace RibbonKit.Writer.Tests.Document;

public sealed class WriterDocumentSessionTests
{
    [Fact]
    public void WriterFormatsHaveStableValues()
    {
        Assert.Equal(1, (int)WriterDocumentFormat.RichText);
        Assert.Equal(2, (int)WriterDocumentFormat.PlainText);
        Assert.Equal(3, (int)WriterDocumentFormat.RibbonKitWriter);
    }

    [Fact]
    public void NewDocumentIsUntitledCleanAndHasFlowDocument()
    {
        StaTestHelper.Run(() =>
        {
            var session = NewSession();
            Assert.True(session.CurrentDocument.IsUntitled);
            Assert.Null(session.CurrentDocument.Path);
            Assert.False(session.CurrentDocument.IsDirty);
            Assert.NotNull(session.CurrentDocument.Content);
        });
    }

    [Fact]
    public async Task DirtyAndSuccessfulSaveBecomeClean()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new FakeStore { SaveResult = true };
            var session = NewSession(store);
            session.CurrentDocument.MarkDirty();
            Assert.True(await session.SaveAsAsync("one.rtf", WriterDocumentFormat.RichText));
            Assert.False(session.CurrentDocument.IsDirty);
            Assert.Equal("one.rtf", session.CurrentDocument.Path);
        });
    }

    [Fact]
    public async Task FailedSavePreservesDirtyIdentityAndCurrentDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new FakeStore { SaveResult = false };
            var session = NewSession(store);
            session.CurrentDocument.MarkDirty();
            var document = session.CurrentDocument;
            Assert.False(await session.SaveAsAsync("failed.txt", WriterDocumentFormat.PlainText));
            Assert.Same(document, session.CurrentDocument);
            Assert.True(document.IsDirty);
            Assert.Null(document.Path);
        });
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Cancel, false)]
    [InlineData(UnsavedChangesDecision.Discard, true)]
    [InlineData(UnsavedChangesDecision.Save, true)]
    public async Task NewHonorsUnsavedDecision(UnsavedChangesDecision decision, bool expected)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new FakeStore { SaveResult = true };
            var session = NewSession(store, new FakeDestination
            {
                Destination = new WriterSaveDestination("new.rtf", WriterDocumentFormat.RichText)
            });
            session.CurrentDocument.MarkDirty();
            var old = session.CurrentDocument;
            session.Decider.Decisions.Enqueue(decision);
            Assert.Equal(expected, await session.NewAsync());
            if (!expected) Assert.Same(old, session.CurrentDocument);
            else Assert.NotSame(old, session.CurrentDocument);
        });
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Cancel, false)]
    [InlineData(UnsavedChangesDecision.Discard, true)]
    [InlineData(UnsavedChangesDecision.Save, true)]
    public async Task OpenHonorsEachUnsavedDecision(UnsavedChangesDecision decision, bool expected)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new FakeStore { SaveResult = true };
            var session = NewSession(store, new FakeDestination
            {
                Destination = new WriterSaveDestination("before-open.rtf", WriterDocumentFormat.RichText)
            });
            session.CurrentDocument.MarkDirty();
            var old = session.CurrentDocument;
            session.Decider.Decisions.Enqueue(decision);
            Assert.Equal(expected, await session.OpenAsync("opened.rtf", WriterDocumentFormat.RichText));
            if (expected) Assert.NotSame(old, session.CurrentDocument);
            else Assert.Same(old, session.CurrentDocument);
        });
    }

    [Theory]
    [InlineData(UnsavedChangesDecision.Cancel, false, true)]
    [InlineData(UnsavedChangesDecision.Discard, true, true)]
    [InlineData(UnsavedChangesDecision.Save, true, false)]
    public async Task CloseHonorsEachUnsavedDecision(UnsavedChangesDecision decision, bool expected,
        bool remainsDirty)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = NewSession(new FakeStore { SaveResult = true }, new FakeDestination
            {
                Destination = new WriterSaveDestination("closed-save.rtf", WriterDocumentFormat.RichText)
            });
            session.CurrentDocument.MarkDirty();
            session.Decider.Decisions.Enqueue(decision);
            Assert.Equal(expected, await session.RequestCloseAsync());
            Assert.Equal(remainsDirty, session.CurrentDocument.IsDirty);
            if (decision == UnsavedChangesDecision.Save)
                Assert.Equal("closed-save.rtf", session.CurrentDocument.Path);
        });
    }

    [Fact]
    public async Task SaveDecisionWithFailedSaveBlocksNewAndOpenAndClose()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new FakeStore { SaveResult = false };
            var session = NewSession(store);
            session.CurrentDocument.MarkDirty();
            var old = session.CurrentDocument;
            foreach (var operation in new Func<Task<bool>>[]
                     {
                         () => session.NewAsync(),
                         () => session.OpenAsync("candidate.rtf", WriterDocumentFormat.RichText),
                         () => session.RequestCloseAsync()
                     })
            {
                session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Save);
                Assert.False(await operation());
                Assert.Same(old, session.CurrentDocument);
                Assert.True(old.IsDirty);
            }
        });
    }

    [Fact]
    public async Task CancelledDestinationBlocksUntitledDestructiveTransitions()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = NewSession(new FakeStore { SaveResult = true }, new FakeDestination());
            session.CurrentDocument.MarkDirty();
            var old = session.CurrentDocument;
            foreach (var operation in new Func<Task<bool>>[]
                     {
                         () => session.NewAsync(),
                         () => session.OpenAsync("candidate.rtf", WriterDocumentFormat.RichText),
                         () => session.RequestCloseAsync()
                     })
            {
                session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Save);
                Assert.False(await operation());
                Assert.Same(old, session.CurrentDocument);
                Assert.True(old.IsDirty);
            }
        });
    }

    [Fact]
    public async Task OpenFailureDoesNotReplaceCurrentDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = NewSession(new FakeStore { LoadedDocument = null });
            var old = session.CurrentDocument;
            Assert.False(await session.OpenAsync("missing.txt", WriterDocumentFormat.PlainText));
            Assert.Same(old, session.CurrentDocument);
        });
    }

    [Fact]
    public async Task OpenReplacesOnlyAfterCandidateLoads()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var candidate = new WriterDocument(new FlowDocument());
            var session = NewSession(new FakeStore { LoadedDocument = candidate });
            var old = session.CurrentDocument;
            Assert.True(await session.OpenAsync("loaded.txt", WriterDocumentFormat.PlainText));
            Assert.NotSame(old, session.CurrentDocument);
            Assert.Same(candidate, session.CurrentDocument);
            Assert.Equal("loaded.txt", candidate.Path);
            Assert.False(candidate.IsDirty);
        });
    }

    [Fact]
    public async Task SaveAsCommitsPathAndFormatOnlyAfterSuccess()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new FakeStore { SaveResult = false };
            var session = NewSession(store);
            session.CurrentDocument.MarkDirty();
            Assert.False(await session.SaveAsAsync("draft.txt", WriterDocumentFormat.PlainText));
            Assert.Null(session.CurrentDocument.Path);
            Assert.Equal(WriterDocumentFormat.RichText, session.CurrentDocument.Format);
            store.SaveResult = true;
            Assert.True(await session.SaveAsAsync("draft.txt", WriterDocumentFormat.PlainText));
            Assert.Equal("draft.txt", session.CurrentDocument.Path);
            Assert.Equal(WriterDocumentFormat.PlainText, session.CurrentDocument.Format);
        });
    }

    [Fact]
    public async Task PropertyChangesAnnounceDirtyIdentityAndSuccessfulReplacementOnly()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = NewSession(new FakeStore { SaveResult = true });
            var documentProperties = new List<string?>();
            session.CurrentDocument.PropertyChanged += (_, args) => documentProperties.Add(args.PropertyName);
            session.CurrentDocument.MarkDirty();
            session.CurrentDocument.MarkDirty();
            session.CurrentDocument.MarkClean();
            Assert.Equal(new[] { "IsDirty", "IsDirty" }, documentProperties);

            var replacements = 0;
            session.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(WriterDocumentSession.CurrentDocument)) replacements++;
            };
            session.CurrentDocument.MarkDirty();
            session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Cancel);
            Assert.False(await session.NewAsync());
            Assert.Equal(0, replacements);
            session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Discard);
            Assert.True(await session.NewAsync());
            Assert.Equal(1, replacements);
            var replacementProperties = new List<string?>();
            session.CurrentDocument.PropertyChanged += (_, args) => replacementProperties.Add(args.PropertyName);
            Assert.True(await session.SaveAsAsync("identity.txt", WriterDocumentFormat.PlainText));
            Assert.Equal(new[] { "Path", "IsUntitled", "Format" },
                replacementProperties.ToArray());
            Assert.True(await session.OpenAsync("opened.txt", WriterDocumentFormat.PlainText));
            Assert.Equal(2, replacements);
        });
    }

    [Fact]
    public async Task InvalidDestinationAndFormatAreRejectedBeforePersistence()
    {
        Assert.Throws<ArgumentException>(() => new WriterSaveDestination(" ", WriterDocumentFormat.RichText));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WriterSaveDestination("x.rtf", (WriterDocumentFormat)99));
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new FakeStore();
            var session = NewSession(store);
            await Assert.ThrowsAsync<ArgumentException>(() => session.SaveAsAsync(" ", WriterDocumentFormat.RichText));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.SaveAsAsync("x.rtf", (WriterDocumentFormat)99));
            await Assert.ThrowsAsync<ArgumentException>(() => session.OpenAsync(" ", WriterDocumentFormat.RichText));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => session.OpenAsync("x.rtf", (WriterDocumentFormat)99));
            Assert.Equal(0, store.SaveCalls);
            Assert.Equal(0, store.LoadCalls);
        });
    }

    [Fact]
    public async Task CancelClosePreservesCurrentDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = NewSession();
            session.CurrentDocument.MarkDirty();
            var document = session.CurrentDocument;
            session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Cancel);
            Assert.False(await session.RequestCloseAsync());
            Assert.Same(document, session.CurrentDocument);
            Assert.True(document.IsDirty);
        });
    }

    [Fact]
    public async Task AsyncLoadResumesOnStaAndCommitsCandidateSafely()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var candidate = new WriterDocument(new FlowDocument());
            var session = NewSession(new AsyncStore(candidate));
            Assert.True(await session.OpenAsync("async.rtf", WriterDocumentFormat.RichText));
            Assert.Same(candidate, session.CurrentDocument);
            Assert.False(candidate.IsDirty);
        });
    }

    [Fact]
    public async Task OrdinaryFailuresPropagateWithoutChangingSessionState()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var store = new ThrowingStore();
            var session = NewSession(store, new FakeDestination
            {
                Destination = new WriterSaveDestination("save.rtf", WriterDocumentFormat.RichText)
            });
            session.CurrentDocument.MarkDirty();
            var old = session.CurrentDocument;
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsAsync("save.rtf", WriterDocumentFormat.RichText));
            Assert.Same(old, session.CurrentDocument);
            Assert.True(old.IsDirty);
            var cancellingDecider = new CancellingDecider();
            var decisionSession = new WriterDocumentSession(new FakeStore(), cancellingDecider);
            decisionSession.CurrentDocument.MarkDirty();
            var decisionDocument = decisionSession.CurrentDocument;
            Assert.False(await decisionSession.NewAsync());
            Assert.Same(decisionDocument, decisionSession.CurrentDocument);
            Assert.True(decisionDocument.IsDirty);
            session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Discard);
            await Assert.ThrowsAsync<InvalidOperationException>(() => session.OpenAsync("bad.rtf", WriterDocumentFormat.RichText));
            Assert.Same(old, session.CurrentDocument);
            Assert.True(old.IsDirty);
        });
    }

    [Fact]
    public async Task OperationCancellationPreservesDocumentAndDirtyState()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = NewSession(new CancellingStore(), new CancellingDestination());
            session.CurrentDocument.MarkDirty();
            var old = session.CurrentDocument;
            session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Discard);
            Assert.False(await session.SaveAsAsync("cancel.rtf", WriterDocumentFormat.RichText));
            Assert.Same(old, session.CurrentDocument);
            Assert.True(old.IsDirty);
            session.Decider.Decisions.Enqueue(UnsavedChangesDecision.Discard);
            Assert.False(await session.OpenAsync("cancel.rtf", WriterDocumentFormat.RichText));
            Assert.Same(old, session.CurrentDocument);
            Assert.True(old.IsDirty);
        });
    }

    private static SessionWithDecider NewSession(IWriterDocumentPersistence? store = null,
        IWriterSaveDestinationProvider? destination = null)
    {
        var decider = new FakeDecider();
        return new SessionWithDecider(new WriterDocumentSession(store ?? new FakeStore(), decider,
            destination ?? new FakeDestination()), decider);
    }

    private sealed record SessionWithDecider(WriterDocumentSession Session, FakeDecider Decider)
    {
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => Session.PropertyChanged += value;
            remove => Session.PropertyChanged -= value;
        }
        public WriterDocument CurrentDocument => Session.CurrentDocument;
        public Task<bool> NewAsync() => Session.NewAsync();
        public Task<bool> OpenAsync(string path, WriterDocumentFormat format) => Session.OpenAsync(path, format);
        public Task<bool> SaveAsAsync(string path, WriterDocumentFormat format) => Session.SaveAsAsync(path, format);
        public Task<bool> RequestCloseAsync() => Session.RequestCloseAsync();
    }

    private sealed class FakeStore : IWriterDocumentPersistence
    {
        public WriterDocument? LoadedDocument { get; set; } = new(new FlowDocument());
        public bool SaveResult { get; set; }
        public int SaveCalls { get; private set; }
        public int LoadCalls { get; private set; }
        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format, CancellationToken cancellationToken) =>
            LoadCore();
        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format, CancellationToken cancellationToken) =>
            SaveCore();
        private Task<WriterDocument?> LoadCore()
        {
            LoadCalls++;
            return Task.FromResult(LoadedDocument);
        }
        private Task<bool> SaveCore()
        {
            SaveCalls++;
            return Task.FromResult(SaveResult);
        }
    }

    private sealed class AsyncStore : IWriterDocumentPersistence
    {
        private readonly WriterDocument _candidate;
        public AsyncStore(WriterDocument candidate) => _candidate = candidate;
        public async Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            return _candidate;
        }
        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class ThrowingStore : IWriterDocumentPersistence
    {
        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("load failed");
        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) => throw new InvalidOperationException("save failed");
    }

    private sealed class CancellingStore : IWriterDocumentPersistence
    {
        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format, CancellationToken cancellationToken) =>
            throw new OperationCanceledException(cancellationToken);
        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) => throw new OperationCanceledException(cancellationToken);
    }

    private sealed class FakeDestination : IWriterSaveDestinationProvider
    {
        public WriterSaveDestination? Destination { get; set; }
        public Task<WriterSaveDestination?> GetDestinationAsync(WriterDocument document,
            CancellationToken cancellationToken) => Task.FromResult(Destination);
    }

    private sealed class CancellingDestination : IWriterSaveDestinationProvider
    {
        public Task<WriterSaveDestination?> GetDestinationAsync(WriterDocument document,
            CancellationToken cancellationToken) => throw new OperationCanceledException(cancellationToken);
    }

    private sealed class CancellingDecider : IUnsavedChangesDecider
    {
        public Task<UnsavedChangesDecision> DecideAsync(WriterDocument document, DocumentTransition transition,
            CancellationToken cancellationToken) => throw new OperationCanceledException(cancellationToken);
    }

    private sealed class FakeDecider : IUnsavedChangesDecider
    {
        public Queue<UnsavedChangesDecision> Decisions { get; } = new();
        public Task<UnsavedChangesDecision> DecideAsync(WriterDocument document, DocumentTransition transition,
            CancellationToken cancellationToken) => Task.FromResult(Decisions.Dequeue());
    }
}
