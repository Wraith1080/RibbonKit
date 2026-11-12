namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonApplicationMenuSurfaceDefaults(int Stroke, int DividerInset, int HeaderRadius, int DefaultHeaderInset, int ActiveHeaderInset, double PaneTint);

// Paint values only. The existing layout still supplies columns, divider, heading and targets.
internal readonly record struct RibbonApplicationMenuSurfaceMetrics(RibbonTheme Theme, int Dpi, RibbonApplicationMenuSurfaceDefaults Defaults)
{
    private RibbonApplicationMenuAppearanceSettings? Settings => Theme.ApplicationMenuAppearance;
    private int Scale(int n) => RibbonLayoutEngine.Scale(n, Dpi);
    internal int Radius(int actualPopupRadius) => Settings?.InnerFrameCornerRadius is { } n ? Scale(n) : Math.Max(0, actualPopupRadius - Defaults.Stroke);
    internal int BorderWidth(Rectangle frame) => Math.Min(Settings?.InnerFrameBorderWidth is { } n ? Scale(n) : Defaults.Stroke, Math.Max(0, Math.Min(frame.Width, frame.Height) / 2));
    internal Color Border => Settings is { InnerFrameBorderColor.IsEmpty: false } a ? a.InnerFrameBorderColor : Theme.Border;
    internal Region ItemClip(Rectangle frame, int actualPopupRadius)
    {
        int inset = Math.Max(Defaults.Stroke, BorderWidth(frame)); var inner = Rectangle.Inflate(frame, -inset, -inset);
        var clip = RoundedPainter.CreateOpaqueRegion(inner.Size, Math.Max(0, Radius(actualPopupRadius) - inset)); clip.Translate(inner.Left, inner.Top); return clip;
    }
    internal Color DefaultTint
    {
        get
        {
            var popup = Theme.PopupColor; var border = Theme.Border; double tint = Settings?.DefaultPaneTintStrength ?? Defaults.PaneTint;
            return Color.FromArgb(255, (int)((1 - tint) * popup.R + tint * border.R), (int)((1 - tint) * popup.G + tint * border.G), (int)((1 - tint) * popup.B + tint * border.B));
        }
    }
    internal RibbonFill Navigation => Settings?.NavigationBackground ?? new(Theme.PopupColor);
    internal RibbonFill Pane(bool isDefault) => isDefault ? Settings?.DefaultPaneBackground ?? new(DefaultTint) : Settings?.ActivePaneBackground ?? new(Theme.PopupColor);
    internal Rectangle Divider(Rectangle frame, int x)
    {
        int inset = Settings?.NavigationDividerInset is { } n ? Math.Min(Scale(n), Math.Max(0, frame.Height / 2)) : Defaults.DividerInset;
        int width = Settings?.NavigationDividerWidth is { } w ? Math.Min(Scale(w), Math.Max(0, frame.Right - x)) : Defaults.Stroke;
        return new(x, frame.Top + inset, width, Math.Max(0, frame.Height - 2 * inset));
    }
    internal Color DividerInk => Settings is { NavigationDividerColor.IsEmpty: false } a ? a.NavigationDividerColor : Theme.Border;
    internal int HeaderRadius => Settings?.HeaderCornerRadius is { } n ? Scale(n) : Defaults.HeaderRadius;
    internal Rectangle HeaderText(Rectangle bounds, bool isDefault)
    {
        int? authored = isDefault ? Settings?.DefaultHeaderTextInset : Settings?.ActiveHeaderTextInset;
        int inset = authored is { } n ? Math.Min(Scale(n), Math.Max(0, (bounds.Width - 1) / 2)) : Scale(isDefault ? Defaults.DefaultHeaderInset : Defaults.ActiveHeaderInset);
        return Rectangle.Inflate(bounds, -inset, 0);
    }
    internal FontStyle HeaderStyle(FontStyle host) => Settings?.HeaderBold == false ? host & ~FontStyle.Bold : host | FontStyle.Bold;
    internal Color HeaderRuleInk => Settings is { HeaderRuleColor.IsEmpty: false } a ? a.HeaderRuleColor : Theme.Border;
    internal Rectangle HeaderRule(Rectangle bounds)
    {
        int height = Math.Min(Settings?.HeaderRuleThickness is { } n ? Scale(n) : Defaults.Stroke, Math.Max(0, bounds.Height));
        return new(bounds.Left, bounds.Bottom - height, bounds.Width, height);
    }
}
