namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonApplicationMenuChromeDefaults(int RowRadius, int Inset, int RowBorderWidth, int FooterBorderWidth, int SplitWidth, int SplitInset, double PrimaryTint);

// Resolved paint only; Application Menu layout, targets and native ownership stay shared.
internal readonly record struct RibbonApplicationMenuChromeMetrics(RibbonTheme Theme, int Dpi, RibbonApplicationMenuChromeDefaults Defaults)
{
    internal int Radius(bool footer) => footer ? RibbonFrameMetrics.Radius(Theme.ApplicationMenuFooterCornerRadius, RibbonLayoutEngine.Scale(3, Dpi), Dpi)
        : Theme.ApplicationMenuAppearance?.RowCornerRadius is { } radius ? RibbonLayoutEngine.Scale(radius, Dpi) : Defaults.RowRadius;
    internal Rectangle PaintBounds(Rectangle target, bool footer)
    {
        int? authored = footer ? Theme.ApplicationMenuAppearance?.FooterInset : Theme.ApplicationMenuAppearance?.RowInset;
        int inset = authored is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, (Math.Min(target.Width, target.Height) - 1) / 2)) : Defaults.Inset;
        return Rectangle.Inflate(target, -inset, -inset);
    }
    internal Color Border(bool footer)
    {
        var authored = footer ? Theme.ApplicationMenuAppearance?.FooterBorderColor : Theme.ApplicationMenuAppearance?.RowBorderColor;
        return authored is { IsEmpty: false } ink ? ink : footer ? Theme.PopupBorder : Color.Transparent;
    }
    internal int BorderWidth(Rectangle bounds, bool footer)
    {
        int? authored = footer ? Theme.ApplicationMenuAppearance?.FooterBorderWidth : Theme.ApplicationMenuAppearance?.RowBorderWidth;
        return authored is { } width ? Math.Min(RibbonLayoutEngine.Scale(width, Dpi), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2)) : footer ? Defaults.FooterBorderWidth : Defaults.RowBorderWidth;
    }
    internal Rectangle SplitDivider(Rectangle whole, Rectangle arrow)
    {
        int inset = Theme.ApplicationMenuAppearance?.SplitDividerInset is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, whole.Height / 2)) : Defaults.SplitInset;
        int width = Theme.ApplicationMenuAppearance?.SplitDividerWidth is { } stroke ? Math.Min(RibbonLayoutEngine.Scale(stroke, Dpi), Math.Max(0, arrow.Width)) : Defaults.SplitWidth;
        return new(arrow.Left, whole.Top + inset, width, Math.Max(0, whole.Height - 2 * inset));
    }
    internal Color SplitInk => Theme.ApplicationMenuAppearance is { SplitDividerColor.IsEmpty: false } settings ? settings.SplitDividerColor : Theme.Border;
    internal RibbonFill SelectedPrimary
    {
        get
        {
            if (Theme.ApplicationMenuAppearance?.SelectedPrimaryBackground is { } fill) return fill;
            double strength = Theme.ApplicationMenuAppearance?.SelectedPrimaryTintStrength ?? Defaults.PrimaryTint;
            var background = Theme.PopupColor;
            Color Mix(Color pressed) => Color.FromArgb(255, (int)((1 - strength) * background.R + strength * pressed.R),
                (int)((1 - strength) * background.G + strength * pressed.G), (int)((1 - strength) * background.B + strength * pressed.B));
            return new(Mix(Theme.Pressed.Start), Mix(Theme.Pressed.End));
        }
    }
}
