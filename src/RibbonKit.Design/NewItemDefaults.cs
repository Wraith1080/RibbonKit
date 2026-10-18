using System;

namespace RibbonKit.Design;

// Model-independent creation policy. The adapter supplies local-assignment and
// collection state; no runtime controls or designer SDK objects enter this policy.
internal static class NewItemDefaults
{
    internal static bool IsFrameworkType(string type) => type is "ComboBoxItem" or "StackPanel" or "TextBlock" or "Separator";

    internal static bool IsSimplifiedCommand(string type) => type is
        "RibbonButton" or "RibbonToggleButton" or "RibbonSplitButton" or "RibbonDropDownButton"
        or "RibbonComboBox" or "RibbonGallery" or "InRibbonGallery";

    internal static void Apply(string type, Func<string, bool> isSet, Func<string, int> childCount,
        Action<string, string> setValue, Action<string, string, string, string> addChild, bool ribbonStarterTab)
    {
        if (IsSimplifiedCommand(type))
        {
            Set("Ribbon.SimplifiedPlacement", "Inline");
            Set("Ribbon.SimplifiedSize", "Medium");
        }

        string? caption = type switch
        {
            "RibbonTab" => ribbonStarterTab ? "Home" : "New Tab",
            "RibbonGroup" => "New Group",
            "RibbonButton" => "Button",
            "RibbonToggleButton" => "Toggle Button",
            "RibbonCheckBox" => "Check Box",
            "RibbonRadioButton" => "Radio Button",
            "RibbonSplitButton" => "Split Button",
            "RibbonDropDownButton" => "Drop-Down Button",
            "RibbonComboBox" => "Combo Box",
            "RibbonTextBox" => "Text Box",
            "RibbonGallery" or "InRibbonGallery" => "Gallery",
            "RibbonMenuItem" => "Menu Item",
            "BackstageTabItem" => "Page",
            "RibbonApplicationMenuItem" => "New Command",
            _ => null,
        };
        if (caption is not null) Set("Header", caption);
        if (type == "RibbonComboBox") Set("InputWidth", "130");
        switch (type)
        {
            case "ComboBoxItem": case "RibbonGalleryItem": Set("Content", "Item"); break;
            case "RibbonApplicationMenuPaneItem": Set("Content", "Pane Item"); break;
            case "RibbonApplicationMenuButton": Set("Content", "Footer Button"); break;
            case "TextBlock": Set("Text", "Text"); break;
            case "RibbonMessage": Set("Title", "Notification"); Set("Message", "Message text"); break;
        }

        switch (type)
        {
            case "Ribbon": Seed("Tabs", "RibbonTab", "Header", "Home"); break;
            case "RibbonTab": Seed("Groups", "RibbonGroup", "Header", ribbonStarterTab ? "Group" : "New Group"); break;
            case "Backstage": Seed("Items", "BackstageTabItem", "Header", "Info"); break;
            case "RibbonApplicationMenu": Seed("Items", "RibbonApplicationMenuItem", "Header", "New Command"); break;
            case "RibbonMessageBar": Seed("Items", "RibbonMessage", "Title", "Notification"); break;
            case "RibbonComboBox": Seed("Items", "ComboBoxItem", "Content", "Item"); break;
            case "RibbonGallery": case "InRibbonGallery": Seed("Items", "RibbonGalleryItem", "Content", "Item"); break;
            case "RibbonDropDownButton": case "RibbonSplitButton": Seed("Items", "RibbonMenuItem", "Header", "Menu Item"); break;
        }

        void Set(string property, string value)
        {
            if (!isSet(property)) setValue(property, value);
        }

        void Seed(string collection, string childType, string property, string value)
        {
            // An explicit empty collection/ItemsSource is still an authored choice.
            if (!isSet(collection) && childCount(collection) == 0
                && (collection != "Items" || !isSet("ItemsSource")))
                addChild(collection, childType, property, value);
        }
    }
}
