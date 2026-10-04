using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Template-only inputs. Derived paint never writes host resources or public tab properties.
internal static class ContextualMaterial
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(ContextualMaterial), new FrameworkPropertyMetadata(false));
    public static bool GetEnabled(DependencyObject target) => (bool)target.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject target, bool value) => target.SetValue(EnabledProperty, value);

    public static readonly DependencyProperty SelectedBackgroundProperty = DependencyProperty.RegisterAttached(
        "SelectedBackground", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null));
    public static Brush? GetSelectedBackground(DependencyObject target) => (Brush?)target.GetValue(SelectedBackgroundProperty);
    public static void SetSelectedBackground(DependencyObject target, Brush? value) => target.SetValue(SelectedBackgroundProperty, value);

    public static readonly DependencyProperty HoverBackgroundProperty = DependencyProperty.RegisterAttached(
        "HoverBackground", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null));
    public static Brush? GetHoverBackground(DependencyObject target) => (Brush?)target.GetValue(HoverBackgroundProperty);
    public static void SetHoverBackground(DependencyObject target, Brush? value) => target.SetValue(HoverBackgroundProperty, value);

    public static readonly DependencyProperty SelectedBorderProperty = DependencyProperty.RegisterAttached(
        "SelectedBorder", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null));
    public static Brush? GetSelectedBorder(DependencyObject target) => (Brush?)target.GetValue(SelectedBorderProperty);
    public static void SetSelectedBorder(DependencyObject target, Brush? value) => target.SetValue(SelectedBorderProperty, value);

    public static readonly DependencyProperty HoverBorderProperty = DependencyProperty.RegisterAttached(
        "HoverBorder", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null));
    public static Brush? GetHoverBorder(DependencyObject target) => (Brush?)target.GetValue(HoverBorderProperty);
    public static void SetHoverBorder(DependencyObject target, Brush? value) => target.SetValue(HoverBorderProperty, value);

    public static readonly DependencyProperty MarkerBrushProperty = DependencyProperty.RegisterAttached(
        "MarkerBrush", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null));
    public static Brush? GetMarkerBrush(DependencyObject target) => (Brush?)target.GetValue(MarkerBrushProperty);
    public static void SetMarkerBrush(DependencyObject target, Brush? value) => target.SetValue(MarkerBrushProperty, value);

    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.RegisterAttached(
        "TextBrush", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null));
    public static Brush? GetTextBrush(DependencyObject target) => (Brush?)target.GetValue(TextBrushProperty);
    public static void SetTextBrush(DependencyObject target, Brush? value) => target.SetValue(TextBrushProperty, value);

    public static readonly DependencyProperty PrimaryTextBrushProperty = DependencyProperty.RegisterAttached(
        "PrimaryTextBrush", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null));
    public static Brush? GetPrimaryTextBrush(DependencyObject target) => (Brush?)target.GetValue(PrimaryTextBrushProperty);
    public static void SetPrimaryTextBrush(DependencyObject target, Brush? value) => target.SetValue(PrimaryTextBrushProperty, value);

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.RegisterAttached(
        "AccentBrush", typeof(Brush), typeof(ContextualMaterial), new FrameworkPropertyMetadata(null, OnAccentChanged));
    public static Brush? GetAccentBrush(DependencyObject target) => (Brush?)target.GetValue(AccentBrushProperty);
    public static void SetAccentBrush(DependencyObject target, Brush? value) => target.SetValue(AccentBrushProperty, value);

    private static void OnAccentChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is RibbonTab tab) tab.UpdateContextualBrush();
    }
}

internal sealed class ContextualMaterialConverter : IMultiValueConverter
{
    public static readonly ContextualMaterialConverter Instance = new();

