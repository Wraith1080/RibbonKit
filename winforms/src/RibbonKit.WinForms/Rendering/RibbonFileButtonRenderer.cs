using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonFileButtonRenderer
{
    internal static RibbonFill OpenFill(RibbonTheme theme)
    {
        if (theme.FileButtonAppearance is not { } a) return theme.HeaderPressed ?? theme.Pressed;
        return a.OpenBackground ?? (theme.IdentityAccent is { } recipe ? recipe.AdaptFill(recipe.FileOpenBackground, theme.Accent, recipe.FileStrength) : RibbonFileButtonAppearanceSettings.Darken(theme.Accent, a.OpenDarkening));
    }
    internal static bool Draw(RibbonRenderer owner, RibbonPaintContext context, RibbonDropDownItem menu)
    {
        if (context.Theme.FileButtonAppearance is not { } a) return false;
        bool enabled = menu.EffectiveEnabled, open = enabled && (menu.IsDropDownOpen || context.FileMenuPreviewOpen) && !context.FileMenuClosing;
        var recipe = context.Theme.IdentityAccent;
        RibbonFill Identity(RibbonFill reference) => recipe!.AdaptFill(reference, context.Theme.Accent, recipe.FileStrength);
        var idle = a.Background ?? (recipe is null ? new RibbonFill(context.Theme.Accent) : Identity(recipe.FileBackground));
        var fill = !enabled ? a.DisabledBackground ?? idle : open ? OpenFill(context.Theme)
            : context.Pressed ? a.PressedBackground ?? (recipe is null ? RibbonFileButtonAppearanceSettings.Darken(context.Theme.Accent, a.PressedDarkening) : Identity(recipe.FilePressedBackground))
            : context.Hot ? a.HoverBackground ?? (recipe is null ? RibbonFileButtonAppearanceSettings.Darken(context.Theme.Accent, a.HoverDarkening) : Identity(recipe.FileHoverBackground)) : idle;
        var ink = !enabled ? a.DisabledForeground.IsEmpty ? context.Theme.DisabledColor : a.DisabledForeground
            : open ? a.OpenForeground : context.Pressed ? a.PressedForeground : context.Hot ? a.HoverForeground : a.Foreground;
        if (ink.IsEmpty) ink = a.Foreground.IsEmpty ? context.Theme.HeaderTextColor : a.Foreground;
        var border = !a.BorderColor.IsEmpty || recipe is null ? a.BorderColor : Identity(new(recipe.FileBorderColor)).Start;
        owner.PaintFilledHeaderState(context with { StatePressed = open || context.Pressed, IsBottomEdgeConnected = a.SquareBottomCorners }, RibbonLayoutEngine.Scale(context.Theme.HeaderFilledCornerRadius ?? 0, context.DeviceDpi), fill, enabled ? a.StateChrome : null, border);
        owner.DrawText(context, menu.Text, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping);
        if (enabled && context.Focused) owner.DrawFocus(context with { Theme = context.Theme with { Accent = ink } });
        return true;
    }
}
