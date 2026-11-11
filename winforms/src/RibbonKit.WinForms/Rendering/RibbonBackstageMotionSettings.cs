namespace RibbonKit.WinForms;

/// <summary>Chooses a normalized visual progress curve for Backstage pane, title or page motion.</summary>
public enum RibbonBackstageEasing
{
    /// <summary>Moves quickly at first and slows with a cubic ease-out curve.</summary>
    EaseOutCubic,
    /// <summary>Uses a smooth-step curve with gradual starting and stopping.</summary>
    SmoothStep,
    /// <summary>Advances at a constant rate.</summary>
    Linear
}

/// <summary>Immutable Backstage motion overrides. Null members retain the active family's animation-level defaults. Assign through RibbonTheme.BackstageMotion.</summary>
public sealed record RibbonBackstageMotionSettings
{
    /// <summary>Initializes overrides that retain all family defaults.</summary>
    public RibbonBackstageMotionSettings() { }
    /// <summary>Gets optional pane and title duration in milliseconds, 0..60000. Null retains Subtle/Expressive defaults; zero snaps both immediately.</summary>
    public int? PaneDuration { get; init; }
    /// <summary>Gets optional page transition duration in milliseconds, 0..60000. Null retains Subtle/Expressive defaults; zero switches pages immediately.</summary>
    public int? PageDuration { get; init; }
    /// <summary>Gets optional page SlideDown travel in 96-DPI logical pixels, 0..1024. Null retains Subtle/Expressive defaults. Zero removes travel while duration remains independent; Fade is unaffected.</summary>
    public int? PageSlideDistance { get; init; }
    /// <summary>Gets optional pane travel easing. Null retains the family curve.</summary>
    public RibbonBackstageEasing? PaneEasing { get; init; }
    /// <summary>Gets optional pane fade easing. Null retains the family curve.</summary>
    public RibbonBackstageEasing? PaneFadeEasing { get; init; }
    /// <summary>Gets optional title travel easing. Null retains the family curve. Title ink stays opaque and title duration follows PaneDuration.</summary>
    public RibbonBackstageEasing? TitleEasing { get; init; }
    /// <summary>Gets optional page travel/fade easing. Null retains the family curve.</summary>
    public RibbonBackstageEasing? PageEasing { get; init; }
    /// <summary>Gets optional pane fade strength from 0 to 1. Null retains full fading; zero keeps the pane opaque for slide-only motion. Must be finite. Native hit-testing opacity safeguards remain effective.</summary>
    public double? PaneFadeAmount { get; init; }

    internal static void Validate(RibbonBackstageMotionSettings? settings)
    {
        if (settings is null) return;
        if (settings.PaneDuration is < 0 or > 60000 || settings.PageDuration is < 0 or > 60000 || settings.PageSlideDistance is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(settings), "Backstage durations must be 0..60000 ms, page travel 0..1024 logical pixels, or null for family defaults.");
        if (settings.PaneFadeAmount is { } amount && (!double.IsFinite(amount) || amount < 0 || amount > 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "Backstage pane fade strength must be a finite fraction from 0 to 1, or null for full fading.");
        RibbonBackstageEasing?[] curves = [settings.PaneEasing, settings.PaneFadeEasing, settings.TitleEasing, settings.PageEasing];
        if (curves.Any(curve => curve is { } value && !Enum.IsDefined(value)))
            throw new ArgumentOutOfRangeException(nameof(settings), "Backstage easing must be a defined curve or null for the family curve.");
    }
}
