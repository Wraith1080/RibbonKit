using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Stateless family helpers are paired here. Drawing caches and native state
// remain with the active public renderer and the ribbon/window that own them.
internal sealed record RibbonBackstagePresentation(RibbonBackstageLayoutProvider Layout, RibbonBackstageRenderer Renderer,
    RibbonBackstageCaptionProfile Caption, RibbonBackstageMotionProfile Motion)
{
    internal RibbonBackstageMetrics ResolveMetrics(RibbonTheme? theme, RibbonDensity density) => new(theme, density, Layout.Defaults);
}

internal sealed record RibbonHeaderPresentationRegistration(RibbonHeaderRenderer Renderer, RibbonHeaderLayoutDefaults Defaults)
{
    internal RibbonHeaderMetrics ResolveMetrics(RibbonTheme theme, RibbonDensity density) => new(theme, density, Defaults);
}

internal static class RibbonPresentationResolver
{
    internal static readonly RibbonOptionsTreeDefaults OptionsTree = new(CompactIndent: 24, TouchIndent: 32, CompactCheckOffset: 22, TouchCheckOffset: 28, IconOffset: 20, IconSize: 16, IconGap: 2, CompactCheck: 18, TouchCheck: 24, CompactChevron: 12, TouchChevron: 16, CompactStroke: 1.6f, TouchStroke: 1.8f, FocusInset: 1);
    internal static readonly RibbonOptionsRowDefaults OptionsRows = new(CommandPadding: 6, SimplifiedPadding: 8, IconSize: 20, SelectionRadius: 0);
    internal static readonly RibbonOptionsFieldDefaults OptionsFields = new(FrameBorder: 1, NativeClearance: 1, SearchInset: 4, SearchDeduction: 8, ChoiceInset: 6);
    internal static readonly RibbonOptionsShellDefaults OptionsShell = new(ButtonInset: 0, ButtonFocusInset: 4, ButtonBorder: 1, AccentBorder: 0, HeadingIncrement: 5, HeadingMinimum: 14, HeadingBottomPadding: 12);
    internal static readonly RibbonOptionsNavigationDefaults OptionsNavigation = new(SelectionInset: 2, TextPadding: 12, MarkerWidth: 3, MarkerInset: 7, MarkerDeduction: 14, FocusInset: 1, DividerWidth: 1);
    internal static readonly RibbonTipDefaults Tips = new(MaximumWidth: 320, ContentPadding: 12, TitleGap: 6, PlacementGap: 8, BackstageHorizontalAllowance: 8, BackstageVerticalAllowance: 4, BackstageRightClearance: 4, KeyTipStroke: 1, OfficeKeyTipRadius: 2);
    internal static readonly RibbonGalleryContentDefaults GalleryContent = new(LabelLines: 2, PaintReserve: 12, MeasureReserve: 24, ArtworkHorizontalExtra: 4, ArtworkTopExtra: 2, ExpandHeight: 18, BarWidth: 10, BarInset: 4, BarStroke: 2);
    internal static readonly RibbonGalleryChromeDefaults GalleryChrome = new(Inset: 1, BorderWidth: 1);
    internal static readonly RibbonApplicationMenuContentDefaults ApplicationMenuContent = new(LargeIcon: 32, SmallIcon: 16, LargeIconInset: 8, SmallIconInset: 6, Gap: 8, LargeTextInset: 52, TextInset: 6,
        PaintTrailing: 6, MeasureTrailing: 8, RowPadding: 10, IconPadding: 8, DescriptionGap: 2, DescriptionInset: 4, DescriptionLines: 2, FooterPadding: 6, FooterIconPreferred: 46, FooterPlainPreferred: 24,
        CompactArrow: 26, TouchArrow: 44, SeparatorHeight: 9, SeparatorInset: 8, SeparatorStroke: 1);
    internal static readonly RibbonApplicationMenuSurfaceDefaults ApplicationMenuSurface = new(Stroke: 1, DividerInset: 1, HeaderRadius: 0, DefaultHeaderInset: 2, ActiveHeaderInset: 10, PaneTint: .125);
    internal static readonly RibbonApplicationMenuChromeDefaults ApplicationMenuChrome = new(RowRadius: 0, Inset: 1, RowBorderWidth: 0, FooterBorderWidth: 1, SplitWidth: 1, SplitInset: 1, PrimaryTint: .25);
    internal static readonly RibbonMenuDecorationDefaults MenuDecoration = new(Inset: 1, BorderWidth: 1, SeparatorThickness: 1);
    internal static readonly RibbonPopupFrameDefaults PopupFrame = new(BorderWidth: 1, Blur: 6, HorizontalOffset: 0, VerticalOffset: 2, Falloff: 2.5, LegacyPadding: 8);
    internal static readonly RibbonEditorChromeDefaults EditorChrome = new(Inset: 1, BorderWidth: 1, ChoiceInset: 1, ChoiceTextPadding: 6);
    internal static readonly RibbonCommandChromeDefaults CommandChrome = new(Inset: 1, BorderWidth: 1, SplitWidth: 1, SplitInset: 1);
    internal static readonly RibbonBodyFrameDefaults NeutralBody = new(PaintShadow: false, BorderFallback: false, AllowBottomBorderOnly: false, RadiusDivisor: 0);
    internal static readonly RibbonBodyFrameDefaults OfficeBody = new(PaintShadow: true, BorderFallback: true, AllowBottomBorderOnly: true, RadiusDivisor: 48);
    // Both styles currently share strip geometry. This is an explicit pairing,
    // independent of palette, radius, caption extension and built-in identity.
    private static readonly RibbonHeaderLayoutDefaults HeaderGeometry = new(TabPadding: 14, TabMinimum: 58, FileMinimum: 50,
        IndicatorPadding: 4, IndicatorThickness: 3, IndicatorBottomInset: 1, FilledCornerRadius: 0);
    private static readonly RibbonHeaderPresentationRegistration Underline = new(new RibbonHeader2024Renderer(), HeaderGeometry);
    private static readonly RibbonHeaderPresentationRegistration Filled = new(new RibbonHeader2019Renderer(), HeaderGeometry);
    internal static RibbonHeaderPresentationRegistration Header(RibbonTheme theme) => theme.HeaderPresentation switch
    {
        RibbonHeaderPresentation.Underline => Underline,
        RibbonHeaderPresentation.Filled => Filled,
        _ => throw new ArgumentOutOfRangeException(nameof(theme))
    };

