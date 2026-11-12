namespace RibbonKit.WinForms;

/// <summary>Immutable built-in gallery tile-state and heading decoration. Tile content layout, inline frame/utilities and application presenter visuals retain their existing owners.</summary>
public sealed record RibbonGalleryAppearanceSettings
{
    /// <summary>Initializes overrides retaining existing gallery paint.</summary>
    public RibbonGalleryAppearanceSettings() { }
    /// <summary>Gets optional active tile-state corner radius in logical pixels, 0..64. Null keeps the actual renderer's neutral/Office primitive radius.</summary>
    public int? TileCornerRadius { get; init; }
    /// <summary>Gets optional active tile-state paint inset in logical pixels, 0..1024. Null retains one device pixel. Targets, content and focus slots do not move.</summary>
    public int? TileInset { get; init; }
    /// <summary>Gets optional active tile-state outline width in logical pixels, 0..32. Null keeps one device pixel when ink exists; zero hides the outline.</summary>
    public int? TileBorderWidth { get; init; }
    /// <summary>Gets optional active tile-state outline ink, including alpha. Empty retains Accent for selected tiles and transparent ink otherwise. This does not activate idle tiles.</summary>
    public Color TileBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional heading-fill corner radius in logical pixels, 0..64. Null keeps the actual renderer's primitive radius; shape alone does not add a neutral fill.</summary>
    public int? HeaderCornerRadius { get; init; }
    /// <summary>Gets optional heading paint inset in logical pixels, 0..1024. Null retains one device pixel. Existing heading text padding and measured height remain independent.</summary>
    public int? HeaderInset { get; init; }
    /// <summary>Gets optional heading outline width in logical pixels, 0..32. Null keeps one device pixel when ink exists; zero hides the outline.</summary>
    public int? HeaderBorderWidth { get; init; }
    /// <summary>Gets optional heading outline ink, including alpha. Empty retains the transparent outline; explicit ink can add a neutral heading outline without inventing a fill.</summary>
    public Color HeaderBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets an optional heading fill, including alpha/gradients. Null retains no fill for the neutral renderer or existing GalleryHeadingBackground for Office. Ordinary-menu and Application Menu headings keep their own roles.</summary>
    public RibbonFill? HeaderBackground { get; init; }
    /// <summary>Gets optional heading weight. Null/true adds Bold to host styles; false removes Bold while preserving other host styles. Text bounds and heading measurement remain unchanged.</summary>
    public bool? HeaderBold { get; init; }

    internal bool HasTileChrome => TileCornerRadius is not null || TileInset is not null || TileBorderWidth is not null || !TileBorderColor.IsEmpty;
    internal bool HasHeaderChrome => HeaderCornerRadius is not null || HeaderInset is not null || HeaderBorderWidth is not null || !HeaderBorderColor.IsEmpty;
    internal static void Validate(RibbonGalleryAppearanceSettings? a)
    {
        if (a is null) return;
        if (a.TileCornerRadius is < 0 or > 64 || a.HeaderCornerRadius is < 0 or > 64 || a.TileInset is < 0 or > 1024 || a.HeaderInset is < 0 or > 1024 || a.TileBorderWidth is < 0 or > 32 || a.HeaderBorderWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(a), "Gallery radii must be 0..64, insets 0..1024 and outline widths 0..32 logical pixels, or null for existing paint.");
    }
}
