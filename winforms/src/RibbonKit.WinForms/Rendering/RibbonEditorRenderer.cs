using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal enum RibbonEditorChromeKind { None, Field, Choice }

// Both light families share these recipes. The actual public renderer supplies
// chrome/text/arrow hooks and its own neutral or Office primitive defaults.
internal static class RibbonEditorRenderer
{
    internal static void DrawField(RibbonRenderer owner, RibbonPaintContext c, Rectangle bounds, bool combo, bool open, bool enabled)
    {
        var metrics = new RibbonEditorAppearanceMetrics(c.Theme, c.DeviceDpi, RibbonPresentationResolver.EditorChrome);
        bool active = combo ? c.Hot || c.Focused || c.Pressed || open : c.Focused && enabled;
        var border = active ? metrics.ActiveBorder : c.Theme.EditorBorderColor;
        var field = c with { Bounds = bounds };
        owner.PaintEditorChrome(field, new(c.Theme.EditorColor), border);
        if (!combo) return;
        var button = RibbonEditorLayout.ComboButton(bounds, c.DeviceDpi, c.Density, c.Theme);
        if (c.Hot || c.Pressed || open)
        {
            var saved = c.Graphics.Save();
            try
            {
                c.Graphics.SetClip(button, CombineMode.Intersect);
                owner.PaintEditorChrome(field, c.Pressed ? c.Theme.Pressed : open ? c.Theme.Checked : c.Theme.Hover, border);
            }
            finally { c.Graphics.Restore(saved); }
        }
        owner.DrawDropDownArrow(c with { Bounds = button }, enabled ? c.Theme.EditorTextColor : c.Theme.DisabledColor);
    }
    internal static void DrawChoice(RibbonRenderer owner, RibbonPaintContext c, string text, bool selected, bool enabled)
    {
        using var surface = new SolidBrush(c.Theme.PopupColor);
        c.Graphics.FillRectangle(surface, c.Bounds);
        if (selected || c.Hot || c.Pressed)
            owner.PaintEditorChrome(c, c.Pressed ? c.Theme.Pressed : c.Hot ? c.Theme.Hover : c.Theme.Checked, Color.Transparent, choice: true);
        var metrics = new RibbonEditorAppearanceMetrics(c.Theme, c.DeviceDpi, RibbonPresentationResolver.EditorChrome);
        owner.DrawText(c with { Bounds = metrics.ChoiceTextBounds(c.Bounds) }, text, enabled ? c.Theme.EditorTextColor : c.Theme.DisabledColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}

public partial class RibbonRenderer
{
    internal void PaintEditorChrome(RibbonPaintContext c, RibbonFill fill, Color border, bool choice = false) =>
        DrawChrome(c with { EditorChrome = choice ? RibbonEditorChromeKind.Choice : RibbonEditorChromeKind.Field }, fill, border);
    private bool TryPaintEditorChrome(RibbonPaintContext c, RibbonFill fill, Color border, int defaultRadius)
    {
        var settings = c.Theme.EditorAppearance;
        bool choice = c.EditorChrome == RibbonEditorChromeKind.Choice;
        if (settings is null || (choice ? !settings.HasChoiceChrome : c.EditorChrome != RibbonEditorChromeKind.Field || !settings.HasFieldChrome)) return false;
        var metrics = new RibbonEditorAppearanceMetrics(c.Theme, c.DeviceDpi, RibbonPresentationResolver.EditorChrome);
        var bounds = metrics.PaintBounds(c.Bounds, choice);
        PaintRounded(c with { Bounds = bounds }, metrics.Radius(defaultRadius, choice), fill, border, choice ? 0 : metrics.BorderWidth(bounds, border));
        return true;
    }
}
