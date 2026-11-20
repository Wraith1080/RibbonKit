namespace RibbonKit.WinForms.Layout;

internal static class RibbonApplicationButtonMetrics
{
    internal static Rectangle Measure(RibbonControl ribbon)
    {
        if (!ribbon.HasFileMenu || ribbon.EffectiveApplicationButtonShape != RibbonApplicationButtonShape.Orb) return Rectangle.Empty;
        int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
        var a = ribbon.Theme.ApplicationButtonAppearance ?? RibbonApplicationButtonAppearanceSettings.Default;
        var density = RibbonDensityMetrics.For(ribbon.Density);
        var action = ribbon.HeaderAction;
        int utility = action is null ? 0 : Math.Min(action.Value.Element is { } command ? TextRenderer.MeasureText(command.Text, ribbon.Font).Width + S(density.Touch ? 44 : 36) : S(density.SmallWidth), Math.Max(0, ribbon.ClientSize.Width - (action.Value.Element is null ? 0 : S(24))));
        bool tabQat = ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TabRow || ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar && !ribbon.HasWindowCaption;
        int keepQat = ribbon.ShowQuickAccess && tabQat ? S(density.QuickAccessButton + 2 * new RibbonChromeMetrics(ribbon.Theme, ribbon.Density).QuickAccessPadding) : 0;
        int available = Math.Max(0, ribbon.ClientSize.Width - utility - S(24) - keepQat);
        int top = ribbon.WindowCaptionHeight;
        int? resizeInset = ribbon.HasWindowCaption ? ribbon.WindowHost!.CaptionResizeInset : null;
        var bounds = Measure(a, ribbon.Density, ribbon.DeviceDpi, top, S(ribbon.LayoutTheme.TabHeight), available, resizeInset);
        // At very narrow widths the native caption buttons own the whole row.
        // Keep File in the tab band rather than drawing over their hit targets.
        if (ribbon.HasWindowCaption && bounds.Top < top && bounds.Right + ShadowPadding(a, ribbon.DeviceDpi) > ribbon.WindowHost!.Caption.Buttons.Left)
            bounds = Measure(a with { CaptionOverlap = 0 }, ribbon.Density, ribbon.DeviceDpi, top, S(ribbon.LayoutTheme.TabHeight), available, resizeInset);
        return bounds;
    }
    internal static Rectangle Measure(RibbonApplicationButtonAppearanceSettings a, RibbonDensity density, int dpi, int tabTop, int tabHeight, int available, int? nativeResizeInset = null)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int shadow = ShadowPadding(a, dpi);
        int left = Math.Min(Math.Max(S(a.LeftInset), shadow), Math.Max(0, available));
        int top = Math.Max(shadow, Math.Max(Math.Min(tabTop, nativeResizeInset ?? S(8)), tabTop - S(a.CaptionOverlap)));
        int side = Math.Max(0, Math.Min(S(density == RibbonDensity.Touch ? a.TouchDiameter : a.Diameter), Math.Min(Math.Max(0, available - left - shadow), tabTop + tabHeight - top - shadow)));
        return side > 0 ? new(left, top, side, side) : Rectangle.Empty;
    }
    internal static int Reservation(RibbonControl ribbon)
    {
        var orb = Measure(ribbon);
        var a = ribbon.Theme.ApplicationButtonAppearance ?? RibbonApplicationButtonAppearanceSettings.Default;
        return orb.IsEmpty ? 0 : orb.Right + Math.Max(RibbonLayoutEngine.Scale(a.RightGap, ribbon.DeviceDpi), ShadowPadding(a, ribbon.DeviceDpi));
    }
    internal static int ShadowPadding(RibbonApplicationButtonAppearanceSettings a, int dpi) => a.ShadowColor.IsEmpty || a.ShadowColor.A == 0 ? 0 : RibbonLayoutEngine.Scale(a.ShadowBlur, dpi);
    internal static bool Contains(Rectangle bounds, Point point)
    {
        if (!bounds.Contains(point) || bounds.Width <= 0 || bounds.Height <= 0) return false;
        double x = (point.X + .5 - bounds.Left) / bounds.Width * 2 - 1, y = (point.Y + .5 - bounds.Top) / bounds.Height * 2 - 1;
        return x * x + y * y <= 1;
    }
}
