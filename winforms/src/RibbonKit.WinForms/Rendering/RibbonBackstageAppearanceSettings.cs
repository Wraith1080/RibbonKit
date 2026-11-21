namespace RibbonKit.WinForms;

/// <summary>Immutable Backstage frame, marker, Back target/glyph, navigation-state and separate-caption tint overrides. Null members retain the active family's defaults. Assign through RibbonTheme.BackstageAppearance.</summary>
public sealed record RibbonBackstageAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing appearance.</summary>
    public RibbonBackstageAppearanceSettings() { }
    /// <summary>Gets optional shared-frame/header overrides for Office2007 Backstage. Null resets these roles independently; other styles retain their rendering.</summary>
    public RibbonBackstageWorkspaceSettings? Workspace { get; init; }
    /// <summary>Gets optional Back target visibility. Null follows the family: hidden for Office2007/Office2010, visible otherwise. Hiding removes its layout space, keyboard target and KeyTip; Office2007 retains its File Orb return.</summary>
    public bool? BackButtonVisible { get; init; }
    /// <summary>Gets top/bottom hover edge ink, including alpha. Empty/Transparent adds no edge; full navigation outlines remain independent.</summary>
    public Color NavigationHoverEdgeColor { get; init; } = Color.Empty;
    /// <summary>Gets optional top/bottom hover edge width in logical pixels, 0..32. Null uses one device pixel when edge ink exists; zero hides it.</summary>
    public int? NavigationHoverEdgeWidth { get; init; }
    /// <summary>Gets optional full-width top seam height in logical pixels, 0..32. Null uses 3 for Office2010 and 0 otherwise. Zero removes its layout/paint space.</summary>
    public int? TopSeamHeight { get; init; }
    /// <summary>Gets an optional seam fill, including alpha/gradients. Null follows the effective open File fill's bottom color. Transparent hides ink while retaining height.</summary>
    public RibbonFill? TopSeamBackground { get; init; }
    /// <summary>Gets optional page-divider shadow width in logical pixels, 0..64. Null uses 6 for Office2010 and 0 otherwise. The shadow lies on the navigation side below the seam, without moving row targets.</summary>
    public int? PageDividerShadowWidth { get; init; }
    /// <summary>Gets page-divider shadow ink, including alpha. Empty uses Black; Transparent hides it.</summary>
    public Color PageDividerShadowColor { get; init; } = Color.Empty;
    /// <summary>Gets optional page-divider shadow strength, finite 0..1. Null uses 0.18; zero hides it independently of width and ink.</summary>
    public double? PageDividerShadowOpacity { get; init; }
    /// <summary>Gets optional marker left clearance in logical pixels, 0..1024. Null uses 2; available row bounds still clip painting.</summary>
    public int? MarkerLeftInset { get; init; }
    /// <summary>Gets optional marker width in logical pixels, 0..1024. Null uses 3; zero hides the marker without changing selection or hit targets.</summary>
    public int? MarkerWidth { get; init; }
    /// <summary>Gets optional marker top clearance in logical pixels, 0..1024. Null uses 10. Top and bottom clearances are summed before DPI rounding.</summary>
    public int? MarkerTopInset { get; init; }
    /// <summary>Gets optional marker bottom clearance in logical pixels, 0..1024. Null uses 10. Short-row safety clipping remains effective.</summary>
    public int? MarkerBottomInset { get; init; }
    /// <summary>Gets marker ink, including alpha. Empty follows Accent; Transparent hides its paint. Visibility and NavigationMarkerCornerRadius remain independent.</summary>
    public Color MarkerColor { get; init; } = Color.Empty;
    /// <summary>Gets optional Back circle clearance inside its click target in logical pixels, 0..1024. Null uses 2; target geometry and corner-radius settings remain independent.</summary>
    public int? BackVisualInset { get; init; }
    /// <summary>Gets optional Back circle outline thickness in logical pixels, 0..32. Null preserves the rounded 1.5-pixel default and minimum one device pixel; zero hides the outline. Must be finite.</summary>
    public double? BackOutlineWidth { get; init; }
    /// <summary>Gets optional Back arrow scale, 0..8. Null uses 1, retaining separately rounded 5/6-pixel vector extents. Zero hides the arrow; the click target and circle stay independent. Must be finite.</summary>
    public double? BackGlyphScale { get; init; }
    /// <summary>Gets optional Back arrow stroke in logical pixels, 0..32. Null preserves the 1.5-pixel default and minimum one device pixel; zero hides the arrow. Must be finite.</summary>
    public double? BackGlyphStrokeWidth { get; init; }
    /// <summary>Gets optional hover/selected/pressed navigation-state radius in logical pixels, 0..64. Null retains square full-width rows or the active renderer's inset chrome.</summary>
    public int? NavigationCornerRadius { get; init; }
    /// <summary>Gets optional navigation-state outline width in logical pixels, 0..32. Null retains the active primitive's one-device-pixel outline when a border color is authored; zero hides it. Coverage and click targets remain independent.</summary>
    public int? NavigationBorderWidth { get; init; }
    /// <summary>Gets navigation-state outline ink, including alpha. Empty follows the family: Office2010 derives selected/pressed borders from IdentityAccent; existing families remain transparent. Transparent hides the outline.</summary>
    public Color NavigationBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional neutral separate-caption tint eligibility. Null retains the family default (Modern true, Classic/Office2010 false). False disables tint. Full-height panes and authored Form/theme caption colors retain precedence.</summary>
    public bool? CaptionTintEnabled { get; init; }
    /// <summary>Gets optional separate-caption tint color, including alpha. Empty follows navigation color with its existing coverage; Transparent hides tint. Full-height pane paint is unaffected. TitleBarForeground can author caption ink.</summary>
    public Color CaptionTintColor { get; init; } = Color.Empty;
    /// <summary>Gets optional separate-caption tint strength, 0..1. Null uses 1. Multiplies pane alpha, material navigation opacity and authored tint-color alpha; zero hides tint. Must be finite. Does not change motion or RibbonForm.TitleBarTintOpacity.</summary>
    public double? CaptionTintOpacity { get; init; }

    internal bool HasNavigationOverrides => NavigationCornerRadius is not null || NavigationBorderWidth is not null || !NavigationBorderColor.IsEmpty;
    internal static void Validate(RibbonBackstageAppearanceSettings? settings)
    {
        if (settings is null) return;
        RibbonBackstageWorkspaceSettings.Validate(settings.Workspace);
        int?[] spacing = [settings.MarkerLeftInset, settings.MarkerWidth, settings.MarkerTopInset, settings.MarkerBottomInset, settings.BackVisualInset];
        if (spacing.Any(value => value is < 0 or > 1024) || settings.NavigationCornerRadius is < 0 or > 64 || settings.NavigationBorderWidth is < 0 or > 32
            || settings.TopSeamHeight is < 0 or > 32 || settings.PageDividerShadowWidth is < 0 or > 64 || settings.NavigationHoverEdgeWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(settings), "Backstage spacing must be 0..1024, radius/shadow width 0..64 and border/seam width 0..32 logical pixels, or null for family defaults.");
        Check(settings.BackOutlineWidth, 32); Check(settings.BackGlyphStrokeWidth, 32); Check(settings.BackGlyphScale, 8); Check(settings.CaptionTintOpacity, 1); Check(settings.PageDividerShadowOpacity, 1);
        static void Check(double? value, double maximum)
        {
            if (value is { } number && (!double.IsFinite(number) || number < 0 || number > maximum))
                throw new ArgumentOutOfRangeException(nameof(settings), "Backstage outline/glyph/tint/shadow values must be finite and within their documented range, or null for family defaults.");
        }
    }
}
