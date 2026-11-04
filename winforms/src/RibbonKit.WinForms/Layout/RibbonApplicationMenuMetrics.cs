namespace RibbonKit.WinForms.Layout;

internal enum RibbonApplicationMenuPart { None, Navigation, Pane, Footer }

// Component-scoped metrics: ordinary dropdown, gallery and ribbon geometry stays independent.
internal static class RibbonApplicationMenuMetrics
{
    internal static int NavigationMinimum(RibbonDensity density, RibbonTheme? theme) => Math.Max(theme?.ApplicationMenuNavigationMinimumRowHeight ?? (density == RibbonDensity.Touch ? 44 : 40), density == RibbonDensity.Touch ? 44 : 1);
    internal static int PaneMinimum(RibbonDensity density, RibbonTheme? theme) => Math.Max(theme?.ApplicationMenuPaneMinimumRowHeight ?? (density == RibbonDensity.Touch ? 44 : 28), density == RibbonDensity.Touch ? 44 : 1);
    internal static int PanePadding(RibbonTheme? theme) => theme?.ApplicationMenuPaneHorizontalPadding ?? 10;
    internal static int FooterGap(RibbonTheme? theme) => theme?.ApplicationMenuFooterItemGap ?? 6;
    internal static int FooterRowHeight(IEnumerable<RibbonItem> items, Font font, int dpi, RibbonDensity density, RibbonTheme theme)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int height = Math.Max(S(Math.Max(theme.ApplicationMenuFooterMinimumRowHeight ?? (density == RibbonDensity.Touch ? 44 : 30), density == RibbonDensity.Touch ? 44 : 1)), font.Height + S(10));
        if (items.Any(i => HasIcon(i) || i is RibbonCheckBoxItem or RibbonRadioButtonItem)) height = Math.Max(height, S(24));
        if (items.Any(i => i is IRibbonEditorItem) && RibbonEditorMetrics.HasHeightOverride(theme))
            height = Math.Max(height, RibbonEditorLayout.Height(font, dpi, density, theme: theme) + S(2));
        return height;
    }
    internal static bool HasIcon(RibbonItem item) => item.Icon is not null || item.Image is not null || item.LargeImage is not null;
    internal static bool Illustrated(RibbonItem item) => item is not (RibbonDropDownItem or IRibbonEditorItem or RibbonMenuSeparatorItem)
        && item.Parent is RibbonDropDownItem { MenuPresentation: RibbonMenuPresentation.IllustratedChoices };
    internal static int ArrowWidth(int dpi, RibbonDensity density) => RibbonLayoutEngine.Scale(density == RibbonDensity.Touch ? 44 : 26, dpi);
    internal static int RowHeight(RibbonItem item, int width, Font font, int dpi, RibbonDensity density, bool navigation, RibbonTheme? theme = null)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        if (item is RibbonMenuSeparatorItem) return S(9);
        theme ??= item.Ribbon?.Theme;
        int minimum = S(navigation ? NavigationMinimum(density, theme) : PaneMinimum(density, theme));
        if (item is IRibbonEditorItem)
        {
            int height = Math.Max(S(density == RibbonDensity.Touch ? 44 : 32), RibbonMenuLayout.RowHeight(item, width, font, dpi, density, quickAccess: true));
            if ((navigation ? theme?.ApplicationMenuNavigationMinimumRowHeight : theme?.ApplicationMenuPaneMinimumRowHeight) is not null) height = Math.Max(height, minimum);
            return RibbonEditorMetrics.HasHeightOverride(theme ?? item.Ribbon?.Theme)
                ? Math.Max(height, RibbonEditorLayout.Height(font, dpi, density, theme: theme ?? item.Ribbon?.Theme) + S(2)) : height;
        }
        bool rich = !navigation && Illustrated(item);
        int textInset = navigation || rich ? S(52) : HasIcon(item) ? S(30) : S(6);
        int arrow = item is RibbonDropDownItem ? ArrowWidth(dpi, density) : 0;
        int description = item.MenuDescription.Length == 0 ? 0 : Math.Min(2 * font.Height,
            TextRenderer.MeasureText(item.MenuDescription, font, new(Math.Max(1, width - textInset - S(8) - arrow), int.MaxValue), RibbonMenuLayout.DescriptionFlags).Height);
        int artwork = navigation || rich ? S(40) : HasIcon(item) || item is RibbonCheckBoxItem or RibbonRadioButtonItem ? S(24) : 0;
        return Math.Max(minimum, Math.Max(artwork, font.Height + (description == 0 ? 0 : S(2) + description) + S(10)));
    }
    internal static IEnumerable<RibbonTarget> Expand(RibbonTarget target, int dpi, RibbonDensity density)
    {
        if (target.Element is not RibbonSplitButtonItem) return [target];
        int span = Math.Min(ArrowWidth(dpi, density), Math.Max(1, target.Bounds.Width / 2));
        var primary = target.Bounds with { Width = target.Bounds.Width - span };
        return [target with { Bounds = primary }, target with { Bounds = new(primary.Right, primary.Top, span, primary.Height), DropDownPart = true }];
    }
    internal static void Validate(RibbonTheme theme)
    {
        if (theme.ApplicationMenuNavigationMinimumRowHeight is < 1 or > 1024 || theme.ApplicationMenuPaneMinimumRowHeight is < 1 or > 1024 ||
            theme.ApplicationMenuFooterMinimumRowHeight is < 1 or > 1024 || theme.ApplicationMenuPaneHorizontalPadding is < 0 or > 1024 ||
            theme.ApplicationMenuFooterItemGap is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Application Menu row minima must be 1..1024 and padding/gap values 0..1024, or null for existing geometry.");
    }
}
