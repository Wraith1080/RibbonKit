using System.Windows;
using System.Windows.Media.Animation;
using RibbonKit.Controls;

namespace RibbonKit.Animation;

/// <summary>Effective ribbon state changes participating in one presentation transition.</summary>
[Flags]
public enum RibbonLayoutTransitionReason
{
    /// <summary>No effective change.</summary>
    None = 0,
    /// <summary>Classic/Simplified changed.</summary>
    LayoutMode = 1,
    /// <summary>Compact/Touch changed.</summary>
    Density = 2,
    /// <summary>The QAT changed placement.</summary>
    QuickAccessPlacement = 4,
    /// <summary>Simplified capability caused an effective layout change.</summary>
    Capability = 8,
    /// <summary>QAT visibility changed.</summary>
    QuickAccessVisibility = 16,
}

/// <summary>How a prepared presentation transition terminated.</summary>
public enum RibbonLayoutTransitionOutcome
{
    /// <summary>All participating clocks finished.</summary>
    Completed,
    /// <summary>A newer effective state replaced this transition.</summary>
    Superseded,
    /// <summary>Input, lifecycle, geometry or policy invalidated the presentation.</summary>
    Interrupted,
    /// <summary>No safe enabled presentation could start.</summary>
    Skipped,
}

/// <summary>An immutable effective state; requested dependency-property values remain untouched.</summary>
public sealed class RibbonLayoutTransitionState
{
    internal RibbonLayoutTransitionState(RibbonLayoutMode mode, RibbonDensity density,
        RibbonQuickAccessPosition position, bool visible, bool minimized)
    { LayoutMode = mode; Density = density; QuickAccessPosition = position; IsQuickAccessVisible = visible; IsMinimized = minimized; }

    /// <summary>Effective body layout.</summary>
    public RibbonLayoutMode LayoutMode { get; }
    /// <summary>Effective command density.</summary>
    public RibbonDensity Density { get; }
    /// <summary>Selected QAT placement.</summary>
    public RibbonQuickAccessPosition QuickAccessPosition { get; }
    /// <summary>Whether the QAT is enabled for display.</summary>
    public bool IsQuickAccessVisible { get; }
    /// <summary>Whether the ribbon body is minimized.</summary>
    public bool IsMinimized { get; }
}

/// <summary>Measured geometry in the event's coordinate root, in device-independent pixels.</summary>
public sealed class RibbonLayoutTransitionGeometry
{
    internal RibbonLayoutTransitionGeometry(Rect occupied, Rect painted, Rect? qat, Rect? panel, DpiScale dpi)
    { OccupiedBounds = occupied; PaintedBounds = painted; QuickAccessBounds = qat; QuickAccessPanelBounds = panel; Dpi = dpi; }

    /// <summary>Ribbon layout extent, including the lower QAT and hosted message rows; excludes shadow overhang.</summary>
    public Rect OccupiedBounds { get; }
    /// <summary>Occupied extent plus measured library paint and shadow overhang.</summary>
    public Rect PaintedBounds { get; }
    /// <summary>QAT command-strip bounds, or null when not realized or safely mappable.</summary>
    public Rect? QuickAccessBounds { get; }
    /// <summary>BelowRibbon panel layout bounds, or null when absent.</summary>
    public Rect? QuickAccessPanelBounds { get; }
    /// <summary>DPI at measurement. A DPI change interrupts playback.</summary>
    public DpiScale Dpi { get; }
}

