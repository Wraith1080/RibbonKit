namespace RibbonKit.WinForms;

/// <summary>Immutable Backstage marker, Back glyph, navigation-state and separate-caption tint overrides. Null members retain the active renderer's defaults. Assign through RibbonTheme.BackstageAppearance.</summary>
public sealed record RibbonBackstageAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing appearance.</summary>
    public RibbonBackstageAppearanceSettings() { }
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
    /// <summary>Gets navigation-state outline ink, including alpha. Empty retains the transparent default; width alone does not invent an outline color.</summary>
    public Color NavigationBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional neutral separate-caption tint eligibility. Null retains the family default (Modern true, Classic false). False disables tint. Full-height panes and authored Form/theme caption colors retain precedence.</summary>
    public bool? CaptionTintEnabled { get; init; }
    /// <summary>Gets optional separate-caption tint color, including alpha. Empty follows navigation color with its existing coverage; Transparent hides tint. Full-height pane paint is unaffected. TitleBarForeground can author caption ink.</summary>
    public Color CaptionTintColor { get; init; } = Color.Empty;
    /// <summary>Gets optional separate-caption tint strength, 0..1. Null uses 1. Multiplies pane alpha, material navigation opacity and authored tint-color alpha; zero hides tint. Must be finite. Does not change motion or RibbonForm.TitleBarTintOpacity.</summary>
    public double? CaptionTintOpacity { get; init; }

    internal bool HasNavigationOverrides => NavigationCornerRadius is not null || NavigationBorderWidth is not null || !NavigationBorderColor.IsEmpty;
    internal static void Validate(RibbonBackstageAppearanceSettings? settings)
    {
        if (settings is null) return;
        int?[] spacing = [settings.MarkerLeftInset, settings.MarkerWidth, settings.MarkerTopInset, settings.MarkerBottomInset, settings.BackVisualInset];
        if (spacing.Any(value => value is < 0 or > 1024) || settings.NavigationCornerRadius is < 0 or > 64 || settings.NavigationBorderWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(settings), "Backstage appearance spacing must be 0..1024, radius 0..64 and border width 0..32 logical pixels, or null for existing appearance.");
        Check(settings.BackOutlineWidth, 32); Check(settings.BackGlyphStrokeWidth, 32); Check(settings.BackGlyphScale, 8); Check(settings.CaptionTintOpacity, 1);
        static void Check(double? value, double maximum)
        {
            if (value is { } number && (!double.IsFinite(number) || number < 0 || number > maximum))
                throw new ArgumentOutOfRangeException(nameof(settings), "Backstage outline/glyph/tint values must be finite and within their documented range, or null for existing appearance.");
        }
    }
}
