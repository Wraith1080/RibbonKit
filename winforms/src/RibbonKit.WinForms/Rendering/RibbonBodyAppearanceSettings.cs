namespace RibbonKit.WinForms;

/// <summary>Immutable body/connected-frame decoration and join overrides. Null members retain existing renderer defaults; command layout and popup geometry remain shared.</summary>
public sealed record RibbonBodyAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing frame appearance.</summary>
    public RibbonBodyAppearanceSettings() { }
    /// <summary>Gets optional body-frame border thickness in logical pixels, 0..32. Null retains one device pixel; zero hides it. Existing BodyBorder/BodyBottomBorderOnly supply ink and edge policy.</summary>
    public int? BorderWidth { get; init; }
    /// <summary>Gets optional message outer-frame thickness in logical pixels, 0..32. Null retains one device pixel; zero hides it. MessageSideBordersVisible and connected-edge ownership remain authoritative.</summary>
    public int? MessageBorderWidth { get; init; }
    /// <summary>Gets optional Classic/Simplified group-divider thickness in logical pixels, 0..32. Null retains one device pixel; zero hides it without changing reduction, gaps or hit targets.</summary>
    public int? GroupSeparatorWidth { get; init; }
    /// <summary>Gets group-divider ink, including alpha. Empty follows Border; Transparent hides its paint.</summary>
    public Color GroupSeparatorColor { get; init; } = Color.Empty;
    /// <summary>Gets optional joined below-ribbon Quick Access separator thickness in logical pixels, 0..32. Null retains one device pixel; zero hides it.</summary>
    public int? QuickAccessSeparatorWidth { get; init; }
    /// <summary>Gets optional joined Quick Access separator side inset in logical pixels, 0..1024. Null retains zero for bottom-border-only bodies or one device pixel otherwise. Available row bounds clamp it.</summary>
    public int? QuickAccessSeparatorInset { get; init; }
    /// <summary>Gets joined Quick Access separator ink, including alpha. Empty follows the resolved body border; Transparent hides it.</summary>
    public Color QuickAccessSeparatorColor { get; init; } = Color.Empty;
    /// <summary>Gets optional below-ribbon Quick Access join preference. Null/true uses the active renderer's join capability; false separates it through the existing layout. A renderer override that disables joining remains authoritative.</summary>
    public bool? JoinBelowRibbonQuickAccess { get; init; }
    /// <summary>Gets optional message-to-body join preference. Null/true uses the active renderer's capability; false separates the stack. Existing Quick Access placement/join constraints remain authoritative.</summary>
    public bool? JoinMessageBarToBody { get; init; }
    internal static void Validate(RibbonBodyAppearanceSettings? settings)
    {
        if (settings is null) return;
        int?[] widths = [settings.BorderWidth, settings.MessageBorderWidth, settings.GroupSeparatorWidth, settings.QuickAccessSeparatorWidth];
        if (widths.Any(value => value is < 0 or > 32) || settings.QuickAccessSeparatorInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(settings), "Body/frame widths must be 0..32 and Quick Access separator inset 0..1024 logical pixels, or null for existing decoration.");
    }
}
