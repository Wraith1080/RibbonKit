using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using RibbonKit.Layout;

namespace RibbonKit.Controls;

/// <summary>
/// The strip that hosts <see cref="Ribbon.QuickAccessItems"/>. When the items need more room than
/// the strip is allowed, the ones that don't fit move into an overflow flyout behind a » button,
/// like Office.
/// </summary>
/// <remarks>
/// <para>
/// Whether overflow can happen at all is decided by the width the placement gives the toolbar.
/// Below the ribbon the strip owns a full-width row, so it is unconstrained and never overflows.
/// In the tab-strip row and the title bar it competes with the tabs and the window title, so the
/// ribbon caps it at <see cref="Ribbon.QuickAccessMaxWidth"/> and the overflow button appears as
/// soon as that cap is reached.
/// </para>
/// <para>
/// The flyout shows PROXIES rather than the real buttons: a WPF element has one visual parent, and
/// the real ones are still in the strip. Proxying is how the quick access toolbar already mirrors
/// ribbon commands (design notes §3.19), so the overflow menu reuses that machinery — and it
/// proxies the ORIGINAL command, not the QAT proxy, to avoid proxy-of-proxy chains.
/// </para>
/// </remarks>
public class RibbonQuickAccessToolBar : ItemsControl
{
    private static readonly DependencyPropertyKey HasOverflowPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HasOverflow),
            typeof(bool),
            typeof(RibbonQuickAccessToolBar),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="HasOverflow"/> dependency property.</summary>
    public static readonly DependencyProperty HasOverflowProperty = HasOverflowPropertyKey.DependencyProperty;

    private RibbonQuickAccessPanel? _panel;
    private ToggleButton? _overflowButton;
    private Popup? _overflowPopup;
    private ItemsControl? _overflowHost;
    private PopupDismissHelper? _dismiss;
    private bool _overflowUpdatePending;

    // Flyout proxy per quick-access item, kept for the toolbar's lifetime. See GetOrCreateEntry:
    // rebuilding these per open strands borrowed drop-down menus.
    private readonly Dictionary<FrameworkElement, FrameworkElement> _entries = new();

    static RibbonQuickAccessToolBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonQuickAccessToolBar),
            new FrameworkPropertyMetadata(typeof(RibbonQuickAccessToolBar)));
    }

    /// <summary>Whether one or more items currently don't fit and live in the overflow flyout.</summary>
    public bool HasOverflow => (bool)GetValue(HasOverflowProperty);

    /// <summary>
    /// The ribbon whose quick access items this strip shows. Set by the ribbon for every placement,
    /// including the title bar — where the toolbar is hosted by the window, outside the ribbon's
    /// visual tree, so it cannot be found by walking up.
    /// </summary>
    internal Ribbon? Owner { get; set; }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_overflowPopup is not null)
        {
            _overflowPopup.Opened -= OnOverflowOpened;
            _overflowPopup.Closed -= OnOverflowClosed;
        }

        _overflowButton = GetTemplateChild("PART_OverflowButton") as ToggleButton;
        _overflowPopup = GetTemplateChild("PART_OverflowPopup") as Popup;
        _overflowHost = GetTemplateChild("PART_OverflowHost") as ItemsControl;

        // The flyout is StaysOpen=True and dismissed explicitly, like every other RibbonKit popup.
        // With WPF's own light-dismiss, clicking the » button a SECOND time does nothing visible:
        // the popup's mouse capture closes it on mouse-DOWN (clearing IsChecked), then the button's
        // click sets IsChecked back to true and it reopens. Excluding the button from the dismiss
        // walk leaves its own toggle as the single owner of open/close.
        _dismiss = _overflowButton is null
            ? null
            : new PopupDismissHelper(_overflowButton, () => _overflowPopup, CloseOverflow);

        if (_overflowPopup is not null)
        {
            _overflowPopup.Opened += OnOverflowOpened;
            _overflowPopup.Closed += OnOverflowClosed;
        }
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        // The flyout's contents are a SNAPSHOT taken when it opened, so any change to the toolbar
        // makes it stale. That matters most for "Remove from Quick Access Toolbar" invoked on an
        // entry inside the flyout: the command would otherwise stay listed and still clickable,
        // and the strip behind it wouldn't reflow until the flyout was dismissed. Closing is the
        // honest answer — reopening shows the real state.
        //
        // Deferred to Background so the collection change finishes dispatching first: closing can
        // make a drop-down entry return its borrowed menu items, and §3.19's rule is never to
        // reparent a menu item mid-dispatch.
        if (_overflowPopup is { IsOpen: true })
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)CloseOverflow);
        }
    }

    /// <summary>
    /// Called by <see cref="RibbonQuickAccessPanel"/> from its measure pass with the current
    /// overflow state.
    /// </summary>
    /// <remarks>
    /// The flag is published on the DISPATCHER, not inline: it drives the overflow button's
    /// visibility, and setting it during the panel's own measure would invalidate layout from
    /// inside layout. Posting it lets the current pass finish and the next one settle.
    /// </remarks>
    internal void OnOverflowChanged(RibbonQuickAccessPanel panel, bool hasOverflow)
    {
        _panel = panel;

        if (hasOverflow == HasOverflow || _overflowUpdatePending)
        {
            return;
        }

        _overflowUpdatePending = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            (Action)(() =>
            {
                _overflowUpdatePending = false;
                bool current = _panel is { OverflowedChildren.Count: > 0 };
                SetValue(HasOverflowPropertyKey, current);

                // Items came back into the strip while the flyout was open — close it rather than
                // leave a menu of commands that are now visible right next to it.
                if (!current && _overflowPopup is { IsOpen: true })
                {
                    _overflowPopup.IsOpen = false;
                }
            }));
    }

    private void OnOverflowOpened(object? sender, EventArgs e)
    {
        _dismiss?.OnOpened();

        if (_overflowHost is null)
        {
            return;
        }

        var entries = new List<object>();

        if (_panel is not null && Owner is { } ribbon)
        {
            foreach (UIElement child in _panel.OverflowedChildren)
            {
                if (child is FrameworkElement element)
                {
                    entries.Add(GetOrCreateEntry(ribbon, element));
                }
            }
        }

        // Anything no longer in the toolbar at all can go; anything merely back in the strip is
        // kept, because it will very likely overflow again on the next resize.
        PruneEntries();

        _overflowHost.ItemsSource = entries;
    }

    /// <summary>
    /// Returns the flyout proxy for a quick-access item, creating it on first use and REUSING it
    /// forever after.
    /// </summary>
    /// <remarks>
    /// Reuse is not an optimisation, it is a correctness requirement. A drop-down or split proxy
    /// BORROWS its source's menu items while its flyout is open and returns them when that flyout
    /// closes (design notes §3.19 — a <c>RibbonMenuItem</c> is a single-parent element, so borrowing
    /// is the only option). Rebuilding the proxies on every open meant a proxy could be discarded
    /// while it still held the borrowed items: they were never returned, the SOURCE menu was left
    /// permanently empty, and every later open showed an empty popup — a bare rounded panel. That
    /// is exactly the symptom, and it only appeared after a few opens because the first borrow is
    /// what strands the items.
    /// </remarks>
    private FrameworkElement GetOrCreateEntry(Ribbon ribbon, FrameworkElement item)
    {
        if (_entries.TryGetValue(item, out FrameworkElement? existing))
        {
            return existing;
        }

        // Proxy the ORIGINAL command where there is one — a proxy of a QAT proxy would mirror the
        // mirror, and the catalog deliberately refuses to build those.
        FrameworkElement source = Ribbon.GetQuickAccessSource(item) ?? item;
        FrameworkElement entry = ribbon.CreateCommandProxy(source, RibbonControlSize.Medium);

        entry.HorizontalAlignment = HorizontalAlignment.Stretch;

        // Stretched so the hover highlight spans the flyout like a menu row, but the CONTENT is
        // left-aligned — a drop-down or split otherwise centres its icon+label and reads as a
        // different kind of thing from the plain buttons beside it.
        entry.SetValue(HorizontalContentAlignmentProperty, HorizontalAlignment.Left);

        // Remember which quick-access item this stands for, so a right-click inside the flyout can
        // offer "Remove from Quick Access Toolbar" for the REAL item rather than for this proxy.
        Ribbon.SetQuickAccessOverflowItemInternal(entry, item);

        // Picking a command closes the flyout, like a menu. A split button isn't a ButtonBase — its
        // primary part raises RibbonSplitButton.Click — and a drop-down opener must NOT close it,
        // since opening its menu is the whole point.
        switch (entry)
        {
            case RibbonSplitButton split:
                split.Click += OnOverflowEntryInvoked;
                break;
            case RibbonDropDownButton:
                break;
            case ButtonBase button:
                button.Click += OnOverflowEntryInvoked;
                break;
        }

        _entries[item] = entry;
        return entry;
    }

    private void PruneEntries()
    {
        if (Owner is not { } ribbon)
        {
            return;
        }

        foreach (FrameworkElement item in _entries.Keys.ToList())
        {
            if (ribbon.QuickAccessItems.Contains(item))
            {
                continue;
            }

            if (_entries[item] is RibbonDropDownButton { IsDropDownOpen: true } stale)
            {
                // Never drop a proxy still holding a borrowed menu — close it so the items go home.
                stale.SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, false);
            }

            _entries.Remove(item);
        }
    }

    private void OnOverflowClosed(object? sender, EventArgs e)
    {
        _dismiss?.OnClosed();

        // Close any entry menu still open. Its items are BORROWED from the source and are only
        // returned by the drop-down's own close path, so letting the flyout disappear around an
        // open menu would strand them (see GetOrCreateEntry).
        foreach (FrameworkElement entry in _entries.Values)
        {
            if (entry is RibbonDropDownButton { IsDropDownOpen: true } dropDown)
            {
                dropDown.SetCurrentValue(RibbonDropDownButton.IsDropDownOpenProperty, false);
            }
        }

        // The entries themselves are kept — see GetOrCreateEntry for why rebuilding is unsafe.
    }

    private void OnOverflowEntryInvoked(object sender, RoutedEventArgs e) => CloseOverflow();

    // Always closes through the BUTTON's IsChecked, never the popup's IsOpen: the two are bound
    // two-way, and driving the popup directly would leave the button stuck looking pressed.
    private void CloseOverflow()
    {
        if (_overflowButton is not null)
        {
            _overflowButton.SetCurrentValue(ToggleButton.IsCheckedProperty, false);
        }
        else if (_overflowPopup is not null)
        {
            _overflowPopup.SetCurrentValue(Popup.IsOpenProperty, false);
        }
    }
}
