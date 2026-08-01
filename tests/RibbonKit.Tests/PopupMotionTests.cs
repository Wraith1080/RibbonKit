using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Animation;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>
/// The flyout open transition — design notes §3.42. Two things are pinned: the invariant the whole
/// design rests on (the SURFACE is never transformed, only its content moves) and the wiring of
/// <see cref="RibbonPopupMotion"/>, the only entry point that hangs off an event rather than a
/// control's own override.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>Why the surface invariant matters.</b> A <see cref="System.Windows.Controls.Primitives.Popup"/>'s
/// window is sized to its child's LAYOUT size and a transform is not layout, so a surface that
/// moves is sliced against the window edge. The obvious remedy — extra margin on the child plus a
/// placement offset to cancel it — cost this project two rounds of broken positioning, because
/// whether that margin displaces the surface differs between popup kinds: a plain <c>Popup</c>
/// compensates for it, a <see cref="ComboBox"/>'s managed popup does not. There was no single pair
/// of numbers correct everywhere.
/// </para>
/// <para>
/// Animating the CONTENT instead removes the question. It travels inside the surface's padding, so
/// it never reaches the window edge, and every template keeps its original geometry.
/// <see cref="The_surface_is_never_transformed"/> is what stops a future "make the whole card move"
/// edit from quietly reintroducing the displacement on all seven flyouts at once.
/// </para>
/// <para>
/// No popup is ever opened here, per the harness doctrine in <see cref="Sta"/>: the routed
/// <see cref="ContextMenu.Opened"/> event can be raised directly, which exercises the real handler
/// without needing a rendered popup window.
/// </para>
/// </remarks>
public class PopupMotionTests
{
    /// <summary>The actions that drive a flyout surface. Keep in step with the call sites.</summary>
    public static TheoryData<RibbonAnimationAction> FlyoutActions => new()
    {
        // Drop-down and split buttons, combo box, context menu and submenus, QAT overflow,
        // collapsed-group flyout.
        RibbonAnimationAction.DropdownMenu,

        // In-ribbon gallery.
        RibbonAnimationAction.Gallery,
    };

    [Theory]
    [MemberData(nameof(FlyoutActions))]
    public void The_surface_is_never_transformed(RibbonAnimationAction action) => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();
        RibbonAnimationLevel original = RibbonAnimation.GlobalLevel;

