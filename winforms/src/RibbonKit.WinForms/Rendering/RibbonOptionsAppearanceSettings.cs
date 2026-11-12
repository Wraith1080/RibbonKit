namespace RibbonKit.WinForms;

/// <summary>Immutable library Options navigation and divider decoration. Existing palette, marker corners, native controls, dialog layout and draft lifetime remain independent.</summary>
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

    internal static void Validate(RibbonOptionsAppearanceSettings? a)
    {
        if (a is null) return;
        if (a.NavigationCornerRadius is < 0 or > 64 || a.NavigationMarkerWidth is < 0 or > 64 || a.DividerWidth is < 0 or > 32
            || a.NavigationSelectionInset is < 0 or > 1024 || a.NavigationTextPadding is < 0 or > 1024 || a.NavigationMarkerInset is < 0 or > 1024 || a.NavigationFocusInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(a), "Options corners/marker width must be 0..64, divider width 0..32 and insets/padding 0..1024 logical pixels, or null for existing presentation.");
    }
}
