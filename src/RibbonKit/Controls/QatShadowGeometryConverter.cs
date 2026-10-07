using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RibbonKit.Controls;

// Clip the connected drawer's upward halo after its child effect has rendered.
internal sealed class QatShadowGeometryConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not double width || values[1] is not double height || width <= 0 || height <= 0
            || values[2] is not DropShadowEffect shadow || values[3] is not true
            || values[4] is not false || values[5] is not Thickness { Top: 0 }) return null;

        double halo = shadow.BlurRadius + Math.Abs(shadow.ShadowDepth) + 1;
        var clip = new RectangleGeometry(new Rect(-halo, 0, width + 2 * halo, height + halo));
        clip.Freeze();
        return clip;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
