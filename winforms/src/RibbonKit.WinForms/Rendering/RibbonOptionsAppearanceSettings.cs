namespace RibbonKit.WinForms;

/// <summary>Immutable library Options navigation, divider, button, heading, frame, list/tree and group icon-picker presentation. Native controls, application pages and draft lifetime remain independent.</summary>
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
    /// <summary>Gets optional button outline width in logical pixels, 0..32. Null retains ordinary one-device-pixel outlines when ink exists and zero for accent buttons, except the accent-palette pressed state uses one device pixel. Zero hides the outline. Authoring ordinary-button paint values opts those buttons into shared painting when their corner setting is null.</summary>
    public int? ButtonBorderWidth { get; init; }
    /// <summary>Gets optional button outline ink, including alpha. Empty retains PopupBorder for ordinary buttons or the current fill's first color for accent buttons; an enabled pressed button uses Accent when OptionsButtonAccentStates is enabled. Transparent hides the outline.</summary>
    public Color ButtonBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional native button focus-rectangle inset in logical pixels, 0..1024. Null retains four device pixels. Authored values clamp inside the unchanged button.</summary>
    public int? ButtonFocusInset { get; init; }
    /// <summary>Gets an optional enabled idle ordinary-button fill, including alpha/gradients. Null retains Surface. Existing Hover/Pressed/disabled recipes remain. Authoring an ordinary-button override opts that button into shared painting when its corner setting is null.</summary>
    public RibbonFill? ButtonBackground { get; init; }
    /// <summary>Gets an optional enabled idle accent-button fill, including alpha/gradients. Null retains Accent. Default hover/press darken this fill without changing alpha or gradient stops.</summary>
    public RibbonFill? AccentButtonBackground { get; init; }
    /// <summary>Gets an optional primary-button hover fill, including alpha/gradients. Null uses an explicit Checked override or darkens the idle fill when OptionsButtonAccentStates is enabled.</summary>
    public RibbonFill? AccentButtonHoverBackground { get; init; }
    /// <summary>Gets an optional primary-button pressed fill, including alpha/gradients. Null uses an explicit Pressed override or darkens the idle fill when OptionsButtonAccentStates is enabled.</summary>
    public RibbonFill? AccentButtonPressedBackground { get; init; }
    /// <summary>Gets primary-button hover darkening, a finite fraction 0..1. Null uses 0.10; zero retains the idle fill. Independent of ordinary-button accent washes.</summary>
    public double? AccentButtonHoverDarkening { get; init; }
    /// <summary>Gets primary-button pressed darkening, a finite fraction 0..1. Null uses 0.20; zero retains the idle fill. Explicit state fills take precedence.</summary>
    public double? AccentButtonPressedDarkening { get; init; }
    /// <summary>Gets enabled primary-button ink in all states, including alpha. Empty retains Surface ink for the accent palette (white by default) or the legacy state foreground when that palette is null. Disabled ink remains independent.</summary>
    public Color AccentButtonForeground { get; init; } = Color.Empty;
    /// <summary>Gets an optional heading font-size increment in points, finite 0..64. Null retains 5. The resulting size also respects HeadingMinimumFontSize. Existing construction/load/font/DPI layout resolves heading settings; choose them before opening a dialog.</summary>
    public float? HeadingFontSizeIncrement { get; init; }
    /// <summary>Gets an optional minimum heading font size in points, finite 1..128. Null retains 14. The host font and increment may produce a larger size.</summary>
    public float? HeadingMinimumFontSize { get; init; }
    /// <summary>Gets optional heading weight. Null retains legacy Bold-only style; explicit true adds Bold to host styles, false removes Bold while preserving other host styles.</summary>
    public bool? HeadingBold { get; init; }
    /// <summary>Gets optional spacing below the heading in logical pixels, 0..1024. Null retains 12. Existing native heading/page layout consumes the padding at construction, load, font and DPI refresh.</summary>
    public int? HeadingBottomPadding { get; init; }
    /// <summary>Gets optional native field/list wrapper corner radius in logical pixels, 0..64. Null retains the actual popup-radius/command-radius minimum. Native list child regions use the same resolved radius and content clearance.</summary>
    public int? FrameCornerRadius { get; init; }
    /// <summary>Gets optional native field/list wrapper outline width in logical pixels, 0..32. Null retains one device pixel. Zero hides paint while keeping one-device-pixel native clearance. Wider outlines reserve native content space and search-field height through existing layout paths; choose settings before opening.</summary>
    public int? FrameBorderWidth { get; init; }
    /// <summary>Gets optional native field/list wrapper outline ink, including alpha. Empty retains PopupBorder; Transparent hides paint without reclaiming its reserved native clearance. Surface and native child backgrounds retain their existing owners.</summary>
    public Color FrameBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional command-list row horizontal text/content padding in logical pixels, 0..1024. Null retains 6 for QAT/Classic and 8 for Simplified, with legacy ceiling rounding. Authored padding clamps inside the unchanged native row.</summary>
    public int? RowTextPadding { get; init; }
    /// <summary>Gets optional command-list icon size in logical pixels, 0..256. Null retains 20 with legacy ceiling rounding. Authored sizes clamp inside the row; zero hides the borrowed icon and reclaims its slot and gap. Native row height and tree/icon-picker slots do not change.</summary>
    public int? RowIconSize { get; init; }
    /// <summary>Gets optional command-list icon-to-text gap in logical pixels, 0..1024. Null retains 6 for QAT/Classic and 8 for Simplified, with legacy ceiling rounding. The gap clamps to remaining content space; zero removes it.</summary>
    public int? RowIconTextGap { get; init; }
    /// <summary>Gets optional command-list selection corner radius in logical pixels, 0..64. Null retains square selection. Native row targets and focus rectangles remain unchanged.</summary>
    public int? RowCornerRadius { get; init; }
    /// <summary>Gets optional command-list selected fill, including alpha/gradients. Null retains the solid Checked.Start recipe. Authored selection paint composites over Surface; Transparent hides it. Existing Foreground and unselected Surface roles remain authoritative.</summary>
    public RibbonFill? RowSelectedBackground { get; init; }
    /// <summary>Gets optional Simplified command-list heading weight. Null retains legacy Bold-only style; true adds Bold to host styles, false removes it while preserving other styles. Choose before opening; existing construction/font/DPI layout owns the font.</summary>
    public bool? RowHeadingBold { get; init; }
    /// <summary>Gets optional customization-tree selection corner radius in logical pixels, 0..64. Null retains square selection. Native row/check/expand targets and scrolling remain unchanged.</summary>
    public int? TreeCornerRadius { get; init; }
    /// <summary>Gets optional customization-tree selected fill, including alpha/gradients. Null retains solid Checked.Start. Authored selection paint composites over Surface; Transparent hides it. Command-list selection remains independent.</summary>
    public RibbonFill? TreeSelectedBackground { get; init; }
    /// <summary>Gets optional customization-tree checkbox size in logical pixels, 0..64. Null retains Compact 18 / Touch 24 and existing slot clearance. Authored sizes clamp within the existing native check slot and row; zero hides paint while retaining the check target and actual indicator renderer hook.</summary>
    public int? TreeCheckSize { get; init; }
    /// <summary>Gets optional customization-tree icon size in logical pixels, 0..256. Null retains 16. Authored size/gap fits within the existing icon reservation and row without moving native text or hit targets; zero hides borrowed artwork.</summary>
    public int? TreeIconSize { get; init; }
    /// <summary>Gets optional customization-tree icon-to-text paint gap in logical pixels, 0..1024. Null retains 2. Authored size/gap clamps within the existing reservation to the left of native text; zero removes the gap. Native text bounds, indentation and keyboard targets do not move.</summary>
    public int? TreeIconTextGap { get; init; }
    /// <summary>Gets optional customization-tree native focus-rectangle inset in logical pixels, 0..1024. Null retains one device pixel. Authored values clamp inside the existing row. Focus style and cue eligibility remain native.</summary>
    public int? TreeFocusInset { get; init; }
    /// <summary>Gets optional group icon-picker selection corner radius in logical pixels, 0..64. Null retains the actual popup/command-radius minimum. None and artwork share the same square cell and selection bounds.</summary>
    public int? IconPickerCornerRadius { get; init; }
    /// <summary>Gets optional group icon-picker selection paint inset in logical pixels, 0..1024. Null retains one device pixel. Authored inset clamps inside the existing cell; zero removes it. The same inset and outline clearance constrain artwork and None text.</summary>
    public int? IconPickerInset { get; init; }
    /// <summary>Gets optional group icon-picker selection outline width in logical pixels, 0..32. Null retains one device pixel. Zero hides the outline; authored widths clamp inside the selection paint bounds.</summary>
    public int? IconPickerBorderWidth { get; init; }
    /// <summary>Gets optional group icon-picker selection outline ink, including alpha. Empty retains Border; Transparent hides the outline.</summary>
    public Color IconPickerBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional group icon-picker selected fill, including alpha/gradients. Null retains Checked. Surface and Foreground keep their existing roles; a Transparent fill preserves an independently configured outline.</summary>
    public RibbonFill? IconPickerSelectedBackground { get; init; }
    /// <summary>Gets optional group icon-picker artwork size in logical pixels, 0..256. Null retains Compact 24 / Touch 32 with legacy ceiling rounding. Positive authored sizes resize the common square cells for artwork and None, respecting font/density minima and the native 256-device-pixel cell limit. Zero hides artwork and retains the default cell footprint; item identity and selection remain.</summary>
    public int? IconPickerIconSize { get; init; }
    /// <summary>Gets optional group icon-picker None-label paint inset in logical pixels, 0..1024. Null retains zero. Authored inset applies inside the shared selection/content clearance, so None follows tile size and selection inset. Font, ink, centering and focus policy retain their owners.</summary>
    public int? IconPickerTextInset { get; init; }

    internal bool HasButtonOverrides(bool accent) => ButtonInset is not null || ButtonBorderWidth is not null || !ButtonBorderColor.IsEmpty || ButtonFocusInset is not null || (accent
        ? AccentButtonBackground is not null || AccentButtonHoverBackground is not null || AccentButtonPressedBackground is not null || AccentButtonHoverDarkening is not null || AccentButtonPressedDarkening is not null || !AccentButtonForeground.IsEmpty
        : ButtonBackground is not null);

    internal static void Validate(RibbonOptionsAppearanceSettings? a)
    {
        if (a is null) return;
        if (a.AccentButtonHoverDarkening is { } hover && (!double.IsFinite(hover) || hover < 0 || hover > 1)
            || a.AccentButtonPressedDarkening is { } press && (!double.IsFinite(press) || press < 0 || press > 1))
            throw new ArgumentOutOfRangeException(nameof(a), "Primary button darkening must be a finite fraction from 0 to 1, or null for the default.");
        if (a.IconPickerCornerRadius is < 0 or > 64 || a.IconPickerInset is < 0 or > 1024 || a.IconPickerBorderWidth is < 0 or > 32 || a.IconPickerIconSize is < 0 or > 256 || a.IconPickerTextInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(a), "Options icon-picker radius must be 0..64, paint/text insets 0..1024, border width 0..32 and artwork size 0..256 logical pixels, or null for existing presentation.");
        if (a.TreeCornerRadius is < 0 or > 64 || a.TreeCheckSize is < 0 or > 64 || a.TreeIconSize is < 0 or > 256 || a.TreeIconTextGap is < 0 or > 1024 || a.TreeFocusInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(a), "Options tree corners/check size must be 0..64, icon size 0..256 and icon gap/focus inset 0..1024 logical pixels, or null for existing presentation.");
        if (a.RowTextPadding is < 0 or > 1024 || a.RowIconTextGap is < 0 or > 1024 || a.RowIconSize is < 0 or > 256 || a.RowCornerRadius is < 0 or > 64)
            throw new ArgumentOutOfRangeException(nameof(a), "Options row padding/gap must be 0..1024, icon size 0..256 and corners 0..64 logical pixels, or null for existing presentation.");
        if (a.FrameCornerRadius is < 0 or > 64 || a.FrameBorderWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(a), "Options frame radius must be 0..64 and outline width 0..32 logical pixels, or null for existing presentation.");
        if (a.NavigationCornerRadius is < 0 or > 64 || a.NavigationMarkerWidth is < 0 or > 64 || a.DividerWidth is < 0 or > 32
            || a.NavigationSelectionInset is < 0 or > 1024 || a.NavigationTextPadding is < 0 or > 1024 || a.NavigationMarkerInset is < 0 or > 1024 || a.NavigationFocusInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(a), "Options corners/marker width must be 0..64, divider width 0..32 and insets/padding 0..1024 logical pixels, or null for existing presentation.");
        if (a.ButtonInset is < 0 or > 1024 || a.ButtonFocusInset is < 0 or > 1024 || a.HeadingBottomPadding is < 0 or > 1024 || a.ButtonBorderWidth is < 0 or > 32
            || a.HeadingFontSizeIncrement is { } increment && (!float.IsFinite(increment) || increment < 0 || increment > 64)
            || a.HeadingMinimumFontSize is { } minimum && (!float.IsFinite(minimum) || minimum < 1 || minimum > 128))
            throw new ArgumentOutOfRangeException(nameof(a), "Options button insets must be 0..1024, outline width 0..32 logical pixels, heading increment finite 0..64 and minimum finite 1..128 points, or null for existing presentation.");
    }
}