    internal static MultiBinding MarkerBinding(RibbonTab tab)
    {
        var binding = new MultiBinding { Converter = Instance, ConverterParameter = "Marker" };
        foreach (string path in new[] { "(controls:ContextualMaterial.MarkerBrush)", nameof(RibbonTab.ContextualBrush),
            "ContextualBrush.Color", "(controls:ContextualMaterial.Enabled)",
            "(controls:ContextualMaterial.PrimaryTextBrush)", nameof(RibbonTab.IsContextual),
            nameof(RibbonTab.ContextualSelectionBrush), nameof(Control.Foreground), "." })
        {
            // Programmatic attached-property paths use PropertyPath parameters, not a XAML namespace.
            PropertyPath propertyPath = path.StartsWith("(controls:", StringComparison.Ordinal)
                ? new PropertyPath("(0)", path.Contains("MarkerBrush", StringComparison.Ordinal) ? ContextualMaterial.MarkerBrushProperty
                    : path.Contains("Enabled", StringComparison.Ordinal) ? ContextualMaterial.EnabledProperty
                    : ContextualMaterial.PrimaryTextBrushProperty)
                : path == "ContextualBrush.Color"
                    ? new PropertyPath("ContextualBrush.(0)", SolidColorBrush.ColorProperty)
                    : new PropertyPath(path);
            binding.Bindings.Add(new Binding { Source = tab, Path = propertyPath });
        }
        return binding;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 9) return DependencyProperty.UnsetValue;
        string role = parameter as string ?? "";
        Brush? original = values[0] as Brush;
        Brush? tint = values[1] as Brush;
        bool contextual = values[5] is true;
        if (role == "Marker" && contextual && values[6] is Brush explicitMarker) return explicitMarker;
        if (role == "Text" && values[8] is RibbonTab owner
            && owner.ReadLocalValue(Control.ForegroundProperty) != DependencyProperty.UnsetValue
            && values[7] is Brush explicitText) return explicitText;

        if (!contextual || values[3] is not true || tint is not SolidColorBrush solid)
            return (object?)((role is "Text" or "Marker") ? tint ?? original : original) ?? DependencyProperty.UnsetValue;
        if (original is null || !ContextualMaterial.GetEnabled(original))
            return (object?)original ?? DependencyProperty.UnsetValue;

        Color color = solid.Color;
        bool dark = values[4] is SolidColorBrush text && text.Color.R + text.Color.G + text.Color.B > 450;
        Brush result;
        switch (role)
        {
            case "Surface":
                var surface = new RadialGradientBrush
                {
                    Center = new Point(0.5, 0.25), GradientOrigin = new Point(0.5, 0.25),
                    RadiusX = 0.85, RadiusY = 0.95,
                };
                surface.GradientStops.Add(new GradientStop(Mix(color, dark ? Colors.Black : Colors.White, dark ? 0.48 : 0.95), 0));
                surface.GradientStops.Add(new GradientStop(Mix(color, dark ? Colors.Black : Colors.White, dark ? 0.62 : 0.86), 0.55));
                surface.GradientStops.Add(new GradientStop(Mix(color, dark ? Colors.Black : Colors.White, 0.72), 1));
                result = surface;
                break;
            case "Hover":
                result = new SolidColorBrush(Color.FromArgb(28, color.R, color.G, color.B));
                break;
            case "Border":
            case "HoverBorder":
                if (original is not DrawingBrush rim || rim.Drawing is not DrawingGroup rimDrawing
                    || rimDrawing.Children.Count == 0 || rimDrawing.Children[0] is not GeometryDrawing) return original;
                var border = rim.CloneCurrentValue();
                ((GeometryDrawing)((DrawingGroup)border.Drawing).Children[0]).Brush =
                    new SolidColorBrush(Mix(color, Colors.White, dark ? 0.3 : 0.5));
                result = border;
                break;
            case "Marker":
                if (original is not DrawingBrush bubble || bubble.Drawing is not DrawingGroup drawing
                    || drawing.Children.Count == 0 || drawing.Children[0] is not GeometryDrawing geometry
                    || geometry.Brush is not LinearGradientBrush gradient || gradient.GradientStops.Count != 5) return original;
                var marker = bubble.CloneCurrentValue();
                var ramp = (LinearGradientBrush)((GeometryDrawing)((DrawingGroup)marker.Drawing).Children[0]).Brush;
                double[] light = dark ? new[] { 0.22, 0.48, 0.78, 0.60, 0.32 } : new[] { 0.35, 0.66, 0.94, 0.80, 0.45 };
                for (int i = 0; i < light.Length; i++) ramp.GradientStops[i].Color = Mix(color, Colors.White, light[i]);
                result = marker;
                break;
            case "Text":
                result = new SolidColorBrush(Mix(color, dark ? Colors.White : Colors.Black, dark ? 0.72 : 0.45));
                break;
            default:
                return original;
        }
        if (result.CanFreeze) result.Freeze();
        return result;
    }

    private static Color Mix(Color color, Color target, double amount) => Color.FromRgb(
        (byte)(color.R + (target.R - color.R) * amount),
        (byte)(color.G + (target.G - color.G) * amount),
        (byte)(color.B + (target.B - color.B) * amount));

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
