using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RibbonKit.Animation;

/// <summary>
/// The edge a <see cref="RibbonMotion"/> open animation slides in from.
/// </summary>
public enum RibbonSlideFrom
{
    /// <summary>No slide — the element only fades.</summary>
    None,

    /// <summary>Starts slightly above its resting position and settles downward (menus).</summary>
    Top,

    /// <summary>Starts slightly left of its resting position and settles rightward (backstage).</summary>
    Left,

    /// <summary>Starts slightly right of its resting position and settles leftward.</summary>
    Right,

    /// <summary>Starts slightly below its resting position and settles upward.</summary>
    Bottom,
}

/// <summary>
/// Small, reusable transform/opacity transitions shared by the ribbon controls. Every
/// method reads its timing from <see cref="RibbonAnimation"/> for the given
/// <see cref="RibbonAnimationAction"/>, so a disabled action (or system reduced-motion)
/// makes the element appear instantly at its resting state. All motion is composited
/// (opacity + <see cref="TranslateTransform"/>) and never animates a layout-affecting
/// property.
/// </summary>
public static class RibbonMotion
{
    /// <summary>
    /// Plays an "appear" transition on <paramref name="element"/>: a fade-in combined with
    /// an optional short slide from <paramref name="from"/>. Used for menus, flyouts, the
    /// gallery drop-down, and the backstage. When the action is disabled the element is
    /// simply left opaque and untransformed.
    /// </summary>
    public static void PlayOpen(
        FrameworkElement? element,
        RibbonAnimationAction action,
        RibbonSlideFrom from = RibbonSlideFrom.Top)
    {
        if (element is null)
        {
            return;
        }

        if (!RibbonAnimation.IsEnabled(action))
        {
            Rest(element);
            return;
        }

        Duration duration = RibbonAnimation.GetDuration(action);
        IEasingFunction ease = RibbonAnimation.GetEase(action);
        double offset = from == RibbonSlideFrom.None ? 0d : RibbonAnimation.GetSlideOffset(action);

        var fade = new DoubleAnimation(0d, 1d, duration) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            // Leave a normal resting value behind. The seed prevents a first-frame flash, but
            // would leave the element invisible if a later caller cleared the animation alone.
            element.SetValue(UIElement.OpacityProperty, 1d);
            element.BeginAnimation(UIElement.OpacityProperty, null);
        };

        // WPF ticks animation clocks before layout/render. Seed the base value so a transition
        // started later in that frame cannot briefly fall back to the resting opacity of 1.
        element.SetValue(UIElement.OpacityProperty, 0d);
        element.BeginAnimation(UIElement.OpacityProperty, fade);

        TranslateTransform translate = EnsureTranslate(element);
        if (offset <= 0d)
        {
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
            return;
        }

