using System.Windows.Documents;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Persistence;

public sealed class WriterDocumentFormatTransitionPolicyTests
{
    [Theory]
    [InlineData(WriterDocumentFormat.PlainText, WriterDocumentFormat.PlainText,
        WriterDocumentFormatTransitionKind.Same, WriterDocumentDataLoss.None, false)]
    [InlineData(WriterDocumentFormat.PlainText, WriterDocumentFormat.RichText,
        WriterDocumentFormatTransitionKind.Upgrade, WriterDocumentDataLoss.None, false)]
    [InlineData(WriterDocumentFormat.PlainText, WriterDocumentFormat.RibbonKitWriter,
        WriterDocumentFormatTransitionKind.Upgrade, WriterDocumentDataLoss.None, false)]
    [InlineData(WriterDocumentFormat.RichText, WriterDocumentFormat.RichText,
        WriterDocumentFormatTransitionKind.Same, WriterDocumentDataLoss.None, false)]
    [InlineData(WriterDocumentFormat.RichText, WriterDocumentFormat.RibbonKitWriter,
        WriterDocumentFormatTransitionKind.Upgrade, WriterDocumentDataLoss.None, false)]
    [InlineData(WriterDocumentFormat.RichText, WriterDocumentFormat.PlainText,
        WriterDocumentFormatTransitionKind.Downgrade, WriterDocumentDataLoss.Formatting, true)]
    [InlineData(WriterDocumentFormat.RibbonKitWriter, WriterDocumentFormat.RichText,
        WriterDocumentFormatTransitionKind.Downgrade,
        WriterDocumentDataLoss.Images | WriterDocumentDataLoss.Hyperlinks | WriterDocumentDataLoss.Tables
            | WriterDocumentDataLoss.PageSettings, true)]
    [InlineData(WriterDocumentFormat.RibbonKitWriter, WriterDocumentFormat.PlainText,
        WriterDocumentFormatTransitionKind.Downgrade,
        WriterDocumentDataLoss.Formatting | WriterDocumentDataLoss.Images | WriterDocumentDataLoss.Hyperlinks
            | WriterDocumentDataLoss.Tables | WriterDocumentDataLoss.PageSettings, true)]
    [InlineData(WriterDocumentFormat.RibbonKitWriter, WriterDocumentFormat.RibbonKitWriter,
        WriterDocumentFormatTransitionKind.Same, WriterDocumentDataLoss.None, false)]
    public void PolicyClassifiesUpgradeDowngradeAndLoss(
        WriterDocumentFormat source,
        WriterDocumentFormat target,
        WriterDocumentFormatTransitionKind kind,
        WriterDocumentDataLoss losses,
        bool requiresConfirmation)
    {
        var transition = WriterDocumentFormatTransitionPolicy.Default.Evaluate(source, target);
        Assert.Equal(kind, transition.Kind);
        Assert.Equal(losses, transition.Losses);
        Assert.Equal(requiresConfirmation, transition.RequiresConfirmation);
        Assert.Equal(!requiresConfirmation, transition.IsLossless);
        Assert.Equal(kind == WriterDocumentFormatTransitionKind.Upgrade, transition.IsUpgrade);
        Assert.Equal(kind == WriterDocumentFormatTransitionKind.Downgrade, transition.IsDowngrade);
        Assert.Equal(kind == WriterDocumentFormatTransitionKind.Same, transition.IsSameFormat);
        if (requiresConfirmation)
            Assert.False(string.IsNullOrWhiteSpace(transition.WarningMessage));
        else
            Assert.Null(transition.WarningMessage);
    }

    [Fact]
    public void PolicyWarningUsesOneCentralizedDescription()
    {
        var transition = WriterDocumentFormatTransitionPolicy.Default.Evaluate(
            WriterDocumentFormat.RibbonKitWriter, WriterDocumentFormat.PlainText);
        Assert.Equal(new[] { "formatting", "images", "hyperlinks", "tables", "page settings" },
            transition.LossDescriptions);
        Assert.Equal(
            "Saving as Plain Text will not preserve formatting, images, hyperlinks, tables, and page settings.",
            transition.WarningMessage);
    }

    [Fact]
    public async Task CancelledDowngradeDoesNotInvokePersistenceOrChangeIdentity()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var persistence = new RecordingPersistence { SaveResult = true };
            var decider = new FormatDecider(WriterFormatTransitionDecision.Cancel);
            var session = new WriterDocumentSession(
                persistence, new CleanDecider(), transitionDecider: decider);
            session.CurrentDocument.MarkDirty();
            var current = session.CurrentDocument;

