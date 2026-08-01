using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

/// <summary>
/// The Office 2007 <b>application menu</b>: the two-pane drop-down the round orb opens — a left
/// command column, a right content pane, and a footer bar (Word's <i>Word Options</i> /
/// <i>Exit Word</i>).
/// <code language="xaml">
/// &lt;rk:Ribbon.ApplicationMenu&gt;
///     &lt;rk:RibbonApplicationMenu&gt;
///         &lt;rk:RibbonApplicationMenu.DefaultContent&gt;
///             &lt;StackPanel&gt; ... Recent Documents ... &lt;/StackPanel&gt;
///         &lt;/rk:RibbonApplicationMenu.DefaultContent&gt;
///         &lt;rk:RibbonApplicationMenuItem Header="New" Icon="{StaticResource Icon.New}" /&gt;
///         &lt;rk:RibbonApplicationMenuItem Header="Save As" PaneHeader="Save a copy of the document"&gt;
///             &lt;StackPanel&gt; ... pane rows ... &lt;/StackPanel&gt;
///         &lt;/rk:RibbonApplicationMenuItem&gt;
///         &lt;rk:RibbonApplicationMenu.FooterContent&gt; ... &lt;/rk:RibbonApplicationMenu.FooterContent&gt;
///     &lt;/rk:RibbonApplicationMenu&gt;
/// &lt;/rk:Ribbon.ApplicationMenu&gt;
/// </code>
/// <para>
/// <b>This is not the backstage.</b> <see cref="Backstage"/> is a full-window overlay hosted in the
/// window's adorner layer; the application menu is a drop-down rendered <i>inside the ribbon's own
/// tab-strip row</i>, deliberately BEHIND the application button so the orb keeps sitting on top of
/// it exactly as it does in Office 2007. Assign one to <see cref="Ribbon.ApplicationMenu"/> and the
/// File button opens it instead of the backstage.
/// </para>
/// <para>
/// <b>The hover model</b> (matching Office 2007, see 04-DESIGN-NOTES §3.46). The pane shows
/// <see cref="DefaultContent"/> until the pointer enters a nav item that has one of its own. That
/// item remains active across the separator gap and while the pointer uses its pane; only entering
/// another main nav item changes the pane. A pane-less nav item restores the default page, and
/// closing/reopening the menu resets it.
/// </para>
/// </summary>
[TemplatePart(Name = FramePartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PanePartName, Type = typeof(FrameworkElement))]
public class RibbonApplicationMenu : ItemsControl
{
    private const string FramePartName = "PART_Frame";
    private const string PanePartName = "PART_Pane";

