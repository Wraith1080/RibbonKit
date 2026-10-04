using System.Windows;

namespace RibbonKit.Controls;

// Inputs for the shared action-button template; hosts retain ordinary Button properties.
internal static class ActionChrome
{
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
        "CornerRadius", typeof(CornerRadius), typeof(ActionChrome), new FrameworkPropertyMetadata(new CornerRadius(2)));
    public static CornerRadius GetCornerRadius(DependencyObject target) => (CornerRadius)target.GetValue(CornerRadiusProperty);
    public static void SetCornerRadius(DependencyObject target, CornerRadius value) => target.SetValue(CornerRadiusProperty, value);

    public static readonly DependencyProperty RecognizesAccessKeyProperty = DependencyProperty.RegisterAttached(
        "RecognizesAccessKey", typeof(bool), typeof(ActionChrome), new FrameworkPropertyMetadata(false));
    public static bool GetRecognizesAccessKey(DependencyObject target) => (bool)target.GetValue(RecognizesAccessKeyProperty);
    public static void SetRecognizesAccessKey(DependencyObject target, bool value) => target.SetValue(RecognizesAccessKeyProperty, value);

    public static readonly DependencyProperty UseGlassMaterialProperty = DependencyProperty.RegisterAttached(
        "UseGlassMaterial", typeof(bool), typeof(ActionChrome), new FrameworkPropertyMetadata(false));
    public static bool GetUseGlassMaterial(DependencyObject target) => (bool)target.GetValue(UseGlassMaterialProperty);
    public static void SetUseGlassMaterial(DependencyObject target, bool value) => target.SetValue(UseGlassMaterialProperty, value);

    public static readonly DependencyProperty DisabledOpacityProperty = DependencyProperty.RegisterAttached(
        "DisabledOpacity", typeof(double), typeof(ActionChrome), new FrameworkPropertyMetadata(0.45));
    public static double GetDisabledOpacity(DependencyObject target) => (double)target.GetValue(DisabledOpacityProperty);
    public static void SetDisabledOpacity(DependencyObject target, double value) => target.SetValue(DisabledOpacityProperty, value);
}
