namespace RibbonKit.WinForms;

/// <summary>Immutable message-panel decoration. Null members preserve existing renderer behavior; content, measured targets and application actions retain their owners.</summary>
public sealed record RibbonMessageAppearanceSettings
{
    /// <summary>Initializes overrides that preserve existing message presentation.</summary>
    public RibbonMessageAppearanceSettings() { }
    /// <summary>Gets whether MessageBackground restarts across each measured row. Null/false retains the full-stack fill. Exact stops, direction and alpha are preserved.</summary>
    public bool? BackgroundPerRow { get; init; }
    /// <summary>Gets the independent message-frame radius in logical pixels, 0..64. Null follows the active renderer's body radius. Connected top/bottom edges remain square.</summary>
    public int? CornerRadius { get; init; }
    /// <summary>Gets whether message titles use bold measurement and paint. Null/true retains bold; false uses the ribbon font unchanged.</summary>
    public bool? TitleBold { get; init; }
    /// <summary>Gets whether the default shield includes an exclamation mark. Null/false retains the existing outline. Borrowed message icons/images remain authoritative.</summary>
    public bool? ShowDefaultIconExclamation { get; init; }
    /// <summary>Gets the idle action fill, including alpha and exact stops. Null follows Surface; native controls are unaffected.</summary>
    public RibbonFill? ActionBackground { get; init; }
    /// <summary>Gets the action hover fill. Null follows Hover; disabled actions use their idle fill.</summary>
    public RibbonFill? ActionHoverBackground { get; init; }
    /// <summary>Gets the action pressed fill. Null follows Pressed.</summary>
    public RibbonFill? ActionPressedBackground { get; init; }
    /// <summary>Gets action text ink. Empty follows MessageForeground; Transparent hides enabled ink. DisabledColor retains authority.</summary>
    public Color ActionForeground { get; init; } = Color.Empty;
    /// <summary>Gets the action radius in logical pixels, 0..64. Null preserves existing command chrome.</summary>
    public int? ActionCornerRadius { get; init; }
    /// <summary>Gets the action outline width in logical pixels, 0..32. Null preserves one device pixel for explicit message decoration; zero hides it.</summary>
    public int? ActionBorderWidth { get; init; }
    /// <summary>Gets independent action outer/inner state rims. Null retains the existing Border fallback. Empty/Transparent/zero roles reset independently.</summary>
    public RibbonStateChromeSettings? ActionStateChrome { get; init; }
    /// <summary>Gets the close-state radius in logical pixels, 0..64. Null preserves existing command chrome.</summary>
    public int? CloseCornerRadius { get; init; }
    /// <summary>Gets the close-state outline width in logical pixels, 0..32. Null preserves one device pixel for explicit message decoration; zero hides it. Idle remains unpainted.</summary>
    public int? CloseBorderWidth { get; init; }
    /// <summary>Gets independent close hover/pressed outer/inner rims. Null follows MessageBorder. Idle remains unpainted.</summary>
    public RibbonStateChromeSettings? CloseStateChrome { get; init; }

    internal bool HasActionChrome => ActionBackground is not null || ActionHoverBackground is not null || ActionPressedBackground is not null
        || ActionCornerRadius is not null || ActionBorderWidth is not null || ActionStateChrome is not null;
    internal bool HasCloseChrome => CloseCornerRadius is not null || CloseBorderWidth is not null || CloseStateChrome is not null;
    internal static void Validate(RibbonMessageAppearanceSettings? value)
    {
        if (value is null) return;
        if (value.CornerRadius is < 0 or > 64 || value.ActionCornerRadius is < 0 or > 64 || value.CloseCornerRadius is < 0 or > 64
            || value.ActionBorderWidth is < 0 or > 32 || value.CloseBorderWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(value), "Message radii must be 0..64 and outlines 0..32 logical pixels, or null.");
        RibbonStateChromeSettings.Validate(value.ActionStateChrome); RibbonStateChromeSettings.Validate(value.CloseStateChrome);
    }
}
