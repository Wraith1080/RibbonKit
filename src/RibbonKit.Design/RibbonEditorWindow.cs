using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>The level a tree node sits at, which decides which verbs apply.</summary>
internal enum NodeKind
{
    Ribbon,
    Tab,
    Group,

    /// <summary>A layout container inside a group (a <c>StackPanel</c> etc.) — has a <c>Children</c> collection we recurse into.</summary>
    Container,

    /// <summary>A leaf control (button/toggle/split/drop-down, combo, gallery, …).</summary>
    Control,
}

/// <summary>
/// Pairs a <see cref="ModelItem"/> with the <see cref="NodeKind"/> the tree shows it as, plus the
/// name of the collection it lives in on its parent. That collection is stored (not derived from
/// kind) because the same kind can sit in different collections: a control or nested container is in
/// a group's <c>Items</c> OR a parent container's <c>Children</c>, depending on where it was dropped.
/// </summary>
internal sealed class NodeInfo
{
    public NodeInfo(ModelItem item, NodeKind kind, string parentCollection)
    {
        Item = item;
        Kind = kind;
        ParentCollection = parentCollection;
    }

    public ModelItem Item { get; }

    public NodeKind Kind { get; }

    /// <summary>The collection property this node lives in on its parent (null for the ribbon root).</summary>
    public string ParentCollection { get; }
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
    private Button _addStack;
    private Button _addItem;
    private Button _moveUp;
    private Button _moveDown;
    private Button _delete;
    private Button _rename;
    private ComboBox _previewCombo;
    private CheckBox _backstageCheck;
    private bool _syncingPreview;
    private readonly StackPanel _propsPanel = new StackPanel { Orientation = Orientation.Vertical };
    private bool _syncingProps;

    /// <summary>Creates the editor over <paramref name="ribbon"/> (the selected Ribbon's design model item).</summary>
    public RibbonEditorWindow(ModelItem ribbon)
    {
        _ribbon = ribbon ?? throw new ArgumentNullException(nameof(ribbon));

        Title = "Ribbon Editor";
        Width = 780;
        Height = 580;
        MinWidth = 600;
        MinHeight = 420;
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
        catch (Exception ex)
        {
            DesignLog.Error("set VS owner", ex); // non-fatal: window is still shown, just unowned
        }

        DesignLog.Write("RibbonEditorWindow: building layout…");
        Content = BuildLayout();
        DesignLog.Write("RibbonEditorWindow: layout built; building tree…");
        RebuildTree();
        DesignLog.Write("RibbonEditorWindow: ready.");
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
        _addStack = MakeButton("Add Stack", (_, _) => OnAddStack());
        _addItem = MakeButton("Add Item", (_, _) => OnAddItem());
        _moveUp = MakeButton("Move Up", (_, _) => OnMove(-1));
        _moveDown = MakeButton("Move Down", (_, _) => OnMove(+1));
        _delete = MakeButton("Delete", (_, _) => OnDelete());

        _toolbar.Children.Add(addTab);
        _toolbar.Children.Add(_addGroup);
        _toolbar.Children.Add(_addControl);
        _toolbar.Children.Add(_addStack);
        _toolbar.Children.Add(_addItem);
        _toolbar.Children.Add(new Separator { Width = 1, Margin = new Thickness(4, 2, 4, 2) });
        _toolbar.Children.Add(_moveUp);
        _toolbar.Children.Add(_moveDown);
        _toolbar.Children.Add(_delete);

        // The "Add Control" type menu. Buttons carry a Header caption; combos/galleries/separators
        // don't (isButton = false), so no stray "ComboBox" label is written.
        _addControlMenu = new ContextMenu { PlacementTarget = _addControl, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        _addControlMenu.Items.Add(MakeControlMenuItem("Button", "RibbonButton", true));
        _addControlMenu.Items.Add(MakeControlMenuItem("Toggle Button", "RibbonToggleButton", true));
        _addControlMenu.Items.Add(MakeControlMenuItem("Split Button", "RibbonSplitButton", true));
        _addControlMenu.Items.Add(MakeControlMenuItem("Drop-Down Button", "RibbonDropDownButton", true));
        _addControlMenu.Items.Add(new Separator());
        _addControlMenu.Items.Add(MakeControlMenuItem("Combo Box", "RibbonComboBox", false));
        _addControlMenu.Items.Add(MakeControlMenuItem("Gallery (in-ribbon)", "InRibbonGallery", false));
        _addControlMenu.Items.Add(MakeControlMenuItem("Gallery (drop-down)", "RibbonGallery", false));
        _addControlMenu.Items.Add(MakeControlMenuItem("Separator", "Separator", false));
        _addControlMenu.Items.Add(MakeControlMenuItem("Text Block", "TextBlock", false));

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
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // title
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // type
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // properties
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // preview

        var title = new TextBlock
        {
            Text = "Selected item",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8),
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        var typeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        typeRow.Children.Add(new TextBlock { Text = "Type: ", VerticalAlignment = VerticalAlignment.Center });
        typeRow.Children.Add(_typeText);
        Grid.SetRow(typeRow, 1);
        grid.Children.Add(typeRow);

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
        headerRow.Children.Add(new TextBlock { Text = "Caption: ", VerticalAlignment = VerticalAlignment.Center });
        headerRow.Children.Add(_headerBox);
        _rename = MakeButton("Apply", (_, _) => OnRename());
        headerRow.Children.Add(_rename);
        Grid.SetRow(headerRow, 2);
        grid.Children.Add(headerRow);

        // Enter in the header box commits the rename.
        _headerBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                OnRename();
                e.Handled = true;
            }
        };

