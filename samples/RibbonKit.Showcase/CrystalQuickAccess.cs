using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

internal static class CrystalQuickAccess
{
    public static void Apply(Ribbon ribbon, bool enabled)
    {
        ribbon.ApplyTemplate();
        if (ribbon.Template?.FindName("QatBelowHost", ribbon) is not Border panel ||
            ribbon.Template.FindName("TabControlHost", ribbon) is not RibbonTabControl tabs) return;
        const string bodyCorner = "RibbonKit.Metrics.ContentCornerRadiusTop";
        if (!enabled)
        {
            tabs.Resources.Remove(bodyCorner);
            foreach (var property in new[] { FrameworkElement.HorizontalAlignmentProperty,
                FrameworkElement.MarginProperty, Border.PaddingProperty, Border.CornerRadiusProperty,
                Border.BorderThicknessProperty, Border.BackgroundProperty, Border.BorderBrushProperty })
                panel.ClearValue(property);
            return;
        }
        // Keep both surfaces complete rather than joining the body's foot to a bar.
        tabs.Resources[bodyCorner] = ribbon.FindResource("RibbonKit.Metrics.ContentCornerRadius");
        var bodyMargin = (Thickness)ribbon.FindResource("RibbonKit.Metrics.ContentMarginQatBelow");
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.Margin = new Thickness(bodyMargin.Left, 3, bodyMargin.Right, 4);
        panel.Padding = new Thickness(8, 2, 8, 2);
        panel.CornerRadius = new CornerRadius(10);
        panel.BorderThickness = new Thickness(1);
        panel.SetResourceReference(Border.BackgroundProperty, "Crystal.Brushes.QuickAccessSurface");
        panel.SetResourceReference(Border.BorderBrushProperty, "RibbonKit.Brushes.Control.HoverBorder");
    }
}
