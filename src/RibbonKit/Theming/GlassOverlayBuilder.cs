using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Theming;

internal static class GlassOverlayBuilder
{
    internal static ResourceDictionary Create(FrameworkElement scope, bool darkMode)
    {
        var overlay = new ResourceDictionary();
        // Let each palette choose the tab band's glass translucency without an
        // opaque ribbon-wide fill covering the command surface below it.
        double tabStripOpacity = (double)scope.FindResource("RibbonKit.Metrics.Ribbon.TabStripGlassOpacity");
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.Ribbon.TabStripBackground",
            "RibbonKit.Brushes.Ribbon.Background", tabStripOpacity);
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.Ribbon.Background", 0.0);
        double bodyOpacity = (double)scope.FindResource("RibbonKit.Metrics.Ribbon.BodyGlassOpacity");
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.Ribbon.BodyBackground", bodyOpacity);
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.Tab.SelectedBackground", 0.62);
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.Tab.SelectedUnderline", 0.65);
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.TitleBar.Background", 0.48);
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.Window.Background", 0.48);

        // Keep the command wash light enough to read as glass, but carry the
        // current theme/accent hue through tint changes. Dark themes use less wash.
        Color accent = ((SolidColorBrush)scope.FindResource("RibbonKit.Brushes.Accent")).Color;
        Color hover = TintGlass(accent, darkMode ? (byte)0x50 : (byte)0x98);
        // Keep the tab hover quieter than the selected glass pill while letting
        // its wash follow the accent instead of retaining a fixed blue cast.
        Color tabHover = TintGlass(accent, darkMode ? (byte)0x30 : (byte)0x50);
        Color splitActive = TintGlass(accent, darkMode ? (byte)0x68 : (byte)0xB8);
        overlay["RibbonKit.Brushes.Control.HoverBackground"] = new SolidColorBrush(hover);
        overlay["RibbonKit.Brushes.Control.CompanionBackground"] = new SolidColorBrush(hover);
        overlay["RibbonKit.Brushes.Control.SplitActiveHover"] = new SolidColorBrush(splitActive);
        overlay["RibbonKit.Brushes.Tab.HoverBackground"] = new SolidColorBrush(tabHover);
        overlay["RibbonKit.Brushes.QatExtender.Background"] = new SolidColorBrush(tabHover);
        overlay["RibbonKit.Brushes.ApplicationButton.HoverBackground"] =
            overlay["RibbonKit.Brushes.Tab.HoverBackground"];
        // Light palettes can give File its own colored block with white text.
        // Preserve that block's hover contrast instead of borrowing a pale tab wash.
        if (scope.FindResource("RibbonKit.Brushes.ApplicationButton.Foreground") is SolidColorBrush fileText
            && scope.FindResource("RibbonKit.Brushes.Text.Primary") is SolidColorBrush commandText
            && fileText.Color.R + fileText.Color.G + fileText.Color.B > 450
            && commandText.Color.R + commandText.Color.G + commandText.Color.B <= 450)
        {
            ScaleOpacity(scope, overlay, "RibbonKit.Brushes.ApplicationButton.HoverBackground", 1.0);
        }
        ScaleOpacity(scope, overlay, "RibbonKit.Brushes.Tab.HoverBorder", 1.0);

        return overlay;
    }

    private static Color TintGlass(Color accent, byte alpha)
    {
        static byte Mix(byte neutral, byte tint) =>
            (byte)Math.Round(neutral * 0.7 + tint * 0.3);
        return Color.FromArgb(alpha, Mix(0xF3, accent.R), Mix(0xFA, accent.G),
            Mix(0xFF, accent.B));
    }

    private static void ScaleOpacity(FrameworkElement scope, ResourceDictionary overlay, string key, double opacity)
        => ScaleOpacity(scope, overlay, key, key, opacity);

    private static void ScaleOpacity(FrameworkElement scope, ResourceDictionary overlay, string key, string sourceKey, double opacity)
    {
        var brush = ((Brush)scope.FindResource(sourceKey)).Clone();
        brush.Opacity = Math.Min(1, brush.Opacity * opacity);
        overlay[key] = brush;
    }
}
