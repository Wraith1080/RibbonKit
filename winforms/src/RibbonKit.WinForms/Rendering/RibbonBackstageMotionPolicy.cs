namespace RibbonKit.WinForms;

// Only the two curves already used by Backstage are represented here.
internal enum RibbonBackstageEasing { EaseOutCubic, SmoothStep }

internal sealed record RibbonBackstageMotionProfile(
    double PaneTravelRatio, double TitleTravelRatio, int SubtlePaneDuration, int ExpressivePaneDuration,
    int SubtlePageTravel, int ExpressivePageTravel, int SubtlePageDuration, int ExpressivePageDuration,
    RibbonBackstageEasing PaneEasing, RibbonBackstageEasing PaneOpacityEasing,
    RibbonBackstageEasing TitleEasing, RibbonBackstageEasing PageEasing)
{
    internal RibbonBackstageMotionPolicy Resolve(RibbonTheme theme, RibbonAnimationLevel level)
    {
        bool expressive = level == RibbonAnimationLevel.Expressive;
        return new(theme.BackstageSlideDistanceRatio ?? PaneTravelRatio, theme.BackstageTitleSlideDistanceRatio ?? TitleTravelRatio,
            expressive ? ExpressivePaneDuration : SubtlePaneDuration, expressive ? ExpressivePageTravel : SubtlePageTravel,
            expressive ? ExpressivePageDuration : SubtlePageDuration, PaneEasing, PaneOpacityEasing, TitleEasing, PageEasing);
    }
}

// Resolved visual choices are captured for a transition. Timers, cancellation,
// native opacity ownership and reduced-motion gates remain with the engines.
internal readonly record struct RibbonBackstageMotionPolicy(
    double PaneTravelRatio, double TitleTravelRatio, double PaneDuration, int PageTravel, double PageDuration,
    RibbonBackstageEasing PaneEasing, RibbonBackstageEasing PaneOpacityEasing,
    RibbonBackstageEasing TitleEasing, RibbonBackstageEasing PageEasing)
{
    internal int PaneTravel(int width) => PaneTravelRatio == 0 ? 0 : Math.Max(1, (int)Math.Round(width * PaneTravelRatio));
    internal double PaneProgress(double progress) => Ease(PaneEasing, progress);
    internal double TitleProgress(double progress) => Ease(TitleEasing, progress);
    internal double PageProgress(double progress) => Ease(PageEasing, progress);
    internal byte PaneOpacity(double progress, bool closing)
    {
        double fade = Ease(PaneOpacityEasing, progress);
        return (byte)Math.Clamp((int)Math.Round(255 * (closing ? 1 - fade : fade)), 1, 255);
    }
    internal Rectangle InitialTitleBounds(Rectangle from, Rectangle target)
    {
        double travel = TitleTravelRatio;
        int Start(int a, int b) => (int)Math.Round(b + (a - b) * travel);
        return new(Start(from.X, target.X), Start(from.Y, target.Y), Start(from.Width, target.Width), Start(from.Height, target.Height));
    }
    private static double Ease(RibbonBackstageEasing curve, double progress)
    {
        double t = Math.Clamp(progress, 0, 1);
        return curve switch
        {
            RibbonBackstageEasing.EaseOutCubic => 1 - Math.Pow(1 - t, 3),
            RibbonBackstageEasing.SmoothStep => t * t * (3 - 2 * t),
            _ => throw new ArgumentOutOfRangeException(nameof(curve))
        };
    }
}
