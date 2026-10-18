using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

internal static partial class DesignModel
{
    // Always inside the Toolbox's creation transaction or the caller's edit scope.
    // Raw child construction avoids running a second SDK initializer mid-seed.
    internal static void InitializeNewItemDefaults(ModelItem item, bool ribbonStarterTab = false)
    {
        NewItemDefaults.Apply(TypeName(item), IsAssigned, Count, Set,
            (collection, type, property, value) =>
            {
                ModelItem child = CreateRawAny(item, type)
                    ?? throw new System.InvalidOperationException("Could not create starter " + type);
                var caption = FindProperty(child, property);
                if (caption is { IsSet: false, IsReadOnly: false }) caption.SetValue(value);
                InitializeNewItemDefaults(child, TypeName(item) == "Ribbon" && type == "RibbonTab");
                Add(item, collection, child);
            }, ribbonStarterTab);

        ModelProperty? Property(string name) => name.StartsWith("Ribbon.", System.StringComparison.Ordinal)
            ? FindAttached(item, "RibbonKit.Controls.Ribbon", name.Substring("Ribbon.".Length))
            : FindProperty(item, name);

        bool IsAssigned(string name)
        {
            var property = Property(name);
            DesignLog.Write("New-item default " + TypeName(item) + "." + name + ": IsSet=" + property?.IsSet);
            return property is null || property.IsSet;
        }

        int Count(string name)
        {
            var property = FindProperty(item, name);
            return property is { IsCollection: true } && !property.Collection.IsReadOnly ? property.Collection.Count : -1;
        }

        void Set(string name, string value)
        {
            var property = Property(name);
            if (property is { IsSet: false, IsReadOnly: false }) property.SetValue(value);
        }
    }
}
