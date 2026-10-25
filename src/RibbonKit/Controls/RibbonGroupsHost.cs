using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace RibbonKit.Controls;

/// <summary>
/// The ItemsControl that hosts a tab's groups on the adaptive
/// <see cref="Layout.RibbonGroupsPanel"/>. Created by <see cref="RibbonTab"/>;
/// not intended for direct use.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
[System.ComponentModel.DesignTimeVisible(false)]
public class RibbonGroupsHost : ItemsControl
{
    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonKit.Automation.RibbonGroupsHostAutomationPeer(this);

    private RibbonQuickAccessToolBar? _simplified;
    private readonly Dictionary<FrameworkElement, FrameworkElement> _copies = new();
    private readonly Dictionary<RibbonGroup, RibbonGroupQuickAccessProxy> _groups = new();
    private readonly List<RibbonGroupSeparator> _separators = new();
    private DispatcherOperation? _refreshOperation;
    private bool _refreshing;

    /// <summary>Initializes a groups host that releases borrowed content when disconnected.</summary>
    public RibbonGroupsHost()
    {
        Loaded += (_, _) =>
        {
            if (_simplified?.ItemsSource is null) RequestSimplifiedRefresh();
        };
        Unloaded += (_, _) => ClearSimplified();
    }

    internal RibbonQuickAccessToolBar? SimplifiedToolBar => _simplified;

