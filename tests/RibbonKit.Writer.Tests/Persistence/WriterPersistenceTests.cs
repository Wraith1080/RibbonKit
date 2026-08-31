using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Persistence;
using Xunit;
using RibbonKit.Writer.Tests.Document;

namespace RibbonKit.Writer.Tests.Persistence;

public sealed class WriterPersistenceTests
{
    [Fact]
    public async Task UnicodeTextRoundTripsWithoutStructuralNewlineAccumulation()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "first.txt");
            var secondPath = Path.Combine(directory.Path, "second.txt");
            var thirdPath = Path.Combine(directory.Path, "third.txt");
            var store = new WriterDocumentPersistence();
            var doc = new FlowDocument(new Paragraph(new Run("こんにちは — café")));
            Assert.True(await store.SaveAsync(new WriterDocument(doc), path, WriterDocumentFormat.PlainText, default));
            var loaded = await store.LoadAsync(path, WriterDocumentFormat.PlainText, default);
            Assert.Contains("こんにちは", new TextRange(loaded!.Content.ContentStart, loaded.Content.ContentEnd).Text);
            await store.SaveAsync(loaded, secondPath, WriterDocumentFormat.PlainText, default);
            var reloaded = await store.LoadAsync(secondPath, WriterDocumentFormat.PlainText, default);
            await store.SaveAsync(reloaded!, thirdPath, WriterDocumentFormat.PlainText, default);
            Assert.Equal(await File.ReadAllTextAsync(secondPath), await File.ReadAllTextAsync(thirdPath));
        });
    }

    [Theory]
    [InlineData("alpha", false)]
    [InlineData("alpha\n", true)]
    public async Task PlainTextPreservesIntentionalTerminalNewline(string text, bool terminal)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var input = Path.Combine(directory.Path, "input.txt");
            var output = Path.Combine(directory.Path, "output.txt");
            var secondOutput = Path.Combine(directory.Path, "second-output.txt");
            await File.WriteAllTextAsync(input, text);
            var store = new WriterDocumentPersistence();
            var doc = await store.LoadAsync(input, WriterDocumentFormat.PlainText, default);
            await store.SaveAsync(doc!, output, WriterDocumentFormat.PlainText, default);
            var firstResult = await File.ReadAllTextAsync(output);
            Assert.Equal(text, firstResult);
            Assert.Equal(terminal, firstResult.EndsWith('\n'));
            var reloaded = await store.LoadAsync(output, WriterDocumentFormat.PlainText, default);
            await store.SaveAsync(reloaded!, secondOutput, WriterDocumentFormat.PlainText, default);
            Assert.Equal(text, await File.ReadAllTextAsync(secondOutput));
        });
    }

    [Fact]
    public async Task FormattedRtfRoundTrips()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "formatted.rtf");
            var store = new WriterDocumentPersistence();
            var run = new Run("Bold"); run.FontWeight = System.Windows.FontWeights.Bold;
            await store.SaveAsync(new WriterDocument(new FlowDocument(new Paragraph(run))), path,
                WriterDocumentFormat.RichText, default);
            var loaded = await store.LoadAsync(path, WriterDocumentFormat.RichText, default);
            var range = new TextRange(loaded!.Content.ContentStart, loaded.Content.ContentEnd);
            Assert.Equal("Bold", range.Text.Trim());
            Assert.Equal(System.Windows.FontWeights.Bold, range.GetPropertyValue(TextElement.FontWeightProperty));
        });
    }

    [Fact]
    public async Task StructuredContentCompatibilityFixturesExposeRtfAndTextFidelityLoss()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var rtfPath = Path.Combine(directory.Path, "structured.rtf");
            var textPath = Path.Combine(directory.Path, "structured.txt");
            var table = new Table
            {
                CellSpacing = 2,
                BorderBrush = Brushes.Blue,
                BorderThickness = new Thickness(1)
            };
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            var group = new TableRowGroup();
            group.Rows.Add(new TableRow
            {
                Cells =
                {
                    new TableCell(new Paragraph(new Run("Merged heading")))
                    {
                        ColumnSpan = 2,
                        Padding = new Thickness(2, 3, 4, 5),
                        Background = Brushes.LightBlue
                    }
                }
            });
            table.RowGroups.Add(group);
            var content = new FlowDocument();
            content.Blocks.Add(new Paragraph(new Bold(new Run("Before"))));
            content.Blocks.Add(table);
            content.Blocks.Add(new Paragraph(new Run("After")));
            var source = new WriterDocument(content, pageSettings: DocumentPageSettings.A4());
            var persistence = new WriterDocumentPersistence();

            Assert.True(await persistence.SaveAsync(source, rtfPath,
                WriterDocumentFormat.RichText, default));
            var rtf = Assert.IsType<WriterDocument>(await persistence.LoadAsync(rtfPath,
                WriterDocumentFormat.RichText, default));
            var rtfTable = Assert.Single(rtf.Content.Blocks.OfType<Table>());
            var rtfCell = Assert.Single(rtfTable.RowGroups[0].Rows[0].Cells.Cast<TableCell>());
            Assert.Equal("Merged heading", new TextRange(
                rtfCell.ContentStart, rtfCell.ContentEnd).Text.Trim());
            Assert.Equal(1, rtfCell.ColumnSpan);
            Assert.NotEqual(new Thickness(1), rtfTable.BorderThickness);
            Assert.NotEqual(source.PageSettings, rtf.PageSettings);

            Assert.True(await persistence.SaveAsync(source, textPath,
                WriterDocumentFormat.PlainText, default));
            var text = Assert.IsType<WriterDocument>(await persistence.LoadAsync(textPath,
                WriterDocumentFormat.PlainText, default));
            Assert.Empty(text.Content.Blocks.OfType<Table>());
            var flattened = new TextRange(text.Content.ContentStart, text.Content.ContentEnd).Text;
            Assert.Contains("Before", flattened);
            Assert.Contains("Merged heading", flattened);
            Assert.Contains("After", flattened);
            Assert.False(WriterDocumentPersistence.GetCapabilities(
                WriterDocumentFormat.RichText).PreservesTables);
            Assert.False(WriterDocumentPersistence.GetCapabilities(
                WriterDocumentFormat.PlainText).PreservesTables);
        });
    }

    [Fact]
    public async Task MissingRtfThrowsFileNotFound()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "missing.rtf");
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                new WriterDocumentPersistence().LoadAsync(path, WriterDocumentFormat.RichText, default));
        });
    }

    [Fact]
    public async Task CorruptRtfThrowsInvalidData()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "corrupt.rtf");
            await File.WriteAllTextAsync(path, "not rtf");
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                new WriterDocumentPersistence().LoadAsync(path, WriterDocumentFormat.RichText, default));
        });
    }
    [Fact]
    public async Task UnsupportedAndMismatchedFormatsFail()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var service = new WriterDocumentPersistence();
            await Assert.ThrowsAsync<ArgumentException>(() => service.LoadAsync(
                Path.Combine(directory.Path, "document.rtf"), WriterDocumentFormat.RibbonKitWriter, default));
            await Assert.ThrowsAsync<ArgumentException>(() => service.LoadAsync(
                Path.Combine(directory.Path, "document.rtf"), WriterDocumentFormat.PlainText, default));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.LoadAsync(
                Path.Combine(directory.Path, "document.txt"), (WriterDocumentFormat)99, default));

            var text = WriterDocumentPersistence.GetCapabilities(WriterDocumentFormat.PlainText);
            Assert.False(text.PreservesFormatting);
            Assert.False(text.PreservesImages);
            Assert.False(text.PreservesTables);
            Assert.False(text.PreservesPageSettings);
            var richText = WriterDocumentPersistence.GetCapabilities(WriterDocumentFormat.RichText);
            Assert.True(richText.PreservesFormatting);
            Assert.False(richText.PreservesPageSettings);
            var native = WriterDocumentPersistence.GetCapabilities(WriterDocumentFormat.RibbonKitWriter);
            Assert.True(native.PreservesFormatting);
            Assert.True(native.PreservesImages);
            Assert.True(native.PreservesHyperlinks);
            Assert.True(native.PreservesTables);
            Assert.True(native.PreservesPageSettings);
        });
    }

    [Fact]
    public async Task Utf8BomLoadsOnTheStaDispatcher()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "bom.txt");
            await File.WriteAllTextAsync(path, "héllo", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            var loaded = await new WriterDocumentPersistence().LoadAsync(path, WriterDocumentFormat.PlainText, default);
            Assert.Same(System.Windows.Threading.Dispatcher.CurrentDispatcher, loaded!.Content.Dispatcher);
            Assert.True(loaded.Content.Dispatcher.CheckAccess());
        });
    }

    [Fact]
    public async Task CancellationDoesNotReplaceDestination()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "cancelled.txt");
            await File.WriteAllTextAsync(path, "old");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var service = new WriterDocumentPersistence();
            var document = new WriterDocument(new FlowDocument(new Paragraph(new Run("new"))));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SaveAsync(
                document, path, WriterDocumentFormat.PlainText, cancellation.Token));
            Assert.Equal("old", await File.ReadAllTextAsync(path));
        });
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("rk-writer-").FullName;

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
