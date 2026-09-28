using System.Windows;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Scopes the preview's scrollbar tint to its owned Options dialog.</summary>
internal static class CrystalCustomization
{
    public static void Apply(RibbonOptionsDialog dialog, ResourceDictionary palette)
    {
        Color accent = ((SolidColorBrush)dialog.FindResource("RibbonKit.Brushes.Accent")).Color;
        CrystalScrollBars.Configure(dialog.Resources, palette, accent);
    }
}