    /// <summary>Identifies the <see cref="DefaultContent"/> dependency property.</summary>
    public static readonly DependencyProperty DefaultContentProperty =
        DependencyProperty.Register(
            nameof(DefaultContent),
            typeof(object),
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="DefaultHeader"/> dependency property.</summary>
    public static readonly DependencyProperty DefaultHeaderProperty =
        DependencyProperty.Register(
            nameof(DefaultHeader),
            typeof(string),
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="FooterContent"/> dependency property.</summary>
    public static readonly DependencyProperty FooterContentProperty =
        DependencyProperty.Register(
            nameof(FooterContent),
            typeof(object),
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(null));

    private static readonly DependencyPropertyKey ActiveItemPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ActiveItem),
            typeof(RibbonApplicationMenuItem),
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="ActiveItem"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveItemProperty = ActiveItemPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ActivePaneContentPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ActivePaneContent),
            typeof(object),
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="ActivePaneContent"/> dependency property.</summary>
    public static readonly DependencyProperty ActivePaneContentProperty = ActivePaneContentPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ActivePaneHeaderPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ActivePaneHeader),
            typeof(string),
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the read-only <see cref="ActivePaneHeader"/> dependency property.</summary>
    public static readonly DependencyProperty ActivePaneHeaderProperty = ActivePaneHeaderPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey HasActivePanePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasActivePane),
            typeof(bool),
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasActivePane"/> dependency property.</summary>
    public static readonly DependencyProperty HasActivePaneProperty = HasActivePanePropertyKey.DependencyProperty;

    private FrameworkElement? _frame;

    private Window? _dismissWindow;

    static RibbonApplicationMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonApplicationMenu),
            new FrameworkPropertyMetadata(typeof(RibbonApplicationMenu)));
    }

    /// <summary>Initializes a new <see cref="RibbonApplicationMenu"/>.</summary>
    public RibbonApplicationMenu()
    {
        // Any command click inside the menu dismisses it — the pane rows, the footer buttons, and
        // the plain nav items. The two cases that must NOT dismiss (a nav item's arrow, and a
        // pane-less "drop-down" nav item) mark the click handled themselves, so this handler never
        // has to reason about WHERE the click came from. That is deliberate: the collapsed-group
        // flyout learned the hard way (§3.40) that a visual-tree walk gets menu items backwards.
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyClick));

        // Trap Tab inside the menu while it is up, for the same reason the backstage does: the
        // menu paints over the ribbon and the document but does not sit between them and the
        // keyboard-focus tree, so without this Tab walks straight into controls the user cannot see.
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);

        // So the menu can take focus the moment it opens and Esc works without a click first.
        Focusable = true;

        IsVisibleChanged += OnIsVisibleChanged;
    }

    /// <summary>
    /// Raised when the user asks to leave the menu — Esc, a click outside it, the window
    /// deactivating, or any command inside it being invoked. The hosting <see cref="Ribbon"/>
    /// subscribes and closes the menu.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// The page shown in the right-hand pane while no nav item is claiming it — Word's
    /// <i>Recent Documents</i> list. Unlike a nav item's pane this one is rendered <b>bare</b>: no
    /// surrounding frame and no header band, matching Office 2007, so whatever heading it needs is
    /// part of the content itself.
    /// </summary>
    public object? DefaultContent
    {
        get => GetValue(DefaultContentProperty);
        set => SetValue(DefaultContentProperty, value);
    }

    /// <summary>
    /// Optional accessible name for <see cref="DefaultContent"/>. Not rendered (the default page
    /// draws no header band); it is exposed to automation so the pane is not anonymous.
    /// </summary>
    public string? DefaultHeader
    {
        get => (string?)GetValue(DefaultHeaderProperty);
        set => SetValue(DefaultHeaderProperty, value);
    }

    /// <summary>
    /// The footer bar's content, laid out right-aligned on the glass strip at the foot of the menu.
    /// Word puts <i>Word Options</i> and <i>Exit Word</i> here; use
    /// <see cref="RibbonApplicationMenuButton"/> for buttons that match.
    /// </summary>
    public object? FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    /// <summary>The nav item whose pane is currently shown, or <see langword="null"/> for the default page.</summary>
    public RibbonApplicationMenuItem? ActiveItem
    {
        get => (RibbonApplicationMenuItem?)GetValue(ActiveItemProperty);
        private set => SetValue(ActiveItemPropertyKey, value);
    }

    /// <summary>What the right-hand pane is showing: the active item's content, else <see cref="DefaultContent"/>.</summary>
    public object? ActivePaneContent
    {
        get => GetValue(ActivePaneContentProperty);
        private set => SetValue(ActivePaneContentPropertyKey, value);
    }

    /// <summary>The active item's <see cref="RibbonApplicationMenuItem.PaneHeader"/>, shown in the pane's header band.</summary>
    public string? ActivePaneHeader
    {
        get => (string?)GetValue(ActivePaneHeaderProperty);
        private set => SetValue(ActivePaneHeaderPropertyKey, value);
    }

    /// <summary>
    /// <see langword="true"/> while a nav item's pane is showing (framed, with a header band);
    /// <see langword="false"/> while the bare default page is.
    /// </summary>
    public bool HasActivePane
    {
        get => (bool)GetValue(HasActivePaneProperty);
        private set => SetValue(HasActivePanePropertyKey, value);
    }

    /// <summary>Asks the host to close the menu. Same effect as pressing Esc.</summary>
    public void RequestClose() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _frame = GetTemplateChild(FramePartName) as FrameworkElement;
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) =>
        item is RibbonApplicationMenuItem or RibbonApplicationMenuSeparator;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new RibbonApplicationMenuItem();

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape)
        {
            RequestClose();
            e.Handled = true;
        }
    }

    // ------------------------------------------------------------------ nav hover state machine

    /// <summary>
    /// Called by a nav item as the pointer enters or leaves it. Entering a main-nav row is the only
    /// hover action that changes pane ownership; leaving into separator chrome, the small gap before
    /// the pane, or empty menu space is deliberately neutral.
    /// </summary>
    internal void NotifyItemHoverChanged(RibbonApplicationMenuItem item, bool isOver)
    {
        if (!isOver)
        {
            return;
        }

        // A pane-less command intentionally restores the default page. A pane-bearing row claims
        // its pane immediately and stays active until another row (or menu close) replaces it.
        SetActive(item.HasPane ? item : null);
    }

    /// <summary>Called by a nav item when it is clicked in a way that should keep the menu up.</summary>
    internal void NotifyItemClaimed(RibbonApplicationMenuItem item)
    {
        SetActive(item.HasPane ? item : null);
    }

    private void SetActive(RibbonApplicationMenuItem? item)
    {
        if (ReferenceEquals(ActiveItem, item))
        {
            return;
        }

        ActiveItem?.SetActive(false);
        ActiveItem = item;
        item?.SetActive(true);

        // NEVER falls back to DefaultContent. The default page has its own presenter in the
        // template, and a UIElement can have only one visual parent — routing the same object
        // through both presenters would throw the moment the pane switched back.
        ActivePaneContent = item?.Content;
        ActivePaneHeader = item?.PaneHeader;
        HasActivePane = item is not null;
    }

    // ------------------------------------------------------------------ open / dismiss

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            // Always open on the default page, whatever was showing when it last closed.
            SetActive(null);

            HookDismissal();
            Focus(); // So Esc works without a click first.

            ApplyTemplate();
            RibbonMotion.PlayFlyoutOpen(_frame, RibbonAnimationAction.DropdownMenu);
        }
        else
        {
            UnhookDismissal();
        }
    }

    private void HookDismissal()
    {
        UnhookDismissal(); // Defensive: never double-subscribe.

        _dismissWindow = Window.GetWindow(this);
        if (_dismissWindow is null)
        {
            return;
        }

        _dismissWindow.PreviewMouseDown += OnWindowPreviewMouseDown;
        _dismissWindow.PreviewKeyDown += OnWindowPreviewKeyDown;
        _dismissWindow.Deactivated += OnWindowDeactivated;
        _dismissWindow.LocationChanged += OnWindowMoved;
        _dismissWindow.SizeChanged += OnWindowResized;
    }

    private void UnhookDismissal()
    {
        if (_dismissWindow is null)
        {
            return;
        }

        _dismissWindow.PreviewMouseDown -= OnWindowPreviewMouseDown;
        _dismissWindow.PreviewKeyDown -= OnWindowPreviewKeyDown;
        _dismissWindow.Deactivated -= OnWindowDeactivated;
        _dismissWindow.LocationChanged -= OnWindowMoved;
        _dismissWindow.SizeChanged -= OnWindowResized;
        _dismissWindow = null;
    }

    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        // The APPLICATION BUTTON is exempt. It is a ToggleButton bound two-way to the ribbon's open
        // state, so closing here on mouse-DOWN would leave it unchecked and its own click on
        // mouse-UP would immediately re-open the menu — the orb would look dead. Letting the toggle
        // own that click is the whole fix.
        for (DependencyObject? node = source; node is not null; node = VisualParentOf(node))
        {
            if (ReferenceEquals(node, this))
            {
                return;
            }

            if (node is FrameworkElement { Name: Ribbon.ApplicationButtonPartName })
            {
                return;
            }
        }

        RequestClose();
    }

    // Esc is caught at the WINDOW, not only in OnPreviewKeyDown: focus may still be sitting on the
    // application button (the user opened the menu with the keyboard and never moved on), and that
    // button is outside the menu's subtree.
    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            RequestClose();
            e.Handled = true;
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e) => RequestClose();

    private void OnWindowMoved(object? sender, EventArgs e) => RequestClose();

    private void OnWindowResized(object sender, SizeChangedEventArgs e) => RequestClose();

    private void OnAnyClick(object sender, RoutedEventArgs e) => RequestClose();

    private static DependencyObject? VisualParentOf(DependencyObject node) =>
        node is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node)
            : LogicalTreeHelper.GetParent(node);
}

