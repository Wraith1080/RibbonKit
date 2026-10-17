using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Data;
using RibbonKit.Controls;

namespace RibbonKit.Animation;

// A short-lived vector presentation of the committed layout. Only the card's
// drawing stretches; command drawings retain their natural text/icon geometry.
// The real controls remain in their final tree and own focus, commands and UIA.
internal sealed class RibbonLayoutTransition : Adorner, IDisposable
{
    private static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        "Progress", typeof(double), typeof(RibbonLayoutTransition),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    private readonly FrameworkElement _surface;
    private readonly AdornerLayer _layer;
    private readonly RibbonLayoutSnapshot _previous;
    private readonly RibbonLayoutSnapshot _current;
    private readonly AnimationClock _hide;
    private readonly Action _completed;
    private readonly double _travel;
    private readonly FlowDirection _direction;
    private readonly DpiScale _dpi;
    private readonly Matrix _transform;
    private bool _disposed;
    private readonly Point _origin;
    private readonly FrameworkElement _coordinateRoot;
    private readonly VisualCollection _visuals;
    private readonly ContainerVisual _chrome = new();
    private readonly DrawingVisual _foreground = new();
    internal Point PresentedOrigin
    {
        get
        {
            if (_surface.FindCommonVisualAncestor(_coordinateRoot) is null) return _origin;
            Point final = _surface.TranslatePoint(new Point(), _coordinateRoot);
            double p = Math.Clamp((double)GetValue(ProgressProperty), 0, 1);
            return new Point(_origin.X + (final.X - _origin.X) * p, _origin.Y + (final.Y - _origin.Y) * p);
        }
    }
    internal bool MatchesLayout => _surface.IsVisible && _surface.RenderSize == _current.Size
        && _surface.FlowDirection == _direction && VisualTreeHelper.GetDpi(_surface).Equals(_dpi)
        && _surface.RenderTransform.Value == _transform;

    private RibbonLayoutTransition(FrameworkElement surface, AdornerLayer layer,
        RibbonLayoutSnapshot previous, RibbonLayoutSnapshot current,
        RibbonAnimationAction action, Action completed, RibbonLayoutTransitionMotion? shared, Point? origin, FrameworkElement? coordinateRoot) : base(surface)
    {
        _surface = surface;
        _layer = layer;
        _previous = previous;
        _current = current;
        _completed = completed;
        _coordinateRoot = coordinateRoot ?? layer;
        _origin = origin ?? surface.TranslatePoint(new Point(), _coordinateRoot);
        _travel = RibbonAnimation.GetSlideOffset(action);
        _direction = surface.FlowDirection;
        _dpi = VisualTreeHelper.GetDpi(surface);
        _transform = surface.RenderTransform.Value;
        IsHitTestVisible = false;
        Focusable = false;
        Effect = surface.Effect?.CloneCurrentValue();
        _visuals = new VisualCollection(this) { _chrome, _foreground };
        if (current.CloneChrome() is { } chrome) _chrome.Children.Add(chrome);
        // The adorner can sit above an ancestor with consumer-owned opacity.
        Opacity = RibbonLayoutDrawing.OpacityBelowLayer(surface, layer);
        _hide = RibbonLayoutDrawing.HideImmediately(surface);
        layer.Add(this);
        var motion = new DoubleAnimation(0d, 1d, RibbonAnimation.GetDuration(action))
        { EasingFunction = RibbonAnimation.GetEase(action) };
        motion.Completed += (_, _) => { if (!_disposed) _completed(); };
        if (shared is null) BeginAnimation(ProgressProperty, motion);
        else SetBinding(ProgressProperty, new Binding(nameof(RibbonLayoutTransitionMotion.Progress)) { Source = shared });
    }

