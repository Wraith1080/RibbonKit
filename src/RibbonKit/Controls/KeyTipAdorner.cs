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
