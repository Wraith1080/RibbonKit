using System.Windows.Documents;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.Persistence;
using Xunit;

namespace RibbonKit.Writer.Tests.Document;

public sealed class WriterDocumentProfileTests
{
    [Fact]
    public void CatalogHasThreeExplicitProfilesAndStableExtensions()
    {
        Assert.Equal(
            new[] { WriterDocumentFormat.PlainText, WriterDocumentFormat.RichText,
                WriterDocumentFormat.RibbonKitWriter },
            WriterDocumentProfiles.All.Select(profile => profile.Format));
        Assert.Equal(".txt", WriterDocumentProfiles.PlainText.DefaultExtension);
        Assert.Equal(".rtf", WriterDocumentProfiles.RichText.DefaultExtension);
        Assert.Equal(".rkw", WriterDocumentProfiles.RibbonKitWriter.DefaultExtension);
        Assert.Same(WriterDocumentProfiles.RichText, WriterDocumentProfiles.Default);
        Assert.Equal(".txt", WriterDocumentFormat.PlainText.GetDefaultExtension());
        Assert.Same(WriterDocumentProfiles.RibbonKitWriter,
            WriterDocumentFormat.RibbonKitWriter.GetProfile());
        Assert.Same(WriterDocumentProfiles.PlainText, WriterDocumentProfile.PlainText);
        Assert.True(WriterDocumentProfiles.IsCanonical(WriterDocumentProfile.RichText));
    }

    [Theory]
    [InlineData(WriterDocumentFormat.PlainText, false, false, false, false,
        WriterDocumentCommandCapabilities.TextEditing,
        WriterDocumentContentCapabilities.Text,
        WriterDocumentPageMetadataCapabilities.None)]
    [InlineData(WriterDocumentFormat.RichText, true, false, false, false,
        WriterDocumentCommandCapabilities.CharacterFormatting | WriterDocumentCommandCapabilities.ParagraphFormatting,
        WriterDocumentContentCapabilities.CharacterFormatting | WriterDocumentContentCapabilities.ParagraphFormatting,
        WriterDocumentPageMetadataCapabilities.None)]
    [InlineData(WriterDocumentFormat.RibbonKitWriter, true, true, true, true,
        WriterDocumentCommandCapabilities.CharacterFormatting | WriterDocumentCommandCapabilities.ParagraphFormatting
            | WriterDocumentCommandCapabilities.PageSettings | WriterDocumentCommandCapabilities.TableEditing,
        WriterDocumentContentCapabilities.CharacterFormatting | WriterDocumentContentCapabilities.ParagraphFormatting
            | WriterDocumentContentCapabilities.Images | WriterDocumentContentCapabilities.Hyperlinks
            | WriterDocumentContentCapabilities.Tables,
        WriterDocumentPageMetadataCapabilities.PageSettings)]
    public void CapabilityMatrixMatchesPersistenceFacts(
        WriterDocumentFormat format,
        bool preservesFormatting,
        bool preservesImages,
        bool preservesTables,
        bool preservesPageSettings,
        WriterDocumentCommandCapabilities requiredCommands,
        WriterDocumentContentCapabilities requiredContent,
        WriterDocumentPageMetadataCapabilities requiredPageMetadata)
    {
        var profile = WriterDocumentProfiles.ForFormat(format);
        Assert.Equal(preservesFormatting, profile.Capabilities.Persistence.PreservesFormatting);
        Assert.Equal(preservesImages, profile.Capabilities.Persistence.PreservesImages);
        Assert.Equal(preservesTables, profile.Capabilities.Persistence.PreservesTables);
        Assert.Equal(preservesImages && format == WriterDocumentFormat.RibbonKitWriter,
            profile.Capabilities.Persistence.PreservesHyperlinks);
        Assert.Equal(preservesPageSettings, profile.Capabilities.Persistence.PreservesPageSettings);
        Assert.True(profile.Supports(requiredCommands));
        Assert.True(profile.Supports(
            WriterDocumentCommandCapabilities.Preview | WriterDocumentCommandCapabilities.Printing));
        Assert.True(profile.Preserves(requiredContent));
        Assert.True(profile.Preserves(requiredPageMetadata));
        Assert.Equal(profile.Capabilities.Persistence,
            WriterDocumentPersistence.GetCapabilities(format));
    }

    [Fact]
    public void NativeProfileAdvertisesAcceptedTableEditingAndRoundTrip()
    {
        Assert.False(WriterDocumentProfiles.PlainText.Supports(
            WriterDocumentCommandCapabilities.TableEditing));
        Assert.False(WriterDocumentProfiles.RichText.Supports(
            WriterDocumentCommandCapabilities.TableEditing));
        Assert.True(WriterDocumentProfiles.RibbonKitWriter.Supports(
            WriterDocumentCommandCapabilities.TableEditing));
        Assert.True(WriterDocumentProfiles.RibbonKitWriter.Capabilities.PreservesTables);
        Assert.True(WriterDocumentProfiles.RibbonKitWriter.PersistenceCapabilities.PreservesTables);
    }

