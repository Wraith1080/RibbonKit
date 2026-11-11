namespace RibbonKit.WinForms;

/// <summary>Immutable header state, scroller, contextual ink and open CommandMenu File-highlight overrides. Null members retain existing presentation defaults.</summary>
public sealed record RibbonHeaderAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing appearance.</summary>
    public RibbonHeaderAppearanceSettings() { }
    /// <summary>Gets Filled-presentation tab/File state outline ink, including alpha. Empty retains the transparent default.</summary>
    public Color FilledBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional Filled-state outline width in logical pixels, 0..32. Null uses one device pixel when ink is authored; zero hides the border. Hit targets and body joins are unchanged.</summary>
    public int? FilledBorderWidth { get; init; }
    /// <summary>Gets optional header-scroller corner radius in logical pixels, 0..64. Null preserves configured square scrollers or the active renderer's ordinary chrome. Body scrollers are unaffected.</summary>
    public int? ScrollerCornerRadius { get; init; }
    /// <summary>Gets optional header-scroller outline width in logical pixels, 0..32. Null retains one device pixel; zero hides it. Existing HeaderScrollButtonBorder supplies ink.</summary>
    public int? ScrollerBorderWidth { get; init; }
    /// <summary>Gets optional unselected contextual-tab opacity, 0..1, multiplied by its authored color alpha before composition. Null uses 0.6. Selected/disabled ink remains independent. Must be finite.</summary>
    public double? ContextualUnselectedOpacity { get; init; }
    /// <summary>Gets optional open CommandMenu File-highlight inset in logical pixels, 0..1024. Null uses 3. Existing highlight corner radius remains independent; available bounds clamp inset.</summary>
    public int? FileHighlightInset { get; init; }
    /// <summary>Gets optional open CommandMenu File-highlight fill. Null follows Accent; authored alpha and gradients are retained.</summary>
    public RibbonFill? FileHighlightBackground { get; init; }
    /// <summary>Gets optional open CommandMenu File-highlight ink. Empty uses automatic black/white contrast against the highlight fill's first color.</summary>
    public Color FileHighlightForeground { get; init; } = Color.Empty;

    internal bool HasScrollerOverrides => ScrollerCornerRadius is not null || ScrollerBorderWidth is not null;
    internal bool HasFileHighlightOverrides => FileHighlightInset is not null || FileHighlightBackground is not null || !FileHighlightForeground.IsEmpty;
    internal static void Validate(RibbonHeaderAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.FilledBorderWidth is < 0 or > 32 || settings.ScrollerBorderWidth is < 0 or > 32 || settings.ScrollerCornerRadius is < 0 or > 64 || settings.FileHighlightInset is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(settings), "Header outline widths must be 0..32, radius 0..64 and File inset 0..1024 logical pixels, or null for existing appearance.");
        if (settings.ContextualUnselectedOpacity is { } opacity && (!double.IsFinite(opacity) || opacity < 0 || opacity > 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "Contextual inactive opacity must be a finite fraction from 0 to 1, or null for 0.6.");
    }
}
