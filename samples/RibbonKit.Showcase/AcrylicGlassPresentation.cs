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

        // Neutral specular washes contrast with the material while the theme supplies
        // text, pressed states and rims. Dark themes use less white to protect labels.
        Color hover = darkMode
            ? Color.FromArgb(0x50, 0xF3, 0xFA, 0xFF)
            : Color.FromArgb(0x98, 0xF3, 0xFA, 0xFF);
        // The tab hover stays quieter than the selected glass pill.
        Color tabHover = darkMode
            ? Color.FromArgb(0x30, 0xF3, 0xFA, 0xFF)
            : Color.FromArgb(0x50, 0xF3, 0xFA, 0xFF);
        Color splitActive = darkMode
            ? Color.FromArgb(0x68, 0xF3, 0xFA, 0xFF)
            : Color.FromArgb(0xB8, 0xF3, 0xFA, 0xFF);
        overlay["RibbonKit.Brushes.Control.HoverBackground"] = new SolidColorBrush(hover);
        overlay["RibbonKit.Brushes.Control.CompanionBackground"] = new SolidColorBrush(hover);
        overlay["RibbonKit.Brushes.Control.SplitActiveHover"] = new SolidColorBrush(splitActive);
        overlay["RibbonKit.Brushes.Tab.HoverBackground"] = new SolidColorBrush(tabHover);
        ScaleOpacity(overlay, "RibbonKit.Brushes.Tab.HoverBorder", 1.0);

        _window.Resources.MergedDictionaries.Add(overlay);
        _overlay = overlay;
    }

    private void ScaleOpacity(ResourceDictionary overlay, string key, double opacity)
    {
        var brush = ((Brush)_window.FindResource(key)).Clone();
        brush.Opacity = Math.Min(1, brush.Opacity * opacity);
        overlay[key] = brush;
    }
}
