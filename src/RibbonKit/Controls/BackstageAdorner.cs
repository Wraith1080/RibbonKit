using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// Hosts the backstage in the window-content adorner layer. The default arrangement fills that
/// surface; an optional header anchor can inset the child below exposed ribbon tabs.
/// Unlike a Popup (a separate top-level window that ignores minimize and doesn't
/// follow the window), the adorner layer lives inside the window's visual tree, so
/// the overlay moves, resizes, minimizes, and z-orders with the window naturally.
/// </summary>
internal sealed class BackstageAdorner : Adorner
{
    private UIElement? _child;
    private FrameworkElement? _flowBoundChild;
    private FrameworkElement? _topEdgeAnchor;
    private double _topInset;
    private FrameworkElement? _classicOrbProxy;
    private Point _classicOrbProxyOrigin;
    private Size _classicOrbProxySize;

    public BackstageAdorner(
        UIElement adornedElement,
        UIElement backstage,
        FrameworkElement flowSource,
        FrameworkElement? topEdgeAnchor = null)
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

        SetTopEdgeAnchor(topEdgeAnchor);
    }

    /// <summary>
    /// Raised when a deferred template/layout pass makes the optional below-header placement
    /// available or unavailable. The owning ribbon uses it to reconcile exposed chrome.
    /// </summary>
    internal event EventHandler? PlacementChanged;

    /// <summary>Whether the child is currently arranged below a realized header anchor.</summary>
    internal bool IsInsetPlacementActive => _topEdgeAnchor is not null && _topInset > 0d;

    /// <summary>The live top inset, in the adorned element's DIPs.</summary>
    internal double TopInset => _topInset;

    /// <summary>
    /// Changes the element whose bottom edge becomes the Backstage's top edge. A null or
    /// disconnected anchor restores the established full-content placement.
    /// </summary>
    internal void SetTopEdgeAnchor(FrameworkElement? anchor)
    {
        if (ReferenceEquals(_topEdgeAnchor, anchor))
        {
            RefreshPlacement();
            return;
        }

        if (_topEdgeAnchor is not null)
        {
            _topEdgeAnchor.LayoutUpdated -= OnTopEdgeAnchorLayoutUpdated;
        }

        _topEdgeAnchor = anchor;
        if (_topEdgeAnchor is not null)
        {
            _topEdgeAnchor.LayoutUpdated += OnTopEdgeAnchorLayoutUpdated;
        }

        RefreshPlacement();
    }

    /// <summary>Re-evaluates the anchor after a template, DPI, or window-layout change.</summary>
    internal void RefreshPlacement()
    {
        bool wasActive = IsInsetPlacementActive;
        double inset = ResolveTopInset();
        bool changed = Math.Abs(_topInset - inset) > 0.01d;
        _topInset = inset;

        if (changed)
        {
            InvalidateMeasure();
            InvalidateArrange();
        }

        if (wasActive != IsInsetPlacementActive)
        {
            PlacementChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnTopEdgeAnchorLayoutUpdated(object? sender, EventArgs e) => RefreshPlacement();

    private double ResolveTopInset()
    {
        if (_topEdgeAnchor is null
            || _topEdgeAnchor.ActualHeight <= 0d
            || AdornedElement.RenderSize.Height <= 0d)
        {
            return 0d;
        }

        try
        {
            Point bottom = _topEdgeAnchor
                .TransformToVisual(AdornedElement)
                .Transform(new Point(0d, _topEdgeAnchor.ActualHeight));
            if (double.IsNaN(bottom.Y) || double.IsInfinity(bottom.Y))
            {
                return 0d;
            }

            return Math.Clamp(bottom.Y, 0d, AdornedElement.RenderSize.Height);
        }
        catch (InvalidOperationException)
        {
            // A custom template can realize the anchor before it joins the adorned root.
            // LayoutUpdated retries; until then the safe compatibility placement is full-content.
            return 0d;
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

        if (_topEdgeAnchor is not null)
        {
            _topEdgeAnchor.LayoutUpdated -= OnTopEdgeAnchorLayoutUpdated;
            _topEdgeAnchor = null;
        }

        PlacementChanged = null;

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
        double topInset = Math.Clamp(_topInset, 0d, size.Height);
        _child?.Measure(new Size(size.Width, Math.Max(0d, size.Height - topInset)));
        _classicOrbProxy?.Measure(_classicOrbProxySize);
        return size;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        Size adornedSize = AdornedElement.RenderSize;
        double topInset = Math.Clamp(_topInset, 0d, adornedSize.Height);
        _child?.Arrange(
            new Rect(
                new Point(0d, topInset),
                new Size(adornedSize.Width, Math.Max(0d, adornedSize.Height - topInset))));
        _classicOrbProxy?.Arrange(new Rect(_classicOrbProxyOrigin, _classicOrbProxySize));
        return adornedSize;
    }

    /// <inheritdoc />
    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        // The exposed title/tab band must remain interactive. Returning null here lets hit
        // testing continue into the adorned ribbon branch instead of stopping on the adorner.
        if (IsInsetPlacementActive && hitTestParameters.HitPoint.Y < _topInset)
        {
            return null;
        }

        return base.HitTestCore(hitTestParameters);
    }
}
