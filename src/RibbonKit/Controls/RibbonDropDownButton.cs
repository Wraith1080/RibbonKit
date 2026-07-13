using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Layout;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonDropDownButtonAutomationPeer = RibbonKit.Automation.RibbonDropDownButtonAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A ribbon button whose only action is opening a dropdown of
/// <see cref="RibbonMenuItem"/>s. Declare items directly as content:
/// <code language="xaml">
/// &lt;rk:RibbonDropDownButton Header="Select"&gt;
///     &lt;rk:RibbonMenuItem Header="Select All" /&gt;
/// &lt;/rk:RibbonDropDownButton&gt;
/// </code>
/// </summary>
/// <remarks>
/// The flyout popup uses <c>StaysOpen=True</c> plus <see cref="PopupDismissHelper"/>
/// for light-dismiss, so WPF's popup mouse-capture never interferes with the opener
/// toggle — the toggle keeps completely standard click semantics.
/// </remarks>
[TemplatePart(Name = MenuHostPartName, Type = typeof(UIElement))]
[TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
public class RibbonDropDownButton : ItemsControl, IRibbonSizeAware
{
    private const string MenuHostPartName = "PART_MenuHost";
    private const string PopupPartName = "PART_Popup";

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(ImageSource),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(
                RibbonControlSize.Large,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(string),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsDropDownOpenChanged));

    /// <summary>Identifies the <see cref="ScreenTipTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTitleProperty =
        DependencyProperty.Register(
            nameof(ScreenTipTitle),
            typeof(string),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    /// <summary>Identifies the <see cref="ScreenTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTextProperty =
        DependencyProperty.Register(
            nameof(ScreenTipText),
            typeof(string),
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    private readonly PopupDismissHelper _dismissHelper;
    private UIElement? _menuHost;
    private Popup? _popup;
    private RibbonDropDownButton? _borrowSource;
    private bool _borrowed;

    static RibbonDropDownButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonDropDownButton),
            new FrameworkPropertyMetadata(typeof(RibbonDropDownButton)));
    }

    /// <summary>Initializes the dropdown button and its light-dismiss plumbing.</summary>
    public RibbonDropDownButton()
    {
        _dismissHelper = new PopupDismissHelper(
            this,
            () => _popup,
            () => SetCurrentValue(IsDropDownOpenProperty, false));
    }

    /// <summary>The button's label text.</summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The 16px icon used by the Medium and Small layouts.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The 32px icon used by the Large layout. Falls back to <see cref="Icon"/> when unset.</summary>
    public ImageSource? LargeIcon
    {
        get => (ImageSource?)GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>The size the button currently renders at.</summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Comma-separated sizes for the group states Large, Medium, Small. When set, the
    /// sizing engine drives <see cref="Size"/>.
    /// </summary>
    public string? SizeDefinition
    {
        get => (string?)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Whether the dropdown is open.</summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Bold first line of the ScreenTip (rich tooltip).</summary>
    public string? ScreenTipTitle
    {
        get => (string?)GetValue(ScreenTipTitleProperty);
        set => SetValue(ScreenTipTitleProperty, value);
    }

    /// <summary>Descriptive body of the ScreenTip.</summary>
    public string? ScreenTipText
    {
        get => (string?)GetValue(ScreenTipTextProperty);
        set => SetValue(ScreenTipTextProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_menuHost is not null)
        {
            _menuHost.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnMenuItemClicked));
        }

        if (_popup is not null)
        {
            _popup.Opened -= OnPopupOpened;
            _popup.Closed -= OnPopupClosed;
        }

        base.OnApplyTemplate();

        _menuHost = GetTemplateChild(MenuHostPartName) as UIElement;
        _menuHost?.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnMenuItemClicked));

        _popup = GetTemplateChild(PopupPartName) as Popup;
        if (_popup is not null)
        {
            _popup.Opened += OnPopupOpened;
            _popup.Closed += OnPopupClosed;
        }
    }

    /// <summary>
    /// Makes THIS dropdown a PROXY of <paramref name="source"/> (see
    /// <see cref="Ribbon.CreateCommandProxy"/>): it's a real dropdown button living elsewhere
    /// (the QAT / a custom group), with its OWN popup — so the flyout opens under the proxy and
    /// toggles/dismisses correctly, unlike merely re-opening the source. It has no items of its
    /// own; instead it BORROWS the source's menu items while open (moved in on open, returned on
    /// close by <see cref="OnIsDropDownOpenChanged"/>). Borrowing (not sharing) avoids the
    /// single-parent conflict — a <see cref="RibbonMenuItem"/> can only live in one dropdown — and
    /// works even when the source's own tab isn't currently realized, since <see cref="Items"/> is
    /// a logical collection independent of visual realization.
    /// </summary>
    internal void BorrowMenuFrom(RibbonDropDownButton source) => _borrowSource = source;

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (RibbonDropDownButton)d;

        // Borrow the source's items as the proxy OPENS — before the popup lays out, so it sizes to
        // the real menu. (Returning them happens in OnPopupClosed, deferred, to avoid reparenting
        // a menu item mid-click.) The _borrowed guard makes a quick close→reopen a no-op.
        if ((bool)e.NewValue && button._borrowSource is { } source && !button._borrowed)
        {
            MoveItems(source, button);
            button._borrowed = true;
        }
    }

    private void ReturnBorrowedItems()
    {
        if (_borrowSource is { } source && _borrowed)
        {
            MoveItems(this, source);
            _borrowed = false;
        }
    }

    private static void MoveItems(RibbonDropDownButton from, RibbonDropDownButton to)
    {
        while (from.Items.Count > 0)
        {
            object item = from.Items[0];
            from.Items.RemoveAt(0);
            to.Items.Add(item);
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonDropDownButtonAutomationPeer(this);

    void IRibbonSizeAware.ApplySizeState(RibbonGroupSizeState state) => ApplySizeStateCore(state);

    /// <summary>Applies the sizing engine's state via <see cref="SizeDefinition"/>.</summary>
    protected void ApplySizeStateCore(RibbonGroupSizeState state)
    {
        string? definition = SizeDefinition;
        if (string.IsNullOrWhiteSpace(definition))
        {
            return;
        }

        RibbonGroupSizeState effectiveState =
            state == RibbonGroupSizeState.Collapsed ? RibbonGroupSizeState.Large : state;

        try
        {
            Size = RibbonSizeDefinition.SizeFor(definition, effectiveState);
        }
        catch (ArgumentException)
        {
            // Invalid definitions are ignored during layout.
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        _dismissHelper.OnOpened();
        // Animate the menu's inner content, not the popup's own child border — transforming
        // that border would shift the transparent popup's resting position (see InRibbonGallery).
        FrameworkElement? content = (_menuHost as Border)?.Child as FrameworkElement ?? _menuHost as FrameworkElement;
        RibbonMotion.PlayOpen(content, RibbonAnimationAction.DropdownMenu);
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _dismissHelper.OnClosed();

        // Return borrowed items AFTER the popup has closed and the UI thread is idle, so we never
        // reparent a menu item while its click is still being dispatched. Deferred + guarded, so a
        // fast close→reopen keeps the items in the proxy (the reopen re-checks IsDropDownOpen).
        if (_borrowSource is not null && _borrowed)
        {
            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() =>
                {
                    if (!IsDropDownOpen)
                    {
                        ReturnBorrowedItems();
                    }
                }));
        }
    }

    private void OnMenuItemClicked(object sender, RoutedEventArgs e)
    {
        // Any button click inside the dropdown closes it (menu semantics).
        SetCurrentValue(IsDropDownOpenProperty, false);
    }

    private static void OnScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (RibbonDropDownButton)d;
        ScreenTipHelper.Update(button, button.ScreenTipTitle, button.ScreenTipText);
    }
}
