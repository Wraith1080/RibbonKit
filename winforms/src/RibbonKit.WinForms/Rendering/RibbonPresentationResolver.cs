using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Stateless family helpers are paired here. Drawing caches and native state
// remain with the active public renderer and the ribbon/window that own them.
internal sealed record RibbonBackstagePresentation(RibbonBackstageLayoutProvider Layout, RibbonBackstageRenderer Renderer,
    RibbonBackstageCaptionProfile Caption, RibbonBackstageMotionProfile Motion)
{
    internal RibbonBackstageMetrics ResolveMetrics(RibbonTheme? theme, RibbonDensity density) => new(theme, density, Layout.Defaults);
}

internal static class RibbonPresentationResolver
{
    private static readonly RibbonBackstagePresentation Modern = new(new RibbonBackstage2024Layout(), new RibbonBackstage2024Renderer(),
        new(TintNeutralSeparateCaption: true),
        new(PaneTravelRatio: .15, TitleTravelRatio: .25, SubtlePaneDuration: 220, ExpressivePaneDuration: 308,
            SubtlePageTravel: 8, ExpressivePageTravel: 12, SubtlePageDuration: 130, ExpressivePageDuration: 182,
            PaneEasing: RibbonBackstageEasing.EaseOutCubic, PaneOpacityEasing: RibbonBackstageEasing.SmoothStep,
            TitleEasing: RibbonBackstageEasing.EaseOutCubic, PageEasing: RibbonBackstageEasing.EaseOutCubic));
    private static readonly RibbonBackstagePresentation Classic = new(new RibbonBackstage2019Layout(), new RibbonBackstage2019Renderer(),
        new(TintNeutralSeparateCaption: false),
        new(PaneTravelRatio: .15, TitleTravelRatio: .25, SubtlePaneDuration: 220, ExpressivePaneDuration: 308,
            SubtlePageTravel: 8, ExpressivePageTravel: 12, SubtlePageDuration: 130, ExpressivePageDuration: 182,
            PaneEasing: RibbonBackstageEasing.EaseOutCubic, PaneOpacityEasing: RibbonBackstageEasing.SmoothStep,
            TitleEasing: RibbonBackstageEasing.EaseOutCubic, PageEasing: RibbonBackstageEasing.EaseOutCubic));

    // Standalone measurement without an attached ribbon retains Modern defaults.
    internal static RibbonBackstagePresentation Backstage(RibbonTheme? theme) => (theme?.BackstageStyle ?? RibbonBackstageStyle.Modern) switch
    {
        RibbonBackstageStyle.Modern => Modern,
        RibbonBackstageStyle.Classic => Classic,
        _ => throw new ArgumentOutOfRangeException(nameof(theme))
    };
}
