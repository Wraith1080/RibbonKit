namespace RibbonKit.WinForms.Layout;

internal static class RibbonSplitLayout
{
    internal static bool IsSplit(RibbonElement? element) => element is RibbonSplitButtonItem or RibbonQuickAccessDropDownReference { Source: RibbonSplitButtonItem };
    internal static bool IsVertical(RibbonSplitButtonItem item, RibbonItemSize size, bool menu) =>
        !menu && item.Layout == RibbonSplitButtonLayout.Vertical && size == RibbonItemSize.Large;

    internal static (Rectangle Primary, Rectangle DropDown) Calculate(Rectangle bounds, RibbonSplitButtonItem item, RibbonItemSize size, bool menu, int dpi)
    {
        if (IsVertical(item, size, menu))
        {
            int primaryHeight = Math.Clamp(RibbonLayoutEngine.Scale(44, dpi), 1, Math.Max(1, bounds.Height - 1));
            return (bounds with { Height = primaryHeight }, new(bounds.Left, bounds.Top + primaryHeight, bounds.Width, Math.Max(0, bounds.Height - primaryHeight)));
        }
        int arrowWidth = Math.Min(RibbonLayoutEngine.Scale(20, dpi), Math.Max(1, bounds.Width / 3));
        return (bounds with { Width = bounds.Width - arrowWidth }, new(bounds.Right - arrowWidth, bounds.Top, arrowWidth, bounds.Height));
    }

    internal static IEnumerable<RibbonTarget> Expand(RibbonTarget target, int dpi, bool menu)
    {
        if (target.Element is RibbonQuickAccessDropDownReference { Source: RibbonSplitButtonItem } reference)
        {
            int span = Math.Min(RibbonLayoutEngine.Scale(menu ? 20 : 12, dpi), Math.Max(1, target.Bounds.Width / 3));
            var primary = target.Bounds with { Width = Math.Max(0, target.Bounds.Width - span) };
            var arrow = new Rectangle(primary.Right, primary.Top, span, primary.Height);
            string automatic = target.AutomaticKeyTip ?? "";
            return [target with { Bounds = primary, AutomaticKeyTip = automatic + "A" },
                target with { Bounds = arrow, DropDownPart = true, AutomaticKeyTip = automatic + "B" }];
        }
        if (target.Element is not RibbonSplitButtonItem split)
            return [target];
        var bounds = Calculate(target.Bounds, split, target.Size, menu, dpi);
        return [target with { Bounds = bounds.Primary }, target with { Bounds = bounds.DropDown, DropDownPart = true }];
    }
}