/// <summary>
/// One row in a <see cref="RibbonApplicationMenu"/>'s left command column. Three shapes, all
/// driven by what you give it:
/// <list type="bullet">
///   <item><description>
///     <b>Plain command</b> — no <see cref="ContentControl.Content"/>. One button, no arrow;
///     clicking runs <see cref="Command"/> / raises <see cref="Click"/> and closes the menu
///     (New, Save, Close).
///   </description></item>
///   <item><description>
///     <b>Split</b> (the default whenever there IS content) — a command half plus an arrow half
///     separated by a hairline. Clicking the command half runs the default action and closes;
///     hovering anywhere, or clicking the arrow, shows the pane and keeps the menu up
///     (Save As, Print, Prepare, Send, Publish).
///   </description></item>
///   <item><description>
///     <b>Drop-down</b> — content plus <see cref="IsSplit"/> <c>= False</c>. The whole row is the
///     opener: it has no default action, so clicking anywhere just shows the pane.
///   </description></item>
/// </list>
/// </summary>
[TemplatePart(Name = PrimaryPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = ArrowPartName, Type = typeof(ButtonBase))]
public class RibbonApplicationMenuItem : HeaderedContentControl
{
    private const string PrimaryPartName = "PART_Primary";
    private const string ArrowPartName = "PART_Arrow";

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="PaneHeader"/> dependency property.</summary>
    public static readonly DependencyProperty PaneHeaderProperty =
        DependencyProperty.Register(
            nameof(PaneHeader),
            typeof(string),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsSplit"/> dependency property.</summary>
    public static readonly DependencyProperty IsSplitProperty =
        DependencyProperty.Register(
            nameof(IsSplit),
            typeof(bool),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(true, (d, _) => ((RibbonApplicationMenuItem)d).UpdateShape()));

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(null));

