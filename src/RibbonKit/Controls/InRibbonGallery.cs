using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

/// <summary>
/// A gallery that lives directly in the ribbon: a compact strip of tiles with
/// scroll-up / scroll-down / expand buttons on its right edge. The expand button
/// opens an overlay popup showing the full wrapped gallery, positioned over the
/// strip like Office. Committing a pick in the popup closes it (Office-style) and
/// scrolls the collapsed strip to reveal the chosen tile.
/// </summary>
/// <remarks>
/// The strip and the popup share ONE items presenter, re-homed between them while
/// the popup is open (the same pattern as collapsed <see cref="RibbonGroup"/>
/// flyouts). Light-dismiss comes from <see cref="PopupDismissHelper"/> — the popup
/// itself never takes mouse capture.
///
/// <para><b>Scroll-to-chosen-item (design notes §3.13).</b> Two independent bugs had
/// to be solved, on the OPEN path and the CLOSE path:</para>
///
/// <para><b>Open path — popup hit-testing.</b> The shared re-homed
/// <see cref="ScrollViewer"/> must be at offset 0 with a viewport measured for the
/// popup (not the strip's stale one-row viewport) before any click hit-tests, or
/// clicks below the top row clamp wrong. Handled by zeroing the offset BEFORE the
/// re-home into the popup and force-relaying-out on the popup's <c>Opened</c> event.</para>
///
/// <para><b>Close path — drag-follow re-selection.</b> A mouse pick selects the tile
/// correctly, but closing the popup re-homes the presenter back into the strip and
/// scrolls it. If that happens while the mouse button is still down (the ListBox
/// still holds capture), the tiles move under the captured pointer and the ListBox's
/// single-select drag-follow re-selects the tile that lands under the cursor — the
/// row below (symptom: "the pick commits, then jumps one tile down"). So the close
/// is postponed until the mouse button is RELEASED. The pick happens in the popup's
/// own window, so the release is observed via a handler on the popup host; the
/// gallery itself never receives the popup's mouse events.</para>
/// </remarks>
[TemplatePart(Name = ContentHostPartName, Type = typeof(Decorator))]
[TemplatePart(Name = PopupHostPartName, Type = typeof(Border))]
[TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
[TemplatePart(Name = ScrollViewerPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = LineUpPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = LineDownPartName, Type = typeof(ButtonBase))]
public class InRibbonGallery : RibbonGallery
{
    private const string ContentHostPartName = "PART_ContentHost";
    private const string PopupHostPartName = "PART_PopupHost";
    private const string PopupPartName = "PART_Popup";
    private const string ScrollViewerPartName = "PART_ScrollViewer";
    private const string LineUpPartName = "PART_LineUp";
    private const string LineDownPartName = "PART_LineDown";

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(InRibbonGallery),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsDropDownOpenChanged));

    private readonly PopupDismissHelper _dismissHelper;
    private Decorator? _contentHost;
    private Border? _popupHost;
    private Popup? _popup;
    private ScrollViewer? _scrollViewer;
    private ButtonBase? _lineUp;
    private ButtonBase? _lineDown;

    // A mouse pick is committed but the close is being held until the button is
    // released (see the class remarks — closing while the button is down drag-follows
    // the selection to the tile below).
    private bool _commitPending;

    // The strip's scroll offset captured just before the popup opens (before it is zeroed
    // for hit-testing). The strip reveal glides FROM this so a higher pick slides up and a
    // lower pick slides down, instead of always gliding down from the top.
    private double _stripOffsetBeforeOpen;

    static InRibbonGallery()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(InRibbonGallery),
            new FrameworkPropertyMetadata(typeof(InRibbonGallery)));
    }

    /// <summary>Initializes the gallery and its light-dismiss plumbing.</summary>
    public InRibbonGallery()
    {
        _dismissHelper = new PopupDismissHelper(
            this,
            () => _popup,
            () => SetCurrentValue(IsDropDownOpenProperty, false));

        // Self-heal: if the gallery is pulled out of the tree while expanded (e.g.
        // its host group flyout closes and re-homes its content), close the popup so
        // the items presenter returns to the strip instead of staying orphaned.
        Unloaded += OnGalleryUnloaded;
    }

    private void OnGalleryUnloaded(object sender, RoutedEventArgs e)
    {
        if (IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
        }
    }

    /// <summary>Whether the expanded gallery popup is open.</summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_lineUp is not null)
        {
            _lineUp.Click -= OnLineUpClick;
        }

        if (_lineDown is not null)
        {
            _lineDown.Click -= OnLineDownClick;
        }

        if (_popup is not null)
        {
            _popup.Opened -= OnPopupOpened;
        }

        CancelPendingCommit();

        base.OnApplyTemplate();

        _contentHost = GetTemplateChild(ContentHostPartName) as Decorator;
        _popupHost = GetTemplateChild(PopupHostPartName) as Border;
        _popup = GetTemplateChild(PopupPartName) as Popup;
        _scrollViewer = GetTemplateChild(ScrollViewerPartName) as ScrollViewer;
        _lineUp = GetTemplateChild(LineUpPartName) as ButtonBase;
        _lineDown = GetTemplateChild(LineDownPartName) as ButtonBase;

        if (_lineUp is not null)
        {
            _lineUp.Click += OnLineUpClick;
        }

        if (_lineDown is not null)
        {
            _lineDown.Click += OnLineDownClick;
        }

        if (_popup is not null)
        {
            _popup.Opened += OnPopupOpened;
        }
    }

    /// <summary>
    /// Office behavior: committing a pick in the expanded popup closes it (and the
    /// collapsed strip then scrolls to reveal the pick — see the close branch of
    /// <see cref="HandleDropDownStateChanged"/>).
    /// </summary>
    /// <remarks>
    /// The close is NOT started here. Only <b>mouse</b> picks auto-close (keyboard
    /// arrow-navigation in the open popup must leave it open), and even for a mouse
    /// pick the close waits until the button is released: closing re-homes the shared
    /// presenter into the strip and scrolls it, and doing that under a still-pressed,
    /// still-captured pointer makes the ListBox drag-follow the selection to the tile
    /// that slides under the cursor (one row down). The release is caught on the popup
    /// host (<see cref="OnPopupHostPreviewMouseLeftButtonUp"/>) because the pick occurs
    /// in the popup's own window.
    /// </remarks>
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);

        if (!IsDropDownOpen || e.AddedItems.Count == 0)
        {
            return;
        }

        // Keyboard / programmatic selection change → leave the popup open.
        if (Mouse.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        // Mouse pick: arm the deferred close and wait for the button-up on the popup
        // host. handledEventsToo so we still hear it after the ListBoxItem handles it.
        if (_popupHost is not null && !_commitPending)
        {
            _commitPending = true;
            _popupHost.AddHandler(
                UIElement.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(OnPopupHostPreviewMouseLeftButtonUp),
                handledEventsToo: true);
        }
    }

    private void OnPopupHostPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CancelPendingCommit();

        // Defer one input cycle so the button-up finishes routing and the ListBox has
        // released mouse capture before we re-home + scroll (no drag-follow possible).
        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                if (IsDropDownOpen)
                {
                    SetCurrentValue(IsDropDownOpenProperty, false);
                }
            }));
    }

    // Detach the popup-host button-up handler and clear the pending flag. Safe to call
    // when nothing is armed. Covers the drag-out-and-release-outside case (the popup
    // host never sees the up) via the close/open transitions and template re-apply.
    private void CancelPendingCommit()
    {
        if (_commitPending && _popupHost is not null)
        {
            _popupHost.RemoveHandler(
                UIElement.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(OnPopupHostPreviewMouseLeftButtonUp));
        }

        _commitPending = false;
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((InRibbonGallery)d).HandleDropDownStateChanged((bool)e.NewValue);
    }

    /// <summary>
    /// Re-homing is driven by the PROPERTY change, never by Popup.Opened/Closed:
    /// when this gallery sits inside another popup (a collapsed group's flyout) and
    /// that outer popup closes, WPF tears the inner popup down asynchronously and
    /// its Closed event cannot be relied on — the items presenter would stay
    /// orphaned in the popup and the strip would render empty. (Popup.Opened is only
    /// used for a viewport refresh below, which is a no-op if the open is torn down.)
    /// </summary>
    private void HandleDropDownStateChanged(bool open)
    {
        // Any state transition invalidates a held-open mouse commit.
        CancelPendingCommit();

        if (open)
        {
            _dismissHelper.OnOpened();

            // Remember where the strip sat so the reveal can glide FROM here later (an
            // upper pick then slides up, a lower pick down). Captured before zeroing.
            _stripOffsetBeforeOpen = _scrollViewer?.VerticalOffset ?? 0d;

            // Zero the shared scroller BEFORE re-homing so the popup never inherits
            // the strip's scroll position (the strip may be scrolled to a previously
            // chosen tile). Offset 0 is valid in any viewport, so this is safe to do
            // synchronously against the strip's one-row viewport. Stop any in-flight
            // strip-reveal glide first so it doesn't drive the offset back off zero.
            RibbonMotion.StopScrollAnimation(_scrollViewer);
            _scrollViewer?.ScrollToVerticalOffset(0);

            // Move the shared items presenter from the strip into the popup and show
            // a scrollbar there (the strip keeps it hidden).
            if (_contentHost?.Child is { } content && _popupHost is not null)
            {
                _contentHost.Child = null;
                _popupHost.Child = content;
            }

            _scrollViewer?.SetCurrentValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            // Fade + slide the expanded gallery in (honors the global animation level).
            // Animate the popup's INNER content, not the popup's own child border: the
            // transparent popup positions itself from that border, so transforming it would
            // bake the start offset into the popup's resting position (dropping it down).
            RibbonMotion.PlayOpen(_popupHost?.Child as FrameworkElement, RibbonAnimationAction.Gallery);
        }
        else
        {
            _dismissHelper.OnClosed();

            if (_popupHost?.Child is { } content && _contentHost is not null)
            {
                _popupHost.Child = null;
                _contentHost.Child = content;
            }

            _scrollViewer?.SetCurrentValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);

            // Reveal the committed pick in the collapsed strip — AFTER the presenter is
            // back in the strip and has re-measured its one-row viewport. Doing this
            // synchronously here reads a stale viewport (design notes §3.13); deferring
            // to Loaded lets the strip lay out first.
            ScrollSelectedIntoStrip();
        }
    }

    /// <summary>
    /// Refresh the shared scroller's layout once the presenter is actually laid out
    /// in the taller popup host, so its viewport reflects the popup height rather
    /// than the strip's stale ~54px row. Without this the ScrollViewer keeps the
    /// one-row viewport it was last measured with in the strip, and clicks below that
    /// row hit-test as "past the viewport" and clamp to the last item — the
    /// "scale-like miss" the first attempt hit (design notes §3.13).
    /// </summary>
    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        // Defer one cycle: at Opened the popup window exists but its content may not
        // be fully arranged yet. On the next Loaded tick, force a fresh measure/arrange.
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                if (!IsDropDownOpen || _scrollViewer is null)
                {
                    return;
                }

                RibbonMotion.StopScrollAnimation(_scrollViewer);
                _scrollViewer.ScrollToVerticalOffset(0);
                _scrollViewer.InvalidateMeasure();
                _scrollViewer.UpdateLayout();
            }));
    }

    /// <summary>
    /// Scroll the collapsed strip so the selected tile is the visible row. Deferred
    /// to <see cref="DispatcherPriority.Loaded"/> so the strip has re-measured after
    /// the presenter returned from the popup; skipped if the popup was reopened in
    /// the meantime.
    /// </summary>
    private void ScrollSelectedIntoStrip()
    {
        if (SelectedItem is null)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                if (IsDropDownOpen || _scrollViewer is null)
                {
                    return;
                }

                if (ItemContainerGenerator.ContainerFromItem(SelectedItem) is FrameworkElement container)
                {
                    // Make sure the strip's one-row viewport/extent are current before
                    // computing how far to scroll.
                    _scrollViewer.UpdateLayout();

                    if (!_scrollViewer.IsAncestorOf(container))
                    {
                        // Defensive: if the container isn't under the scroller (shouldn't
                        // happen once re-homed), fall back to the instant reveal.
                        container.BringIntoView();
                        return;
                    }

                    // Offset that brings the selected tile's row to the top of the strip's
                    // one-row viewport: current offset + the tile's Y relative to the viewport.
                    double y = container
                        .TransformToAncestor(_scrollViewer)
                        .Transform(default)
                        .Y;
                    double target = _scrollViewer.VerticalOffset + y;
                    target = Math.Max(0d, Math.Min(target, _scrollViewer.ScrollableHeight));

                    // Glide to it (Office-style) instead of jumping — starting from where the
                    // strip sat before the popup opened, so the direction matches the pick
                    // (upper tile → slides up, lower tile → slides down).
                    double from = Math.Max(0d, Math.Min(_stripOffsetBeforeOpen, _scrollViewer.ScrollableHeight));
                    RibbonMotion.AnimateScrollToVerticalOffset(
                        _scrollViewer, target, RibbonAnimationAction.RibbonScroll, from);
                }
            }));
    }

    private void OnLineUpClick(object sender, RoutedEventArgs e) => AnimateStripScroll(-1);

    private void OnLineDownClick(object sender, RoutedEventArgs e) => AnimateStripScroll(+1);

    /// <summary>
    /// Glide the gallery one viewport toward <paramref name="direction"/> (−1 up, +1 down) —
    /// one tile row in the collapsed strip, one page in the expanded popup — matching the
    /// old PageUp/PageDown reach but animated. Whichever host currently owns the shared
    /// scroller (strip or popup) is the one that scrolls.
    /// </summary>
    private void AnimateStripScroll(int direction)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        double target = _scrollViewer.VerticalOffset + (direction * _scrollViewer.ViewportHeight);
        target = Math.Max(0d, Math.Min(target, _scrollViewer.ScrollableHeight));
        RibbonMotion.AnimateScrollToVerticalOffset(_scrollViewer, target, RibbonAnimationAction.RibbonScroll);
    }
}
