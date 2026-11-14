namespace RibbonKit.WinForms;

/// <summary>Immutable reference ramps for dimensional identity surfaces. Rebasing retains stop positions, direction, alpha and relative channel depth; explicit role fills win.</summary>
public sealed record RibbonIdentityAccentSettings
{
    /// <summary>Initializes the Office 2010 Blue identity recipe.</summary>
    public RibbonIdentityAccentSettings() { }
    /// <summary>Gets the RGB reference accent against which ramp colors were authored.</summary>
    public Color ReferenceAccent { get; init; } = Color.FromArgb(30, 90, 156);
    /// <summary>Gets File ramp rebasing strength, finite 0..1. Zero retains reference colors.</summary>
    public double FileStrength { get; init; } = 1;
    /// <summary>Gets colored-caption rebasing strength, finite 0..1. Independent of FileStrength.</summary>
    public double CaptionStrength { get; init; } = 1;
    /// <summary>Gets selected Backstage ramp/outline rebasing strength, finite 0..1. Explicit Backstage role fills/ink win.</summary>
    public double NavigationStrength { get; init; } = 1;
    /// <summary>Gets the reference selected Backstage navigation ramp, retaining linear depth independently of gold command states.</summary>
    public RibbonFill NavigationSelectedBackground { get; init; } = new(Rgb(0x1C6ACA), Rgb(0x4D8BCE))
    { IntermediateStops = new(new[] { new RibbonGradientStop(.5, Rgb(0x3A7EC5)) }) };
    /// <summary>Gets reference selected Backstage outline ink, including alpha. Explicit BackstageAppearance.NavigationBorderColor wins.</summary>
    public Color NavigationBorderColor { get; init; } = Rgb(0x17578F);
    /// <summary>Gets the reference idle File ramp.</summary>
    public RibbonFill FileBackground { get; init; } = Ramp(0x4E97CE, 0x2470B2, 0x3D86C4);
    /// <summary>Gets the reference hovered File ramp.</summary>
    public RibbonFill FileHoverBackground { get; init; } = Ramp(0x63A9DD, 0x3382C2, 0x4E96CE);
    /// <summary>Gets the reference pressed File ramp.</summary>
    public RibbonFill FilePressedBackground { get; init; } = Ramp(0x2C71AC, 0x175792, 0x276CA4);
    /// <summary>Gets the reference open File ramp.</summary>
    public RibbonFill FileOpenBackground { get; init; } = Ramp(0x3E8BC5, 0x1E62A1, 0x3277B4);
    /// <summary>Gets reference File outline ink, including alpha. Rebased with FileStrength; explicit File/header border ink wins.</summary>
    public Color FileBorderColor { get; init; } = Rgb(0x17578F);
    /// <summary>Gets the opaque reference colored-caption ramp. The neutral caption stays independently configured on RibbonTheme.</summary>
    public RibbonFill CaptionBackground { get; init; } = Ramp(0x6894BE, 0x1E5A9C, 0x326BA6);
    /// <summary>Rebases an arbitrary reference fill onto accent RGB with finite strength 0..1. Alpha, offsets and direction are retained. Zero or matching reference RGB returns the original value.</summary>
    public RibbonFill AdaptFill(RibbonFill fill, Color accent, double strength = 1)
    {
        ValidateStrength(strength);
        if (strength == 0 || (ReferenceAccent.R == accent.R && ReferenceAccent.G == accent.G && ReferenceAccent.B == accent.B)) return fill;
        int Part(byte value, byte reference, byte target)
        {
            double rebased = value == reference ? target : value > reference
                ? target + (255d - target) * (value - reference) / (255 - reference)
                : target * (double)value / reference;
            return (int)Math.Round(value + (rebased - value) * strength);
        }
        return RibbonFillColors.Map(fill, color => Color.FromArgb(color.A,
            Part(color.R, ReferenceAccent.R, accent.R), Part(color.G, ReferenceAccent.G, accent.G), Part(color.B, ReferenceAccent.B, accent.B)));
    }
    // Navigation has a broad uninterrupted color field. Preserve the chosen hue
    // while transferring relative lightness/saturation; per-channel blue depth
    // would otherwise make red selections purple. File/caption rebasing is separate.
    internal RibbonFill AdaptNavigationFill(RibbonFill fill, Color accent)
    {
        ValidateStrength(NavigationStrength);
        if (NavigationStrength == 0 || (ReferenceAccent.R == accent.R && ReferenceAccent.G == accent.G && ReferenceAccent.B == accent.B)) return fill;
        double referenceLight = ReferenceAccent.GetBrightness(), referenceSaturation = ReferenceAccent.GetSaturation();
        double light = accent.GetBrightness(), saturation = accent.GetSaturation(), hue = accent.GetHue() / 60d;
        return RibbonFillColors.Map(fill, color =>
        {
            double sourceLight = color.GetBrightness();
            double rebasedLight = sourceLight > referenceLight
                ? light + (1 - light) * (sourceLight - referenceLight) / (1 - referenceLight)
                : referenceLight > 0 ? light * sourceLight / referenceLight : light;
            double rebasedSaturation = referenceSaturation > 0 ? Math.Clamp(saturation * color.GetSaturation() / referenceSaturation, 0, 1) : saturation;
            double chroma = (1 - Math.Abs(2 * rebasedLight - 1)) * rebasedSaturation;
            double x = chroma * (1 - Math.Abs(hue % 2 - 1)), offset = rebasedLight - chroma / 2;
            (double r, double g, double b) = hue switch
            {
                < 1 => (chroma, x, 0d), < 2 => (x, chroma, 0d), < 3 => (0d, chroma, x),
                < 4 => (0d, x, chroma), < 5 => (x, 0d, chroma), _ => (chroma, 0d, x)
            };
            int Channel(byte original, double projected) => (int)Math.Clamp(Math.Round(original + ((projected + offset) * 255 - original) * NavigationStrength), 0, 255);
            return Color.FromArgb(color.A, Channel(color.R, r), Channel(color.G, g), Channel(color.B, b));
        });
    }
    /// <summary>Creates the colored-caption fill for an application's title-color preference. Theme/Accent changes can reapply this recipe; Form.TitleBarColor still wins.</summary>
    public RibbonFill CreateCaptionFill(Color accent) => AdaptFill(CaptionBackground, accent, CaptionStrength);
    internal static RibbonFill Ramp(int start, int middle, int end) => new(Rgb(start), Rgb(end))
    { IntermediateStops = new(new[] { new RibbonGradientStop(.5, Rgb(middle)) }) };
    internal static Color Rgb(int value) => Color.FromArgb(255, (value >> 16) & 255, (value >> 8) & 255, value & 255);
    private static void ValidateStrength(double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Identity accent strength must be finite and between zero and one.");
    }
    internal static void Validate(RibbonIdentityAccentSettings? settings)
    {
        if (settings is null) return;
        ValidateStrength(settings.FileStrength); ValidateStrength(settings.CaptionStrength); ValidateStrength(settings.NavigationStrength);
        if (!RibbonFillColors.IsOpaque(settings.CaptionBackground)) throw new ArgumentException("Every colored-caption stop must be opaque.", nameof(settings));
    }
}
