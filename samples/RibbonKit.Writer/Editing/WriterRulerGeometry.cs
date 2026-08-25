using System.Collections.ObjectModel;
using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Editing;

/// <summary>Which physical page edge is being adjusted by the Writer ruler.</summary>
public enum WriterRulerMarginEdge
{
    /// <summary>The physical left page margin.</summary>
    Left = 0,

    /// <summary>The physical right page margin.</summary>
    Right = 1
}

/// <summary>Which paragraph marker is being adjusted by the Writer ruler.</summary>
public enum WriterRulerIndentMarker
{
    /// <summary>The paragraph's first-line marker.</summary>
    FirstLine = 0,

    /// <summary>The paragraph's hanging-indent marker.</summary>
    Hanging = 1,

    /// <summary>The paragraph's left-indent marker.</summary>
    Left = 2,

    /// <summary>The paragraph's right-indent marker.</summary>
    Right = 3
}

/// <summary>A single calibrated horizontal-ruler tick.</summary>
public readonly record struct WriterRulerTick(
    double PositionDip,
    double LengthDip,
    bool IsMajor,
    string Label);

/// <summary>A shaded page-margin interval in ruler coordinates.</summary>
public readonly record struct WriterRulerMarginZone(double StartDip, double EndDip)
{
    /// <summary>Gets the width of the interval.</summary>
    public double WidthDip => Math.Max(0, EndDip - StartDip);
}

/// <summary>
/// Immutable paragraph indentation values used by the horizontal ruler. Values are logical DIPs
/// before the current page zoom is applied.
/// </summary>
public readonly record struct WriterRulerIndentation(
    double LeftDip,
    double FirstLineDip,
    double HangingDip,
    double RightDip)
{
    /// <summary>Gets the default no-indent values.</summary>
    public static WriterRulerIndentation Empty => new(0, 0, 0, 0);

    /// <summary>
    /// Gets the signed WPF <c>TextIndent</c> value used by the first-line marker. Positive values
    /// move the first line inward; negative values place it before the body marker.
    /// </summary>
    public double TextIndentDip => FirstLineDip;

    /// <summary>Gets the first-line marker's logical position from the content origin.</summary>
    public double FirstLineMarkerDip => LeftDip + FirstLineDip;

    /// <summary>Gets the body/hanging marker's logical position from the content origin.</summary>
    public double HangingMarkerDip => LeftDip;
}

/// <summary>
/// The physical page, content boundary, margin zones, ticks and paragraph markers projected into
/// the Writer ruler's coordinate space.
/// </summary>
public sealed class WriterRulerLayout
{
    internal WriterRulerLayout(
        double pageOriginDip,
        double pageWidthDip,
        double scale,
        double contentStartDip,
        double contentEndDip,
        IReadOnlyList<WriterRulerTick> ticks,
        IReadOnlyList<WriterRulerMarginZone> marginZones,
        WriterRulerIndentation indentation)
    {
        PageOriginDip = pageOriginDip;
        PageWidthDip = pageWidthDip;
        Scale = scale;
        ContentStartDip = contentStartDip;
        ContentEndDip = contentEndDip;
        Ticks = ticks;
        MarginZones = marginZones;
        Indentation = indentation;
    }

    /// <summary>Gets the physical page's left coordinate in the control.</summary>
    public double PageOriginDip { get; }

    /// <summary>Gets the rendered page width after zoom.</summary>
    public double PageWidthDip { get; }

    /// <summary>Gets the logical-to-rendered scale factor.</summary>
    public double Scale { get; }

    /// <summary>Gets the rendered left content boundary.</summary>
    public double ContentStartDip { get; }

    /// <summary>Gets the rendered right content boundary.</summary>
    public double ContentEndDip { get; }

    /// <summary>Gets the rendered page's right coordinate.</summary>
    public double PageEndDip => PageOriginDip + PageWidthDip;

    /// <summary>Gets the rendered content width.</summary>
    public double ContentWidthDip => Math.Max(0, ContentEndDip - ContentStartDip);

    /// <summary>Gets calibrated quarter-inch and inch ticks.</summary>
    public IReadOnlyList<WriterRulerTick> Ticks { get; }

    /// <summary>Gets the shaded left and right page-margin intervals.</summary>
    public IReadOnlyList<WriterRulerMarginZone> MarginZones { get; }

    /// <summary>Gets the logical paragraph-indent values used for markers.</summary>
    public WriterRulerIndentation Indentation { get; }

    /// <summary>Gets a marker coordinate, or <see langword="null"/> when it is outside the page.</summary>
    public double? GetMarkerPosition(WriterRulerIndentMarker marker)
    {
        var position = marker switch
        {
            WriterRulerIndentMarker.FirstLine => ContentStartDip + Indentation.FirstLineMarkerDip * Scale,
            WriterRulerIndentMarker.Hanging => ContentStartDip + Indentation.HangingMarkerDip * Scale,
            WriterRulerIndentMarker.Left => ContentStartDip + Indentation.LeftDip * Scale,
            WriterRulerIndentMarker.Right => ContentEndDip - Indentation.RightDip * Scale,
            _ => throw new ArgumentOutOfRangeException(nameof(marker), marker, "Unknown ruler marker.")
        };
        return position >= PageOriginDip - 0.5 && position <= PageEndDip + 0.5
            ? position
            : null;
    }
}

