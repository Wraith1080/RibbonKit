using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// A ribbon tab. Its header renders in the tab strip; its <see cref="Groups"/> render
/// in the groups row when the tab is selected. Declare groups directly as content:
/// <code language="xaml">
/// &lt;rk:RibbonTab Header="Home"&gt;
///     &lt;rk:RibbonGroup Header="Clipboard"&gt; ... &lt;/rk:RibbonGroup&gt;
/// &lt;/rk:RibbonTab&gt;
/// </code>
/// </summary>
[ContentProperty(nameof(Groups))]
public class RibbonTab : TabItem
{
    private static readonly DependencyPropertyKey GroupsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Groups),
            typeof(ObservableCollection<RibbonGroup>),
            typeof(RibbonTab),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="Groups"/> dependency property.</summary>
    public static readonly DependencyProperty GroupsProperty = GroupsPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="IsContextual"/> dependency property.</summary>
    public static readonly DependencyProperty IsContextualProperty =
        DependencyProperty.Register(
            nameof(IsContextual),
            typeof(bool),
            typeof(RibbonTab),
            new FrameworkPropertyMetadata(false));

    static RibbonTab()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonTab),
            new FrameworkPropertyMetadata(typeof(RibbonTab)));
    }

    /// <summary>Initializes a new tab with an empty <see cref="Groups"/> collection.</summary>
    public RibbonTab()
    {
        var groups = new ObservableCollection<RibbonGroup>();
        SetValue(GroupsPropertyKey, groups);

        // The tab's TabControl content is a host ItemsControl whose panel is the
        // adaptive RibbonGroupsPanel. Users interact only with the Groups collection.
        Content = new RibbonGroupsHost { ItemsSource = groups };
    }

    /// <summary>The groups shown in the ribbon when this tab is selected.</summary>
    public ObservableCollection<RibbonGroup> Groups =>
        (ObservableCollection<RibbonGroup>)GetValue(GroupsProperty);

    /// <summary>
    /// Marks this as a contextual tab (e.g. "Picture Format"): the header renders with
    /// an accent tint. Show and hide it by setting <see cref="UIElement.Visibility"/>
    /// from application state; the ribbon moves selection to the first visible tab
    /// when a selected contextual tab disappears.
    /// </summary>
    public bool IsContextual
    {
        get => (bool)GetValue(IsContextualProperty);
        set => SetValue(IsContextualProperty, value);
    }

    /// <summary>
    /// Double-clicking a tab header toggles ribbon minimize mode, matching Office.
    /// </summary>
    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (e.ChangedButton == MouseButton.Left && FindAncestorRibbon() is { } ribbon)
        {
            ribbon.IsMinimized = !ribbon.IsMinimized;
            e.Handled = true;
        }
    }

    private Ribbon? FindAncestorRibbon()
    {
        DependencyObject? node = this;
        while (node is not null and not Ribbon)
        {
            node = VisualTreeHelper.GetParent(node);
        }

        return node as Ribbon;
    }
}
