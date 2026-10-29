namespace RibbonKit.WinForms.Layout;

internal static class RibbonEditorLayout
{
    internal static Rectangle ComboButton(Rectangle field, int dpi, RibbonDensity density = RibbonDensity.Compact) => new(
        field.Right - Math.Min(field.Width, RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).ComboButton, dpi)), field.Top,
        Math.Min(field.Width, RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).ComboButton, dpi)), field.Height);
    internal static Rectangle ComboText(Rectangle field, int dpi, RibbonDensity density = RibbonDensity.Compact)
    {
        int inset = RibbonLayoutEngine.Scale(4, dpi);
        return new(field.Left + inset, field.Top, Math.Max(0, ComboButton(field, dpi, density).Left - field.Left - 2 * inset), field.Height);
    }
    internal static int ComboRowHeight(Font font, int dpi, RibbonDensity density = RibbonDensity.Compact) => Math.Max(RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).MenuRow, dpi), font.Height + RibbonLayoutEngine.Scale(10, dpi));
    internal static int LabelWidth(RibbonItem item, Font font, int dpi) =>
        string.IsNullOrEmpty(item.Text) ? 0 : TextRenderer.MeasureText(item.Text, font, new Size(10000, 1000),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width + RibbonLayoutEngine.Scale(6, dpi);

    internal static Rectangle Field(Rectangle bounds, RibbonItem item, int labelWidth, Font font, int dpi, bool simplified = false)
    {
        int padding = RibbonLayoutEngine.Scale(3, dpi);
        int minimum = RibbonDensityMetrics.For(item.Ribbon?.Density ?? RibbonDensity.Compact).EditorHeight;
        if (simplified && item is RibbonComboBoxItem) minimum = Math.Max(minimum, RibbonDensityMetrics.For(item.Ribbon?.Density ?? RibbonDensity.Compact).SimplifiedEditorMinimum);
        int height = Math.Min(bounds.Height, Math.Max(RibbonLayoutEngine.Scale(minimum, dpi), font.Height + RibbonLayoutEngine.Scale(8, dpi)));
        int left = bounds.Left + padding + labelWidth;
        return new(left, bounds.Top + (bounds.Height - height) / 2,
            Math.Max(0, Math.Min(RibbonLayoutEngine.Scale(((IRibbonEditorItem)item).InputWidth, dpi), bounds.Right - padding - left)), height);
    }
}