    static RibbonGroupsHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonGroupsHost),
            new FrameworkPropertyMetadata(typeof(RibbonGroupsHost)));
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        ClearSimplified();
        base.OnApplyTemplate();
        _simplified = GetTemplateChild("PART_SimplifiedToolBar") as RibbonQuickAccessToolBar;
        if (_simplified is not null) _simplified.IsSimplified = true;
        RequestSimplifiedRefresh();
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        RequestSimplifiedRefresh();
    }

    internal void RequestSimplifiedRefresh()
    {
        if (_refreshOperation is not null) return;
        InvalidateMeasure();
        _refreshOperation = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            _refreshOperation = null;
            RefreshSimplified();
        }));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        // A tab's first measure must already include its command copies. Waiting
        // for Background after Loaded exposes an empty row for a render frame.
        // Open flyouts keep the deferred path so borrowed content is never
        // returned during an invocation that forced layout synchronously.
        if (!_refreshing && _simplified is not null
            && (_refreshOperation is not null || _simplified.ItemsSource is null)
            && Ribbon.GetLayoutMode(this) == RibbonLayoutMode.Simplified
            && !PopupDismissHelper.HasOpenPopupWithin(this))
        {
            _refreshOperation?.Abort();
            _refreshOperation = null;
            RefreshSimplified(allowUnloaded: true);
        }
        return base.MeasureOverride(constraint);
    }

    private void RefreshSimplified(bool allowUnloaded = false)
    {
        if (_refreshing) return;
        _refreshing = true;
        try { BuildSimplified(allowUnloaded); }
        finally
        {
            _refreshing = false;
            UIElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        }
    }

    private void BuildSimplified(bool allowUnloaded)
    {
        if (_simplified is null) return;
        if (Ribbon.GetLayoutMode(this) != RibbonLayoutMode.Simplified || !allowUnloaded && !IsLoaded)
        {
            ClearSimplified();
            return;
        }
        Ribbon? owner = null;
        for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node))
            if (node is Ribbon ribbon) { owner = ribbon; break; }
        if (owner is null) return;
        _simplified.Owner = owner;
        _simplified.CloseOverflow();
        var items = new List<FrameworkElement>();
        var retained = new HashSet<FrameworkElement>();
        RibbonTab? tab = owner.Tabs.FirstOrDefault(tab => ReferenceEquals(tab.Content, this));
        if (tab is null) return;
        var sources = Ribbon.SimplifiedSources(tab);
        var choices = owner.GetSimplifiedRow(tab);
        RibbonGroup? previous = null;
        _separators.Clear();
        _simplified.SimplifiedGroups.Clear();
        foreach (var choice in choices)
        {
            FrameworkElement source = choice.Source;
            var entry = Ribbon.ResolveSimplifiedChoice(sources, choice);
            if (entry is null) continue;
            RibbonGroup group = entry.Group;
            if (group != previous)
            {
                var separator = new RibbonGroupSeparator();
                separator.SetValue(RibbonSimplifiedRow.UseMetricsProperty, true);
                ((IRibbonSizeAware)separator).ApplySizeState(RibbonGroupSizeState.Small);
                separator.SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = group });
                _separators.Add(separator);
                items.Add(separator);
                _simplified.SimplifiedGroups[separator] = group;
            }
            previous = group;
            retained.Add(entry.Declaration);
            if (!_copies.TryGetValue(entry.Declaration, out FrameworkElement? copy))
            {
                bool originalEditor = source is RibbonComboBox && ReferenceEquals(source, entry.Declaration)
                    && !Ribbon.GetIsCustom(group) && !Ribbon.GetIsCustom(tab);
                copy = owner.CreateCommandProxy(source, choice.Size, originalEditor);
                if (originalEditor) copy.SetValue(RibbonSimplifiedRow.UseMetricsProperty, true);
                copy.SetBinding(KeyTip.KeysProperty, new Binding { Source = source, Path = new PropertyPath(KeyTip.KeysProperty) });
                _copies.Add(entry.Declaration, copy);
            }
            copy.SetBinding(VisibilityProperty, VisibilityBinding(entry.Declaration, group));
            copy.SetValue(Ribbon.SimplifiedCommandGroupProperty, group);
            Ribbon.MirrorEnabledState(copy, entry.Declaration, group);
            if (!ReferenceEquals(source, entry.Declaration))
            {
                Ribbon.MirrorCommandPresentation(copy, entry.Declaration);
            }
            switch (copy)
            {
                case RibbonButton button: button.SetCurrentValue(RibbonButton.SizeProperty, choice.Size); break;
                case RibbonToggleButton toggle: toggle.SetCurrentValue(RibbonToggleButton.SizeProperty, choice.Size); break;
                case RibbonDropDownButton dropdown: dropdown.SetCurrentValue(RibbonDropDownButton.SizeProperty, choice.Size); break;
            }
            copy.SetCurrentValue(Ribbon.SimplifiedPriorityProperty, Ribbon.GetSimplifiedPriority(entry.Declaration));
            items.Add(copy);
            _simplified.SimplifiedGroups[copy] = group;
        }
        foreach (RibbonGroup group in Items.OfType<RibbonGroup>())
        {
            if (!_groups.TryGetValue(group, out RibbonGroupQuickAccessProxy? opener))
            {
                opener = (RibbonGroupQuickAccessProxy)owner.CreateCommandProxy(group, RibbonControlSize.Medium);
                opener.SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = group });
                _groups.Add(group, opener);
                group.RetainQuickAccess(opener);
            }
            items.Add(opener);
            _simplified.SimplifiedGroups[opener] = group;
        }
        foreach (FrameworkElement source in _copies.Keys.Where(source => !retained.Contains(source)).ToArray())
        {
            CloseCopy(_copies[source]);
            _simplified.SimplifiedGroups.Remove(_copies[source]);
            _copies.Remove(source);
        }
        foreach (RibbonGroup group in _groups.Keys.Where(group => !Items.Contains(group)).ToArray())
        {
            group.ReleaseQuickAccess(_groups[group]);
            _simplified.SimplifiedGroups.Remove(_groups[group]);
            _groups.Remove(group);
        }
        _simplified.ItemsSource = items;
        _simplified.InvalidateMeasure();
    }

    // Walk application content, never template chrome or dropdown/gallery choices.
    internal static IEnumerable<FrameworkElement> Commands(DependencyObject parent)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is not DependencyObject node) continue;
            if (node is RibbonButton or RibbonToggleButton or RibbonDropDownButton or RibbonComboBox or RibbonGallery)
                yield return (FrameworkElement)node;
            else if (node is Panel or Decorator or ContentControl or RibbonGroup)
                foreach (FrameworkElement command in Commands(node)) yield return command;
        }
    }

    private static MultiBinding VisibilityBinding(FrameworkElement source, RibbonGroup group)
    {
        var binding = new MultiBinding { Converter = SimplifiedVisibilityConverter.Instance };
        for (DependencyObject? node = source; node is not null; node = LogicalTreeHelper.GetParent(node))
        {
            if (node is UIElement element) binding.Bindings.Add(new Binding(nameof(Visibility)) { Source = element });
            if (ReferenceEquals(node, group)) break;
        }
        return binding;
    }

    private void ClearSimplified()
    {
        _simplified?.CloseOverflow();
        foreach (FrameworkElement copy in _copies.Values) CloseCopy(copy);
        foreach (var pair in _groups) pair.Key.ReleaseQuickAccess(pair.Value);
        _copies.Clear();
        _groups.Clear();
        _separators.Clear();
        if (_simplified is not null)
        {
            _simplified.ItemsSource = null;
            _simplified.SimplifiedGroups.Clear();
        }
    }

    private static void CloseCopy(FrameworkElement copy)
    {
        if (copy is RibbonComboBoxSimplifiedProxy editor) editor.Release();
        if (copy is RibbonDropDownButton menu) menu.EnsureBorrowedItemsReturned();
    }

    private sealed class SimplifiedVisibilityConverter : IMultiValueConverter
    {
        internal static readonly SimplifiedVisibilityConverter Instance = new();
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            values.All(value => value is Visibility.Visible) ? Visibility.Visible : Visibility.Collapsed;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
