namespace RibbonKit.WinForms.Layout;

// The complete header slot remains clickable and occludes overflowing tabs.
// Both utility renderers share the smaller, centered visual bounds within it.
internal static class RibbonHeaderButtonLayout
{
    internal static Rectangle PaintBounds(Rectangle slot, RibbonTheme theme, int dpi)
    {
        int limit = Math.Max(0, Math.Min(slot.Width, slot.Height) / 2);
        int logical = Math.Clamp(theme.HeaderButtonInset, 0, limit);
        int inset = Math.Min(limit, RibbonLayoutEngine.Scale(logical, dpi));
        return Rectangle.Inflate(slot, -inset, -inset);
    }
}
