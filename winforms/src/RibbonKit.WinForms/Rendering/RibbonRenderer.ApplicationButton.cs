using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    internal bool DrawApplicationOrb(RibbonPaintContext context, RibbonDropDownItem menu)
    {
        if ((context.ApplicationButtonShape ?? context.Theme.ApplicationButtonShape) != RibbonApplicationButtonShape.Orb) return false;
        var a = context.Theme.ApplicationButtonAppearance ?? RibbonApplicationButtonAppearanceSettings.Default;
        bool enabled = menu.EffectiveEnabled;
        bool open = enabled && (menu.IsDropDownOpen || context.FileMenuPreviewOpen) && !context.FileMenuClosing;
        var idle = a.Background ?? new(context.Theme.Surface);
        var fill = !enabled ? a.DisabledBackground ?? idle : open ? a.OpenBackground ?? a.PressedBackground ?? context.Theme.Pressed
            : context.Pressed ? a.PressedBackground ?? context.Theme.Pressed : context.Hot ? a.HoverBackground ?? context.Theme.Hover : idle;
        Color Fallback(Color value, Color fallback) => value.IsEmpty ? fallback : value;
        var border = Fallback(a.BorderColor, context.Theme.Border);
        if (enabled) border = open ? Fallback(a.OpenBorderColor, Fallback(a.PressedBorderColor, border)) : context.Pressed ? Fallback(a.PressedBorderColor, border) : context.Hot ? Fallback(a.HoverBorderColor, border) : border;
        int S(int n) => RibbonLayoutEngine.Scale(n, context.DeviceDpi);
        int side = Math.Min(context.Bounds.Width, context.Bounds.Height);
        var sphere = new Rectangle(context.Bounds.Left + (context.Bounds.Width - side) / 2, context.Bounds.Top + (context.Bounds.Height - side) / 2, side, side);
        if (side <= 0) return true;
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.CompositingMode = CompositingMode.SourceOver;
            if (!a.ShadowColor.IsEmpty) _shadow.Draw(context.Graphics, sphere, side / 2, S(a.ShadowBlur), a.ShadowColor);
            _rounded.Draw(context.Graphics, sphere, side / 2, fill, border, S(a.BorderWidth));
            int inset = Math.Min(S(a.BorderWidth), Math.Max(0, (side - 1) / 2));
            if (!a.InnerBorderColor.IsEmpty && a.InnerBorderWidth > 0)
                _rounded.Draw(context.Graphics, Rectangle.Inflate(sphere, -inset, -inset), Math.Max(0, side / 2 - inset), new(Color.Transparent), a.InnerBorderColor, S(a.InnerBorderWidth));
            int glyph = Math.Min(S(a.GlyphSize), Math.Max(0, side - 2 * S(4)));
            var glyphBounds = new Rectangle(sphere.Left + (side - glyph) / 2, sphere.Top + (side - glyph) / 2, glyph, glyph);
            if (glyph > 0)
            {
                var ink = enabled ? Fallback(a.GlyphColor, context.Theme.HeaderTextColor) : context.Theme.DisabledColor;
                var glyphState = context.Graphics.Save();
                try
                {
                    context.Graphics.SetClip(glyphBounds, CombineMode.Intersect);
                    DrawApplicationButtonGlyph(context with { Bounds = glyphBounds, Theme = context.Theme with { Foreground = ink } }, enabled);
                }
                finally { context.Graphics.Restore(glyphState); }
            }
            if (enabled && context.Focused)
                DrawFocus(context with { Bounds = sphere, IsApplicationButtonFocus = true });
        }
        finally { context.Graphics.Restore(saved); }
        return true;
    }
    internal void PaintApplicationButtonFocus(RibbonPaintContext context)
    {
        var bounds = RibbonSharedEffectMetrics.FocusBounds(context);
        _rounded.Draw(context.Graphics, bounds, Math.Min(bounds.Width, bounds.Height) / 2, new(Color.Transparent), context.Theme.Accent, RibbonSharedEffectMetrics.FocusStroke(context));
    }
    /// <summary>Paints an Orb glyph into its measured, clipped square. Uses the borrowed context icon when supplied, otherwise the shared four-square vector. Overrides replace only the glyph, retaining sphere, state, focus and input ownership.</summary>
    public virtual void DrawApplicationButtonGlyph(RibbonPaintContext context, bool enabled)
    {
        if (context.ApplicationButtonIcon is { } icon)
        {
            icon.Draw(context.Graphics, context.Bounds, new(context.DeviceDpi, context.Theme.Foreground, enabled));
            return;
        }
        int gap = Math.Max(1, RibbonLayoutEngine.Scale(2, context.DeviceDpi));
        int side = Math.Max(0, (Math.Min(context.Bounds.Width, context.Bounds.Height) - gap) / 2);
        if (side == 0) return;
        int size = 2 * side + gap;
        int left = context.Bounds.Left + (context.Bounds.Width - size) / 2, top = context.Bounds.Top + (context.Bounds.Height - size) / 2;
        using var brush = new SolidBrush(context.Theme.Foreground);
        for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++)
            context.Graphics.FillRectangle(brush, left + x * (side + gap), top + y * (side + gap), side, side);
    }
}
