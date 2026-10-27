using System.ComponentModel;
using System.Globalization;

namespace RibbonKit.WinForms;

// Give the property grid an explicit Auto choice while retaining nullable integer
// serialization and editable logical-pixel values.
internal sealed class RibbonNavigationWidthConverter : NullableConverter
{
    public RibbonNavigationWidthConverter() : base(typeof(int?)) { }
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text && (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("Auto", StringComparison.OrdinalIgnoreCase))
            ? null : base.ConvertFrom(context, culture, value);
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        value is null && destinationType == typeof(string) ? "Auto" : base.ConvertTo(context, culture, value, destinationType);
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(new object?[] { null });
}
