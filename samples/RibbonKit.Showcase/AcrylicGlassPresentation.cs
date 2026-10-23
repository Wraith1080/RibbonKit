using System.Windows;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Scopes the Showcase's optional glass paint to the active window and theme.</summary>
internal sealed class AcrylicGlassPresentation
{
    private readonly RibbonWindow _window;
    private ResourceDictionary? _overlay;
    private readonly CapturedBackdrop? _flyouts;

    public AcrylicGlassPresentation(RibbonWindow window, Ribbon? ribbon = null)
    {
        _window = window;
        if (ribbon is not null)
        {
            _flyouts = new CapturedBackdrop(ribbon, window);
            window.Closed += (_, _) => _flyouts.Dispose();
        }
    }

    public void Apply(bool enabled, bool darkMode)
    {
        _flyouts?.Apply(false);
        if (_overlay != null)
        {
            _window.Resources.MergedDictionaries.Remove(_overlay);
            _overlay = null;
        }

        if (!enabled) return;

        _overlay = RibbonKit.Theming.ThemeManager.CreateGlassOverlay(_window, darkMode);
        _window.Resources.MergedDictionaries.Add(_overlay);
        _flyouts?.Apply(true);
    }
}
