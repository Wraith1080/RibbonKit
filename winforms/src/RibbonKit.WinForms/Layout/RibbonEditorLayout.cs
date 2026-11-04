namespace RibbonKit.WinForms.Layout;

internal static class RibbonEditorLayout
{
    internal static bool IsField(RibbonItem item, RibbonItemSize size) => item is IRibbonEditorItem && (item is not RibbonComboBoxItem || size != RibbonItemSize.Small);
    internal static Rectangle ComboButton(Rectangle field, int dpi, RibbonDensity density = RibbonDensity.Compact, RibbonTheme? theme = null) => new(
        field.Right - Math.Min(Math.Max(0, field.Width), RibbonLayoutEngine.Scale(RibbonEditorMetrics.For(density, theme).ComboButton, dpi)), field.Top,
        Math.Min(Math.Max(0, field.Width), RibbonLayoutEngine.Scale(RibbonEditorMetrics.For(density, theme).ComboButton, dpi)), field.Height);
    internal static Rectangle TextBounds(Rectangle field, int dpi, RibbonTheme? theme = null)
    {
        int inset = RibbonLayoutEngine.Scale(RibbonEditorMetrics.For(RibbonDensity.Compact, theme).TextInset, dpi);
        return new(field.Left + Math.Min(Math.Max(0, field.Width), inset), field.Top, Math.Max(0, field.Width - 2 * inset), field.Height);
    }
    internal static Rectangle ComboText(Rectangle field, int dpi, RibbonDensity density = RibbonDensity.Compact, RibbonTheme? theme = null) =>
        TextBounds(field with { Width = Math.Max(0, ComboButton(field, dpi, density, theme).Left - field.Left) }, dpi, theme);
    internal static int ComboRowHeight(Font font, int dpi, RibbonDensity density = RibbonDensity.Compact, RibbonTheme? theme = null)
    {
        var metrics = RibbonEditorMetrics.For(density, theme);
        return Math.Min(255, Math.Max(RibbonLayoutEngine.Scale(metrics.ChoiceMinimum, dpi), font.Height + metrics.ChoicePaddingHeight(dpi)));
    }
    internal static int LabelWidth(RibbonItem item, Font font, int dpi) =>
        item is RibbonComboBoxItem { ShowLabel: false } || string.IsNullOrEmpty(item.Text) ? 0 : TextRenderer.MeasureText(item.Text, font, new Size(10000, 1000),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width + RibbonLayoutEngine.Scale(RibbonEditorMetrics.For(item.Ribbon?.Density ?? RibbonDensity.Compact, item.Ribbon?.Theme).LabelGap, dpi);

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
        var theme = (item as RibbonItem)?.Ribbon?.Theme;
        int width = Math.Max((int)Math.Ceiling(compactWidth * (double)Height(font, dpi, density, simplified) / Height(font, dpi, RibbonDensity.Compact, simplified)),
            compactWidth + S(RibbonEditorMetrics.For(density, theme).ComboButton) - S(RibbonEditorMetrics.For(RibbonDensity.Compact, theme).ComboButton));
        // Reserve both scaled field insets, including at fractional DPI. Compact
        // keeps its original summed rounding; menu padding adds outer clearance.
        return width + (padding == 0 ? 0 : Math.Max(S(padding), 2 * S(3) + S(padding - 6)));
    }

    internal static int Height(Font font, int dpi, RibbonDensity density, bool simplified = false, RibbonTheme? theme = null)
    {
        var metrics = RibbonEditorMetrics.For(density, theme);
        return Math.Max(RibbonLayoutEngine.Scale(metrics.MinimumHeight(simplified), dpi), font.Height + metrics.FieldPaddingHeight(dpi));
    }

    // Preserve default row rounding. Explicit field heights reserve two logical
    // pixels around the field plus the existing one-logical-pixel row gap.
    internal static int CommandRowLogical(Font font, int dpi, RibbonDensity density, RibbonTheme theme, bool fields)
    {
        int row = Math.Max(RibbonDensityMetrics.For(density).CommandRow, (int)Math.Ceiling(font.Height * 96d / dpi) + 8);
        return fields && RibbonEditorMetrics.HasHeightOverride(theme)
            ? Math.Max(row, (int)Math.Ceiling((Height(font, dpi, density, theme: theme) + RibbonLayoutEngine.Scale(2, dpi)) * 96d / dpi) + 1) : row;
    }

    internal static Rectangle Field(Rectangle bounds, RibbonItem item, int labelWidth, Font font, int dpi, bool simplified = false, bool menu = false)
    {
        int padding = RibbonLayoutEngine.Scale(3, dpi);
        var density = item.Ribbon?.Density ?? RibbonDensity.Compact;
        bool simplifiedCombo = simplified && item is RibbonComboBoxItem;
        int height = Math.Min(bounds.Height, Height(font, dpi, density, simplifiedCombo, item.Ribbon?.Theme));
        int left = bounds.Left + padding + (item is RibbonComboBoxItem { ShowLabel: false } ? 0 : labelWidth);
        int available = Math.Max(0, bounds.Right - padding - left);
        int width = menu && ((IRibbonEditorItem)item).FillMenuWidth ? available
            : Math.Min(InputWidth((IRibbonEditorItem)item, dpi, density, font, simplified: simplifiedCombo), available);
        return new(left, bounds.Top + (bounds.Height - height) / 2,
            width, height);
    }
}
