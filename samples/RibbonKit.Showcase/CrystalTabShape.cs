using System.Windows;
using System.Windows.Data;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Rounds detached Crystal tab headers without replacing their shared template.</summary>
internal static class CrystalTabShape
{
    private const string RadiusKey = "RibbonKit.Metrics.TabCornerRadius";
    private const string BorderKey = "RibbonKit.Metrics.TabSelectedBorderThickness";
    private static readonly DependencyProperty MinimizedProperty = DependencyProperty.RegisterAttached(
        "Minimized", typeof(bool), typeof(CrystalTabShape),
        new PropertyMetadata(false, (target, _) => Update((Ribbon)target)));

    public static void Apply(Ribbon ribbon, bool enabled)
    {
        if (enabled)
            BindingOperations.SetBinding(ribbon, MinimizedProperty,
                new Binding(nameof(Ribbon.IsMinimized)) { Source = ribbon, Mode = BindingMode.OneWay });
        else
            BindingOperations.ClearBinding(ribbon, MinimizedProperty);
        Update(ribbon);
    }

    private static void Update(Ribbon ribbon)
    {
        if ((bool)ribbon.GetValue(MinimizedProperty))
        {
            ribbon.Resources[RadiusKey] = new CornerRadius(8);
            ribbon.Resources[BorderKey] = new Thickness(1);
        }
        else
        {
            ribbon.Resources.Remove(RadiusKey);
            ribbon.Resources.Remove(BorderKey);
        }
    }
}
