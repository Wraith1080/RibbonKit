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
                if (child is not FrameworkElement element)
                {
                    continue;
                }

                // Proxy the ORIGINAL command where there is one — a proxy of a QAT proxy would
                // mirror the mirror, and the catalog deliberately refuses to build those.
                FrameworkElement source = Ribbon.GetQuickAccessSource(element) ?? element;
                FrameworkElement entry = ribbon.CreateCommandProxy(source, RibbonControlSize.Medium);
                entry.HorizontalAlignment = HorizontalAlignment.Stretch;

                // Picking a command closes the flyout, like a menu.
                if (entry is ButtonBase button)
                {
                    button.Click += OnOverflowEntryInvoked;
                }

                entries.Add(entry);
            }
        }

        _overflowHost.ItemsSource = entries;
    }

    private void OnOverflowClosed(object? sender, EventArgs e)
    {
        _dismiss?.OnClosed();

        // Proxies are rebuilt per open: they're cheap, and holding them would keep handlers alive
        // on commands whose source may have been removed from the toolbar in the meantime.
        if (_overflowHost?.ItemsSource is IEnumerable<object> entries)
        {
            foreach (object entry in entries)
            {
                if (entry is ButtonBase button)
                {
                    button.Click -= OnOverflowEntryInvoked;
                }
            }
        }

        if (_overflowHost is not null)
        {
            _overflowHost.ItemsSource = null;
        }
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
