using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

internal enum CrystalBackstageLayout { Sidebar, Floating }

/// <summary>Owns the preview's detached overlay resource scope and explicit document binding.</summary>
internal sealed class CrystalBackstagePresentation
{
    private readonly Backstage _stage;
    private readonly ResourceDictionary _scope = new();
    private ResourceDictionary? _palette;
    public CrystalBackstageLayout Layout { get; private set; } = CrystalBackstageLayout.Sidebar;

    public void SetLayout(CrystalBackstageLayout layout)
    {
        Layout = layout;
        UpdateStyle();
    }

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
        if (palette != null) _scope.MergedDictionaries.Add(palette);
        UpdateStyle();
    }

    private void UpdateStyle()
    {
        if (_palette == null)
            _stage.ClearValue(FrameworkElement.StyleProperty);
        else
            _stage.SetResourceReference(FrameworkElement.StyleProperty, Layout == CrystalBackstageLayout.Sidebar
                ? "Crystal.Backstage.Sidebar" : "Crystal.Backstage.Style");
    }
}
