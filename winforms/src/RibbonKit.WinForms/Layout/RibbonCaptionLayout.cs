using System.Globalization;

namespace RibbonKit.WinForms.Layout;

// Only Large command captions use compact two-line layout. Each line is still
// measured and drawn by TextRenderer; the font and first-line origin stay intact.
internal sealed class RibbonCaptionLayout
{
    private readonly Dictionary<Key, Caption> _cache = [];
    private readonly record struct Key(string Text, string FontName, float Size, FontStyle Style,
        GraphicsUnit Unit, byte CharSet, bool Vertical, int Dpi, int Width, int ArrowSpan, bool ArrowBelowSingle);
    internal sealed record Caption(string[] Lines, int[] Widths);
    internal static int LineAdvance(Font font, int dpi) => Math.Max(1, font.Height - (int)Math.Ceiling(2 * dpi / 96d));
    internal static int TextWidth(string text, Font font) => string.IsNullOrEmpty(text) ? 0 :
        TextRenderer.MeasureText(text, font, new(int.MaxValue, int.MaxValue),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
    internal Caption Wrap(string text, Font font, int dpi, int width, int arrowSpan = 0, bool arrowBelowSingle = false)
    {
        var key = new Key(text, font.Name, font.Size, font.Style, font.Unit, font.GdiCharSet, font.GdiVerticalFont, dpi, width, arrowSpan, arrowBelowSingle);
        if (_cache.TryGetValue(key, out var lines)) return lines;
        var rows = Measure(text, font, width, arrowBelowSingle ? 0 : arrowSpan);
        int lastWidth = Math.Max(0, width - (rows.Length == 1 && arrowBelowSingle ? 0 : arrowSpan));
        rows[^1] = Ellipsize(rows[^1], font, lastWidth);
        var widths = rows.Select(row => TextWidth(row, font)).ToArray();
        lines = new(rows, widths);
        if (_cache.Count >= 96) _cache.Clear();
        _cache[key] = lines;
        return lines;
    }
    private static string Ellipsize(string text, Font font, int width)
    {
        if (TextWidth(text, font) <= width) return text;
        const string ellipsis = "…";
        if (TextWidth(ellipsis, font) > width) return "";
        int[] elements = StringInfo.ParseCombiningCharacters(text);
        int low = 0, high = elements.Length;
        string Candidate(int count) => text[..(count == elements.Length ? text.Length : elements[count])].TrimEnd() + ellipsis;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            if (TextWidth(Candidate(middle), font) <= width) low = middle;
            else high = middle - 1;
        }
        return Candidate(low);
    }
    private static string[] Measure(string text, Font font, int width, int singleLineArrow)
    {
        if (string.IsNullOrEmpty(text) || width <= 0) return [text];
        int hardBreak = text.IndexOfAny(['\r', '\n']);
        string first = hardBreak < 0 ? text : text[..hardBreak];
        bool Fits(string value) => TextWidth(value, font) <= width;
        if (Fits(first) && (hardBreak >= 0 || TextWidth(first, font) + singleLineArrow <= width))
            return hardBreak < 0 ? [first] : [first, text[hardBreak..].Replace('\r', ' ').Replace('\n', ' ').TrimStart()];
        // Break long words only at text-element boundaries, preserving surrogate
        // pairs and combining marks. Prefer a word boundary when one fits.
        int[] elements = StringInfo.ParseCombiningCharacters(first);
        if (elements.Length == 1 && hardBreak < 0) return [first];
        // If the caption alone fits but its inline arrow does not, retain some
        // text for row two rather than creating an empty arrow-only row.
        int low = 0, high = Math.Max(0, elements.Length - (hardBreak < 0 ? 1 : 0));
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            int end = middle == elements.Length ? first.Length : elements[middle];
            if (Fits(first[..end])) low = middle;
            else high = middle - 1;
        }
        int split = low == elements.Length ? first.Length : elements[low];
        if (split < first.Length && !char.IsWhiteSpace(first[split]))
            for (int index = split - 1; index > 0; index--)
                if (char.IsWhiteSpace(first[index])) { split = index; break; }
        return [first[..split].TrimEnd(), text[split..].Replace('\r', ' ').Replace('\n', ' ').TrimStart()];
    }
    internal void Clear() => _cache.Clear();
}
