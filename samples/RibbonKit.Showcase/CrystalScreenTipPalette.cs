using System.Windows;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Keeps the preview's detached tooltips on the same local palette.</summary>
internal sealed class CrystalScreenTipPalette
{
    private readonly ResourceDictionary _scope = new();
    private ResourceDictionary? _palette;

    public CrystalScreenTipPalette(ResourceDictionary baseline) => _scope.MergedDictionaries.Add(baseline);

    public void Attach(RibbonScreenTip tip)
    {
        if (!tip.Resources.MergedDictionaries.Contains(_scope)) tip.Resources.MergedDictionaries.Add(_scope);
    }

    public void Apply(ResourceDictionary? palette)
    {
        if (_palette != null) _scope.MergedDictionaries.Remove(_palette);
        _palette = palette;
        if (palette != null)
        {
            _scope.MergedDictionaries.Add(palette);
            // ToolTip.Resources precedes Style.Resources, so explicitly publish
            // the tooltip rim above the merged palette's general popup border.
            _scope["RibbonKit.Brushes.ScreenTip.Border"] = palette["Crystal.Brushes.GalleryBorder"];
        }
        else _scope.Remove("RibbonKit.Brushes.ScreenTip.Border");
    }
}
