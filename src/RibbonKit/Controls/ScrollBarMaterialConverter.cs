using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Composes only the opt-in Crystal token brushes. An explicit host brush has no
// wash marker and passes through unchanged, as do all Office theme brushes.
internal sealed class ScrollBarMaterialConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 3 || values[0] is not Brush original) return DependencyProperty.UnsetValue;
        double opacity = UtilityChrome.GetWashOpacity(original);
        if (opacity <= 0 || values[1] is not Brush surface || values[2] is not Brush wash) return original;
        var face = new RectangleGeometry(new Rect(0, 0, 1, 1));
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(surface.CloneCurrentValue(), null, face));
        var overlay = new DrawingGroup { Opacity = opacity };
        overlay.Children.Add(new GeometryDrawing(wash.CloneCurrentValue(), null, face));
        drawing.Children.Add(overlay);
        var result = new DrawingBrush(drawing) { Viewbox = new Rect(0, 0, 1, 1),
            ViewboxUnits = BrushMappingMode.Absolute, Stretch = Stretch.Fill };
        if (result.CanFreeze) result.Freeze();
        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
