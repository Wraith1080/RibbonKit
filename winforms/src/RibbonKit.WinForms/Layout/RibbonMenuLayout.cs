namespace RibbonKit.WinForms.Layout;

// Menu-only geometry; strip/group command slots and native editor metrics stay independent.
internal static class RibbonMenuLayout
{
    internal const TextFormatFlags DescriptionFlags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;
    // The split secondary area defines one visual indicator column. Keep the
    // same existing narrow-row cap for every menu presentation, including zero.
    internal static Rectangle ArrowColumn(Rectangle row, int dpi, RibbonDensity density, RibbonTheme? theme = null)
    {
        int span = Math.Min(Math.Max(0, row.Width), Math.Min(RibbonLayoutEngine.Scale(RibbonMenuMetrics.For(density, theme).SplitArrow, dpi), Math.Max(1, row.Width / 3)));
        return new(row.Right - span, row.Top, span, row.Height);
    }
    internal static int ArrowCaptionReservation(int rowWidth, int dpi, RibbonDensity density, RibbonTheme? theme, bool chooser)
    {
        var metrics = RibbonMenuMetrics.For(density, theme);
        return Math.Max(ArrowColumn(new(0, 0, rowWidth, 1), dpi, density, theme).Width,
            RibbonLayoutEngine.Scale(chooser ? metrics.ChooserArrow : metrics.SubmenuArrow, dpi));
    }
    internal static Rectangle TextBounds(Rectangle row, Font font, int dpi, RibbonDensity density, RibbonTheme? theme)
    {
        var metrics = RibbonMenuMetrics.For(density, theme);
        return new(row.Left + RibbonLayoutEngine.Scale(metrics.MenuTextInset, dpi), row.Top + (row.Height - font.Height) / 2,
            Math.Max(0, row.Width - RibbonLayoutEngine.Scale(metrics.MenuTextInset + metrics.MenuTextRightPadding, dpi)), font.Height);
    }
    internal static Rectangle HeaderTextBounds(Rectangle bounds, int dpi, RibbonDensity density, RibbonTheme theme)
    {
        int inset = RibbonLayoutEngine.Scale(RibbonMenuMetrics.For(density, theme).MenuHeaderHorizontalPadding, dpi);
        int left = Math.Min(bounds.Right, bounds.Left + inset);
        return new(left, bounds.Top, Math.Max(0, bounds.Right - inset - left), bounds.Height);
    }
    internal static bool IsIllustratedChoice(RibbonItem item) => item is not (RibbonDropDownItem or IRibbonEditorItem or RibbonMenuSeparatorItem)
        && item.Parent is RibbonDropDownItem { MenuPresentation: RibbonMenuPresentation.IllustratedChoices };
    internal static bool HasDescription(RibbonItem item) => item is not IRibbonEditorItem and not RibbonMenuSeparatorItem && item.MenuDescription.Length > 0;
    internal static int RowHeight(RibbonItem item, int width, Font font, int dpi, RibbonDensity? density = null, bool illustrated = true, RibbonTheme? theme = null, bool quickAccess = false)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var metrics = RibbonMenuMetrics.For(density ?? item.Ribbon?.Density ?? RibbonDensity.Compact, theme);
        if (item is RibbonMenuSeparatorItem) return S(metrics.MenuSeparatorHeight);
        if (illustrated && IsIllustratedChoice(item)) return RibbonIllustratedMenuMetrics.For(metrics.Density, theme).RowHeight(font, dpi, HasDescription(item));
        int basic = Math.Max(S(metrics.MenuRow), font.Height + S(10));
        var editorTheme = theme ?? item.Ribbon?.Theme;
        if (!quickAccess && item is IRibbonEditorItem && RibbonEditorMetrics.HasHeightOverride(editorTheme))
            basic = Math.Max(basic, RibbonEditorLayout.Height(font, dpi, metrics.Density, theme: editorTheme) + S(2));
        if (item is not IRibbonEditorItem) basic = Math.Max(basic, metrics.IconMinimumRowHeight(dpi));
        if (!HasDescription(item)) return basic;
        int arrow = item is RibbonSplitButtonItem ? ArrowColumn(new(0, 0, width, 1), dpi, metrics.Density, theme).Width
            : item is RibbonDropDownItem ? ArrowCaptionReservation(width, dpi, metrics.Density, theme, quickAccess) : 0;
        int textWidth = Math.Max(1, width - S(metrics.MenuTextInset + metrics.MenuTextRightPadding) - arrow);
        int description = metrics.Touch ? 2 * font.Height : Math.Min(2 * font.Height, TextRenderer.MeasureText(item.MenuDescription, font, new(textWidth, int.MaxValue), DescriptionFlags).Height);
        return Math.Max(basic, metrics.DescriptionPaddingHeight(dpi) + font.Height + S(metrics.MenuDescriptionGap) + description);
    }
    internal static int[] RowHeights(IReadOnlyList<RibbonItem?> items, int width, Font font, int dpi, RibbonDensity density, bool illustrated = true, RibbonTheme? theme = null, bool quickAccess = false)
    {
        var metrics = RibbonMenuMetrics.For(density, theme);
        var heights = items.Select(item => item is null ? Math.Max(RibbonLayoutEngine.Scale(metrics.MenuRow, dpi), metrics.IconMinimumRowHeight(dpi)) : RowHeight(item, width, font, dpi, density, illustrated, theme, quickAccess)).ToArray();
        int tallest = items.Select((item, index) => item is RibbonMenuSeparatorItem ? 0 : heights[index]).DefaultIfEmpty(0).Max();
        return items.Select((item, index) => item is RibbonMenuSeparatorItem ? heights[index] : tallest).ToArray();
    }
    internal static RibbonContentLayout IllustratedContent(Rectangle row, Font font, int dpi, bool description, RibbonTheme? theme = null)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var metrics = RibbonIllustratedMenuMetrics.For(RibbonDensity.Compact, theme);
        int iconSize = S(metrics.IconSize), textHeight = font.Height * (description ? 3 : 1) + (description ? S(metrics.DescriptionGap) : 0);
        int left = row.Left + S(metrics.Leading), right = row.Right - S(metrics.Trailing);
        if (metrics.HasOverride)
        {
            left = Math.Min(row.Right, left); right = Math.Max(left, right);
            iconSize = Math.Min(iconSize, Math.Max(0, Math.Min(right - left, row.Height)));
        }
        var icon = new Rectangle(left, row.Top + (row.Height - iconSize) / 2, iconSize, iconSize);
        int textLeft = icon.Right + S(metrics.IconTextGap), textTop = row.Top + (row.Height - textHeight) / 2;
        if (metrics.HasOverride) { textLeft = Math.Min(right, textLeft); textTop = Math.Max(row.Top, textTop); }
        var text = new Rectangle(textLeft, textTop, Math.Max(0, right - textLeft), metrics.HasOverride ? Math.Min(font.Height, Math.Max(0, row.Bottom - textTop)) : font.Height);
        return new(icon, text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    internal static Rectangle IllustratedDescription(Rectangle title, Rectangle row, Font font, int dpi, RibbonTheme theme)
    {
        var metrics = RibbonIllustratedMenuMetrics.For(RibbonDensity.Compact, theme);
        int top = title.Bottom + RibbonLayoutEngine.Scale(metrics.DescriptionGap, dpi);
        if (metrics.HasOverride) top = Math.Min(row.Bottom, top);
        return new(title.Left, top, title.Width, Math.Min(2 * font.Height, Math.Max(0, row.Bottom - RibbonLayoutEngine.Scale(5, dpi) - top)));
    }
    internal static RibbonContentLayout Content(RibbonContentLayout content, RibbonItem item, Rectangle bounds, Font font, int dpi, RibbonDensity density = RibbonDensity.Compact, RibbonTheme? theme = null, bool projectedEditorCaption = false)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var metrics = RibbonMenuMetrics.For(density, theme);
        if (item is IRibbonEditorItem) return projectedEditorCaption ? content with { Text = TextBounds(bounds, font, dpi, density, theme) } : content;
        int slot = S(metrics.MenuIcon), size = item is RibbonCheckBoxItem or RibbonRadioButtonItem ? Math.Min(slot, S(metrics.Touch ? 20 : 16)) : slot;
        content = content with { Icon = new(bounds.Left + S(metrics.MenuIconInset) + (slot - size) / 2, bounds.Top + (bounds.Height - size) / 2, size, size),
            Text = TextBounds(bounds, font, dpi, density, theme) };
        return !HasDescription(item) ? content : content with { Text = content.Text with { Y = bounds.Top + S(metrics.MenuDescriptionVerticalPadding), Height = font.Height } };
    }
    internal static Rectangle Description(Rectangle title, Rectangle row, Font font, int dpi, RibbonDensity density = RibbonDensity.Compact, RibbonTheme? theme = null)
    {
        var metrics = RibbonMenuMetrics.For(density, theme);
        int top = title.Bottom + RibbonLayoutEngine.Scale(metrics.MenuDescriptionGap, dpi);
        return new(title.Left, top, title.Width, Math.Min(2 * font.Height, Math.Max(0, row.Bottom - RibbonLayoutEngine.Scale(metrics.MenuDescriptionVerticalPadding, dpi) - top)));
    }
}
