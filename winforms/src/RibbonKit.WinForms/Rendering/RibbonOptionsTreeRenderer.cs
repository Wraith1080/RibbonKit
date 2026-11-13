using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonOptionsTreeRenderer
{
    internal static void Draw(RibbonRenderer owner, RoundedPainter painter, RibbonPaintContext c, RibbonOptionsTreeSlots slots, string text, bool selected, bool expanded, bool children, int? state, bool enabled, bool showFocus, RibbonCheckBoxItem check, Action<Graphics, Rectangle>? icon)
    {
        var a = c.Theme.OptionsAppearance; var metrics = new RibbonOptionsTreeMetrics(c.Theme, c.DeviceDpi, c.Density);
        if (!selected || a?.TreeSelectedBackground is null && a?.TreeCornerRadius is null)
        { using var background = new SolidBrush(selected ? c.Theme.Checked.Start : c.Theme.Surface); c.Graphics.FillRectangle(background, c.Bounds); }
        else
        { using var background = new SolidBrush(c.Theme.Surface); c.Graphics.FillRectangle(background, c.Bounds); painter.Draw(c.Graphics, c.Bounds, metrics.Radius, a.TreeSelectedBackground ?? new(c.Theme.Checked.Start), Color.Transparent, 0); }
        if (children) DrawChevron(c, slots.Expand, expanded);
        if (state is not null)
        {
            var bounds = metrics.CheckBounds(slots.Check); check.Checked = state == 1; check.Enabled = enabled;
            if (!bounds.IsEmpty) owner.DrawCheckBoxIndicator(c with { Bounds = bounds }, check);
        }
        else if (!slots.Icon.IsEmpty) icon?.Invoke(c.Graphics, slots.Icon);
        if (slots.Text.Width > 0 && slots.Text.Height > 0) TextRenderer.DrawText(c.Graphics, text, c.Font, slots.Text, enabled ? c.Theme.Foreground : SystemColors.GrayText,
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (c.Focused && showFocus) ControlPaint.DrawFocusRectangle(c.Graphics, metrics.FocusBounds(c.Bounds), c.Theme.Foreground, c.Theme.Checked.Start);
    }
    internal static void DrawChevron(RibbonPaintContext c, Rectangle slot, bool expanded)
    {
        var glyph = new RibbonOptionsTreeMetrics(c.Theme, c.DeviceDpi, c.Density).Chevron(slot, expanded);
        if (glyph.Length <= 0 || glyph.Depth <= 0 || glyph.Stroke <= 0) return;
        float cx = slot.Left + (slot.Width - 1) / 2f, cy = slot.Top + (slot.Height - 1) / 2f;
        var saved = c.Graphics.Save();
        try
        {
            if (glyph.Authored) c.Graphics.SetClip(slot, CombineMode.Intersect);
            c.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(c.Theme.SecondaryText, glyph.Stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            c.Graphics.DrawLines(pen, expanded
                ? new PointF[] { new(cx - glyph.Length / 2, cy - glyph.Depth / 2), new(cx, cy + glyph.Depth / 2), new(cx + glyph.Length / 2, cy - glyph.Depth / 2) }
                : new PointF[] { new(cx - glyph.Depth / 2, cy - glyph.Length / 2), new(cx + glyph.Depth / 2, cy), new(cx - glyph.Depth / 2, cy + glyph.Length / 2) });
        }
        finally { c.Graphics.Restore(saved); }
    }
}