    internal static RibbonLayoutTransition? Start(FrameworkElement surface,
        RibbonLayoutSnapshot previous, RibbonAnimationAction action, Action completed, RibbonLayoutTransitionMotion? shared = null, Point? origin = null, FrameworkElement? coordinateRoot = null)
    {
        if (AdornerLayer.GetAdornerLayer(surface) is not { } layer
            || surface.ActualWidth <= 0 || surface.ActualHeight <= 0) return null;
        var current = RibbonLayoutSnapshot.Capture(surface);
        if (current.Frame is null && current.Content.Children.Count == 0) return null;
        return new RibbonLayoutTransition(surface, layer, previous, current, action, completed, shared, origin, coordinateRoot);
    }

    protected override Size MeasureOverride(Size constraint) => new(
        Math.Max(_previous.Size.Width, _current.Size.Width),
        Math.Max(_previous.Size.Height, _current.Size.Height));

    protected override int VisualChildrenCount => _visuals?.Count ?? 0;
    protected override Visual GetVisualChild(int index) => _visuals[index];

    protected override void OnRender(DrawingContext drawing)
    {
        double progress = (double)GetValue(ProgressProperty);
        double height = _previous.Size.Height + (_current.Size.Height - _previous.Size.Height) * progress;
        if (_surface.FindCommonVisualAncestor(_layer) is not null)
        {
            Point final = _surface.TranslatePoint(new Point(), _layer);
            Point presented = _coordinateRoot.TranslatePoint(PresentedOrigin, _layer);
            _chrome.Transform = new TransformGroup { Children =
            {
                new ScaleTransform(1, Math.Max(0, height) / _current.Size.Height),
                new TranslateTransform(presented.X - final.X, presented.Y - final.Y),
            } };
        }
        using var content = _foreground.RenderOpen();
        DrawPresentation(content);
    }

    private void DrawPresentation(DrawingContext drawing, bool includeOrigin = true)
    {
        if (_surface.FindCommonVisualAncestor(_layer) is null) return;
        double progress = (double)GetValue(ProgressProperty);
        Point final = _surface.TranslatePoint(new Point(), _layer), presented = _coordinateRoot.TranslatePoint(PresentedOrigin, _layer);
        drawing.PushTransform(new TranslateTransform(includeOrigin ? presented.X - final.X : 0,
            includeOrigin ? presented.Y - final.Y : 0));
        double height = _previous.Size.Height + (_current.Size.Height - _previous.Size.Height) * progress;
        // The frame alone follows the moving bottom edge. No layout property and
        // no glyph/image scale participates in the accordion presentation.
        if (_current.Frame is { } frame)
        {
            drawing.PushTransform(new ScaleTransform(1, height / _current.Size.Height));
            drawing.DrawDrawing(frame);
            drawing.Pop();
        }
        drawing.PushClip(new RectangleGeometry(new Rect(0, 0, _current.Size.Width, Math.Max(0, height))));
        drawing.PushOpacity(Math.Max(0, 1 - progress * 3));
        drawing.PushTransform(new TranslateTransform(0, -_travel * progress));
        drawing.DrawDrawing(_previous.FullContent);
        drawing.Pop();
        drawing.Pop();
        drawing.PushOpacity(Math.Min(1, 0.35 + 1.65 * progress));
        drawing.DrawDrawing(_current.Content);
        foreach (var item in _current.Items)
        {
            Point origin = _previous.Positions.GetValueOrDefault(item.Key, item.Position);
            double offsetY = Math.Clamp(origin.Y - item.Position.Y, -_travel * 4 / 3, _travel * 4 / 3);
            if (!_previous.Positions.ContainsKey(item.Key))
                offsetY = _current.Size.Height >= _previous.Size.Height ? -_travel : _travel;
            drawing.PushClip(item.Clip);
            drawing.PushTransform(new TranslateTransform(
                (origin.X - item.Position.X) * (1 - progress), offsetY * (1 - progress)));
            drawing.PushTransform(new MatrixTransform(item.Transform));
            drawing.DrawDrawing(item.Drawing);
            drawing.Pop();
            drawing.Pop();
            drawing.Pop();
        }
        drawing.Pop();
        drawing.Pop();
        drawing.Pop();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        BeginAnimation(ProgressProperty, null);
        BindingOperations.ClearBinding(this, ProgressProperty);
        RibbonLayoutDrawing.ReleaseHide(_hide);
        _layer.Remove(this);
    }

