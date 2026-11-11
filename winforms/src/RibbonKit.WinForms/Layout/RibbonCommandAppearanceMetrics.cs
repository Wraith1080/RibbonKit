namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonCommandChromeDefaults(int Inset, int BorderWidth, int SplitWidth, int SplitInset);

// Device values only. The active drawing primitive supplies its own default radius.
internal readonly record struct RibbonCommandAppearanceMetrics
{
    internal RibbonCommandAppearanceMetrics(RibbonTheme theme, int dpi, int defaultRadius, Color border, RibbonCommandChromeDefaults defaults)
    {
        var settings = theme.CommandAppearance;
        Radius = settings?.CornerRadius is { } radius ? RibbonLayoutEngine.Scale(radius, dpi) : defaultRadius;
        Inset = settings?.Inset is { } inset ? RibbonLayoutEngine.Scale(inset, dpi) : defaults.Inset;
        HasInsetOverride = settings?.Inset is not null;
        Border = settings is null || settings.BorderColor.IsEmpty ? border : settings.BorderColor;
        BorderWidth = Border.A == 0 ? 0 : settings?.BorderWidth is { } width ? RibbonLayoutEngine.Scale(width, dpi) : defaults.BorderWidth;
        SplitWidth = settings?.SplitSeparatorWidth is { } separator ? RibbonLayoutEngine.Scale(separator, dpi) : defaults.SplitWidth;
        SplitInset = settings?.SplitSeparatorInset is { } clearance ? RibbonLayoutEngine.Scale(clearance, dpi) : settings?.Inset is not null ? Inset : defaults.SplitInset;
        SplitColor = settings is null || settings.SplitSeparatorColor.IsEmpty ? theme.Border : settings.SplitSeparatorColor;
    }
    internal int Radius { get; }
    internal int Inset { get; }
    internal bool HasInsetOverride { get; }
    internal Color Border { get; }
    internal int BorderWidth { get; }
    internal int SplitWidth { get; }
    internal int SplitInset { get; }
    internal Color SplitColor { get; }
    internal Rectangle StateBounds(Rectangle target)
    {
        int inset = HasInsetOverride ? Math.Min(Inset, Math.Max(0, (Math.Min(target.Width, target.Height) - 1) / 2)) : Inset;
        return Rectangle.Inflate(target, -inset, -inset);
    }
    internal Rectangle SeparatorBounds(Rectangle whole, Rectangle dropDown, bool vertical)
    {
        int inset = Math.Min(SplitInset, Math.Max(0, (vertical ? whole.Width : whole.Height) / 2));
        int stroke = Math.Min(SplitWidth, Math.Max(0, vertical ? dropDown.Height : dropDown.Width));
        return vertical ? new(whole.Left + inset, dropDown.Top, Math.Max(0, whole.Width - 2 * inset), stroke)
            : new(dropDown.Left, whole.Top + inset, stroke, Math.Max(0, whole.Height - 2 * inset));
    }
}
