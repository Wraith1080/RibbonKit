using System;
using System.Windows;
using System.Windows.Media;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Scopes the Showcase's optional glass paint to the active window and theme.</summary>
internal sealed class AcrylicGlassPresentation
{
    private readonly RibbonWindow _window;
    private ResourceDictionary? _overlay;

    public AcrylicGlassPresentation(RibbonWindow window) => _window = window;

    public void Apply(bool enabled, bool darkMode)
    {
        if (_overlay != null)
        {
            _window.Resources.MergedDictionaries.Remove(_overlay);
            _overlay = null;
        }

        if (!enabled) return;

        var overlay = new ResourceDictionary();
        ScaleOpacity(overlay, "RibbonKit.Brushes.Ribbon.BodyBackground", 0.44);
        ScaleOpacity(overlay, "RibbonKit.Brushes.Tab.SelectedBackground", 0.62);
        ScaleOpacity(overlay, "RibbonKit.Brushes.Tab.SelectedUnderline", 0.65);
        ScaleOpacity(overlay, "RibbonKit.Brushes.TitleBar.Background", 0.48);
        ScaleOpacity(overlay, "RibbonKit.Brushes.Window.Background", 0.48);

        // Keep the command wash light enough to read as glass, but carry the
        // current theme/accent hue through tint changes. Dark themes use less wash.
        Color accent = ((SolidColorBrush)_window.FindResource("RibbonKit.Brushes.Accent")).Color;
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
        ScaleOpacity(overlay, "RibbonKit.Brushes.Tab.HoverBorder", 1.0);

        _window.Resources.MergedDictionaries.Add(overlay);
        _overlay = overlay;
    }

    private static Color TintGlass(Color accent, byte alpha)
    {
        static byte Mix(byte neutral, byte tint) =>
            (byte)Math.Round(neutral * 0.7 + tint * 0.3);
        return Color.FromArgb(alpha, Mix(0xF3, accent.R), Mix(0xFA, accent.G),
            Mix(0xFF, accent.B));
    }

    private void ScaleOpacity(ResourceDictionary overlay, string key, double opacity)
    {
        var brush = ((Brush)_window.FindResource(key)).Clone();
        brush.Opacity = Math.Min(1, brush.Opacity * opacity);
        overlay[key] = brush;
    }
}