    private static readonly DependencyPropertyKey HasPanePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasPane),
            typeof(bool),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasPane"/> dependency property.</summary>
    public static readonly DependencyProperty HasPaneProperty = HasPanePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsSplitPresentationPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsSplitPresentation),
            typeof(bool),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="IsSplitPresentation"/> dependency property.</summary>
    public static readonly DependencyProperty IsSplitPresentationProperty = IsSplitPresentationPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsActivePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsActive),
            typeof(bool),
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="Click"/> routed event.</summary>
    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RibbonApplicationMenuItem));

    private ButtonBase? _primary;

    private ButtonBase? _arrow;

    /// <summary>The command half used as this row's primary KeyTip target.</summary>
    internal ButtonBase? PrimaryPart => _primary;

    /// <summary>The pane-opener half used as a split row's secondary KeyTip target.</summary>
    internal ButtonBase? ArrowPart => _arrow;

    static RibbonApplicationMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonApplicationMenuItem),
            new FrameworkPropertyMetadata(typeof(RibbonApplicationMenuItem)));
    }

    /// <summary>Raised when the row's command half is invoked.</summary>
    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>The 32px glyph at the left of the row.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Caption for the header band above this item's pane — Word's "Save a copy of the document".
    /// Leave it unset and the pane renders framed but band-less.
    /// </summary>
    public string? PaneHeader
    {
        get => (string?)GetValue(PaneHeaderProperty);
        set => SetValue(PaneHeaderProperty, value);
    }

    /// <summary>
    /// Whether a row that HAS a pane also has its own default action, i.e. whether it is a SPLIT
    /// row (default <see langword="true"/>) rather than a plain drop-down. Ignored on a row with no
    /// pane, which is always a single command button. See the type remarks for what each shape does
    /// on click — and note the two shapes also differ while the pointer is in the pane: a split row
    /// dims its command half and keeps its arrow half lit, a drop-down row stays fully lit.
    /// </summary>
    public bool IsSplit
    {
        get => (bool)GetValue(IsSplitProperty);
        set => SetValue(IsSplitProperty, value);
    }

    /// <summary>The command run when the row's command half is invoked.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Parameter passed to <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary><see langword="true"/> when the row carries pane content, so it shows an arrow.</summary>
    public bool HasPane
    {
        get => (bool)GetValue(HasPaneProperty);
        private set => SetValue(HasPanePropertyKey, value);
    }

    /// <summary>
    /// <see langword="true"/> when the row renders as two halves — <see cref="HasPane"/> AND
    /// <see cref="IsSplit"/>. The template keys the hairline and the half-lit active state off this
    /// single flag so the two halves can never disagree about which shape the row is.
    /// </summary>
    public bool IsSplitPresentation
    {
        get => (bool)GetValue(IsSplitPresentationProperty);
        private set => SetValue(IsSplitPresentationPropertyKey, value);
    }

    /// <summary><see langword="true"/> while this row's pane is the one on show.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        private set => SetValue(IsActivePropertyKey, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_primary is not null)
        {
            _primary.Click -= OnPrimaryClick;
        }

        if (_arrow is not null)
        {
            _arrow.Click -= OnArrowClick;
        }

        base.OnApplyTemplate();

        _primary = GetTemplateChild(PrimaryPartName) as ButtonBase;
        if (_primary is not null)
        {
            _primary.Click += OnPrimaryClick;
        }

        _arrow = GetTemplateChild(ArrowPartName) as ButtonBase;
        if (_arrow is not null)
        {
            _arrow.Click += OnArrowClick;
        }
    }

    /// <summary>
    /// Content decides the row's SHAPE, so both read-only flags are recomputed whenever it changes.
    /// Overriding the virtual rather than <c>ContentProperty</c>'s metadata on purpose: a metadata
    /// override would also have to restate the base default value and options, and this hook is
    /// what it exists for.
    /// </summary>
    protected override void OnContentChanged(object? oldContent, object? newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateShape();
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        OwnerMenu?.NotifyItemHoverChanged(this, isOver: true);
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        OwnerMenu?.NotifyItemHoverChanged(this, isOver: false);
    }

    internal void SetActive(bool value) => IsActive = value;

    private RibbonApplicationMenu? OwnerMenu =>
        ItemsControl.ItemsControlFromItemContainer(this) as RibbonApplicationMenu
        ?? FindMenu(this);

    private static RibbonApplicationMenu? FindMenu(DependencyObject start)
    {
        for (DependencyObject? node = start; node is not null; node = VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node))
        {
            if (node is RibbonApplicationMenu menu)
            {
                return menu;
            }
        }

        return null;
    }

    private void UpdateShape()
    {
        HasPane = Content is not null;
        IsSplitPresentation = HasPane && IsSplit;
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        // Drop-down shape: the row has no default action of its own, so a click on the command
        // half is just another way of asking for the pane. Handled — the menu stays up.
        if (HasPane && !IsSplit)
        {
            OwnerMenu?.NotifyItemClaimed(this);
            e.Handled = true;
            return;
        }

        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }

        // Left UNHANDLED on purpose: the menu's ButtonBase.Click handler sees it bubble past and
        // dismisses, exactly like Office.
    }

    private void OnArrowClick(object sender, RoutedEventArgs e)
    {
        OwnerMenu?.NotifyItemClaimed(this);
        e.Handled = true;
    }
}

