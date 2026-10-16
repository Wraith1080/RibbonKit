using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RibbonKit.Animation;

// Detached vector visuals preserve child Effect -> ancestor Clip ordering. No live
// control, binding, focus target or popup is moved into the presentation layer.
internal sealed class RibbonVectorSnapshot
{
    internal ContainerVisual Visual { get; }
    internal Rect Bounds { get; }
    private RibbonVectorSnapshot(ContainerVisual visual, Rect bounds) { Visual = visual; Bounds = bounds; }

    internal static RibbonVectorSnapshot? Capture(Visual? source, Visual root, Visual? exclude = null, bool includeAncestors = true)
    {
        if (source is null || ContainsUnsupported(source, exclude)) return null;
        try
        {
            Rect bounds = source is FrameworkElement element ? new Rect(element.RenderSize)
                : VisualTreeHelper.GetDescendantBounds(source);
            if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) return null;
            GeneralTransform transform = source.TransformToVisual(root);
            var wrapper = new ContainerVisual { Transform = new MatrixTransform(Matrix(transform)) };
            if (includeAncestors && !ReferenceEquals(source, root))
            {
                DependencyObject? common = source.FindCommonVisualAncestor(root);
                for (DependencyObject? parent = VisualTreeHelper.GetParent(source); parent is Visual ancestor && parent != common;
                     parent = VisualTreeHelper.GetParent(parent)) wrapper.Opacity *= VisualTreeHelper.GetOpacity(ancestor);
            }
            wrapper.Children.Add(Clone(source, exclude));
            return new RibbonVectorSnapshot(wrapper, transform.TransformBounds(bounds));
        }
        catch (InvalidOperationException) { return null; }
    }

    internal static bool ContainsUnsupported(DependencyObject source, Visual? exclude = null)
    {
        if (ReferenceEquals(source, exclude)) return false;
        if (source is System.Windows.Interop.HwndHost or MediaElement or System.Windows.Media.Media3D.Visual3D) return true;
        // A VisualBrush still referencing live UI cannot be a frozen old frame.
        if (source is Visual visual && VisualTreeHelper.GetDrawing(visual) is { } drawing
            && !drawing.CloneCurrentValue().CanFreeze) return true;
        if (source is UIElement { OpacityMask: { } mask } && !mask.CloneCurrentValue().CanFreeze) return true;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(source); i++)
            if (ContainsUnsupported(VisualTreeHelper.GetChild(source, i), exclude)) return true;
        return false;
    }

    internal static ContainerVisual Clone(Visual source, Visual? exclude = null)
    {
        var result = new DrawingVisual
        {
            Opacity = VisualTreeHelper.GetOpacity(source),
            Clip = VisualTreeHelper.GetClip(source)?.CloneCurrentValue(),
            Effect = (source as UIElement)?.Effect?.CloneCurrentValue()
                ?? (source as ContainerVisual)?.Effect?.CloneCurrentValue(),
            OpacityMask = (source as UIElement)?.OpacityMask?.CloneCurrentValue()
                ?? (source as ContainerVisual)?.OpacityMask?.CloneCurrentValue(),
        };
        using (var drawing = result.RenderOpen())
        if (VisualTreeHelper.GetDrawing(source) is { } own) drawing.DrawDrawing(own.CloneCurrentValue());
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(source); i++)
        {
            if (VisualTreeHelper.GetChild(source, i) is not Visual child || ReferenceEquals(child, exclude)
                || child is UIElement { Visibility: not Visibility.Visible }) continue;
            ContainerVisual copy = Clone(child, exclude);
            copy.Transform = new MatrixTransform(Matrix(child.TransformToAncestor(source)));
            result.Children.Add(copy);
        }
        return result;
    }

    private static Matrix Matrix(GeneralTransform transform)
    {
        Point origin = transform.Transform(new Point());
        Point x = transform.Transform(new Point(1, 0));
        Point y = transform.Transform(new Point(0, 1));
        return new Matrix(x.X - origin.X, x.Y - origin.Y, y.X - origin.X, y.Y - origin.Y, origin.X, origin.Y);
    }
}

internal sealed class RibbonQuickAccessFrame
{
    internal bool IsSafe { get; init; } = true;
    internal RibbonVectorSnapshot? Header { get; init; }
    internal RibbonVectorSnapshot? Caption { get; init; }
    internal RibbonVectorSnapshot? Strip { get; init; }
    internal RibbonVectorSnapshot? Panel { get; init; }
    internal Rect Occupied { get; init; }
    internal bool CanTranslate { get; init; } = true;
}

