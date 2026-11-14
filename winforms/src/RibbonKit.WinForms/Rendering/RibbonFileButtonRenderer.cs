using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonFileButtonRenderer
{
    internal static bool Draw(RibbonRenderer owner, RibbonPaintContext context, RibbonDropDownItem menu)
    {
        if (context.Theme.FileButtonAppearance is not { } a) return false;
        bool enabled = menu.EffectiveEnabled, open = enabled && menu.IsDropDownOpen && !context.FileMenuClosing;
        var idle = a.Background ?? new RibbonFill(context.Theme.Accent);
        var fill = !enabled ? a.DisabledBackground ?? idle : open ? a.OpenBackground ?? RibbonFileButtonAppearanceSettings.Darken(context.Theme.Accent, a.OpenDarkening)
            : context.Pressed ? a.PressedBackground ?? RibbonFileButtonAppearanceSettings.Darken(context.Theme.Accent, a.PressedDarkening)
            : context.Hot ? a.HoverBackground ?? RibbonFileButtonAppearanceSettings.Darken(context.Theme.Accent, a.HoverDarkening) : idle;
        var ink = !enabled ? a.DisabledForeground.IsEmpty ? context.Theme.DisabledColor : a.DisabledForeground
            : open ? a.OpenForeground : context.Pressed ? a.PressedForeground : context.Hot ? a.HoverForeground : a.Foreground;
        if (ink.IsEmpty) ink = a.Foreground.IsEmpty ? context.Theme.HeaderTextColor : a.Foreground;
        owner.PaintFilledHeaderState(context with { StatePressed = open || context.Pressed }, RibbonLayoutEngine.Scale(context.Theme.HeaderFilledCornerRadius ?? 0, context.DeviceDpi), fill, enabled ? a.StateChrome : null);
        owner.DrawText(context, menu.Text, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
        if (enabled && context.Focused) owner.DrawFocus(context with { Theme = context.Theme with { Accent = ink } });
        return true;
    }
}
