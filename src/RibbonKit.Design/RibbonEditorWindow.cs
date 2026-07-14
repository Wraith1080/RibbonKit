using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>The level a tree node sits at, which decides its parent collection and the verbs that apply.</summary>
internal enum NodeKind
{
    Ribbon,
    Tab,
    Group,
    Control,
}

/// <summary>Pairs a <see cref="ModelItem"/> with the <see cref="NodeKind"/> the tree is showing it as.</summary>
internal sealed class NodeInfo
{
    public NodeInfo(ModelItem item, NodeKind kind)
    {
        Item = item;
        Kind = kind;
    }

    public ModelItem Item { get; }

    public NodeKind Kind { get; }

    /// <summary>The property name of the collection this node lives in on its parent (null for the ribbon root).</summary>
    public string ParentCollection => Kind switch
    {
        NodeKind.Tab => "Tabs",
        NodeKind.Group => "Groups",
        NodeKind.Control => "Items",
        _ => null,
    };
}

/// <summary>
/// Design-time modal editor for a ribbon's structure — tabs, groups, and the leaf controls
/// (button / toggle / split / drop-down) inside them. Launched from the Ribbon's
/// "Edit Ribbon…" context-menu verb.
/// </summary>
/// <remarks>
/// <para>
/// This runs INSIDE the Visual Studio process (the design assembly is net472 and loaded by
/// VS), so a plain WPF <see cref="Window"/> can be shown with <see cref="Window.ShowDialog"/>;
/// only the design SURFACE is process-isolated, not extension code. The window is a
/// self-contained, code-built visual tree: the design assembly does not reference RibbonKit,
/// so it cannot use the ribbon's own themes/controls.
/// </para>
/// <para>
/// All edits go through <see cref="DesignModel"/> against the live <see cref="ModelItem"/> tree.
/// Each structural change (add / move / delete / rename) is applied immediately in its own
/// <c>ModelEditingScope</c>, so each is a single undo — the same transaction model as the
/// right-click verbs, just with a richer tree UI. There is intentionally no OK/Cancel
/// "transaction" around the whole session; the surface updates live and Ctrl+Z reverts one
/// action at a time.
/// </para>
/// </remarks>
internal sealed class RibbonEditorWindow : Window
{
    private readonly ModelItem _ribbon;
    private readonly TreeView _tree = new TreeView { BorderThickness = new Thickness(1) };
    private readonly TextBox _headerBox = new TextBox { MinWidth = 160, VerticalContentAlignment = VerticalAlignment.Center };
    private readonly TextBlock _typeText = new TextBlock { Opacity = 0.75, VerticalAlignment = VerticalAlignment.Center };
    private readonly Dictionary<ModelItem, TreeViewItem> _map = new Dictionary<ModelItem, TreeViewItem>();

    private Button _addGroup;
    private Button _addControl;
    private Button _moveUp;
    private Button _moveDown;
    private Button _delete;
    private Button _rename;
    private ComboBox _previewCombo;
    private bool _syncingPreview;

    /// <summary>Creates the editor over <paramref name="ribbon"/> (the selected Ribbon's design model item).</summary>
    public RibbonEditorWindow(ModelItem ribbon)
    {
        _ribbon = ribbon ?? throw new ArgumentNullException(nameof(ribbon));

        Title = "Ribbon Editor";
        Width = 720;
        Height = 520;
        MinWidth = 560;
        MinHeight = 380;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UseLayoutRounding = true;

        // Own the dialog to the VS main window so it is properly modal over the IDE (and
        // centres/minimises with it). Best-effort: a null/zero HWND just leaves it unowned.
        try
        {
            IntPtr vs = Process.GetCurrentProcess().MainWindowHandle;
            if (vs != IntPtr.Zero)
            {
                new WindowInteropHelper(this).Owner = vs;
            }
        }
        catch
        {
            // Non-fatal: without an owner the window is still shown, just not VS-owned.
        }

        Content = BuildLayout();
        RebuildTree();
    }

    // ---- UI construction --------------------------------------------------------------

    private UIElement BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(10) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // toolbar
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // body
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // footer

        Panel toolbar = BuildToolbar();
        Grid.SetRow(toolbar, 0);
        root.Children.Add(toolbar);

