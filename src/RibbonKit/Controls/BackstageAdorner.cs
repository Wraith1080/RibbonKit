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
    private FrameworkElement? _applicationButton;
    private Point _applicationButtonOrigin;
    private Size _applicationButtonSize;

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
    protected override int VisualChildrenCount =>
        (_child is null ? 0 : 1) + (_applicationButton is null ? 0 : 1);

    /// <summary>Hosts the ribbon's real application button above the Backstage surface.</summary>
    internal void AttachApplicationButton(FrameworkElement button, Point origin, Size size)
    {
        DetachApplicationButton();
        _applicationButton = button;
        _applicationButtonOrigin = origin;
        _applicationButtonSize = size;
        AddVisualChild(button);
        AddLogicalChild(button);
        InvalidateMeasure();
    }

    /// <summary>Releases the application button so the ribbon can restore its original slot.</summary>
    internal void DetachApplicationButton()
    {
        if (_applicationButton is null)
        {
            return;
        }

        RemoveVisualChild(_applicationButton);
        RemoveLogicalChild(_applicationButton);
        _applicationButton = null;
        InvalidateMeasure();
    }

    /// <summary>Releases the hosted backstage so it can be shown again later.</summary>
    public void Detach()
    {
        DetachApplicationButton();

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
    protected override Visual GetVisualChild(int index)
    {
        if (_child is not null)
        {
            if (index == 0)
            {
                return _child;
            }

            index--;
        }

        return index == 0 && _applicationButton is not null
            ? _applicationButton
            : throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        Size size = AdornedElement.RenderSize;
        _child?.Measure(size);
        _applicationButton?.Measure(_applicationButtonSize);
        return size;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _child?.Arrange(new Rect(new Point(0, 0), AdornedElement.RenderSize));
        _applicationButton?.Arrange(new Rect(_applicationButtonOrigin, _applicationButtonSize));
        return AdornedElement.RenderSize;
    }
}
