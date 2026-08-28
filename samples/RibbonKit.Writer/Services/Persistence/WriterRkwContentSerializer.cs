using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using RibbonKit.Writer.Editing;

namespace RibbonKit.Writer.Services.Persistence;

/// <summary>
/// Writes WPF XamlPackage content but reads it through a strict data-only allowlist. Untrusted XAML is
/// never passed to XamlReader or TextRange.Load because those APIs instantiate arbitrary object graphs.
/// </summary>
internal static partial class WriterRkwContentSerializer
{
    internal const int MaximumPackageBytes = 64 * 1024 * 1024;
    internal const int MaximumXamlBytes = 4 * 1024 * 1024;
    private const int MaximumXmlDepth = 128;
    private const int MaximumXmlElements = 100_000;
    private const int MaximumAttributesPerElement = 96;
    private const int MaximumImageCount = 512;
    private const int MaximumImageBytes = 16 * 1024 * 1024;
    private const long MaximumImagePixels = 32 * 1024 * 1024;
    private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string EntryRelationshipType = "http://schemas.microsoft.com/wpf/2005/10/xaml/entry";
    private const string ComponentRelationshipType = "http://schemas.microsoft.com/wpf/2005/10/xaml/component";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private static readonly HashSet<string> RequiredParts = new(StringComparer.Ordinal)
    {
        "Xaml/Document.xaml",
        "_rels/.rels",
        "[Content_Types].xml"
    };

    private static readonly HashSet<string> AllowedElements = new(StringComparer.Ordinal)
    {
        "Section", "Paragraph", "List", "ListItem",
        "Run", "Span", "Bold", "Italic", "Underline", "LineBreak",
        "Hyperlink", "InlineUIContainer", "Image", "Image.Source", "BitmapImage",
        "Run.TextDecorations", "Span.TextDecorations", "Bold.TextDecorations",
        "Italic.TextDecorations", "Underline.TextDecorations", "Hyperlink.TextDecorations",
        "InlineUIContainer.TextDecorations", "TextDecorationCollection", "TextDecoration"
    };

