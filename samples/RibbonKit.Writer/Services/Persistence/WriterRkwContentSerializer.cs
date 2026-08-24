using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

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
    private const string PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
    private const string EntryRelationshipType = "http://schemas.microsoft.com/wpf/2005/10/xaml/entry";
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
        "Run", "Span", "Bold", "Italic", "Underline", "LineBreak"
    };

    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.Ordinal)
    {
        "FontFamily", "FontStyle", "FontWeight", "FontStretch", "FontSize",
        "Foreground", "Background", "FlowDirection", "TextDecorations", "BaselineAlignment",
        "TextAlignment", "LineHeight", "LineStackingStrategy", "IsHyphenationEnabled",
        "Margin", "Padding", "TextIndent", "KeepTogether", "KeepWithNext",
        "MinOrphanLines", "MinWidowLines", "MarkerStyle", "MarkerOffset", "StartIndex"
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
            ValidateContentTypes(ReadEntry(entries["[Content_Types].xml"], 16 * 1024));
            var xaml = ReadEntry(entries["Xaml/Document.xaml"], MaximumXamlBytes);
            return BuildDocument(ParseAndValidateXaml(xaml));
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or XmlException
                                         or DecoderFallbackException or ArgumentException)
        {
            throw new InvalidDataException("The native content package is corrupt or unsafe.", exception);
        }
    }

    private static Dictionary<string, ZipArchiveEntry> ValidateEntries(ZipArchive archive)
    {
        if (archive.Entries.Count != RequiredParts.Count)
            throw new InvalidDataException("The native content package contains unexpected parts.");

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        var collisionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalLength = 0;
        foreach (var entry in archive.Entries)
        {
            if (!RequiredParts.Contains(entry.FullName) || !collisionNames.Add(entry.FullName)
                || !entries.TryAdd(entry.FullName, entry))
                throw new InvalidDataException("The native content package contains an invalid or duplicate part.");
            if (entry.Length < 0 || entry.Length > MaximumPackageBytes)
                throw new InvalidDataException("A native content part exceeds its size limit.");
            totalLength = checked(totalLength + entry.Length);
            if (totalLength > MaximumPackageBytes)
                throw new InvalidDataException("The native content package exceeds its expanded size limit.");
        }

        if (!RequiredParts.SetEquals(entries.Keys))
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
            || string.IsNullOrWhiteSpace(relationship.Attribute("Id")?.Value))
            throw new InvalidDataException("The native content entry relationship is unsafe or unsupported.");
    }

    private static void ValidateContentTypes(byte[] bytes)
    {
        var document = ParseXml(bytes, 16 * 1024);
        XNamespace ns = ContentTypesNamespace;
        if (document.Root?.Name != ns + "Types")
            throw new InvalidDataException("The native content type part is invalid.");

        var defaults = document.Root.Elements().ToArray();
        if (defaults.Length != 2 || defaults.Any(element => element.Name != ns + "Default"))
            throw new InvalidDataException("The native content package declares unsupported content types.");
        var pairs = defaults.Select(element => (
                Extension: element.Attribute("Extension")?.Value,
                ContentType: element.Attribute("ContentType")?.Value))
            .ToHashSet();
        (string? Extension, string? ContentType)[] requiredPairs =
        [
            ("xaml", "application/vnd.ms-wpf.xaml+xml"),
            ("rels", "application/vnd.openxmlformats-package.relationships+xml")
        ];
        if (!pairs.SetEquals(requiredPairs))
            throw new InvalidDataException("The native content package declares unsupported content types.");
        if (defaults.Any(element => element.Attributes().Count() != 2))
            throw new InvalidDataException("The native content type part contains unexpected data.");
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
                if (attribute.Name.LocalName != "xmlns" || attribute.Value != PresentationNamespace)
                    throw new InvalidDataException("The native document contains an unsupported XML namespace.");
                continue;
            }
            if (attribute.Name.Namespace == XNamespace.Xml)
            {
                if (attribute.Name.LocalName is not ("space" or "lang"))
                    throw new InvalidDataException("The native document contains an unsupported XML attribute.");
                continue;
            }
            if (attribute.Name.Namespace != XNamespace.None)
                throw new InvalidDataException("The native document contains an unsupported namespaced attribute.");
            if (!AllowedAttributes.Contains(attribute.Name.LocalName)
                && !attribute.Name.LocalName.StartsWith("Typography.", StringComparison.Ordinal)
                && !attribute.Name.LocalName.StartsWith("NumberSubstitution.", StringComparison.Ordinal))
                throw new InvalidDataException($"Unsupported native document attribute '{attribute.Name.LocalName}'.");
            if (attribute.Value.Length > 512 || attribute.Value.Contains('{') || attribute.Value.Contains('}')
                || attribute.Value.Contains("://", StringComparison.Ordinal)
                || attribute.Value.Contains('\\'))
                throw new InvalidDataException("The native document contains an unsafe attribute value.");
        }
    }

    private static FlowDocument BuildDocument(XDocument source)
    {
        var document = new FlowDocument();
        ApplyTextProperties(source.Root!, document);
        ApplyBlockProperties(source.Root!, document);
        foreach (var block in BuildBlocks(source.Root!))
            document.Blocks.Add(block);
        return document;
    }

    private static IEnumerable<Block> BuildBlocks(XElement parent)
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
                "Paragraph" => BuildParagraph(element),
                "List" => BuildList(element),
                "Section" => BuildSection(element),
                _ => throw new InvalidDataException($"Element '{element.Name.LocalName}' is not valid block content.")
            };
        }
    }

    private static Paragraph BuildParagraph(XElement element)
    {
        var paragraph = new Paragraph();
        ApplyTextProperties(element, paragraph);
        ApplyBlockProperties(element, paragraph);
        ApplyParagraphProperties(element, paragraph);
        foreach (var inline in BuildInlines(element))
            paragraph.Inlines.Add(inline);
        return paragraph;
    }

    private static Section BuildSection(XElement element)
    {
        var section = new Section();
        ApplyTextProperties(element, section);
        ApplyBlockProperties(element, section);
        foreach (var block in BuildBlocks(element))
            section.Blocks.Add(block);
        return section;
    }

    private static List BuildList(XElement element)
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
            foreach (var block in BuildBlocks(child))
                item.Blocks.Add(block);
            list.ListItems.Add(item);
        }
        if (element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            throw new InvalidDataException("A native list contains text outside a list item.");
        return list;
    }

    private static IEnumerable<Inline> BuildInlines(XElement parent)
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

            Inline inline = element.Name.LocalName switch
            {
                "Run" => BuildRun(element),
                "Span" => BuildSpan(element, new Span()),
                "Bold" => BuildSpan(element, new Bold()),
                "Italic" => BuildSpan(element, new Italic()),
                "Underline" => BuildSpan(element, new Underline()),
                "LineBreak" => BuildLineBreak(element),
                _ => throw new InvalidDataException($"Element '{element.Name.LocalName}' is not valid inline content.")
            };
            yield return inline;
        }
    }

    private static Run BuildRun(XElement element)
    {
        if (element.Elements().Any())
            throw new InvalidDataException("A native Run cannot contain child elements.");
        var run = new Run(string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value)));
        ApplyTextProperties(element, run);
        ApplyInlineProperties(element, run);
        return run;
    }

    private static Span BuildSpan(XElement element, Span span)
    {
        ApplyTextProperties(element, span);
        ApplyInlineProperties(element, span);
        foreach (var inline in BuildInlines(element))
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
    }

    private static bool TryAttribute(XElement element, string name, out string value)
    {
        value = element.Attribute(name)?.Value ?? string.Empty;
        return element.Attribute(name) is not null;
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
