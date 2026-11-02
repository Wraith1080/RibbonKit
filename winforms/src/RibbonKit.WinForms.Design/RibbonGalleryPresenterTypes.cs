using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;

namespace RibbonKit.WinForms.Design;

// Inspect metadata only: discovery, detached drafts and previews must never run
// application UserControl constructors.
internal static class RibbonGalleryPresenterTypes
{
    internal const string None = "(None)";
    internal static string Label(Type? type) => RibbonBackstageContentTypes.Label(type);
    internal static Type[] Available(IDesignerHost host, RibbonControl? ribbon = null)
    {
        var discovered = (host.GetService(typeof(ITypeDiscoveryService)) as ITypeDiscoveryService)?.GetTypes(typeof(UserControl), true).OfType<Type>() ?? [];
        var retained = ribbon is null ? [] : RibbonDesignCommitter.EditableElements(ribbon).OfType<RibbonGalleryItem>().Select(gallery => gallery.DropDownPresenterType).OfType<Type>();
        return discovered.Concat(retained).Where(RibbonGalleryItem.IsPresenterType).Distinct().OrderBy(Label, StringComparer.Ordinal).ToArray();
    }
    internal static Type? Resolve(string label, IEnumerable<Type> types) => label == None ? null
        : types.SingleOrDefault(type => Label(type) == label) ?? throw new ArgumentException("The selected gallery UserControl is unavailable. Build the project, then reopen Edit Ribbon.");
}

internal sealed class RibbonGalleryPresenterTypeConverter : TypeConverter
{
    public RibbonGalleryPresenterTypeConverter() { }
    private static Type[] Types(ITypeDescriptorContext? context) => context?.GetService(typeof(IDesignerHost)) is IDesignerHost host
        ? RibbonGalleryPresenterTypes.Available(host, (context.Instance as RibbonGalleryItem)?.Ribbon) : [];
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(new Type?[] { null }.Concat(Types(context)).ToArray());
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value is string text
        ? RibbonGalleryPresenterTypes.Resolve(text, Types(context)) : base.ConvertFrom(context, culture, value);
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) => destinationType == typeof(string)
        ? RibbonGalleryPresenterTypes.Label(value as Type) : base.ConvertTo(context, culture, value, destinationType);
}
