using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.Persistence;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Persistence;

public sealed class WriterRkwPersistenceTests
{
    [Fact]
    public async Task NativePackageRoundTripsFontDialogStrikeAndBaselineEffects()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "font-effects.rkw");
            var run = new Run("effects")
            {
                TextDecorations = WriterFontEffects.CreateDecorations(
                    underline: true, WriterStrikethroughStyle.Double),
                BaselineAlignment = BaselineAlignment.Superscript
            };
            var source = new WriterDocument(new FlowDocument(new Paragraph(run)));
            var persistence = new WriterDocumentPersistence();

            Assert.True(await persistence.SaveAsync(source, path,
                WriterDocumentFormat.RibbonKitWriter, default));
            var loaded = await persistence.LoadAsync(path,
                WriterDocumentFormat.RibbonKitWriter, default);

            var paragraph = Assert.IsType<Paragraph>(loaded!.Content.Blocks.FirstBlock);
            var inline = Assert.IsAssignableFrom<Inline>(paragraph.Inlines.FirstInline);
            Assert.Equal(BaselineAlignment.Superscript, inline.BaselineAlignment);
            Assert.Contains(inline.TextDecorations, decoration =>
                decoration.Location == TextDecorationLocation.Underline);
            Assert.Equal(WriterStrikethroughStyle.Double,
                WriterFontEffects.ReadStrikethrough(inline.TextDecorations));
        });
    }

    [Fact]
    public async Task NativePackageRejectsUnsafeTextDecorationGraphs()
    {
        const string prefix = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph><Run><Run.TextDecorations><TextDecorationCollection>";
        const string suffix = "</TextDecorationCollection></Run.TextDecorations>effects</Run></Paragraph></Section>";
        foreach (var decoration in new[]
                 {
                     "<TextDecoration Location=\"DropShadow\" />",
                     "<TextDecoration Location=\"Strikethrough\" PenOffset=\"2\" />",
                     "<Button />"
                 })
        {
            await AssertInvalidContentAsync(RkwPackageFixture.CreateInnerXamlPackage(
                prefix + decoration + suffix));
        }
    }


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
    public async Task NativePackageSaveCloseReopenPreservesRepresentativeStructuredDocument()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var firstPath = Path.Combine(directory.Path, "structured-first.rkw");
            var reopenedPath = Path.Combine(directory.Path, "structured-reopened.rkw");
            var intro = new Paragraph();
            intro.Inlines.Add(new Bold(new Run("Formatted")) { Foreground = Brushes.DarkBlue });
            intro.Inlines.Add(new Run(" "));
            intro.Inlines.Add(new Hyperlink(new Run("safe link"))
            {
                NavigateUri = new Uri("https://example.com/document")
            });
            intro.Inlines.Add(new Run(" "));
            intro.Inlines.Add(new InlineUIContainer(new Image
            {
                Source = CreateBitmap(3, 2), Width = 30, Height = 20
            }));

            var table = new Table
            {
                CellSpacing = 2,
                BorderBrush = Brushes.DarkSlateBlue,
                BorderThickness = new Thickness(1),
                Background = Brushes.WhiteSmoke
            };
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            var group = new TableRowGroup();
            var heading = new TableCell(new Paragraph(new Run("Heading")))
            {
                ColumnSpan = 2,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(2, 3, 4, 5),
                BorderBrush = Brushes.CornflowerBlue,
                BorderThickness = new Thickness(0.5),
                Background = Brushes.LightBlue
            };
            group.Rows.Add(new TableRow { Cells = { heading } });
            group.Rows.Add(new TableRow
            {
                Cells =
                {
                    new TableCell(new Paragraph(new Run("Left"))),
                    new TableCell(new Paragraph(new Run("Right")))
                }
            });
            table.RowGroups.Add(group);

            var content = new FlowDocument();
            content.Blocks.Add(intro);
            content.Blocks.Add(table);
            var settings = DocumentPageSettings.CreateCustom(700, 1000,
                DocumentPageOrientation.Landscape, new DocumentPageMargins(30, 40, 50, 60));
            var persistence = new WriterDocumentPersistence();

            Assert.True(await persistence.SaveAsync(new WriterDocument(content, pageSettings: settings),
                firstPath, WriterDocumentFormat.RibbonKitWriter, default));
            var firstLoad = Assert.IsType<WriterDocument>(await persistence.LoadAsync(firstPath,
                WriterDocumentFormat.RibbonKitWriter, default));
            Assert.True(await persistence.SaveAsync(firstLoad, reopenedPath,
                WriterDocumentFormat.RibbonKitWriter, default));
            var reopened = Assert.IsType<WriterDocument>(await persistence.LoadAsync(reopenedPath,
                WriterDocumentFormat.RibbonKitWriter, default));

            Assert.Equal(settings, reopened.PageSettings);
            var loadedIntro = Assert.IsType<Paragraph>(reopened.Content.Blocks.FirstBlock);
            Assert.Equal(FontWeights.Bold,
                Assert.IsAssignableFrom<Inline>(loadedIntro.Inlines.FirstInline).FontWeight);
            var loadedLink = Assert.Single(loadedIntro.Inlines.OfType<Hyperlink>());
            Assert.Equal("https://example.com/document", loadedLink.NavigateUri!.AbsoluteUri.TrimEnd('/'));
            var loadedImage = Assert.IsType<Image>(Assert.Single(
                loadedIntro.Inlines.OfType<InlineUIContainer>()).Child);
            Assert.Equal(30, loadedImage.Width);
            Assert.Equal(20, loadedImage.Height);

            var loadedTable = Assert.IsType<Table>(loadedIntro.NextBlock);
            Assert.Equal(2, loadedTable.Columns.Count);
            Assert.Equal(new GridLength(120), loadedTable.Columns[0].Width);
            Assert.Equal(new GridLength(80), loadedTable.Columns[1].Width);
            Assert.Equal(2, loadedTable.CellSpacing);
            Assert.Equal(new Thickness(1), loadedTable.BorderThickness);
            Assert.Equal(Colors.DarkSlateBlue,
                Assert.IsType<SolidColorBrush>(loadedTable.BorderBrush).Color);
            var loadedGroup = Assert.Single(loadedTable.RowGroups.Cast<TableRowGroup>());
            Assert.Equal(2, loadedGroup.Rows.Count);
            var loadedHeading = Assert.Single(loadedGroup.Rows[0].Cells.Cast<TableCell>());
            Assert.Equal(2, loadedHeading.ColumnSpan);
            Assert.Equal(TextAlignment.Center, loadedHeading.TextAlignment);
            Assert.Equal(new Thickness(2, 3, 4, 5), loadedHeading.Padding);
            Assert.Equal(new Thickness(0.5), loadedHeading.BorderThickness);
            Assert.Equal("Heading", new TextRange(
                loadedHeading.ContentStart, loadedHeading.ContentEnd).Text.Trim());
            Assert.Equal(new[] { "Left", "Right" }, loadedGroup.Rows[1].Cells.Cast<TableCell>()
                .Select(cell => new TextRange(cell.ContentStart, cell.ContentEnd).Text.Trim()));
        });
    }

    [Theory]
    [InlineData(WriterTableHorizontalAlignment.Left, 0, 360)]
    [InlineData(WriterTableHorizontalAlignment.Center, 180, 180)]
    [InlineData(WriterTableHorizontalAlignment.Right, 360, 0)]
    public async Task TablePlacementWithDefaultMarginsSavesAndReopens(
        WriterTableHorizontalAlignment alignment, double expectedLeft, double expectedRight)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, $"table-{alignment}.rkw");
            var document = new FlowDocument(new Paragraph(new Run("before")));
            var editor = new RichTextBox { Document = document };
            var paragraph = Assert.IsType<Paragraph>(document.Blocks.FirstBlock);
            editor.Selection.Select(paragraph.ContentStart, paragraph.ContentStart);
            using var tables = new WriterTableService(editor);
            var table = Assert.IsType<Table>(tables.InsertTable(1, 2));

            Assert.True(tables.SetTableHorizontalAlignment(table, alignment,
                tableWidth: 240, availableWidth: 600));

            var persistence = new WriterDocumentPersistence();
            Assert.True(await persistence.SaveAsync(new WriterDocument(document), path,
                WriterDocumentFormat.RibbonKitWriter, default));
            var reopened = Assert.IsType<WriterDocument>(await persistence.LoadAsync(path,
                WriterDocumentFormat.RibbonKitWriter, default));
            var reopenedTable = Assert.Single(reopened.Content.Blocks.OfType<Table>());
            Assert.Equal(new Thickness(expectedLeft, 0, expectedRight, 0), reopenedTable.Margin);
        });
    }

    [Fact]
    public async Task ContentSchemaTwoOwnsTablesAndVersionOneFixtureRemainsReadable()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var versionOnePath = Path.Combine(directory.Path, "version-one.rkw");
            var migratedPath = Path.Combine(directory.Path, "migrated.rkw");
            RkwPackageFixture.WriteOuterPackage(versionOnePath, new[]
            {
                RkwPackageFixture.ManifestEntry(),
                RkwPackageFixture.SettingsEntry(),
                RkwPackageFixture.ContentEntry("<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph><Run>version one</Run></Paragraph></Section>")
            });
            var persistence = new WriterDocumentPersistence();
            var versionOne = Assert.IsType<WriterDocument>(await persistence.LoadAsync(versionOnePath,
                WriterDocumentFormat.RibbonKitWriter, default));
            Assert.Equal("version one", Text(versionOne.Content).Trim());
            Assert.True(await persistence.SaveAsync(versionOne, migratedPath,
                WriterDocumentFormat.RibbonKitWriter, default));
            using (var archive = new ZipArchive(File.OpenRead(migratedPath), ZipArchiveMode.Read))
            using (var manifest = JsonDocument.Parse(Read(archive.GetEntry("manifest.json")!)))
            {
                Assert.Equal(1, manifest.RootElement.GetProperty("schemaVersion").GetInt32());
                Assert.Equal(2, manifest.RootElement.GetProperty("minimumReaderVersion").GetInt32());
                Assert.Equal(2, manifest.RootElement.GetProperty("contentSchemaVersion").GetInt32());
                Assert.Equal(1, manifest.RootElement.GetProperty("settingsSchemaVersion").GetInt32());
            }

            const string tableXaml = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Table><Table.Columns><TableColumn Width=\"100\" /></Table.Columns><TableRowGroup><TableRow><TableCell><Paragraph><Run>cell</Run></Paragraph></TableCell></TableRow></TableRowGroup></Table></Section>";
            await AssertInvalidContentAsync(RkwPackageFixture.CreateInnerXamlPackage(tableXaml));

            var versionTwoPath = Path.Combine(directory.Path, "version-two-table.rkw");
            RkwPackageFixture.WriteOuterPackage(versionTwoPath, new[]
            {
                ("manifest.json", RkwPackageFixture.Utf8("{\"format\":\"RibbonKit.Writer\",\"schemaVersion\":1,\"minimumReaderVersion\":2,\"contentSchemaVersion\":2,\"settingsSchemaVersion\":1,\"requiredFeatures\":[]}")),
                RkwPackageFixture.SettingsEntry(),
                ("content.xamlpackage", RkwPackageFixture.CreateInnerXamlPackage(tableXaml))
            });
            var versionTwo = Assert.IsType<WriterDocument>(await persistence.LoadAsync(versionTwoPath,
                WriterDocumentFormat.RibbonKitWriter, default));
            Assert.IsType<Table>(versionTwo.Content.Blocks.FirstBlock);
        });
    }

    [Theory]
    [InlineData("<Table><TableRowGroup><TableRow><TableCell ColumnSpan=\"1025\"><Paragraph /></TableCell></TableRow></TableRowGroup></Table>")]
    [InlineData("<Table><TableRowGroup><TableRow><TableCell><Button /></TableCell></TableRow></TableRowGroup></Table>")]
    [InlineData("<Table><TableRowGroup><TableRow><TableCell><Paragraph /></TableCell><TableCell RowSpan=\"2\"><Paragraph /></TableCell></TableRow></TableRowGroup></Table>")]
    public async Task NativePackageRejectsUnsafeOrInvalidTableGraphs(string table)
    {
        var xaml = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
            table + "</Section>";
        var manifest = RkwPackageFixture.Utf8("{\"format\":\"RibbonKit.Writer\",\"schemaVersion\":1,\"minimumReaderVersion\":2,\"contentSchemaVersion\":2,\"settingsSchemaVersion\":1,\"requiredFeatures\":[]}");
        await AssertInvalidAsync(new[]
        {
            ("manifest.json", manifest),
            RkwPackageFixture.SettingsEntry(),
            ("content.xamlpackage", RkwPackageFixture.CreateInnerXamlPackage(xaml))
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
            Assert.Equal(2, manifest.RootElement.GetProperty("minimumReaderVersion").GetInt32());
            Assert.Equal(2, manifest.RootElement.GetProperty("contentSchemaVersion").GetInt32());
            Assert.Equal(1, manifest.RootElement.GetProperty("settingsSchemaVersion").GetInt32());
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
    [InlineData("{\"format\":\"RibbonKit.Writer\",\"schemaVersion\":1,\"minimumReaderVersion\":1,\"contentSchemaVersion\":2,\"settingsSchemaVersion\":1,\"requiredFeatures\":[]}")]
    [InlineData("{\"format\":\"RibbonKit.Writer\",\"schemaVersion\":1,\"minimumReaderVersion\":3,\"contentSchemaVersion\":2,\"settingsSchemaVersion\":1,\"requiredFeatures\":[]}")]
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
    [InlineData("https://user:password@example.com/secret")]
    [InlineData("https://example.com/a b")]
    [InlineData("file:///C:/secret.txt")]
    [InlineData("javascript:alert(1)")]
    public async Task UnsafeStructuredHyperlinkUrisAreRejected(string uri)
    {
        var xaml = $"<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph><Hyperlink NavigateUri=\"{uri}\"><Run>link</Run></Hyperlink></Paragraph></Section>";
        await AssertInvalidContentAsync(RkwPackageFixture.CreateInnerXamlPackage(xaml));
    }

    [Fact]
    public async Task ExternalImageAndUnsafeUiElementShapesAreRejected()
    {
        const string prefix = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>";
        const string suffix = "</Paragraph></Section>";
        await AssertInvalidContentAsync(RkwPackageFixture.CreateInnerXamlPackage(
            prefix + "<InlineUIContainer><Button /></InlineUIContainer>" + suffix));
        await AssertInvalidContentAsync(RkwPackageFixture.CreateInnerXamlPackage(
            prefix + "<InlineUIContainer><Image><Image.Source><BitmapImage UriSource=\"https://example.invalid/image.png\" /></Image.Source></Image></InlineUIContainer>" + suffix));

        var xaml = prefix + "<InlineUIContainer><Image><Image.Source><BitmapImage UriSource=\"./Image1.png\" CacheOption=\"OnLoad\" /></Image.Source></Image></InlineUIContainer>" + suffix;
        var inner = RkwPackageFixture.CreateOuterPackage(new[]
        {
            RkwPackageFixture.XamlEntry(xaml),
            RkwPackageFixture.RelationshipsEntry(),
            RkwPackageFixture.ContentTypesEntry(
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"xaml\" ContentType=\"application/vnd.ms-wpf.xaml+xml\" /><Default Extension=\"png\" ContentType=\"text/plain\" /></Types>"),
            ("Xaml/Image1.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ("Xaml/_rels/Document.xaml.rels", RkwPackageFixture.Utf8(
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rImage\" Type=\"http://schemas.microsoft.com/wpf/2005/10/xaml/component\" Target=\"/Xaml/Image1.png\" /></Relationships>"))
        });
        await AssertInvalidContentAsync(inner);
    }

    [Fact]
    public async Task ImageContentTypeDeclarationsMustMatchImagePartsExactly()
    {
        const string prefix = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>";
        const string suffix = "</Paragraph></Section>";
        const string imageXaml = prefix + "<InlineUIContainer><Image><Image.Source><BitmapImage UriSource=\"./Image1.png\" CacheOption=\"OnLoad\" /></Image.Source></Image></InlineUIContainer>" + suffix;
        const string imageRelationships = "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rImage\" Type=\"http://schemas.microsoft.com/wpf/2005/10/xaml/component\" Target=\"/Xaml/Image1.png\" /></Relationships>";
        const string missingImageDeclaration = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"xaml\" ContentType=\"application/vnd.ms-wpf.xaml+xml\" /></Types>";
        const string extraImageDeclaration = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"xaml\" ContentType=\"application/vnd.ms-wpf.xaml+xml\" /><Default Extension=\"png\" ContentType=\"image/png\" /><Default Extension=\"gif\" ContentType=\"image/gif\" /></Types>";
        const string caseCollision = "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"xaml\" ContentType=\"application/vnd.ms-wpf.xaml+xml\" /><Default Extension=\"png\" ContentType=\"image/png\" /><Default Extension=\"PNG\" ContentType=\"image/png\" /></Types>";

        var image = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52,
            0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0
        };

        foreach (var contentTypes in new[] { missingImageDeclaration, extraImageDeclaration, caseCollision })
        {
            var inner = RkwPackageFixture.CreateOuterPackage(new[]
            {
                RkwPackageFixture.XamlEntry(imageXaml),
                RkwPackageFixture.RelationshipsEntry(),
                RkwPackageFixture.ContentTypesEntry(contentTypes),
                ("Xaml/Image1.png", image),
                ("Xaml/_rels/Document.xaml.rels", RkwPackageFixture.Utf8(imageRelationships))
            });
            await AssertInvalidContentAsync(inner);
        }

        var extraOnly = RkwPackageFixture.CreateOuterPackage(new[]
        {
            RkwPackageFixture.XamlEntry(),
            RkwPackageFixture.RelationshipsEntry(),
            RkwPackageFixture.ContentTypesEntry(extraImageDeclaration)
        });
        await AssertInvalidContentAsync(extraOnly);
    }

    [Fact]
    public async Task ValidSignatureCorruptPngIsWrappedAsInvalidData()
    {
        const string xaml = "<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph><InlineUIContainer><Image><Image.Source><BitmapImage UriSource=\"./Image1.png\" CacheOption=\"OnLoad\" /></Image.Source></Image></InlineUIContainer></Paragraph></Section>";
        var inner = RkwPackageFixture.CreateOuterPackage(new[]
        {
            RkwPackageFixture.XamlEntry(xaml),
            RkwPackageFixture.RelationshipsEntry(),
            RkwPackageFixture.ContentTypesEntry(
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" /><Default Extension=\"xaml\" ContentType=\"application/vnd.ms-wpf.xaml+xml\" /><Default Extension=\"png\" ContentType=\"image/png\" /></Types>"),
            ("Xaml/Image1.png", CreateCorruptPngWithValidHeader()),
            ("Xaml/_rels/Document.xaml.rels", RkwPackageFixture.Utf8(
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rImage\" Type=\"http://schemas.microsoft.com/wpf/2005/10/xaml/component\" Target=\"/Xaml/Image1.png\" /></Relationships>"))
        });
        await AssertInvalidContentAsync(inner);
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

    [Fact]
    public async Task OversizedExpandedPackageIsRejectedBeforeSessionReplacement()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var validPath = Path.Combine(directory.Path, "valid.rkw");
            var oversizedPath = Path.Combine(directory.Path, "oversized.rkw");
            var persistence = new WriterDocumentPersistence();
            await persistence.SaveAsync(
                new WriterDocument(new FlowDocument(new Paragraph(new Run("valid")))), validPath,
                WriterDocumentFormat.RibbonKitWriter, default);
            RkwPackageFixture.WriteOuterPackage(oversizedPath, new[]
            {
                RkwPackageFixture.ManifestEntry(),
                RkwPackageFixture.SettingsEntry(),
                ("content.xamlpackage", new byte[WriterRkwPackage.MaximumExpandedBytes])
            });

            var session = new WriterDocumentSession(persistence, new DiscardUnsavedChangesDecider());
            Assert.True(await session.OpenAsync(validPath, WriterDocumentFormat.RibbonKitWriter));
            var current = session.CurrentDocument;

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                session.OpenAsync(oversizedPath, WriterDocumentFormat.RibbonKitWriter));

            Assert.Same(current, session.CurrentDocument);
            Assert.Equal(validPath, session.CurrentDocument.Path);
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

    private static byte[] CreateCorruptPngWithValidHeader()
    {
        var bytes = new byte[29];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        bytes[11] = 13;
        bytes[12] = 0x49;
        bytes[13] = 0x48;
        bytes[14] = 0x44;
        bytes[15] = 0x52;
        bytes[19] = 1;
        bytes[23] = 1;
        return bytes;
    }

    private static BitmapSource CreateBitmap(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = 220;
            pixels[index + 1] = 100;
            pixels[index + 2] = 40;
            pixels[index + 3] = 255;
        }
        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null,
            pixels, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

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
