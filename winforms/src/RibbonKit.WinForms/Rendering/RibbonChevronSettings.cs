namespace RibbonKit.WinForms;

/// <summary>Immutable size/stroke overrides for shared stroked horizontal/vertical utility and customization-tree chevrons. Filled dropdown triangles and the Back arrow remain separate.</summary>
public sealed record RibbonChevronSettings
{
    /// <summary>Initializes overrides that retain existing chevrons.</summary>
    public RibbonChevronSettings() { }
    /// <summary>Gets optional long-axis size in logical pixels, 0..64. Null retains ordinary utilities' 10 and tree Compact 12 / Touch 16. Zero hides paint; existing slots and click targets remain.</summary>
    public int? Length { get; init; }
    /// <summary>Gets optional short-axis size in logical pixels, 0..64. Null retains ordinary utilities' 6 and the tree's half-length legacy recipe. Zero hides paint; click targets remain unchanged.</summary>
    public int? Depth { get; init; }
    /// <summary>Gets optional stroke width in logical pixels, 0..32. Null retains ordinary utilities' 1.5 and tree Compact 1.6 / Touch 1.8, with a one-device-pixel minimum. Zero hides paint. Must be finite.</summary>
    public double? StrokeWidth { get; init; }
    internal static void Validate(RibbonChevronSettings? settings)
    {
        if (settings is null) return;
        if (settings.Length is < 0 or > 64 || settings.Depth is < 0 or > 64 || settings.StrokeWidth is { } width && (!double.IsFinite(width) || width < 0 || width > 32))
            throw new ArgumentOutOfRangeException(nameof(settings), "Chevron dimensions must be 0..64 and stroke a finite 0..32 logical pixels, or null for existing glyphs.");
    }
}
