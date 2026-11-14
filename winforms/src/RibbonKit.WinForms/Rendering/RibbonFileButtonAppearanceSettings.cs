namespace RibbonKit.WinForms;

/// <summary>Immutable opt-in full-rectangle File states for CommandMenu and Backstage. Null on RibbonTheme retains legacy File rendering. Null fills follow Accent with the configured darkening; explicit alpha, gradients and Transparent remain authoritative.</summary>
public sealed record RibbonFileButtonAppearanceSettings
{
    /// <summary>Gets optional per-state outer ink and inner rim. Existing explicit header outline settings remain authoritative.</summary>
    public RibbonStateChromeSettings? StateChrome { get; init; }
    /// <summary>Initializes accent-filled File states with white ink.</summary>
    public RibbonFileButtonAppearanceSettings() { }
    /// <summary>Gets the optional idle fill. Null uses Accent.</summary>
    public RibbonFill? Background { get; init; }
    /// <summary>Gets the optional hover fill. Null darkens Accent by HoverDarkening.</summary>
    public RibbonFill? HoverBackground { get; init; }
    /// <summary>Gets the optional pressed fill. Null darkens Accent by PressedDarkening.</summary>
    public RibbonFill? PressedBackground { get; init; }
    /// <summary>Gets the optional open fill for both File routes. Null darkens Accent by OpenDarkening.</summary>
    public RibbonFill? OpenBackground { get; init; }
    /// <summary>Gets the optional disabled fill. Null retains the idle fill.</summary>
    public RibbonFill? DisabledBackground { get; init; }
    /// <summary>Gets idle ink. Defaults to White; Empty follows HeaderForeground/Foreground.</summary>
    public Color Foreground { get; init; } = Color.White;
    /// <summary>Gets hover ink. Empty follows Foreground.</summary>
    public Color HoverForeground { get; init; } = Color.Empty;
    /// <summary>Gets pressed ink. Empty follows Foreground.</summary>
    public Color PressedForeground { get; init; } = Color.Empty;
    /// <summary>Gets open ink. Empty follows Foreground.</summary>
    public Color OpenForeground { get; init; } = Color.Empty;
    /// <summary>Gets disabled ink. Empty follows the theme's disabled color.</summary>
    public Color DisabledForeground { get; init; } = Color.Empty;
    /// <summary>Gets hover darkening, a finite fraction 0..1. Defaults to 0.10; zero retains Accent.</summary>
    public double HoverDarkening { get; init; } = .10;
    /// <summary>Gets pressed darkening, a finite fraction 0..1. Defaults to 0.20.</summary>
    public double PressedDarkening { get; init; } = .20;
    /// <summary>Gets open darkening, a finite fraction 0..1. Defaults to 0.20.</summary>
    public double OpenDarkening { get; init; } = .20;
    internal static RibbonFill Darken(Color accent, double amount) => new(Color.FromArgb(accent.A,
        (int)Math.Round(accent.R * (1 - amount)), (int)Math.Round(accent.G * (1 - amount)), (int)Math.Round(accent.B * (1 - amount))));
    internal static void Validate(RibbonFileButtonAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (new[] { settings.HoverDarkening, settings.PressedDarkening, settings.OpenDarkening }.Any(n => !double.IsFinite(n) || n < 0 || n > 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "File darkening must be a finite fraction from 0 to 1.");
    }
}
