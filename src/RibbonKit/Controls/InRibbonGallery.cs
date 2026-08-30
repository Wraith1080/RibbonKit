using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Theming;

namespace RibbonKit.Controls;

/// <summary>
/// A gallery that lives directly in the ribbon: a compact strip of tiles with
/// scroll-up / scroll-down / expand buttons on its right edge. The expand button
/// opens an overlay popup showing the full wrapped gallery, positioned over the
/// strip like Office. Committing a pick in the popup closes it (Office-style) and
/// scrolls the collapsed strip to reveal the chosen tile.
/// </summary>
/// <remarks>
/// The strip and popup keep separate permanent <see cref="ScrollViewer"/> instances
/// and share one items presenter, which is re-homed between those viewports while the
/// popup is open. This keeps viewport and clip state inside one HWND/DPI context.
/// Light-dismiss comes from <see cref="PopupDismissHelper"/> — the popup itself never
/// takes mouse capture.
///
/// <para><b>Scroll-to-chosen-item (design notes §3.13).</b> Two independent bugs had
/// to be solved, on the OPEN path and the CLOSE path:</para>
///
/// <para><b>Open path — popup hit-testing.</b> The popup viewport must be at offset 0
/// and measured after the presenter arrives, before any click hit-tests. The popup's
/// own permanent scroller prevents the strip's one-row viewport and clip from crossing
/// the separate Popup HWND.</para>
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
[TemplatePart(Name = PopupScrollViewerPartName, Type = typeof(ScrollViewer))]
[TemplatePart(Name = ItemsPresenterPartName, Type = typeof(ItemsPresenter))]
[TemplatePart(Name = LineUpPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = LineDownPartName, Type = typeof(ButtonBase))]
public class InRibbonGallery : RibbonGallery
{
    private const string ContentHostPartName = "PART_ContentHost";
    private const string PopupHostPartName = "PART_PopupHost";
    private const string PopupPartName = "PART_Popup";
    private const string ScrollViewerPartName = "PART_ScrollViewer";
    private const string PopupScrollViewerPartName = "PART_PopupScrollViewer";
    private const string ItemsPresenterPartName = "PART_ItemsPresenter";
    private const string LineUpPartName = "PART_LineUp";
    private const string LineDownPartName = "PART_LineDown";
    private const string RibbonContentBackgroundResourceKey =
        "RibbonKit.Brushes.Ribbon.ContentBackground";

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
    private ScrollViewer? _stripScrollViewer;
    private ScrollViewer? _popupScrollViewer;
    private ItemsPresenter? _itemsPresenter;
    private ButtonBase? _lineUp;
    private ButtonBase? _lineDown;
    private Window? _dpiOwner;
    private int _viewportRefreshGeneration;

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
        Loaded += OnGalleryLoaded;
        Unloaded += OnGalleryUnloaded;
    }

    private void OnGalleryLoaded(object sender, RoutedEventArgs e)
    {
        ThemeManager.Changed -= OnThemeConfigurationChanged;
        ThemeManager.Changed += OnThemeConfigurationChanged;
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        AttachDpiOwner();
    }

    private void OnGalleryUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeManager.Changed -= OnThemeConfigurationChanged;
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        _viewportRefreshGeneration++;

        // A collapsed RibbonGroup temporarily re-homes this control through a Popup,
        // producing an unload/load pair where Window.GetWindow can briefly be null.
        // Keep the known owner through that transition, but release it if the gallery
        // is genuinely removed and stays unloaded.
        Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(() =>
            {
                if (!IsLoaded)
                {
                    DetachDpiOwner();
                }
            }));

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
            _popup.CustomPopupPlacementCallback = null;
        }

        CancelPendingCommit();

        base.OnApplyTemplate();

        _contentHost = GetTemplateChild(ContentHostPartName) as Decorator;
        _popupHost = GetTemplateChild(PopupHostPartName) as Border;
        _popup = GetTemplateChild(PopupPartName) as Popup;
        _stripScrollViewer = GetTemplateChild(ScrollViewerPartName) as ScrollViewer;
        _popupScrollViewer = GetTemplateChild(PopupScrollViewerPartName) as ScrollViewer;
        _itemsPresenter = GetTemplateChild(ItemsPresenterPartName) as ItemsPresenter;
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
            _popup.CustomPopupPlacementCallback = PlacePopupBesideSideButtons;
        }

        // A template can be reapplied while the DP remains true, in which case no
        // property-change callback will run for the new visual tree.
        if (IsDropDownOpen)
        {
            MoveGalleryContent(open: true);
        }
    }

    private CustomPopupPlacement[] PlacePopupBesideSideButtons(
        Size popupSize,
        Size targetSize,
        Point offset)
    {
        // Keep the separate popup HWND out of the side-button column without
        // constraining the gallery card itself. In LTR the card keeps its natural
        // width and expands left from the content host's right edge; RTL mirrors
        // that behavior and expands right from its left edge.
        Thickness chromeMargin = _popupHost?.Margin ?? default;
        double x = FlowDirection == FlowDirection.RightToLeft
            ? -offset.X
            : targetSize.Width - popupSize.Width - chromeMargin.Left - chromeMargin.Right + offset.X;

        return new[]
        {
            new CustomPopupPlacement(
                new Point(x, offset.Y),
                PopupPrimaryAxis.Horizontal),
        };
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
        // It also invalidates a deferred viewport pass for the previous host. An open
        // transition queues a fresh popup pass from OnPopupOpened; a close transition
        // lets ScrollSelectedIntoStrip own the newly re-homed strip layout.
        _viewportRefreshGeneration++;

        if (open)
        {
            _dismissHelper.OnOpened();

            // Remember where the strip sat so the reveal can glide FROM here later (an
            // upper pick then slides up, a lower pick down). Captured before zeroing.
            _stripOffsetBeforeOpen = _stripScrollViewer?.VerticalOffset ?? 0d;

            // The strip viewport never crosses into the Popup HWND. Stop any strip
            // reveal still in flight, reset the popup's independent viewport, then
            // move only the ItemsPresenter into it.
            RibbonMotion.StopScrollAnimation(_stripScrollViewer);
            RibbonMotion.StopScrollAnimation(_popupScrollViewer);
            _popupScrollViewer?.ScrollToVerticalOffset(0d);
            MoveGalleryContent(open: true);

            // Unfold the WHOLE flyout surface — border, shadow and tiles together (honors the
            // global animation level). This used to animate only _popupHost.Child, on the theory
            // that transforming the border would drop the transparent popup's resting position;
            // it does not — a RenderTransform never moves the popup window. What a TRANSLATE does
            // is get sliced against that window's top edge, which is why this is a scale: it never
            // leaves its resting bounds, so the overlay's -4 placement is untouched. See §3.42.
            RibbonMotion.PlayFlyoutOpen(_popupHost, RibbonAnimationAction.Gallery);
        }
        else
        {
            _dismissHelper.OnClosed();

            // Clear the popup's page while it remains in the Popup HWND, then return
            // only the presenter. The strip's viewport/clip object has never left the
            // main window and therefore cannot inherit the popup's stale DPI geometry.
            RibbonMotion.StopScrollAnimation(_popupScrollViewer);
            _popupScrollViewer?.ScrollToVerticalOffset(0d);
            MoveGalleryContent(open: false);

            if (_stripScrollViewer is not null)
            {
                RefreshViewportLayout(_stripScrollViewer);
            }

            // Reveal the committed pick in the collapsed strip — AFTER the presenter is
            // back in the strip and has re-measured its one-row viewport. Doing this
            // synchronously here reads a stale viewport (design notes §3.13); deferring
            // to Loaded lets the strip lay out first.
            ScrollSelectedIntoStrip();
        }
    }

    private bool UsesHostSpecificScrollers =>
        _stripScrollViewer is not null
        && _popupScrollViewer is not null
        && _itemsPresenter is not null;

    private ScrollViewer? ActiveScrollViewer =>
        IsDropDownOpen && UsesHostSpecificScrollers
            ? _popupScrollViewer
            : _stripScrollViewer;

    private void MoveGalleryContent(bool open)
    {
        if (UsesHostSpecificScrollers)
        {
            ScrollViewer source = open ? _stripScrollViewer! : _popupScrollViewer!;
            ScrollViewer destination = open ? _popupScrollViewer! : _stripScrollViewer!;

            if (ReferenceEquals(source.Content, _itemsPresenter))
            {
                source.Content = null;
            }

            if (!ReferenceEquals(destination.Content, _itemsPresenter))
            {
                destination.Content = _itemsPresenter;
            }

            return;
        }

        // Preserve the original template-part contract for custom templates that do
        // not yet provide PART_PopupScrollViewer/PART_ItemsPresenter. The default
        // template uses the host-specific path above.
        if (open)
        {
            RibbonMotion.StopScrollAnimation(_stripScrollViewer);
            _stripScrollViewer?.ScrollToVerticalOffset(0d);

            if (_contentHost?.Child is { } content && _popupHost is not null)
            {
                _contentHost.Child = null;
                _popupHost.Child = content;
            }

            _stripScrollViewer?.SetCurrentValue(
                ScrollViewer.VerticalScrollBarVisibilityProperty,
                ScrollBarVisibility.Auto);
        }
        else
        {
            RibbonMotion.StopScrollAnimation(_stripScrollViewer);
            _stripScrollViewer?.ScrollToVerticalOffset(0d);

            if (_popupHost?.Child is { } content && _contentHost is not null)
            {
                _popupHost.Child = null;
                _contentHost.Child = content;
            }

            _stripScrollViewer?.SetCurrentValue(
                ScrollViewer.VerticalScrollBarVisibilityProperty,
                ScrollBarVisibility.Hidden);
        }
    }

    /// <summary>
    /// Refresh the popup scroller's layout once the presenter is actually laid out
    /// in the taller popup host, so its viewport reflects the popup height rather
    /// than the strip's stale ~54px row. Without this the ScrollViewer keeps the
    /// one-row viewport it was last measured with in the strip, and clicks below that
    /// row hit-test as "past the viewport" and clamp to the last item — the
    /// "scale-like miss" the first attempt hit (design notes §3.13).
    /// </summary>
    private void OnPopupOpened(object? sender, EventArgs e)
    {
        ResolvePopupHostBackground();
        PrepareViewportRefresh();
        QueueViewportRefresh();
    }

    private void AttachDpiOwner()
    {
        Window? owner = Window.GetWindow(this);
        if (owner is null)
        {
            return;
        }

        if (ReferenceEquals(owner, _dpiOwner))
        {
            return;
        }

        DetachDpiOwner();
        _dpiOwner = owner;
        if (_dpiOwner is not null)
        {
            _dpiOwner.DpiChanged += OnOwnerDpiChanged;
        }
    }

    private void DetachDpiOwner()
    {
        if (_dpiOwner is not null)
        {
            _dpiOwner.DpiChanged -= OnOwnerDpiChanged;
            _dpiOwner = null;
        }
    }

    private void OnOwnerDpiChanged(object sender, DpiChangedEventArgs e)
    {
        // Reset both host-specific offsets synchronously, then remeasure the active
        // viewport after WPF has applied the new root/Popup DPI. Neither ScrollViewer
        // crosses the HWND boundary; only the presenter moves on the next open/close.
        PrepareViewportRefresh();
        QueueViewportRefresh();
    }

    private void PrepareViewportRefresh()
    {
        RibbonMotion.StopScrollAnimation(_stripScrollViewer);
        _stripScrollViewer?.ScrollToVerticalOffset(0d);

        if (!ReferenceEquals(_popupScrollViewer, _stripScrollViewer))
        {
            RibbonMotion.StopScrollAnimation(_popupScrollViewer);
            _popupScrollViewer?.ScrollToVerticalOffset(0d);
        }
    }

    private void QueueViewportRefresh()
    {
        ScrollViewer? activeScrollViewer = ActiveScrollViewer;
        if (activeScrollViewer is null)
        {
            return;
        }

        int generation = ++_viewportRefreshGeneration;
        QueueViewportRefreshPass(
            generation,
            activeScrollViewer,
            DispatcherPriority.Loaded,
            queueRenderPass: true);
    }

    private void QueueViewportRefreshPass(
        int generation,
        ScrollViewer expectedScrollViewer,
        DispatcherPriority priority,
        bool queueRenderPass)
    {
        Dispatcher.BeginInvoke(
            priority,
            new Action(() =>
            {
                if (generation != _viewportRefreshGeneration
                    || !IsLoaded
                    || !ReferenceEquals(expectedScrollViewer, ActiveScrollViewer))
                {
                    return;
                }

                RefreshViewportLayout(expectedScrollViewer);

                if (queueRenderPass)
                {
                    QueueViewportRefreshPass(
                        generation,
                        expectedScrollViewer,
                        DispatcherPriority.Render,
                        queueRenderPass: false);
                }
                else if (!IsDropDownOpen)
                {
                    RestoreSelectedStripOffset(expectedScrollViewer);
                }
            }));
    }

    private void RefreshViewportLayout(ScrollViewer scrollViewer)
    {
        RibbonMotion.StopScrollAnimation(scrollViewer);
        scrollViewer.ScrollToVerticalOffset(0d);

        FrameworkElement? activeHost = IsDropDownOpen ? _popupHost : _contentHost;
        if (scrollViewer.Content is UIElement content)
        {
            content.InvalidateMeasure();
            content.InvalidateArrange();
        }

        scrollViewer.InvalidateMeasure();
        scrollViewer.InvalidateArrange();
        activeHost?.InvalidateMeasure();
        activeHost?.InvalidateArrange();
        InvalidateMeasure();
        InvalidateArrange();

        activeHost?.UpdateLayout();
        scrollViewer.UpdateLayout();
    }


    private void RestoreSelectedStripOffset(ScrollViewer scrollViewer)
    {
        if (SelectedItem is null
            || ItemContainerGenerator.ContainerFromItem(SelectedItem) is not FrameworkElement container
            || !scrollViewer.IsAncestorOf(container))
        {
            return;
        }

        scrollViewer.UpdateLayout();
        double target = container
            .TransformToAncestor(scrollViewer)
            .Transform(default)
            .Y;
        target = Math.Max(0d, Math.Min(target, scrollViewer.ScrollableHeight));
        scrollViewer.ScrollToVerticalOffset(target);
        scrollViewer.UpdateLayout();
    }

    private void OnThemeConfigurationChanged(object? sender, EventArgs e)
    {
        if (IsDropDownOpen)
        {
            ResolvePopupHostBackground();
        }
    }

    private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsDropDownOpen
            && (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(SystemParameters.HighContrast)))
        {
            ResolvePopupHostBackground();
        }
    }

    /// <summary>
    /// Resolves the popup surface from the gallery while it is still connected to the host's
    /// resource scope. A Popup owns a separate HWND, where the template's DynamicResource can
    /// otherwise remain unresolved and leave the card transparent.
    /// </summary>
    private void ResolvePopupHostBackground()
    {
        if (_popupHost is null)
        {
            return;
        }

        Brush background = SystemParameters.HighContrast
            ? SystemColors.WindowBrush
            : TryFindResource(RibbonContentBackgroundResourceKey) as Brush
                ?? SystemColors.WindowBrush;
        _popupHost.SetCurrentValue(Border.BackgroundProperty, background);
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
                if (IsDropDownOpen || _stripScrollViewer is null)
                {
                    return;
                }

                if (ItemContainerGenerator.ContainerFromItem(SelectedItem) is FrameworkElement container)
                {
                    // Make sure the strip's one-row viewport/extent are current before
                    // computing how far to scroll.
                    _stripScrollViewer.UpdateLayout();

                    if (!_stripScrollViewer.IsAncestorOf(container))
                    {
                        // Defensive: if the container isn't under the scroller (shouldn't
                        // happen once re-homed), fall back to the instant reveal.
                        container.BringIntoView();
                        return;
                    }

                    // Offset that brings the selected tile's row to the top of the strip's
                    // one-row viewport: current offset + the tile's Y relative to the viewport.
                    double y = container
                        .TransformToAncestor(_stripScrollViewer)
                        .Transform(default)
                        .Y;
                    double target = _stripScrollViewer.VerticalOffset + y;
                    target = Math.Max(0d, Math.Min(target, _stripScrollViewer.ScrollableHeight));

                    // Glide to it (Office-style) instead of jumping — starting from where the
                    // strip sat before the popup opened, so the direction matches the pick
                    // (upper tile → slides up, lower tile → slides down).
                    double from = Math.Max(0d, Math.Min(_stripOffsetBeforeOpen, _stripScrollViewer.ScrollableHeight));
                    RibbonMotion.AnimateScrollToVerticalOffset(
                        _stripScrollViewer, target, RibbonAnimationAction.RibbonScroll, from);
                }
            }));
    }

    private void OnLineUpClick(object sender, RoutedEventArgs e) => AnimateStripScroll(-1);

    private void OnLineDownClick(object sender, RoutedEventArgs e) => AnimateStripScroll(+1);

    /// <summary>
    /// Glide the gallery one viewport toward <paramref name="direction"/> (−1 up, +1 down) —
    /// one tile row in the collapsed strip, one page in the expanded popup — matching the
    /// old PageUp/PageDown reach but animated. The active host's permanent scroller is
    /// used, so no viewport object crosses the Popup HWND boundary.
    /// </summary>
    private void AnimateStripScroll(int direction)
    {
        ScrollViewer? scrollViewer = ActiveScrollViewer;
        if (scrollViewer is null)
        {
            return;
        }

        // A live DPI transition can invalidate the cached extent/viewport before WPF's
        // next render. Refresh synchronously at the command boundary so PageDown never
        // targets an empty page derived from the previous monitor's metrics.
        scrollViewer.InvalidateMeasure();
        scrollViewer.UpdateLayout();

        double viewport = scrollViewer.ViewportHeight;
        if (!double.IsFinite(viewport) || viewport <= 0d)
        {
            return;
        }

        double target = scrollViewer.VerticalOffset + (direction * viewport);
        target = Math.Max(0d, Math.Min(target, scrollViewer.ScrollableHeight));
        RibbonMotion.AnimateScrollToVerticalOffset(scrollViewer, target, RibbonAnimationAction.RibbonScroll);
    }
}
