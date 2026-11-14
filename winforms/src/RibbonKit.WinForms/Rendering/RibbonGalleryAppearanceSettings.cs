namespace RibbonKit.WinForms;

/// <summary>Immutable built-in gallery tile, heading and inline frame/utility decoration. Content settings and application presenter visuals retain their existing owners.</summary>
public sealed record RibbonGalleryAppearanceSettings
{
    /// <summary>Gets optional selected/hover tile outlines and inner rims. Explicit tile border settings win; headings/strip chrome are independent.</summary>
    public RibbonStateChromeSettings? StateChrome { get; init; }
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
    /// <summary>Gets optional inline strip frame radius in logical pixels, 0..64. Null retains the actual renderer's primitive corners. Decoration does not move native presenter bounds.</summary>
    public int? StripCornerRadius { get; init; }
    /// <summary>Gets optional inline strip frame paint inset in logical pixels, 0..1024. Null retains one device pixel; native content padding remains GalleryContentPadding.</summary>
    public int? StripInset { get; init; }
    /// <summary>Gets optional inline strip frame stroke in logical pixels, 0..32. Null retains one device pixel when ink exists; zero hides the outline.</summary>
    public int? StripBorderWidth { get; init; }
    /// <summary>Gets optional inline strip frame ink, including alpha. Empty follows PopupBorder; Transparent hides paint.</summary>
    public Color StripBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional inline strip frame fill, including alpha/gradients. Null follows Surface. Native application Controls keep their own backgrounds.</summary>
    public RibbonFill? StripBackground { get; init; }
    /// <summary>Gets optional active utility-button radius in logical pixels, 0..64. Null retains the actual renderer's primitive corners.</summary>
    public int? UtilityCornerRadius { get; init; }
    /// <summary>Gets optional active utility-button paint inset in logical pixels, 0..1024. Null retains one device pixel; action targets are unchanged.</summary>
    public int? UtilityInset { get; init; }
    /// <summary>Gets optional active utility-button outline width in logical pixels, 0..32. Null retains one device pixel when ink exists; zero hides the outline.</summary>
    public int? UtilityBorderWidth { get; init; }
    /// <summary>Gets optional active utility-button ink, including alpha. Empty retains transparent ink. This does not activate idle or disabled buttons.</summary>
    public Color UtilityBorderColor { get; init; } = Color.Empty;

    internal bool HasTileChrome => StateChrome?.HasOverrides == true || TileCornerRadius is not null || TileInset is not null || TileBorderWidth is not null || !TileBorderColor.IsEmpty;
    internal bool HasHeaderChrome => HeaderCornerRadius is not null || HeaderInset is not null || HeaderBorderWidth is not null || !HeaderBorderColor.IsEmpty;
    internal bool HasStripChrome => StripCornerRadius is not null || StripInset is not null || StripBorderWidth is not null || !StripBorderColor.IsEmpty;
    internal bool HasUtilityChrome => UtilityCornerRadius is not null || UtilityInset is not null || UtilityBorderWidth is not null || !UtilityBorderColor.IsEmpty;
    internal static void Validate(RibbonGalleryAppearanceSettings? a)
    {
        if (a is null) return;
        if (a.TileCornerRadius is < 0 or > 64 || a.HeaderCornerRadius is < 0 or > 64 || a.StripCornerRadius is < 0 or > 64 || a.UtilityCornerRadius is < 0 or > 64
            || a.TileInset is < 0 or > 1024 || a.HeaderInset is < 0 or > 1024 || a.StripInset is < 0 or > 1024 || a.UtilityInset is < 0 or > 1024
            || a.TileBorderWidth is < 0 or > 32 || a.HeaderBorderWidth is < 0 or > 32 || a.StripBorderWidth is < 0 or > 32 || a.UtilityBorderWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(a), "Gallery radii must be 0..64, insets 0..1024 and outline widths 0..32 logical pixels, or null for existing paint.");
    }
}
