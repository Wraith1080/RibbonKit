using System.Globalization;
using System.Windows;
using System.Windows.Media;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Page;

/// <summary>Named margin choices exposed by Writer's Page ribbon tab.</summary>
public enum WriterMarginPreset
{
    Normal = 0,
    Narrow = 1,
    Moderate = 2,
    Wide = 3
}

/// <summary>Shared validation and presentation helpers for Writer page controls.</summary>
public static class WriterPageUi
{
    /// <summary>Creates the physical margins represented by a named Writer preset.</summary>
    public static DocumentPageMargins CreateMargins(WriterMarginPreset preset) => preset switch
    {
        WriterMarginPreset.Normal => DocumentPageMargins.Normal,
        WriterMarginPreset.Narrow => UniformInches(0.5),
        WriterMarginPreset.Moderate => new DocumentPageMargins(
            DocumentLength.InchesToDips(0.75), DocumentLength.InchesToDips(1),
            DocumentLength.InchesToDips(0.75), DocumentLength.InchesToDips(1)),
        WriterMarginPreset.Wide => new DocumentPageMargins(
            DocumentLength.InchesToDips(2), DocumentLength.InchesToDips(1),
            DocumentLength.InchesToDips(2), DocumentLength.InchesToDips(1)),
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown Writer margin preset.")
    };

    /// <summary>Parses four non-negative inch values and validates them against the current page.</summary>
    public static bool TryCreateCustomSettings(DocumentPageSettings current,
        string? topText, string? bottomText, string? leftText, string? rightText,
        IFormatProvider? formatProvider, out DocumentPageSettings? settings, out string error)
    {
        ArgumentNullException.ThrowIfNull(current);
        var culture = formatProvider ?? CultureInfo.CurrentCulture;
        if (!TryParseLength(topText, culture, out var top) ||
            !TryParseLength(bottomText, culture, out var bottom) ||
            !TryParseLength(leftText, culture, out var left) ||
            !TryParseLength(rightText, culture, out var right))
        {
            settings = null;
            error = "Enter a non-negative number for every margin.";
            return false;
        }

        var leftDip = DocumentLength.InchesToDips(left);
        var topDip = DocumentLength.InchesToDips(top);
        var rightDip = DocumentLength.InchesToDips(right);
        var bottomDip = DocumentLength.InchesToDips(bottom);
        if (leftDip + rightDip >= current.WidthDip)
        {
            settings = null;
            error = "Left and right margins must leave a positive content width.";
            return false;
        }
        if (topDip + bottomDip >= current.HeightDip)
        {
            settings = null;
            error = "Top and bottom margins must leave a positive content height.";
            return false;
        }

        try
        {
            settings = current.WithMargins(new DocumentPageMargins(
                leftDip, topDip, rightDip, bottomDip));
            error = string.Empty;
            return true;
        }
        catch (ArgumentException exception)
        {
            settings = null;
            error = exception.Message;
            return false;
        }
    }

    /// <summary>Formats the current page settings and optional preview page count for shell surfaces.</summary>
    public static string FormatSummary(DocumentPageSettings settings, int pageCount = 0, Brush? pageColor = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var size = settings.PaperSize == DocumentPaperSize.Custom ? "Custom" : settings.PaperSize.ToString();
        var orientation = settings.Orientation == DocumentPageOrientation.Portrait ? "Portrait" : "Landscape";
        var margins = settings.Margins;
        var pages = pageCount > 0 ? $" · {pageCount:N0} {(pageCount == 1 ? "page" : "pages")}" : string.Empty;
        var color = PageColorName(pageColor);
        return $"{size} · {orientation}{pages}\n" +
            $"Margins (in): {Inches(margins.TopDip)} top, {Inches(margins.BottomDip)} bottom, " +
            $"{Inches(margins.LeftDip)} left, {Inches(margins.RightDip)} right\nPage colour: {color}";
    }

    private static DocumentPageMargins UniformInches(double inches)
    {
        var dip = DocumentLength.InchesToDips(inches);
        return new DocumentPageMargins(dip, dip, dip, dip);
    }

    private static bool TryParseLength(string? text, IFormatProvider provider, out double value) =>
        double.TryParse(text, NumberStyles.Float, provider, out value) && double.IsFinite(value) && value >= 0;

    private static string Inches(double dip) =>
        DocumentLength.DipsToInches(dip).ToString("0.##", CultureInfo.CurrentCulture);

    private static string PageColorName(Brush? brush)
    {
        if (brush is not SolidColorBrush solid)
            return "White";
        return solid.Color switch
        {
            var color when color == Colors.White => "White",
            var color when color == Color.FromRgb(255, 253, 240) => "Ivory",
            var color when color == Color.FromRgb(235, 246, 255) => "Light blue",
            _ => solid.Color.ToString()
        };
    }
}
