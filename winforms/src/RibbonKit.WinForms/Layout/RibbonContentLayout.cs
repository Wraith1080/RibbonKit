namespace RibbonKit.WinForms.Layout;

// Shared content slots for main-strip and popup presentations. Native text drawing
// consumes these rectangles; it does not independently invent row/column offsets.
internal readonly record struct RibbonContentLayout(Rectangle Icon, Rectangle Text, TextFormatFlags Flags)
{
    internal static RibbonContentLayout Calculate(Rectangle bounds, RibbonItemSize size, bool menu, bool reserveIcon, int dpi, Font font, RibbonDensity density = RibbonDensity.Compact, bool verticalSplit = false, int captionRows = 2)
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
            if (density == RibbonDensity.Touch)
            {
                var metrics = RibbonDensityMetrics.For(density);
                int imageBand = LargeImageBandHeight(bounds, font, dpi, density);
                icon.Y = bounds.Top + (imageBand - iconSize) / 2;
                int captionHeight = font.Height + (verticalSplit ? Math.Clamp(captionRows, 1, 2) - 1 : 1) * RibbonCaptionLayout.LineAdvance(font, dpi);
                int tail = verticalSplit ? S(metrics.LargeCaptionArrowGap + metrics.LargeCaptionArrowHeight) : S(3);
                // Ordinary captions stay close to the icon. A vertical split
                // centers its actual label/indicator stack in the lower action.
                text.Y = verticalSplit ? bounds.Top + imageBand + Math.Max(0, (bounds.Height - imageBand - captionHeight - tail) / 2)
                    : icon.Bottom + S(metrics.LargeIconCaptionGap);
                text.Height = Math.Min(captionHeight, Math.Max(0, bounds.Bottom - text.Top - tail));
            }
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

    internal static int LargeImageBandHeight(Rectangle bounds, Font font, int dpi, RibbonDensity density)
    {
        var metrics = RibbonDensityMetrics.For(density);
        int desired = RibbonLayoutEngine.Scale(metrics.VerticalPrimary, dpi);
        if (density == RibbonDensity.Touch)
        {
            int caption = font.Height + RibbonCaptionLayout.LineAdvance(font, dpi)
                + RibbonLayoutEngine.Scale(metrics.LargeCaptionArrowGap + metrics.LargeCaptionArrowHeight + 6, dpi);
            desired = Math.Min(desired, Math.Max(1, bounds.Height - caption));
        }
        return Math.Clamp(desired, 1, Math.Max(1, bounds.Height - 1));
    }

    internal static Rectangle LargeDropDownArrow(Rectangle bounds, Font font, int dpi, RibbonDensity density, int captionRows)
    {
        var content = Calculate(bounds, RibbonItemSize.Large, false, true, dpi, font, density, true, captionRows);
        var metrics = RibbonDensityMetrics.For(density);
        int top = content.Text.Bottom + RibbonLayoutEngine.Scale(metrics.LargeCaptionArrowGap, dpi);
        return new(bounds.Left, top, bounds.Width, Math.Max(0, Math.Min(RibbonLayoutEngine.Scale(metrics.LargeCaptionArrowHeight, dpi), bounds.Bottom - top)));
    }
}
