using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RibbonKit.Controls;

// Private template plumbing; compact and non-ribbon windows retain their palette margin.
internal sealed class WindowFrameGeometryConverter : IMultiValueConverter
{
    internal static readonly DependencyProperty HeaderBottomProperty = DependencyProperty.RegisterAttached(
        "HeaderBottom", typeof(double), typeof(WindowFrameGeometryConverter), new PropertyMetadata(double.NaN));

    public static double GetHeaderBottom(DependencyObject target) => (double)target.GetValue(HeaderBottomProperty);
    public static void SetHeaderBottom(DependencyObject target, double value) => target.SetValue(HeaderBottomProperty, value);

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not Thickness margin) return DependencyProperty.UnsetValue;
        return values[1] is double bottom && double.IsFinite(bottom)
            ? new Thickness(margin.Left, bottom, margin.Right, margin.Bottom)
            : margin;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
