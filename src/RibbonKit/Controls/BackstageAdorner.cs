using System.Windows;
using System.Windows.Data;
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
    private FrameworkElement? _flowBoundChild;

    public BackstageAdorner(
        UIElement adornedElement,
        UIElement backstage,
        FrameworkElement flowSource)
        : base(adornedElement)
    {
        BindingOperations.SetBinding(
            this,
            FlowDirectionProperty,
            new Binding(nameof(FrameworkElement.FlowDirection))
            {
                Source = flowSource,
                Mode = BindingMode.OneWay,
            });

        _child = backstage;
        AddVisualChild(backstage);
        AddLogicalChild(backstage);

        if (backstage is FrameworkElement frameworkBackstage)
        {
            ValueSource flowValueSource = DependencyPropertyHelper.GetValueSource(
                frameworkBackstage,
                FlowDirectionProperty);
            if (flowValueSource.BaseValueSource is BaseValueSource.Default or BaseValueSource.Inherited)
            {
                // Bind after establishing the logical parent. This keeps an application-owned
                // local/style value intact, while a default/inherited Backstage tracks the
                // adorner's live flow even though the adorner branch crosses the physical LTR
                // window frame.
                BindingOperations.SetBinding(
                    frameworkBackstage,
                    FlowDirectionProperty,
                    new Binding(nameof(FlowDirection))
                    {
                        Source = this,
                        Mode = BindingMode.OneWay,
                    });
                _flowBoundChild = frameworkBackstage;
            }
        }
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

        if (_flowBoundChild is not null)
        {
            BindingOperations.ClearBinding(_flowBoundChild, FlowDirectionProperty);
            _flowBoundChild = null;
        }

        BindingOperations.ClearBinding(this, FlowDirectionProperty);
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