    private static readonly RibbonBackstagePresentation Modern = new(new RibbonBackstage2024Layout(), new RibbonBackstage2024Renderer(),
        new(TintNeutralSeparateCaption: true),
        new(PaneTravelRatio: .15, TitleTravelRatio: .25, SubtlePaneDuration: 220, ExpressivePaneDuration: 308,
            SubtlePageTravel: 8, ExpressivePageTravel: 12, SubtlePageDuration: 130, ExpressivePageDuration: 182,
            PaneEasing: RibbonBackstageEasing.EaseOutCubic, PaneOpacityEasing: RibbonBackstageEasing.SmoothStep,
            TitleEasing: RibbonBackstageEasing.EaseOutCubic, PageEasing: RibbonBackstageEasing.EaseOutCubic, PaneFadeAmount: 1));
    private static readonly RibbonBackstagePresentation Classic = new(new RibbonBackstage2019Layout(), new RibbonBackstage2019Renderer(),
        new(TintNeutralSeparateCaption: false),
        new(PaneTravelRatio: .15, TitleTravelRatio: .25, SubtlePaneDuration: 220, ExpressivePaneDuration: 308,
            SubtlePageTravel: 8, ExpressivePageTravel: 12, SubtlePageDuration: 130, ExpressivePageDuration: 182,
            PaneEasing: RibbonBackstageEasing.EaseOutCubic, PaneOpacityEasing: RibbonBackstageEasing.SmoothStep,
            TitleEasing: RibbonBackstageEasing.EaseOutCubic, PageEasing: RibbonBackstageEasing.EaseOutCubic, PaneFadeAmount: 1));

    // Standalone measurement without an attached ribbon retains Modern defaults.
    internal static RibbonBackstagePresentation Backstage(RibbonTheme? theme) => (theme?.BackstageStyle ?? RibbonBackstageStyle.Modern) switch
    {
        RibbonBackstageStyle.Modern => Modern,
        RibbonBackstageStyle.Classic => Classic,
        _ => throw new ArgumentOutOfRangeException(nameof(theme))
    };
}
