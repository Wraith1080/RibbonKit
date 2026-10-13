using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

public partial class Ribbon
{
    private RibbonLayoutTransition? _layoutMotion;
    private RibbonLayoutHold? _layoutHold;
    private RibbonDensityTransition? _simplifiedTabMotion;
    internal bool IsLayoutMotionChanging { get; private set; }

    private void RequestLayoutMotion(RibbonAnimationAction action)
    {
        CancelDensityTransition();
        IsLayoutMotionChanging = true;
        bool animate = _densityTransitionsReady && IsLoaded && IsVisible
            && !DesignerProperties.GetIsInDesignMode(this) && RibbonAnimation.IsEnabled(action);
        FrameworkElement? body = _ribbonContentHost;
        var previous = animate && !IsMinimized && body is { IsVisible: true, ActualHeight: > 0 }
            ? RibbonLayoutSnapshot.Capture(body) : null;
        // Move the clipping/shadow wrapper as a unit. Translating only its Border
        // would crop an upward glide against the wrapper's final layout slot.
        var below = FindDescendantByName(this, "QatBelowShadowHost") ?? _qatBelowHost;
        Point? previousQat = animate && !IsMinimized && below is { IsVisible: true }
            ? below.TranslatePoint(new Point(), this) : null;
        if (previous is not null)
            _layoutHold = RibbonLayoutHold.Start(body!, previous,
                QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon ? below : null);
        int generation = _densityTransitionGeneration;
        // Simplified copies, borrowed group content and density measurements all
        // settle before this capture. The final real layout is never animated.
        _pendingDensityTransition = Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)(() =>
        {
            _pendingDensityTransition = null;
            if (!IsLoaded || !IsVisible || generation != _densityTransitionGeneration)
            { IsLayoutMotionChanging = false; return; }
            _layoutHold?.Dispose();
            _layoutHold = null;
            (Window.GetWindow(this) as FrameworkElement ?? this).UpdateLayout();
            RefreshSelectionVisuals();
            if (generation != _densityTransitionGeneration) return;
            if (!animate || !RibbonAnimation.IsEnabled(action))
            { IsLayoutMotionChanging = false; return; }
            if (previous is not null && ReferenceEquals(body, _ribbonContentHost) && !HasLayoutMotionPopup())
                _layoutMotion = RibbonLayoutTransition.Start(body!, previous, action, () =>
                { if (generation == _densityTransitionGeneration) CancelDensityTransition(); });
            // Custom templates without an adorner layer and anchored native menus
            // retain an opacity-only settle; never move a live popup anchor.
            if (_layoutMotion is null && _ribbonTabControl is { IsVisible: true } tabs)
                _densityTransitions.Add(new RibbonDensityTransition(tabs, action: action));
            if (QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon && below is { IsVisible: true })
            {
                double offset = previousQat is { } point && !HasLayoutMotionPopup() && !IsMinimized
                    ? point.Y - below.TranslatePoint(new Point(), this).Y : 0;
                _densityTransitions.Add(new RibbonDensityTransition(below, offset, action,
                    fade: _layoutMotion is null));
            }
            else if (action == RibbonAnimationAction.DensityChange && _titleBarQatHost is { IsVisible: true } title)
                _densityTransitions.Add(new RibbonDensityTransition(title, action: action));
            IsLayoutMotionChanging = false;
        }));
    }

    private bool HasLayoutMotionPopup() => IsBackstageOpen || IsApplicationMenuOpen
        || ContextMenu?.IsOpen == true || PopupDismissHelper.HasOpenPopupWithin(this)
        || _qatBelowHost?.ContextMenu?.IsOpen == true
        || _titleBarQatHost?.ContextMenu?.IsOpen == true
        || _titleBarQatHost is { } title && PopupDismissHelper.HasOpenPopupWithin(title);

    private void OnLayoutMotionInput(object sender, InputEventArgs e) => CancelDensityTransition();

    private void AttachLayoutMotionInput()
    {
        DetachLayoutMotionInput();
        AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnLayoutMotionInput), true);
        AddHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnLayoutMotionInput), true);
        AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnLayoutMotionInput), true);
    }

    private void DetachLayoutMotionInput()
    {
        RemoveHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(OnLayoutMotionInput));
        RemoveHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnLayoutMotionInput));
        RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnLayoutMotionInput));
    }
}
