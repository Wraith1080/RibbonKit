using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// A single KeyTip badge rendered over a target element in its adorner layer: a small
/// themed rounded chip carrying the access key(s), anchored to the lower edge of the
/// target the way Office draws them. Non-interactive; the <see cref="KeyTipService"/>
/// creates, positions, dims, and removes these.
/// </summary>
internal sealed class KeyTipAdorner : Adorner
{
    private readonly Border _badge;
    private (Point Origin, Point X, Point Y, Size Size)? _lastPlacement;

    internal KeyTipAdorner(UIElement adornedElement, string keys)
        : base(adornedElement)
    {
        IsHitTestVisible = false;

        var text = new TextBlock
        {
            Text = keys,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "RibbonKit.Brushes.KeyTip.Foreground");

        _badge = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(4, 0, 4, 0),
            MinWidth = 16,
            MinHeight = 16,
            SnapsToDevicePixels = true,
            Child = text,
        };
        _badge.SetResourceReference(Border.BackgroundProperty, "RibbonKit.Brushes.KeyTip.Background");
        _badge.SetResourceReference(Border.BorderBrushProperty, "RibbonKit.Brushes.KeyTip.Border");

        AddVisualChild(_badge);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _lastPlacement = null;
        CompositionTarget.Rendering -= OnRendering;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
        _lastPlacement = null;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (VisualTreeHelper.GetParent(this) is not AdornerLayer layer
            || !AdornedElement.IsVisible
            || VisualTreeHelper.GetParent(layer) is not Visual root
            || !root.IsAncestorOf(AdornedElement))
        {
            return;
        }

        // AdornerLayer caches the target-to-layer transform during layout. Backstage's
        // render-only slide can keep moving after that layout, leaving badges at an
        // intermediate position until unrelated input causes another layout pass.
        // Track the full coordinate basis (including RTL/scaling), refreshing only
        // when placement changes, and only while the badge is loaded.
        GeneralTransform transform = AdornedElement.TransformToVisual(layer);
        var placement = (
            transform.Transform(new Point(0, 0)),
            transform.Transform(new Point(1, 0)),
            transform.Transform(new Point(0, 1)),
            AdornedElement.RenderSize);
        if (_lastPlacement == placement)
        {
            return;
        }

        _lastPlacement = placement;
        layer.Update(AdornedElement);
    }

    /// <summary>Dims the badge when its key no longer matches what the user has typed.</summary>
    internal bool Dimmed
    {
        set => Opacity = value ? 0.3 : 1.0;
    }

    /// <inheritdoc />
    protected override int VisualChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) => _badge;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        _badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return AdornedElement.RenderSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        Size badge = _badge.DesiredSize;

        // Centered horizontally, straddling the target's bottom edge — clamped so the
        // badge never spills outside the target horizontally for very narrow controls.
        double x = Math.Max(0, (finalSize.Width - badge.Width) / 2);
        double y = Math.Max(0, finalSize.Height - (badge.Height / 2) - 1);

        _badge.Arrange(new Rect(new Point(x, y), badge));
        return finalSize;
    }
}
