using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>The kind of entry a <see cref="RibbonCustomizeNode"/> represents.</summary>
public enum RibbonCustomizeNodeKind
{
    /// <summary>A ribbon tab (checkbox = visibility).</summary>
    Tab,

    /// <summary>A group within a tab.</summary>
    Group,

    /// <summary>A command control within a group.</summary>
    Command,
}

/// <summary>
/// One node of the <see cref="RibbonCustomizePage"/> tree: a tab, group, or command, with
/// the display caption/icon and the tree state (selection, expansion, tab visibility).
/// </summary>
public sealed class RibbonCustomizeNode : INotifyPropertyChanged
{
    private readonly Ribbon _ribbon;
    private bool _isSelected;
    private bool _isExpanded = true;

    internal RibbonCustomizeNode(
        Ribbon ribbon,
        RibbonCustomizeNodeKind kind,
        object item,
        RibbonCustomizeNode? parent,
        string header,
        ImageSource? icon,
        bool isCustom)
    {
        _ribbon = ribbon;
        Kind = kind;
        Item = item;
        Parent = parent;
        Header = isCustom ? $"{header} (Custom)" : header;
        Icon = icon;
        IsCustom = isCustom;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Whether this node is a tab, group, or command.</summary>
    public RibbonCustomizeNodeKind Kind { get; }

    /// <summary>Display caption; custom entries carry an "(Custom)" suffix, like Office.</summary>
    public string Header { get; }

    /// <summary>16px icon, when the underlying group/command has one.</summary>
    public ImageSource? Icon { get; }

    /// <summary>Child nodes (groups under a tab; commands under a group).</summary>
    public ObservableCollection<RibbonCustomizeNode> Children { get; } = new();

    /// <summary>Only tab nodes show the visibility checkbox.</summary>
    public bool ShowCheckBox => Kind == RibbonCustomizeNodeKind.Tab;

    /// <summary>Whether the underlying tab/group is user-created/user-editable.</summary>
    public bool IsCustom { get; }

    internal object Item { get; }

    internal RibbonCustomizeNode? Parent { get; }

    /// <summary>
    /// A tab node's visibility, driving <see cref="UIElement.Visibility"/> directly. Refuses
    /// to hide the LAST visible non-contextual tab (an all-hidden ribbon is a dead end) by
    /// snapping the checkbox back.
    /// </summary>
    public bool IsVisibleChecked
    {
        get => Item is RibbonTab tab && tab.Visibility == Visibility.Visible;
        set
        {
            if (Item is not RibbonTab tab || value == (tab.Visibility == Visibility.Visible))
            {
                return;
            }

            if (!value && !_ribbon.Tabs.Any(other =>
                    !ReferenceEquals(other, tab)
                    && !other.IsContextual
                    && other.Visibility == Visibility.Visible))
            {
                // Refused: notify AFTER the binding transfer completes, so the re-read snaps
                // the checkbox back (a synchronous raise inside the setter is swallowed by
                // the binding's own reentrancy guard).
                _ribbon.Dispatcher.BeginInvoke(
                    new Action(() => OnPropertyChanged(nameof(IsVisibleChecked))));
                return;
            }

            tab.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            OnPropertyChanged(nameof(IsVisibleChecked));
        }
    }

    /// <summary>Tree selection state (two-way bound to the TreeViewItem container).</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    /// <summary>Tree expansion state (two-way bound; nodes start expanded).</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// The built-in "Customize the Ribbon" page for <see cref="RibbonOptionsDialog"/>: available
/// commands on the left; the ribbon's structure (tabs → groups → commands) as a checkbox tree
/// on the right, with Add / Remove / Up / Down / New Tab / New Group / Rename — mirroring
/// Office's rules:
/// <list type="bullet">
/// <item>any non-contextual tab can be shown/hidden (except the last visible one) and reordered;</item>
/// <item>groups reorder within their tab; commands reorder within CUSTOM groups;</item>
/// <item>commands are ADDED only into custom groups (as proxy buttons — see
/// <see cref="Ribbon.AddToQuickAccess"/> for the proxy rationale);</item>
/// <item>only custom tabs/groups/commands can be REMOVED (marked via
/// <see cref="Ribbon.IsCustomProperty"/>; apps may pre-mark their own);</item>
/// <item>tabs, groups, and custom commands can be renamed.</item>
/// </list>
/// Contextual tabs are excluded — the application drives their visibility. Edits are LIVE;
/// the dialog's <see cref="RibbonOptionsDialog.Applied"/> signals when to persist.
/// </summary>
[TemplatePart(Name = AvailableListPartName, Type = typeof(ListBox))]
[TemplatePart(Name = TreePartName, Type = typeof(TreeView))]
[TemplatePart(Name = AddButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = RemoveButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = UpButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = DownButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = NewTabButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = NewGroupButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = EditButtonPartName, Type = typeof(ButtonBase))]
public class RibbonCustomizePage : Control, IRibbonFillPage
{
    private const string AvailableListPartName = "PART_AvailableList";
    private const string TreePartName = "PART_Tree";
    private const string AddButtonPartName = "PART_AddButton";
    private const string RemoveButtonPartName = "PART_RemoveButton";
    private const string UpButtonPartName = "PART_UpButton";
    private const string DownButtonPartName = "PART_DownButton";
    private const string NewTabButtonPartName = "PART_NewTabButton";
    private const string NewGroupButtonPartName = "PART_NewGroupButton";
    private const string EditButtonPartName = "PART_EditButton";

    /// <summary>Identifies the <see cref="Ribbon"/> dependency property.</summary>
    public static readonly DependencyProperty RibbonProperty =
        DependencyProperty.Register(
            nameof(Ribbon),
            typeof(Ribbon),
            typeof(RibbonCustomizePage),
            new FrameworkPropertyMetadata(null, (d, _) => ((RibbonCustomizePage)d).RebuildAll()));

    private readonly ObservableCollection<RibbonCustomizeNode> _rootNodes = new();

    private ListBox? _availableList;
    private TreeView? _tree;
    private ButtonBase? _addButton;
    private ButtonBase? _removeButton;
    private ButtonBase? _upButton;
    private ButtonBase? _downButton;
    private ButtonBase? _newTabButton;
    private ButtonBase? _newGroupButton;
    private ButtonBase? _editButton;
    private RibbonCustomizeNode? _selectedNode;

    static RibbonCustomizePage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonCustomizePage),
            new FrameworkPropertyMetadata(typeof(RibbonCustomizePage)));
    }

    /// <summary>Initializes the page; the tree builds once it loads.</summary>
    public RibbonCustomizePage()
    {
        Loaded += (_, _) => RebuildAll();
    }

    /// <summary>The ribbon whose structure this page edits.</summary>
    public Ribbon? Ribbon
    {
        get => (Ribbon?)GetValue(RibbonProperty);
        set => SetValue(RibbonProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        Unhook(_addButton, OnAddClick);
        Unhook(_removeButton, OnRemoveClick);
        Unhook(_upButton, OnUpClick);
        Unhook(_downButton, OnDownClick);
        Unhook(_newTabButton, OnNewTabClick);
        Unhook(_newGroupButton, OnNewGroupClick);
        Unhook(_editButton, OnEditClick);
        if (_tree is not null)
        {
            _tree.SelectedItemChanged -= OnTreeSelectionChanged;
        }

        if (_availableList is not null)
        {
            _availableList.SelectionChanged -= OnAvailableSelectionChanged;
        }

        base.OnApplyTemplate();

        _availableList = GetTemplateChild(AvailableListPartName) as ListBox;
        _tree = GetTemplateChild(TreePartName) as TreeView;
        _addButton = GetTemplateChild(AddButtonPartName) as ButtonBase;
        _removeButton = GetTemplateChild(RemoveButtonPartName) as ButtonBase;
        _upButton = GetTemplateChild(UpButtonPartName) as ButtonBase;
        _downButton = GetTemplateChild(DownButtonPartName) as ButtonBase;
        _newTabButton = GetTemplateChild(NewTabButtonPartName) as ButtonBase;
        _newGroupButton = GetTemplateChild(NewGroupButtonPartName) as ButtonBase;
        _editButton = GetTemplateChild(EditButtonPartName) as ButtonBase;

        Hook(_addButton, OnAddClick);
        Hook(_removeButton, OnRemoveClick);
        Hook(_upButton, OnUpClick);
        Hook(_downButton, OnDownClick);
        Hook(_newTabButton, OnNewTabClick);
        Hook(_newGroupButton, OnNewGroupClick);
        Hook(_editButton, OnEditClick);

        if (_tree is not null)
        {
            _tree.ItemsSource = _rootNodes;
            _tree.SelectedItemChanged += OnTreeSelectionChanged;
        }

        if (_availableList is not null)
        {
            _availableList.SelectionChanged += OnAvailableSelectionChanged;
        }

        RebuildAll();
    }

    private static void Hook(ButtonBase? button, RoutedEventHandler handler)
    {
        if (button is not null)
        {
            button.Click += handler;
        }
    }

    private static void Unhook(ButtonBase? button, RoutedEventHandler handler)
    {
        if (button is not null)
        {
            button.Click -= handler;
        }
    }

    // ---- Tree construction ----------------------------------------------------------

    private void RebuildAll()
    {
        if (_availableList is not null)
        {
            _availableList.ItemsSource = Ribbon is { } ribbon
                ? RibbonCommandCatalog.CollectAvailable(ribbon)
                : new ObservableCollection<RibbonCommandEntry>();
        }

        RebuildTree(selectItem: _selectedNode?.Item);
    }

    /// <summary>
    /// Rebuilds the whole tree from the live ribbon structure (small trees; rebuilding after
    /// each edit is simpler and safer than incremental sync), then re-selects the node for
    /// <paramref name="selectItem"/> so repeated Up/Down clicks keep walking the same entry.
    /// </summary>
    private void RebuildTree(object? selectItem)
    {
        _rootNodes.Clear();
        _selectedNode = null;

        if (Ribbon is { } ribbon)
        {
            foreach (RibbonTab tab in ribbon.Tabs)
            {
                // Contextual tabs are excluded: the APPLICATION drives their visibility
                // (e.g. "Picture Format" appears when a picture is selected), so a manual
                // visibility checkbox would fight it.
                if (tab.IsContextual)
                {
                    continue;
                }

                var tabNode = new RibbonCustomizeNode(
                    ribbon,
                    RibbonCustomizeNodeKind.Tab,
                    tab,
                    parent: null,
                    tab.Header?.ToString() ?? "Tab",
                    icon: null,
                    Controls.Ribbon.GetIsCustom(tab));

                foreach (RibbonGroup group in tab.Groups)
                {
                    bool groupIsCustom = Controls.Ribbon.GetIsCustom(group);
                    var groupNode = new RibbonCustomizeNode(
                        ribbon,
                        RibbonCustomizeNodeKind.Group,
                        group,
                        tabNode,
                        group.Header?.ToString() ?? "Group",
                        group.Icon,
                        groupIsCustom);

                    // Custom groups host proxies DIRECTLY as Items (mutable); built-in groups
                    // host arbitrary content, so their commands are found by the catalog walk
                    // and shown read-only (informational, like Office).
                    IEnumerable<FrameworkElement> commands = groupIsCustom
                        ? group.Items.OfType<FrameworkElement>()
                        : RibbonCommandCatalog.CollectControls(group);

                    foreach (FrameworkElement command in commands)
                    {
                        RibbonCommandEntry entry = RibbonCommandCatalog.Describe(command);
                        groupNode.Children.Add(new RibbonCustomizeNode(
                            ribbon,
                            RibbonCustomizeNodeKind.Command,
                            command,
                            groupNode,
                            entry.DisplayName,
                            entry.Icon,
                            isCustom: groupIsCustom));
                    }

                    tabNode.Children.Add(groupNode);
                }

                _rootNodes.Add(tabNode);
            }
        }

        if (selectItem is not null && FindNode(_rootNodes, selectItem) is { } node)
        {
            for (RibbonCustomizeNode? ancestor = node.Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                ancestor.IsExpanded = true;
            }

            node.IsSelected = true; // _selectedNode updates via OnTreeSelectionChanged.
        }

        UpdateButtonStates();
    }

    private static RibbonCustomizeNode? FindNode(IEnumerable<RibbonCustomizeNode> nodes, object item)
    {
        foreach (RibbonCustomizeNode node in nodes)
        {
            if (ReferenceEquals(node.Item, item))
            {
                return node;
            }

            if (FindNode(node.Children, item) is { } inChildren)
            {
                return inChildren;
            }
        }

        return null;
    }

    // ---- Selection / enablement -------------------------------------------------------

    private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selectedNode = e.NewValue as RibbonCustomizeNode;
        UpdateButtonStates();
    }

    private void OnAvailableSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateButtonStates();

    private void UpdateButtonStates()
    {
        RibbonCustomizeNode? node = _selectedNode;

        SetEnabled(_addButton, _availableList?.SelectedItem is RibbonCommandEntry && ResolveTargetCustomGroup() is not null);
        SetEnabled(_removeButton, CanRemove(node));
        SetEnabled(_upButton, CanMove(node, -1));
        SetEnabled(_downButton, CanMove(node, +1));
        SetEnabled(_newTabButton, Ribbon is not null);
        SetEnabled(_newGroupButton, ResolveTab(node) is not null);
        SetEnabled(_editButton, CanEdit(node));
    }

    /// <summary>The selected item's raw header — the display header carries the "(Custom)"
    /// suffix and must not be offered for editing.</summary>
    private string RawHeaderOfSelection() => _selectedNode?.Item switch
    {
        RibbonTab tab => tab.Header?.ToString() ?? string.Empty,
        RibbonGroup group => group.Header?.ToString() ?? string.Empty,
        RibbonButton button => button.Header ?? string.Empty,
        RibbonToggleButton toggle => toggle.Header ?? string.Empty,
        _ => string.Empty,
    };

    private static void SetEnabled(ButtonBase? button, bool enabled)
    {
        if (button is not null)
        {
            button.IsEnabled = enabled;
        }
    }

    private bool CanRemove(RibbonCustomizeNode? node) => node switch
    {
        { Kind: RibbonCustomizeNodeKind.Tab, IsCustom: true } => true,
        { Kind: RibbonCustomizeNodeKind.Group, IsCustom: true } => true,
        // A command is removable when it lives DIRECTLY in a custom group's Items.
        { Kind: RibbonCustomizeNodeKind.Command, Parent.IsCustom: true } => true,
        _ => false,
    };

    private bool CanMove(RibbonCustomizeNode? node, int delta)
    {
        if (Ribbon is not { } ribbon || node is null)
        {
            return false;
        }

        (int index, int count) = node.Kind switch
        {
            RibbonCustomizeNodeKind.Tab when node.Item is RibbonTab tab =>
                (ribbon.Tabs.IndexOf(tab), ribbon.Tabs.Count),
            RibbonCustomizeNodeKind.Group when node.Item is RibbonGroup group && node.Parent?.Item is RibbonTab tab =>
                (tab.Groups.IndexOf(group), tab.Groups.Count),
            // Commands reorder only within CUSTOM groups (built-in groups host arbitrary
            // panels, so their commands aren't direct Items and can't be reordered).
            RibbonCustomizeNodeKind.Command when node.Parent is { IsCustom: true } && node.Parent.Item is RibbonGroup group =>
                (group.Items.IndexOf(node.Item), group.Items.Count),
            _ => (-1, 0),
        };

        int target = index + delta;
        return index >= 0 && target >= 0 && target < count;
    }

    // Tabs and groups are always editable (name-only for built-ins, like Office); commands
    // only when they're PROXIES in a custom group — editing a built-in control would change
    // the actual ribbon button.
    private bool CanEdit(RibbonCustomizeNode? node) => node switch
    {
        { Kind: RibbonCustomizeNodeKind.Tab } => true,
        { Kind: RibbonCustomizeNodeKind.Group } => true,
        { Kind: RibbonCustomizeNodeKind.Command, Parent.IsCustom: true } => true,
        _ => false,
    };

    // ---- Operations ---------------------------------------------------------------

    /// <summary>The custom group targeted by Add: the selected group itself, or the selected
    /// command's parent group. Office-consistent: built-in groups never take new commands.</summary>
    private RibbonGroup? ResolveTargetCustomGroup() => _selectedNode switch
    {
        { Kind: RibbonCustomizeNodeKind.Group, IsCustom: true } node => node.Item as RibbonGroup,
        { Kind: RibbonCustomizeNodeKind.Command, Parent.IsCustom: true } node => node.Parent!.Item as RibbonGroup,
        _ => null,
    };

    /// <summary>The tab containing the selection (the selection itself when a tab).</summary>
    private RibbonTab? ResolveTab(RibbonCustomizeNode? node)
    {
        while (node is not null)
        {
            if (node.Item is RibbonTab tab)
            {
                return tab;
            }

            node = node.Parent;
        }

        return null;
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (Ribbon is not { } ribbon
            || _availableList?.SelectedItem is not RibbonCommandEntry entry
            || ResolveTargetCustomGroup() is not { } group)
        {
            return;
        }

        // Proxy size follows the target group's layout: Large layout → Large buttons;
        // otherwise Medium (16px icon + label), the natural stacked-column size.
        RibbonControlSize proxySize = group.Layout == RibbonGroupLayout.Large
            ? RibbonControlSize.Large
            : RibbonControlSize.Medium;
        FrameworkElement proxy = ribbon.CreateCommandProxy(entry.Control, proxySize);

        // Insert after the selected command when one is selected, else append.
        int insertAt = _selectedNode is { Kind: RibbonCustomizeNodeKind.Command } commandNode
            ? group.Items.IndexOf(commandNode.Item) + 1
            : group.Items.Count;
        group.Items.Insert(Math.Clamp(insertAt, 0, group.Items.Count), proxy);

        RebuildTree(selectItem: proxy);
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (Ribbon is not { } ribbon || _selectedNode is not { } node || !CanRemove(node))
        {
            return;
        }

        object? reselect = node.Parent?.Item;
        switch (node.Kind)
        {
            case RibbonCustomizeNodeKind.Tab when node.Item is RibbonTab tab:
                ribbon.Tabs.Remove(tab);
                reselect = null;
                break;
            case RibbonCustomizeNodeKind.Group when node.Item is RibbonGroup group
                && node.Parent?.Item is RibbonTab parentTab:
                parentTab.Groups.Remove(group);
                break;
            case RibbonCustomizeNodeKind.Command when node.Parent?.Item is RibbonGroup parentGroup:
                parentGroup.Items.Remove(node.Item);
                break;
        }

        RebuildTree(selectItem: reselect);
    }

    private void OnUpClick(object sender, RoutedEventArgs e) => MoveSelected(-1);

    private void OnDownClick(object sender, RoutedEventArgs e) => MoveSelected(+1);

    private void MoveSelected(int delta)
    {
        if (Ribbon is not { } ribbon || _selectedNode is not { } node || !CanMove(node, delta))
        {
            return;
        }

        switch (node.Kind)
        {
            case RibbonCustomizeNodeKind.Tab when node.Item is RibbonTab tab:
            {
                int index = ribbon.Tabs.IndexOf(tab);
                ribbon.Tabs.Move(index, index + delta);
                break;
            }

            case RibbonCustomizeNodeKind.Group when node.Item is RibbonGroup group
                && node.Parent?.Item is RibbonTab tab:
            {
                int index = tab.Groups.IndexOf(group);
                tab.Groups.Move(index, index + delta);
                break;
            }

            case RibbonCustomizeNodeKind.Command when node.Parent?.Item is RibbonGroup group:
            {
                int index = group.Items.IndexOf(node.Item);
                object item = node.Item;
                group.Items.RemoveAt(index);
                group.Items.Insert(index + delta, item);
                break;
            }
        }

        RebuildTree(selectItem: node.Item);
    }

    private void OnNewTabClick(object sender, RoutedEventArgs e)
    {
        if (Ribbon is not { } ribbon)
        {
            return;
        }

        var tab = new RibbonTab { Header = "New Tab" };
        Controls.Ribbon.SetIsCustom(tab, true);
        tab.Groups.Add(CreateCustomGroup());

        // Insert after the tab containing the selection (Office behavior), else append.
        int insertAt = ResolveTab(_selectedNode) is { } anchor && ribbon.Tabs.IndexOf(anchor) is >= 0 and var anchorIndex
            ? anchorIndex + 1
            : ribbon.Tabs.Count;
        ribbon.Tabs.Insert(insertAt, tab);

        RebuildTree(selectItem: tab);
    }

    private void OnNewGroupClick(object sender, RoutedEventArgs e)
    {
        if (ResolveTab(_selectedNode) is not { } tab)
        {
            return;
        }

        RibbonGroup group = CreateCustomGroup();

        // Insert after the selected group (or the selected command's group), else append.
        RibbonGroup? anchor = _selectedNode?.Item as RibbonGroup ?? _selectedNode?.Parent?.Item as RibbonGroup;
        int insertAt = anchor is not null && tab.Groups.IndexOf(anchor) is >= 0 and var anchorIndex
            ? anchorIndex + 1
            : tab.Groups.Count;
        tab.Groups.Insert(insertAt, group);

        RebuildTree(selectItem: group);
    }

    private static RibbonGroup CreateCustomGroup()
    {
        var group = new RibbonGroup { Header = "New Group" };
        Controls.Ribbon.SetIsCustom(group, true);

        // Stacked applies the vertical-wrap items panel (3-row columns of Medium/Small
        // proxies) via RibbonGroup.ApplyGroupLayout — the customize default, like Office.
        group.Layout = RibbonGroupLayout.Stacked;
        return group;
    }

    /// <summary>
    /// Opens the edit dialog for the selection. What's editable follows the target
    /// (Office-style): built-in tabs/groups and custom tabs → name; custom groups → name +
    /// icon (harvested from the ribbon's own icons) + layout; custom-group commands → name +
    /// size, locked to Large when the group's layout is Large.
    /// </summary>
    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (Ribbon is not { } ribbon || _selectedNode is not { } node || !CanEdit(node))
        {
            return;
        }

        var dialog = new RibbonCustomizeEditDialog
        {
            Owner = Window.GetWindow(this),
            Title = node.Kind switch
            {
                RibbonCustomizeNodeKind.Tab => "Edit Tab",
                RibbonCustomizeNodeKind.Group => "Edit Group",
                _ => "Edit Command",
            },
            ItemName = RawHeaderOfSelection(),
        };

        RibbonGroupLayout parentLayout = (node.Parent?.Item as RibbonGroup)?.Layout ?? RibbonGroupLayout.Default;
        switch (node)
        {
            case { Kind: RibbonCustomizeNodeKind.Group, IsCustom: true } when node.Item is RibbonGroup group:
                dialog.CanEditIcon = true;
                dialog.IconChoices = RibbonCommandCatalog.CollectIcons(ribbon);
                dialog.SelectedIcon = group.Icon;
                dialog.CanEditLayout = true;
                dialog.SelectedLayout = group.Layout;
                break;

            case { Kind: RibbonCustomizeNodeKind.Command, Parent.IsCustom: true }:
                dialog.CanEditSize = true;
                dialog.SizeLocked = parentLayout == RibbonGroupLayout.Large;
                dialog.SelectedSize = node.Item switch
                {
                    RibbonButton b => b.Size,
                    RibbonToggleButton t => t.Size,
                    RibbonDropDownButton d => d.Size,
                    _ => RibbonControlSize.Medium,
                };
                break;
        }

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ApplyEdit(node, dialog);
        RebuildTree(selectItem: node.Item);
        RebuildAvailableListOnly();
    }

    private static void ApplyEdit(RibbonCustomizeNode node, RibbonCustomizeEditDialog dialog)
    {
        string? name = dialog.ItemName;
        switch (node.Item)
        {
            case RibbonTab tab when !string.IsNullOrWhiteSpace(name):
                tab.Header = name;
                break;
            case RibbonGroup group:
                if (!string.IsNullOrWhiteSpace(name))
                {
                    group.Header = name;
                }

                if (dialog.CanEditIcon)
                {
                    group.Icon = dialog.SelectedIcon;
                }

                if (dialog.CanEditLayout)
                {
                    // Applies the panel and normalizes the item sizes (see RibbonGroup.Layout).
                    group.Layout = dialog.SelectedLayout;
                }

                break;
            case RibbonButton button:
                if (!string.IsNullOrWhiteSpace(name))
                {
                    button.Header = name;
                }

                if (dialog.CanEditSize && !dialog.SizeLocked)
                {
                    button.Size = dialog.SelectedSize;
                }

                break;
            case RibbonToggleButton toggle:
                if (!string.IsNullOrWhiteSpace(name))
                {
                    toggle.Header = name;
                }

                if (dialog.CanEditSize && !dialog.SizeLocked)
                {
                    toggle.Size = dialog.SelectedSize;
                }

                break;
        }
    }

    // Renames change the "Tab › Group › Command" paths shown on the left.
    private void RebuildAvailableListOnly()
    {
        if (_availableList is not null && Ribbon is { } ribbon)
        {
            _availableList.ItemsSource = RibbonCommandCatalog.CollectAvailable(ribbon);
        }
    }
}
