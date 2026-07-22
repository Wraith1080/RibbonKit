using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

/// <summary>
/// One themed "child window" inside an <see cref="MdiContainer"/>: a caption bar
/// (icon, title, minimize/maximize/close), a resizable border, and a content host.
/// Fully re-templatable; colors and metrics come from the <c>RibbonKit.*.MdiChild.*</c>
/// token family so the chrome tracks the active theme. Consumers rarely create one
/// directly — the container generates them per document.
/// </summary>
[TemplatePart(Name = DragThumbPartName, Type = typeof(Thumb))]
[TemplatePart(Name = MinimizeButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = MaximizeRestoreButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CloseButtonPartName, Type = typeof(ButtonBase))]
public class MdiChild : ContentControl
{
    private const string DragThumbPartName = "PART_DragThumb";
    private const string MinimizeButtonPartName = "PART_MinimizeButton";
    private const string MaximizeRestoreButtonPartName = "PART_MaximizeRestoreButton";
    private const string CloseButtonPartName = "PART_CloseButton";

    // Width a minimized child collapses to (its height is the caption bar's own height).
    private const double MinimizedWidth = 186;

    // How much of a dragged child must stay reachable inside the container.
    private const double DragKeepVisible = 48;

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="WindowState"/> dependency property.</summary>
    public static readonly DependencyProperty WindowStateProperty =
        DependencyProperty.Register(
            nameof(WindowState),
            typeof(WindowState),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(
                WindowState.Normal,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                | FrameworkPropertyMetadataOptions.AffectsMeasure
                | FrameworkPropertyMetadataOptions.AffectsParentMeasure
                | FrameworkPropertyMetadataOptions.AffectsParentArrange,
                OnWindowStateChanged));

    /// <summary>Identifies the <see cref="CanClose"/> dependency property.</summary>
    public static readonly DependencyProperty CanCloseProperty =
        DependencyProperty.Register(
            nameof(CanClose),
            typeof(bool),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsModified"/> dependency property.</summary>
    public static readonly DependencyProperty IsModifiedProperty =
        DependencyProperty.Register(
            nameof(IsModified),
            typeof(bool),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the <see cref="Left"/> dependency property.</summary>
    public static readonly DependencyProperty LeftProperty =
        DependencyProperty.Register(
            nameof(Left),
            typeof(double),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(
                double.NaN,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                | FrameworkPropertyMetadataOptions.AffectsParentArrange));

    /// <summary>Identifies the <see cref="Top"/> dependency property.</summary>
    public static readonly DependencyProperty TopProperty =
        DependencyProperty.Register(
            nameof(Top),
            typeof(double),
            typeof(MdiChild),
            new FrameworkPropertyMetadata(
                double.NaN,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                | FrameworkPropertyMetadataOptions.AffectsParentArrange));

    /// <summary>
    /// Raised (bubbling) when the user interacts with this child so the container can
    /// bring it to front and make it the active document.
    /// </summary>
    public static readonly RoutedEvent ActivationRequestedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(ActivationRequested),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MdiChild));

