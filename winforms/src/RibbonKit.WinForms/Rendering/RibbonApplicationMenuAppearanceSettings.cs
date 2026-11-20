namespace RibbonKit.WinForms;

/// <summary>Immutable Application Menu surface, heading and command-state overrides. Content layout, native editors and the shared outer popup frame keep their existing owners.</summary>
public sealed record RibbonApplicationMenuAppearanceSettings
{
    /// <summary>Initializes overrides that retain existing Application Menu command paint.</summary>
    public RibbonApplicationMenuAppearanceSettings() { }
    /// <summary>Gets optional outer popup-frame ink for the root Application Menu. Empty follows PopupBorder; Transparent hides ink. Ordinary/nested popups retain their own border, and renderer width/radius hooks remain authoritative.</summary>
    public Color OuterBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets an optional top-band fill between the surface top and the measured inner frame. Null retains SurroundBackground. The gradient spans that complete band under partial repaint; layout, Orb/caption exclusions and native targets remain unchanged.</summary>
    public RibbonFill? TopBandBackground { get; init; }
    /// <summary>Gets optional row-state outlines and inner rims. Explicit RowBorderColor/RowBorderWidth win; null retains existing row paint.</summary>
    public RibbonStateChromeSettings? RowStateChrome { get; init; }
    /// <summary>Gets optional footer-state outlines and inner rims. Explicit FooterBorderColor/FooterBorderWidth win; null retains existing footer paint.</summary>
    public RibbonStateChromeSettings? FooterStateChrome { get; init; }
    /// <summary>Gets optional idle/disabled footer-button fill. Null follows PopupBackground/Surface. Existing Hover/Pressed/Checked state fills remain independent.</summary>
    public RibbonFill? FooterBackground { get; init; }
    /// <summary>Gets idle/disabled footer-button outline ink, preceding PopupBorder. Empty retains PopupBorder. FooterStateChrome may replace this ink for active states; explicit FooterBorderColor remains authoritative for every state.</summary>
    public Color FooterIdleBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional open row/arrow fill when not physically hovered or pressed. Null retains Pressed; selected split-primary fill remains independent.</summary>
    public RibbonFill? OpenBackground { get; init; }
    /// <summary>Gets optional default-pane heading fill. Null retains MenuHeadingBackground. Transparent reveals the already painted pane without a separate heading patch.</summary>
    public RibbonFill? DefaultHeaderBackground { get; init; }
    /// <summary>Gets optional active-pane heading fill. Null retains MenuHeadingBackground/GalleryHeadingBackground. Does not affect ordinary menu headings.</summary>
    public RibbonFill? ActiveHeaderBackground { get; init; }
    /// <summary>Gets optional footer-band fill, spanning the surface below the inner panel. Null retains SurroundBackground; button fills are independent.</summary>
    public RibbonFill? FooterSurfaceBackground { get; init; }
    /// <summary>Gets optional surrounding rim ink. Empty/Transparent draws no rim; the outer popup and inner panel borders remain independent.</summary>
    public Color SurroundRimColor { get; init; } = Color.Empty;
    /// <summary>Gets optional surrounding rim width in logical pixels, 0..32. Null uses one device pixel when ink exists; zero hides it.</summary>
    public int? SurroundRimWidth { get; init; }
    /// <summary>Gets optional surrounding rim inset in logical pixels, 0..1024. Null uses one device pixel; bounds clamp the rim.</summary>
    public int? SurroundRimInset { get; init; }
    /// <summary>Gets optional leading split-divider highlight ink. Empty/Transparent adds no highlight. Base divider visibility, width and ink remain authoritative.</summary>
    public Color SplitDividerHighlightColor { get; init; } = Color.Empty;
    /// <summary>Gets optional leading divider-highlight width in logical pixels, 0..32. Null uses one device pixel; zero hides it. Clamps inside the existing divider paint.</summary>
    public int? SplitDividerHighlightWidth { get; init; }
    /// <summary>Gets optional active navigation/pane row corner radius in logical pixels, 0..64. Null retains square rows. Footer radius keeps ApplicationMenuFooterCornerRadius.</summary>
    public int? RowCornerRadius { get; init; }
    /// <summary>Gets optional navigation/pane state inset in logical pixels, 0..1024. Null retains one device pixel; authored values clamp inside unchanged command bounds.</summary>
    public int? RowInset { get; init; }
    /// <summary>Gets optional navigation/pane outline width in logical pixels, 0..32. Null retains zero, or a one-device-pixel state outline with RowStateChrome. Hit targets and focus slots remain unchanged.</summary>
    public int? RowBorderWidth { get; init; }
    /// <summary>Gets optional navigation/pane outline ink, including alpha. Empty follows RowStateChrome, then the transparent outline. This does not make idle rows active.</summary>
    public Color RowBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional footer state inset in logical pixels, 0..1024. Null retains one device pixel; authored values clamp inside unchanged footer targets.</summary>
    public int? FooterInset { get; init; }
    /// <summary>Gets optional footer outline width in logical pixels, 0..32. Null retains one device pixel, including the legacy transparent-border rim; zero removes the outline.</summary>
    public int? FooterBorderWidth { get; init; }
    /// <summary>Gets optional footer outline ink, including alpha. Empty follows FooterStateChrome, then PopupBorder. Footer radius retains its existing independent setting.</summary>
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

