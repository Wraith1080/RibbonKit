using Microsoft.VisualStudio.DesignTools.Extensibility;
using Microsoft.VisualStudio.DesignTools.Extensibility.Metadata;

namespace RibbonKit.Design;

/// <summary>Toolbox discovery intent for hosts that reflect project-reference controls.</summary>
internal static class ToolboxMetadata
{
    // Keep these names aligned with the package manifest. Contract tests also
    // require every public FrameworkElement to have an explicit inclusion/exclusion.
    private static readonly string[] IncludedTypes =
    {
        "RibbonKit.Controls.Ribbon",
        "RibbonKit.Controls.RibbonTab",
        "RibbonKit.Controls.RibbonGroup",
        "RibbonKit.Controls.RibbonGroupSeparator",
        "RibbonKit.Controls.RibbonButton",
        "RibbonKit.Controls.RibbonToggleButton",
        "RibbonKit.Controls.RibbonCheckBox",
        "RibbonKit.Controls.RibbonRadioButton",
        "RibbonKit.Controls.RibbonSplitButton",
        "RibbonKit.Controls.RibbonDropDownButton",
        "RibbonKit.Controls.RibbonComboBox",
        "RibbonKit.Controls.RibbonTextBox",
        "RibbonKit.Controls.RibbonScrollBar",
        "RibbonKit.Controls.RibbonMenuItem",
        "RibbonKit.Controls.RibbonGallery",
        "RibbonKit.Controls.InRibbonGallery",
        "RibbonKit.Controls.RibbonGalleryItem",
        "RibbonKit.Controls.Backstage",
        "RibbonKit.Controls.BackstageTabItem",
        "RibbonKit.Controls.RibbonApplicationMenu",
        "RibbonKit.Controls.RibbonApplicationMenuItem",
        "RibbonKit.Controls.RibbonApplicationMenuSeparator",
        "RibbonKit.Controls.RibbonApplicationMenuPaneItem",
        "RibbonKit.Controls.RibbonApplicationMenuButton",
        "RibbonKit.Controls.RibbonOptionsPage",
        "RibbonKit.Controls.RibbonCustomizePage",
        "RibbonKit.Controls.RibbonSimplifiedCustomizePage",
        "RibbonKit.Controls.RibbonQuickAccessPage",
        "RibbonKit.Controls.RibbonMessageBar",
        "RibbonKit.Controls.RibbonMessage",
        "RibbonKit.Controls.MdiContainer",
    };

    private static readonly string[] ExcludedTypes =
    {
        "RibbonKit.Controls.RibbonWindow",
        "RibbonKit.Controls.RibbonOptionsDialog",
        "RibbonKit.Controls.RibbonCustomizeEditDialog",
        "RibbonKit.Controls.RibbonScreenTip",
        "RibbonKit.Controls.RibbonMergeSource",
        "RibbonKit.Controls.MdiChild",
        "RibbonKit.Controls.RibbonTabControl",
        "RibbonKit.Controls.RibbonGroupsHost",
        "RibbonKit.Controls.RibbonQuickAccessToolBar",
        "RibbonKit.Controls.BackstageNavPanel",
        "RibbonKit.Layout.RibbonGroupsPanel",
        "RibbonKit.Layout.RibbonQuickAccessPanel",
        "RibbonKit.Layout.RibbonScrollContentHost",
        "RibbonKit.Layout.MdiCanvasPanel",
    };

    internal static void Register(AttributeTableBuilder builder)
    {
        foreach (string type in IncludedTypes)
            builder.AddCustomAttributes(type,
                new ToolboxBrowsableAttribute(true),
                new ToolboxTabNameAttribute("RibbonKit"),
                new ToolboxCategoryAttribute("RibbonKit", false));

        foreach (string type in ExcludedTypes)
            builder.AddCustomAttributes(type, new ToolboxBrowsableAttribute(false));
    }
}
