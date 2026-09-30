using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Template-only material selector. It carries no input, layout or host behavior.
internal static class UtilityChrome
{
    public static readonly DependencyProperty UseScrollMaterialProperty = DependencyProperty.RegisterAttached(
        "UseScrollMaterial", typeof(bool), typeof(UtilityChrome), new FrameworkPropertyMetadata(false));

    public static bool GetUseScrollMaterial(DependencyObject target) => (bool)target.GetValue(UseScrollMaterialProperty);
    public static void SetUseScrollMaterial(DependencyObject target, bool value) => target.SetValue(UseScrollMaterialProperty, value);

    public static readonly DependencyProperty SurfaceBrushProperty = DependencyProperty.RegisterAttached(
        "SurfaceBrush", typeof(Brush), typeof(UtilityChrome), new FrameworkPropertyMetadata(null));
    public static Brush? GetSurfaceBrush(DependencyObject target) => (Brush?)target.GetValue(SurfaceBrushProperty);
    public static void SetSurfaceBrush(DependencyObject target, Brush? value) => target.SetValue(SurfaceBrushProperty, value);

    public static readonly DependencyProperty WashBrushProperty = DependencyProperty.RegisterAttached(
        "WashBrush", typeof(Brush), typeof(UtilityChrome), new FrameworkPropertyMetadata(null));
    public static Brush? GetWashBrush(DependencyObject target) => (Brush?)target.GetValue(WashBrushProperty);
    public static void SetWashBrush(DependencyObject target, Brush? value) => target.SetValue(WashBrushProperty, value);

    public static readonly DependencyProperty WashOpacityProperty = DependencyProperty.RegisterAttached(
        "WashOpacity", typeof(double), typeof(UtilityChrome), new FrameworkPropertyMetadata(0d));
    public static double GetWashOpacity(DependencyObject target) => (double)target.GetValue(WashOpacityProperty);
    public static void SetWashOpacity(DependencyObject target, double value) => target.SetValue(WashOpacityProperty, value);
}
