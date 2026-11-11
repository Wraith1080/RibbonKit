namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonMenuDecorationDefaults(int Inset, int BorderWidth, int SeparatorThickness);

// Paint values only. Existing menu metrics continue to own rows, bands and text slots.
internal readonly record struct RibbonMenuAppearanceMetrics(RibbonTheme Theme, int Dpi, RibbonMenuDecorationDefaults Defaults)
{
    internal int Radius(int fallback, bool header) => (header ? Theme.MenuAppearance?.HeaderCornerRadius : Theme.MenuAppearance?.OverflowCornerRadius) is { } radius ? RibbonLayoutEngine.Scale(radius, Dpi) : fallback;
    internal Rectangle PaintBounds(Rectangle target, bool header)
    {
        int? authored = header ? Theme.MenuAppearance?.HeaderInset : Theme.MenuAppearance?.OverflowInset;
        int inset = authored is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, (Math.Min(target.Width, target.Height) - 1) / 2)) : Defaults.Inset;
        return Rectangle.Inflate(target, -inset, -inset);
    }
    internal Color Border(bool header)
    {
        var color = header ? Theme.MenuAppearance?.HeaderBorderColor : Theme.MenuAppearance?.OverflowBorderColor;
        return color is { IsEmpty: false } ink ? ink : Color.Transparent;
    }
    internal int BorderWidth(Rectangle bounds, bool header, Color color)
    {
        int? authored = header ? Theme.MenuAppearance?.HeaderBorderWidth : Theme.MenuAppearance?.OverflowBorderWidth;
        return color.A == 0 ? 0 : authored is { } width ? Math.Min(RibbonLayoutEngine.Scale(width, Dpi), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2)) : Defaults.BorderWidth;
    }
    internal Rectangle SeparatorBounds(Rectangle row, int horizontalInset, bool ordinary)
    {
        int thickness = ordinary && Theme.MenuAppearance?.SeparatorThickness is { } n ? Math.Min(RibbonLayoutEngine.Scale(n, Dpi), Math.Max(0, row.Height)) : Defaults.SeparatorThickness;
        return new(row.Left + horizontalInset, row.Top + (row.Height - thickness) / 2, Math.Max(0, row.Width - 2 * horizontalInset), Math.Max(0, thickness));
    }
}
