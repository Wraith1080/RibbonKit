using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// A display entry in the <see cref="RibbonQuickAccessPage"/> lists: the underlying
/// control plus the caption and 16px icon the lists render.
/// </summary>
public sealed class RibbonCommandEntry
{
    internal RibbonCommandEntry(FrameworkElement control, string displayName, ImageSource? icon)
    {
        Control = control;
        DisplayName = displayName;
        Icon = icon;
    }

    /// <summary>The ribbon control (or quick-access item) this entry represents.</summary>
    public FrameworkElement Control { get; }

    /// <summary>Caption shown in the list, e.g. "Home › Font › Bold".</summary>
    public string DisplayName { get; }

    /// <summary>The entry's 16px icon, if the control has one.</summary>
    public ImageSource? Icon { get; }
}

/// <summary>
/// The built-in "Quick Access Toolbar" customization page for
/// <see cref="RibbonOptionsDialog"/> (usable anywhere, but designed for it): the ribbon's
/// commands on the left, the current quick-access items on the right, and
/// Add / Remove / Up / Down between them — like Office's QAT customization.
/// Changes edit <see cref="Ribbon.QuickAccessItems"/> live; the dialog's
/// <see cref="RibbonOptionsDialog.Applied"/> event tells the app when to persist them.
/// </summary>
[TemplatePart(Name = AvailableListPartName, Type = typeof(ListBox))]
[TemplatePart(Name = CurrentListPartName, Type = typeof(ListBox))]
[TemplatePart(Name = AddButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = RemoveButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = UpButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = DownButtonPartName, Type = typeof(ButtonBase))]
public class RibbonQuickAccessPage : Control
{
    private const string AvailableListPartName = "PART_AvailableList";
    private const string CurrentListPartName = "PART_CurrentList";
    private const string AddButtonPartName = "PART_AddButton";
    private const string RemoveButtonPartName = "PART_RemoveButton";
    private const string UpButtonPartName = "PART_UpButton";
    private const string DownButtonPartName = "PART_DownButton";

    /// <summary>Identifies the <see cref="Ribbon"/> dependency property.</summary>
    public static readonly DependencyProperty RibbonProperty =
        DependencyProperty.Register(
            nameof(Ribbon),
            typeof(Ribbon),
            typeof(RibbonQuickAccessPage),
            new FrameworkPropertyMetadata(null, OnRibbonChanged));

    private ListBox? _availableList;
    private ListBox? _currentList;
    private ButtonBase? _addButton;
    private ButtonBase? _removeButton;
    private ButtonBase? _upButton;
    private ButtonBase? _downButton;
    private Ribbon? _observedRibbon;

