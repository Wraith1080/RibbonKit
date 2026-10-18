using System.Windows;
using System.Windows.Input;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

public partial class Ribbon
{
    private RibbonLayoutTransition? _layoutMotion;
    private RibbonLayoutHold? _layoutHold;
    private RibbonDensityTransition? _simplifiedTabMotion;
    internal bool IsLayoutMotionChanging { get; private set; }

    private void RequestLayoutMotion(RibbonAnimationAction action, RibbonLayoutTransitionState? previous = null)
    {
        var reason = action == RibbonAnimationAction.LayoutModeChange ? RibbonLayoutTransitionReason.LayoutMode
            : action == RibbonAnimationAction.DensityChange ? RibbonLayoutTransitionReason.Density
            : previous?.QuickAccessPosition != QuickAccessPosition ? RibbonLayoutTransitionReason.QuickAccessPlacement
            : RibbonLayoutTransitionReason.QuickAccessVisibility;
        if (_changingSimplifiedCapability) reason |= RibbonLayoutTransitionReason.Capability;
        (_layoutCoordinator ??= new LayoutTransitionCoordinator(this)).Request(previous ?? LayoutState(), reason);
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
