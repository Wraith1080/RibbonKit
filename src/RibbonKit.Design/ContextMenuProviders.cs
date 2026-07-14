using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

// Design-time right-click verbs for building a ribbon on the XAML surface.

/// <summary>
/// Ribbon-level verbs: Add Tab, Add Backstage (once), and a Quick Access Toolbar position submenu.
/// </summary>
public sealed class RibbonContextMenuProvider : ContextMenuProvider
{
    private readonly MenuAction _addBackstage;
    private readonly MenuAction _qatTitleBar;
    private readonly MenuAction _qatTabRow;
    private readonly MenuAction _qatBelowRibbon;

    public RibbonContextMenuProvider()
    {
        // Full structure editor (tabs / groups / controls) in one modal, launched from the
        // Ribbon's right-click menu. The verb bodies below remain as quick single-action
        // shortcuts; this opens the richer tree UI.
        var edit = new MenuAction("Edit Ribbon…");
        edit.Execute += OnEditRibbon;
        Items.Add(edit);

        var addTab = new MenuAction("Add Tab");
        addTab.Execute += OnAddTab;
        Items.Add(addTab);

        // Backstage is a set-once property (and what makes the File button appear — it's hidden
        // while Backstage is null). Disabled in UpdateItemStatus once one exists.
        _addBackstage = new MenuAction("Add Backstage");
        _addBackstage.Execute += OnAddBackstage;
        Items.Add(_addBackstage);

        // Quick Access Toolbar position — radio-style submenu, checked on the current value.
        var qat = new MenuGroup("RibbonKit.QatPosition", "Quick Access Toolbar") { HasDropDown = true };
        _qatTitleBar = MakeQatItem("Title Bar", "TitleBar");
        _qatTabRow = MakeQatItem("Tab Row", "TabRow");
        _qatBelowRibbon = MakeQatItem("Below Ribbon", "BelowRibbon");
        qat.Items.Add(_qatTitleBar);
        qat.Items.Add(_qatTabRow);
        qat.Items.Add(_qatBelowRibbon);
        Items.Add(qat);

        UpdateItemStatus += OnUpdateItemStatus;
    }

    private MenuAction MakeQatItem(string caption, string enumValue)
    {
        var action = new MenuAction(caption) { Checkable = true };
        action.Execute += (sender, e) =>
        {
            ModelItem ribbon = e.Selection.PrimarySelection;
            using (ModelEditingScope scope = ribbon.BeginEdit("Quick Access Toolbar: " + caption))
            {
                // Enum set by name string — the property's type converter resolves it (the design
                // assembly can't reference RibbonQuickAccessPosition to pass a boxed value).
                ribbon.Properties["QuickAccessPosition"].SetValue(enumValue);
                scope.Complete();
            }
        };
        return action;
    }

    private void OnUpdateItemStatus(object sender, MenuActionEventArgs e)
    {
        ModelItem ribbon = e.Selection.PrimarySelection;

        // Only one backstage allowed.
        _addBackstage.Enabled = ribbon.Properties["Backstage"].Value is null;

        // Reflect the current QAT position as the checked item (ComputedValue includes the default).
        string current = ribbon.Properties["QuickAccessPosition"].ComputedValue?.ToString() ?? "TabRow";
        _qatTitleBar.Checked = current == "TitleBar";
        _qatTabRow.Checked = current == "TabRow";
        _qatBelowRibbon.Checked = current == "BelowRibbon";
    }

    private void OnEditRibbon(object sender, MenuActionEventArgs e)
    {
        ModelItem ribbon = e.Selection.PrimarySelection;

        // Runs in-process on the VS UI thread, so a plain WPF modal is fine here. The dialog
        // edits the same ModelItem tree these verbs do; each of its edits is its own undo.
        var window = new RibbonEditorWindow(ribbon);
        window.ShowDialog();
    }

    private void OnAddTab(object sender, MenuActionEventArgs e)
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
    }

    private void OnAddBackstage(object sender, MenuActionEventArgs e)
    {
        ModelItem ribbon = e.Selection.PrimarySelection;
        if (ribbon.Properties["Backstage"].Value is not null)
        {
            return; // already has one (belt-and-braces with UpdateItemStatus)
        }

        using (ModelEditingScope scope = ribbon.BeginEdit("Add Backstage"))
        {
            ModelItem backstage = DesignModel.Create(ribbon, "Backstage");

            // Seed one nav item so the backstage isn't empty.
            ModelItem item = DesignModel.Create(ribbon, "BackstageTabItem");
            item.Properties["Header"].SetValue("Info");
            DesignModel.Add(backstage, "Items", item);

            ribbon.Properties["Backstage"].SetValue(backstage);
            scope.Complete();
        }
    }
}

