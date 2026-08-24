using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.Persistence;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Persistence;

public sealed class WriterRkwPersistenceTests
{
    [Fact]
    public async Task NativePackageRoundTripsAllowedFormattingListsAndPageSettings()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "formatted.rkw");
            var paragraph = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(3, 4, 5, 6)
            };
            paragraph.Inlines.Add(new Bold(new Run("Bold")) { Foreground = Brushes.DarkBlue });
            paragraph.Inlines.Add(new Run(" "));
            paragraph.Inlines.Add(new Italic(new Run("Italic")));
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Underline(new Run("Underline")));
            var list = new List { MarkerStyle = TextMarkerStyle.Decimal, StartIndex = 3 };
            list.ListItems.Add(new ListItem(new Paragraph(new Run("Item"))));
            var content = new FlowDocument();
            content.Blocks.Add(paragraph);
            content.Blocks.Add(list);
            var settings = DocumentPageSettings.CreateCustom(700, 1000,
                DocumentPageOrientation.Landscape, new DocumentPageMargins(30, 40, 50, 60));
            var source = new WriterDocument(content, pageSettings: settings);
            var persistence = new WriterDocumentPersistence();

            Assert.True(await persistence.SaveAsync(source, path,
                WriterDocumentFormat.RibbonKitWriter, default));
            var loaded = await persistence.LoadAsync(path,
                WriterDocumentFormat.RibbonKitWriter, default);

            Assert.NotNull(loaded);
            Assert.Equal(path, loaded!.Path);
            Assert.Equal(WriterDocumentFormat.RibbonKitWriter, loaded.Format);
            Assert.Equal(settings, loaded.PageSettings);
            Assert.Equal("Bold Italic\r\nUnderline\r\n3.\tItem", Text(loaded.Content).Trim());
            var loadedParagraph = Assert.IsType<Paragraph>(loaded.Content.Blocks.FirstBlock);
            Assert.Equal(TextAlignment.Center, loadedParagraph.TextAlignment);
            Assert.Equal(new Thickness(3, 4, 5, 6), loadedParagraph.Margin);
            var formattedSpan = Assert.IsType<Span>(loadedParagraph.Inlines.FirstInline);
            Assert.Equal(FontWeights.Bold, formattedSpan.FontWeight);
            Assert.Equal(Colors.DarkBlue, ((SolidColorBrush)formattedSpan.Foreground).Color);
            var loadedList = Assert.IsType<List>(loadedParagraph.NextBlock);
            Assert.Equal(TextMarkerStyle.Decimal, loadedList.MarkerStyle);
            Assert.Equal(3, loadedList.StartIndex);
        });
    }

    [Fact]
    public async Task SavedPackageHasOnlyVersionedRequiredParts()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "shape.rkw");
            await new WriterDocumentPersistence().SaveAsync(
                new WriterDocument(new FlowDocument(new Paragraph(new Run("shape")))), path,
                WriterDocumentFormat.RibbonKitWriter, default);

            using var stream = File.OpenRead(path);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            Assert.Equal(new[] { "manifest.json", "document-settings.json", "content.xamlpackage" },
                archive.Entries.Select(entry => entry.FullName));
            using var manifest = JsonDocument.Parse(Read(archive.GetEntry("manifest.json")!));
            Assert.Equal("RibbonKit.Writer", manifest.RootElement.GetProperty("format").GetString());
            Assert.Equal(1, manifest.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Empty(manifest.RootElement.GetProperty("requiredFeatures").EnumerateArray());
            using var settings = JsonDocument.Parse(Read(archive.GetEntry("document-settings.json")!));
            Assert.Equal("Letter", settings.RootElement.GetProperty("paperSize").GetString());
            Assert.Equal(816, settings.RootElement.GetProperty("portraitWidthDip").GetDouble());
        });
    }

    [Theory]
    [InlineData("extra.bin")]
    [InlineData("../manifest.json")]
    [InlineData("Manifest.json")]
    public async Task UnexpectedTraversalAndCaseCollidingOuterPartsAreRejected(string invalidName)
    {
        await AssertInvalidAsync(new[]
        {
            RkwPackageFixture.ManifestEntry(),
            RkwPackageFixture.SettingsEntry(),
            RkwPackageFixture.ContentEntry(),
            (invalidName, Array.Empty<byte>())
        });
    }

    [Fact]
    public async Task DuplicateAndMissingOuterPartsAreRejected()
    {
        await AssertInvalidAsync(new[]
        {
            RkwPackageFixture.ManifestEntry(),
            RkwPackageFixture.ManifestEntry(),
            RkwPackageFixture.SettingsEntry()
        });
        await AssertInvalidAsync(new[]
        {
            RkwPackageFixture.ManifestEntry(),
            RkwPackageFixture.SettingsEntry()
        });
    }

    [Theory]
    [InlineData("{\"format\":\"Other\",\"schemaVersion\":1,\"minimumReaderVersion\":1,\"contentSchemaVersion\":1,\"settingsSchemaVersion\":1,\"requiredFeatures\":[]}")]
    [InlineData("{\"format\":\"RibbonKit.Writer\",\"schemaVersion\":2,\"minimumReaderVersion\":1,\"contentSchemaVersion\":1,\"settingsSchemaVersion\":1,\"requiredFeatures\":[]}")]
    [InlineData("{\"format\":\"RibbonKit.Writer\",\"format\":\"RibbonKit.Writer\",\"schemaVersion\":1,\"minimumReaderVersion\":1,\"contentSchemaVersion\":1,\"settingsSchemaVersion\":1,\"requiredFeatures\":[]}")]
    public async Task WrongVersionIdentityAndDuplicateManifestPropertiesAreRejected(string manifest)
    {
        await AssertInvalidAsync(new[]
        {
            ("manifest.json", RkwPackageFixture.Utf8(manifest)),
            RkwPackageFixture.SettingsEntry(),
            RkwPackageFixture.ContentEntry()
        });
    }

    [Theory]
    [InlineData("<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><ObjectDataProvider /></Section>")]
    [InlineData("<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph FontFamily=\"{Binding Path=Secret}\" /></Section>")]
    [InlineData("<!DOCTYPE Section [<!ENTITY xxe SYSTEM \"file:///c:/windows/win.ini\">]><Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>&xxe;</Paragraph></Section>")]
    public async Task UnsafeXamlIsRejectedAsDataWithoutObjectInstantiation(string xaml)
    {
        await AssertInvalidAsync(new[]
        {
            RkwPackageFixture.ManifestEntry(),
            RkwPackageFixture.SettingsEntry(),
            RkwPackageFixture.ContentEntry(xaml)
        });
    }

    [Fact]
    public async Task InvalidInnerPackageShapeAndRelationshipsAreRejected()
    {
        var duplicate = RkwPackageFixture.CreateOuterPackage(new[]
        {
            RkwPackageFixture.XamlEntry(),
            RkwPackageFixture.XamlEntry(),
            RkwPackageFixture.ContentTypesEntry()
        });
        await AssertInvalidContentAsync(duplicate);

        var extra = RkwPackageFixture.CreateOuterPackage(new[]
        {
            RkwPackageFixture.XamlEntry(),
            RkwPackageFixture.RelationshipsEntry(),
            RkwPackageFixture.ContentTypesEntry(),
            ("../payload.xaml", Array.Empty<byte>())
        });
        await AssertInvalidContentAsync(extra);

        var externalRelationship = RkwPackageFixture.CreateOuterPackage(new[]
        {
            RkwPackageFixture.XamlEntry(),
            RkwPackageFixture.RelationshipsEntry(
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rDocument\" Type=\"http://schemas.microsoft.com/wpf/2005/10/xaml/entry\" Target=\"https://example.invalid/payload\" TargetMode=\"External\" /></Relationships>"),
            RkwPackageFixture.ContentTypesEntry()
        });
        await AssertInvalidContentAsync(externalRelationship);

        await AssertInvalidContentAsync([0x50, 0x4B, 0x03]);
    }

    [Theory]
    [InlineData("{\"schemaVersion\":1,\"paperSize\":\"Letter\",\"portraitWidthDip\":999,\"portraitHeightDip\":1056,\"orientation\":\"Portrait\",\"marginsDip\":{\"left\":96,\"top\":96,\"right\":96,\"bottom\":96}}")]
    [InlineData("{\"schemaVersion\":1,\"paperSize\":\"Custom\",\"portraitWidthDip\":600,\"portraitHeightDip\":900,\"orientation\":\"Portrait\",\"marginsDip\":{\"left\":300,\"top\":0,\"right\":300,\"bottom\":0}}")]
    [InlineData("{\"schemaVersion\":1,\"paperSize\":\"Letter\",\"paperSize\":\"Letter\",\"portraitWidthDip\":816,\"portraitHeightDip\":1056,\"orientation\":\"Portrait\",\"marginsDip\":{\"left\":96,\"top\":96,\"right\":96,\"bottom\":96}}")]
    public async Task InvalidOrDuplicateDocumentSettingsAreRejected(string settings)
    {
        await AssertInvalidAsync(new[]
        {
            RkwPackageFixture.ManifestEntry(),
            ("document-settings.json", RkwPackageFixture.Utf8(settings)),
            RkwPackageFixture.ContentEntry()
        });
    }

    [Fact]
    public async Task CancelledNativeSaveDoesNotReplaceDestination()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "cancelled.rkw");
            await File.WriteAllTextAsync(path, "old");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new WriterDocumentPersistence().SaveAsync(
                    new WriterDocument(new FlowDocument(new Paragraph(new Run("new")))), path,
                    WriterDocumentFormat.RibbonKitWriter, cancellation.Token));
            Assert.Equal("old", await File.ReadAllTextAsync(path));
        });
    }

    [Fact]
    public async Task UnsupportedStructuredContentCannotReplaceNativeDestination()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "unsupported.rkw");
            await File.WriteAllTextAsync(path, "old");
            var content = new FlowDocument();
            content.Blocks.Add(new Table());

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new WriterDocumentPersistence().SaveAsync(new WriterDocument(content), path,
                    WriterDocumentFormat.RibbonKitWriter, default));
            Assert.Equal("old", await File.ReadAllTextAsync(path));
        });
    }

    [Fact]
    public async Task RealNativeSessionCommitsOnlyValidStaCandidate()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var validPath = Path.Combine(directory.Path, "valid.rkw");
            var invalidPath = Path.Combine(directory.Path, "invalid.rkw");
            var persistence = new WriterDocumentPersistence();
            await persistence.SaveAsync(
                new WriterDocument(new FlowDocument(new Paragraph(new Run("valid")))), validPath,
                WriterDocumentFormat.RibbonKitWriter, default);
            await File.WriteAllBytesAsync(invalidPath, [0x50, 0x4B, 0x03]);
            var decider = new DiscardUnsavedChangesDecider();
            var session = new WriterDocumentSession(persistence, decider);

            Assert.True(await session.OpenAsync(validPath, WriterDocumentFormat.RibbonKitWriter));
            Assert.Equal(validPath, session.CurrentDocument.Path);
            Assert.Same(System.Windows.Threading.Dispatcher.CurrentDispatcher,
                session.CurrentDocument.Content.Dispatcher);

            session.CurrentDocument.MarkDirty();
            var current = session.CurrentDocument;
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                session.OpenAsync(invalidPath, WriterDocumentFormat.RibbonKitWriter));
            Assert.Same(current, session.CurrentDocument);
            Assert.Equal(validPath, current.Path);
            Assert.Equal(WriterDocumentFormat.RibbonKitWriter, current.Format);
            Assert.True(current.IsDirty);
        });
    }

    private static async Task AssertInvalidAsync(
        IEnumerable<(string Name, byte[] Content)> entries)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "invalid.rkw");
            RkwPackageFixture.WriteOuterPackage(path, entries);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new WriterDocumentPersistence().LoadAsync(path,
                    WriterDocumentFormat.RibbonKitWriter, default));
        });
    }

    private static Task AssertInvalidContentAsync(byte[] content) => AssertInvalidAsync(new[]
    {
        RkwPackageFixture.ManifestEntry(),
        RkwPackageFixture.SettingsEntry(),
        ("content.xamlpackage", content)
    });

    private static byte[] Read(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var destination = new MemoryStream();
        stream.CopyTo(destination);
        return destination.ToArray();
    }

    private static string Text(FlowDocument document) =>
        new TextRange(document.ContentStart, document.ContentEnd).Text;

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("rk-writer-rkw-").FullName;
        public string Path { get; }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private sealed class DiscardUnsavedChangesDecider : IUnsavedChangesDecider
    {
        public Task<UnsavedChangesDecision> DecideAsync(WriterDocument document,
            DocumentTransition transition, CancellationToken cancellationToken) =>
            Task.FromResult(UnsavedChangesDecision.Discard);
    }
}