    [Theory]
    [InlineData(WriterDocumentFormat.PlainText)]
    [InlineData(WriterDocumentFormat.RichText)]
    [InlineData(WriterDocumentFormat.RibbonKitWriter)]
    public async Task TypedNewCreatesCleanUntitledDocumentWithExplicitFormat(WriterDocumentFormat format)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = new WriterDocumentSession(new RecordingPersistence(), new CleanDecider());
            Assert.True(await session.NewAsync(format));
            Assert.Equal(format, session.CurrentDocument.Format);
            Assert.Null(session.CurrentDocument.Path);
            Assert.False(session.CurrentDocument.IsDirty);
            Assert.Same(System.Windows.Threading.Dispatcher.CurrentDispatcher,
                session.CurrentDocument.Content.Dispatcher);
        });
    }

    [Fact]
    public async Task ConfiguredDefaultProfileControlsNoArgumentNewAndInitialDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var session = new WriterDocumentSession(
                new RecordingPersistence(), new CleanDecider(),
                defaultProfile: WriterDocumentProfiles.PlainText);
            Assert.Equal(WriterDocumentFormat.PlainText, session.CurrentDocument.Format);
            Assert.True(await session.NewAsync());
            Assert.Equal(WriterDocumentFormat.PlainText, session.CurrentDocument.Format);
        });
    }

    [Fact]
    public async Task ProfileNewHonorsUnsavedDecisionBeforeReplacingDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var decider = new SequencedUnsavedDecider(UnsavedChangesDecision.Cancel);
            var session = new WriterDocumentSession(new RecordingPersistence(), decider);
            session.CurrentDocument.MarkDirty();
            var current = session.CurrentDocument;
            Assert.False(await session.NewAsync(WriterDocumentProfiles.RibbonKitWriter));
            Assert.Same(current, session.CurrentDocument);
            Assert.Equal(WriterDocumentFormat.RichText, current.Format);
            Assert.True(current.IsDirty);
        });
    }

    [Fact]
    public async Task SessionRejectsNonCanonicalProfilesAtItsBoundaries()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            var custom = new WriterDocumentProfile(
                WriterDocumentFormat.RichText,
                "Custom Rich Text",
                "Not a canonical profile.",
                ".rtf",
                WriterDocumentProfileCapabilities.Create(
                    WriterDocumentPersistence.GetCapabilities(WriterDocumentFormat.RichText),
                    WriterDocumentCommandCapabilities.TextEditing
                    | WriterDocumentCommandCapabilities.Clipboard
                    | WriterDocumentCommandCapabilities.UndoRedo
                    | WriterDocumentCommandCapabilities.FindReplace
                    | WriterDocumentCommandCapabilities.SpellCheck
                    | WriterDocumentCommandCapabilities.CharacterFormatting
                    | WriterDocumentCommandCapabilities.ParagraphFormatting
                    | WriterDocumentCommandCapabilities.Preview
                    | WriterDocumentCommandCapabilities.Printing));
            Assert.False(WriterDocumentProfiles.IsCanonical(custom));
            Assert.Throws<ArgumentException>(() => new WriterDocumentSession(
                new RecordingPersistence(), new CleanDecider(), defaultProfile: custom));

            var session = new WriterDocumentSession(new RecordingPersistence(), new CleanDecider());
            await Assert.ThrowsAsync<ArgumentException>(() => session.NewAsync(custom));
            await Assert.ThrowsAsync<ArgumentException>(() => session.SaveAsAsync("custom.rtf", custom));
        });
    }

    private sealed class RecordingPersistence : IWriterDocumentPersistence
    {
        public int SaveCalls { get; private set; }

        public Task<WriterDocument?> LoadAsync(string path, WriterDocumentFormat format,
            CancellationToken cancellationToken) =>
            Task.FromResult<WriterDocument?>(new WriterDocument(new FlowDocument()));

        public Task<bool> SaveAsync(WriterDocument document, string path, WriterDocumentFormat format,
            CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.FromResult(true);
        }
    }

    private sealed class CleanDecider : IUnsavedChangesDecider
    {
        public Task<UnsavedChangesDecision> DecideAsync(WriterDocument document,
            DocumentTransition transition, CancellationToken cancellationToken) =>
            Task.FromResult(UnsavedChangesDecision.Discard);
    }

    private sealed class SequencedUnsavedDecider(UnsavedChangesDecision decision) : IUnsavedChangesDecider
    {
        public Task<UnsavedChangesDecision> DecideAsync(WriterDocument document,
            DocumentTransition transition, CancellationToken cancellationToken) =>
            Task.FromResult(decision);
    }
}
