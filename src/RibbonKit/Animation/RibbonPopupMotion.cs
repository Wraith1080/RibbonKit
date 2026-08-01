using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace RibbonKit.Animation;

/// <summary>
/// Attached behaviour that plays <see cref="RibbonMotion.PlayFlyoutOpen"/> on a flyout as it
/// opens, for the flyouts whose opener is not a RibbonKit control with an <c>Opened</c>
/// override to hook — the stock <see cref="ContextMenu"/> and the <see cref="MenuItem"/>
/// submenu <see cref="Popup"/> inside <c>Menus.xaml</c>.
/// </summary>
/// <remarks>
/// <para>
/// Set <see cref="AnimateOpenProperty"/> on either a <see cref="Popup"/> (its
/// <see cref="Popup.Child"/> becomes the surface) or a <see cref="ContextMenu"/> (the menu itself
/// is the surface — it <em>is</em> the hosting popup's child). Anything else is ignored.
/// </para>
/// <para>
/// The transition fades the surface and slides its CONTENT, never the surface itself, precisely so
/// that attaching this behaviour needs NO template geometry changes — no headroom margin, no
/// placement offset. See <see cref="RibbonMotion.PlayFlyoutOpen"/>'s remarks and
/// 04-DESIGN-NOTES §3.42.
/// </para>
/// </remarks>
public static class RibbonPopupMotion
{
    private static readonly ConditionalWeakTable<ResourceDictionary, NativeMenuAnimationState>
        NativeMenuAnimationStates = new();

    /// <summary>
    /// Identifies the AnimateOpen attached property: whether the flyout's surface plays an
    /// open transition. Default <see langword="false"/>.
    /// </summary>
    public static readonly DependencyProperty AnimateOpenProperty =
        DependencyProperty.RegisterAttached(
            "AnimateOpen",
            typeof(bool),
            typeof(RibbonPopupMotion),
            new PropertyMetadata(false, OnAnimateOpenChanged));

    /// <summary>
    /// Identifies the OpenAction attached property: which <see cref="RibbonAnimationAction"/>
    /// supplies the timing. Default <see cref="RibbonAnimationAction.DropdownMenu"/>.
    /// </summary>
    public static readonly DependencyProperty OpenActionProperty =
        DependencyProperty.RegisterAttached(
            "OpenAction",
            typeof(RibbonAnimationAction),
            typeof(RibbonPopupMotion),
            new PropertyMetadata(RibbonAnimationAction.DropdownMenu));

    /// <summary>Gets whether the flyout's surface plays an open transition.</summary>
    public static bool GetAnimateOpen(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(AnimateOpenProperty);
    }

    /// <summary>Sets whether the flyout's surface plays an open transition.</summary>
    public static void SetAnimateOpen(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(AnimateOpenProperty, value);
    }

    /// <summary>Gets the action whose timing drives the open transition.</summary>
    public static RibbonAnimationAction GetOpenAction(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (RibbonAnimationAction)element.GetValue(OpenActionProperty);
    }

    /// <summary>Sets the action whose timing drives the open transition.</summary>
    public static void SetOpenAction(DependencyObject element, RibbonAnimationAction value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(OpenActionProperty, value);
    }

    /// <summary>
    /// Temporarily disables WPF's native animation for a RibbonKit-owned <see cref="ContextMenu"/>.
    /// Call during the placement target's <c>ContextMenuOpening</c> event, before WPF creates the
    /// menu's private parent <see cref="Popup"/>. RibbonKit's attached transition remains the sole
    /// entrance and therefore follows <see cref="RibbonAnimation.GlobalLevel"/> and reduced motion.
    /// </summary>
    internal static void SuppressNativeContextMenuAnimationForOpen(ContextMenu menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        if (Application.Current is { } application)
        {
            SuppressNativeContextMenuAnimationForOpen(menu, application.Resources);
        }
    }

    /// <summary>Resource-dictionary overload used by the headless regression tests.</summary>
    internal static void SuppressNativeContextMenuAnimationForOpen(
        ContextMenu menu,
        ResourceDictionary applicationResources)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(applicationResources);

        NativeMenuAnimationState state = NativeMenuAnimationStates.GetOrCreateValue(applicationResources);
        if (state.CloseHandlers.ContainsKey(menu))
        {
            return;
        }

        if (state.CloseHandlers.Count == 0)
        {
            state.HadLocalValue = applicationResources.Contains(SystemParameters.MenuPopupAnimationKey);
            state.PreviousValue = state.HadLocalValue
                ? applicationResources[SystemParameters.MenuPopupAnimationKey]
                : null;
            applicationResources[SystemParameters.MenuPopupAnimationKey] = PopupAnimation.None;
        }

        RoutedEventHandler? closeHandler = null;
        closeHandler = (_, _) => EndNativeContextMenuSuppression(menu, applicationResources);
        state.CloseHandlers.Add(menu, closeHandler);
        menu.Closed += closeHandler;

        // ContextMenuOpening can still be cancelled by another handler. Do not leave the host
        // application's resource overridden if WPF never actually opened this menu.
        menu.Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            (Action)(() =>
            {
                if (!menu.IsOpen)
                {
                    EndNativeContextMenuSuppression(menu, applicationResources);
                }
            }));
    }

    private static void EndNativeContextMenuSuppression(
        ContextMenu menu,
        ResourceDictionary applicationResources)
    {
        if (!NativeMenuAnimationStates.TryGetValue(applicationResources, out NativeMenuAnimationState? state)
            || !state.CloseHandlers.Remove(menu, out RoutedEventHandler? closeHandler))
        {
            return;
        }

        menu.Closed -= closeHandler;
        if (state.CloseHandlers.Count != 0)
        {
            return;
        }

        if (state.HadLocalValue)
        {
            applicationResources[SystemParameters.MenuPopupAnimationKey] = state.PreviousValue!;
        }
        else
        {
            applicationResources.Remove(SystemParameters.MenuPopupAnimationKey);
        }

        state.PreviousValue = null;
    }

    private static void OnAnimateOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        bool enable = e.NewValue is true;

        switch (d)
        {
            case ContextMenu menu:
                // Unsubscribe first in every case: a template can be applied more than once
                // (theme switch), and a duplicate handler would start the animation twice.
                menu.Opened -= OnContextMenuOpened;
                if (enable)
                {
                    menu.Opened += OnContextMenuOpened;
                }

                break;

            case Popup popup:
                popup.Opened -= OnPopupOpened;
                if (enable)
                {
                    popup.Opened += OnPopupOpened;
                }

                break;
        }
    }

    private static void OnContextMenuOpened(object sender, RoutedEventArgs e)
    {
        // The ContextMenu IS the hosting popup's child (WPF builds that popup internally and
        // never exposes it), so the menu itself is the surface: it fades, and PlayFlyoutOpen digs
        // one template level in to find the content that slides.
        var menu = (ContextMenu)sender;
        RibbonMotion.PlayFlyoutOpen(menu, GetOpenAction(menu));
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is Popup { Child: FrameworkElement child } popup)
        {
            RibbonMotion.PlayFlyoutOpen(child, GetOpenAction(popup));
        }
    }

    private sealed class NativeMenuAnimationState
    {
        internal Dictionary<ContextMenu, RoutedEventHandler> CloseHandlers { get; } = new();

        internal bool HadLocalValue { get; set; }

        internal object? PreviousValue { get; set; }
    }
}