        try
        {
            foreach (RibbonAnimationLevel level in Enum.GetValues<RibbonAnimationLevel>())
            {
                RibbonAnimation.GlobalLevel = level;

                var content = new Border();
                var surface = new Border { Child = content };

                RibbonMotion.PlayFlyoutOpen(surface, action);

                Assert.True(
                    IsIdentity(surface.RenderTransform),
                    $"The flyout surface carries a {surface.RenderTransform?.GetType().Name} at " +
                    $"{level}. It must only ever FADE: a popup's window is sized to its child's " +
                    "layout size, so a transformed surface is clipped at the window edge, and the " +
                    "margin/offset compensation that would give it room is not portable across " +
                    "popup kinds (§3.42). Move the content instead.");
            }
        }
        finally
        {
            RibbonAnimation.GlobalLevel = original;
        }
    });

    [Theory]
    [MemberData(nameof(FlyoutActions))]
    public void The_content_starts_above_its_resting_place(RibbonAnimationAction action) => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();

        var content = new Border();
        var surface = new Border { Child = content };

        RibbonMotion.PlayFlyoutOpen(surface, action);

        // Seeded negative, so the content settles DOWNWARD out of the opener. Seeding before
        // BeginAnimation is deliberate — see PlayFlyoutOpen and §3.41's FLIP notes.
        Assert.True(OffsetY(content) < 0d, $"{action} should seed the content above its rest.");
        Assert.True(content.HasAnimatedProperties || content.RenderTransform is TranslateTransform);
    });

    [Theory]
    [MemberData(nameof(FlyoutActions))]
    public void The_surface_starts_transparent(RibbonAnimationAction action) => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();

        var surface = new Border { Child = new Border() };
        RibbonMotion.PlayFlyoutOpen(surface, action);

        // Seeded, not merely animated from 0. An unseeded fade renders ONE frame at the base value
        // (1) before the clock ticks, so the surface pops in at full strength and only then fades —
        // a flicker, not a fade. §3.41 found the same rule for the title glide.
        Assert.Equal(0d, surface.Opacity);
    });

    [Fact]
    public void AnimateOpen_is_off_and_defaults_to_the_menu_timing() => Sta.Run(() =>
    {
        var menu = new ContextMenu();

        Assert.False(RibbonPopupMotion.GetAnimateOpen(menu));
        Assert.Equal(RibbonAnimationAction.DropdownMenu, RibbonPopupMotion.GetOpenAction(menu));
    });

    [Fact]
    public void An_opening_context_menu_fades_itself() => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();
        var menu = new ContextMenu();
        RibbonPopupMotion.SetAnimateOpen(menu, true);

        RaiseOpened(menu);

        // The menu IS the child of the popup WPF builds for it, so the menu itself is the surface —
        // faded, never moved.
        Assert.True(menu.HasAnimatedProperties);
        Assert.True(IsIdentity(menu.RenderTransform));
    });

    [Fact]
    public void Clearing_AnimateOpen_unsubscribes() => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();
        var menu = new ContextMenu();
        RibbonPopupMotion.SetAnimateOpen(menu, true);
        RaiseOpened(menu);

        menu.BeginAnimation(UIElement.OpacityProperty, null);
        RibbonPopupMotion.SetAnimateOpen(menu, false);
        RaiseOpened(menu);

        Assert.False(menu.HasAnimatedProperties);
    });

    [Fact]
    public void A_disabled_action_leaves_everything_at_rest() => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();

        var content = new Border();
        var surface = new Border { Child = content };

        RibbonAnimation.SetActionLevel(RibbonAnimationAction.DropdownMenu, RibbonAnimationLevel.None);

        try
        {
            RibbonMotion.PlayFlyoutOpen(surface, RibbonAnimationAction.DropdownMenu);

            // Rest(), not merely "no animation": a surface left at Opacity 0 would be an invisible
            // menu, far worse than a missing transition.
            Assert.Equal(1d, surface.Opacity);
            Assert.Equal(1d, content.Opacity);
            Assert.Equal(0d, OffsetY(content));
        }
        finally
        {
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.DropdownMenu);
        }
    });

    [Fact]
    public void Ribbon_context_menus_scope_and_restore_the_native_wpf_popup_animation() => Sta.Run(() =>
    {
        var resources = new ResourceDictionary
        {
            [SystemParameters.MenuPopupAnimationKey] = PopupAnimation.Fade,
        };
        var menu = new ContextMenu();

        RibbonPopupMotion.SuppressNativeContextMenuAnimationForOpen(menu, resources);

        Assert.Equal(
            PopupAnimation.None,
            Assert.IsType<PopupAnimation>(resources[SystemParameters.MenuPopupAnimationKey]));

        menu.RaiseEvent(new RoutedEventArgs(ContextMenu.ClosedEvent, menu));

        Assert.Equal(
            PopupAnimation.Fade,
            Assert.IsType<PopupAnimation>(resources[SystemParameters.MenuPopupAnimationKey]));
    });

    [Fact]
    public void Rest_undoes_an_open_transition() => Sta.Run(() =>
    {
        using var motion = new ForcedMotion();

        var content = new Border();
        var surface = new Border { Child = content };

        RibbonMotion.PlayFlyoutOpen(surface, RibbonAnimationAction.DropdownMenu);
        RibbonMotion.Rest(surface);
        RibbonMotion.Rest(content);

        Assert.Equal(1d, surface.Opacity);
        Assert.Equal(0d, OffsetY(content));
        Assert.False(content.HasAnimatedProperties);
    });

    /// <summary>
    /// ⚠ Every transition must survive being started from a thread that did not initialise
    /// <see cref="RibbonAnimation"/>. Its shared easing function is a <see cref="Freezable"/>, and
    /// an unfrozen one would take affinity from the FIRST thread to touch the class — after which
    /// building a clock on any other thread throws from deep inside <c>Clock.AllocateClock</c>, with
    /// nothing in the stack naming the shared static. Real apps hit this the moment a second window
    /// runs on its own dispatcher.
    /// </summary>
    /// <remarks>
    /// Every other test here already crosses threads (<see cref="Sta.Run"/> makes a new one each
    /// time) and they all failed together when this regressed. This one exists so the NAME says why.
    /// </remarks>
    [Fact]
    public void A_transition_starts_on_any_thread()
    {
        for (int pass = 0; pass < 2; pass++)
        {
            Sta.Run(() =>
            {
                using var motion = new ForcedMotion();
                var surface = new Border { Child = new Border() };

                RibbonMotion.PlayFlyoutOpen(surface, RibbonAnimationAction.DropdownMenu);

                Assert.Equal(0d, surface.Opacity);
            });
        }
    }

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

    /// <summary>
    /// Whether a transform is a no-op. ⚠ Test the MATRIX, not the type: an element that has never
    /// been transformed does NOT report null here — <see cref="UIElement.RenderTransform"/> defaults
    /// to <see cref="Transform.Identity"/>, which is a <see cref="MatrixTransform"/>. A type switch
    /// therefore reports "carries a MatrixTransform" for a completely untouched Border, which is how
    /// the first version of this helper failed on every level including None.
    /// </summary>
    private static bool IsIdentity(Transform? transform) => transform is null || transform.Value.IsIdentity;

    private static double OffsetY(UIElement element) =>
        element.RenderTransform is TranslateTransform translate ? translate.Y : 0d;
}
