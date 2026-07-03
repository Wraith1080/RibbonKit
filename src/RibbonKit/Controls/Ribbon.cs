using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonAutomationPeer = RibbonKit.Automation.RibbonAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// The root Ribbon control. Hosts the tab strip and the groups row of the selected tab.
/// Declare tabs directly as content:
/// <code language="xaml">
/// &lt;rk:Ribbon&gt;
///     &lt;rk:RibbonTab Header="Home"&gt; ... &lt;/rk:RibbonTab&gt;
/// &lt;/rk:Ribbon&gt;
/// </code>
/// </summary>
[ContentProperty(nameof(Tabs))]
public class Ribbon : Control
{
    private static readonly DependencyPropertyKey TabsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(Tabs),
            typeof(ObservableCollection<RibbonTab>),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="Tabs"/> dependency property.</summary>
    public static readonly DependencyProperty TabsProperty = TabsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey QuickAccessItemsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(QuickAccessItems),
            typeof(ObservableCollection<object>),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="QuickAccessItems"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessItemsProperty =
        QuickAccessItemsPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="QuickAccessPosition"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessPositionProperty =
        DependencyProperty.Register(
            nameof(QuickAccessPosition),
            typeof(RibbonQuickAccessPosition),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(RibbonQuickAccessPosition.TabRow));

    /// <summary>Identifies the <see cref="IsMinimized"/> dependency property.</summary>
    public static readonly DependencyProperty IsMinimizedProperty =
        DependencyProperty.Register(
            nameof(IsMinimized),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="IsBackstageOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackstageOpenProperty =
        DependencyProperty.Register(
            nameof(IsBackstageOpen),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsBackstageOpenChanged));

