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
        var content = new RibbonApplicationMenuContentMetrics(theme, dpi, density);
        int height = Math.Max(S(Math.Max(theme.ApplicationMenuFooterMinimumRowHeight ?? (density == RibbonDensity.Touch ? 44 : 30), density == RibbonDensity.Touch ? 44 : 1)), font.Height + content.RowPadding);
        if (items.Any(i => HasIcon(i) || i is RibbonCheckBoxItem or RibbonRadioButtonItem)) height = Math.Max(height, content.ArtworkHeight(false));
        if (items.Any(i => i is IRibbonEditorItem) && RibbonEditorMetrics.HasHeightOverride(theme))
            height = Math.Max(height, RibbonEditorLayout.Height(font, dpi, density, theme: theme) + S(2));
        return height;
    }
    internal static bool HasIcon(RibbonItem item) => item.Icon is not null || item.Image is not null || item.LargeImage is not null;
    internal static bool Illustrated(RibbonItem item) => item is not (RibbonDropDownItem or IRibbonEditorItem or RibbonMenuSeparatorItem)
        && item.Parent is RibbonDropDownItem { MenuPresentation: RibbonMenuPresentation.IllustratedChoices };
    internal static int ArrowWidth(int dpi, RibbonDensity density, RibbonTheme? theme = null) => new RibbonApplicationMenuContentMetrics(theme, dpi, density).Arrow;
    internal static int RowHeight(RibbonItem item, int width, Font font, int dpi, RibbonDensity density, bool navigation, RibbonTheme? theme = null)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        theme ??= item.Ribbon?.Theme;
        var content = new RibbonApplicationMenuContentMetrics(theme, dpi, density);
        if (item is RibbonMenuSeparatorItem) return content.SeparatorHeight;
        int minimum = S(navigation ? NavigationMinimum(density, theme) : PaneMinimum(density, theme));
        if (item is IRibbonEditorItem)
        {
            int height = Math.Max(S(density == RibbonDensity.Touch ? 44 : 32), RibbonMenuLayout.RowHeight(item, width, font, dpi, density, quickAccess: true));
            if ((navigation ? theme?.ApplicationMenuNavigationMinimumRowHeight : theme?.ApplicationMenuPaneMinimumRowHeight) is not null) height = Math.Max(height, minimum);
            return RibbonEditorMetrics.HasHeightOverride(theme ?? item.Ribbon?.Theme)
                ? Math.Max(height, RibbonEditorLayout.Height(font, dpi, density, theme: theme ?? item.Ribbon?.Theme) + S(2)) : height;
        }
        bool rich = !navigation && Illustrated(item);
        bool hasIcon = HasIcon(item) || content.CustomIconGeometry && (item is RibbonCheckBoxItem or RibbonRadioButtonItem);
        int textInset = content.TextLeading(navigation || rich, hasIcon, measure: true);
        int arrow = item is RibbonDropDownItem ? content.Arrow : 0;
        int description = content.DescriptionHeight(item.MenuDescription, font, width - textInset - content.Trailing(measure: true) - arrow);
        int artwork = navigation || rich ? content.ArtworkHeight(true) : HasIcon(item) || item is RibbonCheckBoxItem or RibbonRadioButtonItem ? content.ArtworkHeight(false) : 0;
        return Math.Max(minimum, Math.Max(artwork, font.Height + (description == 0 ? 0 : content.DescriptionGap + description) + content.TextPadding(description > 0)));
    }
    internal static IEnumerable<RibbonTarget> Expand(RibbonTarget target, int dpi, RibbonDensity density, RibbonTheme? theme = null)
    {
        if (target.Element is not RibbonSplitButtonItem) return [target];
        int span = Math.Min(ArrowWidth(dpi, density, theme), Math.Max(1, target.Bounds.Width / 2));
        var primary = target.Bounds with { Width = target.Bounds.Width - span };
        return [target with { Bounds = primary }, target with { Bounds = new(primary.Right, primary.Top, span, primary.Height), DropDownPart = true }];
    }
    internal static void Validate(RibbonTheme theme)
    {
        RibbonApplicationMenuAppearanceSettings.Validate(theme.ApplicationMenuAppearance);
        RibbonApplicationMenuContentSettings.Validate(theme.ApplicationMenuContent);
        if (theme.ApplicationMenuNavigationMinimumRowHeight is < 1 or > 1024 || theme.ApplicationMenuPaneMinimumRowHeight is < 1 or > 1024 ||
            theme.ApplicationMenuFooterMinimumRowHeight is < 1 or > 1024 || theme.ApplicationMenuPaneHorizontalPadding is < 0 or > 1024 ||
            theme.ApplicationMenuFooterItemGap is < 0 or > 1024)
            throw new ArgumentOutOfRangeException(nameof(theme), "Application Menu row minima must be 1..1024 and padding/gap values 0..1024, or null for existing geometry.");
    }
}
