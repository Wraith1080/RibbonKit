namespace RibbonKit.WinForms;

/// <summary>Immutable Application Menu command-state, split-perimeter and footer-outline overrides. Backgrounds, headings, content layout, native editors and the shared popup frame keep their existing owners.</summary>
public sealed record RibbonApplicationMenuAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing Application Menu command paint.</summary>
    public RibbonApplicationMenuAppearanceSettings() { }
    /// <summary>Gets optional active navigation/pane row corner radius in logical pixels, 0..64. Null retains square rows. Footer radius keeps ApplicationMenuFooterCornerRadius.</summary>
    public int? RowCornerRadius { get; init; }
    /// <summary>Gets optional navigation/pane state inset in logical pixels, 0..1024. Null retains one device pixel; authored values clamp inside unchanged command bounds.</summary>
    public int? RowInset { get; init; }
    /// <summary>Gets optional navigation/pane outline width in logical pixels, 0..32. Null retains zero. Set both width and ink to add an outline; hit targets and focus slots remain unchanged.</summary>
    public int? RowBorderWidth { get; init; }
    /// <summary>Gets optional navigation/pane outline ink, including alpha. Empty retains the transparent outline. This does not make idle rows active.</summary>
    public Color RowBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional footer state inset in logical pixels, 0..1024. Null retains one device pixel; authored values clamp inside unchanged footer targets.</summary>
    public int? FooterInset { get; init; }
    /// <summary>Gets optional footer outline width in logical pixels, 0..32. Null retains one device pixel, including the legacy transparent-border rim; zero removes the outline.</summary>
    public int? FooterBorderWidth { get; init; }
    /// <summary>Gets optional footer outline ink, including alpha. Empty follows PopupBorder. Footer radius retains its existing independent setting.</summary>
    public Color FooterBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional active split-divider width in logical pixels, 0..32. Null retains one device pixel; zero hides it. ApplicationMenuSplitDividerVisible remains authoritative.</summary>
    public int? SplitDividerWidth { get; init; }
    /// <summary>Gets optional split-divider end inset in logical pixels, 0..1024. Null retains one device pixel at each end; authored values clamp inside the unchanged row.</summary>
    public int? SplitDividerInset { get; init; }
    /// <summary>Gets optional active split-divider ink, including alpha. Empty follows Border; Transparent hides its paint.</summary>
    public Color SplitDividerColor { get; init; } = Color.Empty;
    /// <summary>Gets optional primary-half fill while a split pane is open and neither half is hovered/pressed. Authored alpha/gradients precede SelectedPrimaryTintStrength. Null retains the derived PopupBackground/Pressed fill.</summary>
    public RibbonFill? SelectedPrimaryBackground { get; init; }
    /// <summary>Gets optional finite 0..1 Pressed-color contribution to the unhovered open primary half. Null retains 0.25. The derived channel mix stays opaque; explicit SelectedPrimaryBackground supports alpha. Hover, press, checked and expanded-half precedence remain unchanged.</summary>
    public double? SelectedPrimaryTintStrength { get; init; }

    internal static void Validate(RibbonApplicationMenuAppearanceSettings? settings)
    {
        if (settings is null) return;
        if (settings.RowCornerRadius is < 0 or > 64 || settings.RowInset is < 0 or > 1024 || settings.FooterInset is < 0 or > 1024 || settings.SplitDividerInset is < 0 or > 1024
            || settings.RowBorderWidth is < 0 or > 32 || settings.FooterBorderWidth is < 0 or > 32 || settings.SplitDividerWidth is < 0 or > 32
            || settings.SelectedPrimaryTintStrength is { } strength && (!double.IsFinite(strength) || strength < 0 || strength > 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "Application Menu row radius must be 0..64, widths 0..32 and insets 0..1024 logical pixels, and selected-primary strength finite 0..1, or null for existing paint.");
    }
}
