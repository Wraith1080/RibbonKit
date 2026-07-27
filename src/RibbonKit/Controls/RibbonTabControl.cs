using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

/// <summary>
/// The tab host inside a <see cref="Ribbon"/>: a restyled <see cref="TabControl"/>
/// whose headers form the ribbon tab strip and whose content area shows the selected
/// tab's groups row. Usually created by the <see cref="Ribbon"/> template rather than
/// used directly.
/// </summary>
/// <remarks>
/// Owns the <b>sliding selection marker</b>: a single underline (<c>PART_TabMarker</c> in the
/// template) that glides — position and width — from the old tab to the newly selected one,
/// instead of the underline cutting instantly. Driven by the <see cref="RibbonAnimationAction.TabMarker"/>
/// timing, so it snaps when that action is disabled or under system reduced-motion. Flat themes
/// leave <c>Tab.SelectedUnderline</c> transparent, so the marker is simply invisible there.
/// <para>
/// Also owns the <b>body-border notch</b> (<c>PART_ConnectNotch</c>): in the connected-tab themes
/// (Office 2010/2013) a 1px body-coloured sliver in the BODY row that covers the body's top border
/// directly under the selected tab, so the tab appears to open into the body. See
/// <see cref="UpdateConnectNotch"/> for why it cannot live inside the tab strip.
/// </para>
/// </remarks>
public class RibbonTabControl : TabControl
{
    /// <summary>Horizontal inset of the underline from each edge of the tab (matches the old per-tab underline's <c>Margin="10,0"</c>).</summary>
    private const double UnderlineInset = 10d;

    private Rectangle? _marker;
    private TranslateTransform? _markerTranslate;
    private Panel? _markerHost;
    private bool _markerPlaced;
    private RibbonKit.Layout.RibbonScrollContentHost? _contentScroll;
    private RibbonKit.Layout.RibbonScrollContentHost? _tabScroll;
    private Border? _connectNotch;
    private TranslateTransform? _connectNotchTranslate;

