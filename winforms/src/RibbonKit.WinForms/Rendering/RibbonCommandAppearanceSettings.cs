namespace RibbonKit.WinForms;

/// <summary>Immutable ordinary command-state and split-perimeter overrides for ribbon/menu commands and Quick Access projections. Native editor frames, indicators, headers and Application Menu recipes remain separate.</summary>
public sealed record RibbonCommandAppearanceSettings
{
    /// <summary>Gets optional per-state outer ink and inner rim. Explicit BorderColor/BorderWidth remain authoritative.</summary>
    public RibbonStateChromeSettings? StateChrome { get; init; }
    /// <summary>Initializes overrides that retain existing command appearance.</summary>
    public RibbonCommandAppearanceSettings() { }
    /// <summary>Gets optional state corner radius in logical pixels, 0..64. Null retains square neutral chrome or the active Office radius. Hit targets and split layout are unchanged.</summary>
    public int? CornerRadius { get; init; }
    /// <summary>Gets optional state-outline width in logical pixels, 0..32. Null retains one device pixel when the recipe supplies border ink; zero hides the outline. Width alone does not invent ink.</summary>
    public int? BorderWidth { get; init; }
    /// <summary>Gets optional state-outline ink, including alpha. Empty retains the current recipe's border, including borderless ordinary states.</summary>
    public Color BorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional state inset inside the unchanged target in logical pixels, 0..1024. Null retains one device pixel; zero fills the target. Authored insets clamp to available bounds.</summary>
    public int? Inset { get; init; }
    /// <summary>Gets optional active split separator width in logical pixels, 0..32. Null retains one device pixel; zero hides it. Action/dropdown target geometry is unchanged.</summary>
    public int? SplitSeparatorWidth { get; init; }
    /// <summary>Gets optional split separator end clearance in logical pixels, 0..1024. Null follows the command inset, retaining one device pixel by default; explicit values reset independently.</summary>
    public int? SplitSeparatorInset { get; init; }
    /// <summary>Gets optional split separator ink, including alpha. Empty follows Border; Transparent hides its paint.</summary>
    public Color SplitSeparatorColor { get; init; } = Color.Empty;
    /// <summary>Gets optional resting-half fill while the other split half is active. Null retains each ordinary/Quick Access recipe, including material transparency. Authored fills retain alpha and precede material fallback. Hover, press and checked fills keep their existing settings.</summary>
    public RibbonFill? SplitCompanionBackground { get; init; }
    internal bool HasChromeOverrides => StateChrome?.HasOverrides == true || CornerRadius is not null || BorderWidth is not null || !BorderColor.IsEmpty || Inset is not null;
    internal static void Validate(RibbonCommandAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.CornerRadius is < 0 or > 64 || settings.BorderWidth is < 0 or > 32 || settings.SplitSeparatorWidth is < 0 or > 32 || settings.Inset is < 0 or > 1024 || settings.SplitSeparatorInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(settings), "Command radius must be 0..64, border/separator widths 0..32 and insets 0..1024 logical pixels, or null for existing recipes.");
    }
}
