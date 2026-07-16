using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
            Offset -= Math.Sign(e.Delta) * LineSize;
            e.Handled = true;
        }

        base.OnMouseWheel(e);
    }

    private static object CoerceOffset(DependencyObject d, object baseValue)
    {
        double value = (double)baseValue;
        return value < 0d ? 0d : value;
    }

    private void Scroll(int direction) => Offset += direction * LineSize;

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