        // Dynamic per-item property editors (scrolls when there are many).
        var propsScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 12, 0, 0),
            Content = _propsPanel,
        };
        Grid.SetRow(propsScroll, 3);
        grid.Children.Add(propsScroll);

        // Design-only tab preview: shows a tab on the surface without writing to the XAML.
        var previewArea = new StackPanel { Orientation = Orientation.Vertical };
        previewArea.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 12) });

        var previewRow = new StackPanel { Orientation = Orientation.Horizontal };
        previewRow.Children.Add(new TextBlock { Text = "Preview tab: ", VerticalAlignment = VerticalAlignment.Center });
        _previewCombo = new ComboBox { MinWidth = 180 };
        _previewCombo.SelectionChanged += OnPreviewChanged;
        previewRow.Children.Add(_previewCombo);
        _backstageCheck = new CheckBox
        {
            Content = "Show backstage",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0),
        };
        _backstageCheck.Click += OnBackstageToggle;
        previewRow.Children.Add(_backstageCheck);
        previewArea.Children.Add(previewRow);

        previewArea.Children.Add(new TextBlock
        {
            Text = "Design-only: renders a tab (and optionally the backstage) on the surface without "
                 + "changing your XAML or the running app. Reset to “(no preview)” to clear the tab. "
                 + "Structure and property edits apply immediately (each is one Ctrl+Z).",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Thickness(0, 4, 0, 0),
        });
        Grid.SetRow(previewArea, 4);
        grid.Children.Add(previewArea);

        return grid;
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

    private MenuItem MakeControlMenuItem(string caption, string typeName, bool isButton)
    {
        var item = new MenuItem { Header = caption };
        item.Click += (_, _) => OnAddControl(typeName, caption, isButton);
        return item;
    }

    // ---- Tree building ----------------------------------------------------------------

    private void RebuildTree(ModelItem select = null)
    {
        _map.Clear();
        _tree.Items.Clear();

        var rootItem = MakeTreeItem(new NodeInfo(_ribbon, NodeKind.Ribbon, null), "Ribbon");
        rootItem.IsExpanded = true;
        _tree.Items.Add(rootItem);

        // Backstage (the File menu) is a scalar property of the ribbon, not part of Tabs — surface it
        // as its own node so its nav items can be edited. ParentCollection stays null (it's not in a
        // collection, so Move/Delete don't apply).
        ModelItem backstage = DesignModel.FindProperty(_ribbon, "Backstage")?.Value;
        if (backstage != null)
        {
            try
            {
                TreeViewItem backstageItem = MakeTreeItem(new NodeInfo(backstage, NodeKind.Control, null), "Backstage");
                backstageItem.IsExpanded = true;
                rootItem.Items.Add(backstageItem);
                foreach (ModelItem navItem in SafeChildren(backstage, "Items"))
                {
                    AddNode(backstageItem, navItem, "Items"); // nav items
                }
            }
            catch (Exception ex)
            {
                DesignLog.Error("build backstage node", ex);
            }
        }

        // Each node is read defensively: the complex ribbons have control types and properties
        // the starter one doesn't, and one bad read shouldn't abort the whole editor. Anything
        // that throws is logged (with the offending type) and skipped.
        IReadOnlyList<ModelItem> tabs = SafeChildren(_ribbon, "Tabs");
        DesignLog.Write("RebuildTree: " + tabs.Count + " tab(s).");

        foreach (ModelItem tab in tabs)
        {
            TreeViewItem tabItem;
            try
            {
                tabItem = MakeTreeItem(new NodeInfo(tab, NodeKind.Tab, "Tabs"), SafeHeader(tab));
                tabItem.IsExpanded = true;
                rootItem.Items.Add(tabItem);
            }
            catch (Exception ex)
            {
                DesignLog.Error("build tab node (" + SafeType(tab) + ")", ex);
                continue;
            }

            foreach (ModelItem group in SafeChildren(tab, "Groups"))
            {
                TreeViewItem groupItem;
                try
                {
                    groupItem = MakeTreeItem(new NodeInfo(group, NodeKind.Group, "Groups"), SafeHeader(group));
                    groupItem.IsExpanded = true;
                    tabItem.Items.Add(groupItem);
                }
                catch (Exception ex)
                {
                    DesignLog.Error("build group node (" + SafeType(group) + ")", ex);
                    continue;
                }

                // A group's items may be leaf controls, layout containers (StackPanels), item
                // containers (combos/galleries), or controls with rich Content — AddNode recurses
                // into whichever structure each child has.
                foreach (ModelItem child in SafeChildren(group, "Items"))
                {
                    AddNode(groupItem, child, "Items");
                }
            }
        }

        // Restore selection to the requested item (default: the ribbon root).
        ModelItem target = select ?? _ribbon;
        if (target != null && _map.TryGetValue(target, out TreeViewItem selected))
        {
            selected.IsSelected = true;
            selected.BringIntoView();
        }

        PopulatePreviewCombo();
        UpdateDetails();
    }

    /// <summary>
    /// Adds <paramref name="child"/> (which lives in its parent's <paramref name="parentCollection"/>,
    /// or null for a scalar <c>Content</c> element) as a tree node, then recurses into whatever
    /// structure it has: a Panel's <c>Children</c>, an item container's <c>Items</c> (combo/gallery),
    /// or a rich <c>Content</c> element (a gallery item's visual). One bad node is logged and skipped.
    /// </summary>
    private void AddNode(TreeViewItem parentTreeItem, ModelItem child, string parentCollection)
    {
        try
        {
            bool isPanel = DesignModel.HasProperty(child, "Children");
            NodeKind kind = isPanel ? NodeKind.Container : NodeKind.Control;
            TreeViewItem node = MakeTreeItem(new NodeInfo(child, kind, parentCollection), DisplayFor(child, isPanel));
            parentTreeItem.Items.Add(node);

            if (isPanel)
            {
                node.IsExpanded = true;
                foreach (ModelItem c in SafeChildren(child, "Children"))
                {
                    AddNode(node, c, "Children");
                }
            }
            else if (ItemRule(child) != null)
            {
                node.IsExpanded = true;
                foreach (ModelItem c in SafeChildren(child, "Items"))
                {
                    AddNode(node, c, "Items");
                }
            }

            // NOTE: we intentionally do NOT descend into a control's Content element — expanding every
            // backstage page / gallery item into its full visual tree was too noisy. Content that is a
            // plain string (a combo item's text, etc.) is shown/edited as the item's caption instead.
        }
        catch (Exception ex)
        {
            DesignLog.Error("build node (" + SafeType(child) + ")", ex);
        }
    }

    private static string DisplayFor(ModelItem item, bool isPanel)
    {
        string type = FriendlyType(SafeType(item));
        if (isPanel)
        {
            string orientation = DesignModel.GetString(item, "Orientation");
            return string.IsNullOrEmpty(orientation) ? type : type + " (" + orientation + ")";
        }

        string label = DesignModel.GetCaption(item);
        if (string.IsNullOrEmpty(label) && type == "Text Block")
        {
            label = DesignModel.GetString(item, "Text"); // show a TextBlock by its text
        }

        return string.IsNullOrEmpty(label) ? type : label + "  [" + type + "]";
    }

    // Defensive model reads — log the failure (with the item's type where possible) and carry on.

    private static IReadOnlyList<ModelItem> SafeChildren(ModelItem parent, string collectionProperty)
    {
        try
        {
            return DesignModel.Children(parent, collectionProperty);
        }
        catch (Exception ex)
        {
            DesignLog.Error("read " + collectionProperty + " of " + SafeType(parent), ex);
            return new List<ModelItem>();
        }
    }

    private static string SafeHeader(ModelItem item)
    {
        try
        {
            return DesignModel.Header(item);
        }
        catch (Exception ex)
        {
            DesignLog.Error("read Header", ex);
            return string.Empty;
        }
    }

    private static string SafeType(ModelItem item)
    {
        try
        {
            return DesignModel.TypeName(item);
        }
        catch (Exception ex)
        {
            DesignLog.Error("read ItemType", ex);
            return "?";
        }
    }

    /// <summary>Rebuilds the "Preview tab" list from the current tabs, keeping any active preview selected.</summary>
    private void PopulatePreviewCombo()
    {
        _syncingPreview = true;
        try
        {
            _previewCombo.Items.Clear();
            _previewCombo.Items.Add("(no preview)");

            IReadOnlyList<ModelItem> tabs = SafeChildren(_ribbon, "Tabs");
            for (int i = 0; i < tabs.Count; i++)
            {
                string header = SafeHeader(tabs[i]);
                _previewCombo.Items.Add("Tab " + (i + 1) + (string.IsNullOrEmpty(header) ? string.Empty : ": " + header));
            }

            int selected = 0;
            if (TabPreviewCoordinator.TryGetTab(_ribbon, out int idx) && idx >= 0 && idx < tabs.Count)
            {
                selected = idx + 1; // +1 for the "(no preview)" row at index 0
            }
            else if (TabPreviewCoordinator.CurrentIndex.HasValue)
            {
                // The previewed tab no longer exists (e.g. it was deleted) — clear the preview.
                TabPreviewCoordinator.SetTab(_ribbon, null);
            }

            _previewCombo.SelectedIndex = selected;

            // Backstage toggle: only meaningful when the ribbon actually has a backstage.
            bool hasBackstage = DesignModel.FindProperty(_ribbon, "Backstage")?.Value != null;
            _backstageCheck.IsEnabled = hasBackstage;
            _backstageCheck.IsChecked = hasBackstage
                && TabPreviewCoordinator.TryGetBackstage(_ribbon, out bool open) && open;
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
        TabPreviewCoordinator.SetTab(_ribbon, sel <= 0 ? (int?)null : sel - 1);
    }

    private void OnBackstageToggle(object sender, RoutedEventArgs e)
    {
        if (!_syncingPreview)
        {
            TabPreviewCoordinator.SetBackstage(_ribbon, _backstageCheck.IsChecked == true);
        }
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
        "RibbonComboBox" => "Combo Box",
        "InRibbonGallery" => "Gallery (in-ribbon)",
        "RibbonGallery" => "Gallery",
        "Separator" => "Separator",
        "StackPanel" => "Stack Panel",
        "RibbonGalleryItem" => "Gallery Item",
        "ComboBoxItem" => "Combo Item",
        "BackstageTabItem" => "Backstage Page",
        "TextBlock" => "Text Block",
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
            string type = SafeType(node.Item);
            _typeText.Text = node.Kind == NodeKind.Control || node.Kind == NodeKind.Container
                ? FriendlyType(type) + " (" + type + ")"
                : node.Kind.ToString();
            _headerBox.Text = node.Kind == NodeKind.Ribbon ? string.Empty : DesignModel.GetCaption(node.Item);
        }

        // Anything with a parent collection (tab / group / container / control / item) can be moved/deleted.
        bool structural = node != null && node.ParentCollection != null;
        // Caption edits work for Header controls AND Content items (combo/gallery items).
        bool renameable = node != null && node.Kind != NodeKind.Ribbon && DesignModel.HasCaption(node.Item);

        _addGroup.IsEnabled = ResolveTab(node) != null;
        bool canAddChild = ResolveChildTarget(node) != null;
        _addControl.IsEnabled = canAddChild;
        _addStack.IsEnabled = canAddChild;
        _addItem.IsEnabled = ResolveItemTarget(node) != null; // combo/gallery/backstage entries

        _rename.IsEnabled = renameable;
        _headerBox.IsEnabled = renameable;
        _delete.IsEnabled = structural;

        // Move enabled only when there is somewhere to move to.
        int index = -1;
        int count = 0;
        if (structural)
        {
            try
            {
                index = DesignModel.IndexInParent(node.Item, node.ParentCollection);
                count = DesignModel.SiblingCount(node.Item, node.ParentCollection);
            }
            catch (Exception ex)
            {
                DesignLog.Error("index/count of " + SafeType(node.Item), ex);
            }
        }

        _moveUp.IsEnabled = structural && index > 0;
        _moveDown.IsEnabled = structural && index >= 0 && index < count - 1;

        BuildProps(node);
    }

    // ---- Per-item property editors ----------------------------------------------------

    private enum EditorKind
    {
        Text,
        Bool,
        Enum,
        IconRef,
    }

    private sealed class PropSpec
    {
        public PropSpec(string name, string label, EditorKind kind, string[] enumValues = null)
        {
            Name = name;
            Label = label;
            Kind = kind;
            EnumValues = enumValues;
        }

        public string Name { get; }

        public string Label { get; }

        public EditorKind Kind { get; }

        public string[] EnumValues { get; }
    }

    private static readonly PropSpec[] ControlSpecs =
    {
        new PropSpec("Size", "Size", EditorKind.Enum, new[] { "Large", "Medium", "Small" }),
        new PropSpec("Icon", "Icon (resource key)", EditorKind.IconRef),
        new PropSpec("LargeIcon", "Large icon (resource key)", EditorKind.IconRef),
        new PropSpec("SizeDefinition", "Size definition", EditorKind.Text),
        new PropSpec("ScreenTipTitle", "ScreenTip title", EditorKind.Text),
        new PropSpec("ScreenTipText", "ScreenTip text", EditorKind.Text),
    };

    private static readonly PropSpec[] TabSpecs =
    {
        new PropSpec("IsContextual", "Contextual tab", EditorKind.Bool),
        new PropSpec("ContextualColor", "Contextual color", EditorKind.Text),
    };

    private static readonly PropSpec[] GroupSpecs =
    {
        new PropSpec("ShowDialogLauncher", "Show dialog launcher", EditorKind.Bool),
        new PropSpec("ReductionMode", "Reduction mode", EditorKind.Enum, new[] { "Collapse", "ResizeThenCollapse", "Resize" }),
        new PropSpec("CanResize", "Can resize", EditorKind.Bool),
    };

    private static readonly PropSpec[] ContainerSpecs =
    {
        new PropSpec("Orientation", "Orientation", EditorKind.Enum, new[] { "Horizontal", "Vertical" }),
    };

    // Type-specific editors, keyed by simple type name. Shown ahead of the kind-based specs.
    private static readonly PropSpec[] BackstageItemSpecs =
    {
        new PropSpec("IsButton", "Is button (action)", EditorKind.Bool),
        new PropSpec("Placement", "Placement", EditorKind.Enum, new[] { "Top", "Bottom" }),
    };

    private static readonly PropSpec[] ComboSpecs =
    {
        new PropSpec("InputWidth", "Input width", EditorKind.Text),
        new PropSpec("IsEditable", "Editable", EditorKind.Bool),
    };

    // For editing a gallery item's content visually (its TextBlocks): text and basic appearance.
    private static readonly PropSpec[] TextBlockSpecs =
    {
        new PropSpec("Text", "Text", EditorKind.Text),
        new PropSpec("FontSize", "Font size", EditorKind.Text),
        new PropSpec("FontWeight", "Font weight", EditorKind.Enum, new[] { "Normal", "Light", "SemiBold", "Bold" }),
        new PropSpec("FontStyle", "Font style", EditorKind.Enum, new[] { "Normal", "Italic" }),
        new PropSpec("Foreground", "Foreground", EditorKind.Text),
    };

    private static PropSpec[] SpecsFor(NodeKind kind) => kind switch
    {
        NodeKind.Control => ControlSpecs,
        NodeKind.Tab => TabSpecs,
        NodeKind.Group => GroupSpecs,
        NodeKind.Container => ContainerSpecs,
        _ => System.Array.Empty<PropSpec>(),
    };

    private static PropSpec[] TypeSpecs(string typeName) => typeName switch
    {
        "BackstageTabItem" => BackstageItemSpecs,
        "RibbonComboBox" => ComboSpecs,
        "TextBlock" => TextBlockSpecs,
        _ => System.Array.Empty<PropSpec>(),
    };

    /// <summary>Type-specific editors first, then the kind's editors, de-duplicated by name.</summary>
    private List<PropSpec> SpecsForNode(NodeInfo node)
    {
        var result = new List<PropSpec>();
        var seen = new HashSet<string>();

        void AddAll(PropSpec[] specs)
        {
            foreach (PropSpec spec in specs)
            {
                if (seen.Add(spec.Name))
                {
                    result.Add(spec);
                }
            }
        }

        AddAll(TypeSpecs(SafeType(node.Item)));
        AddAll(SpecsFor(node.Kind));
        return result;
    }

    /// <summary>Rebuilds the property editors for the selected item, skipping any property it doesn't have.</summary>
    private void BuildProps(NodeInfo node)
    {
        _syncingProps = true;
        try
        {
            _propsPanel.Children.Clear();

            if (node is null || node.Kind == NodeKind.Ribbon)
            {
                return;
            }

            bool any = false;
            foreach (PropSpec spec in SpecsForNode(node))
            {
                if (!DesignModel.HasProperty(node.Item, spec.Name))
                {
                    continue;
                }

                _propsPanel.Children.Add(BuildPropRow(node.Item, spec));
                any = true;
            }

            if (!any)
            {
                _propsPanel.Children.Add(new TextBlock { Text = "No editable properties.", Opacity = 0.6 });
            }
        }
        finally
        {
            _syncingProps = false;
        }
    }

    private UIElement BuildPropRow(ModelItem item, PropSpec spec)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock { Text = spec.Label, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(label, 0);
        row.Children.Add(label);

        UIElement editor = spec.Kind switch
        {
            EditorKind.Bool => BuildBoolEditor(item, spec),
            EditorKind.Enum => BuildEnumEditor(item, spec),
            EditorKind.IconRef => BuildIconEditor(item, spec),
            _ => BuildTextEditor(item, spec),
        };
        Grid.SetColumn(editor, 1);
        row.Children.Add(editor);

        return row;
    }

    private UIElement BuildTextEditor(ModelItem item, PropSpec spec)
    {
        var box = new TextBox
        {
            Text = DesignModel.GetString(item, spec.Name),
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        void Commit()
        {
            if (!_syncingProps)
            {
                DesignModel.SetProperty(item, spec.Name, box.Text ?? string.Empty);
            }
        }

        box.LostFocus += (_, _) => Commit();
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Commit();
                e.Handled = true;
            }
        };
        return box;
    }

    private UIElement BuildBoolEditor(ModelItem item, PropSpec spec)
    {
        var check = new CheckBox
        {
            IsChecked = DesignModel.GetBool(item, spec.Name),
            VerticalAlignment = VerticalAlignment.Center,
        };
        // Click fires only on user interaction (not on the initial IsChecked set above).
        check.Click += (_, _) => DesignModel.SetProperty(item, spec.Name, check.IsChecked == true);
        return check;
    }

    // Icon editor: a "…" button opens the visual picker (icons used in this ribbon, plus the full
    // Icons.xaml catalog once loaded); the text field shows/accepts the resource key directly. Both
    // write via the proven DesignModel.SetStaticResource (a {StaticResource key} model reference).
    private UIElement BuildIconEditor(ModelItem item, PropSpec spec)
    {
        var dock = new DockPanel { LastChildFill = true };

        var set = new Button { Content = "Set", Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(10, 3, 10, 3) };
        DockPanel.SetDock(set, Dock.Right);
        dock.Children.Add(set);

        var browse = new Button { Content = "…", Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(10, 3, 10, 3) };
        DockPanel.SetDock(browse, Dock.Right);
        dock.Children.Add(browse);

        var box = new TextBox
        {
            Text = DesignModel.GetStaticResourceKey(item, spec.Name),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        dock.Children.Add(box);

        void Apply()
        {
            if (!_syncingProps)
            {
                DesignModel.SetStaticResource(item, spec.Name, box.Text);
            }
        }

        set.Click += (_, _) => Apply();
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Apply();
                e.Handled = true;
            }
        };

        browse.Click += (_, _) =>
        {
            var picker = new IconPickerDialog(CollectUsedIconKeys(), box.Text) { Owner = this };
            if (picker.ShowDialog() == true && !string.IsNullOrEmpty(picker.SelectedKey))
            {
                box.Text = picker.SelectedKey;
                DesignModel.SetStaticResource(item, spec.Name, picker.SelectedKey);
            }
        };
        return dock;
    }

    /// <summary>Every distinct icon resource key already used on a control/group in this ribbon (for the picker's default list).</summary>
    private List<string> CollectUsedIconKeys()
    {
        var keys = new List<string>();

        void Collect(ModelItem owner, string prop)
        {
            string key = DesignModel.GetStaticResourceKey(owner, prop);
            if (!string.IsNullOrEmpty(key))
            {
                keys.Add(key);
            }
        }

        void Walk(ModelItem parent, string collection)
        {
            foreach (ModelItem child in DesignModel.Children(parent, collection))
            {
                Collect(child, "Icon");
                Collect(child, "LargeIcon");
                if (DesignModel.HasProperty(child, "Children"))
                {
                    Walk(child, "Children"); // descend into stack panels etc.
                }
            }
        }

        foreach (ModelItem tab in DesignModel.Children(_ribbon, "Tabs"))
        {
            foreach (ModelItem group in DesignModel.Children(tab, "Groups"))
            {
                Collect(group, "Icon");
                Walk(group, "Items");
            }
        }

        return keys;
    }

    private UIElement BuildEnumEditor(ModelItem item, PropSpec spec)
    {
        var combo = new ComboBox();
        foreach (string value in spec.EnumValues)
        {
            combo.Items.Add(value);
        }

        string current = DesignModel.GetString(item, spec.Name);
        combo.SelectedItem = combo.Items.Contains(current) ? current : null;

        combo.SelectionChanged += (_, _) =>
        {
            if (!_syncingProps && combo.SelectedItem is string chosen)
            {
                DesignModel.SetProperty(item, spec.Name, chosen);
            }
        };
        return combo;
    }

    /// <summary>The nearest <c>RibbonTab</c> ancestor of the selection (walks up through any nesting), or null.</summary>
    private ModelItem ResolveTab(NodeInfo node)
    {
        ModelItem current = node?.Item;
        while (current != null)
        {
            if (SafeType(current) == "RibbonTab")
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Where a new control or container should be added, given the selection: into a group's
    /// <c>Items</c>, into a container's <c>Children</c>, or as a sibling of the selected control.
    /// Null when the selection can't host a child (ribbon/tab).
    /// </summary>
    private (ModelItem Parent, string Collection)? ResolveChildTarget(NodeInfo node)
    {
        switch (node?.Kind)
        {
            case NodeKind.Group:
                return (node.Item, "Items");
            case NodeKind.Container:
                return (node.Item, "Children");
            case NodeKind.Control:
                return node.Item.Parent is ModelItem parent ? (parent, node.ParentCollection) : default((ModelItem, string)?);
            default:
                return null;
        }
    }

    /// <summary>Where an "Add Item" would go, and what child type/caption it uses.</summary>
    private struct ItemTarget
    {
        public ModelItem Container;
        public string TypeName;
        public string CaptionProperty;
        public string Label;
    }

    /// <summary>The item-add rule for an item container by type name, or null if it isn't one.</summary>
    private static ItemTarget? ItemRule(ModelItem container)
    {
        switch (SafeType(container))
        {
            case "RibbonComboBox":
                return new ItemTarget { Container = container, TypeName = "ComboBoxItem", CaptionProperty = "Content", Label = "Item" };
            case "RibbonGallery":
            case "InRibbonGallery":
                return new ItemTarget { Container = container, TypeName = "RibbonGalleryItem", CaptionProperty = "Content", Label = "Item" };
            case "Backstage":
                return new ItemTarget { Container = container, TypeName = "BackstageTabItem", CaptionProperty = "Header", Label = "Page" };
            default:
                return null;
        }
    }

    /// <summary>
    /// Resolves an "Add Item" target from the selection: the combo/gallery/backstage itself, or the
    /// container of the selected item (so you can add a sibling). Null when neither applies.
    /// </summary>
    private ItemTarget? ResolveItemTarget(NodeInfo node)
    {
        if (node?.Item is null)
        {
            return null;
        }

        ItemTarget? here = ItemRule(node.Item);
        if (here != null)
        {
            return here;
        }

        return node.Item.Parent != null ? ItemRule(node.Item.Parent) : null;
    }

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

    private void OnAddControl(string typeName, string caption, bool isButton)
    {
        (ModelItem Parent, string Collection)? target = ResolveChildTarget(Selected);
        if (target != null)
        {
            // Only buttons get a Header caption; buttons stacked inside a container default to
            // Small (the icon-row form). Combos/galleries/separators get neither.
            string header = isButton ? caption : null;
            string size = isButton && target.Value.Collection == "Children" ? "Small" : null;
            ModelItem control = DesignModel.AddControl(target.Value.Parent, target.Value.Collection, typeName, header, size);
            if (control != null)
            {
                RebuildTree(control);
            }
        }
    }

    private void OnAddStack()
    {
        (ModelItem Parent, string Collection)? target = ResolveChildTarget(Selected);
        if (target != null)
        {
            // A stack in a group is the outer vertical column; a stack inside another stack is a
            // horizontal row (matching the Office pattern of rows-of-icons within a column).
            string orientation = target.Value.Collection == "Children" ? "Horizontal" : "Vertical";
            ModelItem stack = DesignModel.AddStackPanel(target.Value.Parent, target.Value.Collection, orientation);
            if (stack != null)
            {
                RebuildTree(stack);
            }
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

        DesignModel.SetCaption(node.Item, _headerBox.Text ?? string.Empty);
        RebuildTree(node.Item);
    }

    private void OnAddItem()
    {
        ItemTarget? target = ResolveItemTarget(Selected);
        if (target != null)
        {
            ModelItem item = DesignModel.AddItem(target.Value.Container, target.Value.TypeName, target.Value.CaptionProperty, target.Value.Label);
            if (item != null)
            {
                RebuildTree(item);
            }
        }
    }
}
