namespace RibbonKit.WinForms.Layout;

internal readonly record struct RibbonCaptionButtonMetrics(RibbonTheme Theme, int Dpi)
{
    private RibbonCaptionButtonAppearanceSettings? Appearance => Theme.CaptionButtonAppearance;
    private int S(int value) => RibbonLayoutEngine.Scale(value, Dpi);
    internal Rectangle StateBounds(Rectangle target)
    {
        int inset = Math.Min(S(Appearance?.StateInset ?? 0), Math.Max(0, Math.Min(target.Width, target.Height) / 2));
        target.Inflate(-inset, -inset); return target;
    }
    internal int Radius(Rectangle bounds) => Math.Min(S(Appearance?.CornerRadius ?? 0), Math.Max(0, Math.Min(bounds.Width, bounds.Height) / 2));
    internal float Stroke(Rectangle target) => Appearance?.GlyphStrokeWidth is { } stroke
        ? (float)Math.Min(stroke * Dpi / 96d, Math.Max(0, Math.Min(target.Width, target.Height))) : Math.Max(1, Dpi / 96f);
    internal Rectangle GlyphBounds(Rectangle target)
    {
        int size = S(Appearance?.GlyphSize ?? 10);
        if (Appearance is { } a && (a.GlyphSize is not null || a.GlyphStrokeWidth is not null || a.RestoreOffset is not null))
            size = Math.Min(size, Math.Max(0, Math.Min(target.Width, target.Height) - (int)Math.Ceiling(Stroke(target))));
        return size <= 0 ? Rectangle.Empty : new(target.Left + (target.Width - size) / 2, target.Top + (target.Height - size) / 2, size, size);
    }
    internal int RestoreOffset(int size) => Math.Clamp(Appearance?.RestoreOffset is { } offset ? S(offset) : 3, 0, Math.Max(0, size - 1));
}
