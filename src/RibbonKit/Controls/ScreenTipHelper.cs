using System.Windows;

namespace RibbonKit.Controls;

/// <summary>
/// Shared plumbing for controls exposing ScreenTipTitle/ScreenTipText properties.
/// </summary>
internal static class ScreenTipHelper
{
    /// <summary>
    /// Creates/updates the element's <see cref="RibbonScreenTip"/> tooltip, or clears
    /// it when both parts are null.
    /// </summary>
    public static void Update(FrameworkElement element, string? title, string? text)
    {
        if (title is null && text is null)
        {
            element.ClearValue(FrameworkElement.ToolTipProperty);
            return;
        }

        element.ToolTip = new RibbonScreenTip
        {
            Title = title,
            Description = text,
        };
    }
}
