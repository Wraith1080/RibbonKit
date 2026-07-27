using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Markup;

namespace RibbonKit.Controls;

/// <summary>
/// A bag of ribbon tabs a child context contributes into a host <see cref="Ribbon"/> while it is
/// active — an embedded document editor, an MDI child, a plug-in. Merge it with
/// <see cref="Ribbon.Merge"/> and take it back with <see cref="Ribbon.Unmerge"/>.
/// </summary>
/// <example>
/// <code language="xaml">
/// &lt;rk:RibbonMergeSource x:Name="ChartTools" Order="10"&gt;
///     &lt;rk:RibbonTab Header="Chart Design" IsContextual="True" ContextualColor="#C43E96"&gt;
///         &lt;rk:RibbonGroup Header="Type"&gt; ... &lt;/rk:RibbonGroup&gt;
///     &lt;/rk:RibbonTab&gt;
/// &lt;/rk:RibbonMergeSource&gt;
/// </code>
/// </example>
/// <remarks>
/// <para>
/// A merge source is a <see cref="FrameworkElement"/> so it can carry a
/// <see cref="FrameworkElement.DataContext"/>: while merged, each of its tabs has its own
/// <c>DataContext</c> bound to the source's, so an MVVM child's bindings keep resolving against
/// the child's view model rather than the host window's. Inherited *visual* properties (font,
/// foreground, flow direction) deliberately still come from the host ribbon, so a merged tab
/// looks like a native one.
/// </para>
/// <para>
/// The source itself is not required to be in a visual tree. Resources referenced by merged tabs
/// therefore resolve through the host ribbon and the application, not through the source — keep
/// them at application scope.
/// </para>
/// <para>
/// Merged tabs are deliberately invisible to ribbon customization: they are excluded from
/// <see cref="RibbonCustomizationSerializer"/> and from the Customize-the-Ribbon tree, because
/// they belong to a transient child and persisting their order or visibility would restore stale
/// state into a ribbon whose source is gone.
/// </para>
/// </remarks>
[ContentProperty(nameof(Tabs))]
public class RibbonMergeSource : FrameworkElement
{
    private static readonly DependencyPropertyKey TabsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Tabs),
            typeof(ObservableCollection<RibbonTab>),
            typeof(RibbonMergeSource),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="Tabs"/> dependency property.</summary>
    public static readonly DependencyProperty TabsProperty = TabsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey GroupsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Groups),
            typeof(ObservableCollection<RibbonGroupContribution>),
            typeof(RibbonMergeSource),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="Groups"/> dependency property.</summary>
    public static readonly DependencyProperty GroupsProperty = GroupsPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="Target"/> dependency property.</summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(Ribbon),
            typeof(RibbonMergeSource),
            new FrameworkPropertyMetadata(null, OnActivationChanged));

    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(RibbonMergeSource),
            new FrameworkPropertyMetadata(false, OnActivationChanged));

    /// <summary>
    /// Identifies the <c>Source</c> attached property: the merge source a child element carries,
    /// so a host that tracks activation (an <see cref="MdiContainer"/>'s active document, a
    /// selection manager) can find the source to merge without knowing the child's type.
    /// </summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached(
            "Source",
            typeof(RibbonMergeSource),
            typeof(RibbonMergeSource),
            new FrameworkPropertyMetadata(null));

    /// <summary>Attaches a merge source to a child element (see <see cref="SourceProperty"/>).</summary>
    public static void SetSource(DependencyObject element, RibbonMergeSource? value) =>
        element.SetValue(SourceProperty, value);

    /// <summary>The merge source attached to a child element, or <see langword="null"/>.</summary>
    public static RibbonMergeSource? GetSource(DependencyObject element) =>
        (RibbonMergeSource?)element.GetValue(SourceProperty);

    /// <summary>Identifies the <see cref="Order"/> dependency property.</summary>
    public static readonly DependencyProperty OrderProperty =
        DependencyProperty.Register(
            nameof(Order),
            typeof(int),
            typeof(RibbonMergeSource),
            new FrameworkPropertyMetadata(0));

    private static readonly DependencyPropertyKey MergedIntoPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(MergedInto),
            typeof(Ribbon),
            typeof(RibbonMergeSource),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="MergedInto"/> dependency property.</summary>
    public static readonly DependencyProperty MergedIntoProperty = MergedIntoPropertyKey.DependencyProperty;

    /// <summary>Initializes a merge source with an empty <see cref="Tabs"/> collection.</summary>
    public RibbonMergeSource()
    {
        SetValue(TabsPropertyKey, new ObservableCollection<RibbonTab>());
        SetValue(GroupsPropertyKey, new ObservableCollection<RibbonGroupContribution>());

        // Nothing to render: a merge source is a declarative container, never laid out itself.
        Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// The tabs this source contributes, in the order they should appear relative to each other.
    /// Modifying the collection while merged is not supported — unmerge, edit, merge again.
    /// </summary>
    public ObservableCollection<RibbonTab> Tabs =>
        (ObservableCollection<RibbonTab>)GetValue(TabsProperty);

    /// <summary>
    /// Groups this source injects into the host's OWN tabs, addressed by the host tab's
    /// <c>Ribbon.CommandId</c>. Independent of <see cref="Tabs"/> — a source can do either or both.
    /// </summary>
    public ObservableCollection<RibbonGroupContribution> Groups =>
        (ObservableCollection<RibbonGroupContribution>)GetValue(GroupsProperty);

    /// <summary>
    /// The ribbon this source merges into when <see cref="IsActive"/> becomes
    /// <see langword="true"/>. Leave unset when driving merges imperatively with
    /// <see cref="Ribbon.Merge"/>.
    /// </summary>
    public Ribbon? Target
    {
        get => (Ribbon?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>
    /// The activation signal: setting it <see langword="true"/> merges this source into
    /// <see cref="Target"/>, setting it <see langword="false"/> unmerges. Bind it to whatever
    /// "my child is active" means in the app — a selection flag, an MDI container's active
    /// document, a view model property.
    /// </summary>
    /// <remarks>
    /// Deliberately <b>not</b> driven by WPF keyboard focus: focus moves to ribbon buttons on
    /// every click, which would thrash the merge (see design notes §3.28 for the equivalent
    /// lesson with a RichTextBox). Activation is the app's concept, not the framework's.
    /// </remarks>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Where this source's tabs sit relative to other merged sources. Sources sort by
    /// <see cref="Order"/> then by the order they were first merged into the host, so a source
    /// that unmerges and merges again returns to the same slot among its peers.
    /// <para>
    /// The host ribbon's own tabs behave as order 0 and come first, so a source with
    /// <see cref="Order"/> ≥ 0 appends after them; a <b>negative</b> order places the source's
    /// tabs <i>before</i> the host's own. Default 0.
    /// </para>
    /// </summary>
    public int Order
    {
        get => (int)GetValue(OrderProperty);
        set => SetValue(OrderProperty, value);
    }

    /// <summary>
    /// The ribbon this source is currently merged into, or <see langword="null"/> when unmerged.
    /// A source can be merged into only one ribbon at a time.
    /// </summary>
    public Ribbon? MergedInto => (Ribbon?)GetValue(MergedIntoProperty);

    /// <summary>Whether this source is currently merged into a ribbon.</summary>
    public bool IsMerged => MergedInto is not null;

    internal void SetMergedInto(Ribbon? ribbon) => SetValue(MergedIntoPropertyKey, ribbon);

    // Target or IsActive moved: reconcile with wherever this source currently is. Written so any
    // combination converges — including Target being retargeted while active, which has to leave
    // the old ribbon before joining the new one.
    private static void OnActivationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var source = (RibbonMergeSource)d;
        Ribbon? desired = source.IsActive ? source.Target : null;
        Ribbon? current = source.MergedInto;

        if (ReferenceEquals(desired, current))
        {
            return;
        }

        current?.Unmerge(source);
        desired?.Merge(source);
    }
}
