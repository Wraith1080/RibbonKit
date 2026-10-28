namespace RibbonKit.WinForms.Layout;

// Menu-only geometry; strip/group command slots and native editor metrics stay independent.
internal static class RibbonMenuLayout
{
    internal const int IllustratedWidth = 300, IllustrationSize = 36, IllustratedMinimumHeight = 64;
    internal const TextFormatFlags DescriptionFlags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;
    internal static bool IsIllustratedChoice(RibbonItem item) => item is not (RibbonDropDownItem or IRibbonEditorItem or RibbonMenuSeparatorItem)
        && item.Parent is RibbonDropDownItem { MenuPresentation: RibbonMenuPresentation.IllustratedChoices };
    internal static bool HasDescription(RibbonItem item) => item is not IRibbonEditorItem and not RibbonMenuSeparatorItem && item.MenuDescription.Length > 0;
    internal static int RowHeight(RibbonItem item, int width, Font font, int dpi, RibbonDensity? density = null, bool illustrated = true)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        if (item is RibbonMenuSeparatorItem) return S(9);
        if (illustrated && IsIllustratedChoice(item)) return Math.Max(S(IllustratedMinimumHeight), font.Height * (HasDescription(item) ? 3 : 1) + S(12));
        var metrics = RibbonDensityMetrics.For(density ?? item.Ribbon?.Density ?? RibbonDensity.Compact);
        int basic = Math.Max(S(metrics.MenuRow), font.Height + S(10));
        if (item is not IRibbonEditorItem) basic = Math.Max(basic, S(metrics.MenuIconMinimumRow));
        if (!HasDescription(item)) return basic;
        int arrow = item is RibbonSplitButtonItem ? metrics.SplitArrow : item is RibbonDropDownItem ? metrics.Touch ? 28 : 16 : 0;
        int textWidth = Math.Max(1, width - S(metrics.MenuTextInset + metrics.MenuTextRightPadding + arrow));
        int description = metrics.Touch ? 2 * font.Height : Math.Min(2 * font.Height, TextRenderer.MeasureText(item.MenuDescription, font, new(textWidth, int.MaxValue), DescriptionFlags).Height);
        return Math.Max(basic, S(2 * metrics.MenuVerticalPadding) + font.Height + S(metrics.MenuDescriptionGap) + description);
    }
    internal static int[] RowHeights(IReadOnlyList<RibbonItem?> items, int width, Font font, int dpi, RibbonDensity density, bool illustrated = true)
    {
        var metrics = RibbonDensityMetrics.For(density);
        var heights = items.Select(item => item is null ? RibbonLayoutEngine.Scale(Math.Max(metrics.MenuRow, metrics.MenuIconMinimumRow), dpi) : RowHeight(item, width, font, dpi, density, illustrated)).ToArray();
        int tallest = items.Select((item, index) => item is RibbonMenuSeparatorItem ? 0 : heights[index]).DefaultIfEmpty(0).Max();
        return items.Select((item, index) => item is RibbonMenuSeparatorItem ? heights[index] : tallest).ToArray();
    }
    internal static RibbonContentLayout IllustratedContent(Rectangle row, Font font, int dpi, bool description)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        int iconSize = S(IllustrationSize), textHeight = font.Height * (description ? 3 : 1) + (description ? S(2) : 0);
        var icon = new Rectangle(row.Left + S(8), row.Top + (row.Height - iconSize) / 2, iconSize, iconSize);
        var text = new Rectangle(icon.Right + S(12), row.Top + (row.Height - textHeight) / 2, Math.Max(0, row.Right - S(8) - icon.Right - S(12)), font.Height);
        return new(icon, text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    internal static RibbonContentLayout Content(RibbonContentLayout content, RibbonItem item, Rectangle bounds, Font font, int dpi, RibbonDensity density = RibbonDensity.Compact)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var metrics = RibbonDensityMetrics.For(density);
        if (item is IRibbonEditorItem) return content;
        int slot = S(metrics.MenuIcon), size = item is RibbonCheckBoxItem or RibbonRadioButtonItem ? S(metrics.Touch ? 20 : 16) : slot;
        content = content with { Icon = new(bounds.Left + S(metrics.MenuIconInset) + (slot - size) / 2, bounds.Top + (bounds.Height - size) / 2, size, size),
            Text = new(bounds.Left + S(metrics.MenuTextInset), bounds.Top + (bounds.Height - font.Height) / 2, Math.Max(0, bounds.Width - S(metrics.MenuTextInset + metrics.MenuTextRightPadding)), font.Height) };
        return !HasDescription(item) ? content : content with { Text = content.Text with { Y = bounds.Top + S(metrics.MenuVerticalPadding), Height = font.Height } };
    }
    internal static Rectangle Description(Rectangle title, Rectangle row, Font font, int dpi, RibbonDensity density = RibbonDensity.Compact)
    {
        var metrics = RibbonDensityMetrics.For(density);
        int top = title.Bottom + RibbonLayoutEngine.Scale(metrics.MenuDescriptionGap, dpi);
        return new(title.Left, top, title.Width, Math.Min(2 * font.Height, Math.Max(0, row.Bottom - RibbonLayoutEngine.Scale(metrics.MenuVerticalPadding, dpi) - top)));
    }
}