/// <summary>Read-only progress driven by RibbonKit's shared transition clock. Hosts may bind render-only presentation to it.</summary>
public sealed class RibbonLayoutTransitionMotion : DependencyObject
{
    private static readonly DependencyPropertyKey ProgressPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Progress), typeof(double), typeof(RibbonLayoutTransitionMotion), new PropertyMetadata(0d));
    /// <summary>Identifies the read-only eased progress property.</summary>
    public static readonly DependencyProperty ProgressProperty = ProgressPropertyKey.DependencyProperty;
    private AnimationClock? _clock;

    internal RibbonLayoutTransitionMotion(RibbonAnimationAction action, bool enabled)
    {
        Action = action;
        EffectiveLevel = RibbonAnimation.GetEffectiveLevel(action);
        Duration = enabled ? RibbonAnimation.GetDuration(action) : new Duration(TimeSpan.Zero);
        Easing = RibbonAnimation.GetEase(action);
        if (Easing is Freezable freezable && !freezable.IsFrozen)
        { freezable = freezable.Clone(); freezable.Freeze(); Easing = (IEasingFunction)freezable; }
    }

    /// <summary>Policy controlling this presentation track.</summary>
    public RibbonAnimationAction Action { get; }
    /// <summary>Effective configured level, including system reduced motion.</summary>
    public RibbonAnimationLevel EffectiveLevel { get; }
    /// <summary>Actual playback duration; zero for a skipped track.</summary>
    public Duration Duration { get; }
    /// <summary>Frozen easing function used by this track.</summary>
    public IEasingFunction Easing { get; }
    /// <summary>Whether this track plays rather than settling immediately.</summary>
    public bool IsAnimated => Duration.TimeSpan > TimeSpan.Zero;
    /// <summary>Eased progress. Expressive translation may overshoot; bound clips and opacity to zero through one.</summary>
    public double Progress => (double)GetValue(ProgressProperty);

    internal event EventHandler? Changed;
    internal DoubleAnimation Timeline() => new(0d, 1d, Duration) { EasingFunction = Easing };
    internal void Attach(AnimationClock clock)
    { _clock = clock; clock.CurrentTimeInvalidated += OnTick; }
    private void OnTick(object? sender, EventArgs e)
    { if (_clock is { } clock) SetProgress((double)clock.GetCurrentValue(0d, 1d)); }
    private void SetProgress(double value)
    { SetValue(ProgressPropertyKey, value); Changed?.Invoke(this, EventArgs.Empty); }
    internal void Detach(bool completed)
    {
        if (_clock is { } clock) clock.CurrentTimeInvalidated -= OnTick;
        _clock = null;
        if (completed || !IsAnimated) SetProgress(1d);
    }
}

/// <summary>Read-only lifecycle context. Preparing has no final geometry; every prepared identity receives one Ended notification.</summary>
public sealed class RibbonLayoutTransitionEventArgs : EventArgs
{
    internal RibbonLayoutTransitionEventArgs(long id, RibbonLayoutTransitionReason reason,
        RibbonLayoutTransitionState previous, RibbonLayoutTransitionState target, FrameworkElement root,
        RibbonLayoutTransitionGeometry before, RibbonLayoutTransitionGeometry? final,
        RibbonLayoutTransitionMotion boundary, RibbonLayoutTransitionMotion? qat,
        bool started, RibbonLayoutTransitionOutcome? outcome = null)
    {
        TransitionId = id; Reason = reason; PreviousState = previous; TargetState = target;
        CoordinateRoot = root; PreviousGeometry = before; FinalGeometry = final;
        BoundaryMotion = boundary; QuickAccessMotion = qat; WasStarted = started; Outcome = outcome;
    }
    /// <summary>Identity scoped to the owning Ribbon; use it to guard host cleanup.</summary>
    public long TransitionId { get; }
    /// <summary>Effective changes included in this transition.</summary>
    public RibbonLayoutTransitionReason Reason { get; }
    /// <summary>Previous effective state.</summary>
    public RibbonLayoutTransitionState PreviousState { get; }
    /// <summary>Destination effective state.</summary>
    public RibbonLayoutTransitionState TargetState { get; }
    /// <summary>Visual whose local DIP coordinates define every reported rectangle. Use visual transforms for RTL and detached QAT hosts.</summary>
    public FrameworkElement CoordinateRoot { get; }
    /// <summary>Previous occupied and painted geometry.</summary>
    public RibbonLayoutTransitionGeometry PreviousGeometry { get; }
    /// <summary>Committed measured geometry; null before measurement or if preparation was interrupted.</summary>
    public RibbonLayoutTransitionGeometry? FinalGeometry { get; }
    /// <summary>Shared boundary progress. Hosts own their content drawing and consume this source without starting a timer.</summary>
    public RibbonLayoutTransitionMotion BoundaryMotion { get; }
    /// <summary>Independent QuickAccessMove track when placement changes; otherwise null.</summary>
    public RibbonLayoutTransitionMotion? QuickAccessMotion { get; }
    /// <summary>Whether Starting was delivered for this identity.</summary>
    public bool WasStarted { get; }
    /// <summary>Terminal outcome on Ended; otherwise null.</summary>
    public RibbonLayoutTransitionOutcome? Outcome { get; }
}