    static RibbonQuickAccessPage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonQuickAccessPage),
            new FrameworkPropertyMetadata(typeof(RibbonQuickAccessPage)));
    }

    /// <summary>Initializes the page; list contents build once it loads.</summary>
    public RibbonQuickAccessPage()
    {
        Loaded += (_, _) => RebuildBothLists();
        Unloaded += (_, _) => ObserveRibbon(null);
    }

    /// <summary>The ribbon whose commands and quick-access items this page edits.</summary>
    public Ribbon? Ribbon
    {
        get => (Ribbon?)GetValue(RibbonProperty);
        set => SetValue(RibbonProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_addButton is not null)
        {
            _addButton.Click -= OnAddClick;
        }

        if (_removeButton is not null)
        {
            _removeButton.Click -= OnRemoveClick;
        }

        if (_upButton is not null)
        {
            _upButton.Click -= OnUpClick;
        }

        if (_downButton is not null)
        {
            _downButton.Click -= OnDownClick;
        }

        base.OnApplyTemplate();

        _availableList = GetTemplateChild(AvailableListPartName) as ListBox;
        _currentList = GetTemplateChild(CurrentListPartName) as ListBox;
        _addButton = GetTemplateChild(AddButtonPartName) as ButtonBase;
        _removeButton = GetTemplateChild(RemoveButtonPartName) as ButtonBase;
        _upButton = GetTemplateChild(UpButtonPartName) as ButtonBase;
        _downButton = GetTemplateChild(DownButtonPartName) as ButtonBase;

        if (_addButton is not null)
        {
            _addButton.Click += OnAddClick;
        }

        if (_removeButton is not null)
        {
            _removeButton.Click += OnRemoveClick;
        }

        if (_upButton is not null)
        {
            _upButton.Click += OnUpClick;
        }

        if (_downButton is not null)
        {
            _downButton.Click += OnDownClick;
        }

        RebuildBothLists();
    }

    private static void OnRibbonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RibbonQuickAccessPage)d).RebuildBothLists();

    private void RebuildBothLists()
    {
        ObserveRibbon(Ribbon);
        RebuildAvailableList();
        RebuildCurrentList();
    }

    // Follow the ribbon's QuickAccessItems while this page is in use, so external changes
    // (e.g. a right-click "Add to Quick Access Toolbar" while the dialog is open) show up.
    private void ObserveRibbon(Ribbon? ribbon)
    {
        if (ReferenceEquals(_observedRibbon, ribbon))
        {
            return;
        }

        if (_observedRibbon is not null)
        {
            ((INotifyCollectionChanged)_observedRibbon.QuickAccessItems).CollectionChanged -= OnQuickAccessItemsChanged;
        }

        _observedRibbon = ribbon;

        if (_observedRibbon is not null)
        {
            ((INotifyCollectionChanged)_observedRibbon.QuickAccessItems).CollectionChanged += OnQuickAccessItemsChanged;
        }
    }

    private void OnQuickAccessItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        RebuildCurrentList();

    private void RebuildAvailableList()
    {
        if (_availableList is null)
        {
            return;
        }

        var entries = new ObservableCollection<RibbonCommandEntry>();
        if (Ribbon is { } ribbon)
        {
            foreach (RibbonTab tab in ribbon.Tabs)
            {
                string tabName = tab.Header?.ToString() ?? "Tab";
                foreach (RibbonGroup group in tab.Groups)
                {
                    string groupName = group.Header?.ToString() ?? "Group";
                    foreach (object item in group.Items)
                    {
                        CollectCommands(item as DependencyObject, $"{tabName} › {groupName}", entries, depth: 0);
                    }
                }
            }
        }

        _availableList.ItemsSource = entries;
    }

    // Groups host arbitrary content (stack panels, grids…), so commandable controls are
    // found by walking the logical tree. Depth-capped defensively; popup content (menu
    // items, gallery tiles) is never reached because those types aren't descended into.
    private static void CollectCommands(
        DependencyObject? node, string path, ObservableCollection<RibbonCommandEntry> entries, int depth)
    {
        if (node is null || depth > 6)
        {
            return;
        }

        switch (node)
        {
            case RibbonToggleButton toggle:
                entries.Add(new RibbonCommandEntry(
                    toggle, $"{path} › {Caption(toggle.Header, toggle.ScreenTipTitle)}", toggle.Icon ?? toggle.LargeIcon));
                return;
            case RibbonButton button:
                entries.Add(new RibbonCommandEntry(
                    button, $"{path} › {Caption(button.Header, button.ScreenTipTitle)}", button.Icon ?? button.LargeIcon));
                return;
            // Covers RibbonSplitButton too (it derives from RibbonDropDownButton). Added as a
            // whole; the proxy invokes the primary action (split) or opens the menu (dropdown).
            case RibbonDropDownButton dropDown:
                entries.Add(new RibbonCommandEntry(
                    dropDown, $"{path} › {Caption(dropDown.Header, dropDown.ScreenTipTitle)}", dropDown.Icon ?? dropDown.LargeIcon));
                return;
        }

        foreach (object child in LogicalTreeHelper.GetChildren(node))
        {
            CollectCommands(child as DependencyObject, path, entries, depth + 1);
        }
    }

    private void RebuildCurrentList()
    {
        if (_currentList is null)
        {
            return;
        }

        var entries = new ObservableCollection<RibbonCommandEntry>();
        if (Ribbon is { } ribbon)
        {
            foreach (object item in ribbon.QuickAccessItems)
            {
                if (item is FrameworkElement element)
                {
                    entries.Add(DescribeQuickAccessItem(element));
                }
            }
        }

        int selected = _currentList.SelectedIndex;
        _currentList.ItemsSource = entries;
        if (selected >= 0 && selected < entries.Count)
        {
            _currentList.SelectedIndex = selected;
        }
    }

    private static RibbonCommandEntry DescribeQuickAccessItem(FrameworkElement element)
    {
        // Proxies added via AddToQuickAccess carry a reference to their source control —
        // describe the source (its header/icon). Hand-declared QAT items describe themselves.
        FrameworkElement subject = RibbonKit.Controls.Ribbon.GetQuickAccessSource(element) ?? element;
        (string? header, string? tipTitle, ImageSource? icon) = subject switch
        {
            RibbonToggleButton t => (t.Header, t.ScreenTipTitle, t.Icon ?? t.LargeIcon),
            RibbonButton b => (b.Header, b.ScreenTipTitle, b.Icon ?? b.LargeIcon),
            RibbonDropDownButton d => (d.Header, d.ScreenTipTitle, d.Icon ?? d.LargeIcon),
            _ => (null, null, null),
        };

        return new RibbonCommandEntry(element, Caption(header, tipTitle), icon);
    }

    private static string Caption(string? header, string? screenTipTitle) =>
        !string.IsNullOrWhiteSpace(header) ? header
        : !string.IsNullOrWhiteSpace(screenTipTitle) ? screenTipTitle
        : "(command)";

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (Ribbon is { } ribbon && _availableList?.SelectedItem is RibbonCommandEntry entry)
        {
            ribbon.AddToQuickAccess(entry.Control);
        }
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (Ribbon is { } ribbon && _currentList?.SelectedItem is RibbonCommandEntry entry)
        {
            ribbon.QuickAccessItems.Remove(entry.Control);
        }
    }

    private void OnUpClick(object sender, RoutedEventArgs e) => MoveSelected(-1);

    private void OnDownClick(object sender, RoutedEventArgs e) => MoveSelected(+1);

    private void MoveSelected(int delta)
    {
        if (Ribbon is not { } ribbon || _currentList?.SelectedItem is not RibbonCommandEntry entry)
        {
            return;
        }

        int index = ribbon.QuickAccessItems.IndexOf(entry.Control);
        int target = index + delta;
        if (index < 0 || target < 0 || target >= ribbon.QuickAccessItems.Count)
        {
            return;
        }

        ribbon.QuickAccessItems.Move(index, target);

        // The move rebuilt the list; keep the moved item selected so repeated
        // Up/Down clicks keep walking it through the toolbar.
        _currentList.SelectedIndex = target;
    }
}
