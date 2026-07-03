using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// Hosts the backstage as a full-window overlay in the window's adorner layer.
/// Unlike a Popup (a separate top-level window that ignores minimize and doesn't
/// follow the window), the adorner layer lives inside the window's visual tree, so
/// the overlay moves, resizes, minimizes, and z-orders with the window naturally.
/// </summary>
internal sealed class BackstageAdorner : Adorner
{
    private UIElement? _child;

    public BackstageAdorner(UIElement adornedElement, UIElement backstage)
        : base(adornedElement)
    {
        _child = backstage;
        AddVisualChild(backstage);
        AddLogicalChild(backstage);
    }

    /// <inheritdoc />
    protected override int VisualChildrenCount => _child is null ? 0 : 1;

    /// <summary>Releases the hosted backstage so it can be shown again later.</summary>
    public void Detach()
    {
        if (_child is not null)
        {
            RemoveVisualChild(_child);
            RemoveLogicalChild(_child);
            _child = null;
        }
    }

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) => _child!;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        Size size = AdornedElement.RenderSize;
        _child?.Measure(size);
        return size;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _child?.Arrange(new Rect(new Point(0, 0), AdornedElement.RenderSize));
        return AdornedElement.RenderSize;
    }
}
