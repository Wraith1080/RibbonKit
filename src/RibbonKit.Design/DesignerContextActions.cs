using System;
using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

internal static class DesignerContextActions
{
    internal static void AddPreviewMenus(ContextMenuProvider provider)
    {
        var tabs = Group("PreviewTab", "Preview Ribbon Tab");
        var file = Group("PreviewFile", "Preview File Page");
        provider.Items.Add(tabs);
        provider.Items.Add(file);
        provider.UpdateItemStatus += (_, e) =>
        {
            tabs.Items.Clear(); file.Items.Clear();
            var ribbon = DesignModel.FindRibbon(e.Selection.PrimarySelection);
            if (ribbon is null) return;
            Add(tabs, "Use authored tab", () => TabPreviewCoordinator.SetTab(ribbon, null));
            var children = DesignModel.Children(ribbon, "Tabs");
            for (int i = 0; i < children.Count; i++)
            {
                int index = i;
                Add(tabs, Caption(children[i], "Tab " + (i + 1)), () =>
                {
                    TabPreviewCoordinator.SetFileSurface(ribbon, FileSurfacePreview.Closed);
                    TabPreviewCoordinator.SetTab(ribbon, index);
                });
            }
            Add(file, "Close File preview", () => TabPreviewCoordinator.SetFileSurface(ribbon, FileSurfacePreview.Closed));
            var backstage = DesignModel.FindProperty(ribbon, "Backstage")?.Value;
            if (backstage is not null && DesignModel.TypeName(backstage) == "Backstage")
            {
                var pages = DesignModel.Children(backstage, "Items");
                for (int i = 0; i < pages.Count; i++)
                {
                    if (DesignModel.GetBool(pages[i], "IsButton")) continue;
                    int index = i;
                    Add(file, "Backstage: " + Caption(pages[i], "Page " + (i + 1)), () =>
                    {
                        TabPreviewCoordinator.SetBackstagePage(backstage, index);
                        TabPreviewCoordinator.SetFileSurface(ribbon, FileSurfacePreview.Backstage);
                    });
                }
            }
            var menu = DesignModel.FindProperty(ribbon, "ApplicationMenu")?.Value;
            if (menu is null || DesignModel.TypeName(menu) != "RibbonApplicationMenu") return;
            PreviewMenuPane("Application menu: default pane", -1);
            var commands = DesignModel.Children(menu, "Items");
            for (int i = 0; i < commands.Count; i++)
                if (DesignModel.TypeName(commands[i]) == "RibbonApplicationMenuItem"
                    && DesignModel.FindProperty(commands[i], "Content")?.Value is not null)
                    PreviewMenuPane("Application menu: " + Caption(commands[i], "Pane " + (i + 1)), i);

            void PreviewMenuPane(string caption, int index) => Add(file, caption, () =>
            {
                ApplicationMenuPreviewCoordinator.SetActiveIndex(menu, index);
                TabPreviewCoordinator.SetFileSurface(ribbon, FileSurfacePreview.ApplicationMenu);
            });
        };
    }

