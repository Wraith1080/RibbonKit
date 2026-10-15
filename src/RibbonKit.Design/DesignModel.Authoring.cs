using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

internal static partial class DesignModel
{
    internal static ModelItem? FindRibbon(ModelItem? item)
    {
        for (var current = item; current is not null; current = current.Parent)
            if (TypeName(current) == "Ribbon") return current;
        return null;
    }

    internal static bool CanAddSingleton(ModelItem ribbon, string name) =>
        FindProperty(ribbon, name) is { IsSet: false, Value: null };

    internal static ModelItem? AddSingleton(ModelItem ribbon, string propertyName, string typeName)
    {
        if (!CanAddSingleton(ribbon, propertyName)) return null;
        using (var scope = ribbon.BeginEdit("Add " + propertyName))
        {
            var child = Create(ribbon, typeName);
            FindProperty(ribbon, propertyName)!.SetValue(child);
            scope.Complete();
            return child;
        }
    }

    internal static ModelItem? AddDefaultItem(ModelItem owner) => !CanAddDefaultItem(owner) ? null : TypeName(owner) switch
    {
        "RibbonComboBox" => AddItem(owner, "ComboBoxItem", "Content", "Item"),
        "RibbonGallery" or "InRibbonGallery" => AddItem(owner, "RibbonGalleryItem", "Content", "Item"),
        "RibbonSplitButton" or "RibbonDropDownButton" => AddItem(owner, "RibbonMenuItem", "Header", "Menu Item"),
        "RibbonMessageBar" => AddItem(owner, "RibbonMessage", "Title", "Notification"),
        _ => null,
    };

    internal static bool CanAddDefaultItem(ModelItem owner) => !(FindProperty(owner, "ItemsSource")?.IsSet ?? false) && TypeName(owner) is
        "RibbonComboBox" or "RibbonGallery" or "InRibbonGallery" or "RibbonSplitButton"
        or "RibbonDropDownButton" or "RibbonMessageBar";
}
