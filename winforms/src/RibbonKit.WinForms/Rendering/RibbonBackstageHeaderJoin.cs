using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonBackstageHeaderJoin
{
    // Keep the caption's crease at its original pixel height, then extend its
    // final ramp across the header rather than restarting the same glass band.
    internal static RibbonFill Extend(RibbonFill reference, int captionHeight, int headerHeight)
    {
        double scale = (captionHeight - 1d) / (captionHeight + headerHeight - 1d);
        return reference with { IntermediateStops = reference.IntermediateStops is { } stops
            ? RibbonGradientStops.WithHardTransitions(stops.Select(s => new RibbonGradientStop(s.Offset * scale, s.Color))) : null };
    }
}

public partial class RibbonControl
{
    internal bool TryGetBackstageHeaderJoin(bool composited, out RibbonFill fill, out Rectangle canvas)
    {
        fill = default; canvas = Rectangle.Empty;
        var theme = Theme; var a = theme.BackstageAppearance;
        if (!IsBackstageOpen || theme.BackstageStyle != RibbonBackstageStyle.Office2007 || !HasWindowCaption
            || WindowHost!.AeroFrameSettings is not null || !WindowHost.TitleBarColor.IsEmpty
            || a?.Workspace?.JoinColoredCaption == false || a?.Workspace?.HeaderBackground is not null
            || a?.TopSeamHeight is > 0 || theme.TitleBarBackgroundFill is not { } caption) return false;
        var header = RibbonBackstage2007Renderer.HeaderFill(theme);
        if (caption != header || header.Direction == RibbonGradientDirection.Horizontal) return false;
        int captionHeight = WindowCaptionHeight;
        int headerHeight = _backstageSurface?.HeaderBounds.Height ?? 0;
        if (headerHeight <= 0) headerHeight = RibbonLayoutEngine.Scale(RibbonPresentationResolver.Backstage(theme).ResolveMetrics(theme, Density).WorkspaceHeaderHeight, DeviceDpi);
        if (captionHeight < 2 || headerHeight < 1) return false;
        fill = RibbonBackstageHeaderJoin.Extend(header, captionHeight, headerHeight);
        if (composited) fill = RibbonFillColors.Map(fill, color => Color.FromArgb((int)Math.Round(color.A * WindowHost.TitleBarTintOpacity), color));
        canvas = new(0, 0, WindowHost.CaptionHostBounds.Width, captionHeight + headerHeight);
        return true;
    }
}
