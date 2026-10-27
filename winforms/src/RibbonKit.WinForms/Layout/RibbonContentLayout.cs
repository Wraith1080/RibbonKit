namespace RibbonKit.WinForms.Layout;

// Shared content slots for main-strip and popup presentations. Native text drawing
// consumes these rectangles; it does not independently invent row/column offsets.
internal readonly record struct RibbonContentLayout(Rectangle Icon, Rectangle Text, TextFormatFlags Flags)
{
    internal static RibbonContentLayout Calculate(Rectangle bounds, RibbonItemSize size, bool menu, bool reserveIcon, int dpi, Font font)
    {
        int S(double n) => RibbonLayoutEngine.Scale(n, dpi);
        int iconSize = S(size == RibbonItemSize.Large && !menu ? 32 : 16);
        var icon = new Rectangle(bounds.Left + S(6), bounds.Top + (bounds.Height - iconSize) / 2, iconSize, iconSize);
        var text = Rectangle.Inflate(bounds, -S(5), -S(3));
        // Measurement also uses NoPadding; default GDI glyph overhang padding
        // otherwise truncates neighbouring labels despite a measured exact fit.
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
        if (size == RibbonItemSize.Large && !menu)
        {
            icon.Location = new(bounds.Left + (bounds.Width - iconSize) / 2, bounds.Top + S(8));
            text.Y = bounds.Top + S(45);
            text.Height = Math.Max(0, bounds.Bottom - text.Top - S(3));
            flags |= TextFormatFlags.Top | TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak;
        }
        else
        {
            if (reserveIcon || menu)
            {
                text.X += S(26);
                text.Width = Math.Max(0, text.Width - S(26));
            }
            text.Y = bounds.Top + (bounds.Height - font.Height) / 2;
            text.Height = font.Height;
            flags |= TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
        }
        return new(icon, text, flags);
    }
}
