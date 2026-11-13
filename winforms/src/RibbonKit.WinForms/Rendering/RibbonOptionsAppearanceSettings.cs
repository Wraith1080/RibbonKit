namespace RibbonKit.WinForms;

/// <summary>Immutable library Options navigation, divider, button and heading presentation. Native controls, application pages and draft lifetime remain independent.</summary>
public sealed record RibbonOptionsAppearanceSettings
{
    /// <summary>Initializes overrides retaining existing Options presentation.</summary>
    public RibbonOptionsAppearanceSettings() { }
    /// <summary>Gets optional selected navigation corner radius in logical pixels, 0..64. Null uses the actual renderer's GetPopupCornerRadius hook.</summary>
    public int? NavigationCornerRadius { get; init; }
    /// <summary>Gets optional navigation selection inset at each vertical edge in logical pixels, 0..1024. Null retains 2; authored values clamp inside the unchanged row.</summary>
    public int? NavigationSelectionInset { get; init; }
    /// <summary>Gets optional navigation text padding at each horizontal edge in logical pixels, 0..1024. Null retains 12. Authored values clamp inside the row without changing its native target.</summary>
    public int? NavigationTextPadding { get; init; }
    /// <summary>Gets an optional selected navigation fill, including alpha/gradients. Null retains Checked. The neutral rail background and existing Accent/Foreground text roles remain authoritative.</summary>
    public RibbonFill? NavigationSelectedBackground { get; init; }
    /// <summary>Gets optional selected navigation marker width in logical pixels, 0..64. Null retains 3; zero hides the marker without changing selection or its target.</summary>
    public int? NavigationMarkerWidth { get; init; }
    /// <summary>Gets optional marker inset at each vertical edge in logical pixels, 0..1024. Null retains the legacy 7 top / summed 14 height deduction with existing DPI rounding. Authored values use the same inset at both ends; consuming the height hides the marker.</summary>
    public int? NavigationMarkerInset { get; init; }
    /// <summary>Gets an optional navigation marker fill, including alpha/gradients. Null retains Accent. NavigationMarkerCornerRadius keeps its existing role.</summary>
    public RibbonFill? NavigationMarkerFill { get; init; }
    /// <summary>Gets optional native navigation focus-rectangle inset in logical pixels, 0..1024. Null retains one device pixel; authored values clamp inside the row. Native focus style and keyboard behavior remain unchanged.</summary>
    public int? NavigationFocusInset { get; init; }
    /// <summary>Gets optional rail/footer divider width in logical pixels, 0..32. Null retains the one-device-pixel line; zero hides it. Authored stripes clamp inside the existing panel without changing layout.</summary>
    public int? DividerWidth { get; init; }
    /// <summary>Gets optional rail/footer divider ink, including alpha. Empty retains Border; Transparent hides paint.</summary>
    public Color DividerColor { get; init; } = Color.Empty;
    /// <summary>Gets optional button paint inset in logical pixels, 0..1024. Null retains zero. Authored values clamp inside the unchanged native button; content and hit targets do not move.</summary>
    public int? ButtonInset { get; init; }
    /// <summary>Gets optional button outline width in logical pixels, 0..32. Null retains ordinary one-device-pixel outlines when ink exists and zero for accent buttons. Zero hides the outline. Authoring ordinary-button paint values opts those buttons into shared painting when their corner setting is null.</summary>
    public int? ButtonBorderWidth { get; init; }
    /// <summary>Gets optional button outline ink, including alpha. Empty retains PopupBorder for ordinary buttons or the current fill's first color for accent buttons. Transparent hides the outline.</summary>
    public Color ButtonBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional native button focus-rectangle inset in logical pixels, 0..1024. Null retains four device pixels. Authored values clamp inside the unchanged button.</summary>
    public int? ButtonFocusInset { get; init; }
    /// <summary>Gets an optional enabled idle ordinary-button fill, including alpha/gradients. Null retains Surface. Existing Hover/Pressed/disabled recipes remain. Authoring an ordinary-button override opts that button into shared painting when its corner setting is null.</summary>
    public RibbonFill? ButtonBackground { get; init; }
    /// <summary>Gets an optional enabled idle accent-button fill, including alpha/gradients. Null retains Accent. Existing Hover/Pressed/disabled recipes and foreground roles remain.</summary>
    public RibbonFill? AccentButtonBackground { get; init; }
    /// <summary>Gets an optional heading font-size increment in points, finite 0..64. Null retains 5. The resulting size also respects HeadingMinimumFontSize. Existing construction/load/font/DPI layout resolves heading settings; choose them before opening a dialog.</summary>
    public float? HeadingFontSizeIncrement { get; init; }
    /// <summary>Gets an optional minimum heading font size in points, finite 1..128. Null retains 14. The host font and increment may produce a larger size.</summary>
    public float? HeadingMinimumFontSize { get; init; }
    /// <summary>Gets optional heading weight. Null retains legacy Bold-only style; explicit true adds Bold to host styles, false removes Bold while preserving other host styles.</summary>
    public bool? HeadingBold { get; init; }
    /// <summary>Gets optional spacing below the heading in logical pixels, 0..1024. Null retains 12. Existing native heading/page layout consumes the padding at construction, load, font and DPI refresh.</summary>
    public int? HeadingBottomPadding { get; init; }

    internal bool HasButtonOverrides(bool accent) => ButtonInset is not null || ButtonBorderWidth is not null || !ButtonBorderColor.IsEmpty || ButtonFocusInset is not null || (accent ? AccentButtonBackground is not null : ButtonBackground is not null);

    internal static void Validate(RibbonOptionsAppearanceSettings? a)
    {
        if (a is null) return;
        if (a.NavigationCornerRadius is < 0 or > 64 || a.NavigationMarkerWidth is < 0 or > 64 || a.DividerWidth is < 0 or > 32
            || a.NavigationSelectionInset is < 0 or > 1024 || a.NavigationTextPadding is < 0 or > 1024 || a.NavigationMarkerInset is < 0 or > 1024 || a.NavigationFocusInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(a), "Options corners/marker width must be 0..64, divider width 0..32 and insets/padding 0..1024 logical pixels, or null for existing presentation.");
        if (a.ButtonInset is < 0 or > 1024 || a.ButtonFocusInset is < 0 or > 1024 || a.HeadingBottomPadding is < 0 or > 1024 || a.ButtonBorderWidth is < 0 or > 32
            || a.HeadingFontSizeIncrement is { } increment && (!float.IsFinite(increment) || increment < 0 || increment > 64)
            || a.HeadingMinimumFontSize is { } minimum && (!float.IsFinite(minimum) || minimum < 1 || minimum > 128))
            throw new ArgumentOutOfRangeException(nameof(a), "Options button insets must be 0..1024, outline width 0..32 logical pixels, heading increment finite 0..64 and minimum finite 1..128 points, or null for existing presentation.");
    }
}
