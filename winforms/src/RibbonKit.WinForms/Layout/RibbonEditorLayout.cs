namespace RibbonKit.WinForms.Layout;

internal static class RibbonEditorLayout
{
    internal static bool IsField(RibbonItem item, RibbonItemSize size) => item is IRibbonEditorItem && (item is not RibbonComboBoxItem || size != RibbonItemSize.Small);
    internal static Rectangle ComboButton(Rectangle field, int dpi, RibbonDensity density = RibbonDensity.Compact) => new(
        field.Right - Math.Min(field.Width, RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).ComboButton, dpi)), field.Top,
        Math.Min(field.Width, RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).ComboButton, dpi)), field.Height);
    internal static Rectangle ComboText(Rectangle field, int dpi, RibbonDensity density = RibbonDensity.Compact)
    {
        int inset = RibbonLayoutEngine.Scale(4, dpi);
        return new(field.Left + inset, field.Top, Math.Max(0, ComboButton(field, dpi, density).Left - field.Left - 2 * inset), field.Height);
    }
    internal static int ComboRowHeight(Font font, int dpi, RibbonDensity density = RibbonDensity.Compact) => Math.Max(RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).ComboChoiceRow, dpi), font.Height + RibbonLayoutEngine.Scale(10, dpi));
    internal static int LabelWidth(RibbonItem item, Font font, int dpi) =>
        item is RibbonComboBoxItem { ShowLabel: false } || string.IsNullOrEmpty(item.Text) ? 0 : TextRenderer.MeasureText(item.Text, font, new Size(10000, 1000),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width + RibbonLayoutEngine.Scale(6, dpi);

    // Menu labels share the command text column. A hidden-label ComboBox puts
    // its field at that column, without the field's intrinsic leading inset.
    internal static Rectangle ContentBounds(Rectangle row, RibbonItem item, int dpi, RibbonDensity density, RibbonTheme theme, bool menu)
    {
        if (!menu) return row;
        var (leading, trailing) = MenuInsets(item, dpi, density, theme);
        return new(row.Left + leading, row.Top, Math.Max(0, row.Width - leading - trailing), row.Height);
    }

    internal static int MenuWidth(RibbonItem item, int labelWidth, Font font, int dpi, RibbonDensity density, RibbonTheme theme)
    {
        int padding = RibbonLayoutEngine.Scale(3, dpi);
        bool hidden = item is RibbonComboBoxItem { ShowLabel: false };
        var (leading, trailing) = MenuInsets(item, dpi, density, theme);
        return RibbonLayoutEngine.Scale(8, dpi) + leading + trailing + 2 * padding + (hidden ? 0 : labelWidth)
            + InputWidth((IRibbonEditorItem)item, dpi, density, font);
    }

    private static (int Leading, int Trailing) MenuInsets(RibbonItem item, int dpi, RibbonDensity density, RibbonTheme theme)
    {
        var metrics = RibbonMenuMetrics.For(density, theme);
        int padding = RibbonLayoutEngine.Scale(3, dpi);
        return (RibbonLayoutEngine.Scale(metrics.MenuTextInset, dpi) - (item is RibbonComboBoxItem { ShowLabel: false } ? padding : 0),
            Math.Max(0, RibbonLayoutEngine.Scale(metrics.MenuTextRightPadding, dpi) - padding));
    }

    // Grow both field types by the same font/DPI-aware height ratio. The arrow
    // allowance is a floor, so large fonts never lose their Compact text budget.
    internal static int InputWidth(IRibbonEditorItem item, int dpi, RibbonDensity density, Font font, int padding = 0, bool simplified = false)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        if (density == RibbonDensity.Compact) return S(item.InputWidth + padding);
        int compactWidth = S(item.InputWidth);
        int width = Math.Max((int)Math.Ceiling(compactWidth * (double)Height(font, dpi, density, simplified) / Height(font, dpi, RibbonDensity.Compact, simplified)),
            compactWidth + S(RibbonDensityMetrics.For(density).ComboButton) - S(RibbonDensityMetrics.For(RibbonDensity.Compact).ComboButton));
        // Reserve both scaled field insets, including at fractional DPI. Compact
        // keeps its original summed rounding; menu padding adds outer clearance.
        return width + (padding == 0 ? 0 : Math.Max(S(padding), 2 * S(3) + S(padding - 6)));
    }

    private static int Height(Font font, int dpi, RibbonDensity density, bool simplified = false)
    {
        var metrics = RibbonDensityMetrics.For(density);
        int minimum = simplified ? Math.Max(metrics.EditorHeight, metrics.SimplifiedEditorMinimum) : metrics.EditorHeight;
        return Math.Max(RibbonLayoutEngine.Scale(minimum, dpi), font.Height + RibbonLayoutEngine.Scale(8, dpi));
    }

    internal static Rectangle Field(Rectangle bounds, RibbonItem item, int labelWidth, Font font, int dpi, bool simplified = false, bool menu = false)
    {
        int padding = RibbonLayoutEngine.Scale(3, dpi);
        var density = item.Ribbon?.Density ?? RibbonDensity.Compact;
        bool simplifiedCombo = simplified && item is RibbonComboBoxItem;
        int height = Math.Min(bounds.Height, Height(font, dpi, density, simplifiedCombo));
        int left = bounds.Left + padding + (item is RibbonComboBoxItem { ShowLabel: false } ? 0 : labelWidth);
        int available = Math.Max(0, bounds.Right - padding - left);
        int width = menu && ((IRibbonEditorItem)item).FillMenuWidth ? available
            : Math.Min(InputWidth((IRibbonEditorItem)item, dpi, density, font, simplified: simplifiedCombo), available);
        return new(left, bounds.Top + (bounds.Height - height) / 2,
            width, height);
    }
}
