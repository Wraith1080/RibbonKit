namespace RibbonKit.WinForms;

/// <summary>Immutable built-in gallery artwork/label and inline utility-glyph overrides. Presenter card internals, shared image treatment, selection and native control ownership remain unchanged.</summary>
public sealed record RibbonGalleryContentSettings
{
    /// <summary>Initializes overrides retaining existing gallery content and glyph recipes.</summary>
    public RibbonGalleryContentSettings() { }
    /// <summary>Gets optional label line cap, 0..32. Null retains two. Zero hides labels and removes their measured line height; inline labels clamp to the existing viewport.</summary>
    public int? LabelMaxLines { get; init; }
    /// <summary>Gets optional label alignment. Null retains MiddleCenter; wrapping/ellipsis and the existing label slot remain in use.</summary>
    public ContentAlignment? LabelAlignment { get; init; }
    /// <summary>Gets optional artwork height reserve in logical pixels, 0..1024, added to the existing triple TilePadding allowance. Null retains 12 for paint and 24 for popup minimum-height measurement; explicit values unify both.</summary>
    public int? ArtworkHeightReserve { get; init; }
    /// <summary>Gets optional artwork horizontal inset in logical pixels, 0..1024. Null retains GalleryTilePadding plus 4; authored values clamp inside the tile.</summary>
    public int? ArtworkHorizontalInset { get; init; }
    /// <summary>Gets optional artwork top inset in logical pixels, 0..1024. Null retains GalleryTilePadding plus 2; authored values also adjust popup minimum-height measurement and clamp inside the tile.</summary>
    public int? ArtworkTopInset { get; init; }
    /// <summary>Gets optional expand glyph slot height in logical pixels, 0..64. Null retains 18; zero hides both its chevron and bar without removing the action target. Up/Down chevrons retain their slots.</summary>
    public int? ExpandGlyphHeight { get; init; }
    /// <summary>Gets optional expand-bar width in logical pixels, 0..64. Null retains 10; zero hides the bar while leaving the chevron. Authored bar geometry clips inside the action target.</summary>
    public int? ExpandBarWidth { get; init; }
    /// <summary>Gets optional expand-bar top/side inset in logical pixels, 0..1024. Null retains 4; values consuming the slot hide the bar.</summary>
    public int? ExpandBarInset { get; init; }
    /// <summary>Gets optional finite expand-bar stroke in logical pixels, 0..32. Null retains the DPI-rounded two-pixel stroke with one-device-pixel minimum; explicit zero hides the bar.</summary>
    public double? ExpandBarStrokeWidth { get; init; }
    /// <summary>Gets optional utility chevron/bar ink, including alpha. Empty retains enabled Foreground or DisabledColor; Transparent hides glyph paint. Existing Chevrons still owns chevron size/stroke.</summary>
    public Color UtilityGlyphColor { get; init; } = Color.Empty;

    internal static void Validate(RibbonGalleryContentSettings? a)
    {
        if (a is null) return;
        if (a.LabelMaxLines is < 0 or > 32 || a.ArtworkHeightReserve is < 0 or > 1024 || a.ArtworkHorizontalInset is < 0 or > 1024 || a.ArtworkTopInset is < 0 or > 1024
            || a.ExpandGlyphHeight is < 0 or > 64 || a.ExpandBarWidth is < 0 or > 64 || a.ExpandBarInset is < 0 or > 1024
            || a.ExpandBarStrokeWidth is { } stroke && (!double.IsFinite(stroke) || stroke < 0 || stroke > 32) || a.LabelAlignment is { } alignment && !Enum.IsDefined(alignment))
            throw new ArgumentOutOfRangeException(nameof(a), "Gallery lines must be 0..32, artwork/bar insets and reserve 0..1024, expand height/width 0..64, stroke finite 0..32 and alignment a defined ContentAlignment, or null for existing content.");
    }
}
