using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal static class RibbonGalleryContentRenderer
{
    internal static void DrawTile(RibbonRenderer owner, RibbonPaintContext c, RibbonGalleryTile tile)
    {
        var m = new RibbonGalleryContentMetrics(c.Theme, c.DeviceDpi, c.Density); var content = m.Content(c.Bounds, c.Font); var art = content.Artwork;
        if (art.Width > 0 && art.Height > 0)
        {
            if (tile.Icon is not null) owner.PaintGalleryImage(c with { Bounds = art }, tile);
            else if ((tile.LargeImage ?? tile.Image) is { } image)
            {
                double scale = Math.Min((double)art.Width / image.Width, (double)art.Height / image.Height);
                var size = new Size(Math.Max(1, (int)Math.Round(image.Width * scale)), Math.Max(1, (int)Math.Round(image.Height * scale)));
                owner.PaintGalleryImage(c with { Bounds = new(art.Left + (art.Width - size.Width) / 2, art.Top + (art.Height - size.Height) / 2, size.Width, size.Height) }, tile);
            }
        }
        if (m.Lines > 0) owner.DrawText(c with { Bounds = content.Label }, tile.Text, tile.EffectiveEnabled ? c.Theme.Foreground : c.Theme.DisabledColor, m.LabelFlags);
        if (c.Focused) owner.DrawFocus(c);
    }
    internal static void DrawUtility(RibbonRenderer owner, RibbonPaintContext c, RibbonGalleryStripButton button, bool enabled)
    {
        if (c.Hot || c.Pressed)
        {
            var ink = new RibbonGalleryAppearanceMetrics(c.Theme, c.DeviceDpi).Border(Color.Transparent, RibbonGalleryChromeKind.Utility);
            owner.PaintGalleryChrome(c, c.Pressed ? c.Theme.Pressed : c.Theme.Hover, ink, RibbonGalleryChromeKind.Utility);
        }
        var m = new RibbonGalleryContentMetrics(c.Theme, c.DeviceDpi, c.Density); var color = m.GlyphInk(enabled ? c.Theme.Foreground : c.Theme.DisabledColor);
        var glyph = button == RibbonGalleryStripButton.Expand ? m.ExpandSlot(c.Bounds) : c.Bounds;
        if (color.A == 0) return;
        RibbonArrowPainter.DrawVertical(c.Graphics, glyph, c.DeviceDpi, color, button == RibbonGalleryStripButton.Up, c.Theme.Chevrons);
        if (button != RibbonGalleryStripButton.Expand) return;
        int inset = m.BarInset; float width = m.BarWidth(glyph), stroke = m.BarStroke;
        if (width <= 0 || stroke <= 0 || glyph.Height <= inset + stroke) return;
        float center = glyph.Left + (glyph.Width - 1) / 2f; var saved = c.Graphics.Save();
        try
        {
            if (m.HasAuthoredBar) c.Graphics.SetClip(c.Bounds, System.Drawing.Drawing2D.CombineMode.Intersect);
            using var pen = new Pen(color, stroke); c.Graphics.DrawLine(pen, center - width / 2f, glyph.Top + inset, center + width / 2f, glyph.Top + inset);
        }
        finally { c.Graphics.Restore(saved); }
    }
}
