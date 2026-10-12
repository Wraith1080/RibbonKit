using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RibbonKit.Controls;

// Clip the page fill to its rim; leave authored layouts and the outer shadow unclipped.
internal sealed class BackstagePageGeometryConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 5 || values[0] is not double width || values[1] is not double height
            || values[2] is not CornerRadius corners || values[3] is not Thickness thickness
            || values[4] is not Visibility visibility || visibility != Visibility.Visible
            || thickness == new Thickness(0) || width <= 0 || height <= 0)
            return null;

        return ApplicationMenuGeometryConverter.RoundedRectangle(width, height, corners);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