internal sealed class RibbonQuickAccessTransition : Adorner, IDisposable
{
    private readonly AdornerLayer _layer;
    private readonly FrameworkElement _root;
    private readonly VisualCollection _visuals;
    private readonly ContainerVisual _headers = new(), _captions = new();
    private readonly ContainerVisual _oldHeader = new(), _newHeader = new(), _oldCaption = new(), _newCaption = new();
    private readonly ContainerVisual _panels = new();
    private readonly ContainerVisual _strips = new();
    private readonly ContainerVisual _oldPanel = new();
    private readonly ContainerVisual _newPanel = new();
    private readonly ContainerVisual _oldStrip = new();
    private readonly ContainerVisual _newStrip = new();
    private RibbonQuickAccessFrame _previous;
    private RibbonQuickAccessFrame? _current;
    private RibbonLayoutTransitionMotion? _boundary;
    private RibbonLayoutTransitionMotion? _motion;
    private bool _disposed;
    private readonly Size _rootSize;
    private readonly DpiScale _dpi;
    private readonly FlowDirection _direction;

    internal RibbonQuickAccessTransition(FrameworkElement root, AdornerLayer layer, RibbonQuickAccessFrame previous) : base(root)
    {
        _root = root; _layer = layer; _previous = previous;
        _rootSize = root.RenderSize; _dpi = VisualTreeHelper.GetDpi(root); _direction = root.FlowDirection;
        IsHitTestVisible = false; Focusable = false;
        Opacity = RibbonLayoutDrawing.OpacityBelowLayer(root, layer);
        _visuals = new VisualCollection(this) { _captions, _headers, _panels, _strips };
        _headers.Children.Add(_oldHeader); _headers.Children.Add(_newHeader);
        _captions.Children.Add(_oldCaption); _captions.Children.Add(_newCaption);
        if (previous.Header is { } header) _oldHeader.Children.Add(header.Visual);
        if (previous.Caption is { } caption) _oldCaption.Children.Add(caption.Visual);
        _panels.Children.Add(_oldPanel); _panels.Children.Add(_newPanel);
        _strips.Children.Add(_oldStrip); _strips.Children.Add(_newStrip);
        if (previous.Panel is { } panel) _oldPanel.Children.Add(panel.Visual);
        if (previous.Strip is { } strip) _oldStrip.Children.Add(strip.Visual);
        layer.Add(this);
    }

    internal bool MatchesRoot => _root.RenderSize == _rootSize && _root.FlowDirection == _direction
        && VisualTreeHelper.GetDpi(_root).Equals(_dpi);
    internal bool HasPresentation => _previous.Strip is not null || _previous.Panel is not null
        || _current?.Strip is not null || _current?.Panel is not null;

    internal void Start(RibbonQuickAccessFrame current, RibbonLayoutTransitionMotion boundary, RibbonLayoutTransitionMotion motion)
    {
        _current = current; _boundary = boundary; _motion = motion;
        if (current.Header is { } header) _newHeader.Children.Add(header.Visual);
        if (current.Caption is { } caption) _newCaption.Children.Add(caption.Visual);
        if (current.Panel is { } panel) _newPanel.Children.Add(panel.Visual);
        if (current.Strip is { } strip) _newStrip.Children.Add(strip.Visual);
        boundary.Changed += OnProgress;
        if (!ReferenceEquals(boundary, motion)) motion.Changed += OnProgress;
        OnProgress(this, EventArgs.Empty);
    }

