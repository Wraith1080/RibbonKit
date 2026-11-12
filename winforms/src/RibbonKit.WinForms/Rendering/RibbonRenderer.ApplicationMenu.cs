using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Paints the Application Menu's measured columns and fixed footer using shared menu surface and border roles. Native application content keeps its own styling.</summary>
    public virtual void DrawApplicationMenuBackground(RibbonPaintContext context, Rectangle navigation, Rectangle pane, Rectangle footer)
        => RibbonApplicationMenuSurfaceRenderer.DrawBackground(this, context, navigation, pane);
    internal static Rectangle ApplicationFrame(Rectangle bounds, Rectangle navigation) => new(navigation.Left - 1, navigation.Top - 1,
        Math.Max(1, bounds.Width - 2 * (navigation.Left - 1)), navigation.Height + 2);
    private void DrawApplicationMenuHeader(RibbonPaintContext context, string text)
        => RibbonApplicationMenuSurfaceRenderer.DrawHeader(this, context, text);
    private void DrawApplicationMenuItem(RibbonPaintContext context, RibbonItem item, bool arrow = true, bool paintChrome = true)
    {
        bool enabled = item.EffectiveEnabled; context = EnabledContext(context, enabled);
        var bounds = context.Bounds; bool footer = context.ApplicationMenuPart == RibbonApplicationMenuPart.Footer;
        if (item is RibbonMenuSeparatorItem separator) { DrawMenuSeparator(context, separator); return; }
        bool check = item is RibbonToggleItem { Checked: true } or RibbonCheckBoxItem { Checked: true } or RibbonRadioButtonItem { Checked: true } or RibbonSplitButtonItem { Checked: true };
        bool open = enabled && item is RibbonDropDownItem { IsDropDownOpen: true };
        if (paintChrome) RibbonApplicationMenuChromeRenderer.DrawItemState(this, context, footer, check, open);
        RibbonApplicationMenuContentRenderer.Draw(this, context, item, arrow);
    }
    internal void PaintApplicationMenuImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size) => DrawItemImage(context, item, size);
    private void DrawApplicationMenuSplit(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item)
    {
        primary = EnabledContext(primary, item.EffectiveEnabled); dropDown = EnabledContext(dropDown, item.EffectiveEnabled);
        var whole = primary with { Bounds = Rectangle.Union(primary.Bounds, dropDown.Bounds), Hot = false, Pressed = false, Focused = false };
        bool footer = primary.ApplicationMenuPart == RibbonApplicationMenuPart.Footer;
        bool open = item.EffectiveEnabled && item.IsDropDownOpen;
        RibbonApplicationMenuChromeRenderer.DrawSplitState(this, primary, dropDown, item.Checked, open, footer);
        DrawApplicationMenuItem(whole, item, false, paintChrome: false);
        DrawSubmenuArrow(dropDown, item.EffectiveEnabled ? primary.Theme.SecondaryText : primary.Theme.DisabledColor);
        if (primary.Focused) DrawFocus(primary); if (dropDown.Focused) DrawFocus(dropDown);
    }
    internal bool DrawApplicationFileButton(RibbonPaintContext context, RibbonDropDownItem menu)
    {
        if (!menu.EffectiveEnabled || !menu.IsDropDownOpen || context.FileMenuClosing || menu.Ribbon?.FileMenuMode != RibbonFileMenuMode.CommandMenu) return false;
        var appearance = context.Theme.HeaderAppearance;
        int inset = appearance?.FileHighlightInset is { } authored ? Math.Min(RibbonLayoutEngine.Scale(authored, context.DeviceDpi), Math.Max(0, (Math.Min(context.Bounds.Width, context.Bounds.Height) - 1) / 2)) : RibbonLayoutEngine.Scale(3, context.DeviceDpi);
        var bounds = Rectangle.Inflate(context.Bounds, -inset, -inset);
        var fill = appearance?.FileHighlightBackground ?? new(context.Theme.Accent);
        PaintRounded(context with { Bounds = bounds }, RibbonFrameMetrics.Radius(context.Theme.ApplicationMenuFileButtonCornerRadius, RibbonLayoutEngine.Scale(3, context.DeviceDpi), context.DeviceDpi), fill, Color.Transparent, 0);
        static double Channel(byte channel) { double v = channel / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        var accent = fill.Start;
        var foreground = appearance is not null && !appearance.FileHighlightForeground.IsEmpty ? appearance.FileHighlightForeground
            : .2126 * Channel(accent.R) + .7152 * Channel(accent.G) + .0722 * Channel(accent.B) > .179 ? Color.Black : Color.White;
        DrawText(context with { Bounds = bounds }, menu.Text, foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (context.Focused) DrawFocus(context with { Bounds = bounds }); return true;
    }
}
