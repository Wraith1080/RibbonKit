using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed partial class RibbonPopupSurface
{
    private RibbonGalleryLayout? _galleryLayout;
    private bool _galleryKeyboardPreview;
    private bool _galleryMovingFocus;
    private Point? _galleryLastPointer;
    private void PaintGalleryHeadings(Graphics graphics)
    {
        if (_galleryLayout is not { } layout) return;
        var saved = graphics.Save();
        try
        {
            graphics.SetClip(ClientRectangle);
            foreach (var heading in layout.Headings)
            {
                var bounds = heading.Bounds; bounds.Offset(AutoScrollPosition);
                if (bounds.IntersectsWith(ClientRectangle)) _ribbon.Painter.DrawGalleryHeading(new(graphics, bounds, _ribbon.Theme, Font, DeviceDpi), heading.Text);
            }
        }
        finally { graphics.Restore(saved); }
    }
    private void PreviewGalleryPointer(RibbonGalleryTile? tile)
    {
        if (GallerySource is not { } gallery) return;
        var pointer = MousePosition;
        if (_galleryLastPointer == pointer) return;
        _galleryLastPointer = pointer;
        _galleryKeyboardPreview = false;
        gallery.Preview(tile, this);
    }
    private void GalleryScrolled()
    {
        if (GallerySource is not { } gallery || _galleryMovingFocus) return;
        gallery.CancelPreview(this);
        _galleryKeyboardPreview = false;
        _galleryLastPointer = null;
        _hot = null;
    }
    private bool ProcessGalleryKey(Keys keys)
    {
        if (GallerySource is not { } gallery) return false;
        var targets = Targets.Where(t => t.Enabled && t.Element is RibbonGalleryTile).ToArray();
        var current = targets.FirstOrDefault(t => ReferenceEquals(t.Element, _focused));
        if (keys is Keys.Enter or Keys.Space)
        {
            if (current is not null) ActivateTarget(current);
            return true;
        }
        if (keys is not (Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown or Keys.Tab or (Keys.Tab | Keys.Shift))) return false;
        if (targets.Length == 0) return true;
        int index = current is null ? 0 : Array.IndexOf(targets, current);
        RibbonTarget next;
        if (keys == Keys.Home) next = targets[0];
        else if (keys == Keys.End) next = targets[^1];
        else if (keys is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown)
        {
            bool backward = keys is Keys.Up or Keys.PageUp;
            var origin = current ?? targets[0];
            int desiredY = origin.Bounds.Top + (keys is Keys.PageUp or Keys.PageDown ? (backward ? -ClientSize.Height : ClientSize.Height) : 0);
            var rows = targets.Where(t => backward ? t.Bounds.Top < origin.Bounds.Top : t.Bounds.Top > origin.Bounds.Top);
            next = rows.OrderBy(t => Math.Abs((long)t.Bounds.Top - desiredY)).ThenBy(t => Math.Abs(t.Bounds.Left - origin.Bounds.Left)).FirstOrDefault() ?? origin;
        }
        else
        {
            int delta = keys is Keys.Left or (Keys.Tab | Keys.Shift) ? -1 : 1;
            next = targets[Math.Clamp(index + delta, 0, targets.Length - 1)];
        }
        _galleryKeyboardPreview = true;
        _galleryLastPointer = MousePosition;
        _hot = null;
        FocusElement(next.Element!);
        gallery.Preview((RibbonGalleryTile)next.Element!, this);
        return true;
    }
}
