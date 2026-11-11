using System.Drawing.Drawing2D;

namespace RibbonKit.WinForms;

internal sealed record RibbonBackstageCaptionProfile(bool TintNeutralSeparateCaption)
{
    internal RibbonBackstageCaptionPolicy Resolve(RibbonTheme theme) => new(
        theme.BackstageExtendsIntoTitleBar, theme.BackstageTitleCentered, TintNeutralSeparateCaption, theme.BackstageNavigationColor);
}

// The window adapter supplies facts; policy never reads a live ribbon or Form.
internal readonly record struct RibbonBackstageCaptionState(
    bool IsOpen, bool HasWindowCaption, bool HasAuthoredCaptionColor, bool HasBackdrop,
    Color NormalColor, int VisibleWidth, int CaptionHeight, byte PaneOpacity, float NavigationOpacity);

internal readonly record struct RibbonBackstageCaptionPolicy(
    bool ExtendsIntoTitleBar, bool CenterTitle, bool TintNeutralSeparateCaption, Color NavigationColor)
{
    internal RibbonBackstageCaptionFrame Resolve(in RibbonBackstageCaptionState state)
    {
        if (!TintNeutralSeparateCaption || !state.IsOpen || !state.HasWindowCaption || ExtendsIntoTitleBar || state.HasAuthoredCaptionColor)
            return new(state.NormalColor, state.NormalColor, Rectangle.Empty);
        int alpha = state.HasBackdrop ? (int)Math.Round(state.PaneOpacity * state.NavigationOpacity) : state.PaneOpacity;
        return new(state.NormalColor, Color.FromArgb(alpha, NavigationColor), new(0, 0, state.VisibleWidth, state.CaptionHeight));
    }
}

internal readonly record struct RibbonBackstageCaptionFrame(Color NormalColor, Color TintColor, Rectangle TintBounds)
{
    internal Color CaptionColor => TintBounds.IsEmpty ? NormalColor : TintColor;
    internal void Paint(Graphics graphics, Rectangle bounds)
    {
        using var normal = new SolidBrush(NormalColor);
        graphics.FillRectangle(normal, bounds);
        var tint = Rectangle.Intersect(bounds, TintBounds);
        if (tint.Width <= 0 || tint.Height <= 0) return;
        using var brush = new SolidBrush(TintColor);
        var mode = graphics.CompositingMode;
        try { graphics.CompositingMode = CompositingMode.SourceOver; graphics.FillRectangle(brush, tint); }
        finally { graphics.CompositingMode = mode; }
    }
}