    private static readonly IReadOnlyDictionary<string, string> ImageContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["png"] = "image/png",
            ["jpg"] = "image/jpeg",
            ["jpeg"] = "image/jpeg",
            ["gif"] = "image/gif",
            ["bmp"] = "image/bmp"
        };

    internal static byte[] Save(FlowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var stream = new MemoryStream();
        new TextRange(document.ContentStart, document.ContentEnd)
            .Save(stream, DataFormats.XamlPackage);
        if (stream.Length > MaximumPackageBytes)
            throw new InvalidDataException("The document content exceeds the native package limit.");
        return stream.ToArray();
    }

    internal static FlowDocument Load(byte[] packageBytes)
    {
        ArgumentNullException.ThrowIfNull(packageBytes);
        if (packageBytes.Length is 0 or > MaximumPackageBytes)
            throw new InvalidDataException("The native content package has an invalid size.");

        try
        {
            using var packageStream = new MemoryStream(packageBytes, writable: false);
            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: false);
            var entries = ValidateEntries(archive);
            ValidateRelationships(ReadEntry(entries["_rels/.rels"], 16 * 1024));
            var imageParts = entries
                .Where(pair => TryGetImagePart(pair.Key, out _))
                .ToDictionary(pair => pair.Key, pair => ReadEntry(pair.Value, MaximumImageBytes),
                    StringComparer.Ordinal);
            foreach (var imagePart in imageParts)
            {
                if (!TryGetImagePart(imagePart.Key, out var extension)
                    || !WriterImageCodecValidation.IsAllowedSignature(imagePart.Value, extension))
                    throw new InvalidDataException("A native image has an unsupported codec signature.");
            }
            ValidateDocumentRelationships(entries, imageParts.Keys);
            ValidateContentTypes(ReadEntry(entries["[Content_Types].xml"], 16 * 1024),
                imageParts.Keys);
            var xaml = ReadEntry(entries["Xaml/Document.xaml"], MaximumXamlBytes);
            return BuildDocument(ParseAndValidateXaml(xaml), imageParts);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or XmlException
                                         or DecoderFallbackException or ArgumentException or FileFormatException
                                         or InvalidOperationException or NotSupportedException)
        {
            throw new InvalidDataException("The native content package is corrupt or unsafe.", exception);
        }
    }

    private static Dictionary<string, ZipArchiveEntry> ValidateEntries(ZipArchive archive)
    {
        if (archive.Entries.Count < RequiredParts.Count
            || archive.Entries.Count > RequiredParts.Count + MaximumImageCount + 1)
            throw new InvalidDataException("The native content package contains an invalid number of parts.");

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        var collisionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalLength = 0;
        foreach (var entry in archive.Entries)
        {
            if ((!RequiredParts.Contains(entry.FullName)
                    && entry.FullName != "Xaml/_rels/Document.xaml.rels"
                    && !TryGetImagePart(entry.FullName, out _))
                || !collisionNames.Add(entry.FullName)
                || !entries.TryAdd(entry.FullName, entry))
                throw new InvalidDataException("The native content package contains an invalid or duplicate part.");
            var maximumPartBytes = TryGetImagePart(entry.FullName, out _)
                ? MaximumImageBytes
                : MaximumPackageBytes;
            if (entry.Length < 0 || entry.Length > maximumPartBytes)
                throw new InvalidDataException("A native content part exceeds its size limit.");
            totalLength = checked(totalLength + entry.Length);
            if (totalLength > MaximumPackageBytes)
                throw new InvalidDataException("The native content package exceeds its expanded size limit.");
        }

        if (!RequiredParts.IsSubsetOf(entries.Keys))
            throw new InvalidDataException("The native content package is missing a required part.");
        return entries;
    }

    private static byte[] ReadEntry(ZipArchiveEntry entry, int maximumBytes)
    {
        if (entry.Length < 0 || entry.Length > maximumBytes)
            throw new InvalidDataException($"Package part '{entry.FullName}' exceeds its size limit.");

        using var source = entry.Open();
        using var destination = new MemoryStream((int)entry.Length);
        var buffer = new byte[81920];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (destination.Length + read > maximumBytes)
                throw new InvalidDataException($"Package part '{entry.FullName}' exceeds its size limit.");
            destination.Write(buffer, 0, read);
        }
        return destination.ToArray();
    }

    private static bool TryGetImagePart(string name, out string extension)
    {
        extension = string.Empty;
        if (!name.StartsWith("Xaml/Image", StringComparison.Ordinal)
            || name.Contains('\\') || name.Contains("..", StringComparison.Ordinal))
            return false;

        var fileName = name[5..];
        var dot = fileName.IndexOf('.');
        if (dot <= "Image".Length || dot == fileName.Length - 1
            || fileName.IndexOf('.', dot + 1) >= 0)
            return false;
        var ordinalText = fileName["Image".Length..dot];
        if (!int.TryParse(ordinalText, NumberStyles.None, CultureInfo.InvariantCulture, out var ordinal)
            || ordinal is < 1 or > MaximumImageCount)
            return false;
        extension = fileName[(dot + 1)..].ToLowerInvariant();
        return ImageContentTypes.ContainsKey(extension);
    }

    private static void ValidateRelationships(byte[] bytes)
    {
        var document = ParseXml(bytes, 16 * 1024);
        XNamespace ns = RelationshipsNamespace;
        if (document.Root?.Name != ns + "Relationships")
            throw new InvalidDataException("The native content relationship part is invalid.");
        var relationships = document.Root.Elements().ToArray();
        if (relationships.Length != 1 || relationships[0].Name != ns + "Relationship")
            throw new InvalidDataException("The native content package must have exactly one entry relationship.");

        var relationship = relationships[0];
        var allowedAttributes = new HashSet<string>(StringComparer.Ordinal) { "Type", "Target", "Id" };
        if (relationship.Attributes().Any(attribute => attribute.IsNamespaceDeclaration
                || !allowedAttributes.Contains(attribute.Name.LocalName)
                || attribute.Name.Namespace != XNamespace.None)
            || relationship.Attribute("Type")?.Value != EntryRelationshipType
            || relationship.Attribute("Target")?.Value != "/Xaml/Document.xaml"
            || !IsSafeRelationshipId(relationship.Attribute("Id")?.Value))
            throw new InvalidDataException("The native content entry relationship is unsafe or unsupported.");
    }

    private static void ValidateDocumentRelationships(
        IReadOnlyDictionary<string, ZipArchiveEntry> entries,
        IReadOnlyCollection<string> imagePartNames)
    {
        var hasRelationships = entries.TryGetValue("Xaml/_rels/Document.xaml.rels", out var relationshipEntry);
        if (imagePartNames.Count == 0)
        {
            if (hasRelationships)
                throw new InvalidDataException("The native content package has orphaned image relationships.");
            return;
        }
        if (!hasRelationships)
            throw new InvalidDataException("The native content package is missing image relationships.");

        var document = ParseXml(ReadEntry(relationshipEntry!, 64 * 1024), 64 * 1024);
        XNamespace ns = RelationshipsNamespace;
        if (document.Root?.Name != ns + "Relationships")
            throw new InvalidDataException("The native image relationship part is invalid.");

        var relationships = document.Root.Elements().ToArray();
        if (relationships.Length != imagePartNames.Count
            || relationships.Any(element => element.Name != ns + "Relationship"))
            throw new InvalidDataException("The native image relationship part is incomplete.");

        var expected = imagePartNames.ToHashSet(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var relationship in relationships)
        {
            var allowedAttributes = new HashSet<string>(StringComparer.Ordinal) { "Type", "Target", "Id" };
            if (relationship.Attributes().Any(attribute => attribute.IsNamespaceDeclaration
                    || !allowedAttributes.Contains(attribute.Name.LocalName)
                    || attribute.Name.Namespace != XNamespace.None))
                throw new InvalidDataException("The native image relationship part contains unsupported attributes.");

            var target = relationship.Attribute("Target")?.Value;
            var id = relationship.Attribute("Id")?.Value;
            if (relationship.Attribute("Type")?.Value != ComponentRelationshipType
                || !IsSafeRelationshipId(id) || !ids.Add(id!)
                || target is null || !target.StartsWith("/Xaml/", StringComparison.Ordinal)
                || target.Contains("\\", StringComparison.Ordinal)
                || target.Contains("..", StringComparison.Ordinal)
                || !expected.Remove(target[1..]))
                throw new InvalidDataException("The native image relationship part is unsafe.");
        }
        if (expected.Count != 0)
            throw new InvalidDataException("The native image relationship part has unreferenced images.");
    }

    private static void ValidateContentTypes(byte[] bytes, IReadOnlyCollection<string> imagePartNames)
    {
        var document = ParseXml(bytes, 16 * 1024);
        XNamespace ns = ContentTypesNamespace;
        if (document.Root?.Name != ns + "Types")
            throw new InvalidDataException("The native content type part is invalid.");

        var defaults = document.Root.Elements().ToArray();
        if (defaults.Length < 2 || defaults.Any(element => element.Name != ns + "Default"))
            throw new InvalidDataException("The native content package declares unsupported content types.");
        var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in defaults)
        {
            if (element.Attributes().Count() != 2)
                throw new InvalidDataException("The native content type part contains unexpected data.");
            var extension = element.Attribute("Extension")?.Value;
            var contentType = element.Attribute("ContentType")?.Value;
            if (extension is null or { Length: 0 } || contentType is null or { Length: 0 }
                || extension.Length > 16 || contentType.Length > 128
                || !pairs.TryAdd(extension, contentType))
                throw new InvalidDataException("The native content type part contains duplicate or invalid data.");
        }
        if (!pairs.TryGetValue("xaml", out var xamlType)
            || xamlType != "application/vnd.ms-wpf.xaml+xml"
            || !pairs.TryGetValue("rels", out var relsType)
            || relsType != "application/vnd.openxmlformats-package.relationships+xml")
            throw new InvalidDataException("The native content package declares unsupported content types.");

        var actualImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var imagePartName in imagePartNames)
        {
            if (!TryGetImagePart(imagePartName, out var extension))
                throw new InvalidDataException("The native content package contains an invalid image part.");
            actualImageExtensions.Add(extension);
        }

        var declaredImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in pairs.Keys)
        {
            if (extension.Equals("xaml", StringComparison.OrdinalIgnoreCase)
                || extension.Equals("rels", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!ImageContentTypes.TryGetValue(extension, out var expectedType)
                || pairs[extension] != expectedType)
                throw new InvalidDataException("The native content package declares an invalid image type.");
            declaredImageExtensions.Add(extension);
        }
        if (!declaredImageExtensions.SetEquals(actualImageExtensions))
            throw new InvalidDataException("The native content package image declarations do not match its image parts.");
    }

    private static XDocument ParseAndValidateXaml(byte[] bytes)
    {
        var document = ParseXml(bytes, MaximumXamlBytes);
        if (document.Root is null || document.Root.Name != XName.Get("Section", PresentationNamespace))
            throw new InvalidDataException("The native document content must begin with a WPF Section.");

        var stack = new Stack<(XElement Element, int Depth)>();
        stack.Push((document.Root, 1));
        var elementCount = 0;
        long characterCount = 0;
        while (stack.Count > 0)
        {
            var (element, depth) = stack.Pop();
            if (++elementCount > MaximumXmlElements || depth > MaximumXmlDepth)
                throw new InvalidDataException("The native document XML exceeds its complexity limit.");
            ValidateElement(element);
            foreach (var node in element.Nodes())
            {
                switch (node)
                {
                    case XElement child:
                        stack.Push((child, depth + 1));
                        break;
                    case XText text:
                        characterCount = checked(characterCount + text.Value.Length);
                        if (characterCount > MaximumXamlBytes)
                            throw new InvalidDataException("The native document text exceeds its size limit.");
                        break;
                    default:
                        throw new InvalidDataException("The native document XML contains unsupported nodes.");
                }
            }
        }
        return document;
    }

    private static XDocument ParseXml(byte[] bytes, int maximumCharacters)
    {
        string xml;
        try
        {
            xml = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException("Native package XML must be valid UTF-8.", exception);
        }
        if (xml.Length > 0 && xml[0] == '\uFEFF')
            xml = xml[1..];
        if (xml.Length > maximumCharacters)
            throw new InvalidDataException("Native package XML exceeds its size limit.");

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = maximumCharacters,
            IgnoreComments = false,
            IgnoreProcessingInstructions = false
        };
        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, settings);
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }

    private static void ValidateElement(XElement element)
    {
        if (element.Name.NamespaceName != PresentationNamespace || !AllowedElements.Contains(element.Name.LocalName))
            throw new InvalidDataException($"Unsupported native document element '{element.Name}'.");
        if (element.Attributes().Count() > MaximumAttributesPerElement)
            throw new InvalidDataException("A native document element has too many attributes.");

        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                if (element.Name.LocalName is not ("Section" or "TextDecorationCollection")
                    || attribute.Name.LocalName != "xmlns" || attribute.Value != PresentationNamespace)
                    throw new InvalidDataException("The native document contains an unsupported XML namespace.");
                continue;
            }
            if (attribute.Name.Namespace == XNamespace.Xml)
            {
                if (attribute.Name.LocalName is not ("space" or "lang")
                    || !AllowsTextAttributes(element.Name.LocalName))
                    throw new InvalidDataException("The native document contains an unsupported XML attribute.");
                continue;
            }
            if (attribute.Name.Namespace != XNamespace.None)
                throw new InvalidDataException("The native document contains an unsupported namespaced attribute.");
            if (!IsAllowedAttribute(element.Name.LocalName, attribute.Name.LocalName))
                throw new InvalidDataException($"Unsupported native document attribute '{attribute.Name.LocalName}'.");
            if (attribute.Name.LocalName == "NavigateUri")
            {
                if (!TryParseSafeUri(attribute.Value, out _))
                    throw new InvalidDataException("The native document contains an unsafe hyperlink URI.");
                continue;
            }
            if (attribute.Name.LocalName == "UriSource")
            {
                if (!TryNormalizeImageReference(attribute.Value, out _))
                    throw new InvalidDataException("The native document contains an unsafe image URI.");
                continue;
            }
            if (attribute.Value.Length > 512 || attribute.Value.Contains('{') || attribute.Value.Contains('}')
                || attribute.Value.Contains("://", StringComparison.Ordinal)
                || attribute.Value.Contains('\\'))
                throw new InvalidDataException("The native document contains an unsafe attribute value.");
        }
        ValidateTextDecorationShape(element);
    }

    private static FlowDocument BuildDocument(XDocument source,
        IReadOnlyDictionary<string, byte[]> imageParts)
    {
        var document = new FlowDocument();
        var usedImageParts = new HashSet<string>(StringComparer.Ordinal);
        ApplyTextProperties(source.Root!, document);
        ApplyBlockProperties(source.Root!, document);
        foreach (var block in BuildBlocks(source.Root!, imageParts, usedImageParts))
            document.Blocks.Add(block);
        if (!usedImageParts.SetEquals(imageParts.Keys))
            throw new InvalidDataException("The native content package contains unreferenced image data.");
        return document;
    }

    private static IEnumerable<Block> BuildBlocks(XElement parent,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        foreach (var node in parent.Nodes())
        {
            if (node is XText text)
            {
                if (!string.IsNullOrWhiteSpace(text.Value))
                    throw new InvalidDataException("Text must be contained by a paragraph or inline element.");
                continue;
            }
            if (node is not XElement element)
                throw new InvalidDataException("The native document contains unsupported block content.");
            yield return element.Name.LocalName switch
            {
                "Paragraph" => BuildParagraph(element, imageParts, usedImageParts),
                "List" => BuildList(element, imageParts, usedImageParts),
                "Section" => BuildSection(element, imageParts, usedImageParts),
                _ => throw new InvalidDataException($"Element '{element.Name.LocalName}' is not valid block content.")
            };
        }
    }

    private static Paragraph BuildParagraph(XElement element,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        var paragraph = new Paragraph();
        ApplyTextProperties(element, paragraph);
        ApplyBlockProperties(element, paragraph);
        ApplyParagraphProperties(element, paragraph);
        foreach (var inline in BuildInlines(element, imageParts, usedImageParts))
            paragraph.Inlines.Add(inline);
        return paragraph;
    }

    private static Section BuildSection(XElement element,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        var section = new Section();
        ApplyTextProperties(element, section);
        ApplyBlockProperties(element, section);
        foreach (var block in BuildBlocks(element, imageParts, usedImageParts))
            section.Blocks.Add(block);
        return section;
    }

    private static List BuildList(XElement element,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        var list = new List();
        ApplyTextProperties(element, list);
        ApplyBlockProperties(element, list);
        if (TryAttribute(element, "MarkerStyle", out var markerStyle))
            list.MarkerStyle = ParseEnum<TextMarkerStyle>(markerStyle, "list marker style");
        if (TryAttribute(element, "StartIndex", out var startIndex))
            list.StartIndex = ParseInt(startIndex, 1, int.MaxValue, "list start index");
        if (TryAttribute(element, "MarkerOffset", out var markerOffset) && markerOffset != "Auto")
            list.MarkerOffset = ParseDouble(markerOffset, -10000, 10000, "list marker offset");

        foreach (var child in element.Elements())
        {
            if (child.Name.LocalName != "ListItem")
                throw new InvalidDataException("A native list contains unsupported content.");
            var item = new ListItem();
            ApplyTextProperties(child, item);
            foreach (var block in BuildBlocks(child, imageParts, usedImageParts))
                item.Blocks.Add(block);
            list.ListItems.Add(item);
        }
        if (element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            throw new InvalidDataException("A native list contains text outside a list item.");
        return list;
    }

    private static IEnumerable<Inline> BuildInlines(XElement parent,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        foreach (var node in parent.Nodes())
        {
            if (node is XText text)
            {
                if (text.Value.Length > 0)
                    yield return new Run(text.Value);
                continue;
            }
            if (node is not XElement element)
                throw new InvalidDataException("The native document contains unsupported inline content.");
            if (element.Name.LocalName.EndsWith(".TextDecorations", StringComparison.Ordinal))
                continue;

            Inline inline = element.Name.LocalName switch
            {
                "Run" => BuildRun(element),
                "Span" => BuildSpan(element, new Span(), imageParts, usedImageParts),
                "Bold" => BuildSpan(element, new Bold(), imageParts, usedImageParts),
                "Italic" => BuildSpan(element, new Italic(), imageParts, usedImageParts),
                "Underline" => BuildSpan(element, new Underline(), imageParts, usedImageParts),
                "LineBreak" => BuildLineBreak(element),
                "Hyperlink" => BuildHyperlink(element, imageParts, usedImageParts),
                "InlineUIContainer" => BuildInlineUiContainer(element, imageParts, usedImageParts),
                _ => throw new InvalidDataException($"Element '{element.Name.LocalName}' is not valid inline content.")
            };
            yield return inline;
        }
    }

    private static Run BuildRun(XElement element)
    {
        if (element.Elements().Any(child =>
                child.Name.LocalName != "Run.TextDecorations"))
            throw new InvalidDataException("A native Run cannot contain child elements.");
        var run = new Run(string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value)));
        ApplyTextProperties(element, run);
        ApplyInlineProperties(element, run);
        return run;
    }

    private static Span BuildSpan(XElement element, Span span,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        ApplyTextProperties(element, span);
        ApplyInlineProperties(element, span);
        foreach (var inline in BuildInlines(element, imageParts, usedImageParts))
            span.Inlines.Add(inline);
        return span;
    }

    private static LineBreak BuildLineBreak(XElement element)
    {
        if (element.Nodes().Any())
            throw new InvalidDataException("A native LineBreak cannot contain content.");
        var lineBreak = new LineBreak();
        ApplyTextProperties(element, lineBreak);
        return lineBreak;
    }

    private static Hyperlink BuildHyperlink(XElement element,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        if (!TryAttribute(element, "NavigateUri", out var value)
            || !TryParseSafeUri(value, out var uri)
            || element.Descendants().Any(child => child.Name.LocalName == "Hyperlink"))
            throw new InvalidDataException("The native document contains an invalid hyperlink URI.");

        var hyperlink = new Hyperlink { NavigateUri = uri };
        ApplyTextProperties(element, hyperlink);
        ApplyInlineProperties(element, hyperlink);
        foreach (var inline in BuildInlines(element, imageParts, usedImageParts))
            hyperlink.Inlines.Add(inline);
        return hyperlink;
    }

    private static InlineUIContainer BuildInlineUiContainer(XElement element,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        if (element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            throw new InvalidDataException("An inline UI container contains unsupported text.");
        var children = element.Elements()
            .Where(child => child.Name.LocalName != "InlineUIContainer.TextDecorations")
            .ToArray();
        if (children.Length != 1 || children[0].Name.LocalName != "Image")
            throw new InvalidDataException("Only one inert image is supported in an inline UI container.");

        var container = new InlineUIContainer(BuildImage(children[0], imageParts, usedImageParts));
        ApplyInlineProperties(element, container);
        return container;
    }

    private static Image BuildImage(XElement element,
        IReadOnlyDictionary<string, byte[]> imageParts, ISet<string> usedImageParts)
    {
        if (element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            throw new InvalidDataException("A native image contains unsupported text.");
        var sourceProperties = element.Elements().ToArray();
        if (sourceProperties.Length != 1 || sourceProperties[0].Name.LocalName != "Image.Source"
            || sourceProperties[0].Nodes().OfType<XText>()
                .Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            throw new InvalidDataException("A native image must have one packaged source.");
        var bitmapElement = sourceProperties[0].Elements().ToArray();
        if (bitmapElement.Length != 1 || bitmapElement[0].Name.LocalName != "BitmapImage"
            || bitmapElement[0].Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            throw new InvalidDataException("A native image has an unsupported source graph.");

        var bitmapSource = bitmapElement[0];
        if (!TryAttribute(bitmapSource, "UriSource", out var uriSource)
            || !TryNormalizeImageReference(uriSource, out var partName)
            || !imageParts.TryGetValue(partName, out var bytes))
            throw new InvalidDataException("A native image source is missing or unsafe.");
        if (TryAttribute(bitmapSource, "CacheOption", out var cacheOption) && cacheOption != "OnLoad")
            throw new InvalidDataException("Native images must use an on-load cache option.");
        if (TryAttribute(bitmapSource, "CreateOptions", out var createOptions)
            && createOptions != "PreservePixelFormat")
            throw new InvalidDataException("Native images must preserve their decoded pixel format.");
        if (!TryGetImagePart(partName, out var extension)
            || !WriterImageCodecValidation.TryReadDimensions(bytes, out var width, out var height)
            || !WriterImageCodecValidation.IsAllowedSignature(bytes, extension)
            || !WriterImageCodecValidation.IsWithinLimits(width, height,
                MaximumImagePixels, 8192))
            throw new InvalidDataException("A native image exceeds its decoded size limit.");
        usedImageParts.Add(partName);

        BitmapImage bitmap;
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            if (!WriterImageCodecValidation.IsWithinLimits(bitmap.PixelWidth, bitmap.PixelHeight,
                MaximumImagePixels, 8192))
                throw new InvalidDataException("A native image exceeds its decoded size limit.");
            bitmap.Freeze();
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or FileFormatException
                                             or IOException or InvalidOperationException
                                             or NotSupportedException or OverflowException
                                             or OutOfMemoryException
                                             or System.Runtime.InteropServices.ExternalException)
        {
            throw new InvalidDataException("A native image cannot be decoded safely.", exception);
        }

        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Uniform,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        ApplyImageProperty(element, image, "Width", minimum: 0.01, maximum: 8192);
        ApplyImageProperty(element, image, "Height", minimum: 0.01, maximum: 8192);
        ApplyImageProperty(element, image, "MaxWidth", minimum: 0, maximum: 8192);
        ApplyImageProperty(element, image, "MaxHeight", minimum: 0, maximum: 8192);
        ApplyImageProperty(element, image, "MinWidth", minimum: 0, maximum: 8192);
        ApplyImageProperty(element, image, "MinHeight", minimum: 0, maximum: 8192);
        if (TryAttribute(element, "Stretch", out var stretch))
            image.Stretch = ParseEnum<Stretch>(stretch, "image stretch");
        if (TryAttribute(element, "SnapsToDevicePixels", out var snaps))
            image.SnapsToDevicePixels = ParseBoolean(snaps, "SnapsToDevicePixels");
        return image;
    }

    private static void ApplyImageProperty(XElement element, Image image, string name,
        double minimum, double maximum)
    {
        if (!TryAttribute(element, name, out var value))
            return;
        var description = name switch
        {
            "Width" => "image width",
            "Height" => "image height",
            "MaxWidth" => "image maximum width",
            "MaxHeight" => "image maximum height",
            "MinWidth" => "image minimum width",
            "MinHeight" => "image minimum height",
            _ => "image dimension"
        };
        var parsed = ParseDouble(value, minimum, maximum, description);
        switch (name)
        {
            case "Width": image.Width = parsed; break;
            case "Height": image.Height = parsed; break;
            case "MaxWidth": image.MaxWidth = parsed; break;
            case "MaxHeight": image.MaxHeight = parsed; break;
            case "MinWidth": image.MinWidth = parsed; break;
            case "MinHeight": image.MinHeight = parsed; break;
        }
    }

    private static bool IsAllowedAttribute(string elementName, string attributeName)
    {
        if (AllowsTextAttributes(elementName)
            && (attributeName.StartsWith("Typography.", StringComparison.Ordinal)
                || attributeName.StartsWith("NumberSubstitution.", StringComparison.Ordinal)))
            return true;

        return elementName switch
        {
            "Section" => attributeName is "FontFamily" or "FontStyle" or "FontWeight"
                or "FontStretch" or "FontSize" or "Foreground" or "Background"
                or "FlowDirection" or "TextAlignment" or "LineHeight"
                or "LineStackingStrategy" or "IsHyphenationEnabled" or "Margin" or "Padding",
            "Paragraph" => attributeName is "FontFamily" or "FontStyle" or "FontWeight"
                or "FontStretch" or "FontSize" or "Foreground" or "Background"
                or "FlowDirection" or "TextAlignment" or "LineHeight"
                or "LineStackingStrategy" or "IsHyphenationEnabled" or "Margin" or "Padding" or "TextIndent"
                or "KeepTogether" or "KeepWithNext" or "MinOrphanLines" or "MinWidowLines",
            "List" => attributeName is "FontFamily" or "FontStyle" or "FontWeight"
                or "FontStretch" or "FontSize" or "Foreground" or "Background"
                or "FlowDirection" or "TextAlignment" or "LineHeight"
                or "LineStackingStrategy" or "IsHyphenationEnabled" or "Margin" or "Padding" or "MarkerStyle"
                or "MarkerOffset" or "StartIndex",
            "ListItem" => attributeName is "FontFamily" or "FontStyle" or "FontWeight"
                or "FontStretch" or "FontSize" or "Foreground" or "Background"
                or "FlowDirection",
            "Run" or "Span" or "Bold" or "Italic" or "Underline" or "LineBreak" =>
                attributeName is "FontFamily" or "FontStyle" or "FontWeight"
                or "FontStretch" or "FontSize" or "Foreground" or "Background"
                or "FlowDirection" or "BaselineAlignment" or "TextDecorations",
            "Hyperlink" => attributeName is "FontFamily" or "FontStyle" or "FontWeight"
                or "FontStretch" or "FontSize" or "Foreground" or "Background"
                or "FlowDirection" or "BaselineAlignment" or "TextDecorations"
                or "NavigateUri",
            "InlineUIContainer" => attributeName is "BaselineAlignment" or "TextDecorations",
            "Run.TextDecorations" or "Span.TextDecorations" or "Bold.TextDecorations"
                or "Italic.TextDecorations" or "Underline.TextDecorations"
                or "Hyperlink.TextDecorations" or "InlineUIContainer.TextDecorations"
                or "TextDecorationCollection" => false,
            "TextDecoration" => attributeName is "Location",
            "Image" => attributeName is "Width" or "Height" or "MaxWidth" or "MaxHeight"
                or "MinWidth" or "MinHeight" or "Stretch" or "SnapsToDevicePixels",
            "Image.Source" => false,
            "BitmapImage" => attributeName is "UriSource" or "CacheOption" or "CreateOptions",
            _ => false
        };
    }

    private static bool AllowsTextAttributes(string elementName) => elementName is
        "Section" or "Paragraph" or "List" or "ListItem" or "Run" or "Span"
        or "Bold" or "Italic" or "Underline" or "LineBreak" or "Hyperlink";

    private static void ApplyTextProperties(XElement source, DependencyObject target)
    {
        if (TryAttribute(source, "FontFamily", out var fontFamily))
        {
            if (fontFamily.Length is 0 or > 128 || fontFamily.IndexOfAny(['/', '\\', ':', '#']) >= 0)
                throw new InvalidDataException("The native document contains an unsafe font family.");
            target.SetValue(TextElement.FontFamilyProperty, new FontFamily(fontFamily));
        }
        if (TryAttribute(source, "FontStyle", out var fontStyle))
            target.SetValue(TextElement.FontStyleProperty, ParseFontStyle(fontStyle));
        if (TryAttribute(source, "FontWeight", out var fontWeight))
            target.SetValue(TextElement.FontWeightProperty, ParseFontWeight(fontWeight));
        if (TryAttribute(source, "FontStretch", out var fontStretch))
            target.SetValue(TextElement.FontStretchProperty, ParseFontStretch(fontStretch));
        if (TryAttribute(source, "FontSize", out var fontSize))
            target.SetValue(TextElement.FontSizeProperty, ParseDouble(fontSize, 1, 512, "font size"));
        if (TryAttribute(source, "Foreground", out var foreground))
            target.SetValue(TextElement.ForegroundProperty, ParseBrush(foreground));
        if (TryAttribute(source, "Background", out var background))
            target.SetValue(TextElement.BackgroundProperty, ParseBrush(background));
        if (TryAttribute(source, "FlowDirection", out var flowDirection))
            target.SetValue(FrameworkElement.FlowDirectionProperty,
                ParseEnum<FlowDirection>(flowDirection, "flow direction"));
        if (source.Attribute(XNamespace.Xml + "lang") is { Value: var language })
            target.SetValue(FrameworkContentElement.LanguageProperty, XmlLanguage.GetLanguage(language));
    }

    private static void ApplyBlockProperties(XElement source, DependencyObject target)
    {
        if (TryAttribute(source, "TextAlignment", out var textAlignment))
            target.SetValue(Block.TextAlignmentProperty,
                ParseEnum<TextAlignment>(textAlignment, "text alignment"));
        if (TryAttribute(source, "LineHeight", out var lineHeight) && lineHeight != "Auto")
            target.SetValue(Block.LineHeightProperty, ParseDouble(lineHeight, 0.1, 10000, "line height"));
        if (TryAttribute(source, "LineStackingStrategy", out var lineStacking))
            target.SetValue(Block.LineStackingStrategyProperty,
                ParseEnum<LineStackingStrategy>(lineStacking, "line stacking strategy"));
        if (TryAttribute(source, "IsHyphenationEnabled", out var hyphenation))
            target.SetValue(Block.IsHyphenationEnabledProperty,
                ParseBoolean(hyphenation, "IsHyphenationEnabled"));
        if (TryAttribute(source, "Margin", out var margin))
            target.SetValue(Block.MarginProperty, ParseThickness(margin, "block margin"));
        if (TryAttribute(source, "Padding", out var padding))
            target.SetValue(Block.PaddingProperty, ParseThickness(padding, "block padding"));
    }

    private static void ApplyParagraphProperties(XElement source, Paragraph paragraph)
    {
        if (TryAttribute(source, "TextIndent", out var textIndent))
            paragraph.TextIndent = ParseDouble(textIndent, -10000, 10000, "text indent");
        if (TryAttribute(source, "KeepTogether", out var keepTogether))
            paragraph.KeepTogether = ParseBoolean(keepTogether, "KeepTogether");
        if (TryAttribute(source, "KeepWithNext", out var keepWithNext))
            paragraph.KeepWithNext = ParseBoolean(keepWithNext, "KeepWithNext");
        if (TryAttribute(source, "MinOrphanLines", out var minOrphanLines))
            paragraph.MinOrphanLines = ParseInt(minOrphanLines, 0, 10000, "minimum orphan lines");
        if (TryAttribute(source, "MinWidowLines", out var minWidowLines))
            paragraph.MinWidowLines = ParseInt(minWidowLines, 0, 10000, "minimum widow lines");
    }

    private static void ApplyInlineProperties(XElement source, Inline inline)
    {
        if (TryAttribute(source, "BaselineAlignment", out var baselineAlignment))
            inline.BaselineAlignment = ParseEnum<BaselineAlignment>(baselineAlignment, "baseline alignment");
        if (TryAttribute(source, "TextDecorations", out var decorations))
            inline.TextDecorations = ParseTextDecorations(decorations);
        var propertyName = source.Name.LocalName + ".TextDecorations";
        var propertyElements = source.Elements()
            .Where(element => element.Name.LocalName == propertyName)
            .ToArray();
        if (propertyElements.Length > 1 ||
            (propertyElements.Length == 1 && TryAttribute(source, "TextDecorations", out _)))
            throw new InvalidDataException("A native inline has duplicate text decorations.");
        if (propertyElements.Length == 1)
            inline.TextDecorations = ParseTextDecorationProperty(propertyElements[0]);
    }

    private static TextDecorationCollection ParseTextDecorationProperty(XElement propertyElement)
    {
        var collection = propertyElement.Elements().Single();
        var result = new TextDecorationCollection();
        foreach (var element in collection.Elements())
        {
            if (!TryAttribute(element, "Location", out var location))
                throw new InvalidDataException("A native text decoration is missing its location.");
            result.Add(location switch
            {
                "Underline" => TextDecorations.Underline[0],
                "Strikethrough" => TextDecorations.Strikethrough[0],
                "OverLine" => TextDecorations.OverLine[0],
                "Baseline" => TextDecorations.Baseline[0],
                _ => throw new InvalidDataException("The native document has unsupported text decorations.")
            });
        }
        return result;
    }

    private static void ValidateTextDecorationShape(XElement element)
    {
        var name = element.Name.LocalName;
        if (name.EndsWith(".TextDecorations", StringComparison.Ordinal))
        {
            var expectedOwner = name[..name.IndexOf('.')];
            var children = element.Elements().ToArray();
            if (element.Parent?.Name.LocalName != expectedOwner || children.Length != 1 ||
                children[0].Name.LocalName != "TextDecorationCollection" ||
                element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                throw new InvalidDataException("A native text-decoration property has an invalid shape.");
            return;
        }
        if (name == "TextDecorationCollection")
        {
            var children = element.Elements().ToArray();
            if (element.Parent?.Name.LocalName.EndsWith(".TextDecorations", StringComparison.Ordinal) != true ||
                children.Length is < 1 or > 4 || children.Any(child => child.Name.LocalName != "TextDecoration") ||
                element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                throw new InvalidDataException("A native text-decoration collection has an invalid shape.");
            return;
        }
        if (name == "TextDecoration" &&
            (element.Parent?.Name.LocalName != "TextDecorationCollection" || element.Elements().Any() ||
             element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)) ||
             !TryAttribute(element, "Location", out var location) ||
             location is not ("Underline" or "Strikethrough" or "OverLine" or "Baseline")))
        {
            throw new InvalidDataException("A native text decoration has an invalid shape.");
        }
    }

    private static bool TryAttribute(XElement element, string name, out string value)
    {
        value = element.Attribute(name)?.Value ?? string.Empty;
        return element.Attribute(name) is not null;
    }

    private static bool TryParseSafeUri(string value, out Uri uri)
        => WriterHyperlinkService.TryParseUri(value, out uri);

    private static bool TryNormalizeImageReference(string value, out string partName)
    {
        partName = string.Empty;
        if (value.Length is 0 or > 128 || !value.StartsWith("./", StringComparison.Ordinal)
            || value.Contains('\\') || value.IndexOf('/', 2) >= 0
            || value.Contains("://", StringComparison.Ordinal)
            || value.Contains("..", StringComparison.Ordinal))
            return false;
        value = value[2..];
        if (!TryGetImagePart("Xaml/" + value, out _))
            return false;
        partName = "Xaml/" + value;
        return true;
    }

    private static bool IsSafeRelationshipId(string? value)
    {
        if (value is null || value.Length is 0 or > 128
            || !(char.IsLetter(value[0]) || value[0] == '_'))
            return false;
        return value.Skip(1).All(static character => char.IsLetterOrDigit(character)
            || character is '_' or '-' or '.');
    }

    private static bool ParseBoolean(string value, string description) => value switch
    {
        "True" => true,
        "False" => false,
        _ => throw new InvalidDataException($"The native document has an invalid {description} value.")
    };

    private static double ParseDouble(string value, double minimum, double maximum, string description)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !double.IsFinite(parsed) || parsed < minimum || parsed > maximum)
            throw new InvalidDataException($"The native document has an invalid {description} value.");
        return parsed;
    }

    private static int ParseInt(string value, int minimum, int maximum, string description)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed < minimum || parsed > maximum)
            throw new InvalidDataException($"The native document has an invalid {description} value.");
        return parsed;
    }

    private static T ParseEnum<T>(string value, string description) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, ignoreCase: false, out var parsed) || !Enum.IsDefined(parsed))
            throw new InvalidDataException($"The native document has an invalid {description} value.");
        return parsed;
    }

    private static FontStyle ParseFontStyle(string value) => value switch
    {
        "Normal" => FontStyles.Normal,
        "Italic" => FontStyles.Italic,
        "Oblique" => FontStyles.Oblique,
        _ => throw new InvalidDataException("The native document has an invalid font style.")
    };

    private static FontWeight ParseFontWeight(string value) => value switch
    {
        "Thin" => FontWeights.Thin,
        "ExtraLight" or "UltraLight" => FontWeights.ExtraLight,
        "Light" => FontWeights.Light,
        "Normal" or "Regular" => FontWeights.Normal,
        "Medium" => FontWeights.Medium,
        "DemiBold" or "SemiBold" => FontWeights.SemiBold,
        "Bold" => FontWeights.Bold,
        "ExtraBold" or "UltraBold" => FontWeights.ExtraBold,
        "Black" or "Heavy" => FontWeights.Black,
        "ExtraBlack" or "UltraBlack" => FontWeights.ExtraBlack,
        _ => throw new InvalidDataException("The native document has an invalid font weight.")
    };

    private static FontStretch ParseFontStretch(string value) => value switch
    {
        "UltraCondensed" => FontStretches.UltraCondensed,
        "ExtraCondensed" => FontStretches.ExtraCondensed,
        "Condensed" => FontStretches.Condensed,
        "SemiCondensed" => FontStretches.SemiCondensed,
        "Normal" => FontStretches.Normal,
        "SemiExpanded" => FontStretches.SemiExpanded,
        "Expanded" => FontStretches.Expanded,
        "ExtraExpanded" => FontStretches.ExtraExpanded,
        "UltraExpanded" => FontStretches.UltraExpanded,
        _ => throw new InvalidDataException("The native document has an invalid font stretch.")
    };

    private static SolidColorBrush ParseBrush(string value)
    {
        if (!SafeColor().IsMatch(value))
            throw new InvalidDataException("The native document contains an unsupported brush.");
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(value)!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("The native document contains an invalid color.", exception);
        }
    }

    private static Thickness ParseThickness(string value, string description)
    {
        var parts = value.Split(',');
        if (parts.Length is not (1 or 2 or 4))
            throw new InvalidDataException($"The native document has an invalid {description} value.");
        var values = parts.Select(part => ParseDouble(part.Trim(), 0, 10000, description)).ToArray();
        return values.Length switch
        {
            1 => new Thickness(values[0]),
            2 => new Thickness(values[0], values[1], values[0], values[1]),
            _ => new Thickness(values[0], values[1], values[2], values[3])
        };
    }

    private static TextDecorationCollection ParseTextDecorations(string value)
    {
        var result = new TextDecorationCollection();
        foreach (var item in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (item)
            {
                case "Underline":
                    result.Add(TextDecorations.Underline[0]);
                    break;
                case "Strikethrough":
                    result.Add(TextDecorations.Strikethrough[0]);
                    break;
                case "OverLine":
                    result.Add(TextDecorations.OverLine[0]);
                    break;
                case "Baseline":
                    result.Add(TextDecorations.Baseline[0]);
                    break;
                default:
                    throw new InvalidDataException("The native document has unsupported text decorations.");
            }
        }
        return result;
    }

    [GeneratedRegex("^(?:#[0-9A-Fa-f]{6}|#[0-9A-Fa-f]{8}|[A-Za-z]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeColor();
}
