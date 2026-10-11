using System.Globalization;
using System.Windows.Data;

namespace RibbonKit.Controls;

// The retiring caption image fades out as Backstage fades in, and returns on exit.
internal sealed class BackstageCaptionIconOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double opacity && double.IsFinite(opacity) ? Math.Clamp(1d - opacity, 0d, 1d) : 0d;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
