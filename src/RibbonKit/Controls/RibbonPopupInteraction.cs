using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RibbonKit.Controls;

// Paint/preview policy only: command hit testing and normal input routing stay live.
internal static class RibbonPopupInteraction
{
    internal static readonly DependencyProperty QuickAccessRibbonProperty = DependencyProperty.RegisterAttached(
        "QuickAccessRibbon", typeof(Ribbon), typeof(RibbonPopupInteraction), new PropertyMetadata(null));
    public static readonly DependencyProperty SuppressHoverProperty = DependencyProperty.RegisterAttached(
        "SuppressHover", typeof(bool), typeof(RibbonPopupInteraction),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
    public static bool GetSuppressHover(DependencyObject element) => (bool)element.GetValue(SuppressHoverProperty);
    public static void SetSuppressHover(DependencyObject element, bool value) => element.SetValue(SuppressHoverProperty, value);

    internal static Ribbon? FindQuickAccessRibbon(DependencyObject? element)
    {
        bool inToolbar = false;
        while (element is not null)
        {
            if (element.GetValue(QuickAccessRibbonProperty) is Ribbon explicitOwner) return explicitOwner;
            if (element is RibbonQuickAccessToolBar toolbar)
            {
                if (toolbar.Owner is { } owner) return owner;
                inToolbar = true;
            }
            if (inToolbar && element is Ribbon ribbon) return ribbon;
            element = Parent(element);
        }
        return null;
    }

    internal static DependencyObject? Parent(DependencyObject element) => element is Visual or Visual3D
        ? VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element)
        : LogicalTreeHelper.GetParent(element);
}
