using System;
using System.Windows;
using Microsoft.VisualStudio.DesignTools.Extensibility.Interaction;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

// Design-time right-click verbs for building a ribbon on the XAML surface.

/// <summary>
/// Ribbon-level verbs: Add Tab, singleton File surfaces, and a Quick Access Toolbar position submenu.
/// </summary>
public sealed class RibbonContextMenuProvider : ContextMenuProvider
{
    private readonly MenuAction _addBackstage;
    private readonly MenuAction _addApplicationMenu;
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

        _addApplicationMenu = new MenuAction("Add Application Menu");
        _addApplicationMenu.Execute += OnAddApplicationMenu;
        Items.Add(_addApplicationMenu);

        // Quick Access Toolbar position — radio-style submenu, checked on the current value.
        var qat = new MenuGroup("RibbonKit.QatPosition", "Quick Access Toolbar") { HasDropDown = true };
        _qatTitleBar = MakeQatItem("Title Bar", "TitleBar");
        _qatTabRow = MakeQatItem("Tab Row", "TabRow");
        _qatBelowRibbon = MakeQatItem("Below Ribbon", "BelowRibbon");
        qat.Items.Add(_qatTitleBar);
        qat.Items.Add(_qatTabRow);
        qat.Items.Add(_qatBelowRibbon);
        Items.Add(qat);

        DesignerContextActions.AddOwnerMenus(this);
        DesignerContextActions.AddPreviewMenus(this);

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
        _addApplicationMenu.Enabled = ribbon.Properties["ApplicationMenu"].Value is null;

        // Reflect the current QAT position as the checked item (ComputedValue includes the default).
        string current = ribbon.Properties["QuickAccessPosition"].ComputedValue?.ToString() ?? "TabRow";
        _qatTitleBar.Checked = current == "TitleBar";
        _qatTabRow.Checked = current == "TabRow";
        _qatBelowRibbon.Checked = current == "BelowRibbon";
    }

    private void OnEditRibbon(object sender, MenuActionEventArgs e)
    {
        // Runs in-process on the VS UI thread, so a plain WPF modal is fine here. The dialog
        // edits the same ModelItem tree these verbs do; each of its edits is its own undo.
        // Wrapped so a failure surfaces (a throw here would otherwise be swallowed by the
        // designer and the editor would just never appear).
        try
        {
            DesignLog.Write("Edit Ribbon: opening editor…");
            ModelItem ribbon = e.Selection.PrimarySelection;
            var window = new RibbonEditorWindow(ribbon);
            window.ShowDialog();
            DesignLog.Write("Edit Ribbon: editor closed.");
        }
        catch (Exception ex)
        {
            DesignLog.Error("OnEditRibbon", ex);
            MessageBox.Show(
                "The Ribbon Editor could not open.\n\n" + ex.Message
                + "\n\nFull details were logged to:\n" + DesignLog.Path,
                "Ribbon Editor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnAddTab(object sender, MenuActionEventArgs e)
    {
        DesignModel.AddTab(e.Selection.PrimarySelection);
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

            ribbon.Properties["Backstage"].SetValue(backstage);
            scope.Complete();
        }
    }

    private void OnAddApplicationMenu(object sender, MenuActionEventArgs e)
    {
        DesignModel.AddApplicationMenu(e.Selection.PrimarySelection);
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
        DesignerContextActions.AddPreviewMenus(this);
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
        DesignerContextActions.AddPreviewMenus(this);
    }

    private void OnAddGroup(object sender, MenuActionEventArgs e)
    {
        DesignModel.AddGroup(e.Selection.PrimarySelection);
    }
}

/// <summary>Control-add verbs plus reorder/delete for a selected <c>RibbonGroup</c> (within its Tab.Groups).</summary>
public sealed class RibbonGroupContextMenuProvider : ContextMenuProvider
{
    public RibbonGroupContextMenuProvider()
    {
        AddControlVerb("Add Button", "RibbonButton");
        AddControlVerb("Add Toggle Button", "RibbonToggleButton");
        AddControlVerb("Add Check Box", "RibbonCheckBox");
        AddControlVerb("Add Radio Button", "RibbonRadioButton");
        AddControlVerb("Add Split Button", "RibbonSplitButton");
        AddControlVerb("Add Drop-Down Button", "RibbonDropDownButton");
        AddControlVerb("Add Text Box", "RibbonTextBox");
        AddControlVerb("Add Combo Box", "RibbonComboBox");
        AddControlVerb("Add In-Ribbon Gallery", "InRibbonGallery");
        AddControlVerb("Add Drop-Down Gallery", "RibbonGallery");
        AddControlVerb("Add Scroll Bar", "RibbonScrollBar");
        AddControlVerb("Add Group Separator", "RibbonGroupSeparator");
        var addStack = new MenuAction("Add Stack Panel");
        addStack.Execute += (_, e) => DesignModel.AddStackPanel(e.Selection.PrimarySelection, "Items", "Vertical");
        Items.Add(addStack);

        DesignVerbs.AddReorderAndDelete(this, "Groups", "Group");
    }

    private void AddControlVerb(string caption, string typeName)
    {
        var action = new MenuAction(caption);
        action.Execute += (sender, e) => DesignModel.AddControl(e.Selection.PrimarySelection, "Items", typeName);
        Items.Add(action);
    }
}

/// <summary>
/// Reorder/delete for a selected leaf command or compact option control within its
/// <c>RibbonGroup.Items</c> or a panel's <c>Children</c>. One provider serves every registered type because it acts on the
/// current selection.
/// </summary>
public sealed class RibbonControlContextMenuProvider : ContextMenuProvider
{
    public RibbonControlContextMenuProvider()
    {
        var addItem = new MenuAction("Add Item");
        addItem.Execute += (_, e) => DesignModel.AddDefaultItem(e.Selection.PrimarySelection);
        Items.Add(addItem);
        UpdateItemStatus += (_, e) => addItem.Enabled = DesignModel.CanAddDefaultItem(e.Selection.PrimarySelection);
        DesignVerbs.AddReorderAndDelete(this, null, "Control");
    }
}

/// <summary>Shared "Move Left / Move Right / Delete" verbs added to a provider's menu.</summary>
internal static class DesignVerbs
{
    /// <param name="provider">The provider to add the verbs to.</param>
    /// <param name="parentCollection">The parent collection (Tabs/Groups), or null to resolve Items/Children for a leaf.</param>
    /// <param name="noun">Human noun for the captions ("Tab", "Group", "Control").</param>
    public static void AddReorderAndDelete(ContextMenuProvider provider, string? parentCollection, string noun)
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
