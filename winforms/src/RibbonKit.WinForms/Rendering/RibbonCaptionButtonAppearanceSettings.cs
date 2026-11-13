namespace RibbonKit.WinForms;

/// <summary>Immutable RibbonForm caption-button paint overrides. Null/Empty members retain existing state recipes and glyph metrics. Assign through RibbonTheme.CaptionButtonAppearance; native targets and window actions are unchanged.</summary>
public sealed record RibbonCaptionButtonAppearanceSettings
{
    /// <summary>Initializes overrides retaining existing caption-button appearance.</summary>
    public RibbonCaptionButtonAppearanceSettings() { }
    /// <summary>Gets optional symmetric state-fill inset in logical pixels, 0..1024. Null uses zero. Clamps inside the native target without moving its glyph or hit area.</summary>
    public int? StateInset { get; init; }
    /// <summary>Gets optional state-fill radius in logical pixels, 0..64. Null uses square corners; available paint bounds clamp the radius.</summary>
    public int? CornerRadius { get; init; }
    /// <summary>Gets optional non-close hover fill, including alpha/gradients. Null retains solid Hover.Start or the existing material BackdropHover.Start/fallback coverage.</summary>
    public RibbonFill? HoverBackground { get; init; }
    /// <summary>Gets optional non-close pressed fill, including alpha/gradients. Null retains solid Pressed.Start or the existing material BackdropPressed.Start/fallback coverage.</summary>
    public RibbonFill? PressedBackground { get; init; }
    /// <summary>Gets optional close hover fill, including alpha/gradients. Null retains red (232,17,35), opaque in solid captions and alpha 210 in material/foreground-plane captions. Transparent hides the fill.</summary>
    public RibbonFill? CloseHoverBackground { get; init; }
    /// <summary>Gets optional close pressed fill, including alpha/gradients. Null retains red (232,17,35), opaque in solid captions and alpha 235 in material/foreground-plane captions. Transparent hides the fill.</summary>
    public RibbonFill? ClosePressedBackground { get; init; }
    /// <summary>Gets optional vector glyph side in logical pixels, 0..256. Null uses 10. Zero hides all caption glyphs; authored geometry clamps within the unchanged native target.</summary>
    public int? GlyphSize { get; init; }
    /// <summary>Gets optional glyph stroke in logical pixels, finite 0..32. Null retains max(1 device pixel, DPI/96). Zero hides glyphs; authored strokes clamp inside the unchanged target.</summary>
    public double? GlyphStrokeWidth { get; init; }
    /// <summary>Gets glyph ink, including alpha. Empty follows the resolved caption Foreground; Transparent hides ordinary/idle glyphs. Close hover/pressed ink is independent.</summary>
    public Color GlyphColor { get; init; } = Color.Empty;
    /// <summary>Gets close hover/pressed glyph ink, including alpha. Empty retains White; Transparent hides this state glyph. Other caption glyphs follow GlyphColor.</summary>
    public Color CloseHotGlyphColor { get; init; } = Color.Empty;
    /// <summary>Gets optional separation of the two restore outlines in logical pixels, 0..256. Null retains the legacy three device pixels at every DPI. Explicit values scale with DPI and clamp inside the glyph; zero overlaps the outlines.</summary>
    public int? RestoreOffset { get; init; }

    internal static void Validate(RibbonCaptionButtonAppearanceSettings? a)
    {
        if (a is null) return;
        if (a.StateInset is < 0 or > 1024 || a.CornerRadius is < 0 or > 64 || a.GlyphSize is < 0 or > 256 || a.RestoreOffset is < 0 or > 256 ||
            a.GlyphStrokeWidth is { } stroke && (!double.IsFinite(stroke) || stroke < 0 || stroke > 32))
            throw new ArgumentOutOfRangeException(nameof(a), "Caption button values must be within their documented ranges; stroke must be finite. Null retains existing appearance.");
    }
}
