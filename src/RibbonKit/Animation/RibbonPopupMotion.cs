using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RibbonKit.Animation;

/// <summary>
/// Attached behaviour that plays <see cref="RibbonMotion.PlayOpen"/> on a flyout's
/// bordered surface as it opens, for the flyouts whose opener is not a RibbonKit control
/// with an <c>Opened</c> override to hook — the stock <see cref="ContextMenu"/> and the
/// <see cref="MenuItem"/> submenu <see cref="Popup"/> inside <c>Menus.xaml</c>.
/// </summary>
/// <remarks>
/// <para>
/// Set <see cref="AnimateOpenProperty"/> on either a <see cref="Popup"/> (its
/// <see cref="Popup.Child"/> is animated) or a <see cref="ContextMenu"/> (the menu itself is
/// animated — it <em>is</em> the hosting popup's child). Anything else is ignored.
/// </para>
/// <para>
/// The transition itself is <see cref="RibbonMotion.PlayFlyoutOpen"/>, which scales rather than
/// slides precisely so that attaching this behaviour needs NO template geometry changes at all —
/// no headroom margin, no placement offset. See its remarks and 04-DESIGN-NOTES §3.42.
/// </para>
/// </remarks>
public static class RibbonPopupMotion
{
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
        // never exposes it), so the menu itself is the surface to move. Its DesiredSize
        // includes the template border's headroom margin, which is what keeps the slide from
        // being clipped.
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
}
