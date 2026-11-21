namespace RibbonKit.WinForms;

/// <summary>Immutable Office2007 Backstage workspace overrides. The shared frame encloses navigation and native pages; other Backstage styles ignore these roles.</summary>
public sealed record RibbonBackstageWorkspaceSettings
{
    /// <summary>Initializes overrides retaining the framed presentation defaults.</summary>
    public RibbonBackstageWorkspaceSettings() { }
    /// <summary>Gets header height in logical pixels, 0..1024. Null uses 48; Touch retains its minimum Back target height.</summary>
    public int? HeaderHeight { get; init; }
    /// <summary>Gets the header fill, including alpha and exact gradient stops. Alpha blends once over an opaque navigation-color backing. Null follows the effective File background.</summary>
    public RibbonFill? HeaderBackground { get; init; }
    /// <summary>Gets whether a matching colored standard caption shares one continuous vertical header gradient while Backstage is open. Null/true enables the join; false keeps separate canvases. Neutral captions, Aero frames, explicit header/Form caption overrides and top seams remain independent.</summary>
    public bool? JoinColoredCaption { get; init; }
    /// <summary>Gets header ink. Empty uses White; Transparent hides ink.</summary>
    public Color HeaderForeground { get; init; } = Color.Empty;
    /// <summary>Gets the localized return label. Null uses "Back to document"; empty hides the label while retaining its accessible Back target.</summary>
    public string? ReturnText { get; init; }
    /// <summary>Gets preferred return target width in logical pixels, 0..1024. Null uses 300. Available header space and the actual File visual clamp it.</summary>
    public int? ReturnWidth { get; init; }
    /// <summary>Gets the shared frame gutter on all four sides in logical pixels, 0..1024. Null uses 8. Available space clamps it symmetrically.</summary>
    public int? FrameInset { get; init; }
    /// <summary>Gets outer frame and single divider ink. Empty follows Border; Transparent hides ink while retaining clearance.</summary>
    public Color BorderColor { get; init; } = Color.Empty;
    /// <summary>Gets outer frame/divider width in logical pixels, 0..32. Null uses one device pixel; zero removes ink and clearance.</summary>
    public int? BorderWidth { get; init; }
    /// <summary>Gets the shared frame's inner highlight ink. Empty/Transparent hides ink while retaining independently authored clearance.</summary>
    public Color InnerBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets inner highlight width in logical pixels, 0..32. Null uses one device pixel; zero removes ink and clearance.</summary>
    public int? InnerBorderWidth { get; init; }
    /// <summary>Gets all four shared-frame corner radii in logical pixels, 0..64. Null uses 3; available bounds clamp it.</summary>
    public int? CornerRadius { get; init; }
    /// <summary>Gets optional gold/navigation-state border and inner-rim overrides. Null retains the shared navigation outline recipe.</summary>
    public RibbonStateChromeSettings? NavigationStateChrome { get; init; }
    /// <summary>Gets extra separator inset inside its already inset row in logical pixels, 0..1024. Null/zero aligns separators with navigation row edges.</summary>
    public int? SeparatorInset { get; init; }

    internal static void Validate(RibbonBackstageWorkspaceSettings? value)
    {
        if (value is null) return;
        if (value.HeaderHeight is < 0 or > 1024 || value.ReturnWidth is < 0 or > 1024 || value.FrameInset is < 0 or > 1024
            || value.SeparatorInset is < 0 or > 1024 || value.BorderWidth is < 0 or > 32 || value.InnerBorderWidth is < 0 or > 32
            || value.CornerRadius is < 0 or > 64)
            throw new ArgumentOutOfRangeException(nameof(value), "Workspace spacing must be 0..1024, strokes 0..32 and radius 0..64 logical pixels, or null.");
        RibbonStateChromeSettings.Validate(value.NavigationStateChrome);
    }
}
