namespace RibbonKit.WinForms;

/// <summary>Immutable Application Menu command-content overrides shared by measurement and paint. Native editor frames, bindings and content ownership retain their existing owners.</summary>
public sealed record RibbonApplicationMenuContentSettings
{
    /// <summary>Initializes overrides retaining existing content recipes and rounding.</summary>
    public RibbonApplicationMenuContentSettings() { }
    /// <summary>Gets optional navigation/illustrated icon size in logical pixels, 0..1024. Null retains 32; zero hides artwork. Authored slots clamp inside the command.</summary>
    public int? LargeIconSize { get; init; }
    /// <summary>Gets optional ordinary-pane/footer icon size in logical pixels, 0..1024. Null retains 16; zero hides artwork.</summary>
    public int? SmallIconSize { get; init; }
    /// <summary>Gets optional navigation/illustrated icon leading inset in logical pixels, 0..1024. Null retains 8.</summary>
    public int? LargeIconInset { get; init; }
    /// <summary>Gets optional ordinary-pane icon leading inset in logical pixels, 0..1024. Null retains 6. Footer content remains centered.</summary>
    public int? SmallIconInset { get; init; }
    /// <summary>Gets optional icon/text gap in logical pixels, 0..1024. Null retains 8; navigation/illustrated text also respects its independent leading inset.</summary>
    public int? IconTextGap { get; init; }
    /// <summary>Gets optional navigation/illustrated text leading inset in logical pixels, 0..1024. Null retains 52. Authored content reserves at least icon plus gap where artwork exists.</summary>
    public int? LargeTextInset { get; init; }
    /// <summary>Gets optional icon-free ordinary-pane text inset in logical pixels, 0..1024. Null retains 6.</summary>
    public int? TextInset { get; init; }
    /// <summary>Gets optional text trailing padding in logical pixels, 0..1024. Null retains the legacy 6-pixel paint and 8-pixel description-measurement allowances; explicit values unify both.</summary>
    public int? TextTrailingPadding { get; init; }
    /// <summary>Gets optional total command/footer text vertical padding in logical pixels, 0..1024. Null retains 10. Existing minimum rows and native editor frame metrics remain authoritative.</summary>
    public int? RowTextVerticalPadding { get; init; }
    /// <summary>Gets optional total artwork vertical padding in logical pixels, 0..1024. Null retains 8; minimum rows retain their existing density rules.</summary>
    public int? IconVerticalPadding { get; init; }
    /// <summary>Gets optional title/description gap in logical pixels, 0..1024. Null retains 2.</summary>
    public int? DescriptionGap { get; init; }
    /// <summary>Gets optional description top/bottom safety inset in logical pixels, 0..1024. Null retains 4. Explicit values are reserved by row measurement.</summary>
    public int? DescriptionVerticalInset { get; init; }
    /// <summary>Gets optional description line cap, 0..32. Null retains 2; zero hides descriptions and removes their measured gap/height.</summary>
    public int? DescriptionMaxLines { get; init; }
    /// <summary>Gets optional described illustrated-title weight. Null/true adds Bold to host styles; false removes Bold while retaining other styles. Ordinary and footer labels keep their existing weight.</summary>
    public bool? RichTitleBold { get; init; }
    /// <summary>Gets optional footer content padding on each side in logical pixels, 0..1024. Null retains 6 for paint and the legacy preferred-width allowances; explicit values reserve symmetric padding in measure and paint.</summary>
    public int? FooterHorizontalPadding { get; init; }
    /// <summary>Gets optional arrow action-column width in logical pixels, 1..1024. Null retains 26 Compact/44 Touch. Touch keeps a 44-pixel minimum; split targets clamp to half the row.</summary>
    public int? ArrowColumnWidth { get; init; }
    /// <summary>Gets optional separator row height in logical pixels, 0..1024. Null retains 9; zero removes its row space.</summary>
    public int? SeparatorHeight { get; init; }
    /// <summary>Gets optional separator horizontal inset in logical pixels, 0..1024. Null retains 8; authored values clamp inside its unchanged row width.</summary>
    public int? SeparatorHorizontalPadding { get; init; }
    /// <summary>Gets optional centered separator thickness in logical pixels, 0..32. Null retains one device pixel; zero hides ink, preserving row space.</summary>
    public int? SeparatorThickness { get; init; }
    /// <summary>Gets optional separator ink, including alpha. Empty follows Border; Transparent hides paint.</summary>
    public Color SeparatorColor { get; init; } = Color.Empty;

    internal static void Validate(RibbonApplicationMenuContentSettings? a)
    {
        if (a is null) return;
        if (Invalid(a.LargeIconSize) || Invalid(a.SmallIconSize) || Invalid(a.LargeIconInset) || Invalid(a.SmallIconInset) || Invalid(a.IconTextGap) || Invalid(a.LargeTextInset) || Invalid(a.TextInset)
            || Invalid(a.TextTrailingPadding) || Invalid(a.RowTextVerticalPadding) || Invalid(a.IconVerticalPadding) || Invalid(a.DescriptionGap) || Invalid(a.DescriptionVerticalInset) || Invalid(a.FooterHorizontalPadding)
            || Invalid(a.SeparatorHeight) || Invalid(a.SeparatorHorizontalPadding) || a.DescriptionMaxLines is < 0 or > 32 || a.SeparatorThickness is < 0 or > 32 || a.ArrowColumnWidth is < 1 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(a), "Application Menu content dimensions must be 0..1024 logical pixels, arrow width 1..1024, description lines and separator stroke 0..32, or null for existing content.");
        static bool Invalid(int? value) => value is < 0 or > 1024;
    }
}