    static RibbonTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonTabControl),
            new FrameworkPropertyMetadata(typeof(RibbonTabControl)));
    }

    /// <summary>Initializes a new <see cref="RibbonTabControl"/>.</summary>
    public RibbonTabControl()
    {
        // Keep the marker and the body-border notch under the selected tab when the strip
        // reflows (window resize). These don't fire on tab selection (the strip's own size is
        // unchanged), so they never race the selection glide.
        SizeChanged += (_, _) => { UpdateMarker(animate: false); UpdateConnectNotch(); };
        Loaded += (_, _) => { UpdateMarker(animate: false); UpdateConnectNotch(); };
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonTab;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new RibbonTab();

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _marker = GetTemplateChild("PART_TabMarker") as Rectangle;
        _markerTranslate = GetTemplateChild("PART_TabMarkerTranslate") as TranslateTransform;
        _markerHost = _marker?.Parent as Panel;
        _markerPlaced = false;
        _contentScroll = GetTemplateChild("PART_ContentScroll") as RibbonKit.Layout.RibbonScrollContentHost;

        // The notch tracks the selected tab from OUTSIDE the tab scroller, so it must be told
        // when the strip scrolls under it (every frame of a glide). Re-applying the template
        // swaps the scroller instance: detach from the old one before attaching to the new.
        if (_tabScroll is not null)
        {
            _tabScroll.OffsetChanged -= OnTabScrollOffsetChanged;
        }

        _tabScroll = GetTemplateChild("PART_TabScroll") as RibbonKit.Layout.RibbonScrollContentHost;
        if (_tabScroll is not null)
        {
            _tabScroll.OffsetChanged += OnTabScrollOffsetChanged;
        }

        _connectNotch = GetTemplateChild("PART_ConnectNotch") as Border;
        _connectNotchTranslate = GetTemplateChild("PART_ConnectNotchTranslate") as TranslateTransform;

        // Selection may already be set before the template applied; place once layout has run.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => { UpdateMarker(animate: false); UpdateConnectNotch(); }));
    }

    /// <inheritdoc />
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        UpdateMarker(animate: true);
        UpdateConnectNotch();

        // Swapping the selected tab replaces the groups row inside the shared content scroller without
        // changing any size, so WPF reuses each level's cached measure and the scroller keeps the
        // previous tab's overflow state (the chevrons don't update for the new row). Re-evaluate once the
        // new content has been realized under the scroller — Loaded priority runs after that layout pass.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => _contentScroll?.Refresh()));
    }

    /// <summary>
    /// Positions the shared underline under the selected tab, gliding when
    /// <paramref name="animate"/> is set and the marker has already been placed at least once
    /// (so the first appearance and reflows are instant, only tab-to-tab moves glide).
    /// </summary>
    private void UpdateMarker(bool animate)
    {
        if (_marker is null || _markerTranslate is null || _markerHost is null)
        {
            return;
        }

        if (SelectedItem is not RibbonTab tab || !tab.IsVisible || tab.ActualWidth <= 0d)
        {
            _marker.Opacity = 0d;
            _markerPlaced = false;
            return;
        }

        // The sliding underline is an Office-2024 accent. Flat themes (Office 2013/2019) set
        // RibbonKit.Brushes.Tab.SelectedUnderline to Transparent to hide it. Gate the WHOLE marker on
        // that token being a visible colour — otherwise a selected contextual tab (which tints the
        // marker with its own colour below) would leak an underline into themes that don't want one.
        if (TryFindResource("RibbonKit.Brushes.Tab.SelectedUnderline") is not Brush underline || !IsVisibleBrush(underline))
        {
            _marker.Opacity = 0d;
            _markerPlaced = false;
            return;
        }

        // Selected tab's top-left in the marker host's coordinate space.
        Point origin;
        try
        {
            origin = tab.TransformToAncestor(_markerHost).Transform(new Point(0d, 0d));
        }
        catch (InvalidOperationException)
        {
            // Tab not yet connected under the host; retry after the next layout pass.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => UpdateMarker(animate)));
            return;
        }

        double targetX = origin.X + UnderlineInset;
        double targetWidth = Math.Max(0d, tab.ActualWidth - (2d * UnderlineInset));
        double targetY = origin.Y + tab.ActualHeight - _marker.Height;

        // Tint: a selected contextual tab underlines in its own colour; otherwise the theme's
        // accent underline token (which flat themes set Transparent, hiding the marker there).
        if (tab.IsContextual && tab.ContextualBrush is { } contextual)
        {
            _marker.Fill = contextual;
        }
        else
        {
            _marker.SetResourceReference(Shape.FillProperty, "RibbonKit.Brushes.Tab.SelectedUnderline");
        }

        _markerTranslate.Y = targetY;

        bool glide = animate && _markerPlaced && RibbonAnimation.IsEnabled(RibbonAnimationAction.TabMarker);
        if (glide)
        {
            Duration duration = RibbonAnimation.GetDuration(RibbonAnimationAction.TabMarker);
            IEasingFunction ease = RibbonAnimation.GetEase(RibbonAnimationAction.TabMarker);
            _marker.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(targetWidth, duration) { EasingFunction = ease });
            _markerTranslate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(targetX, duration) { EasingFunction = ease });
        }
        else
        {
            _marker.BeginAnimation(FrameworkElement.WidthProperty, null);
            _markerTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            _marker.Width = targetWidth;
            _markerTranslate.X = targetX;
        }

        _marker.Opacity = 1d;
        _markerPlaced = true;
    }

    /// <summary>
    /// Re-places both selection visuals (the gliding marker and the body-border notch) without
    /// animation. Called by <see cref="Ribbon"/> after a theme swap: the notch/marker tokens have
    /// changed, but no selection or size change fires, so nothing else would re-evaluate them.
    /// </summary>
    internal void RefreshSelectionVisuals()
    {
        UpdateMarker(animate: false);
        UpdateConnectNotch();
    }

    private void OnTabScrollOffsetChanged(object? sender, EventArgs e)
    {
        // Track the strip scrolling under the notch. Offset is AffectsArrange, so the scroller's
        // translate is applied on the NEXT arrange pass — the immediate update below reads the
        // PREVIOUS frame's transform and lags ~1 frame during a glide (imperceptible). The
        // Loaded-priority re-run fires after layout settles, correcting the final resting position.
        UpdateConnectNotch();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdateConnectNotch));
    }

    /// <summary>
    /// Positions the body-border notch (<c>PART_ConnectNotch</c>): a 1px body-coloured sliver
    /// lying OVER the ribbon body's top border, sized and translated to sit exactly under the
    /// selected tab, cutting a tab-wide gap into the border so the tab opens seamlessly into the
    /// body (the Office 2010/2013 connected-tab look).
    /// </summary>
    /// <remarks>
    /// The notch lives in the BODY row of the <see cref="RibbonTabControl"/> template — NOT
    /// inside the tab strip — for two reasons that defeated the in-strip approaches:
    /// <list type="bullet">
    /// <item><c>PART_TabScroll</c> clips its subtree (<c>ClipToBounds</c>), so nothing inside the
    /// strip (e.g. the tab's <c>ConnectFoot</c>) can ever paint past the strip's bottom edge onto
    /// the body's border.</item>
    /// <item><c>Panel.ZIndex</c> only orders siblings of the SAME panel; the strip (row 0) and the
    /// body (row 1) are separate branches, so no ZIndex on either subtree can reorder one against
    /// the other.</item>
    /// </list>
    /// As a later-declared sibling of <c>ContentHost</c>, the notch simply paints on top of the
    /// border. Themes without a connected tab (2019/2024) set
    /// <c>RibbonKit.Brushes.Tab.ConnectNotch</c> Transparent, which collapses it here. Deliberately
    /// NOT animated: in the connecting themes the tab's chrome (selected fill + outline) swaps
    /// instantly on selection, so the notch snaps with it — a gliding gap under an already-switched
    /// tab would read as a detached white slit sliding along the border.
    /// </remarks>
    private void UpdateConnectNotch()
    {
        if (_connectNotch is null || _connectNotchTranslate is null || _connectNotch.Parent is not Visual host)
        {
            return;
        }

        if (SelectedItem is not RibbonTab tab || !tab.IsVisible || tab.ActualWidth <= 0d
            || TryFindResource("RibbonKit.Brushes.Tab.ConnectNotch") is not Brush fill
            || !IsVisibleBrush(fill))
        {
            _connectNotch.Width = 0d;
            return;
        }

        double left, right;
        try
        {
            // TransformToVisual (not TransformToAncestor — the notch's parent is a sibling
            // branch, not an ancestor of the tab) includes the scroller's translate, so the
            // result is already in the notch parent's coordinate space, scroll and all.
            Point origin = tab.TransformToVisual(host).Transform(new Point(0d, 0d));
            left = origin.X;
            right = origin.X + tab.ActualWidth;

            // Clamp to the strip's viewport: a selected tab scrolled (partly) out of view must
            // not cut an orphan gap into the border where there is no tab above it.
            if (_tabScroll is { IsVisible: true } scroll)
            {
                Point scrollOrigin = scroll.TransformToVisual(host).Transform(new Point(0d, 0d));
                left = Math.Max(left, scrollOrigin.X);
                right = Math.Min(right, scrollOrigin.X + scroll.ActualWidth);
            }
        }
        catch (InvalidOperationException)
        {
            // Tab not yet connected under a common root; retry after the next layout pass.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdateConnectNotch));
            return;
        }

        if (right - left < 2d)
        {
            _connectNotch.Width = 0d;
            return;
        }

        _connectNotch.Width = right - left;
        _connectNotchTranslate.X = left;
    }

    /// <summary>True when the brush would actually paint something — i.e. it isn't null and isn't a fully transparent solid colour (how flat themes disable the underline).</summary>
    private static bool IsVisibleBrush(Brush brush) => brush is not SolidColorBrush solid || solid.Color.A != 0;
}