        switch (from)
        {
            case RibbonSlideFrom.Top:
                Slide(translate, TranslateTransform.YProperty, -offset, duration, ease);
                break;
            case RibbonSlideFrom.Bottom:
                Slide(translate, TranslateTransform.YProperty, offset, duration, ease);
                break;
            case RibbonSlideFrom.Left:
                Slide(translate, TranslateTransform.XProperty, -offset, duration, ease);
                break;
            case RibbonSlideFrom.Right:
                Slide(translate, TranslateTransform.XProperty, offset, duration, ease);
                break;
        }
    }

    /// <summary>
    /// Plays a "disappear" transition on <paramref name="element"/>: a fade-out combined
    /// with an optional short slide toward <paramref name="to"/>, then invokes
    /// <paramref name="onCompleted"/> (used to collapse the ribbon body only after it has
    /// slid up). When the action is disabled the callback runs immediately with no motion.
    /// </summary>
    public static void PlayClose(
        FrameworkElement? element,
        RibbonAnimationAction action,
        RibbonSlideFrom to = RibbonSlideFrom.Top,
        Action? onCompleted = null)
    {
        if (element is null)
        {
            onCompleted?.Invoke();
            return;
        }

        if (!RibbonAnimation.IsEnabled(action))
        {
            Rest(element);
            onCompleted?.Invoke();
            return;
        }

        Duration duration = RibbonAnimation.GetDuration(action);
        IEasingFunction ease = RibbonAnimation.GetEase(action);
        double offset = to == RibbonSlideFrom.None ? 0d : RibbonAnimation.GetSlideOffset(action);

        var fade = new DoubleAnimation(1d, 0d, duration) { EasingFunction = ease };
        if (onCompleted is not null)
        {
            fade.Completed += (_, _) => onCompleted();
        }

        element.BeginAnimation(UIElement.OpacityProperty, fade);

        if (offset <= 0d)
        {
            return;
        }

        TranslateTransform translate = EnsureTranslate(element);
        double target = to switch
        {
            RibbonSlideFrom.Top => -offset,
            RibbonSlideFrom.Bottom => offset,
            RibbonSlideFrom.Left => -offset,
            RibbonSlideFrom.Right => offset,
            _ => 0d,
        };
        DependencyProperty axis = to is RibbonSlideFrom.Left or RibbonSlideFrom.Right
            ? TranslateTransform.XProperty
            : TranslateTransform.YProperty;
        var slide = new DoubleAnimation(0d, target, duration) { EasingFunction = ease };
        translate.BeginAnimation(axis, slide);
    }

    /// <summary>
    /// Fades <paramref name="element"/> from transparent to opaque (no slide). Used for
    /// cross-fades such as tab-content switches, quick-access repositioning, and theme
    /// changes. Instant when the action is disabled.
    /// </summary>
    public static void PlayFadeIn(FrameworkElement? element, RibbonAnimationAction action)
    {
        if (element is null)
        {
            return;
        }

        if (!RibbonAnimation.IsEnabled(action))
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = 1d;
            return;
        }

        var fade = new DoubleAnimation(0d, 1d, RibbonAnimation.GetDuration(action))
        {
            EasingFunction = RibbonAnimation.GetEase(action),
        };
        element.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    /// <summary>
    /// Slides <paramref name="element"/> in from a short offset toward its resting position
    /// WITHOUT touching opacity. Preferred over <see cref="PlayFadeIn"/> when the element is
    /// already realized at full opacity (e.g. a tab's content on switch), where a fade would
    /// flash the content to transparent for a frame. Instant when the action is disabled.
    /// </summary>
    public static void PlaySlideIn(
        FrameworkElement? element,
        RibbonAnimationAction action,
        RibbonSlideFrom from = RibbonSlideFrom.Bottom)
    {
        if (element is null)
        {
            return;
        }

        if (!RibbonAnimation.IsEnabled(action) || from == RibbonSlideFrom.None)
        {
            if (element.RenderTransform is TranslateTransform resting)
            {
                resting.BeginAnimation(TranslateTransform.XProperty, null);
                resting.BeginAnimation(TranslateTransform.YProperty, null);
                resting.X = 0d;
                resting.Y = 0d;
            }

            return;
        }

        double offset = RibbonAnimation.GetSlideOffset(action);
        if (offset <= 0d)
        {
            return;
        }

        Duration duration = RibbonAnimation.GetDuration(action);
        IEasingFunction ease = RibbonAnimation.GetEase(action);
        TranslateTransform translate = EnsureTranslate(element);

        switch (from)
        {
            case RibbonSlideFrom.Top:
                Slide(translate, TranslateTransform.YProperty, -offset, duration, ease);
                break;
            case RibbonSlideFrom.Bottom:
                Slide(translate, TranslateTransform.YProperty, offset, duration, ease);
                break;
            case RibbonSlideFrom.Left:
                Slide(translate, TranslateTransform.XProperty, -offset, duration, ease);
                break;
            case RibbonSlideFrom.Right:
                Slide(translate, TranslateTransform.XProperty, offset, duration, ease);
                break;
        }
    }

    /// <summary>
    /// Animates <paramref name="element"/>'s vertical <see cref="TranslateTransform"/> from
    /// <paramref name="fromY"/> to <paramref name="toY"/> (in DIPs) WITHOUT touching opacity.
    /// Used to carry the below-ribbon quick-access bar along with the collapsing/expanding
    /// ribbon body so it glides to its new spot instead of jumping. Snaps to rest (0) when
    /// the action is disabled.
    /// </summary>
    public static void AnimateTranslateY(
        FrameworkElement? element,
        RibbonAnimationAction action,
        double fromY,
        double toY)
    {
        if (element is null)
        {
            return;
        }

        if (!RibbonAnimation.IsEnabled(action))
        {
            Rest(element);
            return;
        }

        TranslateTransform translate = EnsureTranslate(element);
        translate.BeginAnimation(TranslateTransform.XProperty, null);
        translate.SetValue(TranslateTransform.XProperty, 0d);

        var anim = new DoubleAnimation(fromY, toY, RibbonAnimation.GetDuration(action))
        {
            EasingFunction = RibbonAnimation.GetEase(action),
        };
        translate.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    /// <summary>
    /// Animates <paramref name="element"/>'s horizontal <see cref="TranslateTransform"/> from
    /// <paramref name="fromX"/> to <paramref name="toX"/> (in DIPs) WITHOUT touching opacity —
    /// the horizontal twin of <see cref="AnimateTranslateY"/>. Used to glide the window title
    /// across when the quick-access strip beside it appears or disappears (the backstage hides
    /// it), so the centred title slides to its new centre instead of jumping. Snaps to rest (0)
    /// when the action is disabled.
    /// </summary>
    public static void AnimateTranslateX(
        FrameworkElement? element,
        RibbonAnimationAction action,
        double fromX,
        double toX)
    {
        if (element is null)
        {
            return;
        }

        if (!RibbonAnimation.IsEnabled(action))
        {
            Rest(element);
            return;
        }

        TranslateTransform translate = EnsureTranslate(element);
        translate.BeginAnimation(TranslateTransform.YProperty, null);
        translate.SetValue(TranslateTransform.YProperty, 0d);

        // Seed the BASE value with the start offset before starting the clock. WPF ticks the
        // timing manager at the START of a render frame, before layout, so an animation begun
        // during that same frame's layout is not ticked until the NEXT one — and for that first
        // frame the property falls back to its base value. Leaving the base at 0 therefore
        // renders one frame at the DESTINATION before the motion begins, which reads as a flicker
        // (the element snaps to its end position, then jumps back and animates properly).
        // Once the clock does tick, the animation outranks this value entirely.
        translate.SetValue(TranslateTransform.XProperty, fromX);

        var anim = new DoubleAnimation(fromX, toX, RibbonAnimation.GetDuration(action))
        {
            EasingFunction = RibbonAnimation.GetEase(action),
        };
        translate.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    /// <summary>Rotates an element in place for the Classic 2007 application-orb transition.</summary>
    internal static void PlayRotation(
        FrameworkElement? element,
        RibbonAnimationAction action,
        double degrees)
    {
        if (element is null)
        {
            return;
        }

        RotateTransform rotate = EnsureRotate(element);
        rotate.BeginAnimation(RotateTransform.AngleProperty, null);

        if (!RibbonAnimation.IsEnabled(action))
        {
            rotate.Angle = 0d;
            return;
        }

        element.RenderTransformOrigin = new Point(0.5d, 0.5d);
        var animation = new DoubleAnimation(0d, degrees, RibbonAnimation.GetDuration(action))
        {
            EasingFunction = RibbonAnimation.GetEase(action),
        };
        animation.Completed += (_, _) =>
        {
            rotate.BeginAnimation(RotateTransform.AngleProperty, null);
            rotate.Angle = 0d;
        };
        rotate.BeginAnimation(RotateTransform.AngleProperty, animation);
    }

    /// <summary>
    /// Plays a flyout's open transition: the bordered SURFACE fades in while its CONTENT slides
    /// down into place inside it. Used by every RibbonKit popup — drop-down and split buttons, the
    /// combo box, the context menu and its submenus, the in-ribbon gallery, the QAT overflow flyout
    /// and the collapsed-group flyout. Pass the surface; the content is found from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The surface is only ever faded, never transformed. Keep it that way.</b> A
    /// <see cref="System.Windows.Controls.Primitives.Popup"/>'s window is sized to its child's
    /// LAYOUT size and a <see cref="Transform"/> is not layout, so a surface that translates into
    /// place from above is sliced against the window's top edge. The obvious remedy — extra top
    /// margin on the child — is where this got expensive: whether that margin also DISPLACES the
    /// surface differs between popup kinds. A plain <c>Popup</c> compensates for it; a
    /// <see cref="System.Windows.Controls.ComboBox"/>'s managed popup, a
    /// <see cref="System.Windows.Controls.ContextMenu"/>'s internal popup and the gallery's overlay
    /// popup do not. No single margin/offset pair was correct everywhere, and two rounds of fixes
    /// moved flyouts off their anchors in one direction and then the other (§3.42).
    /// </para>
    /// <para>
    /// Moving the CONTENT instead settles it. The content lives inside the surface's padding, so
    /// its travel never reaches the popup window's edge, and the surface itself keeps the exact
    /// geometry the template always had — no headroom margin, no placement offset, nothing to get
    /// wrong per popup kind. This is what the original code did, for reasons it had not written
    /// down; the only thing actually wrong with it was that the bordered card snapped into
    /// existence around moving items, which the fade fixes.
    /// </para>
    /// <para>
    /// Open only, deliberately — a close transition would mean holding the popup alive past its
    /// close, and <c>ComboBox</c>'s built-in mouse-capture management assumes the popup closes when
    /// it says so.
    /// </para>
    /// </remarks>
    public static void PlayFlyoutOpen(FrameworkElement? surface, RibbonAnimationAction action)
    {
        if (surface is null)
        {
            return;
        }

        FrameworkElement? content = FlyoutContentOf(surface);

        if (!RibbonAnimation.IsEnabled(action))
        {
            Rest(surface);
            Rest(content);
            return;
        }

        Duration duration = RibbonAnimation.GetDuration(action);
        IEasingFunction ease = RibbonAnimation.GetEase(action);

        var fade = new DoubleAnimation(0d, 1d, duration) { EasingFunction = ease };

        // ⚠ The fade must RELEASE itself, and it must be SEEDED. Both halves matter:
        //
        // Seeding — WPF ticks the timing manager at the START of a render frame, so an animation
        // begun during that frame first ticks on the NEXT one, and until then the property falls
        // back to its BASE value. With the base left at 1 the surface is presented fully opaque for
        // one frame and only then fades in from 0, which reads as a FLICKER rather than a fade.
        // (§3.41 found the same rule for the title glide; it applies to opacity just as much as to
        // a transform.)
        //
        // Releasing — a seeded base of 0 is dangerous on its own: anything that drops the animation
        // without calling Rest leaves an INVISIBLE flyout, which is far worse than a flicker. So on
        // completion the base goes back to 1 and the animation is cleared, leaving the surface with
        // no animation and a resting value of 1. Order matters inside the handler: set the base
        // first, THEN clear, or the property momentarily falls back to 0. Same self-releasing shape
        // as PlayKeyTipPop.
        fade.Completed += (_, _) =>
        {
            surface.SetValue(UIElement.OpacityProperty, 1d);
            surface.BeginAnimation(UIElement.OpacityProperty, null);
        };

        surface.SetValue(UIElement.OpacityProperty, 0d);
        surface.BeginAnimation(UIElement.OpacityProperty, fade);

        // Defensive: an earlier revision of this method scaled the surface. If anything ever leaves
        // a transform on it the flyout renders displaced for the rest of the session, and that is
        // exactly the failure this design exists to rule out.
        ClearTransform(surface);

        double offset = RibbonAnimation.GetSlideOffset(action);
        if (content is null || offset <= 0d)
        {
            return;
        }

        TranslateTransform translate = EnsureTranslate(content);
        translate.BeginAnimation(TranslateTransform.XProperty, null);
        translate.SetValue(TranslateTransform.XProperty, 0d);

        // Seed before BeginAnimation: WPF ticks the timing manager at the START of a render frame,
        // so an animation begun during that frame's layout first ticks on the NEXT one and until
        // then the property falls back to its base value. See AnimateTranslateX and §3.41.
        translate.SetValue(TranslateTransform.YProperty, -offset);

        var slide = new DoubleAnimation(-offset, 0d, duration) { EasingFunction = ease };
        translate.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    /// <summary>
    /// The element a flyout's open slide is applied to: the single child sitting inside the
    /// surface's border. A <see cref="System.Windows.Controls.ContextMenu"/> is handled specially
    /// because it IS its popup's child, so its surface is one template level further in.
    /// </summary>
    private static FrameworkElement? FlyoutContentOf(FrameworkElement surface)
    {
        switch (surface)
        {
            case Border border:
                return border.Child as FrameworkElement;

            case Decorator decorator:
                return decorator.Child as FrameworkElement;

            case ContextMenu menu:
                menu.ApplyTemplate();
                return VisualTreeHelper.GetChildrenCount(menu) > 0
                    && VisualTreeHelper.GetChild(menu, 0) is Border root
                    ? root.Child as FrameworkElement
                    : null;

            default:
                return null;
        }
    }

    private static void ClearTransform(FrameworkElement element)
    {
        switch (element.RenderTransform)
        {
            case TranslateTransform translate:
                translate.BeginAnimation(TranslateTransform.XProperty, null);
                translate.BeginAnimation(TranslateTransform.YProperty, null);
                translate.X = 0d;
                translate.Y = 0d;
                break;

            case ScaleTransform scale:
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                scale.ScaleX = 1d;
                scale.ScaleY = 1d;
                break;
        }
    }

    /// <summary>Clears any running transition and returns the element to its resting state.</summary>
    public static void Rest(FrameworkElement? element)
    {
        if (element is null)
        {
            return;
        }

        element.BeginAnimation(UIElement.OpacityProperty, null);
        element.Opacity = 1d;
        ClearTransform(element);
    }

    /// <summary>
    /// Cross-fades a highlight "wash" layer to shown (<paramref name="show"/> = <see langword="true"/>,
    /// opacity 1) or hidden (opacity 0) using the given action's timing. Snaps instantly when the
    /// action is disabled or system reduced-motion is on. Used by the ribbon buttons for their
    /// hover / press / checked highlights — animating only a wash's opacity keeps the theme brush on
    /// the wash and needs no colour math. Template storyboards can't do this because a
    /// <c>DynamicResource</c> duration makes a templated storyboard un-freezable.
    /// </summary>
    public static void FadeWash(FrameworkElement? wash, bool show, RibbonAnimationAction action)
    {
        if (wash is null)
        {
            return;
        }

        double target = show ? 1d : 0d;

        if (!RibbonAnimation.IsEnabled(action))
        {
            wash.BeginAnimation(UIElement.OpacityProperty, null);
            wash.Opacity = target;
            return;
        }

        var fade = new DoubleAnimation(target, RibbonAnimation.GetDuration(action))
        {
            EasingFunction = RibbonAnimation.GetEase(action),
        };
        wash.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    /// <summary>
    /// A subtle "refresh" for a theme or accent change: the element dips to ~85% opacity and
    /// settles back to 100% over the action's timing, softening the swap. Not a full fade (that
    /// would flash an already-opaque element to transparent). Instant when the action is disabled.
    /// </summary>
    public static void PlayThemeCrossfade(FrameworkElement? element, RibbonAnimationAction action)
    {
        if (element is null)
        {
            return;
        }

        if (!RibbonAnimation.IsEnabled(action))
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = 1d;
            return;
        }

        var fade = new DoubleAnimation(0.85d, 1d, RibbonAnimation.GetDuration(action))
        {
            EasingFunction = RibbonAnimation.GetEase(action),
        };
        element.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    /// <summary>
    /// Plays a KeyTip badge's pop-in transition when it first appears: a quick fade
    /// combined with a short downward settle, using the <see cref="RibbonAnimationAction.KeyTip"/>
    /// timing. Unlike <see cref="PlayOpen"/>, the opacity animation releases itself once it
    /// completes (falling back to a plain local value) so the caller can still toggle opacity
    /// afterward with a direct property set — needed because <c>KeyTipAdorner.Dimmed</c> sets
    /// <see cref="UIElement.OpacityProperty"/> directly to dim/undim a badge as the user types,
    /// which an animation still holding the property (its default <c>FillBehavior.HoldEnd</c>)
    /// would silently swallow. Instant when the action is disabled.
    /// </summary>
    public static void PlayKeyTipPop(FrameworkElement? element, RibbonAnimationAction action)
    {
        if (element is null)
        {
            return;
        }

        if (!RibbonAnimation.IsEnabled(action))
        {
            Rest(element);
            return;
        }

        Duration duration = RibbonAnimation.GetDuration(action);
        IEasingFunction ease = RibbonAnimation.GetEase(action);

        var fade = new DoubleAnimation(0d, 1d, duration) { EasingFunction = ease };
        fade.Completed += (_, _) =>
        {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = 1d;
        };
        element.BeginAnimation(UIElement.OpacityProperty, fade);

        double offset = RibbonAnimation.GetSlideOffset(action);
        if (offset <= 0d)
        {
            return;
        }

        TranslateTransform translate = EnsureTranslate(element);
        Slide(translate, TranslateTransform.YProperty, -offset, duration, ease);
    }

    /// <summary>
    /// Attached helper property used to animate a <see cref="ScrollViewer"/>'s vertical scroll
    /// position. <see cref="ScrollViewer.VerticalOffset"/> is read-only and can't be animated
    /// directly, so a <see cref="DoubleAnimation"/> drives THIS property and its changed-callback
    /// forwards each tick to <see cref="ScrollViewer.ScrollToVerticalOffset"/>. Not intended to be
    /// set in XAML — use <see cref="AnimateScrollToVerticalOffset"/>.
    /// </summary>
    public static readonly DependencyProperty AnimatedVerticalOffsetProperty =
        DependencyProperty.RegisterAttached(
            "AnimatedVerticalOffset",
            typeof(double),
            typeof(RibbonMotion),
            new PropertyMetadata(0d, OnAnimatedVerticalOffsetChanged));

    /// <summary>Sets the <see cref="AnimatedVerticalOffsetProperty"/> helper value.</summary>
    public static void SetAnimatedVerticalOffset(DependencyObject element, double value)
        => element.SetValue(AnimatedVerticalOffsetProperty, value);

    /// <summary>Gets the <see cref="AnimatedVerticalOffsetProperty"/> helper value.</summary>
    public static double GetAnimatedVerticalOffset(DependencyObject element)
        => (double)element.GetValue(AnimatedVerticalOffsetProperty);

    private static void OnAnimatedVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }

    /// <summary>
    /// Smoothly scrolls <paramref name="scrollViewer"/> to <paramref name="targetOffset"/> (a
    /// vertical offset in DIPs) using the given action's timing/easing, gliding instead of
    /// jumping. Callers should clamp <paramref name="targetOffset"/> to the valid range. When the
    /// action is disabled (or system reduced-motion is on) it snaps instantly. Starting a new
    /// animation supersedes any in flight, so repeated calls (a held RepeatButton) chain smoothly.
    /// </summary>
    /// <param name="scrollViewer">The scroller to glide; a null reference is a no-op.</param>
    /// <param name="targetOffset">The vertical offset, in DIPs, to land on.</param>
    /// <param name="action">The animation action supplying duration, easing and the disabled check.</param>
    /// <param name="fromOffset">
    /// Explicit start offset for the glide. When omitted the scroller's current
    /// <see cref="ScrollViewer.VerticalOffset"/> is used. Pass this when the visible offset was
    /// just reset but the glide should appear to start somewhere else — e.g. the in-ribbon gallery
    /// strip resuming from where it sat before the popup opened (which was zeroed for hit-testing),
    /// so picking a higher tile slides UP and a lower tile slides DOWN rather than always down.
    /// </param>
    public static void AnimateScrollToVerticalOffset(
        ScrollViewer? scrollViewer,
        double targetOffset,
        RibbonAnimationAction action,
        double? fromOffset = null)
    {
        if (scrollViewer is null)
        {
            return;
        }

        double start = fromOffset ?? scrollViewer.VerticalOffset;

        if (!RibbonAnimation.IsEnabled(action))
        {
            scrollViewer.BeginAnimation(AnimatedVerticalOffsetProperty, null);
            scrollViewer.ScrollToVerticalOffset(targetOffset);
            return;
        }

        if (Math.Abs(targetOffset - start) < 0.5d)
        {
            scrollViewer.BeginAnimation(AnimatedVerticalOffsetProperty, null);
            scrollViewer.ScrollToVerticalOffset(targetOffset);
            return;
        }

        // Pin the visible position to the start before gliding. This is a no-op when start is the
        // scroller's own offset, but when an explicit fromOffset was passed it seats the strip at
        // that row so the very first frame slides FROM there (up or down) instead of from wherever
        // the offset currently sits.
        scrollViewer.ScrollToVerticalOffset(start);

        var anim = new DoubleAnimation(start, targetOffset, RibbonAnimation.GetDuration(action))
        {
            EasingFunction = RibbonAnimation.GetEase(action),
        };
        anim.Completed += (_, _) =>
        {
            // Settle as a plain local value FIRST, then release the animation — otherwise clearing
            // the animation reverts the helper property to its base (0) and fires a spurious
            // scroll-to-top. A later direct ScrollToVerticalOffset (e.g. the gallery zeroing the
            // popup on open) is then free to take over.
            scrollViewer.SetValue(AnimatedVerticalOffsetProperty, targetOffset);
            scrollViewer.BeginAnimation(AnimatedVerticalOffsetProperty, null);
            scrollViewer.ScrollToVerticalOffset(targetOffset);
        };
        scrollViewer.BeginAnimation(AnimatedVerticalOffsetProperty, anim);
    }

    /// <summary>
    /// Cancels any in-flight <see cref="AnimateScrollToVerticalOffset"/> animation on
    /// <paramref name="scrollViewer"/> so a subsequent direct scroll set isn't fought by it.
    /// </summary>
    public static void StopScrollAnimation(ScrollViewer? scrollViewer)
        => scrollViewer?.BeginAnimation(AnimatedVerticalOffsetProperty, null);

    private static void Slide(
        TranslateTransform translate,
        DependencyProperty axis,
        double fromValue,
        Duration duration,
        IEasingFunction ease)
    {
        // Zero out the other axis so a reused transform never leaves a stale offset.
        DependencyProperty other = axis == TranslateTransform.XProperty
            ? TranslateTransform.YProperty
            : TranslateTransform.XProperty;
        translate.BeginAnimation(other, null);
        translate.SetValue(other, 0d);

        // As with opacity, seed before the animation clock starts. Otherwise a render-frame race
        // can expose the base value (the destination) before snapping back to the animated start.
        translate.BeginAnimation(axis, null);
        translate.SetValue(axis, fromValue);

        var slide = new DoubleAnimation(fromValue, 0d, duration) { EasingFunction = ease };
        slide.Completed += (_, _) =>
        {
            translate.SetValue(axis, 0d);
            translate.BeginAnimation(axis, null);
        };
        translate.BeginAnimation(axis, slide);
    }

    private static TranslateTransform EnsureTranslate(FrameworkElement element)
    {
        if (element.RenderTransform is TranslateTransform existing)
        {
            return existing;
        }

        var translate = new TranslateTransform();
        element.RenderTransform = translate;
        return translate;
    }

    private static RotateTransform EnsureRotate(FrameworkElement element)
    {
        if (element.RenderTransform is RotateTransform rotate)
        {
            return rotate;
        }

        if (element.RenderTransform is TransformGroup group)
        {
            foreach (Transform transform in group.Children)
            {
                if (transform is RotateTransform existing)
                {
                    return existing;
                }
            }

            rotate = new RotateTransform();
            group.Children.Add(rotate);
            return rotate;
        }

        Transform current = element.RenderTransform;
        var transforms = new TransformGroup();
        if (current is not null && !ReferenceEquals(current, Transform.Identity))
        {
            transforms.Children.Add(current);
        }

        rotate = new RotateTransform();
        transforms.Children.Add(rotate);
        element.RenderTransform = transforms;
        return rotate;
    }
}
