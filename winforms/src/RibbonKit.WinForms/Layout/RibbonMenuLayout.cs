namespace RibbonKit.WinForms.Layout;

// Menu-only geometry; strip/group command slots and native editor metrics stay independent.
internal static class RibbonMenuLayout
{
    internal const TextFormatFlags DescriptionFlags = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;
    internal static bool HasDescription(RibbonItem item) => item is not IRibbonEditorItem and not RibbonMenuSeparatorItem && item.MenuDescription.Length > 0;
    internal static int RowHeight(RibbonItem item, int width, Font font, int dpi)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        if (item is RibbonMenuSeparatorItem) return S(9);
        int basic = Math.Max(S(28), font.Height + S(10));
        if (!HasDescription(item)) return basic;
        int arrow = item is RibbonSplitButtonItem ? 20 : item is RibbonDropDownItem ? 16 : 0;
        int textWidth = Math.Max(1, width - S(36 + arrow));
        int description = Math.Min(2 * font.Height, TextRenderer.MeasureText(item.MenuDescription, font, new(textWidth, int.MaxValue), DescriptionFlags).Height);
        return Math.Max(basic, S(10) + font.Height + S(2) + description);
    }
    internal static RibbonContentLayout Content(RibbonContentLayout content, RibbonItem item, Rectangle bounds, Font font, int dpi) =>
        !HasDescription(item) ? content : content with
        {
            Text = content.Text with { Y = bounds.Top + RibbonLayoutEngine.Scale(5, dpi), Height = font.Height }
        };
    internal static Rectangle Description(Rectangle title, Rectangle row, Font font, int dpi)
    {
        int top = title.Bottom + RibbonLayoutEngine.Scale(2, dpi);
        return new(title.Left, top, title.Width, Math.Min(2 * font.Height, Math.Max(0, row.Bottom - RibbonLayoutEngine.Scale(5, dpi) - top)));
    }
}
