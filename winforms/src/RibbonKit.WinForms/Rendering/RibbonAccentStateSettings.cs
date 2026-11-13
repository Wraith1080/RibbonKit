namespace RibbonKit.WinForms;

/// <summary>Immutable accent washes and optional neutral defaults for command states. Explicit theme state overrides win; null on RibbonTheme restores the fixed fallback palette.</summary>
public sealed record RibbonAccentStateSettings
{
    /// <summary>Initializes light accent washes over Surface.</summary>
    public RibbonAccentStateSettings() { }
    /// <summary>Gets an optional hover background, including alpha and gradients. Null derives a wash from HoverOpacity; a fill keeps ordinary hover neutral independently of checked selection.</summary>
    public RibbonFill? HoverBackground { get; init; }
    /// <summary>Gets an optional pressed background, including alpha and gradients. Null derives a wash from PressedOpacity.</summary>
    public RibbonFill? PressedBackground { get; init; }
    /// <summary>Gets hover accent coverage, a finite fraction 0..1. Defaults to 0.10.</summary>
    public double HoverOpacity { get; init; } = .10;
    /// <summary>Gets pressed accent coverage, a finite fraction 0..1. Defaults to 0.20.</summary>
    public double PressedOpacity { get; init; } = .20;
    /// <summary>Gets checked accent coverage, a finite fraction 0..1. Defaults to 0.16.</summary>
    public double CheckedOpacity { get; init; } = .16;
    /// <summary>Gets checked-hover accent coverage, a finite fraction 0..1. Defaults to 0.25. Explicit CheckedHover wins.</summary>
    public double CheckedHoverOpacity { get; init; } = .25;
    internal static RibbonFill Wash(Color accent, Color surface, double opacity)
    {
        double a = opacity * accent.A / 255d;
        double backing = surface.A / 255d * (1 - a), alpha = a + backing;
        if (alpha == 0) return new(Color.Transparent);
        return new(Color.FromArgb((int)Math.Round(alpha * 255), (int)Math.Round((accent.R * a + surface.R * backing) / alpha),
            (int)Math.Round((accent.G * a + surface.G * backing) / alpha), (int)Math.Round((accent.B * a + surface.B * backing) / alpha)));
    }
    internal static void Validate(RibbonAccentStateSettings? settings)
    {
        if (settings is null) return;
        if (new[] { settings.HoverOpacity, settings.PressedOpacity, settings.CheckedOpacity, settings.CheckedHoverOpacity }
            .Any(n => !double.IsFinite(n) || n < 0 || n > 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "Accent coverage must be a finite fraction from 0 to 1.");
    }
}
