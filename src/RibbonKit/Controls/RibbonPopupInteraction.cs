using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Shared templates use effective hover so a QAT flyout can pause ribbon paint
// without disabling hit testing, commands, focus or the popup's own interaction.
internal static class RibbonPopupInteraction
{
    public static readonly DependencyProperty SuppressHoverProperty = DependencyProperty.RegisterAttached(
        "SuppressHover", typeof(bool), typeof(RibbonPopupInteraction),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits,
            (d, _) => RefreshHover(d)));

    public static readonly DependencyProperty IsHoveredProperty = DependencyProperty.RegisterAttached(
        "IsHovered", typeof(bool), typeof(RibbonPopupInteraction),
        new FrameworkPropertyMetadata(false));

    internal static readonly DependencyProperty QuickAccessOwnerProperty = DependencyProperty.RegisterAttached(
        "QuickAccessOwner", typeof(Ribbon), typeof(RibbonPopupInteraction),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    internal static readonly DependencyProperty IsQuickAccessHostProperty = DependencyProperty.RegisterAttached(
        "IsQuickAccessHost", typeof(bool), typeof(RibbonPopupInteraction), new PropertyMetadata(false));

    static RibbonPopupInteraction()
    {
        EventManager.RegisterClassHandler(typeof(UIElement), Mouse.MouseEnterEvent,
            new MouseEventHandler(OnHoverChanged), true);
        EventManager.RegisterClassHandler(typeof(UIElement), Mouse.MouseLeaveEvent,
            new MouseEventHandler(OnHoverChanged), true);
        EventManager.RegisterClassHandler(typeof(FrameworkElement), ToolTipService.ToolTipOpeningEvent,
            new ToolTipEventHandler((sender, e) => { if (e.OriginalSource is DependencyObject source && IsHoverSuppressed(source)) e.Handled = true; }));
    }

    public static bool GetSuppressHover(DependencyObject element) => (bool)element.GetValue(SuppressHoverProperty);
    public static void SetSuppressHover(DependencyObject element, bool value) => element.SetValue(SuppressHoverProperty, value);
    public static bool GetIsHovered(DependencyObject element) => (bool)element.GetValue(IsHoveredProperty);
    public static void SetIsHovered(DependencyObject element, bool value) => element.SetValue(IsHoveredProperty, value);

    private static void OnHoverChanged(object sender, MouseEventArgs e) =>
        RefreshHover((DependencyObject)sender);

    private static void RefreshHover(DependencyObject element)
    {
        if (element is UIElement surface)
            element.SetCurrentValue(IsHoveredProperty, (bool)surface.GetValue(UIElement.IsMouseOverProperty) && !IsHoverSuppressed(element));
    }

    internal static bool IsHoverSuppressed(DependencyObject element)
    {
        // Borrowed items keep their logical group/gallery parent. Their actual
        // visual location determines whether they are in the ribbon or a popup.
        for (DependencyObject? node = element; node is Visual; node = VisualTreeHelper.GetParent(node))
        {
            if (node is RibbonQuickAccessToolBar || (bool)node.GetValue(IsQuickAccessHostProperty)) return false;
            if (node is Ribbon ribbon) return GetSuppressHover(ribbon);
        }
        return false;
    }

    internal static void BeginBorrowedContent(FrameworkElement source, FrameworkElement host)
    {
        // Items retain their source's logical parent even while their visuals move.
        source.SetValue(QuickAccessOwnerProperty, host.GetValue(QuickAccessOwnerProperty));
    }

    internal static void EndBorrowedContent(FrameworkElement source)
    {
        source.ClearValue(QuickAccessOwnerProperty);
    }
}
