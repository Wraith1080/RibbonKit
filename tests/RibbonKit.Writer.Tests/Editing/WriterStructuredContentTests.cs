using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Services.Persistence;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterStructuredContentTests
{
    [Fact]
    public async Task PortableImageInsertionRoundTripsThroughNativePackage()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "image.rkw");
            using var fixture = CreateEditor("before after");
            var run = Assert.IsType<Run>(((Paragraph)fixture.Editor.Document.Blocks.First()).Inlines.FirstInline);
            var caret = run.ContentStart.GetPositionAtOffset(7)!;
            fixture.Editor.Selection.Select(caret, caret);
            using var source = new MemoryStream(CreatePng(4, 3));
            var service = new WriterImageService();

            Assert.True(service.TryInsertImage(fixture.Editor, source,
                new WriterImageInsertionOptions { WidthDip = 40, HeightDip = 30 }));
            Assert.Equal(0, source.Position);
            var inserted = Assert.IsType<InlineUIContainer>(
                ((Paragraph)fixture.Editor.Document.Blocks.First()).Inlines.ElementAt(1));
            var image = Assert.IsType<Image>(inserted.Child);
            var bitmap = Assert.IsType<BitmapImage>(image.Source);
            Assert.True(bitmap.IsFrozen);
            Assert.Equal(4, bitmap.PixelWidth);
            Assert.Equal(3, bitmap.PixelHeight);
            Assert.Equal(40, image.Width);
            Assert.Equal(30, image.Height);

            var persistence = new WriterDocumentPersistence();
            Assert.True(await persistence.SaveAsync(new WriterDocument(fixture.Editor.Document), path,
                WriterDocumentFormat.RibbonKitWriter, default));
            using (var stream = File.OpenRead(path))
            using (var outer = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var content = outer.GetEntry("content.xamlpackage");
                Assert.NotNull(content);
                using var contentStream = content!.Open();
                using var inner = new ZipArchive(contentStream, ZipArchiveMode.Read);
                Assert.Contains(inner.Entries, entry => entry.FullName.StartsWith("Xaml/Image", StringComparison.Ordinal));
                Assert.Contains(inner.Entries, entry => entry.FullName == "Xaml/_rels/Document.xaml.rels");
            }

            var loaded = await persistence.LoadAsync(path, WriterDocumentFormat.RibbonKitWriter, default);
            var loadedParagraph = Assert.IsType<Paragraph>(loaded!.Content.Blocks.First());
            var loadedImage = Assert.IsType<Image>(
                Assert.IsType<InlineUIContainer>(loadedParagraph.Inlines.ElementAt(1)).Child);
            Assert.False(loadedImage.IsHitTestVisible);
            Assert.True(loadedImage.Source is BitmapImage bitmapSource && bitmapSource.IsFrozen);
            Assert.Equal(40, loadedImage.Width);
            Assert.Equal(30, loadedImage.Height);
            var loadedBitmap = Assert.IsType<BitmapImage>(loadedImage.Source);
            Assert.Equal(4, loadedBitmap.PixelWidth);
            Assert.Equal(3, loadedBitmap.PixelHeight);
        });
    }

    [Fact]
    public void HyperlinkCreateEditAndRemoveRetainsVisibleText()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("RibbonKit");
            fixture.Editor.SelectAll();
            var service = new WriterHyperlinkService();
            var paragraph = Assert.IsType<Paragraph>(fixture.Editor.Document.Blocks.First());

            Assert.True(service.TryCreate(fixture.Editor, "https://example.com/one"));
            var hyperlink = Assert.IsType<Hyperlink>(paragraph.Inlines.FirstInline);
            Assert.Equal("https://example.com/one", hyperlink.NavigateUri!.ToString());
            Assert.Equal("RibbonKit", new TextRange(hyperlink.ContentStart, hyperlink.ContentEnd).Text);

            Assert.True(service.TryEdit(fixture.Editor, "mailto:writer@example.com", "Writer"));
            hyperlink = Assert.IsType<Hyperlink>(paragraph.Inlines.FirstInline);
            Assert.Equal("mailto:writer@example.com", hyperlink.NavigateUri!.ToString());
            Assert.Equal("Writer", new TextRange(hyperlink.ContentStart, hyperlink.ContentEnd).Text);
            Assert.True(service.TryRemove(fixture.Editor));
            Assert.DoesNotContain(paragraph.Inlines, inline => inline is Hyperlink);
            Assert.Equal("Writer", new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim());
        });
    }

    [Fact]
    public async Task HyperlinkRoundTripsThroughNativePackage()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var path = Path.Combine(directory.Path, "link.rkw");
            using var fixture = CreateEditor("link");
            fixture.Editor.SelectAll();
            Assert.True(new WriterHyperlinkService().TryCreate(fixture.Editor,
                "https://example.com/round-trip", "link"));
            Assert.True(await new WriterDocumentPersistence().SaveAsync(
                new WriterDocument(fixture.Editor.Document), path,
                WriterDocumentFormat.RibbonKitWriter, default));
            var loaded = await new WriterDocumentPersistence().LoadAsync(path,
                WriterDocumentFormat.RibbonKitWriter, default);
            var paragraph = Assert.IsType<Paragraph>(loaded!.Content.Blocks.First());
            var hyperlink = Assert.IsType<Hyperlink>(paragraph.Inlines.FirstInline);
            Assert.Equal("https://example.com/round-trip", hyperlink.NavigateUri!.ToString());
            Assert.Equal("link", new TextRange(hyperlink.ContentStart, hyperlink.ContentEnd).Text);
        });
    }

    [Theory]
    [InlineData("file:///C:/secret.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,unsafe")]
    [InlineData("relative/path")]
    [InlineData("https://user:password@example.com/private")]
    [InlineData("https://example.com/a b")]
    [InlineData("custom://example.com")]
    [InlineData("mailto:user%0A@example.com")]
    [InlineData("mailto:user%0D@example.com")]
    [InlineData("mailto:user%3Apassword@example.com")]
    public void UnsafeHyperlinkUriFailsWithoutMutation(string value)
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("text");
            fixture.Editor.SelectAll();
            Assert.False(new WriterHyperlinkService().TryCreate(fixture.Editor, value));
            var paragraph = Assert.IsType<Paragraph>(fixture.Editor.Document.Blocks.First());
            Assert.DoesNotContain(paragraph.Inlines, inline => inline is Hyperlink);
            Assert.Equal("text", new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim());
        });
    }

    [Fact]
    public void DateTimeInsertionUsesExplicitCultureAndReplacesSelection()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("old value");
            var paragraph = Assert.IsType<Paragraph>(fixture.Editor.Document.Blocks.First());
            fixture.Editor.Selection.Select(paragraph.ContentStart, paragraph.ContentEnd);
            Assert.True(new WriterDateTimeService().TryInsert(fixture.Editor,
                new DateTimeOffset(2026, 8, 26, 14, 5, 0, TimeSpan.Zero), "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture));
            Assert.Equal("2026-08-26 14:05", new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim());
        });
    }

    [Fact]
    public void InvalidImageBytesAndDimensionsFailSafely()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("safe");
            fixture.Editor.SelectAll();
            var service = new WriterImageService();
            using var invalid = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            Assert.False(service.TryInsertImage(fixture.Editor, invalid));
            Assert.False(service.TryInsertImage(fixture.Editor, new MemoryStream(CreatePng(2, 2)),
                new WriterImageInsertionOptions { WidthDip = double.NaN }));
            Assert.False(service.TryInsertImage(fixture.Editor, CreatePngHeader(100_000, 100_000)));
            var paragraph = Assert.IsType<Paragraph>(fixture.Editor.Document.Blocks.First());
            Assert.DoesNotContain(paragraph.Inlines, inline => inline is InlineUIContainer);
            Assert.Equal("safe", new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.Trim());
        });
    }

    [Fact]
    public void DateTimeInsertionUsesOneNativeUndoAndRedoUnit()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("old");
            fixture.Editor.SelectAll();
            var service = new WriterDateTimeService();
            Assert.True(service.TryInsert(fixture.Editor,
                new DateTimeOffset(2026, 8, 26, 14, 5, 0, TimeSpan.Zero),
                "yyyy-MM-dd", CultureInfo.InvariantCulture));
            Assert.Equal("2026-08-26", DocumentText(fixture.Editor));
            fixture.Editor.Undo();
            Assert.Equal("old", DocumentText(fixture.Editor));
            fixture.Editor.Redo();
            Assert.Equal("2026-08-26", DocumentText(fixture.Editor));
        });
    }

    [Fact]
    public void ImageOptionsCannotExceedNativePersistenceLimits()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("safe");
            fixture.Editor.SelectAll();
            var service = new WriterImageService();
            var png = CreatePng(2, 2);
            Assert.False(service.TryInsertImage(fixture.Editor, png,
                new WriterImageInsertionOptions { MaximumBytes = 16 * 1024 * 1024 + 1 }));
            Assert.False(service.TryInsertImage(fixture.Editor, png,
                new WriterImageInsertionOptions { MaximumPixels = 32 * 1024 * 1024 + 1 }));
            Assert.False(service.TryInsertImage(fixture.Editor, png,
                new WriterImageInsertionOptions { MaximumDimension = 8193 }));
            Assert.Equal("safe", DocumentText(fixture.Editor));
        });
    }

    [Fact]
    public void EmptyDocumentInsertionCreatesUndoableParagraph()
    {
        StaTestHelper.Run(() =>
        {
            var editor = new RichTextBox
            {
                Document = new FlowDocument(),
                IsUndoEnabled = true
            };
            var window = new Window { Content = editor, Width = 400, Height = 220, ShowInTaskbar = false };
            window.Show();
            try
            {
                editor.Focus();
                Assert.True(new WriterDateTimeService().TryInsert(editor,
                    new DateTimeOffset(2026, 8, 26, 14, 5, 0, TimeSpan.Zero),
                    "yyyy-MM-dd", CultureInfo.InvariantCulture));
                Assert.Equal("2026-08-26", DocumentText(editor));
                editor.Undo();
                Assert.Equal(string.Empty, DocumentText(editor));
                editor.Redo();
                Assert.Equal("2026-08-26", DocumentText(editor));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void HyperlinkEditRebuildUsesOneNativeUndoAndRedoUnit()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("link");
            fixture.Editor.SelectAll();
            var service = new WriterHyperlinkService();
            Assert.True(service.TryCreate(fixture.Editor, "https://example.com/one", "one"));
            Assert.True(service.TryEdit(fixture.Editor, "https://example.com/two", "two"));
            AssertHyperlink(fixture.Editor, "https://example.com/two", "two");
            fixture.Editor.Undo();
            AssertHyperlink(fixture.Editor, "https://example.com/one", "one");
            fixture.Editor.Redo();
            AssertHyperlink(fixture.Editor, "https://example.com/two", "two");
        });
    }

    [Fact]
    public void CrossParagraphSelectionFailsWithoutDeletingContent()
    {
        StaTestHelper.Run(() =>
        {
            using var fixture = CreateEditor("first");
            fixture.Editor.Document.Blocks.Add(new Paragraph(new Run("second")));
            var start = fixture.Editor.Document.ContentStart.GetPositionAtOffset(1)!;
            var end = fixture.Editor.Document.ContentEnd.GetPositionAtOffset(-1)!;
            fixture.Editor.Selection.Select(start, end);
            Assert.False(new WriterDateTimeService().TryInsert(fixture.Editor,
                new DateTimeOffset(2026, 8, 26, 14, 5, 0, TimeSpan.Zero),
                "yyyy-MM-dd", CultureInfo.InvariantCulture));
            Assert.Equal("first\r\nsecond", DocumentText(fixture.Editor).Trim());
        });
    }

    private static Fixture CreateEditor(string text)
    {
        var editor = new RichTextBox
        {
            Document = new FlowDocument(new Paragraph(new Run(text))),
            IsUndoEnabled = true
        };
        var window = new Window { Content = editor, Width = 400, Height = 220, ShowInTaskbar = false };
        window.Show();
        editor.Focus();
        return new Fixture(window, editor);
    }

    private static byte[] CreatePng(int width, int height)
    {
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[width * height * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = 40;
            pixels[index + 1] = 100;
            pixels[index + 2] = 220;
            pixels[index + 3] = 255;
        }
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static byte[] CreatePngHeader(int width, int height)
    {
        var bytes = new byte[29];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(bytes, 0);
        bytes[11] = 13;
        bytes[12] = 0x49;
        bytes[13] = 0x48;
        bytes[14] = 0x44;
        bytes[15] = 0x52;
        bytes[16] = (byte)(width >> 24);
        bytes[17] = (byte)(width >> 16);
        bytes[18] = (byte)(width >> 8);
        bytes[19] = (byte)width;
        bytes[20] = (byte)(height >> 24);
        bytes[21] = (byte)(height >> 16);
        bytes[22] = (byte)(height >> 8);
        bytes[23] = (byte)height;
        return bytes;
    }

    private static string DocumentText(RichTextBox editor) =>
        new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text.Trim();

    private static void AssertHyperlink(RichTextBox editor, string uri, string text)
    {
        var paragraph = Assert.IsType<Paragraph>(editor.Document.Blocks.First());
        var link = Assert.IsType<Hyperlink>(paragraph.Inlines.FirstInline);
        Assert.Equal(uri, link.NavigateUri!.ToString());
        Assert.Equal(text, new TextRange(link.ContentStart, link.ContentEnd).Text);
    }

    private sealed class Fixture(Window window, RichTextBox editor) : IDisposable
    {
        public RichTextBox Editor { get; } = editor;
        public void Dispose() => window.Close();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("rk-writer-w3a-").FullName;
        public string Path { get; }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
