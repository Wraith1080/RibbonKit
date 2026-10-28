namespace RibbonKit.WinForms.Layout;

// The complete header slot remains clickable and occludes overflowing tabs.
// Both utility renderers share the smaller, centered visual bounds within it.
internal static class RibbonHeaderButtonLayout
{
    internal static Rectangle PaintBounds(Rectangle slot, RibbonTheme theme, int dpi, bool square = false)
    {
        int limit = Math.Max(0, Math.Min(slot.Width, slot.Height) / 2);
        int logical = Math.Clamp(theme.HeaderButtonInset, 0, limit);
        int inset = Math.Min(limit, RibbonLayoutEngine.Scale(logical, dpi));
        var bounds = Rectangle.Inflate(slot, -inset, -inset);
        return square && bounds.Height > bounds.Width ? new(bounds.Left, bounds.Top + (bounds.Height - bounds.Width) / 2, bounds.Width, bounds.Width) : bounds;
    }
}
