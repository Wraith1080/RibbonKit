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
/// </remarks>
public class RibbonTabControl : TabControl
{
    /// <summary>Horizontal inset of the underline from each edge of the tab (matches the old per-tab underline's <c>Margin="10,0"</c>).</summary>
    private const double UnderlineInset = 10d;

    private Rectangle? _marker;
    private TranslateTransform? _markerTranslate;
    private Panel? _markerHost;
    private bool _markerPlaced;

    static RibbonTabControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonTabControl),
            new FrameworkPropertyMetadata(typeof(RibbonTabControl)));
    }

    /// <summary>Initializes a new <see cref="RibbonTabControl"/>.</summary>
    public RibbonTabControl()
    {
        // Keep the marker under the selected tab when the strip reflows (window resize). These
        // don't fire on tab selection (the strip's own size is unchanged), so they never race the
        // selection glide.
        SizeChanged += (_, _) => UpdateMarker(animate: false);
        Loaded += (_, _) => UpdateMarker(animate: false);
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

        // Selection may already be set before the template applied; place once layout has run.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => UpdateMarker(animate: false)));
    }

    /// <inheritdoc />
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        UpdateMarker(animate: true);
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

    /// <summary>True when the brush would actually paint something — i.e. it isn't null and isn't a fully transparent solid colour (how flat themes disable the underline).</summary>
    private static bool IsVisibleBrush(Brush brush) => brush is not SolidColorBrush solid || solid.Color.A != 0;
}
