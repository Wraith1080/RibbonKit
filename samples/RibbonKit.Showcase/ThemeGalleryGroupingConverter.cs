using System.Globalization;
using System.Windows.Data;

namespace RibbonKit.Showcase;

// Theme families are sample content; grouping and heading presentation belong to RibbonKit.
internal sealed class ThemeGalleryGroupingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        "CrystalLight" => "Modern",
        "Office2024" or "Office2019" or "Office2013" => "Office Modern",
        "Office2010" or "Office2007" => "Office Legacy",
        _ => value,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
