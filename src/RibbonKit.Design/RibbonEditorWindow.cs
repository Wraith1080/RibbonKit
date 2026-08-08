using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.DesignTools.Extensibility.Model;

namespace RibbonKit.Design;

/// <summary>The level a tree node sits at, which decides which verbs apply.</summary>
internal enum NodeKind
{
    Ribbon,
    ApplicationMenu,
    ApplicationMenuSection,
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
    public NodeInfo(ModelItem item, NodeKind kind, string? parentCollection, string? parentProperty = null)
    {
        Item = item;
        Kind = kind;
        ParentCollection = parentCollection;
        ParentProperty = parentProperty;
    }

    public ModelItem Item { get; }

    public NodeKind Kind { get; }

    /// <summary>The collection property this node lives in on its parent (null for the ribbon root).</summary>
    public string? ParentCollection { get; }

    /// <summary>
    /// The scalar property this node occupies on its parent (ApplicationMenu, DefaultContent,
    /// Content, or FooterContent). Scalar nodes can be deleted but not reordered.
    /// </summary>
    public string? ParentProperty { get; }
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
    private readonly TextBox _headerBox = new TextBox { MinWidth = 80, VerticalContentAlignment = VerticalAlignment.Center };
    private readonly TextBlock _typeText = new TextBlock { Opacity = 0.75, VerticalAlignment = VerticalAlignment.Center };
    private readonly Dictionary<ModelItem, TreeViewItem> _map = new Dictionary<ModelItem, TreeViewItem>();

    // Built by BuildToolbar/BuildBody/BuildFooter, every one of which runs from the constructor
    // before it returns, so none of these is ever observed null. `= null!` states that. Declaring
    // them nullable instead would put a `?.` on every use site to describe a state that cannot
    // happen, which reads as "this might not exist" and is simply untrue.
    private Button _add = null!;
    private Button _moveUp = null!;
    private Button _moveDown = null!;
    private Button _delete = null!;
    private Button _rename = null!;
    private ComboBox _themeCombo = null!;
    private ComboBox _previewCombo = null!;
    private ComboBox _fileSurfaceCombo = null!;
    private ComboBox _filePageCombo = null!;
    private readonly List<FileSurfacePreview> _fileSurfaceMap = new List<FileSurfacePreview>();
    private readonly List<ThemePreview> _themeMap = new List<ThemePreview>();
    private readonly List<int> _filePageMap = new List<int>();
    private bool _syncingPreview;
    private readonly StackPanel _propsPanel = new StackPanel { Orientation = Orientation.Vertical };
    private bool _syncingProps;

    /// <summary>The node whose editors are currently shown, so a commit can rebuild them in place.</summary>
    private NodeInfo? _propsNode;

    // Drag-drop reorder state.
    private Point _dragStart;
    // Genuinely transient, unlike the widgets above: null between drags, and cleared back to null
    // when a drag is consumed or the adorner removed.
    private NodeInfo? _dragSource;
    private DropAdorner? _dropAdorner;

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
        SnapsToDevicePixels = true;

