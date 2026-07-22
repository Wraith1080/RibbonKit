using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RibbonKit.Animation;

namespace RibbonKit.Layout;

/// <summary>
/// A lightweight horizontal scroller for the ribbon's tab strip and groups row. It shows its single
/// child clipped to the available width and offset by <see cref="Offset"/>, and reports whether there
/// is anything to scroll to via <see cref="CanScrollLeft"/> / <see cref="CanScrollRight"/> so the
/// template can show/hide the chevron buttons. The buttons drive it through
/// <see cref="ScrollLeftCommand"/> / <see cref="ScrollRightCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a stock <see cref="ScrollViewer"/> (which measures its content at infinite width and would
/// therefore stop the groups row from ever collapsing), this host can constrain the child to the
/// viewport width via <see cref="ConstrainChildWidth"/>. For the groups row that lets the adaptive
/// <see cref="RibbonGroupsPanel"/> reduce groups to fit FIRST; only when even the fully-reduced row is
/// still wider than the viewport does the child overflow and scrolling engage — matching Office. The
/// tab strip leaves it off, so tabs keep their natural size and simply scroll when there are too many.
/// </para>
/// <para>
/// The chevron buttons overlay the child's edges (they don't take layout space), so showing/hiding
/// them never changes the viewport width and can't oscillate. Scrolling uses a
/// <see cref="TranslateTransform"/> (no re-layout).
/// </para>
/// </remarks>
public class RibbonScrollContentHost : Decorator
{
    /// <summary>Identifies the <see cref="Offset"/> dependency property.</summary>
    public static readonly DependencyProperty OffsetProperty =
        DependencyProperty.Register(
            nameof(Offset),
            typeof(double),
            typeof(RibbonScrollContentHost),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsArrange, null, CoerceOffset));

    /// <summary>Identifies the <see cref="ConstrainChildWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ConstrainChildWidthProperty =
        DependencyProperty.Register(
            nameof(ConstrainChildWidth),
            typeof(bool),
            typeof(RibbonScrollContentHost),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="LineSize"/> dependency property.</summary>
    public static readonly DependencyProperty LineSizeProperty =
        DependencyProperty.Register(
            nameof(LineSize),
            typeof(double),
            typeof(RibbonScrollContentHost),
            new PropertyMetadata(72d));

    private static readonly DependencyPropertyKey ExtentWidthKey =
        DependencyProperty.RegisterReadOnly(nameof(ExtentWidth), typeof(double), typeof(RibbonScrollContentHost), new PropertyMetadata(0d));

    /// <summary>Identifies the read-only <see cref="ExtentWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ExtentWidthProperty = ExtentWidthKey.DependencyProperty;

    private static readonly DependencyPropertyKey ViewportWidthKey =
        DependencyProperty.RegisterReadOnly(nameof(ViewportWidth), typeof(double), typeof(RibbonScrollContentHost), new PropertyMetadata(0d));

    /// <summary>Identifies the read-only <see cref="ViewportWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ViewportWidthProperty = ViewportWidthKey.DependencyProperty;

    private static readonly DependencyPropertyKey CanScrollLeftKey =
        DependencyProperty.RegisterReadOnly(nameof(CanScrollLeft), typeof(bool), typeof(RibbonScrollContentHost), new PropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="CanScrollLeft"/> dependency property.</summary>
    public static readonly DependencyProperty CanScrollLeftProperty = CanScrollLeftKey.DependencyProperty;

    private static readonly DependencyPropertyKey CanScrollRightKey =
        DependencyProperty.RegisterReadOnly(nameof(CanScrollRight), typeof(bool), typeof(RibbonScrollContentHost), new PropertyMetadata(false));

    /// <summary>Identifies the read-only <see cref="CanScrollRight"/> dependency property.</summary>
    public static readonly DependencyProperty CanScrollRightProperty = CanScrollRightKey.DependencyProperty;

    private readonly TranslateTransform _translate = new TranslateTransform();
    private double _reportedContentWidth;

    // Animated-scroll state: the offset the current glide is heading toward, and a generation
    // counter so a superseded animation's Completed callback doesn't finalize a stale target.
    private double _animTarget;
    private bool _animActive;
    private int _animGeneration;

    /// <summary>Initializes the host (clips to its bounds; wires the scroll commands).</summary>
    public RibbonScrollContentHost()
    {
        ClipToBounds = true;
        ScrollLeftCommand = new ScrollCommand(this, -1);
        ScrollRightCommand = new ScrollCommand(this, +1);
    }

    /// <summary>How far the child is scrolled to the left, in DIPs (0 = start). Clamped to the scrollable range.</summary>
    public double Offset
    {
        get => (double)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    /// <summary>When true, the child is measured at the viewport width (groups row: reduce-then-scroll); when false, at its natural width (tab strip).</summary>
    public bool ConstrainChildWidth
    {
        get => (bool)GetValue(ConstrainChildWidthProperty);
        set => SetValue(ConstrainChildWidthProperty, value);
    }

    /// <summary>How much one chevron click scrolls, in DIPs.</summary>
    public double LineSize
    {
        get => (double)GetValue(LineSizeProperty);
        set => SetValue(LineSizeProperty, value);
    }

    /// <summary>The child's full (unclipped) width.</summary>
    public double ExtentWidth => (double)GetValue(ExtentWidthProperty);

    /// <summary>The visible width.</summary>
    public double ViewportWidth => (double)GetValue(ViewportWidthProperty);

    /// <summary>Whether there is hidden content to the left (chevron-left should show).</summary>
    public bool CanScrollLeft => (bool)GetValue(CanScrollLeftProperty);

    /// <summary>Whether there is hidden content to the right (chevron-right should show).</summary>
    public bool CanScrollRight => (bool)GetValue(CanScrollRightProperty);

    /// <summary>
    /// Called by the adaptive <see cref="RibbonGroupsPanel"/> during its measure pass to report its
    /// TRUE (unclamped) content width. WPF clamps a child's reported DesiredSize to the width we measure
    /// it at, so when we constrain the groups row to the viewport we can't otherwise see the overflow
    /// left after full reduction; the panel hands us the real width so the chevrons can still appear.
    /// </summary>
    internal void ReportContentWidth(double width) => _reportedContentWidth = width;

    /// <summary>Scrolls one line toward the start; disabled when already at the start.</summary>
    public ICommand ScrollLeftCommand { get; }

    /// <summary>Scrolls one line toward the end; disabled when already at the end.</summary>
    public ICommand ScrollRightCommand { get; }

    /// <summary>
    /// Forces a full re-evaluation of the overflow state. When the hosted content is swapped without any
    /// size change — the ribbon replacing the groups row on a tab switch — WPF reuses the cached measure
    /// of every level, so the constrained child never re-reports its true width and the chevrons keep the
    /// previous content's state. Invalidating measure across the ENTIRE visual subtree (not just this
    /// decorator) dirties every level, so the next pass re-measures top-down: the child re-reports and
    /// this host reads it and recomputes <see cref="CanScrollLeft"/>/<see cref="CanScrollRight"/> — the
    /// same cascade a manual window resize produces. The owning control calls this after a content swap.
    /// </summary>
    public void Refresh() => InvalidateSubtreeMeasure(this);

    private static void InvalidateSubtreeMeasure(DependencyObject root)
    {
        if (root is UIElement element)
        {
            element.InvalidateMeasure();
        }

        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            InvalidateSubtreeMeasure(VisualTreeHelper.GetChild(root, i));
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        UIElement child = Child;
        if (child is null)
        {
            SetValue(ExtentWidthKey, 0d);
            return default;
        }

        // Two measuring modes:
        //  • Groups row (ConstrainChildWidth): measure the child AT the viewport width so the adaptive
        //    RibbonGroupsPanel reduces groups to fit the visible area first (reduce-then-scroll). WPF
        //    clamps the child's DesiredSize to that width, hiding any leftover overflow, so the panel
        //    reports its true width to us via ReportContentWidth and we use that for the extent.
        //  • Tab strip (unconstrained): measure at infinity so the tabs keep their natural width and the
        //    reported DesiredSize already reveals the overflow directly.
        double measureWidth = ConstrainChildWidth && !double.IsInfinity(constraint.Width)
            ? constraint.Width
            : double.PositiveInfinity;

        _reportedContentWidth = 0d;
        child.Measure(new Size(measureWidth, constraint.Height));

        double extent = ConstrainChildWidth && _reportedContentWidth > 0d
            ? _reportedContentWidth
            : child.DesiredSize.Width;
        SetValue(ExtentWidthKey, extent);

        double reportedWidth = double.IsInfinity(constraint.Width) ? extent : Math.Min(extent, constraint.Width);
        return new Size(reportedWidth, child.DesiredSize.Height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        UIElement child = Child;
        SetValue(ViewportWidthKey, arrangeSize.Width);

        double extent = ExtentWidth;
        double max = Math.Max(0d, extent - arrangeSize.Width);
        double offset = Math.Max(0d, Math.Min(Offset, max));
        if (Math.Abs(offset - Offset) > 0.1)
        {
            SetCurrentValue(OffsetProperty, offset);
        }

        if (child != null)
        {
            if (!ReferenceEquals(child.RenderTransform, _translate))
            {
                child.RenderTransform = _translate;
            }

            _translate.X = -offset;
            child.Arrange(new Rect(0d, 0d, Math.Max(extent, arrangeSize.Width), arrangeSize.Height));
        }

        SetValue(CanScrollLeftKey, offset > 0.5d);
        SetValue(CanScrollRightKey, offset < max - 0.5d);
        (ScrollLeftCommand as ScrollCommand)?.RaiseCanExecuteChanged();
        (ScrollRightCommand as ScrollCommand)?.RaiseCanExecuteChanged();
        return arrangeSize;
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (ExtentWidth > ViewportWidth + 0.5d)
        {
            ScrollBy(-Math.Sign(e.Delta) * LineSize);
            e.Handled = true;
        }

        base.OnMouseWheel(e);
    }

    private static object CoerceOffset(DependencyObject d, object baseValue)
    {
        double value = (double)baseValue;
        return value < 0d ? 0d : value;
    }

    private void Scroll(int direction) => ScrollBy(direction * LineSize);

    /// <summary>
    /// Scrolls by <paramref name="delta"/> DIPs, gliding to the new offset with the
    /// <see cref="RibbonAnimationAction.RibbonScroll"/> timing. Successive calls accumulate from
    /// the pending target (not the current mid-glide value) so rapid chevron clicks / wheel ticks
    /// keep advancing instead of snapping back to where the last glide happens to be.
    /// </summary>
    private void ScrollBy(double delta)
    {
        double basis = _animActive ? _animTarget : Offset;
        AnimateOffsetTo(basis + delta);
    }

    private void AnimateOffsetTo(double target)
    {
        double max = Math.Max(0d, ExtentWidth - ViewportWidth);
        target = Math.Max(0d, Math.Min(target, max));

        // No motion wanted, or the move is negligible → set the value directly.
        if (!RibbonAnimation.IsEnabled(RibbonAnimationAction.RibbonScroll)
            || Math.Abs(target - Offset) < 0.5d)
        {
            BeginAnimation(OffsetProperty, null);
            _animActive = false;
            Offset = target;
            return;
        }

        int generation = ++_animGeneration;
        _animTarget = target;
        _animActive = true;

        var anim = new DoubleAnimation(Offset, target, RibbonAnimation.GetDuration(RibbonAnimationAction.RibbonScroll))
        {
            EasingFunction = RibbonAnimation.GetEase(RibbonAnimationAction.RibbonScroll),
        };
        anim.Completed += (_, _) =>
        {
            // Ignore if a newer glide superseded this one (BeginAnimation replacing an animation
            // does not fire the old one's Completed, but guard anyway for the finalize race).
            if (generation != _animGeneration)
            {
                return;
            }

            // Release the animation and write the plain value so Offset settles as a local value
            // (keeps CanScrollLeft/Right and the clamp logic reading a stable, non-animated offset).
            BeginAnimation(OffsetProperty, null);
            _animActive = false;
            Offset = target;
        };
        BeginAnimation(OffsetProperty, anim);
    }

    /// <summary>A minimal command that scrolls the host one line in a fixed direction.</summary>
    private sealed class ScrollCommand : ICommand
    {
        private readonly RibbonScrollContentHost _host;
        private readonly int _direction;

        public ScrollCommand(RibbonScrollContentHost host, int direction)
        {
            _host = host;
            _direction = direction;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _direction < 0 ? _host.CanScrollLeft : _host.CanScrollRight;

        public void Execute(object parameter) => _host.Scroll(_direction);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
