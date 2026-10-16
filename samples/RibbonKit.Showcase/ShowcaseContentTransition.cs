using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RibbonKit.Animation;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

// Application-owned example. It uses only public RibbonKit lifecycle/progress,
// captures its own content and never takes responsibility for ribbon completion.
internal sealed class ShowcaseContentTransition : IDisposable
{
    private readonly Ribbon _ribbon;
    private readonly FrameworkElement _surface;
    private readonly Dictionary<long, ContentDrawing> _presentations = new();
    private ContentDrawing? _current;

    internal ShowcaseContentTransition(Ribbon ribbon, FrameworkElement surface)
    {
        _ribbon = ribbon; _surface = surface;
        ribbon.LayoutTransitionPreparing += Prepare;
        ribbon.LayoutTransitionStarting += Start;
        ribbon.LayoutTransitionEnded += End;
        surface.Unloaded += OnUnloaded;
    }

    private void Prepare(object? sender, RibbonLayoutTransitionEventArgs e)
    {
        if (!e.BoundaryMotion.IsAnimated || !_surface.IsVisible || _surface.ActualHeight <= 0
            || AdornerLayer.GetAdornerLayer(e.CoordinateRoot) is not { } layer) return;
        ContainerVisual previous = _current?.Capture() ?? Capture(_surface);
        Rect before = _current?.PresentedBounds ?? Bounds(e.CoordinateRoot);
        var drawing = new ContentDrawing(e.CoordinateRoot, layer, previous, before, _surface);
        _presentations.Add(e.TransitionId, drawing);
        _current = drawing;
    }

    private void Start(object? sender, RibbonLayoutTransitionEventArgs e)
    {
        if (!_presentations.TryGetValue(e.TransitionId, out var drawing)) return;
        if (!e.BoundaryMotion.IsAnimated || e.FinalGeometry is null
            || e.PreviousGeometry.OccupiedBounds == e.FinalGeometry.OccupiedBounds)
        { Remove(e.TransitionId); return; }
        drawing.ShowSurface();
        ContainerVisual final = Capture(_surface);
        Rect bounds = Bounds(e.CoordinateRoot);
        drawing.HideSurface();
        drawing.Start(final, bounds, e.BoundaryMotion);
    }

    private Rect Bounds(FrameworkElement root) => _surface.TransformToVisual(root).TransformBounds(new Rect(_surface.RenderSize));
    private void End(object? sender, RibbonLayoutTransitionEventArgs e) => Remove(e.TransitionId);
    private void Remove(long id)
    {
        if (!_presentations.Remove(id, out var drawing)) return;
        if (ReferenceEquals(_current, drawing)) _current = null;
        drawing.Dispose();
    }
    private void OnUnloaded(object sender, RoutedEventArgs e)
    { foreach (long id in _presentations.Keys.ToArray()) Remove(id); }
    public void Dispose()
    {
        _ribbon.LayoutTransitionPreparing -= Prepare; _ribbon.LayoutTransitionStarting -= Start; _ribbon.LayoutTransitionEnded -= End;
        _surface.Unloaded -= OnUnloaded;
        OnUnloaded(this, new RoutedEventArgs());
    }