        // WPF measures in device-independent units, so the responsive grids below naturally
        // reflow when Visual Studio moves between monitors. Force one deferred layout pass after
        // a live DPI transition as well: the designer window lives in the VS process and can remain
        // open while that process receives WM_DPICHANGED.
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                InvalidateMeasure();
                InvalidateArrange();
                UpdateLayout();
            }));

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

        _add = MakeButton("Add ▾", OnAddClick);
        _moveUp = MakeButton("Move Up", (_, _) => OnMove(-1));
        _moveDown = MakeButton("Move Down", (_, _) => OnMove(+1));
        _delete = MakeButton("Delete", (_, _) => OnDelete());

        _toolbar.Children.Add(_add);
        _toolbar.Children.Add(new Separator { Width = 1, Margin = new Thickness(4, 2, 4, 2) });
        _toolbar.Children.Add(_moveUp);
        _toolbar.Children.Add(_moveDown);
        _toolbar.Children.Add(_delete);

        return _toolbar;
    }

    private WrapPanel _toolbar = null!;
    private ContextMenu _addMenu = null!;

    private UIElement BuildBody()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 220 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star), MinWidth = 280 });

        _tree.SelectedItemChanged += (_, _) => UpdateDetails();
        _tree.AllowDrop = true;
        _tree.PreviewMouseLeftButtonDown += OnTreeMouseDown;
        _tree.PreviewMouseMove += OnTreeMouseMove;
        _tree.DragOver += OnTreeDragOver;
        _tree.Drop += OnTreeDrop;
        _tree.DragLeave += (_, _) => ClearDropAdorner();
        Grid.SetColumn(_tree, 0);
        grid.Children.Add(_tree);

        var splitter = new GridSplitter
        {
            Width = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ResizeDirection = GridResizeDirection.Columns,
            Background = Brushes.Transparent,
        };
        Grid.SetColumn(splitter, 1);
        grid.Children.Add(splitter);

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
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // inspector tabs

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

        var headerRow = new Grid();
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var captionLabel = new TextBlock { Text = "Caption: ", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(captionLabel, 0);
        headerRow.Children.Add(captionLabel);
        Grid.SetColumn(_headerBox, 1);
        headerRow.Children.Add(_headerBox);
        _rename = MakeButton("Apply", (_, _) => OnRename());
        _rename.Margin = new Thickness(6, 0, 0, 0);
        Grid.SetColumn(_rename, 2);
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

        var inspector = new TabControl { Margin = new Thickness(0, 12, 0, 0) };

        // Dynamic per-item property editors (scrolls when there are many).
        var propsScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(8),
            Content = _propsPanel,
        };
        inspector.Items.Add(new TabItem { Header = "Properties", Content = propsScroll });
        inspector.Items.Add(new TabItem { Header = "Design Preview", Content = BuildPreviewPanel() });
        Grid.SetRow(inspector, 3);
        grid.Children.Add(inspector);

        return grid;
    }

    private UIElement BuildPreviewPanel()
    {
        var panel = new StackPanel { Margin = new Thickness(10) };

        _themeCombo = new ComboBox { MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Stretch };
        _themeCombo.SelectionChanged += OnThemePreviewChanged;
        panel.Children.Add(BuildPreviewRow("Theme", _themeCombo));

        _previewCombo = new ComboBox { MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Stretch };
        _previewCombo.SelectionChanged += OnPreviewChanged;
        panel.Children.Add(BuildPreviewRow("Active tab", _previewCombo));

        _fileSurfaceCombo = new ComboBox { MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Stretch };
        _fileSurfaceCombo.SelectionChanged += OnFileSurfaceChanged;
        panel.Children.Add(BuildPreviewRow("File surface", _fileSurfaceCombo));

        _filePageCombo = new ComboBox { MinWidth = 100, HorizontalAlignment = HorizontalAlignment.Stretch };
        _filePageCombo.SelectionChanged += OnFilePageChanged;
        panel.Children.Add(BuildPreviewRow("Page / pane", _filePageCombo));

        panel.Children.Add(new TextBlock
        {
            Text = "Design-only preview state is kept in this designer session. It does not change "
                 + "the XAML or the running application. Theme changes palette and metrics only; "
                 + "authored choices such as the application-button shape stay unchanged. "
                 + "Structure and property edits still apply "
                 + "immediately, one Ctrl+Z step at a time.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
            Margin = new Thickness(0, 8, 0, 0),
        });

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = panel,
        };
    }

    private static UIElement BuildPreviewRow(string label, UIElement editor)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var text = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(text, 0);
        row.Children.Add(text);
        Grid.SetColumn(editor, 1);
        row.Children.Add(editor);
        return row;
    }

    private UIElement BuildFooter()
    {
        var footer = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hint = new TextBlock
        {
            Text = "Edits apply immediately; Ctrl+Z undoes one action.",
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

    private static MenuItem MakeMenuItem(string caption, Action action)
    {
        var item = new MenuItem { Header = caption };
        item.Click += (_, _) => action();
        return item;
    }

    // ---- Tree building ----------------------------------------------------------------

    private void RebuildTree(ModelItem? select = null)
    {
        _map.Clear();
        _tree.Items.Clear();

        var rootItem = MakeTreeItem(new NodeInfo(_ribbon, NodeKind.Ribbon, null), "Ribbon");
        rootItem.IsExpanded = true;
        _tree.Items.Add(rootItem);

        // Backstage (the File menu) is a scalar property of the ribbon, not part of Tabs — surface it
        // as its own node so its nav items can be edited. ParentCollection stays null (it's not in a
        // collection, so Move/Delete don't apply).
        ModelItem? backstage = DesignModel.FindProperty(_ribbon, "Backstage")?.Value;
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

        ModelItem? applicationMenu = DesignModel.FindProperty(_ribbon, "ApplicationMenu")?.Value;
        if (applicationMenu != null)
        {
            try
            {
                AddApplicationMenuTree(rootItem, applicationMenu);
            }
            catch (Exception ex)
            {
                DesignLog.Error("build application-menu node", ex);
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

    private void AddApplicationMenuTree(TreeViewItem ribbonItem, ModelItem menu)
    {
        var menuNode = MakeTreeItem(
            new NodeInfo(menu, NodeKind.ApplicationMenu, null, "ApplicationMenu"),
            "Application Menu");
        menuNode.IsExpanded = true;
        ribbonItem.Items.Add(menuNode);

        AddApplicationMenuSection(menuNode, menu, "DefaultContent", "Default pane", "RibbonApplicationMenuPaneItem");

        foreach (ModelItem item in SafeChildren(menu, "Items"))
        {
            bool isCommand = SafeType(item) == "RibbonApplicationMenuItem";
            TreeViewItem itemNode = MakeTreeItem(
                new NodeInfo(item, NodeKind.Control, "Items"),
                DisplayFor(item, isPanel: false));
            menuNode.Items.Add(itemNode);

            if (isCommand)
            {
                AddApplicationMenuSection(itemNode, item, "Content", "Pane", "RibbonApplicationMenuPaneItem");
            }
        }

        AddApplicationMenuSection(menuNode, menu, "FooterContent", "Footer", "RibbonApplicationMenuButton");
    }

    private void AddApplicationMenuSection(
        TreeViewItem parent,
        ModelItem owner,
        string propertyName,
        string label,
        string managedChildType)
    {
        ModelItem? content = DesignModel.FindProperty(owner, propertyName)?.Value;
        if (content is null)
        {
            return;
        }

        IReadOnlyList<ModelItem> children = SafeChildren(content, "Children");
        bool isManagedStack = SafeType(content) == "StackPanel";
        foreach (ModelItem child in children)
        {
            if (SafeType(child) != managedChildType)
            {
                isManagedStack = false;
                break;
            }
        }

        var section = MakeTreeItem(
            new NodeInfo(content, NodeKind.ApplicationMenuSection, null, propertyName),
            isManagedStack ? label : label + " (custom content — edit in XAML)");
        section.IsExpanded = isManagedStack;
        parent.Items.Add(section);

        if (!isManagedStack)
        {
            return;
        }

        foreach (ModelItem child in children)
        {
            AddNode(section, child, "Children");
        }
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
            PopulateThemePreview();

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

            // File surfaces are mutually exclusive at runtime. Present them as one choice instead
            // of independent checkboxes, and list only surfaces that actually exist in the model.
            ModelItem? backstage = DesignModel.FindProperty(_ribbon, "Backstage")?.Value;
            ModelItem? applicationMenu = DesignModel.FindProperty(_ribbon, "ApplicationMenu")?.Value;
            bool hasBackstage = backstage != null;
            bool hasApplicationMenu = applicationMenu != null;
            PopulateFileSurfaces(hasBackstage, hasApplicationMenu);

            FileSurfacePreview surface = TabPreviewCoordinator.TryGetFileSurface(_ribbon, out FileSurfacePreview currentSurface)
                ? currentSurface
                : FileSurfacePreview.Closed;
            if ((surface == FileSurfacePreview.Backstage && !hasBackstage)
                || (surface == FileSurfacePreview.ApplicationMenu && !hasApplicationMenu))
            {
                surface = FileSurfacePreview.Closed;
                TabPreviewCoordinator.SetFileSurface(_ribbon, surface);
            }

            int surfaceIndex = _fileSurfaceMap.IndexOf(surface);
            _fileSurfaceCombo.SelectedIndex = surfaceIndex < 0 ? 0 : surfaceIndex;
            PopulateFilePages(surface, backstage, applicationMenu);
        }
        finally
        {
            _syncingPreview = false;
        }
    }

    private void PopulateThemePreview()
    {
        _themeCombo.Items.Clear();
        _themeMap.Clear();
        AddThemePreview("(project default)", ThemePreview.ProjectDefault);
        AddThemePreview("Office 2024", ThemePreview.Office2024);
        AddThemePreview("Office 2019", ThemePreview.Office2019);
        AddThemePreview("Office 2013", ThemePreview.Office2013);
        AddThemePreview("Office 2010", ThemePreview.Office2010);
        AddThemePreview("Office 2007", ThemePreview.Office2007);

        ThemePreview theme = TabPreviewCoordinator.TryGetTheme(_ribbon, out ThemePreview current)
            ? current
            : ThemePreview.ProjectDefault;
        int selected = _themeMap.IndexOf(theme);
        _themeCombo.SelectedIndex = selected < 0 ? 0 : selected;
    }

    private void AddThemePreview(string label, ThemePreview theme)
    {
        _themeCombo.Items.Add(label);
        _themeMap.Add(theme);
    }

    /// <summary>
    /// Rebuilds the File-surface list from the two optional scalar Ribbon properties.
    /// </summary>
    private void PopulateFileSurfaces(bool hasBackstage, bool hasApplicationMenu)
    {
        _fileSurfaceCombo.Items.Clear();
        _fileSurfaceMap.Clear();
        AddFileSurface("Closed", FileSurfacePreview.Closed);
        if (hasBackstage)
        {
            AddFileSurface("Backstage", FileSurfacePreview.Backstage);
        }
        if (hasApplicationMenu)
        {
            AddFileSurface("Application menu", FileSurfacePreview.ApplicationMenu);
        }
    }

    private void AddFileSurface(string label, FileSurfacePreview surface)
    {
        _fileSurfaceCombo.Items.Add(label);
        _fileSurfaceMap.Add(surface);
    }

    /// <summary>
    /// Populates the dependent selector with backstage pages or application-menu panes. Entry zero
    /// is always the surface's default page/pane and clears any item-specific override.
    /// </summary>
    private void PopulateFilePages(FileSurfacePreview surface, ModelItem? backstage, ModelItem? applicationMenu)
    {
        _filePageCombo.Items.Clear();
        _filePageMap.Clear();
        _filePageCombo.Items.Add("(default)");
        _filePageCombo.IsEnabled = surface != FileSurfacePreview.Closed;

        if (surface == FileSurfacePreview.Backstage && backstage != null)
        {
            PopulateBackstagePages(backstage);
            return;
        }

        if (surface == FileSurfacePreview.ApplicationMenu && applicationMenu != null)
        {
            PopulateApplicationMenuPanes(applicationMenu);
            return;
        }

        _filePageCombo.SelectedIndex = 0;
    }

    private void PopulateBackstagePages(ModelItem backstage)
    {
        IReadOnlyList<ModelItem> items = SafeChildren(backstage, "Items");
        int selected = 0;
        int? current = TabPreviewCoordinator.TryGetBackstagePage(backstage, out int idx) ? idx : (int?)null;

        for (int i = 0; i < items.Count; i++)
        {
            if (DesignModel.GetBool(items[i], "IsButton"))
            {
                continue; // footer action button, not a page
            }

            string header = SafeHeader(items[i]);
            _filePageCombo.Items.Add(string.IsNullOrEmpty(header) ? "Page " + (i + 1) : header);
            _filePageMap.Add(i);
            if (current == i)
            {
                selected = _filePageMap.Count; // +1 for the "(default)" row at index 0
            }
        }

        _filePageCombo.SelectedIndex = selected;
    }

    private void PopulateApplicationMenuPanes(ModelItem applicationMenu)
    {
        IReadOnlyList<ModelItem> items = SafeChildren(applicationMenu, "Items");
        int selected = 0;
        int? current = ApplicationMenuPreviewCoordinator.CurrentIndexFor(applicationMenu);
        bool foundCurrent = !current.HasValue;
        for (int i = 0; i < items.Count; i++)
        {
            ModelItem item = items[i];
            if (SafeType(item) != "RibbonApplicationMenuItem" || DesignModel.ContentElement(item) is null)
            {
                continue;
            }

            string header = SafeHeader(item);
            _filePageCombo.Items.Add(string.IsNullOrEmpty(header) ? "Pane " + (i + 1) : header);
            _filePageMap.Add(i);
            if (current == i)
            {
                selected = _filePageMap.Count;
                foundCurrent = true;
            }
        }

        if (!foundCurrent)
        {
            ApplicationMenuPreviewCoordinator.SetActiveIndex(applicationMenu, null);
        }

        _filePageCombo.SelectedIndex = selected;
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

    private void OnThemePreviewChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPreview)
        {
            return;
        }

        int selected = _themeCombo.SelectedIndex;
        ThemePreview theme = selected >= 0 && selected < _themeMap.Count
            ? _themeMap[selected]
            : ThemePreview.ProjectDefault;
        TabPreviewCoordinator.SetTheme(_ribbon, theme);
    }

    private void OnFileSurfaceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPreview)
        {
            return;
        }

        int selected = _fileSurfaceCombo.SelectedIndex;
        FileSurfacePreview surface = selected >= 0 && selected < _fileSurfaceMap.Count
            ? _fileSurfaceMap[selected]
            : FileSurfacePreview.Closed;
        // Capture authored objects before invalidating any design-only preview values. Besides
        // avoiding redundant Model API calls, this keeps the event handler safe if a future VS
        // designer version defers an invalidation until the next property read.
        ModelItem? backstage = DesignModel.FindProperty(_ribbon, "Backstage")?.Value;
        ModelItem? applicationMenu = DesignModel.FindProperty(_ribbon, "ApplicationMenu")?.Value;
        TabPreviewCoordinator.SetFileSurface(_ribbon, surface);
        if (surface != FileSurfacePreview.ApplicationMenu)
        {
            ApplicationMenuPreviewCoordinator.SetActiveIndex(applicationMenu, null);
        }

        _syncingPreview = true;
        try
        {
            PopulateFilePages(
                surface,
                backstage,
                applicationMenu);
        }
        finally
        {
            _syncingPreview = false;
        }
    }

    private void OnFilePageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPreview)
        {
            return;
        }

        FileSurfacePreview surface = _fileSurfaceCombo.SelectedIndex >= 0
            && _fileSurfaceCombo.SelectedIndex < _fileSurfaceMap.Count
                ? _fileSurfaceMap[_fileSurfaceCombo.SelectedIndex]
                : FileSurfacePreview.Closed;
        ModelItem? backstage = DesignModel.FindProperty(_ribbon, "Backstage")?.Value;
        ModelItem? applicationMenu = DesignModel.FindProperty(_ribbon, "ApplicationMenu")?.Value;
        int sel = _filePageCombo.SelectedIndex;
        int? itemIndex = sel <= 0 || sel - 1 >= _filePageMap.Count
            ? (int?)null
            : _filePageMap[sel - 1];

        if (surface == FileSurfacePreview.Backstage)
        {
            TabPreviewCoordinator.SetBackstagePage(backstage, itemIndex);
            ApplicationMenuPreviewCoordinator.SetActiveIndex(applicationMenu, null);
        }
        else if (surface == FileSurfacePreview.ApplicationMenu)
        {
            ApplicationMenuPreviewCoordinator.SetActiveIndex(applicationMenu, itemIndex);
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
        "RibbonMenuItem" => "Menu Item",
        "BackstageTabItem" => "Backstage Page",
        "RibbonApplicationMenu" => "Application Menu",
        "RibbonApplicationMenuItem" => "Application Menu Command",
        "RibbonApplicationMenuSeparator" => "Application Menu Separator",
        "RibbonApplicationMenuPaneItem" => "Application Menu Pane Item",
        "RibbonApplicationMenuButton" => "Application Menu Footer Button",
        "TextBlock" => "Text Block",
        _ => typeName,
    };

    // ---- Selection / command state ----------------------------------------------------

    private NodeInfo? Selected => (_tree.SelectedItem as TreeViewItem)?.Tag as NodeInfo;

    private void UpdateDetails()
    {
        NodeInfo? node = Selected;

        if (node is null)
        {
            _typeText.Text = string.Empty;
            _headerBox.Text = string.Empty;
        }
        else
        {
            string type = SafeType(node.Item);
            _typeText.Text = node.Kind switch
            {
                NodeKind.Control or NodeKind.Container => FriendlyType(type) + " (" + type + ")",
                NodeKind.ApplicationMenu => "Application Menu (" + type + ")",
                NodeKind.ApplicationMenuSection => "Application Menu Content (" + type + ")",
                _ => node.Kind.ToString(),
            };
            _headerBox.Text = node.Kind == NodeKind.Ribbon ? string.Empty : DesignModel.GetCaption(node.Item);
        }

        // Collection children can move/delete; scalar application-menu roots/sections can delete only.
        bool structural = node != null && (node.ParentCollection != null || node.ParentProperty != null);
        // Caption edits work for Header controls AND Content items (combo/gallery items).
        bool renameable = node != null && node.Kind != NodeKind.Ribbon && DesignModel.HasCaption(node.Item);

        bool canAddChild = ResolveChildTarget(node) != null;
        _add.IsEnabled = node != null
            && (node.Kind == NodeKind.Ribbon
                || ResolveTab(node) != null
                || canAddChild
                || ResolveItemTarget(node) != null
                || CanAddApplicationMenuContent(node));

        _rename.IsEnabled = renameable;
        _headerBox.IsEnabled = renameable;
        _delete.IsEnabled = structural;

        // Move enabled only when there is somewhere to move to.
        int index = -1;
        int count = 0;
        // Pattern-matched rather than reused from `structural`: a bool cannot carry the not-null it
        // proved, so both arguments below would read as possibly-null through it.
        if (node?.ParentCollection is { } parentCollection)
        {
            try
            {
                index = DesignModel.IndexInParent(node.Item, parentCollection);
                count = DesignModel.SiblingCount(node.Item, parentCollection);
            }
            catch (Exception ex)
            {
                DesignLog.Error("index/count of " + SafeType(node.Item), ex);
            }
        }

        _moveUp.IsEnabled = node?.ParentCollection != null && index > 0;
        _moveDown.IsEnabled = node?.ParentCollection != null && index >= 0 && index < count - 1;

        BuildProps(node);
    }

    // ---- Per-item property editors ----------------------------------------------------

    private enum EditorKind
    {
        Text,
        Bool,
        Enum,
        IconRef,
        Color,
        AttachedText,
    }

    private sealed class PropSpec
    {
        public PropSpec(
            string name,
            string label,
            EditorKind kind,
            string[]? enumValues = null,
            string? attachedOwner = null,
            System.Func<ModelItem, bool>? appliesTo = null)
        {
            Name = name;
            Label = label;
            Kind = kind;
            EnumValues = enumValues;
            AttachedOwner = attachedOwner;
            AppliesTo = appliesTo;
        }

        public string Name { get; }

        public string Label { get; }

        public EditorKind Kind { get; }

        public string[]? EnumValues { get; }

        /// <summary>For <see cref="EditorKind.AttachedText"/>: the full CLR type that DECLARES the attached property (e.g. <c>RibbonKit.Controls.KeyTip</c>).</summary>
        public string? AttachedOwner { get; }

        /// <summary>
        /// Optional gate: when set and it returns false for the selected item, the row is not shown
        /// at all. For properties the control only HONOURS in some states, so the editor never
        /// offers a setting that would silently do nothing.
        /// </summary>
        public System.Func<ModelItem, bool>? AppliesTo { get; }
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
        new PropSpec("ContextualColor", "Contextual color", EditorKind.Color),
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

    private static readonly PropSpec[] ApplicationMenuSpecs =
    {
        new PropSpec("DefaultHeader", "Default pane name", EditorKind.Text),
    };

    private static readonly PropSpec[] ApplicationMenuItemSpecs =
    {
        new PropSpec("PaneHeader", "Pane header", EditorKind.Text),
        new PropSpec("IsSplit", "Split command", EditorKind.Bool),
    };

    private static readonly PropSpec[] ApplicationMenuPaneItemSpecs =
    {
        new PropSpec("Description", "Description", EditorKind.Text),
    };

    // Split layout is Large-only (§3.43), so the row is GATED rather than always shown: offering
    // "Vertical" on a button that can never render Large would set a property with no visible
    // effect, and the author would have no way to tell that from a bug in the control.
    private static readonly PropSpec[] SplitButtonSpecs =
    {
        new PropSpec(
            "Layout",
            "Split layout",
            EditorKind.Enum,
            new[] { "Horizontal", "Vertical" },
            appliesTo: CanRenderLarge),
    };

    // For editing a gallery item's content visually (its TextBlocks): text and basic appearance.
    private static readonly PropSpec[] TextBlockSpecs =
    {
        new PropSpec("Text", "Text", EditorKind.Text),
        new PropSpec("FontSize", "Font size", EditorKind.Text),
        new PropSpec("FontWeight", "Font weight", EditorKind.Enum, new[] { "Normal", "Light", "SemiBold", "Bold" }),
        new PropSpec("FontStyle", "Font style", EditorKind.Enum, new[] { "Normal", "Italic" }),
        new PropSpec("Foreground", "Foreground", EditorKind.Color),
    };

    // ATTACHED-property rows (Ribbon.CommandId persistence identity; KeyTip.Keys Alt-access badge). Shown
    // on tabs, groups, and command controls (not on entries inside a combo/gallery/menu/backstage).
    // Handled outside the normal spec loop because HasProperty can't see attached members.
    private static readonly PropSpec CommandIdSpec =
        new PropSpec("CommandId", "Command Id (persistence)", EditorKind.AttachedText, attachedOwner: "RibbonKit.Controls.Ribbon");

    private static readonly PropSpec KeyTipSpec =
        new PropSpec("Keys", "KeyTip (Alt access key)", EditorKind.AttachedText, attachedOwner: "RibbonKit.Controls.KeyTip");

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
        "RibbonSplitButton" => SplitButtonSpecs,
        "RibbonApplicationMenu" => ApplicationMenuSpecs,
        "RibbonApplicationMenuItem" => ApplicationMenuItemSpecs,
        "RibbonApplicationMenuPaneItem" => ApplicationMenuPaneItemSpecs,
        "TextBlock" => TextBlockSpecs,
        _ => System.Array.Empty<PropSpec>(),
    };

    /// <summary>
    /// Whether the attached-identity rows (<c>Ribbon.CommandId</c>, <c>KeyTip.Keys</c>) apply: tabs and
    /// groups always; a control only when it's a real command placed in a group/panel, not an entry inside
    /// a combo/gallery/menu/backstage (those items carry neither a persistence identity nor a surface
    /// KeyTip). Uses <see cref="ItemRule"/> on the parent to tell an item apart from a command control —
    /// both are <see cref="NodeKind.Control"/> in the tree. Both the customization serializer and the
    /// KeyTip service read these on exactly this set (tab / group / leaf command).
    /// </summary>
    private static bool ShowsIdentityProps(NodeInfo node)
    {
        if (node?.Item is null)
        {
            return false;
        }

        return node.Kind switch
        {
            NodeKind.Tab => true,
            NodeKind.Group => true,
            NodeKind.Control => node.Item.Parent is null
                || (ItemRule(node.Item.Parent) == null && SafeType(node.Item.Parent) != "RibbonApplicationMenu"),
            _ => false,
        };
    }

    private static bool ShowsApplicationMenuKeyTip(NodeInfo node) =>
        node.Kind == NodeKind.Control && SafeType(node.Item) == "RibbonApplicationMenuItem";

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
    private void BuildProps(NodeInfo? node)
    {
        _propsNode = node;
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

                // A gated row (currently: a split button's Large-only Layout) disappears when the
                // state it depends on changes — AfterPropertyCommitted rebuilds the panel so it
                // appears and vanishes as the author edits Size, not only on reselect.
                if (spec.AppliesTo is { } gate && !gate(node.Item))
                {
                    continue;
                }

                _propsPanel.Children.Add(BuildPropRow(node.Item, spec));
                any = true;
            }

            // The attached rows (Ribbon.CommandId, KeyTip.Keys) are added on their own (HasProperty can't
            // detect an attached member). Shown for tabs, groups, and command controls placed in a group.
            if (ShowsIdentityProps(node))
            {
                _propsPanel.Children.Add(BuildPropRow(node.Item, CommandIdSpec));
                _propsPanel.Children.Add(BuildPropRow(node.Item, KeyTipSpec));
                any = true;
            }

            if (ShowsApplicationMenuKeyTip(node))
            {
                _propsPanel.Children.Add(BuildPropRow(node.Item, KeyTipSpec));
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
            EditorKind.Color => BuildColorEditor(item, spec),
            EditorKind.AttachedText => BuildAttachedTextEditor(item, spec),
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
                AfterPropertyCommitted(item, spec.Name);
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

    /// <summary>
    /// A text editor backed by an ATTACHED property (e.g. <c>Ribbon.CommandId</c>) rather than one of the
    /// element's own properties, so it reads/writes through <see cref="DesignModel.GetAttachedString"/> /
    /// <see cref="DesignModel.SetAttached"/> (which resolve the type-qualified member). Clearing the box
    /// removes the attribute.
    /// </summary>
    private UIElement BuildAttachedTextEditor(ModelItem item, PropSpec spec)
    {
        var box = new TextBox
        {
            Text = DesignModel.GetAttachedString(item, spec.AttachedOwner!, spec.Name),
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        void Commit()
        {
            if (!_syncingProps)
            {
                DesignModel.SetAttached(item, spec.AttachedOwner!, spec.Name, box.Text ?? string.Empty);
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
            if (picker.ShowDialog() == true && picker.SelectedKey is { Length: > 0 } key)
            {
                box.Text = key;
                DesignModel.SetStaticResource(item, spec.Name, key);
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

    private UIElement BuildColorEditor(ModelItem item, PropSpec spec)
    {
        var dock = new DockPanel { LastChildFill = true };

        var pick = new Button { Content = "…", Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(10, 3, 10, 3) };
        DockPanel.SetDock(pick, Dock.Right);
        dock.Children.Add(pick);

        var swatch = new Border
        {
            Width = 18,
            Height = 18,
            BorderThickness = new Thickness(1),
            BorderBrush = SystemColors.ActiveBorderBrush,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(swatch, Dock.Left);
        dock.Children.Add(swatch);

        var box = new TextBox
        {
            Text = DesignModel.GetString(item, spec.Name),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        dock.Children.Add(box);

        void UpdateSwatch() => swatch.Background = ColorPickerDialog.ParseBrush(box.Text);

        void Commit()
        {
            if (!_syncingProps)
            {
                DesignModel.SetProperty(item, spec.Name, box.Text ?? string.Empty);
                UpdateSwatch();
            }
        }

        UpdateSwatch();
        box.LostFocus += (_, _) => Commit();
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Commit();
                e.Handled = true;
            }
        };
        pick.Click += (_, _) =>
        {
            var dialog = new ColorPickerDialog(box.Text) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.SelectedColor is { Length: > 0 } color)
            {
                box.Text = color;
                Commit();
            }
        };
        return dock;
    }

    private UIElement BuildEnumEditor(ModelItem item, PropSpec spec)
    {
        var combo = new ComboBox();
        foreach (string value in spec.EnumValues!)
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
                AfterPropertyCommitted(item, spec.Name);
            }
        };
        return combo;
    }

    /// <summary>
    /// Whether this control could ever render at Large — either it IS Large, or its
    /// <c>SizeDefinition</c> names Large for one of the group states. The sizing engine owns
    /// <c>Size</c> whenever a definition is present, so testing <c>Size</c> alone would hide the
    /// split-layout row on exactly the buttons most likely to want it.
    /// </summary>
    private static bool CanRenderLarge(ModelItem item)
    {
        if (string.Equals(DesignModel.GetString(item, "Size"), "Large", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // GetString never returns null (DesignModel normalises to string.Empty).
        return DesignModel.GetString(item, "SizeDefinition")
            .IndexOf("Large", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Follow-up for edits that change which OTHER editors make sense. Today that is only the split
    /// button: a vertical layout it can no longer reach is reset to Horizontal, so the XAML never
    /// carries a setting the control will ignore, and the panel is rebuilt so the row disappears.
    /// </summary>
    /// <remarks>
    /// Deliberately driven by an explicit edit rather than by selection: silently rewriting a
    /// property just because the author clicked a node would put a surprise entry on the undo stack.
    /// The RUNTIME control never coerces <c>Layout</c> either — it falls back to horizontal while
    /// remembering the author's choice, so a button that reduces to Medium and back is unchanged.
    /// </remarks>
    private void AfterPropertyCommitted(ModelItem item, string propertyName)
    {
        if (propertyName is not ("Size" or "SizeDefinition")
            || SafeType(item) != "RibbonSplitButton"
            || CanRenderLarge(item))
        {
            return;
        }

        if (string.Equals(DesignModel.GetString(item, "Layout"), "Vertical", System.StringComparison.OrdinalIgnoreCase))
        {
            DesignModel.SetProperty(item, "Layout", "Horizontal");
        }

        // The row that raised this belongs to the panel currently on screen, so rebuilding that
        // node is always the right target — no ModelItem identity comparison needed (the designer
        // hands out wrappers, and reference equality across calls is not something to rely on).
        if (_propsNode is not null)
        {
            BuildProps(_propsNode);
        }
    }

    /// <summary>The nearest <c>RibbonTab</c> ancestor of the selection (walks up through any nesting), or null.</summary>
    private ModelItem? ResolveTab(NodeInfo? node)
    {
        ModelItem? current = node?.Item;
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
    private (ModelItem Parent, string Collection)? ResolveChildTarget(NodeInfo? node)
    {
        if (node != null && FindApplicationMenu(node) != null)
        {
            return null; // application-menu slots use their semantic Add actions below
        }

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
            // Split and drop-down buttons are ItemsControls whose flyout entries are RibbonMenuItems in
            // Items — same shape as the combos/galleries above (RibbonSplitButton derives from
            // RibbonDropDownButton). This lets the tree list the menu entries and the "Add Item" button
            // add/insert siblings, with Header as the editable caption.
            case "RibbonSplitButton":
            case "RibbonDropDownButton":
                return new ItemTarget { Container = container, TypeName = "RibbonMenuItem", CaptionProperty = "Header", Label = "Menu Item" };
            default:
                return null;
        }
    }

    /// <summary>
    /// Resolves an "Add Item" target from the selection: the combo/gallery/backstage itself, or the
    /// container of the selected item (so you can add a sibling). Null when neither applies.
    /// </summary>
    private ItemTarget? ResolveItemTarget(NodeInfo? node)
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

    private ModelItem? FindApplicationMenu(NodeInfo? node)
    {
        for (ModelItem? item = node?.Item; item != null; item = item.Parent)
        {
            if (SafeType(item) == "RibbonApplicationMenu")
            {
                return item;
            }
        }

        return null;
    }

    private NodeInfo? FindApplicationMenuSection(NodeInfo? node)
    {
        if (node?.Kind == NodeKind.ApplicationMenuSection)
        {
            return node;
        }

        ModelItem? parent = node?.Item.Parent;
        if (parent != null && _map.TryGetValue(parent, out TreeViewItem treeItem))
        {
            return treeItem.Tag as NodeInfo is { Kind: NodeKind.ApplicationMenuSection } section
                ? section
                : null;
        }

        return null;
    }

    private static bool IsManagedContentSlot(ModelItem owner, string propertyName, string childType)
    {
        ModelItem? content = DesignModel.FindProperty(owner, propertyName)?.Value;
        if (content is null)
        {
            return true;
        }

        if (SafeType(content) != "StackPanel")
        {
            return false;
        }

        foreach (ModelItem child in SafeChildren(content, "Children"))
        {
            if (SafeType(child) != childType)
            {
                return false;
            }
        }

        return true;
    }

    private bool CanAddApplicationMenuContent(NodeInfo node)
    {
        if (node.Kind == NodeKind.Ribbon)
        {
            return DesignModel.FindProperty(_ribbon, "ApplicationMenu")?.Value is null;
        }

        ModelItem? menu = FindApplicationMenu(node);
        if (menu is null)
        {
            return false;
        }

        if (node.Kind == NodeKind.ApplicationMenu
            || node.Item.Parent != null && SafeType(node.Item.Parent) == "RibbonApplicationMenu")
        {
            return true; // command or separator sibling
        }

        if (SafeType(node.Item) == "RibbonApplicationMenuItem")
        {
            return IsManagedContentSlot(node.Item, "Content", "RibbonApplicationMenuPaneItem");
        }

        NodeInfo? section = FindApplicationMenuSection(node);
        if (section?.ParentProperty == "DefaultContent")
        {
            return IsManagedContentSlot(menu, "DefaultContent", "RibbonApplicationMenuPaneItem");
        }
        if (section?.ParentProperty == "Content" && section.Item.Parent is { } command)
        {
            return IsManagedContentSlot(command, "Content", "RibbonApplicationMenuPaneItem");
        }
        if (section?.ParentProperty == "FooterContent")
        {
            return IsManagedContentSlot(menu, "FooterContent", "RibbonApplicationMenuButton");
        }

        return false;
    }

    // ---- Commands ---------------------------------------------------------------------

    private void OnAddTab()
    {
        ModelItem tab = DesignModel.AddTab(_ribbon);
        RebuildTree(tab);
    }

    private void OnAddGroup()
    {
        ModelItem? tab = ResolveTab(Selected);
        if (tab != null)
        {
            ModelItem group = DesignModel.AddGroup(tab);
            RebuildTree(group);
        }
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (!_add.IsEnabled)
        {
            return;
        }

        _addMenu = new ContextMenu
        {
            PlacementTarget = _add,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
        };

        NodeInfo? node = Selected;
        if (node?.Kind == NodeKind.Ribbon)
        {
            _addMenu.Items.Add(MakeMenuItem("Tab", OnAddTab));
            if (DesignModel.FindProperty(_ribbon, "ApplicationMenu")?.Value is null)
            {
                _addMenu.Items.Add(MakeMenuItem("Application Menu", OnAddApplicationMenu));
            }
        }

        if (ResolveTab(node) != null)
        {
            _addMenu.Items.Add(MakeMenuItem("Group", OnAddGroup));
        }

        if (ResolveChildTarget(node) != null)
        {
            var control = new MenuItem { Header = "Control" };
            control.Items.Add(MakeControlMenuItem("Button", "RibbonButton", true));
            control.Items.Add(MakeControlMenuItem("Toggle Button", "RibbonToggleButton", true));
            control.Items.Add(MakeControlMenuItem("Split Button", "RibbonSplitButton", true));
            control.Items.Add(MakeControlMenuItem("Drop-Down Button", "RibbonDropDownButton", true));
            control.Items.Add(new Separator());
            control.Items.Add(MakeControlMenuItem("Combo Box", "RibbonComboBox", false));
            control.Items.Add(MakeControlMenuItem("Gallery (in-ribbon)", "InRibbonGallery", false));
            control.Items.Add(MakeControlMenuItem("Gallery (drop-down)", "RibbonGallery", false));
            control.Items.Add(MakeControlMenuItem("Separator", "Separator", false));
            control.Items.Add(MakeControlMenuItem("Text Block", "TextBlock", false));
            _addMenu.Items.Add(control);
            _addMenu.Items.Add(MakeMenuItem("Stack Panel", OnAddStack));
        }

        if (ResolveItemTarget(node) != null)
        {
            _addMenu.Items.Add(MakeMenuItem("Item", OnAddItem));
        }


        AddApplicationMenuActions(node);

        _addMenu.IsOpen = true;
    }

    private void AddApplicationMenuActions(NodeInfo? node)
    {
        ModelItem? menu = FindApplicationMenu(node);
        if (node is null || menu is null)
        {
            return;
        }

        bool directEntry = node.Kind == NodeKind.ApplicationMenu
            || node.Item.Parent != null && SafeType(node.Item.Parent) == "RibbonApplicationMenu";
        if (directEntry)
        {
            _addMenu.Items.Add(MakeMenuItem("Application Menu Command", OnAddApplicationMenuCommand));
            _addMenu.Items.Add(MakeMenuItem("Application Menu Separator", OnAddApplicationMenuSeparator));
        }

        if (node.Kind == NodeKind.ApplicationMenu
            && IsManagedContentSlot(menu, "DefaultContent", "RibbonApplicationMenuPaneItem"))
        {
            _addMenu.Items.Add(MakeMenuItem("Default Pane Item", () =>
                OnAddApplicationMenuContentItem(menu, "DefaultContent", "Vertical", "RibbonApplicationMenuPaneItem", "Pane Item")));
        }

        if (node.Kind == NodeKind.ApplicationMenu
            && IsManagedContentSlot(menu, "FooterContent", "RibbonApplicationMenuButton"))
        {
            _addMenu.Items.Add(MakeMenuItem("Footer Button", () =>
                OnAddApplicationMenuContentItem(menu, "FooterContent", "Horizontal", "RibbonApplicationMenuButton", "Footer Button")));
        }

        if (SafeType(node.Item) == "RibbonApplicationMenuItem"
            && IsManagedContentSlot(node.Item, "Content", "RibbonApplicationMenuPaneItem"))
        {
            _addMenu.Items.Add(MakeMenuItem("Pane Item", () =>
                OnAddApplicationMenuContentItem(node.Item, "Content", "Vertical", "RibbonApplicationMenuPaneItem", "Pane Item")));
        }

        NodeInfo? section = FindApplicationMenuSection(node);
        if (section?.ParentProperty == "DefaultContent"
            && IsManagedContentSlot(menu, "DefaultContent", "RibbonApplicationMenuPaneItem"))
        {
            _addMenu.Items.Add(MakeMenuItem("Pane Item", () =>
                OnAddApplicationMenuContentItem(menu, "DefaultContent", "Vertical", "RibbonApplicationMenuPaneItem", "Pane Item")));
        }
        else if (section?.ParentProperty == "Content" && section.Item.Parent is { } command
            && IsManagedContentSlot(command, "Content", "RibbonApplicationMenuPaneItem"))
        {
            _addMenu.Items.Add(MakeMenuItem("Pane Item", () =>
                OnAddApplicationMenuContentItem(command, "Content", "Vertical", "RibbonApplicationMenuPaneItem", "Pane Item")));
        }
        else if (section?.ParentProperty == "FooterContent"
            && IsManagedContentSlot(menu, "FooterContent", "RibbonApplicationMenuButton"))
        {
            _addMenu.Items.Add(MakeMenuItem("Footer Button", () =>
                OnAddApplicationMenuContentItem(menu, "FooterContent", "Horizontal", "RibbonApplicationMenuButton", "Footer Button")));
        }
    }

    private void OnAddApplicationMenu()
    {
        ModelItem? menu = DesignModel.AddApplicationMenu(_ribbon);
        if (menu != null)
        {
            RebuildTree(menu);
        }
    }

    private void OnAddApplicationMenuCommand()
    {
        ModelItem? menu = FindApplicationMenu(Selected);
        ModelItem? item = menu is null
            ? null
            : DesignModel.AddApplicationMenuEntry(menu, "RibbonApplicationMenuItem", "New Command");
        if (item != null)
        {
            RebuildTree(item);
        }
    }

    private void OnAddApplicationMenuSeparator()
    {
        ModelItem? menu = FindApplicationMenu(Selected);
        ModelItem? item = menu is null
            ? null
            : DesignModel.AddApplicationMenuEntry(menu, "RibbonApplicationMenuSeparator", null);
        if (item != null)
        {
            RebuildTree(item);
        }
    }

    private void OnAddApplicationMenuContentItem(
        ModelItem owner,
        string propertyName,
        string orientation,
        string childType,
        string caption)
    {
        ModelItem? item = DesignModel.AddApplicationMenuContentItem(
            owner,
            propertyName,
            orientation,
            childType,
            caption);
        if (item != null)
        {
            RebuildTree(item);
        }
    }

    private void OnAddControl(string typeName, string caption, bool isButton)
    {
        (ModelItem Parent, string Collection)? target = ResolveChildTarget(Selected);
        if (target != null)
        {
            // Only buttons get a Header caption; buttons stacked inside a container default to
            // Small (the icon-row form). Combos/galleries/separators get neither.
            string? header = isButton ? caption : null;
            string? size = isButton && target.Value.Collection == "Children" ? "Small" : null;
            ModelItem? control = DesignModel.AddControl(target.Value.Parent, target.Value.Collection, typeName, header, size);
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
            ModelItem? stack = DesignModel.AddStackPanel(target.Value.Parent, target.Value.Collection, orientation);
            if (stack != null)
            {
                RebuildTree(stack);
            }
        }
    }

    private void OnMove(int delta)
    {
        NodeInfo? node = Selected;
        if (node?.ParentCollection is not { } parentCollection)
        {
            return;
        }

        DesignModel.Move(node.Item, parentCollection, delta);
        RebuildTree(node.Item);
    }

    // ---- Drag-drop reordering ---------------------------------------------------------
    //
    // Drag a tree node onto another to reorder or reparent it: a line between rows inserts before/after
    // that sibling; a box over a container row drops INTO it (append). Compatibility mirrors the toolbar
    // verbs — tabs among tabs, groups among groups (incl. across tabs), controls among a group/panel's
    // children (incl. across groups/panels), and combo/gallery/menu/backstage items among containers of
    // the same item type. Each drop is one undo via DesignModel.MoveInto.

    private void OnTreeMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragSource = NodeAt(ContainerAtPoint(e.OriginalSource as DependencyObject));
    }

    private void OnTreeMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSource is null || !IsDraggable(_dragSource))
        {
            return;
        }

        Point now = e.GetPosition(null);
        if (Math.Abs(now.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(now.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        NodeInfo source = _dragSource;
        _dragSource = null; // consume so we don't re-enter DoDragDrop
        try
        {
            DragDrop.DoDragDrop(_tree, source, DragDropEffects.Move);
        }
        catch (Exception ex)
        {
            DesignLog.Error("DoDragDrop", ex);
        }
        finally
        {
            ClearDropAdorner();
        }
    }

    private void OnTreeDragOver(object sender, DragEventArgs e)
    {
        DropPlan? plan = PlanFromEvent(e);
        e.Effects = plan is null ? DragDropEffects.None : DragDropEffects.Move;
        if (plan is { } p)
        {
            ShowDropAdorner(p.TargetItem, p.Mode);
        }
        else
        {
            ClearDropAdorner();
        }

        e.Handled = true;
    }

    private void OnTreeDrop(object sender, DragEventArgs e)
    {
        ClearDropAdorner();
        if (PlanFromEvent(e) is not { } plan)
        {
            return;
        }

        DesignModel.MoveInto(plan.Source.Item, plan.Source.ParentCollection, plan.TargetParent, plan.CollectionProperty, plan.Index);
        RebuildTree(plan.Source.Item);
        e.Handled = true;
    }

    /// <summary>Builds the drop plan for the current drag position, or null when the drop isn't allowed.</summary>
    private DropPlan? PlanFromEvent(DragEventArgs e)
    {
        if (e.Data?.GetData(typeof(NodeInfo)) is not NodeInfo source)
        {
            return null;
        }

        TreeViewItem? targetItem = ContainerAtPoint(e.OriginalSource as DependencyObject);
        NodeInfo? target = NodeAt(targetItem);
        // targetItem is tested explicitly even though a null one could only yield a null target:
        // the two are related by the call, not by anything flow analysis can see.
        if (targetItem is null || target is null || target.Item is null || ReferenceEquals(target.Item, source.Item))
        {
            return null;
        }

        DropMode mode = ModeFor(targetItem, e.GetPosition(targetItem));

        // "Into": append into the target when it's a container that accepts the source.
        if (mode == DropMode.Into && ContainerAccept(target, source) is { } into
            && !IsAncestorOrSelf(source.Item, into.Parent))
        {
            return new DropPlan(source, targetItem, DropMode.Into, into.Parent, into.Collection, CountOf(into.Parent, into.Collection));
        }

        // Before/After: insert as a sibling of the target in the target's own collection.
        ModelItem parent = target.Item.Parent;
        string collection = target.ParentCollection;
        if (parent is null || collection is null || !Accepts(parent, collection, source) || IsAncestorOrSelf(source.Item, parent))
        {
            return null;
        }

        int targetIndex = DesignModel.IndexInParent(target.Item, collection);
        if (targetIndex < 0)
        {
            return null;
        }

        int index = mode == DropMode.After ? targetIndex + 1 : targetIndex;
        return new DropPlan(source, targetItem, mode == DropMode.After ? DropMode.After : DropMode.Before, parent, collection, index);
    }

    /// <summary>The count of a parent's collection (the append index for an "into" drop).</summary>
    private static int CountOf(ModelItem parent, string collectionProperty) => SafeChildren(parent, collectionProperty).Count;

    /// <summary>Decides before/into/after from where the pointer sits within the target row's header.</summary>
    private static DropMode ModeFor(TreeViewItem targetItem, Point posInItem)
    {
        double headerHeight = HeaderHeight(targetItem);
        double y = posInItem.Y;
        if (y < headerHeight * 0.3d)
        {
            return DropMode.Before;
        }

        if (y > headerHeight * 0.7d)
        {
            return DropMode.After;
        }

        return DropMode.Into;
    }

    private static double HeaderHeight(TreeViewItem item)
    {
        if (FindVisualChild<ContentPresenter>(item) is { ActualHeight: > 0 } header)
        {
            return header.ActualHeight;
        }

        return Math.Min(item.ActualHeight <= 0 ? 22d : item.ActualHeight, 22d);
    }

    /// <summary>Whether a node can be dragged (structural node with a home collection, not the root).</summary>
    private static bool IsDraggable(NodeInfo node) =>
        node is { ParentCollection: not null } && node.Kind != NodeKind.Ribbon;

    /// <summary>True when <paramref name="node"/> is an entry inside a combo/gallery/menu/backstage (vs a group control).</summary>
    private static bool IsItemEntry(NodeInfo node) =>
        node.Kind == NodeKind.Control && node.Item?.Parent != null && ItemRule(node.Item.Parent) != null;

    /// <summary>Whether the collection <paramref name="collection"/> on <paramref name="parent"/> may hold <paramref name="source"/>.</summary>
    private static bool Accepts(ModelItem parent, string collection, NodeInfo source)
    {
        if (IsInsideApplicationMenu(source.Item))
        {
            string sourceType = SafeType(source.Item);
            if (sourceType == "RibbonApplicationMenuItem" || sourceType == "RibbonApplicationMenuSeparator")
            {
                return collection == "Items" && SafeType(parent) == "RibbonApplicationMenu";
            }

            if (sourceType == "RibbonApplicationMenuPaneItem")
            {
                return collection == "Children"
                    && IsManagedApplicationMenuStack(parent, "RibbonApplicationMenuPaneItem");
            }

            if (sourceType == "RibbonApplicationMenuButton")
            {
                return collection == "Children"
                    && IsManagedApplicationMenuStack(parent, "RibbonApplicationMenuButton");
            }

            return false;
        }

        switch (source.Kind)
        {
            case NodeKind.Tab:
                return collection == "Tabs";
            case NodeKind.Group:
                return collection == "Groups";
            default:
                if (IsItemEntry(source))
                {
                    // Reorder within, or move between, containers of the SAME item type.
                    return collection == "Items"
                        && ItemRule(parent) is { } rule
                        && rule.TypeName == SafeType(source.Item);
                }

                // A real command control / nested panel: lives in a group's Items or a panel's Children.
                return (collection == "Items" && SafeType(parent) == "RibbonGroup")
                    || (collection == "Children" && DesignModel.HasProperty(parent, "Children"));
        }
    }

    /// <summary>The (parent, collection) a container node would append the source into, or null.</summary>
    private static (ModelItem Parent, string Collection)? ContainerAccept(NodeInfo target, NodeInfo source)
    {
        (ModelItem Parent, string Collection)? candidate = target.Kind switch
        {
            NodeKind.Tab => (target.Item, "Groups"),
            NodeKind.Group => (target.Item, "Items"),
            NodeKind.Container => (target.Item, "Children"),
            NodeKind.ApplicationMenu => (target.Item, "Items"),
            NodeKind.ApplicationMenuSection => (target.Item, "Children"),
            NodeKind.Control when ItemRule(target.Item) != null => (target.Item, "Items"),
            _ => ((ModelItem, string)?)null,
        };

        return candidate is { } c && Accepts(c.Parent, c.Collection, source) ? c : null;
    }

    private static bool IsInsideApplicationMenu(ModelItem item)
    {
        for (ModelItem? cursor = item; cursor != null; cursor = cursor.Parent)
        {
            if (SafeType(cursor) == "RibbonApplicationMenu")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsManagedApplicationMenuStack(ModelItem stack, string childType)
    {
        if (SafeType(stack) != "StackPanel" || !IsInsideApplicationMenu(stack))
        {
            return false;
        }

        foreach (ModelItem child in SafeChildren(stack, "Children"))
        {
            if (SafeType(child) != childType)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when <paramref name="ancestor"/> is <paramref name="node"/> or one of its ancestors (blocks dropping a node into itself).</summary>
    private static bool IsAncestorOrSelf(ModelItem ancestor, ModelItem node)
    {
        for (ModelItem cursor = node; cursor != null; cursor = cursor.Parent)
        {
            if (ReferenceEquals(cursor, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private TreeViewItem? ContainerAtPoint(DependencyObject? source)
    {
        while (source != null && source is not TreeViewItem)
        {
            source = source is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source)
                : null;
        }

        return source as TreeViewItem;
    }

    private static NodeInfo? NodeAt(TreeViewItem? item) => item?.Tag as NodeInfo;

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                return typed;
            }

            if (FindVisualChild<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    private void ShowDropAdorner(TreeViewItem item, DropMode mode)
    {
        if (item is null)
        {
            ClearDropAdorner();
            return;
        }

        AdornerLayer layer = AdornerLayer.GetAdornerLayer(item);
        if (layer is null)
        {
            return;
        }

        if (_dropAdorner != null && !ReferenceEquals(_dropAdorner.AdornedElement, item))
        {
            ClearDropAdorner();
        }

        if (_dropAdorner is null)
        {
            _dropAdorner = new DropAdorner(item);
            layer.Add(_dropAdorner);
        }

        _dropAdorner.Update(mode, HeaderHeight(item));
    }

    private void ClearDropAdorner()
    {
        if (_dropAdorner != null)
        {
            AdornerLayer.GetAdornerLayer(_dropAdorner.AdornedElement)?.Remove(_dropAdorner);
            _dropAdorner = null;
        }
    }

    private enum DropMode
    {
        Before,
        Into,
        After,
    }

    private sealed class DropPlan
    {
        public DropPlan(NodeInfo source, TreeViewItem targetItem, DropMode mode, ModelItem targetParent, string collectionProperty, int index)
        {
            Source = source;
            TargetItem = targetItem;
            Mode = mode;
            TargetParent = targetParent;
            CollectionProperty = collectionProperty;
            Index = index;
        }

        public NodeInfo Source { get; }

        public TreeViewItem TargetItem { get; }

        public DropMode Mode { get; }

        public ModelItem TargetParent { get; }

        public string CollectionProperty { get; }

        public int Index { get; }
    }

    /// <summary>Draws the drop hint on a target row: a line above/below for before/after, a rounded box for "into".</summary>
    private sealed class DropAdorner : Adorner
    {
        private static readonly Brush LineBrush = MakeFrozen(new SolidColorBrush(Color.FromRgb(0x0F, 0x6C, 0xBD)));
        private static readonly Pen LinePen = MakeFrozen(new Pen(LineBrush, 2d));
        private static readonly Brush IntoFill = MakeFrozen(new SolidColorBrush(Color.FromArgb(0x33, 0x0F, 0x6C, 0xBD)));

        private DropMode _mode;
        private double _headerHeight;

        public DropAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
        }

        public void Update(DropMode mode, double headerHeight)
        {
            _mode = mode;
            _headerHeight = headerHeight;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            double width = AdornedElement.RenderSize.Width;
            double h = _headerHeight > 0 ? _headerHeight : AdornedElement.RenderSize.Height;

            if (_mode == DropMode.Into)
            {
                drawingContext.DrawRoundedRectangle(IntoFill, LinePen, new Rect(1, 1, Math.Max(0, width - 2), Math.Max(0, h - 2)), 3, 3);
                return;
            }

            double y = _mode == DropMode.After ? h : 0;
            drawingContext.DrawLine(LinePen, new Point(0, y), new Point(width, y));
            // Small end cap so the insertion line reads clearly.
            drawingContext.DrawEllipse(LineBrush, null, new Point(3, y), 3, 3);
        }

        private static T MakeFrozen<T>(T freezable) where T : Freezable
        {
            freezable.Freeze();
            return freezable;
        }
    }

    private void OnDelete()
    {
        NodeInfo? node = Selected;
        if (node is null)
        {
            return;
        }

        if (node.ParentCollection is { } parentCollection)
        {
            ModelItem parent = node.Item.Parent;
            DesignModel.Delete(node.Item, parentCollection);
            RebuildTree(parent);
            return;
        }

        if (node.ParentProperty is { } parentProperty)
        {
            ModelItem? owner = node.Kind == NodeKind.ApplicationMenu ? _ribbon : node.Item.Parent;
            if (owner != null)
            {
                if (node.Kind == NodeKind.ApplicationMenu)
                {
                    TabPreviewCoordinator.SetFileSurface(_ribbon, FileSurfacePreview.Closed);
                    ApplicationMenuPreviewCoordinator.SetActiveIndex(node.Item, null);
                }

                DesignModel.ClearProperty(owner, parentProperty);
                RebuildTree(owner);
            }
        }
    }

    private void OnRename()
    {
        NodeInfo? node = Selected;
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
            ModelItem? item = DesignModel.AddItem(target.Value.Container, target.Value.TypeName, target.Value.CaptionProperty, target.Value.Label);
            if (item != null)
            {
                RebuildTree(item);
            }
        }
    }
}