/// <summary>Backstage-level verbs: Add Nav Item (a page) and Add Nav Button (a footer action).</summary>
public sealed class BackstageContextMenuProvider : ContextMenuProvider
{
    public BackstageContextMenuProvider()
    {
        var addItem = new MenuAction("Add Nav Item");
        addItem.Execute += OnAddNavItem;
        Items.Add(addItem);

        var addButton = new MenuAction("Add Nav Button");
        addButton.Execute += OnAddNavButton;
        Items.Add(addButton);
    }

    private void OnAddNavItem(object sender, MenuActionEventArgs e)
    {
        ModelItem backstage = e.Selection.PrimarySelection;
        using (ModelEditingScope scope = backstage.BeginEdit("Add Nav Item"))
        {
            ModelItem item = DesignModel.Create(backstage, "BackstageTabItem");
            item.Properties["Header"].SetValue("Page");
            DesignModel.Add(backstage, "Items", item);
            scope.Complete();
        }
    }

    private void OnAddNavButton(object sender, MenuActionEventArgs e)
    {
        ModelItem backstage = e.Selection.PrimarySelection;
        using (ModelEditingScope scope = backstage.BeginEdit("Add Nav Button"))
        {
            ModelItem item = DesignModel.Create(backstage, "BackstageTabItem");
            item.Properties["Header"].SetValue("Action");
            item.Properties["IsButton"].SetValue(true);      // action, not a page
            item.Properties["Placement"].SetValue("Bottom"); // footer, like Word's Account/Options
            DesignModel.Add(backstage, "Items", item);
            scope.Complete();
        }
    }
}

/// <summary>"Add Group" plus reorder/delete for a selected <c>RibbonTab</c> (within its Ribbon.Tabs).</summary>
public sealed class RibbonTabContextMenuProvider : ContextMenuProvider
{
    public RibbonTabContextMenuProvider()
    {
        var addGroup = new MenuAction("Add Group");
        addGroup.Execute += OnAddGroup;
        Items.Add(addGroup);

        DesignVerbs.AddReorderAndDelete(this, "Tabs", "Tab");
    }

    private void OnAddGroup(object sender, MenuActionEventArgs e)
    {
        ModelItem tab = e.Selection.PrimarySelection;
        using (ModelEditingScope scope = tab.BeginEdit("Add Group"))
        {
            ModelItem group = DesignModel.Create(tab, "RibbonGroup");
            group.Properties["Header"].SetValue("New Group");
            DesignModel.Add(tab, "Groups", group);
            scope.Complete();
        }
    }
}

/// <summary>"Add Button/Toggle/Split/Drop-Down" plus reorder/delete for a selected <c>RibbonGroup</c> (within its Tab.Groups).</summary>
public sealed class RibbonGroupContextMenuProvider : ContextMenuProvider
{
    public RibbonGroupContextMenuProvider()
    {
        AddButtonVerb("Add Button", "RibbonButton");
        AddButtonVerb("Add Toggle Button", "RibbonToggleButton");
        AddButtonVerb("Add Split Button", "RibbonSplitButton");
        AddButtonVerb("Add Drop-Down Button", "RibbonDropDownButton");

        DesignVerbs.AddReorderAndDelete(this, "Groups", "Group");
    }

    private void AddButtonVerb(string caption, string typeName)
    {
        var action = new MenuAction(caption);
        action.Execute += (sender, e) => AddControl(e, typeName, caption.Substring("Add ".Length));
        Items.Add(action);
    }

    private static void AddControl(MenuActionEventArgs e, string typeName, string label)
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
    }
}

/// <summary>
/// Reorder/delete for a selected leaf control (button/toggle/split/drop-down) within its
/// <c>RibbonGroup.Items</c>. Registered on all four button types (one provider serves them all,
/// since it acts on the current selection).
/// </summary>
public sealed class RibbonControlContextMenuProvider : ContextMenuProvider
{
    public RibbonControlContextMenuProvider() =>
        DesignVerbs.AddReorderAndDelete(this, "Items", "Control");
}

/// <summary>Shared "Move Left / Move Right / Delete" verbs added to a provider's menu.</summary>
internal static class DesignVerbs
{
    /// <param name="provider">The provider to add the verbs to.</param>
    /// <param name="parentCollection">The parent collection the selected item lives in (Tabs/Groups/Items).</param>
    /// <param name="noun">Human noun for the captions ("Tab", "Group", "Control").</param>
    public static void AddReorderAndDelete(ContextMenuProvider provider, string parentCollection, string noun)
    {
        var left = new MenuAction("Move " + noun + " Left");
        left.Execute += (s, e) => DesignModel.Move(e.Selection.PrimarySelection, parentCollection, -1);
        provider.Items.Add(left);

        var right = new MenuAction("Move " + noun + " Right");
        right.Execute += (s, e) => DesignModel.Move(e.Selection.PrimarySelection, parentCollection, +1);
        provider.Items.Add(right);

        var delete = new MenuAction("Delete " + noun);
        delete.Execute += (s, e) => DesignModel.Delete(e.Selection.PrimarySelection, parentCollection);
        provider.Items.Add(delete);
    }
}
