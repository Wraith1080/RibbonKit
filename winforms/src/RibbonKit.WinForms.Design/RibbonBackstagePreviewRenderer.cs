using System.ComponentModel.Design;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Design.Protocol;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms.Design;

internal static class RibbonBackstagePreviewRenderer
{
    internal static RibbonBackstagePreview Render(RibbonControl ribbon, IDesignerHost host, RibbonBackstageSnapshot draft, RibbonTheme? theme = null)
    {
        using var probe = RibbonBackstageCommitter.Probe(ribbon, host, draft, true);
        int dpi = Math.Clamp(draft.PreviewDpi, 96, 384), width = Math.Clamp(draft.PreviewWidth, 320, 1600);
        using var font = new Font(ribbon.Font.FontFamily, ribbon.Font.Size * dpi / ribbon.DeviceDpi, ribbon.Font.Style, ribbon.Font.Unit);
        var nodes = draft.Entries.ToDictionary(node => node.Properties.Single(property => property.Name == "Id").Value);
        foreach (var item in probe.Backstage.NavigationItems)
            if (nodes[item.Id].Key == draft.SelectionKey) item.Visible = true;
        theme ??= ribbon.Theme;
        probe.Theme = theme;
        var initial = RibbonBackstageLayout.Build(new(width, 300), font, dpi, probe.Backstage);
        int contentHeight = probe.Backstage.NavigationItems.Where(item => item.Visible).Sum(initial.EntryHeight);
        int height = Math.Clamp(Math.Max(draft.PreviewHeight, initial.Navigation.Top + contentHeight + initial.Inset), 180, 4096);
        var layout = RibbonBackstageLayout.Build(new(width, height), font, dpi, probe.Backstage);
        using var pixels = new Bitmap(width, height); using var graphics = Graphics.FromImage(pixels);
        var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), theme, font, dpi) { Density = ribbon.Density };
        ribbon.Painter.DrawBackstageBackground(context, layout.Page.Left);
        ribbon.Painter.DrawBackstageEntry(context with { Bounds = layout.Back }, null, false);
        var result = new RibbonBackstagePreview();
        foreach (var slot in layout.Arrange(probe.Backstage))
        {
            var bounds = slot.VisibleBounds; if (bounds.IsEmpty) continue;
            var node = nodes[slot.Item.Id]; bool selected = node.Key == draft.SelectionKey;
            var state = graphics.Save();
            try
            {
                graphics.SetClip(slot.Clip, System.Drawing.Drawing2D.CombineMode.Intersect);
                ribbon.Painter.DrawBackstageEntry(context with { Bounds = slot.Bounds, Hot = selected && slot.Item is not RibbonBackstagePageItem }, slot.Item, selected && slot.Item is RibbonBackstagePageItem);
            }
            finally { graphics.Restore(state); }
            result.Regions.Add(new() { Key = node.Key, X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height });
        }
        var pageBounds = Rectangle.Inflate(layout.PageContentBounds(), -RibbonLayoutEngine.Scale(16, dpi), -RibbonLayoutEngine.Scale(16, dpi));
        TextRenderer.DrawText(graphics, RibbonBackstageDesignPreview.PlaceholderText, font, pageBounds, theme.Foreground,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);
        using var stream = new MemoryStream(); pixels.Save(stream, ImageFormat.Png); result.Png = Convert.ToBase64String(stream.ToArray()); return result;
    }
}
