using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Uses the File button's existing overlay for the preview's tab hover rim.</summary>
internal static class CrystalFileHover
{
    public static void Apply(Ribbon ribbon, bool enabled)
    {
        ribbon.ApplyTemplate();
        if (ribbon.Template?.FindName("TabControlHost", ribbon) is not RibbonTabControl tabs) return;
        tabs.ApplyTemplate();
        if (tabs.Template?.FindName("PART_ApplicationButton", tabs) is not ToggleButton button) return;
        button.ApplyTemplate();
        if (button.Template?.FindName("InnerRim", button) is not Border rim) return;
        if (!enabled)
        {
            rim.ClearValue(FrameworkElement.StyleProperty);
            rim.SetResourceReference(Border.BorderBrushProperty, "RibbonKit.Brushes.ApplicationButton.InnerGlow");
            rim.SetResourceReference(Border.BorderThicknessProperty, "RibbonKit.Metrics.ApplicationButtonBorderThickness");
            rim.SetResourceReference(Border.CornerRadiusProperty, "RibbonKit.Metrics.ApplicationButtonCornerRadius");
            return;
        }
        // This overlay doesn't contribute an extra border around the content, so
        // matching the tab rim never moves the File label or changes hit geometry.
        rim.SetResourceReference(Border.BorderBrushProperty, "RibbonKit.Brushes.Tab.HoverBorder");
        rim.BorderThickness = new Thickness(1);
        rim.SetResourceReference(Border.CornerRadiusProperty, "RibbonKit.Metrics.ApplicationButtonCornerRadius");
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(UIElement.OpacityProperty, 0d));
        var hover = new MultiDataTrigger();
        hover.Conditions.Add(new Condition(new Binding(nameof(button.IsMouseOver)) { Source = button }, true));
        hover.Conditions.Add(new Condition(new Binding(nameof(button.IsPressed)) { Source = button }, false));
        hover.Conditions.Add(new Condition(new Binding(nameof(button.IsChecked)) { Source = button }, false));
        hover.Setters.Add(new Setter(UIElement.OpacityProperty, 1d));
        style.Triggers.Add(hover);
        rim.Style = style;
    }
}