    internal static void AddOwnerMenus(ContextMenuProvider provider)
    {
        var backstage = Group("BackstageAuthoring", "Backstage");
        var application = Group("ApplicationMenuAuthoring", "Application Menu");
        var messages = Group("MessageAuthoring", "Messages");
        var qat = Group("QatAuthoring", "Add Quick Access Command");
        provider.Items.Add(backstage); provider.Items.Add(application);
        provider.Items.Add(messages); provider.Items.Add(qat);
        provider.UpdateItemStatus += (_, e) =>
        {
            backstage.Items.Clear(); application.Items.Clear(); messages.Items.Clear(); qat.Items.Clear();
            var ribbon = DesignModel.FindRibbon(e.Selection.PrimarySelection);
            if (ribbon is null) return;
            var stage = DesignModel.FindProperty(ribbon, "Backstage")?.Value;
            if (stage is not null && DesignModel.TypeName(stage) == "Backstage"
                && DesignModel.FindProperty(stage, "ItemsSource")?.IsSet != true)
            {
                Add(backstage, "Add Page", () => DesignModel.AddItem(stage, "BackstageTabItem", "Header", "Page"));
                Add(backstage, "Add Footer Action", () =>
                {
                    using (var scope = stage.BeginEdit("Add Backstage Footer Action"))
                    {
                        var page = DesignModel.Create(stage, "BackstageTabItem");
                        page.Properties["Header"].SetValue("Action");
                        page.Properties["IsButton"].SetValue(true);
                        page.Properties["Placement"].SetValue("Bottom");
                        DesignModel.Add(stage, "Items", page); scope.Complete();
                    }
                });
            }
            var menu = DesignModel.FindProperty(ribbon, "ApplicationMenu")?.Value;
            if (menu is not null && DesignModel.TypeName(menu) == "RibbonApplicationMenu"
                && DesignModel.FindProperty(menu, "ItemsSource")?.IsSet != true)
            {
                Add(application, "Add Command", () => DesignModel.AddApplicationMenuEntry(menu, "RibbonApplicationMenuItem", "New Command"));
                Add(application, "Add Separator", () => DesignModel.AddApplicationMenuEntry(menu, "RibbonApplicationMenuSeparator", null));
                Add(application, "Add Default Pane Item", () => DesignModel.AddApplicationMenuContentItem(menu, "DefaultContent", "Vertical", "RibbonApplicationMenuPaneItem", "Pane Item"));
                Add(application, "Add Footer Button", () => DesignModel.AddApplicationMenuContentItem(menu, "FooterContent", "Horizontal", "RibbonApplicationMenuButton", "Footer Button"));
            }
            Add(messages, "Add Message Bar", () => DesignModel.AddSingleton(ribbon, "MessageBar", "RibbonMessageBar"),
                DesignModel.CanAddSingleton(ribbon, "MessageBar"));
            var bar = DesignModel.FindProperty(ribbon, "MessageBar")?.Value;
            if (bar is not null && DesignModel.CanAddDefaultItem(bar)) Add(messages, "Add Message", () => DesignModel.AddDefaultItem(bar));
            foreach (string type in new[] { "RibbonButton", "RibbonToggleButton", "RibbonSplitButton", "RibbonDropDownButton", "RibbonComboBox", "RibbonGallery", "InRibbonGallery" })
            {
                string commandType = type;
                string caption = type switch
                {
                    "RibbonButton" => "Button", "RibbonToggleButton" => "Toggle Button",
                    "RibbonSplitButton" => "Split Button", "RibbonDropDownButton" => "Drop-Down Button",
                    "RibbonComboBox" => "Combo Box", "RibbonGallery" => "Gallery", _ => "In-Ribbon Gallery",
                };
                Add(qat, caption, () => DesignModel.AddControl(ribbon, "QuickAccessItems", commandType, size: "Small"));
            }
        };
    }

    private static string Caption(ModelItem item, string fallback)
    {
        string caption = DesignModel.GetCaption(item);
        return string.IsNullOrEmpty(caption) ? fallback : caption;
    }

    private static MenuGroup Group(string id, string caption) => new MenuGroup("RibbonKit." + id, caption) { HasDropDown = true };

    private static void Add(MenuGroup group, string caption, Action action, bool enabled = true)
    {
        var item = new MenuAction(caption) { Enabled = enabled };
        item.Execute += (_, _) =>
        {
            try { action(); }
            catch (Exception error) { DesignLog.Error(caption, error); throw; }
        };
        group.Items.Add(item);
    }
}

/// <summary>Owner-based authoring and design-only File page preview for File-surface selections.</summary>
public sealed class RibbonFileContextMenuProvider : ContextMenuProvider
{
    /// <summary>Creates File-surface authoring and preview actions.</summary>
    public RibbonFileContextMenuProvider()
    {
        DesignerContextActions.AddOwnerMenus(this);
        DesignerContextActions.AddPreviewMenus(this);
    }
}

/// <summary>Adds messages to a selected authored message bar.</summary>
public sealed class RibbonMessageBarContextMenuProvider : ContextMenuProvider
{
    /// <summary>Creates message-bar Add and owner actions.</summary>
    public RibbonMessageBarContextMenuProvider()
    {
        var add = new MenuAction("Add Message");
        add.Execute += (_, e) => DesignModel.AddDefaultItem(e.Selection.PrimarySelection);
        Items.Add(add);
    }
}