    internal RibbonLayoutSnapshot CapturePresentation()
    {
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen()) DrawPresentation(drawing, false);
        double p = Math.Clamp((double)GetValue(ProgressProperty), 0, 1);
        double height = _previous.Size.Height + (_current.Size.Height - _previous.Size.Height) * p;
        var chrome = RibbonVectorSnapshot.Clone(_chrome);
        chrome.Transform = new ScaleTransform(1, Math.Max(0, height) / _current.Size.Height);
        return RibbonLayoutSnapshot.FromDrawing(new Size(_current.Size.Width, height), visual.Drawing, chrome);
    }
}

// Freeze the previous presentation synchronously, before the inherited layout
// callbacks or a render pass can expose the committed destination. Background
// work may still realize simplified copies; this cover stays in place until then.
internal sealed class RibbonLayoutHold : Adorner, IDisposable
{
    internal RibbonLayoutSnapshot Snapshot => _snapshot;
    internal double PresentationOpacity => _bodyOpacity;
    internal Point Origin { get; }
    private readonly FrameworkElement _coordinateRoot;
    private readonly AdornerLayer _layer;
    private readonly RibbonLayoutSnapshot _snapshot;
    private readonly FrameworkElement? _footer;
    private readonly RibbonLayoutSnapshot? _footerSnapshot;
    private readonly Point _footerPosition;
    private readonly double _bodyOpacity;
    private readonly double _footerOpacity;
    private readonly AnimationClock _hide;
    private readonly AnimationClock? _hideFooter;
    private readonly VisualCollection _visuals;
    private bool _disposed;

    private RibbonLayoutHold(FrameworkElement surface, AdornerLayer layer,
        RibbonLayoutSnapshot snapshot, FrameworkElement? footer, Point? origin, FrameworkElement? coordinateRoot, double? opacity) : base(surface)
    {
        _layer = layer;
        _snapshot = snapshot;
        _coordinateRoot = coordinateRoot ?? layer;
        Origin = origin ?? surface.TranslatePoint(new Point(), _coordinateRoot);
        _footer = footer;
        IsHitTestVisible = false;
        Focusable = false;
        Effect = surface.Effect?.CloneCurrentValue();
        _bodyOpacity = opacity ?? RibbonLayoutDrawing.OpacityBelowLayer(surface, layer);
        if (footer is { IsVisible: true, ActualHeight: > 0 })
        {
            _footerSnapshot = RibbonLayoutSnapshot.Capture(footer);
            _footerPosition = footer.TranslatePoint(new Point(), surface);
            _footerOpacity = RibbonLayoutDrawing.OpacityBelowLayer(footer, layer);
            _hideFooter = RibbonLayoutDrawing.HideImmediately(footer);
        }
        _hide = RibbonLayoutDrawing.HideImmediately(surface);
        _visuals = new VisualCollection(this);
        AddPresentation(snapshot, _bodyOpacity, new Point());
        if (_footerSnapshot is { } footerSnapshot)
            AddPresentation(footerSnapshot, _footerOpacity, _footerPosition);
        layer.Add(this);
    }

    internal static RibbonLayoutHold? Start(FrameworkElement surface,
        RibbonLayoutSnapshot previous, FrameworkElement? footer, Point? origin = null, FrameworkElement? coordinateRoot = null, double? opacity = null) =>
        AdornerLayer.GetAdornerLayer(surface) is { } layer
            ? new RibbonLayoutHold(surface, layer, previous, footer, origin, coordinateRoot, opacity) : null;

    public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
    {
        if (AdornedElement.FindCommonVisualAncestor(_layer) is null) return base.GetDesiredTransform(transform);
        Point current = ((FrameworkElement)AdornedElement).TranslatePoint(new Point(), _layer);
        Point previous = _coordinateRoot.TranslatePoint(Origin, _layer);
        return new GeneralTransformGroup { Children = { base.GetDesiredTransform(transform), new TranslateTransform(previous.X - current.X, previous.Y - current.Y) } };
    }

