namespace RibbonKit.WinForms;

/// <summary>Immutable ScreenTip title, KeyTip outline and Backstage badge-placement overrides. Existing colors, corners, padding, keyboard routing and passive-window lifetime remain independent.</summary>
public sealed record RibbonTipAppearanceSettings
{
    /// <summary>Initializes overrides retaining existing tip presentation.</summary>
    public RibbonTipAppearanceSettings() { }
    /// <summary>Gets an optional opaque ScreenTip frame fill, including exact gradients. Null retains PopupBackground/Surface. Native and layered frame hooks receive IsScreenTip; ordinary menus and KeyTips retain their own fills.</summary>
    public RibbonFill? ScreenTipBackground { get; init; }
    /// <summary>Gets ScreenTip frame ink, including alpha. Empty retains PopupBorder; Transparent hides ink. Existing popup width/radius hooks remain authoritative.</summary>
    public Color ScreenTipBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional ScreenTip title weight. Null/true adds Bold to host styles; false removes Bold while preserving other styles. Measurement and paint use the same weight.</summary>
    public bool? ScreenTipTitleBold { get; init; }
    /// <summary>Gets optional KeyTip paint inset in logical pixels, 0..1024. Null retains zero. Insets clamp inside the badge; text slots and command targets do not move.</summary>
    public int? KeyTipInset { get; init; }
    /// <summary>Gets optional KeyTip outline width in logical pixels, 0..32. Null retains one device pixel; zero hides the outline. Existing KeyTipBorder ink, including alpha, remains authoritative.</summary>
    public int? KeyTipBorderWidth { get; init; }
    /// <summary>Gets optional Backstage KeyTip right-edge clearance in logical pixels, 0..1024. Null retains 4. Placement clamps inside the unchanged entry target.</summary>
    public int? BackstageKeyTipRightClearance { get; init; }
    /// <summary>Gets optional total width deduction from the Backstage entry when fitting a KeyTip, in logical pixels, 0..1024. Null retains 8. Zero permits the full entry width; consuming the width hides the badge.</summary>
    public int? BackstageKeyTipHorizontalClearance { get; init; }
    /// <summary>Gets optional total height deduction from the Backstage entry when fitting a KeyTip, in logical pixels, 0..1024. Null retains 4. Zero permits the full entry height; consuming the height hides the badge.</summary>
    public int? BackstageKeyTipVerticalClearance { get; init; }

    internal static void Validate(RibbonTipAppearanceSettings? a)
    {
        if (a is null) return;
        if (a.ScreenTipBackground is { } fill && !RibbonFillColors.IsOpaque(fill))
            throw new ArgumentException("Every ScreenTip frame fill stop must be opaque.", nameof(a));
        if (a.KeyTipInset is < 0 or > 1024 || a.KeyTipBorderWidth is < 0 or > 32 || a.BackstageKeyTipRightClearance is < 0 or > 1024
            || a.BackstageKeyTipHorizontalClearance is < 0 or > 1024 || a.BackstageKeyTipVerticalClearance is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(a), "Tip insets/clearances must be 0..1024 and outline width 0..32 logical pixels, or null for existing presentation.");
    }
}
