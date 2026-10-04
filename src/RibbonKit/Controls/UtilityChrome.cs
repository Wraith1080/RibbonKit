using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Internal material and surface selectors for the shared templates.
internal static class UtilityChrome
{
    public static readonly DependencyProperty QatTitleBarColoredProperty = DependencyProperty.RegisterAttached(
        "QatTitleBarColored", typeof(bool), typeof(UtilityChrome), new FrameworkPropertyMetadata(false, OnQatSurfaceChanged));
    public static bool GetQatTitleBarColored(DependencyObject target) => (bool)target.GetValue(QatTitleBarColoredProperty);
    public static void SetQatTitleBarColored(DependencyObject target, bool value) => target.SetValue(QatTitleBarColoredProperty, value);

    public static readonly DependencyProperty QatTabRowColoredProperty = DependencyProperty.RegisterAttached(
        "QatTabRowColored", typeof(bool), typeof(UtilityChrome), new FrameworkPropertyMetadata(false, OnQatSurfaceChanged));
    public static bool GetQatTabRowColored(DependencyObject target) => (bool)target.GetValue(QatTabRowColoredProperty);
    public static void SetQatTabRowColored(DependencyObject target, bool value) => target.SetValue(QatTabRowColoredProperty, value);

    private static void OnQatSurfaceChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is Ribbon ribbon) ribbon.UpdateQatButtonContext();
    }

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