/// <summary>Pure geometry helpers for the Writer horizontal ruler and margin guide.</summary>
public static class WriterRulerGeometry
{
    private const double DipsPerInch = DocumentLength.DipsPerInch;
    private const double QuarterInch = DipsPerInch / 4d;

    /// <summary>
    /// Projects a logical page into rendered ruler coordinates. The page origin is supplied by the
    /// caller so centring and horizontal scrolling remain the responsibility of the live viewport.
    /// </summary>
    public static WriterRulerLayout Create(
        DocumentPageSettings settings,
        double zoomPercent,
        double pageOriginDip,
        WriterRulerIndentation? indentation = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!double.IsFinite(zoomPercent) || zoomPercent <= 0)
            throw new ArgumentOutOfRangeException(nameof(zoomPercent), zoomPercent,
                "Ruler zoom must be finite and positive.");
        if (!double.IsFinite(pageOriginDip))
            throw new ArgumentOutOfRangeException(nameof(pageOriginDip), pageOriginDip,
                "The page origin must be finite.");

        var scale = zoomPercent / 100d;
        var pageWidth = settings.WidthDip * scale;
        var left = settings.Margins.LeftDip * scale;
        var right = settings.Margins.RightDip * scale;
        var contentStart = pageOriginDip + left;
        var contentEnd = pageOriginDip + pageWidth - right;
        var ticks = BuildTicks(pageOriginDip, settings.WidthDip, scale);
        var zones = new ReadOnlyCollection<WriterRulerMarginZone>(new[]
        {
            new WriterRulerMarginZone(pageOriginDip, contentStart),
            new WriterRulerMarginZone(contentEnd, pageOriginDip + pageWidth)
        });
        var values = indentation ?? WriterRulerIndentation.Empty;
        return new WriterRulerLayout(pageOriginDip, pageWidth, scale, contentStart, contentEnd,
            new ReadOnlyCollection<WriterRulerTick>(ticks), zones, values);
    }

    /// <summary>Converts a rendered ruler coordinate into a logical page coordinate.</summary>
    public static double ToLogicalPageDip(WriterRulerLayout layout, double renderedX) =>
        (renderedX - layout.PageOriginDip) / layout.Scale;

    /// <summary>Converts a rendered content coordinate into a logical content coordinate.</summary>
    public static double ToLogicalContentDip(WriterRulerLayout layout, double renderedX) =>
        (renderedX - layout.ContentStartDip) / layout.Scale;

    /// <summary>Returns a bounded candidate left margin for a rendered pointer position.</summary>
    public static double LeftMarginFromRenderedX(WriterRulerLayout layout, double renderedX,
        double rightMarginDip, double minimumContentDip = 1) =>
        Clamp(ToLogicalPageDip(layout, renderedX), 0,
            Math.Max(0, layout.PageWidthDip / layout.Scale - rightMarginDip - minimumContentDip));

    /// <summary>Returns a bounded candidate right margin for a rendered pointer position.</summary>
    public static double RightMarginFromRenderedX(WriterRulerLayout layout, double renderedX,
        double leftMarginDip, double minimumContentDip = 1) =>
        Clamp(layout.PageWidthDip / layout.Scale - ToLogicalPageDip(layout, renderedX), 0,
            Math.Max(0, layout.PageWidthDip / layout.Scale - leftMarginDip - minimumContentDip));

    private static List<WriterRulerTick> BuildTicks(double pageOriginDip, double logicalPageWidthDip,
        double scale)
    {
        var result = new List<WriterRulerTick>();
        var count = (int)Math.Floor(logicalPageWidthDip / QuarterInch + 0.000001);
        for (var index = 0; index <= count; index++)
        {
            var logical = index * QuarterInch;
            var rendered = pageOriginDip + logical * scale;
            var major = index % 4 == 0;
            var half = index % 2 == 0;
            result.Add(new WriterRulerTick(rendered, major ? 11 : half ? 8 : 5, major,
                major ? (logical / DipsPerInch).ToString("0.#") : string.Empty));
        }

        // Preserve the physical page edge even for custom dimensions that are not an exact
        // quarter-inch multiple. This prevents the last tick from drifting past the page.
        var edge = pageOriginDip + logicalPageWidthDip * scale;
        if (result.Count == 0 || Math.Abs(result[^1].PositionDip - edge) > 0.01)
            result.Add(new WriterRulerTick(edge, 11, true,
                (logicalPageWidthDip / DipsPerInch).ToString("0.#")));
        return result;
    }

    private static double Clamp(double value, double minimum, double maximum) =>
        Math.Min(Math.Max(value, minimum), maximum);
}
