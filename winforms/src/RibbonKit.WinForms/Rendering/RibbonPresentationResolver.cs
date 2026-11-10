using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Stateless family helpers are paired here. Drawing caches and native state
// remain with the active public renderer and the ribbon/window that own them.
internal sealed record RibbonBackstagePresentation(RibbonBackstageLayoutProvider Layout, RibbonBackstageRenderer Renderer);

internal static class RibbonPresentationResolver
{
    private static readonly RibbonBackstagePresentation Modern = new(new RibbonBackstage2024Layout(), new RibbonBackstage2024Renderer());
    private static readonly RibbonBackstagePresentation Classic = new(new RibbonBackstage2019Layout(), new RibbonBackstage2019Renderer());

    // Standalone measurement without an attached ribbon retains Modern defaults.
    internal static RibbonBackstagePresentation Backstage(RibbonTheme? theme) => (theme?.BackstageStyle ?? RibbonBackstageStyle.Modern) switch
    {
        RibbonBackstageStyle.Modern => Modern,
        RibbonBackstageStyle.Classic => Classic,
        _ => throw new ArgumentOutOfRangeException(nameof(theme))
    };
}
