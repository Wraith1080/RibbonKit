using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;

namespace RibbonKit.WinForms.Design;

// Metadata only. Arbitrary application bodies are never constructed in previews.
internal static class RibbonDropDownContentTypes
{
    internal static Type[] Available(IDesignerHost host, RibbonControl? ribbon = null)
    {
        var discovered = (host.GetService(typeof(ITypeDiscoveryService)) as ITypeDiscoveryService)?.GetTypes(typeof(UserControl), true).OfType<Type>() ?? [];
        var retained = ribbon is null ? [] : RibbonDesignCommitter.EditableElements(ribbon).OfType<RibbonDropDownItem>()
            .SelectMany(item => new[] { item.ContentType, (item as RibbonGalleryItem)?.DropDownPresenterType, (item as RibbonGalleryItem)?.InRibbonPresenterType }).OfType<Type>();
        return discovered.Concat(retained).Where(RibbonBackstagePageItem.IsContentType).Distinct().OrderBy(RibbonBackstageContentTypes.Label, StringComparer.Ordinal).ToArray();
    }
    internal static Type? Resolve(string label, IEnumerable<Type> types) => label == RibbonBackstageContentTypes.None ? null
        : types.SingleOrDefault(type => RibbonBackstageContentTypes.Label(type) == label) ?? throw new ArgumentException("The selected popup UserControl is unavailable. Build the project, then reopen Edit Ribbon.");
}

internal sealed class RibbonDropDownContentTypeConverter : TypeConverter
{
    public RibbonDropDownContentTypeConverter() { }
    private static Type[] Types(ITypeDescriptorContext? context) => context?.GetService(typeof(IDesignerHost)) is IDesignerHost host
        ? RibbonDropDownContentTypes.Available(host, (context.Instance as RibbonDropDownItem)?.Ribbon) : [];
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(new Type?[] { null }.Concat(Types(context)).ToArray());
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value is string text
        ? RibbonDropDownContentTypes.Resolve(text, Types(context)) : base.ConvertFrom(context, culture, value);
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) => destinationType == typeof(string)
        ? RibbonBackstageContentTypes.Label(value as Type) : base.ConvertTo(context, culture, value, destinationType);
}
