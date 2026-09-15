using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace RibbonKit.Writer.Pagination;

// Transient, trusted in-process content only. Persisted files continue through the
// strict native package reader. No DispatcherObject crosses the worker boundary.
internal sealed record WriterPaginationContentSnapshot(string Xaml, int SymbolCount,
    ImmutableArray<WriterPaginationImagePixels?> Images, ImmutableArray<string> RunTexts)
{
    internal static WriterPaginationContentSnapshot? Capture(FlowDocument document,
        Dictionary<BitmapSource, WriterPaginationImagePixels> imageCache)
    {
        var images = EnumerateImages(document).ToArray();
        if (images.Any(image => image.Source is not null and not BitmapSource))
            return null;
        var xml = XElement.Parse(XamlWriter.Save(document), LoadOptions.PreserveWhitespace);
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        xml.SetAttributeValue(XNamespace.Xml + "space", "preserve");
        // XamlWriter's implicit inline syntax drops empty Runs. Retain explicit
        // inline containers so source symbol offsets survive the round trip.
        var paragraphs = EnumerateElements(document).OfType<Paragraph>().ToArray();
        var paragraphNodes = xml.Descendants(ns + "Paragraph").ToArray();
        if (paragraphs.Length != paragraphNodes.Length)
            return null;
        for (var index = 0; index < paragraphs.Length; index++)
            ReplaceInlineContent(paragraphNodes[index], paragraphs[index].Inlines);
        var nodes = xml.Descendants(ns + "Image").ToArray();
        if (nodes.Length != images.Length)
            return null;
        var pixels = ImmutableArray.CreateBuilder<WriterPaginationImagePixels?>(images.Length);
        var retained = new HashSet<BitmapSource>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < images.Length; index++)
        {
            nodes[index].Attribute("Source")?.Remove();
            nodes[index].Elements(ns + "Image.Source").Remove();
            if (images[index].Source is not BitmapSource bitmap)
            {
                pixels.Add(null);
                continue;
            }
            retained.Add(bitmap);
            if (!bitmap.IsFrozen || !imageCache.TryGetValue(bitmap, out var snapshot))
            {
                var converted = bitmap.Format == PixelFormats.Pbgra32 ? bitmap :
                    new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);
                var stride = checked(converted.PixelWidth * 4);
                var bytes = new byte[checked(stride * converted.PixelHeight)];
                converted.CopyPixels(bytes, stride, 0);
                snapshot = new WriterPaginationImagePixels(converted.PixelWidth,
                    converted.PixelHeight, converted.DpiX, converted.DpiY, bytes.ToImmutableArray());
                if (bitmap.IsFrozen)
                    imageCache[bitmap] = snapshot;
            }
            pixels.Add(snapshot);
        }
        foreach (var bitmap in imageCache.Keys.Where(bitmap => !retained.Contains(bitmap)).ToArray())
            imageCache.Remove(bitmap);
        return new WriterPaginationContentSnapshot(xml.ToString(SaveOptions.DisableFormatting),
            document.ContentStart.GetOffsetToPosition(document.ContentEnd), pixels.ToImmutable(),
            EnumerateElements(document).OfType<Run>().Select(run => run.Text).ToImmutableArray());
    }

    private static void ReplaceInlineContent(XElement node, InlineCollection inlines)
    {
        node.Nodes().Where(child => child is not XElement element ||
            !element.Name.LocalName.Contains('.')).Remove();
        foreach (var inline in inlines)
        {
            var child = XElement.Parse(XamlWriter.Save(inline), LoadOptions.PreserveWhitespace);
            if (inline is Run run)
            {
                child.Nodes().OfType<XText>().Remove();
                child.Elements(child.Name.Namespace + "Run.Text").Remove();
                // Restore immutable strings after parsing: WPF's XAML reader
                // normalizes CR/LF even in escaped attribute values.
                child.SetAttributeValue("Text", string.Empty);
            }
            else if (inline is Span span)
                ReplaceInlineContent(child, span.Inlines);
            node.Add(child);
        }
    }

    internal FlowDocument CreateDocument()
    {
        FlowDocument document;
        try { document = (FlowDocument)XamlReader.Parse(Xaml); }
        catch (XamlParseException exception)
        {
            throw new WriterPaginationSnapshotException(exception);
        }
        var runs = EnumerateElements(document).OfType<Run>().ToArray();
        if (runs.Length != RunTexts.Length)
            throw new WriterPaginationSnapshotException();
        for (var index = 0; index < runs.Length; index++)
            runs[index].Text = RunTexts[index];
        var images = EnumerateImages(document).ToArray();
        if (images.Length != Images.Length ||
            document.ContentStart.GetOffsetToPosition(document.ContentEnd) != SymbolCount)
            throw new WriterPaginationSnapshotException();
        for (var index = 0; index < images.Length; index++)
        {
            if (Images[index] is not { } pixels)
                continue;
            var bitmap = BitmapSource.Create(pixels.Width, pixels.Height, pixels.DpiX, pixels.DpiY,
                PixelFormats.Pbgra32, null, pixels.Bytes.ToArray(), checked(pixels.Width * 4));
            bitmap.Freeze();
            images[index].Source = bitmap;
        }
        return document;
    }

    private static IEnumerable<Image> EnumerateImages(FlowDocument document)
        => EnumerateElements(document).OfType<InlineUIContainer>()
            .Select(container => container.Child).OfType<Image>();

    private static IEnumerable<TextElement> EnumerateElements(FlowDocument document)
    {
        for (var position = document.ContentStart; position is not null &&
             position.CompareTo(document.ContentEnd) < 0;
             position = position.GetNextContextPosition(LogicalDirection.Forward))
            if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart &&
                position.GetAdjacentElement(LogicalDirection.Forward) is TextElement element)
                yield return element;
    }
}

internal sealed record WriterPaginationImagePixels(int Width, int Height, double DpiX,
    double DpiY, ImmutableArray<byte> Bytes);

internal sealed class WriterPaginationSnapshotException(Exception? inner = null)
    : Exception("The trusted pagination snapshot could not preserve document positions.", inner);
