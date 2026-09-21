using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Owns the preview's detached overlay resource scope and explicit document binding.</summary>
internal sealed class CrystalBackstagePresentation
{
    private readonly Backstage _stage;
    private readonly ResourceDictionary _scope = new();
    private ResourceDictionary? _palette;

    public CrystalBackstagePresentation(Backstage stage, ResourceDictionary baseline,
        TextBlock title, RibbonTextBox titleInput)
    {
        _stage = stage;
        _scope.MergedDictionaries.Add(baseline);
        _scope.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.Backstage.xaml", UriKind.Relative) });
        stage.Resources.MergedDictionaries.Add(_scope);
        title.SetBinding(TextBlock.TextProperty, new Binding(nameof(titleInput.Text)) { Source = titleInput });
    }

    public void Apply(ResourceDictionary? palette)
    {
        if (_palette != null) _scope.MergedDictionaries.Remove(_palette);
        _palette = palette;
        if (palette == null)
            _stage.ClearValue(FrameworkElement.StyleProperty);
        else
        {
            _scope.MergedDictionaries.Add(palette);
            _stage.SetResourceReference(FrameworkElement.StyleProperty, "Crystal.Backstage.Style");
        }
    }
}