    /// <summary>
    /// Raised (bubbling) when the caption close button is pressed. The container
    /// handles it: raises its cancelable closing event, then removes the document.
    /// </summary>
    public static readonly RoutedEvent CloseRequestedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(CloseRequested),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MdiChild));

    private Thumb? _dragThumb;
    private ButtonBase? _minimizeButton;
    private ButtonBase? _maximizeRestoreButton;
    private ButtonBase? _closeButton;
    private readonly List<Thumb> _resizeThumbs = new();
    private Rect _restoreBounds = Rect.Empty;
    private bool _applyingState;

    static MdiChild()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MdiChild),
            new FrameworkPropertyMetadata(typeof(MdiChild)));
    }

    /// <summary>Occurs when the user interacts with this child (bubbles to the container).</summary>
    public event RoutedEventHandler ActivationRequested
    {
        add => AddHandler(ActivationRequestedEvent, value);
        remove => RemoveHandler(ActivationRequestedEvent, value);
    }

    /// <summary>Occurs when the caption close button is pressed (bubbles to the container).</summary>
    public event RoutedEventHandler CloseRequested
    {
        add => AddHandler(CloseRequestedEvent, value);
        remove => RemoveHandler(CloseRequestedEvent, value);
    }

    /// <summary>The caption title.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Optional small icon at the left of the caption.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Whether this is the container's active child (drives the highlighted caption).
    /// Maintained by the container.
    /// </summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Normal, Minimized (caption strip at the bottom), or Maximized (fills the client area).</summary>
    public WindowState WindowState
    {
        get => (WindowState)GetValue(WindowStateProperty);
        set => SetValue(WindowStateProperty, value);
    }

    /// <summary>Whether the caption shows a close button.</summary>
    public bool CanClose
    {
        get => (bool)GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    /// <summary>Shows the dirty marker next to the title.</summary>
    public bool IsModified
    {
        get => (bool)GetValue(IsModifiedProperty);
        set => SetValue(IsModifiedProperty, value);
    }

    /// <summary>Left edge in DIPs relative to the container (NaN until placed).</summary>
    public double Left
    {
        get => (double)GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    /// <summary>Top edge in DIPs relative to the container (NaN until placed).</summary>
    public double Top
    {
        get => (double)GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    /// <summary>
    /// The normal-state bounds saved when the child was minimized or maximized —
    /// what a restore returns to (and what layout persistence should serialize
    /// for a non-normal child). <see cref="Rect.Empty"/> while in the normal state.
    /// </summary>
    public Rect RestoreBounds => _restoreBounds;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        // Unhook the previous template's parts (re-templating at runtime).
        if (_dragThumb is not null)
        {
            _dragThumb.DragDelta -= OnDragThumbDelta;
            _dragThumb.MouseDoubleClick -= OnCaptionDoubleClick;
        }

        if (_minimizeButton is not null)
        {
            _minimizeButton.Click -= OnMinimizeClick;
        }

        if (_maximizeRestoreButton is not null)
        {
            _maximizeRestoreButton.Click -= OnMaximizeRestoreClick;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCloseClick;
        }

        foreach (Thumb thumb in _resizeThumbs)
        {
            thumb.DragDelta -= OnResizeThumbDelta;
        }

        _resizeThumbs.Clear();

        base.OnApplyTemplate();

        _dragThumb = GetTemplateChild(DragThumbPartName) as Thumb;
        _minimizeButton = GetTemplateChild(MinimizeButtonPartName) as ButtonBase;
        _maximizeRestoreButton = GetTemplateChild(MaximizeRestoreButtonPartName) as ButtonBase;
        _closeButton = GetTemplateChild(CloseButtonPartName) as ButtonBase;

        if (_dragThumb is not null)
        {
            _dragThumb.DragDelta += OnDragThumbDelta;
            _dragThumb.MouseDoubleClick += OnCaptionDoubleClick;
        }

        if (_minimizeButton is not null)
        {
            _minimizeButton.Click += OnMinimizeClick;
        }

        if (_maximizeRestoreButton is not null)
        {
            _maximizeRestoreButton.Click += OnMaximizeRestoreClick;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click += OnCloseClick;
        }

        // The eight resize thumbs. Each name encodes which edges it moves.
        HookResizeThumb("PART_ResizeLeft", left: true, top: false, right: false, bottom: false);
        HookResizeThumb("PART_ResizeRight", left: false, top: false, right: true, bottom: false);
        HookResizeThumb("PART_ResizeTop", left: false, top: true, right: false, bottom: false);
        HookResizeThumb("PART_ResizeBottom", left: false, top: false, right: false, bottom: true);
        HookResizeThumb("PART_ResizeTopLeft", left: true, top: true, right: false, bottom: false);
        HookResizeThumb("PART_ResizeTopRight", left: false, top: true, right: true, bottom: false);
        HookResizeThumb("PART_ResizeBottomLeft", left: true, top: false, right: false, bottom: true);
        HookResizeThumb("PART_ResizeBottomRight", left: false, top: false, right: true, bottom: true);
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);

        // Any press anywhere in the child asks the container to activate it. Preview so
        // it fires even when the press lands in interactive content. Not handled: the
        // press continues to whatever was clicked.
        RaiseEvent(new RoutedEventArgs(ActivationRequestedEvent, this));
    }

    private void HookResizeThumb(string partName, bool left, bool top, bool right, bool bottom)
    {
        if (GetTemplateChild(partName) is Thumb thumb)
        {
            thumb.Tag = new ResizeEdges(left, top, right, bottom);
            thumb.DragDelta += OnResizeThumbDelta;
            _resizeThumbs.Add(thumb);
        }
    }

    private void OnDragThumbDelta(object sender, DragDeltaEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            return;
        }

        double left = (double.IsNaN(Left) ? 0 : Left) + e.HorizontalChange;
        double top = (double.IsNaN(Top) ? 0 : Top) + e.VerticalChange;

        // Keep the caption reachable: at least DragKeepVisible of the child inside the
        // container on the sides, and never above the top edge.
        if (VisualTreeHelper.GetParent(this) is FrameworkElement panel && panel.ActualWidth > 0)
        {
            double width = double.IsNaN(ActualWidth) ? 0 : ActualWidth;
            left = Math.Max(left, DragKeepVisible - width);
            left = Math.Min(left, Math.Max(0, panel.ActualWidth - DragKeepVisible));
            top = Math.Min(top, Math.Max(0, panel.ActualHeight - DragKeepVisible));
        }

        top = Math.Max(0, top);

        SetCurrentValue(LeftProperty, left);
        SetCurrentValue(TopProperty, top);
    }

    private void OnCaptionDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            SetCurrentValue(
                WindowStateProperty,
                WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
            e.Handled = true;
        }
    }

    private void OnResizeThumbDelta(object sender, DragDeltaEventArgs e)
    {
        if (WindowState != WindowState.Normal || sender is not Thumb { Tag: ResizeEdges edges })
        {
            return;
        }

        double width = ActualWidth;
        double height = ActualHeight;
        double minWidth = Math.Max(MinWidth, 100);
        double minHeight = Math.Max(MinHeight, 40);

        if (edges.Right)
        {
            SetCurrentValue(WidthProperty, Math.Max(minWidth, width + e.HorizontalChange));
        }
        else if (edges.Left)
        {
            double newWidth = Math.Max(minWidth, width - e.HorizontalChange);
            SetCurrentValue(LeftProperty, (double.IsNaN(Left) ? 0 : Left) + (width - newWidth));
            SetCurrentValue(WidthProperty, newWidth);
        }

        if (edges.Bottom)
        {
            SetCurrentValue(HeightProperty, Math.Max(minHeight, height + e.VerticalChange));
        }
        else if (edges.Top)
        {
            double newHeight = Math.Max(minHeight, height - e.VerticalChange);
            double newTop = (double.IsNaN(Top) ? 0 : Top) + (height - newHeight);
            if (newTop >= 0)
            {
                SetCurrentValue(TopProperty, newTop);
                SetCurrentValue(HeightProperty, newHeight);
            }
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) =>
        // On an already-minimized child the same button restores (like the classic
        // minimized-strip caption), so a minimized child always has a way back.
        SetCurrentValue(
            WindowStateProperty,
            WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized);

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        SetCurrentValue(
            WindowStateProperty,
            WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(CloseRequestedEvent, this));

    private static void OnWindowStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MdiChild)d).ApplyWindowState((WindowState)e.OldValue, (WindowState)e.NewValue);

    /// <summary>
    /// Applies a state transition to the layout properties. Leaving Normal saves the
    /// current bounds; Maximized clears Width/Height so the panel's full-size arrange
    /// stretches the child; Minimized pins a fixed caption-strip width (the template
    /// collapses the content, so the height falls to the caption's); returning to
    /// Normal restores the saved bounds.
    /// </summary>
    private void ApplyWindowState(WindowState oldState, WindowState newState)
    {
        if (_applyingState || oldState == newState)
        {
            return;
        }

        // Where the child sits right now, before any bounds change — the starting frame
        // of the state-transition animation. TranslatePoint sees an in-flight render
        // transform too, so re-targeting mid-animation starts from the visual position.
        Rect? animateFrom = CaptureCurrentRect();

        _applyingState = true;
        try
        {
            if (oldState == WindowState.Normal)
            {
                _restoreBounds = new Rect(
                    double.IsNaN(Left) ? 0 : Left,
                    double.IsNaN(Top) ? 0 : Top,
                    double.IsNaN(Width) ? ActualWidth : Width,
                    double.IsNaN(Height) ? ActualHeight : Height);
            }

            switch (newState)
            {
                case WindowState.Maximized:
                    SetCurrentValue(WidthProperty, double.NaN);
                    SetCurrentValue(HeightProperty, double.NaN);
                    break;

                case WindowState.Minimized:
                    SetCurrentValue(WidthProperty, MinimizedWidth);
                    SetCurrentValue(HeightProperty, double.NaN);
                    break;

                case WindowState.Normal:
                    if (!_restoreBounds.IsEmpty)
                    {
                        SetCurrentValue(LeftProperty, _restoreBounds.X);
                        SetCurrentValue(TopProperty, _restoreBounds.Y);
                        SetCurrentValue(WidthProperty, _restoreBounds.Width);
                        SetCurrentValue(HeightProperty, _restoreBounds.Height);
                        _restoreBounds = Rect.Empty;
                    }

                    break;
            }
        }
        finally
        {
            _applyingState = false;
        }

        if (animateFrom is { } from)
        {
            AnimateStateTransition(from);
        }
    }

    /// <summary>
    /// The child's current rect relative to its panel, or <see langword="null"/> when it
    /// has no meaningful visual yet (not loaded, no parent, zero size).
    /// </summary>
    private Rect? CaptureCurrentRect()
    {
        // The VISUAL parent (the items panel) — the logical Parent of a generated
        // item container is not the panel and can be null.
        if (VisualTreeHelper.GetParent(this) is not UIElement panel
            || !IsLoaded || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return null;
        }

        Point origin = TranslatePoint(new Point(0, 0), panel);
        return new Rect(origin, new Size(ActualWidth, ActualHeight));
    }

    /// <summary>
    /// Plays the minimize/maximize/restore transition: after the panel snaps the child to
    /// its new rect, a scale+translate render transform makes it appear to travel from
    /// <paramref name="oldRect"/> into place. Transform-only (GPU-composited, no layout
    /// churn), honoring the ribbon's animation level and the OS reduce-motion setting via
    /// <see cref="RibbonAnimationAction.MdiWindowState"/> — disabled means it just snaps.
    /// </summary>
    private void AnimateStateTransition(Rect oldRect)
    {
        if (!RibbonAnimation.IsEnabled(RibbonAnimationAction.MdiWindowState))
        {
            return;
        }

        // The new bounds only exist after the layout pass triggered by the state change.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            // Clear any previous transform first so the new-rect math reads untransformed
            // coordinates.
            SetCurrentValue(RenderTransformProperty, null);

            if (CaptureCurrentRect() is not { Width: > 0, Height: > 0 } newRect)
            {
                return;
            }

            double scaleX = oldRect.Width / newRect.Width;
            double scaleY = oldRect.Height / newRect.Height;
            double offsetX = oldRect.X - newRect.X;
            double offsetY = oldRect.Y - newRect.Y;

            const double epsilon = 0.5;
            if (Math.Abs(offsetX) < epsilon && Math.Abs(offsetY) < epsilon
                && Math.Abs(oldRect.Width - newRect.Width) < epsilon
                && Math.Abs(oldRect.Height - newRect.Height) < epsilon)
            {
                return;
            }

            var scale = new ScaleTransform(scaleX, scaleY);
            var translate = new TranslateTransform(offsetX, offsetY);
            var group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(translate);

            RenderTransformOrigin = new Point(0, 0);
            SetCurrentValue(RenderTransformProperty, group);

            Duration duration = RibbonAnimation.GetDuration(RibbonAnimationAction.MdiWindowState);
            IEasingFunction ease = RibbonAnimation.GetEase(RibbonAnimationAction.MdiWindowState);

            var toIdentity = new DoubleAnimation(1d, duration) { EasingFunction = ease };
            var toZero = new DoubleAnimation(0d, duration) { EasingFunction = ease };

            // Release the transform once settled so the child renders (and hit-tests)
            // untransformed between transitions.
            toZero.Completed += (_, _) => SetCurrentValue(RenderTransformProperty, null);

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, toIdentity);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, toIdentity);
            translate.BeginAnimation(TranslateTransform.XProperty, toZero);
            translate.BeginAnimation(TranslateTransform.YProperty, toZero);
        });
    }

    private readonly record struct ResizeEdges(bool Left, bool Top, bool Right, bool Bottom);
}
