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
    private FrameworkElement? _classicOrbProxy;
    private Point _classicOrbProxyOrigin;
    private Size _classicOrbProxySize;

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
        (_child is null ? 0 : 1) + (_classicOrbProxy is null ? 0 : 1);

    /// <summary>
    /// Hosts the Classic2007 Backstage's private orb proxy above the animated surface.
    /// The real ribbon application button remains in its original layout slot.
    /// </summary>
    internal void AttachClassicOrbProxy(FrameworkElement proxy, Point origin, Size size)
    {
        if (!ReferenceEquals(_classicOrbProxy, proxy))
        {
            DetachClassicOrbProxy();
            _classicOrbProxy = proxy;
            AddVisualChild(proxy);
            AddLogicalChild(proxy);
            InvalidateMeasure();
        }

        _classicOrbProxyOrigin = origin;
        _classicOrbProxySize = size;
        InvalidateArrange();
    }

    /// <summary>Releases the Classic2007 orb proxy.</summary>
    internal void DetachClassicOrbProxy()
    {
        if (_classicOrbProxy is null)
        {
            return;
        }

        RemoveVisualChild(_classicOrbProxy);
        RemoveLogicalChild(_classicOrbProxy);
        _classicOrbProxy = null;
        InvalidateMeasure();
    }

    /// <summary>Releases the hosted backstage so it can be shown again later.</summary>
    public void Detach()
    {
        DetachClassicOrbProxy();

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

        return index == 0 && _classicOrbProxy is not null
            ? _classicOrbProxy
            : throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        Size size = AdornedElement.RenderSize;
        _child?.Measure(size);
        _classicOrbProxy?.Measure(_classicOrbProxySize);
        return size;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _child?.Arrange(new Rect(new Point(0, 0), AdornedElement.RenderSize));
        _classicOrbProxy?.Arrange(new Rect(_classicOrbProxyOrigin, _classicOrbProxySize));
        return AdornedElement.RenderSize;
    }
}
