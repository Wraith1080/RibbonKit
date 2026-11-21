namespace RibbonKit.WinForms;

/// <summary>Immutable, opt-in group decoration over measured group slots. Null fills and Empty inks paint nothing; null/empty settings retain existing group rendering. Simplified groups use compact frames without label bands.</summary>
public sealed record RibbonGroupPanelAppearanceSettings
{
    /// <summary>Initializes an empty decoration record.</summary>
    public RibbonGroupPanelAppearanceSettings() { }
    /// <summary>Gets whether the default renderer paints compact panels in Simplified mode. True retains existing behavior. False leaves measured Simplified runs unframed and restores their separators; Classic panels, label bands, layout and targets remain unchanged.</summary>
    public bool ShowInSimplified { get; init; } = true;
    /// <summary>Gets the panel fill, including alpha and gradients. Null leaves the body visible.</summary>
    public RibbonFill? Background { get; init; }
    /// <summary>Gets the outer rim ink. Empty/Transparent paints no rim.</summary>
    public Color BorderColor { get; init; } = Color.Empty;
    /// <summary>Gets outer rim width in logical pixels, 0..32. Zero hides it; available panel bounds clamp it.</summary>
    public int BorderWidth { get; init; } = 1;
    /// <summary>Gets panel corner radius in logical pixels, 0..64, clamped to available bounds.</summary>
    public int CornerRadius { get; init; } = 3;
    /// <summary>Gets uniform inset inside the measured group frame in logical pixels, 0..1024. This reserves paint space only; content and targets do not move.</summary>
    public int Inset { get; init; } = 2;
    /// <summary>Gets the independent inner highlight ink. Empty/Transparent paints none.</summary>
    public Color InnerBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets inner highlight width in logical pixels, 0..32. Zero hides it.</summary>
    public int InnerBorderWidth { get; init; } = 1;
    /// <summary>Gets additional highlight inset after the outer rim in logical pixels, 0..1024.</summary>
    public int InnerBorderInset { get; init; }
    /// <summary>Gets the Classic expanded label-band fill, including alpha and gradients. It spans the measured caption area inside the rim. Null leaves the panel visible; collapsed and Simplified groups have no separate band.</summary>
    public RibbonFill? LabelBackground { get; init; }
    /// <summary>Gets Classic expanded caption ink. Empty follows SecondaryText; disabled groups retain DisabledColor. This does not recolor collapsed or Simplified opener text.</summary>
    public Color LabelForeground { get; init; } = Color.Empty;
    internal bool HasOverrides => Background is not null || !BorderColor.IsEmpty || !InnerBorderColor.IsEmpty || LabelBackground is not null || !LabelForeground.IsEmpty;
    internal static void Validate(RibbonGroupPanelAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.BorderWidth is < 0 or > 32 || settings.InnerBorderWidth is < 0 or > 32 || settings.CornerRadius is < 0 or > 64
            || settings.Inset is < 0 or > 1024 || settings.InnerBorderInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(settings), "Group rim widths must be 0..32, radius 0..64 and insets 0..1024 logical pixels.");
    }
}
