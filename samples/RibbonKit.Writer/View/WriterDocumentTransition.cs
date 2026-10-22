using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RibbonKit.Animation;
using RibbonKit.Controls;

namespace RibbonKit.Writer.View;

/// <summary>Writer-owned presentation of the document boundary; never controls a RibbonKit transition.</summary>
internal sealed class WriterDocumentTransition : IDisposable
{
    private readonly Ribbon _ribbon;
    private readonly Panel _surface;
    private readonly RichTextBox _editor;
    private Session? _active;
    private bool _composing;

    internal WriterDocumentTransition(Ribbon ribbon, Panel surface, RichTextBox editor)
    {
        _ribbon = ribbon; _surface = surface; _editor = editor;
        ribbon.LayoutTransitionPreparing += Preparing;
        ribbon.LayoutTransitionStarting += Starting;
        ribbon.LayoutTransitionEnded += Ended;
        surface.Unloaded += Unloaded;
        surface.LayoutUpdated += Validate;
        TextCompositionManager.AddPreviewTextInputStartHandler(editor, CompositionStarted);
        TextCompositionManager.AddPreviewTextInputUpdateHandler(editor, CompositionStarted);
        TextCompositionManager.AddPreviewTextInputHandler(editor, CompositionEnded);
        editor.LostKeyboardFocus += CompositionFocusLost;
    }

    internal bool IsPresenting => _active is not null;
    internal Point? PresentedOrigin => _active?.Scene.PresentedOrigin;

    private void Preparing(object? sender, RibbonLayoutTransitionEventArgs e)
    {
        Session? previous = _active;
        if (!e.BoundaryMotion.IsAnimated || _composing || !_surface.IsVisible
            || !_surface.LayoutTransform.Value.IsIdentity || HasScaleOrRotation(_surface.RenderTransform.Value)
            || _surface.FindCommonVisualAncestor(e.CoordinateRoot) is null
            || AdornerLayer.GetAdornerLayer(e.CoordinateRoot) is not { } layer)
        { Reset(); return; }

        // A replacement holds the current composed drawing before Ended(old).
        bool successor = previous is not null && ReferenceEquals(previous.Root, e.CoordinateRoot);
        var old = successor
            ? previous!.Scene.HoldPicture() : Capture(_surface, root: true);
        if (old is null) { Reset(); return; }
        Rect bounds = Bounds(e.CoordinateRoot);
        Point origin = successor ? previous!.Scene.PresentedOrigin : bounds.TopLeft;
        double opacity = previous?.Opacity ?? _surface.Opacity;
        if (!successor) old.Opacity = opacity;
        var scene = new DocumentScene(e.CoordinateRoot, layer, old, bounds, origin,
            e.PreviousGeometry.OccupiedBounds.Bottom, _surface.Background, successor);
        var hide = (AnimationClock)new DoubleAnimation(0d, 0d, Duration.Forever).CreateClock(true);
        _surface.ApplyAnimationClock(UIElement.OpacityProperty, hide, HandoffBehavior.Compose);
        _active = new Session(e.TransitionId, e.CoordinateRoot, scene, hide, opacity,
            VisualTreeHelper.GetDpi(_surface), _surface.FlowDirection);
        previous?.Dispose();
    }

    private void Starting(object? sender, RibbonLayoutTransitionEventArgs e)
    {
        if (_active is not { } session || session.Id != e.TransitionId) return;
        Rect bounds = Bounds(session.Root);
        if (!e.BoundaryMotion.IsAnimated || e.FinalGeometry is null
            || bounds.IsEmpty || Capture(_surface, root: true) is not { } picture)
        { Reset(); return; }
        // The source remains held. Its zero opacity is excluded from the capture root.
        session.Scene.Start(picture, bounds, e.FinalGeometry.OccupiedBounds.Bottom,
            e.BoundaryMotion, session.Opacity);
        session.FinalBounds = bounds;
        session.Started = true;
    }

    private void Ended(object? sender, RibbonLayoutTransitionEventArgs e)
    {
        if (_active?.Id == e.TransitionId) Reset();
    }

