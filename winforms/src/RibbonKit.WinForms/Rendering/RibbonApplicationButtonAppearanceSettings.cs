namespace RibbonKit.WinForms;

/// <summary>Chooses the File command's shape independently of its CommandMenu or Backstage route.</summary>
public enum RibbonApplicationButtonShape
{
    /// <summary>Uses the existing rectangular File header.</summary>
    Rectangular,
    /// <summary>Uses a circular application button spanning the available caption and tab band.</summary>
    Orb
}

/// <summary>Immutable shared application-Orb geometry and paint. Null fills and Empty inks follow the ordinary theme roles. This does not select a File route or window material.</summary>
public sealed record RibbonApplicationButtonAppearanceSettings
{
    /// <summary>Initializes shared Orb geometry without a family-specific palette.</summary>
    public RibbonApplicationButtonAppearanceSettings() { }
    /// <summary>Gets the requested Compact diameter in logical pixels, 16..128. Available header height/width clamp it.</summary>
    public int Diameter { get; init; } = 46;
    /// <summary>Gets the requested Touch diameter in logical pixels, 44..128. Available header height/width clamp it.</summary>
    public int TouchDiameter { get; init; } = 52;
    /// <summary>Gets the overlap above the tab band in logical pixels, 0..1024. A supporting caption and its native top resize clearance limit overlap; ordinary Forms keep the Orb within the tab band.</summary>
    public int CaptionOverlap { get; init; } = 18;
    /// <summary>Gets requested leading header clearance in logical pixels, 0..1024. Visible shadow clearance is also reserved.</summary>
    public int LeftInset { get; init; } = 6;
    /// <summary>Gets trailing clearance reserved for tabs, title and Quick Access in logical pixels, 0..1024. The larger of this value and visible shadow clearance is used.</summary>
    public int RightGap { get; init; } = 6;
    /// <summary>Gets the requested Application Menu origin relative to the Orb's top-left, in logical pixels, each -1024..1024. Caption clearance and working-area clamps remain authoritative; the frame starts below the caption or uses the existing above-caption fallback.</summary>
    public Point MenuOffset { get; init; } = new(0, 8);
    /// <summary>Gets minimum Application Menu top-band height in logical pixels, 0..1024. Measured Orb/caption exclusion adds clearance so no menu target or native editor sits under the cutout.</summary>
    public int MenuTopBandHeight { get; init; } = 18;
    /// <summary>Gets the idle sphere fill. Null follows Surface.</summary>
    public RibbonFill? Background { get; init; }
    /// <summary>Gets the hover sphere fill. Null follows Hover.</summary>
    public RibbonFill? HoverBackground { get; init; }
    /// <summary>Gets the pressed sphere fill. Null follows Pressed.</summary>
    public RibbonFill? PressedBackground { get; init; }
    /// <summary>Gets the persistent open sphere fill for either File route. Null follows PressedBackground/Pressed.</summary>
    public RibbonFill? OpenBackground { get; init; }
    /// <summary>Gets the disabled sphere fill. Null follows Background/Surface and suppresses hot/pressed feedback.</summary>
    public RibbonFill? DisabledBackground { get; init; }
    /// <summary>Gets idle rim ink. Empty follows Border; Transparent hides it.</summary>
    public Color BorderColor { get; init; } = Color.Empty;
    /// <summary>Gets hover rim ink. Empty follows BorderColor/Border.</summary>
    public Color HoverBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets pressed rim ink. Empty follows BorderColor/Border.</summary>
    public Color PressedBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets open rim ink. Empty follows PressedBorderColor/BorderColor/Border.</summary>
    public Color OpenBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets rim width in logical pixels, 0..32. Zero hides it.</summary>
    public int BorderWidth { get; init; } = 1;
    /// <summary>Gets independent inner-highlight ink. Empty/Transparent paints none.</summary>
    public Color InnerBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets inner-highlight width in logical pixels, 0..32. Zero hides it.</summary>
    public int InnerBorderWidth { get; init; } = 1;
    /// <summary>Gets glyph ink. Empty follows HeaderForeground/Foreground; disabled glyphs follow DisabledColor.</summary>
    public Color GlyphColor { get; init; } = Color.Empty;
    /// <summary>Gets glyph side length in logical pixels, 0..128, clamped inside the sphere. Zero hides the glyph.</summary>
    public int GlyphSize { get; init; } = 22;
    /// <summary>Gets independently adjustable shadow ink. Empty/Transparent paints none.</summary>
    public Color ShadowColor { get; init; } = Color.Empty;
    /// <summary>Gets shadow clearance/blur in logical pixels, 0..32. Zero hides the shadow.</summary>
    public int ShadowBlur { get; init; } = 2;
    internal static readonly RibbonApplicationButtonAppearanceSettings Default = new();
    internal static void Validate(RibbonApplicationButtonAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.Diameter is < 16 or > 128 || settings.TouchDiameter is < 44 or > 128 || settings.CaptionOverlap is < 0 or > 1024
            || settings.LeftInset is < 0 or > 1024 || settings.RightGap is < 0 or > 1024 || settings.MenuOffset.X is < -1024 or > 1024 || settings.MenuOffset.Y is < -1024 or > 1024
            || settings.MenuTopBandHeight is < 0 or > 1024 || settings.BorderWidth is < 0 or > 32 || settings.InnerBorderWidth is < 0 or > 32
            || settings.GlyphSize is < 0 or > 128 || settings.ShadowBlur is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(settings), "Application button diameters, clearances, rims, glyph and shadow must be within their documented logical-pixel ranges.");
    }
}
