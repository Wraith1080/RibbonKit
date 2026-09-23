using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

internal static class CrystalQuickAccess
{
    private static readonly string[] DrawerMetrics =
    {
        "QatExtenderMargin", "QatExtenderCornerRadius", "QatExtenderBorderThickness",
        "QatExtenderMarginMinimized", "QatExtenderCornerRadiusMinimized", "QatExtenderBorderThicknessMinimized",
        "QatExtenderMarginMessageBar", "QatExtenderCornerRadiusMessageBar",
    };
    public static void Apply(Ribbon ribbon, bool enabled)
    {
        ribbon.ApplyTemplate();
        if (ribbon.Template?.FindName("QatBelowHost", ribbon) is not Border panel ||
            ribbon.Template.FindName("TabControlHost", ribbon) is not RibbonTabControl tabs) return;
        const string bodyCorner = "RibbonKit.Metrics.ContentCornerRadiusTop";
        if (!enabled)
        {
            tabs.Resources.Remove(bodyCorner);
            tabs.ClearValue(Panel.ZIndexProperty);
            foreach (var key in DrawerMetrics) panel.Resources.Remove("RibbonKit.Metrics." + key);
            foreach (var property in new[] { FrameworkElement.HorizontalAlignmentProperty,
                FrameworkElement.MarginProperty, Border.PaddingProperty, Border.CornerRadiusProperty,
                Border.BorderThicknessProperty, Border.BackgroundProperty, Border.BorderBrushProperty, UIElement.EffectProperty })
                panel.ClearValue(property);
            return;
        }
        // The narrower drawer tucks under the rounded body. Native minimized-state
        // triggers select a complete rim when the body is no longer above it.
        tabs.Resources[bodyCorner] = ribbon.FindResource("RibbonKit.Metrics.ContentCornerRadius");
        Panel.SetZIndex(tabs, 1);
        var bodyMargin = (Thickness)ribbon.FindResource("RibbonKit.Metrics.ContentMarginQatBelow");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        var drawerMargin = new Thickness(bodyMargin.Left + 16, 0, bodyMargin.Right + 16, 4);
        panel.Resources["RibbonKit.Metrics.QatExtenderMargin"] = drawerMargin;
        panel.Resources["RibbonKit.Metrics.QatExtenderMarginMessageBar"] = drawerMargin;
        panel.Resources["RibbonKit.Metrics.QatExtenderMarginMinimized"] = new Thickness(drawerMargin.Left, 3, drawerMargin.Right, 4);
        panel.Resources["RibbonKit.Metrics.QatExtenderCornerRadius"] = new CornerRadius(0, 0, 10, 10);
        panel.Resources["RibbonKit.Metrics.QatExtenderCornerRadiusMessageBar"] = new CornerRadius(0, 0, 10, 10);
        panel.Resources["RibbonKit.Metrics.QatExtenderCornerRadiusMinimized"] = new CornerRadius(10);
        panel.Resources["RibbonKit.Metrics.QatExtenderBorderThickness"] = new Thickness(1, 0, 1, 1);
        panel.Resources["RibbonKit.Metrics.QatExtenderBorderThicknessMinimized"] = new Thickness(1);
        panel.ClearValue(FrameworkElement.MarginProperty);
        panel.ClearValue(Border.CornerRadiusProperty);
        panel.ClearValue(Border.BorderThicknessProperty);
        panel.Padding = new Thickness(8, 2, 8, 2);
        panel.SetResourceReference(UIElement.EffectProperty, "Crystal.Effects.QuickAccessShadow");
        // Reuse the inactive tab's hover surface, with the global Crystal tint.
        panel.SetResourceReference(Border.BackgroundProperty, "RibbonKit.Brushes.Tab.HoverBackground");
        panel.SetResourceReference(Border.BorderBrushProperty, "Crystal.Brushes.QuickAccessBorder");
    }
}
