namespace RibbonKit.WinForms.Layout;

// Every Roomier Large command reserves the same icon/two-caption-row/arrow bands.
// Center the reservation, preserving common baselines even when bands are unused.
// Vertical splits use the shared boundary for paint, hit/keyboard and UIA.
internal readonly record struct RibbonLargeCommandLayout(Rectangle Icon, Rectangle Caption, Rectangle Arrow, int PrimaryHeight)
{
    internal static bool Enabled(RibbonTheme? theme) => theme?.CommandContentVerticalPadding is > 3;
    internal static bool TryCalculate(Rectangle bounds, Font font, int dpi, RibbonDensity density, RibbonTheme? theme,
        bool vertical, bool hasIcon, out RibbonLargeCommandLayout layout)
    {
        layout = default;
        if (!Enabled(theme) || bounds.Width <= 0) return false;
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int side = S(32), gap = S(density == RibbonDensity.Touch ? 8 : 5);
        int captionHeight = font.Height + RibbonCaptionLayout.LineAdvance(font, dpi);
        int height = side + gap + captionHeight + S(4) + S(12);
        if (height > bounds.Height) return false;
        int top = bounds.Top + (bounds.Height - height) / 2;
        var icon = hasIcon ? new Rectangle(bounds.Left + (bounds.Width - side) / 2, top, side, side) : Rectangle.Empty;
        var caption = new Rectangle(bounds.Left + S(5), top + side + gap, Math.Max(0, bounds.Width - 2 * S(5)), captionHeight);
        var arrow = vertical ? new Rectangle(bounds.Left, caption.Bottom + S(4), bounds.Width, S(12)) : Rectangle.Empty;
        int primary = Math.Clamp(top + side + gap / 2 - bounds.Top, 1, Math.Max(1, bounds.Height - 1));
        layout = new(icon, caption, arrow, primary); return true;
    }
}