    private Rect Bounds(Visual root) => _surface.FindCommonVisualAncestor(root) is null
        ? Rect.Empty : _surface.TransformToVisual(root).TransformBounds(new Rect(_surface.RenderSize));

    private static bool HasScaleOrRotation(Matrix matrix) =>
        matrix.M11 != 1 || matrix.M22 != 1 || matrix.M12 != 0 || matrix.M21 != 0;

    private void Validate(object? sender, EventArgs e)
    {
        if (_active is not { } session) return;
        if (!_surface.IsVisible || _surface.FindCommonVisualAncestor(session.Root) is null
            || !VisualTreeHelper.GetDpi(_surface).Equals(session.Dpi) || _surface.FlowDirection != session.Direction
            || session.Started && Bounds(session.Root) != session.FinalBounds)
            Reset();
    }

    private void Unloaded(object sender, RoutedEventArgs e) => Reset();
    private void CompositionStarted(object sender, TextCompositionEventArgs e) { _composing = true; Reset(); }
    private void CompositionEnded(object sender, TextCompositionEventArgs e) => _composing = false;
    private void CompositionFocusLost(object sender, KeyboardFocusChangedEventArgs e) => _composing = false;

    internal void Reset()
    {
        var session = _active; _active = null;
        session?.Dispose();
    }

    public void Dispose()
    {
        Reset();
        _ribbon.LayoutTransitionPreparing -= Preparing;
        _ribbon.LayoutTransitionStarting -= Starting;
        _ribbon.LayoutTransitionEnded -= Ended;
        _surface.Unloaded -= Unloaded;
        _surface.LayoutUpdated -= Validate;
        TextCompositionManager.RemovePreviewTextInputStartHandler(_editor, CompositionStarted);
        TextCompositionManager.RemovePreviewTextInputUpdateHandler(_editor, CompositionStarted);
        TextCompositionManager.RemovePreviewTextInputHandler(_editor, CompositionEnded);
        _editor.LostKeyboardFocus -= CompositionFocusLost;
    }

    // Only the realized Writer subtree is captured. Native/media/live-brush content
    // and excessive work settle immediately; no screenshot or editor replacement.
    private static ContainerVisual? Capture(Visual visual, bool root = false)
    {
        int remaining = 4000;
        try { return Copy(visual, root, ref remaining); }
        catch (UnsafeDrawingException) { return null; }
    }

    private static ContainerVisual Copy(Visual visual, bool root, ref int remaining)
    {
        if (visual is UIElement { IsVisible: false }) return new ContainerVisual();
        if (--remaining < 0 || visual is HwndHost or MediaElement)
            throw new UnsafeDrawingException();
        var copy = new ContainerVisual
        {
            Opacity = root ? 1 : VisualTreeHelper.GetOpacity(visual),
            Clip = Frozen(VisualTreeHelper.GetClip(visual)),
            OpacityMask = Frozen(VisualTreeHelper.GetOpacityMask(visual)),
            Effect = Frozen(VisualTreeHelper.GetEffect(visual)),
        };
        if (!root)
        {
            copy.Transform = Frozen(VisualTreeHelper.GetTransform(visual));
            copy.Offset = VisualTreeHelper.GetOffset(visual);
        }
        if (VisualTreeHelper.GetDrawing(visual) is { } drawing)
        {
            var ink = new DrawingVisual();
            using (var context = ink.RenderOpen()) context.DrawDrawing(Frozen(drawing));
            copy.Children.Add(ink);
        }
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            if (VisualTreeHelper.GetChild(visual, i) is Visual child)
                copy.Children.Add(Copy(child, false, ref remaining));
            else throw new UnsafeDrawingException();
        return copy;
    }

    private static T? Frozen<T>(T? source) where T : Freezable
    {
        if (source is null) return null;
        var copy = (T)source.CloneCurrentValue();
        if (!copy.CanFreeze) throw new UnsafeDrawingException();
        copy.Freeze();
        return copy;
    }

    private sealed class UnsafeDrawingException : Exception;