    protected override Size MeasureOverride(Size constraint) => new(_snapshot.Size.Width,
        Math.Max(_snapshot.Size.Height, _footerPosition.Y + (_footerSnapshot?.Size.Height ?? 0)));

    protected override int VisualChildrenCount => _visuals?.Count ?? 0;
    protected override Visual GetVisualChild(int index) => _visuals[index];

    private void AddPresentation(RibbonLayoutSnapshot snapshot, double opacity, Point position)
    {
        var layer = new ContainerVisual { Opacity = opacity,
            Transform = new TranslateTransform(position.X, position.Y) };
        if (snapshot.CloneChrome() is { } chrome) layer.Children.Add(chrome);
        var foreground = new DrawingVisual();
        using (var drawing = foreground.RenderOpen())
        {
            if (snapshot.Frame is { } frame) drawing.DrawDrawing(frame);
            drawing.DrawDrawing(snapshot.FullContent);
        }
        layer.Children.Add(foreground);
        _visuals.Add(layer);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        RibbonLayoutDrawing.ReleaseHide(_hide);
        if (_hideFooter is { } footerClock)
        {
            RibbonLayoutDrawing.ReleaseHide(footerClock);
        }
        _layer.Remove(this);
    }
}

internal static class RibbonLayoutDrawing
{
    internal static AnimationClock HideImmediately(FrameworkElement surface)
    {
        var clock = new DoubleAnimation(0, 0, new Duration(TimeSpan.FromDays(1))) { FillBehavior = FillBehavior.Stop }.CreateClock();
        surface.ApplyAnimationClock(UIElement.OpacityProperty, clock, HandoffBehavior.Compose);
        // Applying a clock alone can leave the base opacity visible until the
        // next timing tick. Seek synchronously without overwriting the binding.
        clock.Controller!.Begin();
        clock.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
        return clock;
    }

    internal static void ReleaseHide(AnimationClock clock)
    {
        // Stop this composed contribution synchronously before removing it. Do
        // not clear another animator's clocks or the consumer's base expression.
        clock.Controller?.SeekAlignedToLastTick(TimeSpan.FromDays(1), TimeSeekOrigin.BeginTime);
        clock.Controller?.Remove();
    }

    internal static double OpacityBelowLayer(Visual surface, Visual layer)
    {
        double opacity = 1;
        DependencyObject? common = surface.FindCommonVisualAncestor(layer);
        for (DependencyObject? node = surface; node is Visual visual && node != common;
             node = VisualTreeHelper.GetParent(visual)) opacity *= VisualTreeHelper.GetOpacity(visual);
        return opacity;
    }
}

internal sealed class RibbonLayoutSnapshot
{
    internal static RibbonLayoutSnapshot FromDrawing(Size size, DrawingGroup drawing, ContainerVisual? chrome = null) => new()
    { Size = size, FullContent = drawing.CloneCurrentValue(), Chrome = chrome };
    private ContainerVisual? Chrome { get; init; }
    internal ContainerVisual? CloneChrome()
    {
        if (Chrome is null) return null;
        var clone = RibbonVectorSnapshot.Clone(Chrome);
        clone.Transform = Chrome.Transform?.CloneCurrentValue();
        return clone;
    }
    internal Size Size { get; private init; }
    internal DrawingGroup? Frame { get; private init; }
    internal DrawingGroup FullContent { get; private init; } = new();
    internal DrawingGroup Content { get; private init; } = new();
    internal Dictionary<FrameworkElement, Point> Positions { get; } = new();
    internal List<(FrameworkElement Key, Point Position, Matrix Transform, Geometry Clip, DrawingGroup Drawing)> Items { get; } = new();

