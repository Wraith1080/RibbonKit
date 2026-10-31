using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Svg;

namespace RibbonKit.WinForms.Imaging;

// Shared by the runtime and net472 picker. The SVG DOM stays private and is
// rendered as vectors; no unbounded bitmap cache or application image ownership.
internal sealed class SvgAsset
{
    internal const int MaximumLength = 512 * 1024;
    private readonly SvgDocument _document;
    private readonly SvgElement[] _fills, _strokes;
    private readonly object _sync = new();
    internal string Source { get; }
    internal string Name { get; }
    internal bool UseForeground { get; }
    private static readonly HashSet<string> Elements = new(StringComparer.Ordinal)
    { "svg", "g", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon", "defs", "linearGradient", "radialGradient", "stop", "clipPath", "use", "symbol", "title", "desc" };

    internal SvgAsset(string source, bool useForeground)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Length > MaximumLength) throw new ArgumentException("Choose a stock icon or a self-contained SVG of at most 512 KB.", nameof(source));
        UseForeground = useForeground;
        var stock = source.StartsWith("stock:", StringComparison.Ordinal) ? StockIcons.Find(source.Substring(6)) : null;
        Source = stock == null ? source.Trim() : "stock:" + stock.Key;
        string xml = stock == null ? Source : StockIcons.Read(stock.Key);
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumLength };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        var document = XDocument.Load(reader);
        XNamespace ns = "http://www.w3.org/2000/svg";
        var root = document.Root;
        if (root == null || root.Name != ns + "svg") throw new ArgumentException("The asset must be an SVG document.");
        Name = stock?.Name ?? root.Elements(ns + "title").FirstOrDefault()?.Value.Trim() ?? "Imported SVG";
        var elements = root.DescendantsAndSelf().ToArray();
        if (elements.Length > 4096 || elements.Any(element => element.Ancestors().Take(65).Count() > 64)) throw new ArgumentException("SVG artwork is too complex for an icon.");
        foreach (var element in elements)
        {
            if (element.Name.Namespace != ns || !Elements.Contains(element.Name.LocalName))
                throw new ArgumentException("Unsupported SVG element: " + element.Name.LocalName + ". Use static paths/shapes; convert text to paths.");
            foreach (var attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
            {
                string name = attribute.Name.LocalName, value = attribute.Value.Trim();
                if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) || name == "base" ||
                    name == "href" && !value.StartsWith("#", StringComparison.Ordinal) ||
                    value.IndexOf("@", StringComparison.Ordinal) >= 0 || value.IndexOf("\\", StringComparison.Ordinal) >= 0)
                    throw new ArgumentException("SVG icons must be self-contained static artwork without external resources.");
                foreach (Match reference in Regex.Matches(value, @"url\s*\(([^)]*)\)", RegexOptions.IgnoreCase))
                    if (!reference.Groups[1].Value.Trim().Trim('\'', '"').StartsWith("#", StringComparison.Ordinal))
                        throw new ArgumentException("SVG icons cannot load external resources.");
                if (value.IndexOf("url", StringComparison.OrdinalIgnoreCase) >= 0 && !Regex.IsMatch(value, @"url\s*\(\s*['"" ]*#[^)]*\)", RegexOptions.IgnoreCase))
                    throw new ArgumentException("Invalid SVG resource reference.");
            }
        }
        var ids = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var element in elements.Where(element => element.Attribute("id") != null))
        {
            string id = (string)element.Attribute("id")!;
            if (ids.ContainsKey(id)) throw new ArgumentException("Duplicate SVG identifier: " + id);
            ids.Add(id, element);
        }
        var visited = new HashSet<XElement>(); var visiting = new HashSet<XElement>();
        void Visit(XElement element, int depth)
        {
            if (visited.Contains(element)) return;
            if (depth > 64 || !visiting.Add(element)) throw new ArgumentException("SVG references must not contain recursive cycles.");
            foreach (var child in element.Elements()) Visit(child, depth + 1);
            foreach (var attribute in element.Attributes())
            {
                var references = Regex.Matches(attribute.Value, @"url\s*\(([^)]*)\)", RegexOptions.IgnoreCase).Cast<Match>().Select(match => match.Groups[1].Value.Trim().Trim('\'', '"'));
                if (attribute.Name.LocalName == "href") references = references.Append(attribute.Value.Trim());
                foreach (string reference in references)
                {
                    if (!ids.TryGetValue(reference.Substring(1), out var target)) throw new ArgumentException("Unknown SVG reference: " + reference);
                    Visit(target, depth + 1);
                }
            }
            visiting.Remove(element); visited.Add(element);
        }
        Visit(root, 0);
        // An explicit viewBox gives the same geometry at every device size.
        var viewBox = ((string?)root.Attribute("viewBox") ?? "").Split(new[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (viewBox.Length != 4 || !viewBox.All(value => float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float number) && !float.IsNaN(number) && !float.IsInfinity(number)) ||
            float.Parse(viewBox[2], System.Globalization.CultureInfo.InvariantCulture) <= 0 || float.Parse(viewBox[3], System.Globalization.CultureInfo.InvariantCulture) <= 0)
            throw new ArgumentException("SVG icons require a valid viewBox with positive width and height.");
        root.SetAttributeValue("width", "100%"); root.SetAttributeValue("height", "100%");
        if (useForeground)
        {
            foreach (var element in root.DescendantsAndSelf())
            {
                foreach (string paint in new[] { "fill", "stroke" })
                    if (element.Attribute(paint) is { } attribute && attribute.Value != "none") attribute.Value = "currentColor";
                // Inline CSS is applied after presentation attributes by SVG.NET.
                if (element.Attribute("style") is { } style)
                    style.Value = Regex.Replace(style.Value, @"(?<name>(?<![-\w])(?:fill|stroke))\s*:\s*(?<value>[^;]+)", match =>
                        match.Groups["name"].Value + ":" + (match.Groups["value"].Value.Trim() == "none" ? "none" : "currentColor"), RegexOptions.IgnoreCase);
                element.Attribute("color")?.Remove();
                if (element.Attribute("style") is { } colors) colors.Value = Regex.Replace(colors.Value, @"(?<![-\w])color\s*:[^;]+;?", "", RegexOptions.IgnoreCase);
            }
            if (root.Attribute("fill") == null) root.SetAttributeValue("fill", "currentColor");
        }
        _document = SvgDocument.FromSvg<SvgDocument>(document.ToString(SaveOptions.DisableFormatting)) ?? throw new ArgumentException("The SVG could not be read.");
        _document.Ppi = 96;
        var paints = Walk(_document).ToArray();
        _fills = useForeground ? paints.Where(element => element.Fill != null && element.Fill != SvgPaintServer.None).ToArray() : Array.Empty<SvgElement>();
        _strokes = useForeground ? paints.Where(element => element.Stroke != null && element.Stroke != SvgPaintServer.None).ToArray() : Array.Empty<SvgElement>();
        // Exercise the same vector renderer before accepting a document. This
        // catches invalid drawing values before they can reach a paint event.
        using var probe = new Bitmap(16, 16); using var graphics = Graphics.FromImage(probe);
        try { Draw(graphics, new Rectangle(0, 0, 16, 16), Color.Black); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException)
        { throw new ArgumentException("The SVG contains unsupported drawing values.", nameof(source), error); }
    }

    internal void Draw(Graphics graphics, Rectangle bounds, Color foreground)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        lock (_sync)
        {
            var state = graphics.Save();
            try
            {
                graphics.SetClip(bounds, CombineMode.Intersect); graphics.TranslateTransform(bounds.Left, bounds.Top);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (UseForeground)
                {
                    // SVG.NET resolves currentColor paint servers lazily and may
                    // retain the first result. Replace our private paints on each
                    // draw so theme/disabled changes never reuse that old color.
                    foreach (var element in _fills) element.Fill = new SvgColourServer(foreground);
                    foreach (var element in _strokes) element.Stroke = new SvgColourServer(foreground);
                }
                _document.Draw(graphics, new SizeF(bounds.Width, bounds.Height));
            }
            finally { graphics.Restore(state); }
        }
    }
    private static IEnumerable<SvgElement> Walk(SvgElement element)
    {
        yield return element;
        foreach (var child in element.Children) foreach (var descendant in Walk(child)) yield return descendant;
    }
}

internal sealed class StockIconInfo(string key, string name)
{
    internal string Key { get; } = key;
    internal string Name { get; } = name;
}

internal static partial class StockIcons
{
    internal static StockIconInfo Find(string key) => All.FirstOrDefault(icon => icon.Key == key)
        ?? throw new ArgumentException("Unknown stock icon: " + key);
    internal static string Read(string key)
    {
        Find(key);
        using var stream = typeof(StockIcons).Assembly.GetManifestResourceStream("RibbonKit.StockIcons." + key + ".svg")
            ?? throw new InvalidOperationException("Missing stock icon: " + key);
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
}