    private static ContainerVisual Capture(Visual source)
    {
        var copy = new DrawingVisual
        {
            Opacity = VisualTreeHelper.GetOpacity(source), Clip = VisualTreeHelper.GetClip(source)?.CloneCurrentValue(),
            Effect = (source as UIElement)?.Effect?.CloneCurrentValue() ?? (source as ContainerVisual)?.Effect?.CloneCurrentValue(),
            OpacityMask = (source as UIElement)?.OpacityMask?.CloneCurrentValue() ?? (source as ContainerVisual)?.OpacityMask?.CloneCurrentValue(),
        };
        using (var dc = copy.RenderOpen())
            if (VisualTreeHelper.GetDrawing(source) is { } drawing) dc.DrawDrawing(drawing.CloneCurrentValue());
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(source); i++)
        {
            if (VisualTreeHelper.GetChild(source, i) is not Visual child || child is UIElement { Visibility: not Visibility.Visible }) continue;
            var cloned = Capture(child);
            var transform = child.TransformToAncestor(source);
            Point o = transform.Transform(new Point()), x = transform.Transform(new Point(1, 0)), y = transform.Transform(new Point(0, 1));
            cloned.Transform = new MatrixTransform(new Matrix(x.X - o.X, x.Y - o.Y, y.X - o.X, y.Y - o.Y, o.X, o.Y));
            copy.Children.Add(cloned);
        }
        return copy;
    }

    private sealed class ContentDrawing : Adorner, IDisposable
    {
        private static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(double), typeof(ContentDrawing), new PropertyMetadata(0d, (d, _) => ((ContentDrawing)d).Update()));
        private readonly FrameworkElement _surface;
        private readonly AdornerLayer _layer;
        private readonly VisualCollection _children;
        private readonly ContainerVisual _viewport = new(), _old = new(), _final = new();
        private readonly Rect _before;
        private Rect _after;
        private AnimationClock? _hide;
        private bool _disposed;
        internal Rect PresentedBounds { get; private set; }

        internal ContentDrawing(FrameworkElement root, AdornerLayer layer, ContainerVisual previous, Rect before, FrameworkElement surface) : base(root)
        {
            _surface = surface; _layer = layer; _before = _after = PresentedBounds = before;
            IsHitTestVisible = false; Focusable = false;
            _children = new VisualCollection(this) { _viewport };
            _viewport.Children.Add(_old); _viewport.Children.Add(_final); _old.Children.Add(previous);
            HideSurface(); Update(); layer.Add(this);
        }
        internal void HideSurface()
        {
            _hide = new DoubleAnimation(0, 0, TimeSpan.FromDays(1)) { FillBehavior = FillBehavior.Stop }.CreateClock();
            _surface.ApplyAnimationClock(OpacityProperty, _hide, HandoffBehavior.Compose);
            _hide.Controller!.Begin(); _hide.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
        }
        internal void ShowSurface()
        {
            _hide?.Controller?.SeekAlignedToLastTick(TimeSpan.FromDays(1), TimeSeekOrigin.BeginTime);
            _hide?.Controller?.Remove(); _hide = null;
        }
        internal void Start(ContainerVisual final, Rect after, RibbonLayoutTransitionMotion motion)
        {
            _after = after; _final.Children.Add(final);
            SetBinding(ProgressProperty, new Binding(nameof(RibbonLayoutTransitionMotion.Progress)) { Source = motion });
            Update();
        }
        private void Update()
        {
            double p = Math.Clamp((double)GetValue(ProgressProperty), 0, 1);
            PresentedBounds = new Rect(_before.X + (_after.X - _before.X) * p, _before.Y + (_after.Y - _before.Y) * p,
                _before.Width + (_after.Width - _before.Width) * p, _before.Height + (_after.Height - _before.Height) * p);
            _viewport.Transform = new TranslateTransform(PresentedBounds.X, PresentedBounds.Y);
            _viewport.Clip = new RectangleGeometry(new Rect(PresentedBounds.Size));
            _old.Opacity = 1 - p; _final.Opacity = p;
        }
        // The clone remains in content-local coordinates, so the next hold can
        // start from the actual composed presentation on a rapid reversal.
        internal ContainerVisual Capture()
        {
            var result = new ContainerVisual();
            ContainerVisual old = ShowcaseContentTransition.Capture(_old), final = ShowcaseContentTransition.Capture(_final);
            result.Children.Add(old); result.Children.Add(final);
            result.Clip = new RectangleGeometry(new Rect(PresentedBounds.Size));
            return result;
        }
        protected override int VisualChildrenCount => _children?.Count ?? 0;
        protected override Visual GetVisualChild(int index) => _children[index];
        protected override Size MeasureOverride(Size constraint) => ((FrameworkElement)AdornedElement).RenderSize;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; BindingOperations.ClearBinding(this, ProgressProperty); ShowSurface(); _layer.Remove(this);
        }
    }
}