        UIElement body = BuildBody();
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        UIElement footer = BuildFooter();
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        return root;
    }

    private Panel BuildToolbar()
    {
        _toolbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };

        Button addTab = MakeButton("Add Tab", (_, _) => OnAddTab());
        _addGroup = MakeButton("Add Group", (_, _) => OnAddGroup());
        _addControl = MakeButton("Add Control ▾", OnAddControlClick);
        _moveUp = MakeButton("Move Up", (_, _) => OnMove(-1));
        _moveDown = MakeButton("Move Down", (_, _) => OnMove(+1));
        _delete = MakeButton("Delete", (_, _) => OnDelete());

        _toolbar.Children.Add(addTab);
        _toolbar.Children.Add(_addGroup);
        _toolbar.Children.Add(_addControl);
        _toolbar.Children.Add(new Separator { Width = 1, Margin = new Thickness(4, 2, 4, 2) });
        _toolbar.Children.Add(_moveUp);
        _toolbar.Children.Add(_moveDown);
        _toolbar.Children.Add(_delete);

        // The "Add Control" type menu.
        _addControlMenu = new ContextMenu { PlacementTarget = _addControl, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        _addControlMenu.Items.Add(MakeControlMenuItem("Button", "RibbonButton"));
        _addControlMenu.Items.Add(MakeControlMenuItem("Toggle Button", "RibbonToggleButton"));
        _addControlMenu.Items.Add(MakeControlMenuItem("Split Button", "RibbonSplitButton"));
        _addControlMenu.Items.Add(MakeControlMenuItem("Drop-Down Button", "RibbonDropDownButton"));

        return _toolbar;
    }

    private WrapPanel _toolbar;
    private ContextMenu _addControlMenu;

    private UIElement BuildBody()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _tree.SelectedItemChanged += (_, _) => UpdateDetails();
        Grid.SetColumn(_tree, 0);
        grid.Children.Add(_tree);

        UIElement details = BuildDetailsPanel();
        Grid.SetColumn(details, 2);
        grid.Children.Add(details);

        return grid;
    }

    private UIElement BuildDetailsPanel()
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };

        panel.Children.Add(new TextBlock
        {
            Text = "Selected item",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var typeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        typeRow.Children.Add(new TextBlock { Text = "Type: ", VerticalAlignment = VerticalAlignment.Center });
        typeRow.Children.Add(_typeText);
        panel.Children.Add(typeRow);

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(new TextBlock { Text = "Header: ", VerticalAlignment = VerticalAlignment.Center });
        headerRow.Children.Add(_headerBox);
        _rename = MakeButton("Rename", (_, _) => OnRename());
        headerRow.Children.Add(_rename);
        panel.Children.Add(headerRow);

        // Enter in the header box commits the rename.
        _headerBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                OnRename();
                e.Handled = true;
            }
        };

        panel.Children.Add(new TextBlock
        {
            Text = "Changes apply immediately. Each add, move, delete, or rename is a single "
                 + "undo (Ctrl+Z) on the design surface.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Thickness(0, 16, 0, 0),
        });

        // Design-only tab preview: shows a tab on the surface without writing to the XAML.
        panel.Children.Add(new Separator { Margin = new Thickness(0, 16, 0, 12) });

        var previewRow = new StackPanel { Orientation = Orientation.Horizontal };
        previewRow.Children.Add(new TextBlock { Text = "Preview tab: ", VerticalAlignment = VerticalAlignment.Center });
        _previewCombo = new ComboBox { MinWidth = 180 };
        _previewCombo.SelectionChanged += OnPreviewChanged;
        previewRow.Children.Add(_previewCombo);
        panel.Children.Add(previewRow);

        panel.Children.Add(new TextBlock
        {
            Text = "Design-only: renders a tab on the surface without changing your XAML or the "
                 + "running app. Reset to “(no preview)” to clear it.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Thickness(0, 4, 0, 0),
        });

        return panel;
    }

    private UIElement BuildFooter()
    {
        var footer = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hint = new TextBlock
        {
            Text = "Right-click items on the surface for the same verbs.",
            Opacity = 0.6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(hint, 0);
        footer.Children.Add(hint);

        var close = MakeButton("Close", (_, _) => Close());
        close.MinWidth = 84;
        close.IsDefault = true;
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);

        return footer;
    }

    private static Button MakeButton(string text, RoutedEventHandler onClick)
    {
        var button = new Button
        {
            Content = text,
            Margin = new Thickness(0, 0, 6, 0),
            Padding = new Thickness(10, 4, 10, 4),
            MinWidth = 72,
        };
        button.Click += onClick;
        return button;
    }

    private MenuItem MakeControlMenuItem(string caption, string typeName)
    {
        var item = new MenuItem { Header = caption };
        item.Click += (_, _) => OnAddControl(typeName, caption);
        return item;
    }

    // ---- Tree building ----------------------------------------------------------------

    private void RebuildTree(ModelItem select = null)
    {
        _map.Clear();
        _tree.Items.Clear();

        var rootItem = MakeTreeItem(new NodeInfo(_ribbon, NodeKind.Ribbon), "Ribbon");
        rootItem.IsExpanded = true;
        _tree.Items.Add(rootItem);

        foreach (ModelItem tab in DesignModel.Children(_ribbon, "Tabs"))
        {
            TreeViewItem tabItem = MakeTreeItem(new NodeInfo(tab, NodeKind.Tab), DesignModel.Header(tab));
            tabItem.IsExpanded = true;
            rootItem.Items.Add(tabItem);

            foreach (ModelItem group in DesignModel.Children(tab, "Groups"))
            {
                TreeViewItem groupItem = MakeTreeItem(new NodeInfo(group, NodeKind.Group), DesignModel.Header(group));
                groupItem.IsExpanded = true;
                tabItem.Items.Add(groupItem);

                foreach (ModelItem control in DesignModel.Children(group, "Items"))
                {
                    string label = DesignModel.Header(control);
                    string display = string.IsNullOrEmpty(label)
                        ? FriendlyType(DesignModel.TypeName(control))
                        : label + "  [" + FriendlyType(DesignModel.TypeName(control)) + "]";
                    TreeViewItem controlItem = MakeTreeItem(new NodeInfo(control, NodeKind.Control), display);
                    groupItem.Items.Add(controlItem);
                }
            }
        }

        // Restore selection to the requested item (default: the ribbon root).
        ModelItem target = select ?? _ribbon;
        if (_map.TryGetValue(target, out TreeViewItem selected))
        {
            selected.IsSelected = true;
            selected.BringIntoView();
        }

        PopulatePreviewCombo();
        UpdateDetails();
    }

    /// <summary>Rebuilds the "Preview tab" list from the current tabs, keeping any active preview selected.</summary>
    private void PopulatePreviewCombo()
    {
        _syncingPreview = true;
        try
        {
            _previewCombo.Items.Clear();
            _previewCombo.Items.Add("(no preview)");

            IReadOnlyList<ModelItem> tabs = DesignModel.Children(_ribbon, "Tabs");
            for (int i = 0; i < tabs.Count; i++)
            {
                string header = DesignModel.Header(tabs[i]);
                _previewCombo.Items.Add("Tab " + (i + 1) + (string.IsNullOrEmpty(header) ? string.Empty : ": " + header));
            }

            int selected = 0;
            if (TabPreviewCoordinator.TryGet(_ribbon, out int idx) && idx >= 0 && idx < tabs.Count)
            {
                selected = idx + 1; // +1 for the "(no preview)" row at index 0
            }
            else if (TabPreviewCoordinator.CurrentIndex.HasValue)
            {
                // The previewed tab no longer exists (e.g. it was deleted) — clear the preview.
                TabPreviewCoordinator.Set(_ribbon, null);
            }

            _previewCombo.SelectedIndex = selected;
        }
        finally
        {
            _syncingPreview = false;
        }
    }

    private void OnPreviewChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPreview)
        {
            return;
        }

        int sel = _previewCombo.SelectedIndex;
        TabPreviewCoordinator.Set(_ribbon, sel <= 0 ? (int?)null : sel - 1);
    }

    private TreeViewItem MakeTreeItem(NodeInfo info, string text)
    {
        var item = new TreeViewItem
        {
            Header = string.IsNullOrEmpty(text) ? "(unnamed)" : text,
            Tag = info,
        };
        _map[info.Item] = item;
        return item;
    }

    private static string FriendlyType(string typeName) => typeName switch
    {
        "RibbonButton" => "Button",
        "RibbonToggleButton" => "Toggle",
        "RibbonSplitButton" => "Split",
        "RibbonDropDownButton" => "Drop-Down",
        _ => typeName,
    };

    // ---- Selection / command state ----------------------------------------------------

    private NodeInfo Selected => (_tree.SelectedItem as TreeViewItem)?.Tag as NodeInfo;

    private void UpdateDetails()
    {
        NodeInfo node = Selected;

        if (node is null)
        {
            _typeText.Text = string.Empty;
            _headerBox.Text = string.Empty;
        }
        else
        {
            _typeText.Text = node.Kind == NodeKind.Control
                ? FriendlyType(DesignModel.TypeName(node.Item)) + " (" + DesignModel.TypeName(node.Item) + ")"
                : node.Kind.ToString();
            _headerBox.Text = node.Kind == NodeKind.Ribbon ? string.Empty : DesignModel.Header(node.Item);
        }

        bool isTab = node?.Kind == NodeKind.Tab;
        bool isGroup = node?.Kind == NodeKind.Group;
        bool isControl = node?.Kind == NodeKind.Control;
        bool renameable = isTab || isGroup || isControl;

        // Add Group needs a tab context; Add Control needs a group context.
        _addGroup.IsEnabled = ResolveTab(node) != null;
        _addControl.IsEnabled = ResolveGroup(node) != null;

        _rename.IsEnabled = renameable;
        _headerBox.IsEnabled = renameable;
        _delete.IsEnabled = renameable;

        // Move enabled only when there is somewhere to move to.
        int index = renameable ? DesignModel.IndexInParent(node.Item, node.ParentCollection) : -1;
        int count = renameable ? DesignModel.SiblingCount(node.Item, node.ParentCollection) : 0;
        _moveUp.IsEnabled = renameable && index > 0;
        _moveDown.IsEnabled = renameable && index >= 0 && index < count - 1;
    }

    /// <summary>The tab a new group would go into, given the current selection (tab / group / control ancestor).</summary>
    private ModelItem ResolveTab(NodeInfo node) => node?.Kind switch
    {
        NodeKind.Tab => node.Item,
        NodeKind.Group => node.Item.Parent,
        NodeKind.Control => node.Item.Parent?.Parent,
        _ => null,
    };

    /// <summary>The group a new control would go into, given the current selection (group / control).</summary>
    private ModelItem ResolveGroup(NodeInfo node) => node?.Kind switch
    {
        NodeKind.Group => node.Item,
        NodeKind.Control => node.Item.Parent,
        _ => null,
    };

    // ---- Commands ---------------------------------------------------------------------

    private void OnAddTab()
    {
        ModelItem tab = DesignModel.AddTab(_ribbon);
        RebuildTree(tab);
    }

    private void OnAddGroup()
    {
        ModelItem tab = ResolveTab(Selected);
        if (tab != null)
        {
            ModelItem group = DesignModel.AddGroup(tab);
            RebuildTree(group);
        }
    }

    private void OnAddControlClick(object sender, RoutedEventArgs e)
    {
        if (_addControl.IsEnabled)
        {
            _addControlMenu.IsOpen = true;
        }
    }

    private void OnAddControl(string typeName, string label)
    {
        ModelItem group = ResolveGroup(Selected);
        if (group != null)
        {
            ModelItem control = DesignModel.AddControl(group, typeName, label);
            RebuildTree(control);
        }
    }

    private void OnMove(int delta)
    {
        NodeInfo node = Selected;
        if (node?.ParentCollection is null)
        {
            return;
        }

        DesignModel.Move(node.Item, node.ParentCollection, delta);
        RebuildTree(node.Item);
    }

    private void OnDelete()
    {
        NodeInfo node = Selected;
        if (node?.ParentCollection is null)
        {
            return;
        }

        ModelItem parent = node.Item.Parent;
        DesignModel.Delete(node.Item, node.ParentCollection);
        RebuildTree(parent);
    }

    private void OnRename()
    {
        NodeInfo node = Selected;
        if (node is null || node.Kind == NodeKind.Ribbon)
        {
            return;
        }

        DesignModel.Rename(node.Item, _headerBox.Text ?? string.Empty);
        RebuildTree(node.Item);
    }
}