    private sealed class Session(long id, FrameworkElement root, DocumentScene scene,
        AnimationClock hide, double opacity, DpiScale dpi, FlowDirection direction) : IDisposable
    {
        internal long Id { get; } = id;
        internal FrameworkElement Root { get; } = root;
        internal DocumentScene Scene { get; } = scene;
        internal double Opacity { get; } = opacity;
        internal DpiScale Dpi { get; } = dpi;
        internal FlowDirection Direction { get; } = direction;
        internal bool Started { get; set; }
        internal Rect FinalBounds { get; set; }
        public void Dispose() { Scene.Dispose(); hide.Controller?.Remove(); }
    }

    private sealed class DocumentScene : Adorner, IDisposable
    {
        private static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            "Progress", typeof(double), typeof(DocumentScene),
            new FrameworkPropertyMetadata(0d, static (d, _) => ((DocumentScene)d).ArrangePicture()));
        private readonly AdornerLayer _layer;
        private readonly VisualCollection _children;
        private readonly ContainerVisual _clip = new();
        private readonly DrawingVisual _workspace = new();
        private readonly ContainerVisual _position = new();
        private readonly Rect _before;
        private readonly Point _origin;
        private readonly double _oldBoundary;
        private readonly Brush? _background;
        private Rect _after;
        private double _newBoundary;
        private bool _started;
        internal Point PresentedOrigin { get; private set; }

        internal DocumentScene(FrameworkElement root, AdornerLayer layer, ContainerVisual picture,
            Rect before, Point origin, double boundary, Brush? background, bool absolute) : base(root)
        {
            _layer = layer; _before = _after = before; _origin = origin;
            _oldBoundary = _newBoundary = boundary; _background = background;
            IsHitTestVisible = false; Focusable = false;
            _children = new VisualCollection(this) { _clip };
            _clip.Children.Add(_workspace); _clip.Children.Add(_position);
            _position.Children.Add(picture);
            _position.Offset = absolute ? new Vector() : (Vector)before.TopLeft;
            PresentedOrigin = origin;
            ClipPicture(boundary, before.Bottom);
            layer.Add(this);
        }

        internal ContainerVisual? HoldPicture() => Capture(_clip);

        internal void Start(ContainerVisual picture, Rect after, double boundary,
            RibbonLayoutTransitionMotion motion, double opacity)
        {
            _after = after; _newBoundary = boundary; _started = true;
            _position.Children.Clear(); _position.Children.Add(picture);
            _position.Opacity = opacity;
            SetBinding(ProgressProperty, new Binding(nameof(RibbonLayoutTransitionMotion.Progress))
                { Source = motion, Mode = BindingMode.OneWay });
            ArrangePicture();
        }

        private void ArrangePicture()
        {
            if (!_started) return;
            double p = Math.Clamp((double)GetValue(ProgressProperty), 0, 1);
            PresentedOrigin = new Point(_origin.X + (_after.X - _origin.X) * p,
                _origin.Y + (_after.Y - _origin.Y) * p);
            _position.Offset = (Vector)PresentedOrigin;
            double boundary = _oldBoundary + (_newBoundary - _oldBoundary) * p;
            // Keep all Writer ink below QAT and above the stationary status bar.
            ClipPicture(boundary, _after.Bottom);
            using var drawing = _workspace.RenderOpen();
            drawing.DrawRectangle(_background, null, new Rect(Math.Min(_before.X, _after.X), boundary,
                Math.Max(_before.Width, _after.Width), Math.Max(0, _after.Bottom - boundary)));
        }

        private void ClipPicture(double top, double bottom) => _clip.Clip = new RectangleGeometry(
            new Rect(Math.Min(_before.X, _after.X), top, Math.Max(_before.Width, _after.Width), Math.Max(0, bottom - top)));

        protected override int VisualChildrenCount => _children?.Count ?? 0;
        protected override Visual GetVisualChild(int index) => _children[index];
        protected override Size MeasureOverride(Size constraint) => AdornedElement.RenderSize;
        public void Dispose() { BindingOperations.ClearBinding(this, ProgressProperty); _layer.Remove(this); }
    }
}
