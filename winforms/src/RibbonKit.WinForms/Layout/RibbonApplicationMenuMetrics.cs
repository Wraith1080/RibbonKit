namespace RibbonKit.WinForms.Layout;

internal enum RibbonApplicationMenuPart { None, Navigation, Pane, Footer }

// Component-scoped metrics: ordinary dropdown, gallery and ribbon geometry stays independent.
internal static class RibbonApplicationMenuMetrics
{
    internal static bool HasIcon(RibbonItem item) => item.Icon is not null || item.Image is not null || item.LargeImage is not null;
    internal static bool Illustrated(RibbonItem item) => item is not (RibbonDropDownItem or IRibbonEditorItem or RibbonMenuSeparatorItem)
        && item.Parent is RibbonDropDownItem { MenuPresentation: RibbonMenuPresentation.IllustratedChoices };
    internal static int ArrowWidth(int dpi, RibbonDensity density) => RibbonLayoutEngine.Scale(density == RibbonDensity.Touch ? 44 : 26, dpi);
    internal static int RowHeight(RibbonItem item, int width, Font font, int dpi, RibbonDensity density, bool navigation, RibbonTheme? theme = null)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        if (item is RibbonMenuSeparatorItem) return S(9);
        if (item is IRibbonEditorItem)
        {
            int height = Math.Max(S(density == RibbonDensity.Touch ? 44 : 32), RibbonMenuLayout.RowHeight(item, width, font, dpi, density, quickAccess: true));
            return RibbonEditorMetrics.HasHeightOverride(theme ?? item.Ribbon?.Theme)
                ? Math.Max(height, RibbonEditorLayout.Height(font, dpi, density, theme: theme ?? item.Ribbon?.Theme) + S(2)) : height;
        }
        int minimum = S(density == RibbonDensity.Touch ? 44 : navigation ? 40 : 28);
        bool rich = !navigation && Illustrated(item);
        int textInset = navigation || rich ? S(52) : HasIcon(item) ? S(30) : S(6);
        int arrow = item is RibbonDropDownItem ? ArrowWidth(dpi, density) : 0;
        int description = item.MenuDescription.Length == 0 ? 0 : Math.Min(2 * font.Height,
            TextRenderer.MeasureText(item.MenuDescription, font, new(Math.Max(1, width - textInset - S(8) - arrow), int.MaxValue), RibbonMenuLayout.DescriptionFlags).Height);
        return Math.Max(minimum, Math.Max(navigation || rich ? S(32 + 8) : 0, font.Height + (description == 0 ? 0 : S(2) + description) + S(10)));
    }
    internal static IEnumerable<RibbonTarget> Expand(RibbonTarget target, int dpi, RibbonDensity density)
    {
        if (target.Element is not RibbonSplitButtonItem) return [target];
        int span = Math.Min(ArrowWidth(dpi, density), Math.Max(1, target.Bounds.Width / 2));
        var primary = target.Bounds with { Width = target.Bounds.Width - span };
        return [target with { Bounds = primary }, target with { Bounds = new(primary.Right, primary.Top, span, primary.Height), DropDownPart = true }];
    }
}