    private void OnProgress(object? sender, EventArgs e)
    {
        if (_disposed || _current is not { } current || _boundary is null || _motion is null) return;
        double p = _motion.IsAnimated ? _motion.Progress : 1;
        double alpha = Math.Clamp(p, 0, 1);
        bool continuous = _previous.CanTranslate && current.CanTranslate
            && _previous.Strip is not null && current.Strip is not null;
        Vector delta = (_current.Strip?.Bounds.TopLeft ?? _previous.Strip?.Bounds.TopLeft ?? new Point())
            - (_previous.Strip?.Bounds.TopLeft ?? _current.Strip?.Bounds.TopLeft ?? new Point());
        double travel = RibbonAnimation.GetSlideOffset(RibbonAnimationAction.QuickAccessMove);
        _oldStrip.Transform = continuous ? new TranslateTransform(delta.X * p, delta.Y * p)
            : new TranslateTransform(0, -travel * alpha);
        _newStrip.Transform = continuous ? new TranslateTransform(-delta.X * (1 - p), -delta.Y * (1 - p))
            : new TranslateTransform(0, travel * (1 - alpha));
        _oldStrip.Opacity = 1 - alpha;
        _newStrip.Opacity = alpha;

        double b = _boundary.IsAnimated ? Math.Clamp(_boundary.Progress, 0, 1) : 1;
        PlaceChrome(_oldHeader, _newHeader, _previous.Header?.Bounds, current.Header?.Bounds, b, false);
        PlaceChrome(_oldCaption, _newCaption, _previous.Caption?.Bounds, current.Caption?.Bounds, b, true);
        Rect? old = _previous.Panel?.Bounds, final = current.Panel?.Bounds;
        double top = (old?.Top ?? _previous.Occupied.Bottom) * (1 - b)
            + (final?.Top ?? current.Occupied.Bottom) * b;
        double height = (old?.Height ?? 0) * (1 - b) + (final?.Height ?? 0) * b;
        PlacePanel(_oldPanel, old, top, height);
        PlacePanel(_newPanel, final, top, height);
        _oldPanel.Opacity = height <= 0.01 ? 0 : final is null ? 1 : 1 - b;
        _newPanel.Opacity = height <= 0.01 ? 0 : old is null ? 1 : b;
    }

    private static void PlaceChrome(ContainerVisual old, ContainerVisual final, Rect? before, Rect? after, double p, bool clip)
    {
        if (before is not { } start || after is not { } end) return;
        Vector delta = end.TopLeft - start.TopLeft;
        old.Transform = new TranslateTransform(delta.X * p, delta.Y * p);
        final.Transform = new TranslateTransform(-delta.X * (1 - p), -delta.Y * (1 - p));
        old.Opacity = 1 - p; final.Opacity = p;
        if (clip && VisualTreeHelper.GetParent(old) is ContainerVisual parent)
            parent.Clip = new RectangleGeometry(new Rect(start.X + delta.X * p, start.Y + delta.Y * p,
                start.Width + (end.Width - start.Width) * p, start.Height + (end.Height - start.Height) * p));
    }

    private static void PlacePanel(ContainerVisual visual, Rect? bounds, double top, double height)
    {
        if (bounds is not { Height: > 0 } rect) return;
        // Only panel chrome (including its clipping/shadow tree) scales. Command
        // visuals are separate children and always retain their native geometry.
        double scale = Math.Max(0.00001, height / rect.Height);
        visual.Transform = new MatrixTransform(new Matrix(1, 0, 0, scale, 0, top - rect.Top * scale));
        visual.Opacity = height <= 0.01 ? 0 : 1;
    }

    internal RibbonQuickAccessFrame CapturePresentation() => new()
    {
        Header = RibbonVectorSnapshot.Capture(_headers, _root, includeAncestors: false),
        Caption = RibbonVectorSnapshot.Capture(_captions, _root, includeAncestors: false),
        Strip = RibbonVectorSnapshot.Capture(_strips, _root, includeAncestors: false),
        Panel = RibbonVectorSnapshot.Capture(_panels, _root, includeAncestors: false),
        Occupied = _current is null || _boundary is null ? _previous.Occupied
            : Interpolate(_previous.Occupied, _current.Occupied, Math.Clamp(_boundary.Progress, 0, 1)),
        CanTranslate = false, // A composed interrupted drawing departs/arrives without stretching.
    };

    private static Rect Interpolate(Rect old, Rect final, double p) => new(
        old.X + (final.X - old.X) * p, old.Y + (final.Y - old.Y) * p,
        old.Width + (final.Width - old.Width) * p, old.Height + (final.Height - old.Height) * p);

    protected override int VisualChildrenCount => _visuals?.Count ?? 0;
    protected override Visual GetVisualChild(int index) => _visuals[index];
    protected override Size MeasureOverride(Size constraint) => _root.RenderSize;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_boundary is not null) _boundary.Changed -= OnProgress;
        if (_motion is not null && !ReferenceEquals(_motion, _boundary)) _motion.Changed -= OnProgress;
        _layer.Remove(this);
    }
}
