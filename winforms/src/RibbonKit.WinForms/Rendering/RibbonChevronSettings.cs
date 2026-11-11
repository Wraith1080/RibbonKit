namespace RibbonKit.WinForms;

/// <summary>Immutable size/stroke overrides for shared stroked horizontal and vertical chevrons. Filled dropdown triangles and the Back arrow remain separate.</summary>
public sealed record RibbonChevronSettings
{
    /// <summary>Initializes overrides that retain existing chevrons.</summary>
    public RibbonChevronSettings() { }
    /// <summary>Gets optional long-axis size in logical pixels, 0..64. Null uses 10; zero hides the chevron. Existing utility slots still clamp size.</summary>
    public int? Length { get; init; }
    /// <summary>Gets optional short-axis size in logical pixels, 0..64. Null uses 6; zero hides the chevron. Click targets remain unchanged.</summary>
    public int? Depth { get; init; }
    /// <summary>Gets optional stroke width in logical pixels, 0..32. Null retains 1.5 with a one-device-pixel minimum; zero hides the chevron. Must be finite.</summary>
    public double? StrokeWidth { get; init; }
    internal static void Validate(RibbonChevronSettings? settings)
    {
        if (settings is null) return;
        if (settings.Length is < 0 or > 64 || settings.Depth is < 0 or > 64 || settings.StrokeWidth is { } width && (!double.IsFinite(width) || width < 0 || width > 32))
            throw new ArgumentOutOfRangeException(nameof(settings), "Chevron dimensions must be 0..64 and stroke a finite 0..32 logical pixels, or null for existing glyphs.");
    }
}
