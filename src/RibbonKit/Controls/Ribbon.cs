using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using RibbonKit.Animation;
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
            new FrameworkPropertyMetadata(
                RibbonQuickAccessPosition.TabRow,
                OnQuickAccessPositionChanged));

    /// <summary>Identifies the <see cref="IsMinimized"/> dependency property.</summary>
    public static readonly DependencyProperty IsMinimizedProperty =
        DependencyProperty.Register(
            nameof(IsMinimized),
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsMinimizedChanged));

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

    /// <summary>
    /// Attached flag the ribbon sets on a QAT button while it sits on a colored surface
    /// (an accent title bar, or the colored Office 2019 tab strip). The button template
    /// then draws its icon as a white silhouette and uses <see cref="QatHoverBackgroundProperty"/>
    /// for its hover, so the QAT blends with the colored band like Office.
    /// </summary>
    public static readonly DependencyProperty QatOnColoredSurfaceProperty =
        DependencyProperty.RegisterAttached(
            "QatOnColoredSurface",
            typeof(bool),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(false));

    /// <summary>Sets the <see cref="QatOnColoredSurfaceProperty"/> for an element.</summary>
    public static void SetQatOnColoredSurface(DependencyObject element, bool value) =>
        element.SetValue(QatOnColoredSurfaceProperty, value);

    /// <summary>Gets the <see cref="QatOnColoredSurfaceProperty"/> for an element.</summary>
    public static bool GetQatOnColoredSurface(DependencyObject element) =>
        (bool)element.GetValue(QatOnColoredSurfaceProperty);

    /// <summary>Attached hover-background brush used by a QAT button on a colored surface.</summary>
    public static readonly DependencyProperty QatHoverBackgroundProperty =
        DependencyProperty.RegisterAttached(
            "QatHoverBackground",
            typeof(System.Windows.Media.Brush),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(null));

    /// <summary>Sets the <see cref="QatHoverBackgroundProperty"/> for an element.</summary>
    public static void SetQatHoverBackground(DependencyObject element, System.Windows.Media.Brush? value) =>
        element.SetValue(QatHoverBackgroundProperty, value);

    /// <summary>Gets the <see cref="QatHoverBackgroundProperty"/> for an element.</summary>
    public static System.Windows.Media.Brush? GetQatHoverBackground(DependencyObject element) =>
        (System.Windows.Media.Brush?)element.GetValue(QatHoverBackgroundProperty);

    /// <summary>Identifies the <see cref="SelectedTab"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTabProperty =
        DependencyProperty.Register(
            nameof(SelectedTab),
            typeof(RibbonTab),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedTabChanged));

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(Ribbon),
            new FrameworkPropertyMetadata(
                -1,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedIndexChanged));

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
        var quickAccessItems = new ObservableCollection<object>();
        quickAccessItems.CollectionChanged += (_, _) => UpdateQatButtonContext();
        SetValue(QuickAccessItemsPropertyKey, quickAccessItems);
        _keyTipService = new KeyTipService(this);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
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

    /// <summary>
    /// The index of the selected tab within <see cref="Tabs"/> (a convenience over
    /// <see cref="SelectedTab"/>, kept in sync with it in both directions). <c>-1</c> when no
    /// tab is selected. Especially useful for <b>design-time preview</b>: set a design-time-only
    /// <c>d:SelectedIndex="2"</c> on the ribbon to view a specific tab's content on the XAML
    /// designer surface without changing the runtime selection.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
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

    // Design-time-only host for the backstage preview (see UpdateDesignTimeBackstage). The
    // runtime adorner path needs a real Window the XAML designer doesn't provide.
    private Border? _designBackstageHost;

    // Guards the SelectedTab <-> SelectedIndex mirroring so setting one to reflect the other
    // never bounces back and re-enters.
    private bool _syncingSelection;

    // Owns the Alt/F10 KeyTip experience for this ribbon; wires itself to the host
    // window on Loaded. Held so it lives as long as the ribbon.
    private readonly KeyTipService _keyTipService;

    // Quick-access-toolbar placement plumbing. When QuickAccessPosition is TitleBar the
    // items are projected into the host RibbonWindow's TitleBarContent via this host; the
    // shared context menu lets the user move the QAT between placements (like Office).
    private System.Windows.Controls.ItemsControl? _titleBarQatHost;
    private object? _savedTitleBarContent;
    private System.Windows.Controls.ContextMenu? _qatContextMenu;
    private System.Windows.Controls.MenuItem? _qatTitleBarItem;
    private System.Windows.Controls.MenuItem? _qatAboveItem;
    private System.Windows.Controls.MenuItem? _qatBelowItem;

    // Cross-fade plumbing: the nested tab control whose selection changes drive a content
    // cross-fade, and the ribbon body host that fades.
    private RibbonTabControl? _ribbonTabControl;
    private FrameworkElement? _ribbonContentHost;

    // Below-ribbon quick-access bar and the last measured body height, so the bar can glide
    // by that height (staying visible) as the body collapses/expands on minimize/restore.
    private FrameworkElement? _qatBelowHost;
    private double _lastRibbonBodyHeight;

    // Backstage close is animated (slide out), so the adorner is removed only after the
    // exit animation; this guards against a re-open racing the pending removal.
    private bool _backstageClosing;

    private static void OnIsBackstageOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Ribbon)d).UpdateBackstageOverlay((bool)e.NewValue);
    }

    private static void OnIsMinimizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // The ribbon body's visibility is managed here (not by a template trigger) so the
        // body can slide UP + fade OUT before it collapses, and slide DOWN + fade IN when it
        // reappears. Only transform/opacity animate — the row height is never animated, so
        // the window's layout still snaps cleanly once the body is hidden/shown.
        var ribbon = (Ribbon)d;
        ribbon._ribbonContentHost ??= FindDescendantByName(ribbon, "ContentHost");
        if (ribbon._ribbonContentHost is not { } host)
        {
            return;
        }

        // The below-ribbon quick-access bar sits under the body; when the body collapses it
        // would jump up by the body's height. Glide it by that height so it follows the body
        // (staying visible) instead of snapping. Only relevant when the QAT is below.
        FrameworkElement? qat = ribbon.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon
            ? ribbon._qatBelowHost
            : null;

        if ((bool)e.NewValue)
        {
            // Remember the body height (still visible now) so restore can reuse it.
            if (host.ActualHeight > 0d)
            {
                ribbon._lastRibbonBodyHeight = host.ActualHeight;
            }

            // The bar glides UP by the body height in step with the body's fade-out...
            RibbonMotion.AnimateTranslateY(qat, RibbonAnimationAction.RibbonMinimize, 0d, -ribbon._lastRibbonBodyHeight);

            // Minimize: lift the body away, then collapse the row. Resetting the bar's
            // transform in the same step as the collapse keeps it visually stationary.
            RibbonMotion.PlayClose(
                host,
                RibbonAnimationAction.RibbonMinimize,
                RibbonSlideFrom.Top,
                () =>
                {
                    if (ribbon.IsMinimized)
                    {
                        host.Visibility = Visibility.Collapsed;
                        RibbonMotion.Rest(qat);
                    }
                });
        }
        else
        {
            // Restore: show the row and slide + fade the body back in. The bar starts at the
            // minimized (raised) offset and glides DOWN to rest in step with the body — the
            // From value is applied on the same frame as the row appears, so it stays put.
            host.Visibility = Visibility.Visible;
            RibbonMotion.PlayOpen(host, RibbonAnimationAction.RibbonMinimize, RibbonSlideFrom.Top);
            RibbonMotion.AnimateTranslateY(qat, RibbonAnimationAction.RibbonMinimize, -ribbon._lastRibbonBodyHeight, 0d);
        }
    }

    private void UpdateBackstageOverlay(bool open)
    {
        // The XAML designer doesn't host the ribbon in a real Window, so the runtime adorner
        // path below (which needs Window.GetWindow) can't run — it would silently no-op and the
        // backstage would never appear on the surface. In design mode, route the backstage into
        // an in-template host instead so its content is visible and editable while designing.
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
        {
            UpdateDesignTimeBackstage(open);
            return;
        }

        // Office hides title-bar quick access content while the backstage is open;
        // the overlay only covers the window CONTENT, so the title bar must opt in.
        if (Window.GetWindow(this) is RibbonWindow ribbonWindow)
        {
            ribbonWindow.SetCurrentValue(RibbonWindow.IsTitleBarContentVisibleProperty, !open);
        }

        if (open)
        {
            _backstageClosing = false;

            // Reopening while a close animation is still running: reuse the existing overlay
            // and just replay the entrance (never create a second adorner for the same
            // content, which a single UIElement can't have two of).
            if (_backstageAdorner is not null)
            {
                if (Backstage is FrameworkElement reopening)
                {
                    reopening.Focus();
                    RibbonMotion.PlayOpen(reopening, RibbonAnimationAction.Backstage, RibbonSlideFrom.Left);
                }

                return;
            }

            if (Backstage is not UIElement content)
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

                // Slide the backstage overlay in from the left (honors the global animation level).
                RibbonMotion.PlayOpen(element, RibbonAnimationAction.Backstage, RibbonSlideFrom.Left);
            }
        }
        else if (_backstageAdorner is not null && !_backstageClosing)
        {
            // Slide the backstage back out to the left (mirroring the entrance), then remove
            // the adorner once the exit animation finishes.
            _backstageClosing = true;
            BackstageAdorner adorner = _backstageAdorner;
            FrameworkElement? content = Backstage as FrameworkElement;

            RibbonMotion.PlayClose(
                content,
                RibbonAnimationAction.Backstage,
                RibbonSlideFrom.Left,
                () =>
                {
                    // A re-open may have cancelled the close mid-flight; only tear down if
                    // we're still closing.
                    if (!_backstageClosing)
                    {
                        return;
                    }

                    AdornerLayer.GetAdornerLayer(adorner.AdornedElement)?.Remove(adorner);
                    adorner.Detach();
                    _backstageAdorner = null;
                    _backstageClosing = false;
                    RibbonMotion.Rest(content);
                });
        }
    }

    /// <summary>
    /// Design-time-only backstage rendering. Hosts the <see cref="Backstage"/> content directly
    /// in the ribbon template's <c>PART_DesignBackstageHost</c> (no window, no adorner layer, no
    /// animation), so it shows and can be edited on the XAML designer surface. Only ever called
    /// under <see cref="System.ComponentModel.DesignerProperties.GetIsInDesignMode"/>; the runtime
    /// path is untouched. Safe to parent the element here because the runtime adorner path is
    /// skipped in design mode, so the backstage is not hosted anywhere else.
    /// </summary>
    private void UpdateDesignTimeBackstage(bool open)
    {
        if (_designBackstageHost is null)
        {
            return;
        }

        if (open && Backstage is UIElement content)
        {
            _designBackstageHost.Child = content;
            _designBackstageHost.Visibility = Visibility.Visible;
        }
        else
        {
            _designBackstageHost.Child = null;
            _designBackstageHost.Visibility = Visibility.Collapsed;
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

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Right-clicking either in-ribbon QAT host opens the placement menu.
        System.Windows.Controls.ContextMenu menu = EnsureQatContextMenu();
        if (GetTemplateChild("QatTabRowHost") is FrameworkElement tabRowHost)
        {
            tabRowHost.ContextMenu = menu;
        }

        if (GetTemplateChild("QatBelowHost") is FrameworkElement belowHost)
        {
            _qatBelowHost = belowHost;
            belowHost.ContextMenu = menu;
        }

        _designBackstageHost = GetTemplateChild("PART_DesignBackstageHost") as Border;

        // In the designer the runtime adorner path can't run (no host Window). If the backstage
        // was already flagged open before the template was applied, reflect it into the
        // design-time host now that the host exists.
        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
        {
            UpdateDesignTimeBackstage(IsBackstageOpen);
        }

        UpdateQuickAccessPlacement();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureSelection();

        // The tab-row QAT host lives in the nested RibbonTabControl's template, so it isn't
        // reachable via this control's GetTemplateChild — find it in the realized visual
        // tree (available by Loaded) and give it the same placement menu.
        if (FindDescendantByName(this, "QatTabRowHost") is { ContextMenu: null } tabRowHost)
        {
            tabRowHost.ContextMenu = EnsureQatContextMenu();
        }

        // React to accent / colored-title-bar / theme changes so the QAT icons + hover keep
        // matching their surface. Re-hook defensively (Loaded can fire more than once).
        Theming.ThemeManager.Changed -= OnThemeConfigurationChanged;
        Theming.ThemeManager.Changed += OnThemeConfigurationChanged;

        // Subscribe to the nested tab control's selection so switching tabs can cross-fade
        // the ribbon body (the control lives in the RibbonTabControl template, not ours).
        if (_ribbonTabControl is null && FindDescendantByType<RibbonTabControl>(this) is { } tabControl)
        {
            _ribbonTabControl = tabControl;
            tabControl.SelectionChanged += OnRibbonTabSelectionChanged;
        }

        // Visibility of the ribbon body is code-managed (see OnIsMinimizedChanged); sync it
        // to the current state in case the ribbon loaded already minimized.
        _ribbonContentHost ??= FindDescendantByName(this, "ContentHost");
        if (_ribbonContentHost is not null)
        {
            _ribbonContentHost.Visibility = IsMinimized ? Visibility.Collapsed : Visibility.Visible;
        }

        UpdateQuickAccessPlacement();
        UpdateQatButtonContext();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Theming.ThemeManager.Changed -= OnThemeConfigurationChanged;
        if (_ribbonTabControl is not null)
        {
            _ribbonTabControl.SelectionChanged -= OnRibbonTabSelectionChanged;
            _ribbonTabControl = null;
        }
    }

    private void OnRibbonTabSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Only the tab strip's own selection counts — ignore selection bubbling up from
        // galleries, combo boxes, or the backstage nested inside a tab's content.
        if (!ReferenceEquals(e.OriginalSource, _ribbonTabControl) || IsMinimized)
        {
            return;
        }

        _ribbonContentHost ??= FindDescendantByName(this, "ContentHost");
        FrameworkElement? target = (_ribbonContentHost as System.Windows.Controls.Border)?.Child as FrameworkElement
            ?? _ribbonContentHost;
        // Slide (no fade): the new content is already realized at full opacity, so a fade
        // would flash it transparent for a frame — a subtle rise reads cleanly instead.
        RibbonMotion.PlaySlideIn(target, RibbonAnimationAction.TabSwitch, RibbonSlideFrom.Top);
    }

    private void OnThemeConfigurationChanged(object? sender, EventArgs e) => UpdateQatButtonContext();

    /// <summary>
    /// Sets, on each QAT button, whether it currently sits on a colored surface and the
    /// hover brush to use there — so the button template can white-out its icon and match
    /// the surrounding band's hover. Applied directly (not via inheritance) so it is robust
    /// regardless of how the items are hosted.
    /// </summary>
    private void UpdateQatButtonContext()
    {
        bool accentTitleBar = Theming.ThemeManager.IsAccentedTitleBar;
        bool titleBarColored = QuickAccessPosition == RibbonQuickAccessPosition.TitleBar && accentTitleBar;
        bool tabRowColored = QuickAccessPosition == RibbonQuickAccessPosition.TabRow
            && accentTitleBar
            && Theming.ThemeManager.CurrentTheme == Theming.RibbonTheme.Office2019;
        bool colored = titleBarColored || tabRowColored;

        // Match the hover of the neighbouring chrome: the caption buttons in the title bar,
        // the tabs on the strip.
        string? hoverKey = titleBarColored ? "RibbonKit.Brushes.CaptionButton.HoverBackground"
            : tabRowColored ? "RibbonKit.Brushes.Tab.HoverBackground"
            : null;

        foreach (object entry in QuickAccessItems)
        {
            if (entry is not FrameworkElement element)
            {
                continue;
            }

            SetQatOnColoredSurface(element, colored);
            if (colored && hoverKey is not null)
            {
                element.SetResourceReference(QatHoverBackgroundProperty, hoverKey);
            }
            else
            {
                element.ClearValue(QatHoverBackgroundProperty);
            }
        }
    }

    private static FrameworkElement? FindDescendantByName(DependencyObject root, string name)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement element && element.Name == name)
            {
                return element;
            }

            if (FindDescendantByName(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static T? FindDescendantByType<T>(DependencyObject root) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendantByType<T>(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static void OnQuickAccessPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Ribbon)d).UpdateQuickAccessPlacement();

    /// <summary>
    /// Projects the quick-access items into the host <see cref="RibbonWindow"/>'s title
    /// bar when <see cref="QuickAccessPosition"/> is <see cref="RibbonQuickAccessPosition.TitleBar"/>,
    /// and restores the window's prior title-bar content otherwise. Exactly one host binds
    /// the (single-parent) item elements at a time, so the switch reparents them cleanly.
    /// </summary>
    private void UpdateQuickAccessPlacement()
    {
        // The quick-access items are single-parent UIElements shared between hosts, so the
        // OLD host must release them before the NEW one claims them. When leaving the title
        // bar, release synchronously (the title-bar host is higher in the tree, so it frees
        // the items at the next layout before the in-ribbon host — lower — claims them).
        if (QuickAccessPosition != RibbonQuickAccessPosition.TitleBar && _titleBarQatHost is not null)
        {
            _titleBarQatHost.ItemsSource = null;
        }

        // Apply the final placement after a layout pass, so whichever host currently owns
        // the items has released them before we (re)claim — avoids a transient double-parent.
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            (Action)ApplyQuickAccessPlacement);
    }

    private void ApplyQuickAccessPlacement()
    {
        var window = Window.GetWindow(this) as RibbonWindow;

        if (QuickAccessPosition == RibbonQuickAccessPosition.TitleBar && window is not null)
        {
            _titleBarQatHost ??= CreateTitleBarQatHost();

            // Remember whatever the window was showing (unless it's already our host) so we
            // can put it back when the QAT leaves the title bar.
            if (!ReferenceEquals(window.TitleBarContent, _titleBarQatHost))
            {
                _savedTitleBarContent = window.TitleBarContent;
            }

            window.SetCurrentValue(RibbonWindow.TitleBarContentProperty, _titleBarQatHost);
            _titleBarQatHost.ItemsSource = QuickAccessItems;
        }
        else
        {
            if (_titleBarQatHost is not null)
            {
                _titleBarQatHost.ItemsSource = null;
            }

            if (window is not null && ReferenceEquals(window.TitleBarContent, _titleBarQatHost))
            {
                window.SetCurrentValue(RibbonWindow.TitleBarContentProperty, _savedTitleBarContent);
                _savedTitleBarContent = null;
            }
        }

        UpdateQatButtonContext();
    }

    private System.Windows.Controls.ItemsControl CreateTitleBarQatHost()
    {
        var panel = new FrameworkElementFactory(typeof(System.Windows.Controls.StackPanel));
        panel.SetValue(System.Windows.Controls.StackPanel.OrientationProperty, System.Windows.Controls.Orientation.Horizontal);

        return new System.Windows.Controls.ItemsControl
        {
            Focusable = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 6, 0),
            ItemsPanel = new ItemsPanelTemplate(panel),
            ContextMenu = EnsureQatContextMenu(),
        };
    }

    private System.Windows.Controls.ContextMenu EnsureQatContextMenu()
    {
        if (_qatContextMenu is not null)
        {
            return _qatContextMenu;
        }

        _qatTitleBarItem = new System.Windows.Controls.MenuItem { Header = "Show Quick Access Toolbar in the Title Bar" };
        _qatTitleBarItem.Click += (_, _) =>
            SetCurrentValue(QuickAccessPositionProperty, RibbonQuickAccessPosition.TitleBar);

        _qatAboveItem = new System.Windows.Controls.MenuItem { Header = "Show Quick Access Toolbar Above the Ribbon" };
        _qatAboveItem.Click += (_, _) =>
            SetCurrentValue(QuickAccessPositionProperty, RibbonQuickAccessPosition.TabRow);

        _qatBelowItem = new System.Windows.Controls.MenuItem { Header = "Show Quick Access Toolbar Below the Ribbon" };
        _qatBelowItem.Click += (_, _) =>
            SetCurrentValue(QuickAccessPositionProperty, RibbonQuickAccessPosition.BelowRibbon);

        _qatContextMenu = new System.Windows.Controls.ContextMenu();
        _qatContextMenu.Items.Add(_qatTitleBarItem);
        _qatContextMenu.Items.Add(_qatAboveItem);
        _qatContextMenu.Items.Add(_qatBelowItem);
        _qatContextMenu.Opened += OnQatContextMenuOpened;
        return _qatContextMenu;
    }

    private void OnQatContextMenuOpened(object sender, RoutedEventArgs e)
    {
        // Show a check next to the current placement.
        if (_qatTitleBarItem is not null)
        {
            _qatTitleBarItem.IsChecked = QuickAccessPosition == RibbonQuickAccessPosition.TitleBar;
        }

        if (_qatAboveItem is not null)
        {
            _qatAboveItem.IsChecked = QuickAccessPosition == RibbonQuickAccessPosition.TabRow;
        }

        if (_qatBelowItem is not null)
        {
            _qatBelowItem.IsChecked = QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon;
        }
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

        // A SelectedIndex set before its target tab existed (including a design-time
        // d:SelectedIndex applied during tree construction) takes effect once the tab arrives.
        if (SelectedTab is null && SelectedIndex >= 0)
        {
            ApplySelectedIndex(SelectedIndex);
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

    private static void OnSelectedTabChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ribbon = (Ribbon)d;
        if (ribbon._syncingSelection)
        {
            return;
        }

        // Mirror the selected tab back into SelectedIndex so the two stay in lock-step.
        ribbon._syncingSelection = true;
        try
        {
            ribbon.SelectedIndex = e.NewValue is RibbonTab tab ? ribbon.Tabs.IndexOf(tab) : -1;
        }
        finally
        {
            ribbon._syncingSelection = false;
        }
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Ribbon)d).ApplySelectedIndex((int)e.NewValue);

    private void ApplySelectedIndex(int index)
    {
        // Ignore re-entrancy from the SelectedTab mirror, and out-of-range indices — the tabs
        // may not be populated yet (a XAML attribute, or a design-time d:SelectedIndex, is
        // applied before the child tabs are parsed). OnTabsCollectionChanged re-applies a pending
        // index once the tabs exist.
        if (_syncingSelection || index < 0 || index >= Tabs.Count)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            SelectedTab = Tabs[index];
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void EnsureSelection()
    {
        if (SelectedTab is not null || Tabs.Count == 0)
        {
            return;
        }

        // Honor a pending SelectedIndex (e.g. a design-time d:SelectedIndex applied before the
        // tabs were parsed); otherwise fall back to the first visible tab.
        int index = SelectedIndex;
        SelectedTab = index >= 0 && index < Tabs.Count ? Tabs[index] : FindFirstVisibleTab();
    }

    private RibbonTab? FindFirstVisibleTab() =>
        Tabs.FirstOrDefault(tab => tab.Visibility == Visibility.Visible);
}
