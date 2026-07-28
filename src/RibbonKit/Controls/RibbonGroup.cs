using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Layout;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonGroupAutomationPeer = RibbonKit.Automation.RibbonGroupAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// How a group lays out DIRECT command items (the proxies in user-created custom groups —
/// see <see cref="RibbonCustomizePage"/>). Built-in groups keep <see cref="Default"/>:
/// their layout comes from their own hand-composed content, which this property never
/// overrides.
/// </summary>
public enum RibbonGroupLayout
{
    /// <summary>Content-driven — the group's own panels decide. Never forced.</summary>
    Default,

    /// <summary>Items wrap vertically into 3-row columns; each item may be Medium or Small.</summary>
    Stacked,

    /// <summary>One horizontal row of Large buttons; item sizes are locked to Large.</summary>
    Large,
}

/// <summary>
/// A labeled group of controls inside a <see cref="RibbonTab"/>. Renders its items in
/// a row with the group name underneath and a separator on its right edge. When ribbon
/// width runs out, the group collapses to a single button whose flyout shows the full
/// content (see <see cref="ReductionMode"/>).
/// </summary>
/// <remarks>
/// Use <see cref="ReductionPriority"/> to control which groups reduce first,
/// <see cref="ReductionMode"/> to choose between collapsing and in-place control
/// resizing, <see cref="CanResize"/> to exempt the group entirely, and
/// <see cref="Icon"/> for the collapsed button's glyph.
/// </remarks>
[TemplatePart(Name = NormalHostPartName, Type = typeof(Decorator))]
[TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
[TemplatePart(Name = PopupHostPartName, Type = typeof(Border))]
[TemplatePart(Name = CollapsedButtonPartName, Type = typeof(ToggleButton))]
[TemplatePart(Name = DialogLauncherPartName, Type = typeof(ButtonBase))]
public class RibbonGroup : HeaderedItemsControl
{
    private const string NormalHostPartName = "PART_NormalHost";
    private const string PopupPartName = "PART_Popup";
    private const string PopupHostPartName = "PART_PopupHost";
    private const string CollapsedButtonPartName = "PART_CollapsedButton";
    private const string DialogLauncherPartName = "PART_DialogLauncher";

    /// <summary>
    /// Identifies the <see cref="DialogLauncherClick"/> routed event, raised when the
    /// small ↘ launcher in the group's corner is clicked.
    /// </summary>
    public static readonly RoutedEvent DialogLauncherClickEvent = EventManager.RegisterRoutedEvent(
        nameof(DialogLauncherClick),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RibbonGroup));

    private static readonly DependencyPropertyKey SizeStatePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(SizeState),
            typeof(RibbonGroupSizeState),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(
                RibbonGroupSizeState.Large,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnSizeStateChanged));

    /// <summary>Identifies the read-only <see cref="SizeState"/> dependency property.</summary>
    public static readonly DependencyProperty SizeStateProperty = SizeStatePropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="ReductionPriority"/> dependency property.</summary>
    public static readonly DependencyProperty ReductionPriorityProperty =
        DependencyProperty.Register(
            nameof(ReductionPriority),
            typeof(int?),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(null, OnLayoutPolicyChanged));

    /// <summary>Identifies the <see cref="ReductionMode"/> dependency property.</summary>
    public static readonly DependencyProperty ReductionModeProperty =
        DependencyProperty.Register(
            nameof(ReductionMode),
            typeof(RibbonGroupReductionMode),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(RibbonGroupReductionMode.Collapse, OnLayoutPolicyChanged));

    /// <summary>Identifies the <see cref="ShowDialogLauncher"/> dependency property.</summary>
    public static readonly DependencyProperty ShowDialogLauncherProperty =
        DependencyProperty.Register(
            nameof(ShowDialogLauncher),
            typeof(bool),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the <see cref="DialogLauncherCommand"/> dependency property.</summary>
    public static readonly DependencyProperty DialogLauncherCommandProperty =
        DependencyProperty.Register(
            nameof(DialogLauncherCommand),
            typeof(ICommand),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="CanResize"/> dependency property.</summary>
    public static readonly DependencyProperty CanResizeProperty =
        DependencyProperty.Register(
            nameof(CanResize),
            typeof(bool),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(true, OnLayoutPolicyChanged));

    /// <summary>Identifies the <see cref="Layout"/> dependency property.</summary>
    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(
            nameof(Layout),
            typeof(RibbonGroupLayout),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(RibbonGroupLayout.Default, OnGroupLayoutChanged));

    /// <summary>Identifies the <see cref="ContentAlignment"/> dependency property.</summary>
    public static readonly DependencyProperty ContentAlignmentProperty =
        DependencyProperty.Register(
            nameof(ContentAlignment),
            typeof(HorizontalAlignment),
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(HorizontalAlignment.Center));

    private Decorator? _normalHost;
    private Border? _popupHost;
    private Popup? _popup;
    private ToggleButton? _collapsedButton;
    private ButtonBase? _dialogLauncher;
    private PopupDismissHelper? _dismissHelper;

    static RibbonGroup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonGroup),
            new FrameworkPropertyMetadata(typeof(RibbonGroup)));
    }

    /// <summary>The size state currently assigned by the sizing engine.</summary>
    public RibbonGroupSizeState SizeState => (RibbonGroupSizeState)GetValue(SizeStateProperty);

    /// <summary>Raised when the group's ↘ dialog launcher is clicked.</summary>
    public event RoutedEventHandler DialogLauncherClick
    {
        add => AddHandler(DialogLauncherClickEvent, value);
        remove => RemoveHandler(DialogLauncherClickEvent, value);
    }

    /// <summary>
    /// Whether the small ↘ dialog launcher button is shown in the group's corner.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool ShowDialogLauncher
    {
        get => (bool)GetValue(ShowDialogLauncherProperty);
        set => SetValue(ShowDialogLauncherProperty, value);
    }

    /// <summary>Command executed when the dialog launcher is clicked.</summary>
    public ICommand? DialogLauncherCommand
    {
        get => (ICommand?)GetValue(DialogLauncherCommandProperty);
        set => SetValue(DialogLauncherCommandProperty, value);
    }

    /// <summary>
    /// How the group's content row is aligned horizontally when the group is wider than its
    /// content (for example when the group name is wider than a single button). Defaults to
    /// <see cref="HorizontalAlignment.Center"/>, matching Office; set <see cref="HorizontalAlignment.Left"/>
    /// to left-pack a specific group instead. Has no visible effect when the content is the
    /// widest thing in the group.
    /// </summary>
    public HorizontalAlignment ContentAlignment
    {
        get => (HorizontalAlignment)GetValue(ContentAlignmentProperty);
        set => SetValue(ContentAlignmentProperty, value);
    }

    /// <summary>The 32px icon shown on the collapsed group button.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Optional reduction priority. Groups with a priority reduce before groups without
    /// one, highest value first, each fully exhausted before the next. Groups without a
    /// priority then reduce largest-first. <see langword="null"/> (default) means
    /// unprioritized.
    /// </summary>
    public int? ReductionPriority
    {
        get => (int?)GetValue(ReductionPriorityProperty);
        set => SetValue(ReductionPriorityProperty, value);
    }

    /// <summary>
    /// How this group reduces when width runs out. Default:
    /// <see cref="RibbonGroupReductionMode.Collapse"/> — straight to a collapsed button
    /// with a flyout, like modern Office.
    /// </summary>
    public RibbonGroupReductionMode ReductionMode
    {
        get => (RibbonGroupReductionMode)GetValue(ReductionModeProperty);
        set => SetValue(ReductionModeProperty, value);
    }

    /// <summary>
    /// Whether the sizing engine may reduce this group at all. Set to
    /// <see langword="false"/> to keep the group at its full Large layout regardless
    /// of available width. Default is <see langword="true"/>.
    /// </summary>
    public bool CanResize
    {
        get => (bool)GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>
    /// The canned layout for DIRECT command items — meaningful for user-created custom
    /// groups whose items are proxy buttons. Setting <see cref="RibbonGroupLayout.Stacked"/>
    /// or <see cref="RibbonGroupLayout.Large"/> swaps the items panel and normalizes the
    /// items' sizes to what the layout allows. <see cref="RibbonGroupLayout.Default"/>
    /// (the default) never touches anything, so built-in groups are unaffected.
    /// </summary>
    public RibbonGroupLayout Layout
    {
        get => (RibbonGroupLayout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    private static void OnGroupLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RibbonGroup)d).ApplyGroupLayout((RibbonGroupLayout)e.NewValue);

    private void ApplyGroupLayout(RibbonGroupLayout layout)
    {
        if (layout == RibbonGroupLayout.Default)
        {
            return; // Content-driven: never force a panel onto hand-composed content.
        }

        // Panel: Large = one horizontal row; Stacked = vertical wrap into 3-row columns
        // (bounded by the groups-row height, so overflow starts a new column).
        FrameworkElementFactory panel;
        if (layout == RibbonGroupLayout.Large)
        {
            panel = new FrameworkElementFactory(typeof(StackPanel));
            panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
        }
        else
        {
            panel = new FrameworkElementFactory(typeof(WrapPanel));
            panel.SetValue(WrapPanel.OrientationProperty, Orientation.Vertical);
        }

        ItemsPanel = new ItemsPanelTemplate(panel);

        foreach (object item in Items)
        {
            NormalizeItemSize(item, layout);
        }
    }

    /// <summary>
    /// Clamps a direct command item's size to what <paramref name="layout"/> allows:
    /// Large layout forces Large; Stacked demotes Large to Medium but preserves a chosen
    /// Medium/Small.
    /// </summary>
    internal static void NormalizeItemSize(object item, RibbonGroupLayout layout)
    {
        void Apply(RibbonControlSize current, Action<RibbonControlSize> set)
        {
            if (layout == RibbonGroupLayout.Large)
            {
                set(RibbonControlSize.Large);
            }
            else if (current == RibbonControlSize.Large)
            {
                set(RibbonControlSize.Medium);
            }
        }

        switch (item)
        {
            case RibbonButton button:
                Apply(button.Size, s => button.Size = s);
                break;
            case RibbonToggleButton toggle:
                Apply(toggle.Size, s => toggle.Size = s);
                break;
            case RibbonDropDownButton dropDown: // covers RibbonSplitButton
                Apply(dropDown.Size, s => dropDown.Size = s);
                break;
        }
    }

    internal void SetSizeState(RibbonGroupSizeState state) => SetValue(SizeStatePropertyKey, state);

    /// <summary>
    /// The single button shown while the group is collapsed; its flyout hosts the full
    /// content. Exposed for the KeyTip service, which targets it when the group is
    /// collapsed. <see langword="null"/> before the template is applied.
    /// </summary>
    internal ToggleButton? CollapsedButton => _collapsedButton;

    /// <summary>
    /// The group's content once it has re-homed into the collapsed flyout, or
    /// <see langword="null"/> when not collapsed/open. Used by the KeyTip service to
    /// badge the controls inside an open flyout.
    /// </summary>
    internal UIElement? FlyoutContent => _popupHost?.Child;

    /// <summary>
    /// The small ↘ dialog-launcher button, or <see langword="null"/> before the template
    /// is applied. Exposed so the KeyTip service can badge it.
    /// </summary>
    internal ButtonBase? DialogLauncher => _dialogLauncher;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_popup is not null)
        {
            _popup.Opened -= OnPopupOpened;
            _popup.Closed -= OnPopupClosed;
        }

        if (_popupHost is not null)
        {
            _popupHost.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnFlyoutInvoked));
        }

        if (_dialogLauncher is not null)
        {
            _dialogLauncher.Click -= OnDialogLauncherClick;
        }

        base.OnApplyTemplate();

        _normalHost = GetTemplateChild(NormalHostPartName) as Decorator;
        _popupHost = GetTemplateChild(PopupHostPartName) as Border;
        _popup = GetTemplateChild(PopupPartName) as Popup;
        _collapsedButton = GetTemplateChild(CollapsedButtonPartName) as ToggleButton;
        _dialogLauncher = GetTemplateChild(DialogLauncherPartName) as ButtonBase;

        if (_popup is not null)
        {
            _popup.Opened += OnPopupOpened;
            _popup.Closed += OnPopupClosed;
        }

        if (_popupHost is not null)
        {
            _popupHost.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnFlyoutInvoked));
        }

        if (_dialogLauncher is not null)
        {
            _dialogLauncher.Click += OnDialogLauncherClick;
        }

    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGroupAutomationPeer(this);

    private void OnDialogLauncherClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(DialogLauncherClickEvent, this));
    }

    /// <summary>
    /// Tells the hosting sizing panel to re-probe group widths when this group's
    /// content changes at runtime.
    /// </summary>
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        InvalidateHostPanel();
    }

    private static void OnLayoutPolicyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((RibbonGroup)d).InvalidateHostPanel();
    }

    private static void OnSizeStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var group = (RibbonGroup)d;
        var state = (RibbonGroupSizeState)e.NewValue;

        group.ApplyStateRecursive(group, state);

        // Defeat WPF's measure short-circuiting: intermediate elements (ItemsPresenter,
        // panels) are not dirtied by a descendant's property change, so a synchronous
        // re-measure of the group would return STALE sizes. Invalidating the whole
        // subtree guarantees the sizing engine's probe reads true per-state widths.
        InvalidateMeasureRecursive(group);

        // Growing back while the flyout is open: close it (content re-homes to the ribbon).
        if (state != RibbonGroupSizeState.Collapsed && group._collapsedButton is not null)
        {
            group._collapsedButton.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        // Light-dismiss is managed explicitly (the popup uses StaysOpen=True so WPF's
        // capture-based dismissal never races the collapsed button's clicks).
        _dismissHelper ??= new PopupDismissHelper(
            this,
            () => _popup,
            () => _collapsedButton?.SetCurrentValue(ToggleButton.IsCheckedProperty, false));
        _dismissHelper.OnOpened();

        // Move the group's content grid from the (hidden) in-ribbon host into the flyout.
        if (_normalHost?.Child is { } content && _popupHost is not null)
        {
            _normalHost.Child = null;
            _popupHost.Child = content;
        }

        // Unfold the WHOLE flyout surface — border, shadow and the re-homed content together
        // (§3.42). Animating _popupHost rather than its Child is also the more stable target
        // here: the Child is swapped in and out on every open and close.
        RibbonMotion.PlayFlyoutOpen(_popupHost, RibbonAnimationAction.DropdownMenu);
    }

    /// <summary>
    /// Menu semantics for the collapsed flyout: invoking a command inside it closes it, the way
    /// Office does. Openers are exempt — clicking a drop-down or split button's chevron, a gallery's
    /// expand or scroll buttons, or a combo box's chevron is the START of an interaction, not the
    /// end of one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Right-clicking is exempt for free: a context menu raises no <see cref="ButtonBase.Click"/>,
    /// and its own rows are <see cref="System.Windows.Controls.MenuItem"/>s, which raise
    /// <c>MenuItem.Click</c> — a different routed event this handler never sees.
    /// </para>
    /// <para>
    /// Galleries and combo boxes commit through selection, not a click, so picking a tile or a list
    /// entry does NOT close the flyout yet. That is deliberate rather than forgotten: their
    /// selection also changes when the user merely ARROWS through the list, and closing the flyout
    /// mid-navigation would be worse than leaving it open.
    /// </para>
    /// </remarks>
    private void OnFlyoutInvoked(object sender, RoutedEventArgs e)
    {
        if (KeepsFlyoutOpen(e.OriginalSource))
        {
            return;
        }

        // Deferred, and this is not optional: closing re-homes the ENTIRE content grid — including
        // the element whose click is still being dispatched — back into the ribbon. Reparenting
        // mid-dispatch is the shape of bug §3.19/§3.39 spent two rounds unpicking. Background
        // priority also lets a nested drop-down finish closing itself first.
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => _collapsedButton?.SetCurrentValue(ToggleButton.IsCheckedProperty, false)));
    }

    /// <summary>
    /// Whether a click came from a control's own opener chrome rather than from a command.
    /// </summary>
    /// <remarks>
    /// Decided by <see cref="FrameworkElement.TemplatedParent"/>, not by walking the tree: openers
    /// are template parts (<c>PART_Toggle</c>, the gallery's expand/scroll buttons, the combo's
    /// chevron) so they always carry the owning control as their templated parent, while the things
    /// a user actually invokes — a <see cref="RibbonButton"/>, a <see cref="RibbonMenuItem"/>, a
    /// split button's <c>PART_Primary</c> — either sit in application markup (no templated parent)
    /// or are the primary part itself. A tree walk would have to hop the popup boundary between a
    /// menu item and its drop-down button, which is exactly where it would get this backwards.
    /// </remarks>
    private static bool KeepsFlyoutOpen(object? originalSource) =>
        originalSource is FrameworkElement element
        && element.TemplatedParent switch
        {
            // The primary half of a split button IS the command; only its chevron is an opener.
            RibbonSplitButton split => !ReferenceEquals(element, split.PrimaryPart),
            RibbonDropDownButton => true,
            InRibbonGallery => true,
            RibbonComboBox => true,
            _ => false,
        };

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _dismissHelper?.OnClosed();

        // Any gallery still expanded inside this flyout must close FIRST, so it
        // re-homes its items presenter back into its strip before we reclaim the
        // content — otherwise the presenter stays orphaned in the gallery's popup
        // and the gallery renders empty back in the ribbon.
        if (_popupHost?.Child is { } flyoutContent)
        {
            CloseNestedFlyouts(flyoutContent);
        }

        // Move the content back into the ribbon so it is ready when the group expands.
        if (_popupHost?.Child is { } content && _normalHost is not null)
        {
            _popupHost.Child = null;
            _normalHost.Child = content;
        }
    }

    private static void CloseNestedFlyouts(DependencyObject node)
    {
        switch (node)
        {
            case InRibbonGallery { IsDropDownOpen: true } gallery:
                gallery.SetCurrentValue(InRibbonGallery.IsDropDownOpenProperty, false);
                break;

            // A drop-down or split button left open when the group flyout closes would otherwise
            // keep a popup alive over a button that is no longer where the user left it.
            case RibbonDropDownButton { IsDropDownOpen: true } dropDown:
                dropDown.SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, false);
                break;
        }

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++)
        {
            CloseNestedFlyouts(VisualTreeHelper.GetChild(node, i));
        }
    }

    private void InvalidateHostPanel()
    {
        if (VisualTreeHelper.GetParent(this) is RibbonGroupsPanel panel)
        {
            panel.InvalidateStateCache();
        }
    }

    private static void InvalidateMeasureRecursive(DependencyObject node)
    {
        if (node is UIElement element)
        {
            element.InvalidateMeasure();
        }

        int childCount = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < childCount; i++)
        {
            InvalidateMeasureRecursive(VisualTreeHelper.GetChild(node, i));
        }
    }

    private void ApplyStateRecursive(DependencyObject parent, RibbonGroupSizeState state)
    {
        foreach (object child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is IRibbonSizeAware sizeAware)
            {
                sizeAware.ApplySizeState(state);
            }
            else if (child is DependencyObject dependencyChild)
            {
                ApplyStateRecursive(dependencyChild, state);
            }
        }
    }
}
