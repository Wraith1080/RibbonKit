namespace RibbonKit.WinForms;

/// <summary>Immutable ordinary-menu heading, separator and overflow-band decoration. Existing menu geometry, state fills, glyph settings, native ownership and Application Menu/Backstage recipes remain separate.</summary>
public sealed record RibbonMenuAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing menu appearance.</summary>
    public RibbonMenuAppearanceSettings() { }
    /// <summary>Gets optional ordinary heading corner radius in logical pixels, 0..64. Null retains the actual renderer's square neutral or Office corners. Shape alone does not invent a neutral heading fill.</summary>
    public int? HeaderCornerRadius { get; init; }
    /// <summary>Gets optional heading paint inset in logical pixels, 0..1024. Null retains one device pixel; authored values clamp inside the unchanged heading bounds.</summary>
    public int? HeaderInset { get; init; }
    /// <summary>Gets optional heading outline width in logical pixels, 0..32. Null retains one device pixel when ink exists; zero hides the outline.</summary>
    public int? HeaderBorderWidth { get; init; }
    /// <summary>Gets optional heading outline ink, including alpha. Empty retains the existing transparent outline; Transparent hides it. MenuHeadingBackground and MenuHeadingForeground remain independent.</summary>
    public Color HeaderBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional ordinary heading bold treatment. Null/true retain the host font's styles plus Bold; false removes Bold and retains the other host styles. Row geometry remains unchanged.</summary>
    public bool? HeaderBold { get; init; }
    /// <summary>Gets optional ordinary horizontal separator thickness in logical pixels, 0..32. Null retains one device pixel; zero hides it. Thickness clamps to the existing row height and keeps its centered placement.</summary>
    public int? SeparatorThickness { get; init; }
    /// <summary>Gets optional ordinary horizontal separator ink, including alpha. Empty follows Border; Transparent hides it. Application Menu, Backstage and vertical group separators keep their existing recipes.</summary>
    public Color SeparatorColor { get; init; } = Color.Empty;
    /// <summary>Gets optional opaque overflow-band backing fill, including opaque gradients. Null retains opaque PopupBackground/Surface. Both color stops must be opaque so scrolled content cannot bleed through.</summary>
    public RibbonFill? OverflowBackground { get; init; }
    /// <summary>Gets optional active overflow-band corner radius in logical pixels, 0..64. Null retains the actual renderer's square neutral or Office corners. Targets and endpoint eligibility remain unchanged.</summary>
    public int? OverflowCornerRadius { get; init; }
    /// <summary>Gets optional active overflow-band inset in logical pixels, 0..1024. Null retains one device pixel; authored values clamp inside the unchanged band.</summary>
    public int? OverflowInset { get; init; }
    /// <summary>Gets optional active overflow-band outline width in logical pixels, 0..32. Null retains one device pixel when ink exists; zero hides it. Hover/Pressed fills retain their existing settings.</summary>
    public int? OverflowBorderWidth { get; init; }
    /// <summary>Gets optional active overflow-band outline ink, including alpha. Empty retains the existing transparent outline; Transparent hides it. Disabled and idle bands retain their backing and glyph only.</summary>
    public Color OverflowBorderColor { get; init; } = Color.Empty;

    internal bool HasHeaderChrome => HeaderCornerRadius is not null || HeaderInset is not null || HeaderBorderWidth is not null || !HeaderBorderColor.IsEmpty;
    internal bool HasOverflowChrome => OverflowCornerRadius is not null || OverflowInset is not null || OverflowBorderWidth is not null || !OverflowBorderColor.IsEmpty;
    internal static void Validate(RibbonMenuAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.HeaderCornerRadius is < 0 or > 64 || settings.OverflowCornerRadius is < 0 or > 64 || settings.HeaderInset is < 0 or > 1024 || settings.OverflowInset is < 0 or > 1024
            || settings.HeaderBorderWidth is < 0 or > 32 || settings.OverflowBorderWidth is < 0 or > 32 || settings.SeparatorThickness is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(settings), "Menu radii must be 0..64, outline/separator widths 0..32 and insets 0..1024 logical pixels, or null for existing paint.");
        if (settings.OverflowBackground is { } fill && (fill.Start.A != 255 || fill.End.A != 255))
            throw new ArgumentException("Both overflow backing fill stops must be opaque.", nameof(settings));
    }
}
