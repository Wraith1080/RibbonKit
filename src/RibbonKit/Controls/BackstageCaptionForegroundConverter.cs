using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Blend caption glyphs with the same live fade as the Backstage paint. Reuse endpoint
// resources, including scoped palettes, rather than introducing another animation clock.
internal sealed class BackstageCaptionForegroundConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not Brush normal) return DependencyProperty.UnsetValue;
        if (values[1] is not Brush page || values[2] is not double progress || progress <= 0d) return normal;
        if (progress >= 1d || ReferenceEquals(normal, page)) return page;

        // Drawing brushes also preserve app-authored gradient foregrounds.
        var geometry = new RectangleGeometry(new Rect(0, 0, 1, 1));
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(normal, null, geometry));
        var overlay = new DrawingGroup { Opacity = progress };
        overlay.Children.Add(new GeometryDrawing(page, null, geometry));
        drawing.Children.Add(overlay);
        var brush = new DrawingBrush(drawing) { Stretch = Stretch.Fill };
        // Freezing this graph would also freeze any mutable app-owned source brushes.
        if (normal.IsFrozen && page.IsFrozen && brush.CanFreeze) brush.Freeze();
        return brush;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