/// <summary>
/// A group divider between runs of <see cref="RibbonApplicationMenuItem"/>s in a
/// <see cref="RibbonApplicationMenu"/>'s command column — the two hairlines Word draws under
/// <i>Save As</i> and under <i>Publish</i>.
/// </summary>
/// <remarks>
/// It exists as its own type rather than reusing <see cref="System.Windows.Controls.Separator"/>
/// because a theme dictionary has nowhere legal to put a style for the stock one: declared
/// implicitly it would restyle every separator in the consuming app, and it cannot be scoped to the
/// menu instead — <c>FrameworkElement.Resources</c> is a plain CLR property, so no
/// <see cref="Setter"/> can assign it. A dedicated control picks up its default style from
/// Generic.xaml with no resource lookup involved.
/// </remarks>
public class RibbonApplicationMenuSeparator : Control
{
    static RibbonApplicationMenuSeparator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonApplicationMenuSeparator),
            new FrameworkPropertyMetadata(typeof(RibbonApplicationMenuSeparator)));
    }
}

/// <summary>
/// A row inside a <see cref="RibbonApplicationMenuItem"/>'s pane (or inside
/// <see cref="RibbonApplicationMenu.DefaultContent"/>): a 32px icon, a bold
/// <see cref="ContentControl.Content"/> title and an optional wrapped
/// <see cref="Description"/> beneath it — Word's "Word Document / Save the document in the default
/// file format". Clicking one runs its command and closes the menu.
/// </summary>
public class RibbonApplicationMenuPaneItem : Button
{
    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonApplicationMenuPaneItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(RibbonApplicationMenuPaneItem),
            new FrameworkPropertyMetadata(null));

    static RibbonApplicationMenuPaneItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonApplicationMenuPaneItem),
            new FrameworkPropertyMetadata(typeof(RibbonApplicationMenuPaneItem)));
    }

    /// <summary>The 32px glyph at the left of the row.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Optional second line, wrapped, in the secondary text colour. Leave it unset for a
    /// single-line row — which is what a recent-documents list wants.
    /// </summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}

/// <summary>
/// A button styled for a <see cref="RibbonApplicationMenu"/>'s footer bar — Word's
/// <i>Word Options</i> and <i>Exit Word</i>. A bordered glass button with an optional 16px icon;
/// clicking it closes the menu like any other command inside it.
/// </summary>
public class RibbonApplicationMenuButton : Button
{
    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonApplicationMenuButton),
            new FrameworkPropertyMetadata(null));

    static RibbonApplicationMenuButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonApplicationMenuButton),
            new FrameworkPropertyMetadata(typeof(RibbonApplicationMenuButton)));
    }

    /// <summary>The 16px glyph shown left of the label.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
