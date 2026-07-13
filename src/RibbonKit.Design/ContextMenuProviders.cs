using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

// Design-time right-click verbs for building a ribbon on the XAML surface.

/// <summary>Adds "Add Tab" to a selected <c>Ribbon</c>.</summary>
public sealed class RibbonContextMenuProvider : ContextMenuProvider
{
    public RibbonContextMenuProvider()
    {
        var addTab = new MenuAction("Add Tab");
        addTab.Execute += OnAddTab;
        Items.Add(addTab);
    }

    private void OnAddTab(object sender, MenuActionEventArgs e) =>
        DesignLog.Run("Add Tab", () =>
        {
            ModelItem ribbon = e.Selection.PrimarySelection;
            using (ModelEditingScope scope = ribbon.BeginEdit("Add Tab"))
            {
                ModelItem tab = DesignModel.Create(ribbon, "RibbonTab");
                tab.Properties["Header"].SetValue("New Tab");

                ModelItem group = DesignModel.Create(ribbon, "RibbonGroup");
                group.Properties["Header"].SetValue("New Group");
                DesignModel.Add(tab, "Groups", group);

                DesignModel.Add(ribbon, "Tabs", tab);
                scope.Complete();
            }
        });
}

/// <summary>Adds "Add Group" to a selected <c>RibbonTab</c>.</summary>
public sealed class RibbonTabContextMenuProvider : ContextMenuProvider
{
    public RibbonTabContextMenuProvider()
    {
        var addGroup = new MenuAction("Add Group");
        addGroup.Execute += OnAddGroup;
        Items.Add(addGroup);
    }

    private void OnAddGroup(object sender, MenuActionEventArgs e) =>
        DesignLog.Run("Add Group", () =>
        {
            ModelItem tab = e.Selection.PrimarySelection;
            using (ModelEditingScope scope = tab.BeginEdit("Add Group"))
            {
                ModelItem group = DesignModel.Create(tab, "RibbonGroup");
                group.Properties["Header"].SetValue("New Group");
                DesignModel.Add(tab, "Groups", group);
                scope.Complete();
            }
        });
}

/// <summary>Adds "Add Button / Toggle / Split / Drop-Down" to a selected <c>RibbonGroup</c>.</summary>
public sealed class RibbonGroupContextMenuProvider : ContextMenuProvider
{
    public RibbonGroupContextMenuProvider()
    {
        AddButtonVerb("Add Button", "RibbonButton");
        AddButtonVerb("Add Toggle Button", "RibbonToggleButton");
        AddButtonVerb("Add Split Button", "RibbonSplitButton");
        AddButtonVerb("Add Drop-Down Button", "RibbonDropDownButton");
    }

    private void AddButtonVerb(string caption, string typeName)
    {
        var action = new MenuAction(caption);
        action.Execute += (sender, e) => AddControl(e, typeName, caption.Substring("Add ".Length));
        Items.Add(action);
    }

    private static void AddControl(MenuActionEventArgs e, string typeName, string label) =>
        DesignLog.Run(label, () =>
        {
            ModelItem group = e.Selection.PrimarySelection;
            using (ModelEditingScope scope = group.BeginEdit(label))
            {
                ModelItem control = DesignModel.Create(group, typeName);

                // All the button types carry their caption in "Header" (see RibbonButton.Header).
                control.Properties["Header"]?.SetValue(label);

                DesignModel.Add(group, "Items", control); // control -> group.Items
                scope.Complete();
            }
        });
}
