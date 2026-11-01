using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;

namespace RibbonKit.WinForms.Design;

// Discovery inspects metadata only. Application constructors must not run merely
// because a designer draft, preview, or property list mentions the class.
internal static class RibbonBackstageContentTypes
{
    internal const string None = "(None)";
    internal static string Label(Type? type) => type is null ? None : type.FullName + " [" + type.Assembly.GetName().Name + "]";
    internal static Type[] Available(IDesignerHost host, RibbonControl? ribbon = null)
    {
        var discovered = (host.GetService(typeof(ITypeDiscoveryService)) as ITypeDiscoveryService)?.GetTypes(typeof(UserControl), true).OfType<Type>() ?? [];
        var retained = ribbon?.Backstage.NavigationItems.OfType<RibbonBackstagePageItem>().Select(page => page.ContentType).OfType<Type>() ?? [];
        return discovered.Concat(retained).Where(RibbonBackstagePageItem.IsContentType).Distinct().OrderBy(Label, StringComparer.Ordinal).ToArray();
    }
    internal static Type? Resolve(string label, IEnumerable<Type> types) => label == None ? null
        : types.SingleOrDefault(type => Label(type) == label) ?? throw new ArgumentException("The selected UserControl is unavailable. Build the project, then reopen Edit Backstage.");
}

internal sealed class RibbonBackstageContentTypeConverter : TypeConverter
{
    public RibbonBackstageContentTypeConverter() { }
    private static Type[] Types(ITypeDescriptorContext? context)
    {
        if (context?.GetService(typeof(IDesignerHost)) is not IDesignerHost host) return [];
        return RibbonBackstageContentTypes.Available(host, (context.Instance as RibbonBackstagePageItem)?.Ribbon);
    }
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(new Type?[] { null }.Concat(Types(context)).ToArray());
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value is string text
        ? RibbonBackstageContentTypes.Resolve(text, Types(context)) : base.ConvertFrom(context, culture, value);
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) => destinationType == typeof(string)
        ? RibbonBackstageContentTypes.Label(value as Type) : base.ConvertTo(context, culture, value, destinationType);
}
