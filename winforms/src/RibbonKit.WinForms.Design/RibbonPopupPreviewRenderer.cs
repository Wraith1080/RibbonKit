using System.Drawing.Imaging;
using RibbonKit.WinForms.Design.Protocol;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms.Design;

// Only detached, design-mode declarations reach this renderer. No popup window,
// editor HWND, application body, command, binding or validation is invoked.
internal static class RibbonPopupPreviewRenderer
{
    internal static RibbonPopupPreview Render(RibbonControl ribbon, RibbonDropDownItem owner,
        string key, string parentKey, IReadOnlyDictionary<RibbonElement, string> keys, bool borrowedContent = false)
    {
        var result = new RibbonPopupPreview { OwnerKey = key, ParentKey = parentKey, Caption = owner.Text };
        bool custom = owner.ContentType is not null || borrowedContent;
        using var surface = custom ? null : new RibbonPopupSurface(ribbon, owner, new(1800, 16384), owner);
        var size = surface?.Size ?? new Size(RibbonLayoutEngine.Scale(320, ribbon.DeviceDpi), RibbonLayoutEngine.Scale(150, ribbon.DeviceDpi));
        if ((long)size.Width * size.Height > 4_194_304)
            throw new ArgumentException("This menu is too large for a static preview. Select a submenu to preview it separately.");
        // Native TextRenderer writes RGB glyphs. An opaque bitmap keeps those
        // pixels opaque when the PNG crosses into the separate designer client.
        using var image = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height), PixelFormat.Format24bppRgb);
        image.SetResolution(ribbon.DeviceDpi, ribbon.DeviceDpi);
        using (var destination = Graphics.FromImage(image))
        using (var buffer = new RibbonBackdropBuffer())
        {
            buffer.Paint(destination, image.Size, 0, ribbon.Theme.Surface, graphics =>
            {
                var context = new RibbonPaintContext(graphics, new(Point.Empty, image.Size), ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density, CompositedText = true };
                ribbon.Painter.DrawSurface(context, true);
                if (custom)
                {
                    int inset = RibbonLayoutEngine.Scale(12, ribbon.DeviceDpi);
                    var bounds = Rectangle.Inflate(context.Bounds, -inset, -inset);
                    ribbon.Painter.DrawText(context with { Bounds = bounds }, "UserControl popup: " + (owner.ContentType?.Name ?? "Application-provided content")
                        + "\r\n\r\nEdit this body in its UserControl designer. Application controls are not created in this preview.",
                        ribbon.Theme.Foreground, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                    Hit(key, context.Bounds);
                }
                else
                {
                    var targets = surface!.Targets;
                    if (!surface.MenuHeaderBounds.IsEmpty)
                        ribbon.Painter.DrawMenuHeader(context with { Bounds = surface.MenuHeaderBounds }, owner.DropDownHeader);
                    foreach (var target in targets.Where(t => !t.DropDownPart && t.Element is RibbonItem))
                    {
                        var item = (RibbonItem)target.Element!;
                        var row = context with { Bounds = target.Bounds, ReserveIcon = target.ReserveIcon };
                        if (item is IRibbonEditorItem editor)
                        {
                            var field = RibbonEditorLayout.Field(target.Bounds, item, target.LabelWidth, ribbon.Font, ribbon.DeviceDpi);
                            if (item is RibbonComboBoxItem combo) ribbon.Painter.DrawComboBox(row, combo, field);
                            else ribbon.Painter.DrawTextBox(row, (RibbonTextBoxItem)item, field);
                            var text = item is RibbonComboBoxItem ? RibbonEditorLayout.ComboText(field, ribbon.DeviceDpi, ribbon.Density)
                                : Rectangle.Inflate(field, -RibbonLayoutEngine.Scale(4, ribbon.DeviceDpi), 0);
                            ribbon.Painter.DrawText(context with { Bounds = text }, editor.Value,
                                item.EffectiveEnabled ? ribbon.Theme.Foreground : ribbon.Theme.DisabledColor,
                                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                        }
                        else if (item is RibbonSplitButtonItem split)
                        {
                            var arrow = targets.Single(t => ReferenceEquals(t.Element, item) && t.DropDownPart);
                            ribbon.Painter.DrawSplitItem(row, context with { Bounds = arrow.Bounds }, split, target.Size, true);
                        }
                        else ribbon.Painter.DrawItem(row, item, target.Size, true);
                        if (!keys.TryGetValue(item, out var itemKey)) continue;
                        Hit(itemKey, target.VisibleBounds);
                        if (item is RibbonDropDownItem and not RibbonGalleryItem)
                        {
                            var arrow = item is RibbonSplitButtonItem
                                ? targets.Single(t => ReferenceEquals(t.Element, item) && t.DropDownPart).VisibleBounds
                                : RibbonEditorLayout.ComboButton(target.Bounds, ribbon.DeviceDpi, ribbon.Density);
                            Hit(itemKey, arrow, true);
                        }
                    }
                }
            }, opaque: true);
        }
        using var stream = new MemoryStream(); image.Save(stream, ImageFormat.Png);
        result.Png = Convert.ToBase64String(stream.ToArray());
        return result;

        void Hit(string itemKey, Rectangle bounds, bool opens = false)
        {
            if (bounds.Width > 0 && bounds.Height > 0)
                result.Hits.Add(new() { Key = itemKey, X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height, OpensSubmenu = opens });
        }
    }
}