    /// <summary>Gets an optional surrounding/footer fill. Null retains the opaque default-pane tint. Authored alpha composites over that opaque backing.</summary>
    public RibbonFill? SurroundBackground { get; init; }
    /// <summary>Gets an optional navigation fill. Null follows PopupBackground/Surface. Authored alpha composites over the surrounding backing.</summary>
    public RibbonFill? NavigationBackground { get; init; }
    /// <summary>Gets an optional default-pane fill, preceding DefaultPaneTintStrength. Null retains the derived opaque tint; authored alpha composites over the navigation fill.</summary>
    public RibbonFill? DefaultPaneBackground { get; init; }
    /// <summary>Gets an optional selected-pane fill. Null follows PopupBackground/Surface. Authored alpha composites over the navigation fill. Application-owned Controls retain their own styling.</summary>
    public RibbonFill? ActivePaneBackground { get; init; }
    /// <summary>Gets optional finite 0..1 Border contribution to the default-pane and surrounding fallback. Null retains 0.125; derived ink is opaque.</summary>
    public double? DefaultPaneTintStrength { get; init; }
    /// <summary>Gets optional inner-frame radius in logical pixels, 0..64. Null retains the actual renderer's popup radius minus one device pixel. Paint clipping follows this radius; targets and native content bounds do not move.</summary>
    public int? InnerFrameCornerRadius { get; init; }
    /// <summary>Gets optional inner-frame stroke in logical pixels, 0..32. Null retains one device pixel. Zero hides the stroke; paint clips reserve at least the existing one-device-pixel inset.</summary>
    public int? InnerFrameBorderWidth { get; init; }
    /// <summary>Gets optional inner-frame ink, including alpha. Empty follows Border; Transparent hides ink.</summary>
    public Color InnerFrameBorderColor { get; init; } = Color.Empty;
    /// <summary>Gets optional navigation/pane divider width in logical pixels, 0..32. Null retains one device pixel; zero hides paint. The measured divider position is unchanged.</summary>
    public int? NavigationDividerWidth { get; init; }
    /// <summary>Gets optional navigation divider end clearance in logical pixels, 0..1024. Null retains one device pixel; explicit values clamp inside the unchanged frame.</summary>
    public int? NavigationDividerInset { get; init; }
    /// <summary>Gets optional navigation divider ink, including alpha. Empty follows Border; Transparent hides paint.</summary>
    public Color NavigationDividerColor { get; init; } = Color.Empty;
    /// <summary>Gets optional heading-fill corner radius in logical pixels, 0..64. Null retains square corners. Independent pane heading fills precede existing MenuHeadingBackground/selected-pane GalleryHeadingBackground.</summary>
    public int? HeaderCornerRadius { get; init; }
    /// <summary>Gets optional heading weight. Null/true adds Bold to host styles; false removes Bold while retaining other styles. The measured header height is unchanged.</summary>
    public bool? HeaderBold { get; init; }
    /// <summary>Gets optional default-pane heading text inset in logical pixels, 0..1024. Null retains 2; explicit values clamp within the unchanged header.</summary>
    public int? DefaultHeaderTextInset { get; init; }
    /// <summary>Gets optional selected-pane heading text inset in logical pixels, 0..1024. Null retains 10; explicit values clamp within the unchanged header.</summary>
    public int? ActiveHeaderTextInset { get; init; }
    /// <summary>Gets optional heading bottom-rule thickness in logical pixels, 0..32. Null retains one device pixel; zero hides paint. The line clamps inside the unchanged header.</summary>
    public int? HeaderRuleThickness { get; init; }
    /// <summary>Gets optional heading bottom-rule ink, including alpha. Empty follows Border; Transparent hides paint.</summary>
    public Color HeaderRuleColor { get; init; } = Color.Empty;

    internal static void Validate(RibbonApplicationMenuAppearanceSettings? settings)
    {
        if (settings is null) return;
        RibbonStateChromeSettings.Validate(settings.RowStateChrome); RibbonStateChromeSettings.Validate(settings.FooterStateChrome);
        if (settings.SurroundRimWidth is < 0 or > 32 || settings.SurroundRimInset is < 0 or > 1024 || settings.SplitDividerHighlightWidth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(settings), "Application Menu rim/highlight widths must be 0..32 and rim inset 0..1024 logical pixels, or null.");
        if (settings.RowCornerRadius is < 0 or > 64 || settings.RowInset is < 0 or > 1024 || settings.FooterInset is < 0 or > 1024 || settings.SplitDividerInset is < 0 or > 1024
            || settings.RowBorderWidth is < 0 or > 32 || settings.FooterBorderWidth is < 0 or > 32 || settings.SplitDividerWidth is < 0 or > 32
            || settings.SelectedPrimaryTintStrength is { } strength && (!double.IsFinite(strength) || strength < 0 || strength > 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "Application Menu row radius must be 0..64, widths 0..32 and insets 0..1024 logical pixels, and selected-primary strength finite 0..1, or null for existing paint.");
        if (settings.InnerFrameCornerRadius is < 0 or > 64 || settings.HeaderCornerRadius is < 0 or > 64
            || settings.InnerFrameBorderWidth is < 0 or > 32 || settings.NavigationDividerWidth is < 0 or > 32 || settings.HeaderRuleThickness is < 0 or > 32
            || settings.NavigationDividerInset is < 0 or > 1024 || settings.DefaultHeaderTextInset is < 0 or > 1024 || settings.ActiveHeaderTextInset is < 0 or > 1024
            || settings.DefaultPaneTintStrength is { } tint && (!double.IsFinite(tint) || tint < 0 || tint > 1))
            throw new ArgumentOutOfRangeException(nameof(settings), "Application Menu surface radii must be 0..64, strokes 0..32, insets 0..1024 logical pixels and pane tint finite 0..1, or null for existing paint.");
    }
}