    /// <summary>Identifies the <see cref="Backstage"/> dependency property.</summary>
    public static readonly DependencyProperty BackstageProperty =
        DependencyProperty.Register(
            nameof(Backstage),
            typeof(object),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null, OnBackstageChanged));

    /// <summary>Identifies the <see cref="ApplicationButtonHeader"/> dependency property.</summary>
    public static readonly DependencyProperty ApplicationButtonHeaderProperty =
        DependencyProperty.Register(
            nameof(ApplicationButtonHeader),
            typeof(string),
            typeof(Ribbon),
            new FrameworkPropertyMetadata("File"));

    /// <summary>Identifies the <see cref="SelectedTab"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTabProperty =
        DependencyProperty.Register(
            nameof(SelectedTab),
            typeof(RibbonTab),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    static Ribbon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Ribbon),
            new FrameworkPropertyMetadata(typeof(Ribbon)));
    }

    /// <summary>Initializes a new ribbon with an empty <see cref="Tabs"/> collection.</summary>
    public Ribbon()
    {
        var tabs = new ObservableCollection<RibbonTab>();
        tabs.CollectionChanged += OnTabsCollectionChanged;
        SetValue(TabsPropertyKey, tabs);
        SetValue(QuickAccessItemsPropertyKey, new ObservableCollection<object>());
        Loaded += OnLoaded;
    }

    /// <summary>The tabs hosted by this ribbon.</summary>
    public ObservableCollection<RibbonTab> Tabs =>
        (ObservableCollection<RibbonTab>)GetValue(TabsProperty);

    /// <summary>
    /// Small controls (typically <see cref="RibbonButton"/>s with
    /// <c>Size="Small"</c>) shown in the quick access strip next to the application
    /// button — Save/Undo/Redo territory. Moves into the title bar once RibbonWindow
    /// chrome integration lands.
    /// </summary>
    public ObservableCollection<object> QuickAccessItems =>
        (ObservableCollection<object>)GetValue(QuickAccessItemsProperty);

    /// <summary>
    /// Where <see cref="QuickAccessItems"/> render: in the tab strip row (default) or
    /// in a full-width row below the ribbon, like classic Office's
    /// "Show Quick Access Toolbar below the Ribbon".
    /// </summary>
    public RibbonQuickAccessPosition QuickAccessPosition
    {
        get => (RibbonQuickAccessPosition)GetValue(QuickAccessPositionProperty);
        set => SetValue(QuickAccessPositionProperty, value);
    }

    /// <summary>
    /// Whether the ribbon is minimized to just its tab strip. Toggled by double-clicking
    /// a tab header or the chevron button at the right end of the tab strip.
    /// </summary>
    public bool IsMinimized
    {
        get => (bool)GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    /// <summary>The currently selected tab, or <see langword="null"/> when none is selected.</summary>
    public RibbonTab? SelectedTab
    {
        get => (RibbonTab?)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    /// <summary>Whether the backstage overlay is open.</summary>
    public bool IsBackstageOpen
    {
        get => (bool)GetValue(IsBackstageOpenProperty);
        set => SetValue(IsBackstageOpenProperty, value);
    }

    /// <summary>
    /// The backstage content opened by the application (File) button — typically a
    /// <see cref="Controls.Backstage"/>. When <see langword="null"/>, the File button
    /// is hidden.
    /// </summary>
    public object? Backstage
    {
        get => GetValue(BackstageProperty);
        set => SetValue(BackstageProperty, value);
    }

    /// <summary>Text of the application button. Default: "File".</summary>
    public string ApplicationButtonHeader
    {
        get => (string)GetValue(ApplicationButtonHeaderProperty);
        set => SetValue(ApplicationButtonHeaderProperty, value);
    }

    private BackstageAdorner? _backstageAdorner;

    private static void OnIsBackstageOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Ribbon)d).UpdateBackstageOverlay((bool)e.NewValue);
    }

    private void UpdateBackstageOverlay(bool open)
    {
        // Office hides title-bar quick access content while the backstage is open;
        // the overlay only covers the window CONTENT, so the title bar must opt in.
        if (Window.GetWindow(this) is RibbonWindow ribbonWindow)
        {
            ribbonWindow.SetCurrentValue(RibbonWindow.IsTitleBarContentVisibleProperty, !open);
        }

        if (open)
        {
            if (_backstageAdorner is not null || Backstage is not UIElement content)
            {
                return;
            }

            // Host the backstage in the window's adorner layer so the overlay lives
            // INSIDE the window (follows moves, minimize, resize) — a Popup would be
            // its own top-level window and do none of those things.
            if (Window.GetWindow(this)?.Content is not UIElement root
                || AdornerLayer.GetAdornerLayer(root) is not { } layer)
            {
                return;
            }

            _backstageAdorner = new BackstageAdorner(root, content);
            layer.Add(_backstageAdorner);

            if (content is FrameworkElement element)
            {
                element.Focusable = true;
                element.Focus(); // So Esc works immediately.
            }
        }
        else if (_backstageAdorner is not null)
        {
            AdornerLayer.GetAdornerLayer(_backstageAdorner.AdornedElement)?.Remove(_backstageAdorner);
            _backstageAdorner.Detach();
            _backstageAdorner = null;
        }
    }

    private static void OnBackstageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (e.OldValue is Backstage oldBackstage)
        {
            oldBackstage.BackRequested -= ribbon.OnBackstageBackRequested;
        }

        if (e.NewValue is Backstage newBackstage)
        {
            newBackstage.BackRequested += ribbon.OnBackstageBackRequested;
        }
    }

    private void OnBackstageBackRequested(object? sender, EventArgs e) =>
        SetCurrentValue(IsBackstageOpenProperty, false);

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonAutomationPeer(this);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureSelection();
    }

    private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Track visibility so selection can escape a contextual tab that hides.
        if (e.OldItems is not null)
        {
            foreach (RibbonTab tab in e.OldItems)
            {
                tab.IsVisibleChanged -= OnTabIsVisibleChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (RibbonTab tab in e.NewItems)
            {
                tab.IsVisibleChanged += OnTabIsVisibleChanged;
            }
        }

        // Keep the selection valid as tabs come and go.
        if (SelectedTab is not null && !Tabs.Contains(SelectedTab))
        {
            SelectedTab = null;
        }

        if (IsLoaded)
        {
            EnsureSelection();
        }
    }

    private void OnTabIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // A selected tab that becomes invisible (a contextual tab deactivating)
        // hands selection to the first visible tab.
        if (sender is RibbonTab { IsVisible: false } tab && ReferenceEquals(SelectedTab, tab))
        {
            SelectedTab = FindFirstVisibleTab();
        }
    }

    private void EnsureSelection()
    {
        if (SelectedTab is null && Tabs.Count > 0)
        {
            SelectedTab = FindFirstVisibleTab();
        }
    }

    private RibbonTab? FindFirstVisibleTab() =>
        Tabs.FirstOrDefault(tab => tab.Visibility == Visibility.Visible);
}
