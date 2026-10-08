using System.Globalization;
using System.Windows.Data;

namespace RibbonKit.Controls;

// Keep the collapsed strip's wrapping while allowing a separately sized popup.
internal sealed class GalleryPopupWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length > 3 && values[3] is true ? double.PositiveInfinity
        : values[2] is true && values[1] is double popupWidth && double.IsFinite(popupWidth)
            ? popupWidth
            : values[0] is double stripWidth ? stripWidth : double.PositiveInfinity;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