            Assert.False(await session.SaveAsAsync("cancelled.txt", WriterDocumentFormat.PlainText));
            Assert.Equal(0, persistence.SaveCalls);
            Assert.Same(current, session.CurrentDocument);
            Assert.Equal(WriterDocumentFormat.RichText, current.Format);
            Assert.Null(current.Path);
            Assert.True(current.IsDirty);
            Assert.NotNull(decider.LastTransition);
            Assert.True(decider.LastTransition!.RequiresConfirmation);
        });
    }

    [Fact]
    public async Task FailedDowngradeSaveCannotChangeActiveProfile()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var persistence = new RecordingPersistence { SaveResult = false };
            var session = new WriterDocumentSession(
                persistence, new CleanDecider(),
                transitionDecider: new FormatDecider(WriterFormatTransitionDecision.Continue));
            session.CurrentDocument.MarkDirty();
            var current = session.CurrentDocument;

            Assert.False(await session.SaveAsAsync("failed.txt", WriterDocumentFormat.PlainText));
            Assert.Same(current, session.CurrentDocument);
            Assert.Equal(WriterDocumentFormat.RichText, current.Format);
            Assert.Null(current.Path);
            Assert.True(current.IsDirty);
        });
    }

    [Fact]
    public async Task SuccessfulDowngradeCommitsIdentityOnlyAfterConfirmationAndSave()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var persistence = new RecordingPersistence { SaveResult = true };
            var decider = new FormatDecider(WriterFormatTransitionDecision.Continue);
            var session = new WriterDocumentSession(persistence, new CleanDecider(),
                transitionDecider: decider);
            session.CurrentDocument.MarkDirty();

            Assert.True(await session.SaveAsAsync("saved.txt", WriterDocumentFormat.PlainText));
            Assert.Equal(1, persistence.SaveCalls);
            Assert.Equal(WriterDocumentFormat.PlainText, session.CurrentDocument.Format);
            Assert.Equal("saved.txt", session.CurrentDocument.Path);
            Assert.False(session.CurrentDocument.IsDirty);
            Assert.NotNull(decider.LastTransition);
        });
    }

    [Fact]
    public async Task CancelledTransitionTokenPreservesDocumentWithoutPersistence()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var persistence = new RecordingPersistence { SaveResult = true };
            var session = new WriterDocumentSession(
                persistence, new CleanDecider(),
                transitionDecider: new CancellingFormatDecider());
            session.CurrentDocument.MarkDirty();
            var current = session.CurrentDocument;
            Assert.False(await session.SaveAsAsync("cancelled.txt", WriterDocumentFormat.PlainText));
            Assert.Equal(0, persistence.SaveCalls);
            Assert.Same(current, session.CurrentDocument);
            Assert.Equal(WriterDocumentFormat.RichText, current.Format);
            Assert.True(current.IsDirty);
        });
    }

    [Fact]
    public async Task OrdinaryPersistenceFailurePreservesIdentityAfterConfirmedTransition()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = new WriterDocumentSession(
                new ThrowingPersistence(), new CleanDecider(),
                transitionDecider: new FormatDecider(WriterFormatTransitionDecision.Continue));
            session.CurrentDocument.MarkDirty();
            var current = session.CurrentDocument;

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                session.SaveAsAsync("failed.txt", WriterDocumentFormat.PlainText));
            Assert.Same(current, session.CurrentDocument);
            Assert.Equal(WriterDocumentFormat.RichText, current.Format);
            Assert.Null(current.Path);
            Assert.True(current.IsDirty);
        });
    }

    private sealed class RecordingPersistence : IWriterDocumentPersistence
    {
        public bool SaveResult { get; init; }
        public int SaveCalls { get; private set; }

        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) =>
            Task.FromResult<WriterDocument?>(new WriterDocument(new FlowDocument()));

        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.FromResult(SaveResult);
        }
    }

    private sealed class CleanDecider : IUnsavedChangesDecider
    {
        public Task<UnsavedChangesDecision> DecideAsync(WriterDocument document,
            DocumentTransition transition, CancellationToken cancellationToken) =>
            Task.FromResult(UnsavedChangesDecision.Discard);
    }

    private sealed class FormatDecider(WriterFormatTransitionDecision decision)
        : IWriterFormatTransitionDecider
    {
        public WriterDocumentFormatTransition? LastTransition { get; private set; }

        public Task<WriterFormatTransitionDecision> DecideAsync(WriterDocument document,
            WriterDocumentFormatTransition transition, CancellationToken cancellationToken)
        {
            LastTransition = transition;
            return Task.FromResult(decision);
        }
    }

    private sealed class CancellingFormatDecider : IWriterFormatTransitionDecider
    {
        public Task<WriterFormatTransitionDecision> DecideAsync(WriterDocument document,
            WriterDocumentFormatTransition transition, CancellationToken cancellationToken) =>
            throw new OperationCanceledException(cancellationToken);
    }

    private sealed class ThrowingPersistence : IWriterDocumentPersistence
    {
        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("load failed");

        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("save failed");
    }
}