    internal static RibbonLayoutSnapshot Capture(FrameworkElement surface)
    {
        var host = Descendants(surface).OfType<RibbonGroupsHost>().FirstOrDefault();
        var items = host is null ? Array.Empty<FrameworkElement>()
            : Ribbon.GetLayoutMode(host) == RibbonLayoutMode.Simplified
                ? host.SimplifiedToolBar?.Items.OfType<FrameworkElement>().ToArray() ?? Array.Empty<FrameworkElement>()
                : host.Items.OfType<FrameworkElement>().ToArray();
        items = items.Where(item => item.IsVisible && item.ActualWidth > 0 && item.ActualHeight > 0).ToArray();
        // Shared templates isolate effect-bearing decoration from foreground.
        // Keep that visual tree intact in playback and reversal holds as well.
        FrameworkElement? chrome = surface is Panel panel ? panel.Children.OfType<FrameworkElement>()
            .FirstOrDefault(child => child.Name is "ContentHost" or "QatBelowHost") : null;
        var allContent = new HashSet<Visual>();
        var excluded = items.Cast<Visual>().ToHashSet();
        if (chrome is not null) { allContent.Add(chrome); excluded.Add(chrome); }
        var snapshot = new RibbonLayoutSnapshot
        {
            Size = surface.RenderSize,
            Frame = VisualTreeHelper.GetDrawing(surface)?.CloneCurrentValue(),
            Chrome = RibbonVectorSnapshot.Capture(chrome, surface, includeAncestors: false)?.Visual,
            FullContent = Children(surface, allContent),
            Content = Children(surface, excluded),
        };
        if (host is not null)
        {
            // Hidden source groups are the compressed columns seen when simplified
            // mode releases its full-group openers. Preserve those starting slots.
            foreach (var group in host.Items.OfType<RibbonGroup>())
            {
                snapshot.Positions[group] = group.TranslatePoint(new Point(), surface);
                foreach (var command in RibbonGroupsHost.Commands(group))
                    snapshot.Positions[command] = command.TranslatePoint(new Point(), surface);
            }
        }
        foreach (var item in items)
        {
            FrameworkElement key = Ribbon.GetQuickAccessSource(item) ?? item;
            Point position = item.TranslatePoint(new Point(), surface);
            snapshot.Positions[key] = position;
            // Keep the source viewport's clipping even while a column travels.
            Rect clip = new(new Point(), surface.RenderSize);
            for (DependencyObject? node = VisualTreeHelper.GetParent(item); node is FrameworkElement parent && node != surface;
                 node = VisualTreeHelper.GetParent(parent))
                if (parent.ClipToBounds)
                    clip.Intersect(parent.TransformToAncestor(surface).TransformBounds(new Rect(parent.RenderSize)));
            snapshot.Items.Add((key, position, MatrixTo(item, surface), new RectangleGeometry(clip), Tree(item, new HashSet<Visual>())));
        }
        return snapshot;
    }

    private static DrawingGroup Children(Visual visual, HashSet<Visual> excluded)
    {
        var result = new DrawingGroup();
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
        {
            if (VisualTreeHelper.GetChild(visual, i) is not Visual child || excluded.Contains(child)
                || child is UIElement { Visibility: not Visibility.Visible }) continue;
            var drawing = Tree(child, excluded);
            drawing.Transform = new MatrixTransform(MatrixTo(child, visual));
            result.Children.Add(drawing);
        }
        return result;
    }

    private static DrawingGroup Tree(Visual visual, HashSet<Visual> excluded)
    {
        var result = Children(visual, excluded);
        if (VisualTreeHelper.GetDrawing(visual) is { } own)
            result.Children.Insert(0, own.CloneCurrentValue());
        result.Opacity = VisualTreeHelper.GetOpacity(visual);
        result.ClipGeometry = VisualTreeHelper.GetClip(visual)?.CloneCurrentValue();
        return result;
    }

    private static Matrix MatrixTo(Visual visual, Visual ancestor)
    {
        var transform = visual.TransformToAncestor(ancestor);
        Point origin = transform.Transform(new Point());
        Point x = transform.Transform(new Point(1, 0));
        Point y = transform.Transform(new Point(0, 1));
        return new Matrix(x.X - origin.X, x.Y - origin.Y, y.X - origin.X, y.Y - origin.Y, origin.X, origin.Y);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
