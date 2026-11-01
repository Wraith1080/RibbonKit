using System.ComponentModel;

namespace RibbonKit.WinForms;

// File is a RibbonDropDownItem with instance-specific initial values. Keep its
// metadata consistent with construction so Reset and CodeDOM serialization do
// not apply the generic dropdown's empty caption / visible-by-default values.
internal static class RibbonFileMenuDefaults
{
    internal static RibbonDropDownItem Create()
    {
        var file = new RibbonDropDownItem { Text = "File", KeyTip = "F", Visible = false };
        var provider = new DefaultsProvider(TypeDescriptor.GetProvider(file));
        TypeDescriptor.AddProvider(provider, file);
        file.Disposed += (_, _) => TypeDescriptor.RemoveProvider(provider, file);
        return file;
    }
    private sealed class DefaultsProvider(TypeDescriptionProvider parent) : TypeDescriptionProvider(parent)
    {
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance) => new DefaultsDescriptor(base.GetTypeDescriptor(objectType, instance)!);
    }
    private sealed class DefaultsDescriptor(ICustomTypeDescriptor parent) : CustomTypeDescriptor(parent)
    {
        public override PropertyDescriptorCollection GetProperties() => GetProperties(null);
        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            var properties = base.GetProperties().Cast<PropertyDescriptor>().Select(property => property.Name switch
            {
                nameof(RibbonElement.Text) => WithDefault(property, "File"),
                nameof(RibbonElement.KeyTip) => WithDefault(property, "F"),
                nameof(RibbonElement.Visible) => WithDefault(property, false),
                _ => property
            });
            // Contains resolves implicit default attributes (for example
            // DesignOnly(false)). Matches only inspects explicit attributes
            // and would hide runtime properties from nested serialization.
            if (attributes is { Length: > 0 }) properties = properties.Where(property => property.Attributes.Contains(attributes));
            return new(properties.ToArray(), true);
        }
        private static PropertyDescriptor WithDefault(PropertyDescriptor property, object value) =>
            TypeDescriptor.CreateProperty(property.ComponentType, property, new DefaultValueAttribute(value));
    }
}
