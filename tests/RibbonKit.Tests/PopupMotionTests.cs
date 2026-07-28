using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Animation;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>
/// The flyout open transition — design notes §3.42. Two things are pinned here: the geometric
/// invariant every animated popup template depends on, and the wiring of
/// <see cref="RibbonPopupMotion"/>, which is the only entry point that hangs off an event rather
/// than a control's own override.
/// </summary>
/// <remarks>
/// <para>
/// The geometry matters because it is invisible in the XAML. A popup's window is sized to its
/// child's LAYOUT size and a <c>RenderTransform</c> does not grow it, so a surface sliding in from
/// above is sliced against the window's top edge. Every animated flyout compensates with a matched
/// pair — extra top margin on the child, and a negative <c>VerticalOffset</c> of the same size —
/// and that headroom only works while it exceeds the slide distance. The slide distance is not a
/// constant: <see cref="RibbonAnimation.GetSlideOffset"/> multiplies by 1.8 at
/// <see cref="RibbonAnimationLevel.Expressive"/>, which is how the original 10px headroom came to
/// clip at 14.4px of travel. These tests fail the moment a future offset change re-opens that gap,
/// which is the whole point — nothing else would notice until someone switched to Expressive and
/// looked closely at a top edge.
/// </para>
/// <para>
/// No popup is ever opened here, per the harness doctrine in <see cref="Sta"/>: the routed
/// <see cref="ContextMenu.Opened"/> event can be raised directly, which exercises the real handler
/// without needing a rendered popup window.
/// </para>
/// </remarks>
public class PopupMotionTests
{
    /// <summary>
    /// Headroom shipped by each animated flyout template, keyed by the action that drives it.
    /// Keep in step with <c>Controls.DropDowns.xaml</c>, <c>Controls.Galleries.xaml</c> and
    /// <c>Menus.xaml</c>.
    /// </summary>
    public static TheoryData<RibbonAnimationAction, double> TemplateHeadroom => new()
    {
        // Drop-down button, split button, combo box, context menu, submenu: top margin +16,
        // VerticalOffset -16.
        { RibbonAnimationAction.DropdownMenu, 16d },

        // InRibbonGallery flyout: top margin 4 -> 24, VerticalOffset -4 -> -24.
        { RibbonAnimationAction.Gallery, 20d },
    };

    [Theory]
    [MemberData(nameof(TemplateHeadroom))]
    public void Headroom_exceeds_the_slide_at_every_animation_level(
        RibbonAnimationAction action,
        double headroom) => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();
        RibbonAnimationLevel original = RibbonAnimation.GlobalLevel;

        try
        {
            foreach (RibbonAnimationLevel level in Enum.GetValues<RibbonAnimationLevel>())
            {
                RibbonAnimation.GlobalLevel = level;
                double offset = RibbonAnimation.GetSlideOffset(action);

                Assert.True(
                    offset < headroom,
                    $"{action} slides {offset}px at {level} but its templates only reserve " +
                    $"{headroom}px of headroom — the popup's top edge will slice the surface. " +
                    "Raise the child's top margin AND the popup's negative VerticalOffset by the " +
                    "same amount (they are a matched pair; the resting position must not move).");
            }
        }
        finally
        {
            RibbonAnimation.GlobalLevel = original;
        }
    });

    [Fact]
    public void AnimateOpen_is_off_and_defaults_to_the_menu_timing() => Sta.Run(() =>
    {
        var menu = new ContextMenu();

        Assert.False(RibbonPopupMotion.GetAnimateOpen(menu));
        Assert.Equal(RibbonAnimationAction.DropdownMenu, RibbonPopupMotion.GetOpenAction(menu));
    });

    [Fact]
    public void An_opening_context_menu_animates_itself() => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();
        var menu = new ContextMenu();
        RibbonPopupMotion.SetAnimateOpen(menu, true);

        RaiseOpened(menu);

        // The menu IS the child of the popup WPF builds for it, so the menu itself is what moves.
        Assert.True(menu.HasAnimatedProperties);
        Assert.IsType<TranslateTransform>(menu.RenderTransform);
    });

    [Fact]
    public void Clearing_AnimateOpen_unsubscribes() => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();
        var menu = new ContextMenu();
        RibbonPopupMotion.SetAnimateOpen(menu, true);
        RaiseOpened(menu);

        Reset(menu);
        RibbonPopupMotion.SetAnimateOpen(menu, false);
        RaiseOpened(menu);

        Assert.False(menu.HasAnimatedProperties);
    });

    [Fact]
    public void A_disabled_action_leaves_the_surface_at_rest() => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();
        var menu = new ContextMenu();
        RibbonPopupMotion.SetAnimateOpen(menu, true);

        RibbonAnimation.SetActionLevel(RibbonAnimationAction.DropdownMenu, RibbonAnimationLevel.None);

        try
        {
            RaiseOpened(menu);

            // Rest(), not merely "no animation": a surface left at Opacity 0 would be an
            // invisible menu, which is a far worse failure than a missing transition.
            Assert.Equal(1d, menu.Opacity);
            Assert.Equal(0d, Offset(menu));
        }
        finally
        {
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.DropdownMenu);
        }
    });

    [Fact]
    public void Attaching_to_something_that_is_not_a_flyout_is_a_no_op() => Sta.Run(() =>
    {
        // A Style setter can land on anything; the behaviour must not throw on a plain element.
        var border = new Border();

        RibbonPopupMotion.SetAnimateOpen(border, true);
        RibbonPopupMotion.SetAnimateOpen(border, false);

        Assert.False(border.HasAnimatedProperties);
    });

    /// <summary>
    /// Pins motion ON for the duration of a test. Without this the suite fails on any machine
    /// whose OS reduced-motion preference is set — <see cref="RibbonAnimation.GetEffectiveLevel"/>
    /// forces every action to <see cref="RibbonAnimationLevel.None"/> there, which is correct
    /// behaviour and exactly what these tests must not be measuring.
    /// </summary>
    private sealed class ForcedMotion : IDisposable
    {
        private readonly bool _original = RibbonAnimation.RespectSystemReduceMotion;

        public ForcedMotion() => RibbonAnimation.RespectSystemReduceMotion = false;

        public void Dispose() => RibbonAnimation.RespectSystemReduceMotion = _original;
    }

    private static void RaiseOpened(ContextMenu menu) =>
        menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent, menu));

    private static double Offset(UIElement element) =>
        element.RenderTransform is TranslateTransform translate ? translate.Y : 0d;

    private static void Reset(UIElement element)
    {
        element.BeginAnimation(UIElement.OpacityProperty, null);

        if (element.RenderTransform is TranslateTransform translate)
        {
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
        }
    }
}
