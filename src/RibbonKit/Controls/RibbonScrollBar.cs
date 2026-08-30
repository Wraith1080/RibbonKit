using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RibbonKit.Controls;

/// <summary>
/// A theme-aware WPF scrollbar using RibbonKit's Office-generation palettes.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RibbonScrollBar"/> preserves the native <see cref="ScrollBar"/> range,
/// command, keyboard, mouse, right-to-left and UI Automation behavior. RibbonKit replaces
/// only its look through the shared control template and theme tokens.
/// </para>
/// <para>
/// Both <see cref="Orientation.Vertical"/> and <see cref="Orientation.Horizontal"/> are
/// supported. Use it directly wherever a standalone range scrollbar is required; RibbonKit's
/// gallery viewports reuse the same template for their generated scrollbars.
/// </para>
/// </remarks>
public class RibbonScrollBar : ScrollBar
{
    /// <summary>
    /// Identifies the <see cref="ButtonCornerRadius"/> attached dependency property.
    /// </summary>
    public static readonly DependencyProperty ButtonCornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "ButtonCornerRadius",
            typeof(CornerRadius),
            typeof(RibbonScrollBar),
            new FrameworkPropertyMetadata(
                default(CornerRadius),
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Identifies the <see cref="ThumbCornerRadius"/> attached dependency property.
    /// </summary>
    public static readonly DependencyProperty ThumbCornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "ThumbCornerRadius",
            typeof(CornerRadius),
            typeof(RibbonScrollBar),
            new FrameworkPropertyMetadata(
                default(CornerRadius),
                FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// Identifies the <see cref="RailCornerRadius"/> attached dependency property.
    /// </summary>
    public static readonly DependencyProperty RailCornerRadiusProperty =
        DependencyProperty.RegisterAttached(
            "RailCornerRadius",
            typeof(CornerRadius),
            typeof(RibbonScrollBar),
            new FrameworkPropertyMetadata(
                default(CornerRadius),
                FrameworkPropertyMetadataOptions.AffectsRender));

    static RibbonScrollBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonScrollBar),
            new FrameworkPropertyMetadata(typeof(RibbonScrollBar)));
    }

    /// <summary>
    /// Gets or sets the corner radius applied to the line-scroll arrow buttons.
    /// </summary>
    public CornerRadius ButtonCornerRadius
    {
        get => GetButtonCornerRadius(this);
        set => SetButtonCornerRadius(this, value);
    }

    /// <summary>
    /// Gets or sets the corner radius applied to the draggable scrollbar thumb.
    /// </summary>
    public CornerRadius ThumbCornerRadius
    {
        get => GetThumbCornerRadius(this);
        set => SetThumbCornerRadius(this, value);
    }

    /// <summary>
    /// Gets or sets the corner radius applied to the scrollbar rail.
    /// </summary>
    public CornerRadius RailCornerRadius
    {
        get => GetRailCornerRadius(this);
        set => SetRailCornerRadius(this, value);
    }

    /// <summary>Gets the line-scroll button corner radius from an element.</summary>
    /// <param name="element">The element carrying the attached value.</param>
    /// <returns>The configured corner radius.</returns>
    public static CornerRadius GetButtonCornerRadius(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (CornerRadius)element.GetValue(ButtonCornerRadiusProperty);
    }

    /// <summary>Sets the line-scroll button corner radius on an element.</summary>
    /// <param name="element">The element that will carry the attached value.</param>
    /// <param name="value">The corner radius to apply.</param>
    public static void SetButtonCornerRadius(DependencyObject element, CornerRadius value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ButtonCornerRadiusProperty, value);
    }

    /// <summary>Gets the draggable thumb corner radius from an element.</summary>
    /// <param name="element">The element carrying the attached value.</param>
    /// <returns>The configured corner radius.</returns>
    public static CornerRadius GetThumbCornerRadius(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (CornerRadius)element.GetValue(ThumbCornerRadiusProperty);
    }

    /// <summary>Sets the draggable thumb corner radius on an element.</summary>
    /// <param name="element">The element that will carry the attached value.</param>
    /// <param name="value">The corner radius to apply.</param>
    public static void SetThumbCornerRadius(DependencyObject element, CornerRadius value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ThumbCornerRadiusProperty, value);
    }

    /// <summary>Gets the scrollbar rail corner radius from an element.</summary>
    /// <param name="element">The element carrying the attached value.</param>
    /// <returns>The configured corner radius.</returns>
    public static CornerRadius GetRailCornerRadius(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (CornerRadius)element.GetValue(RailCornerRadiusProperty);
    }

    /// <summary>Sets the scrollbar rail corner radius on an element.</summary>
    /// <param name="element">The element that will carry the attached value.</param>
    /// <param name="value">The corner radius to apply.</param>
    public static void SetRailCornerRadius(DependencyObject element, CornerRadius value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(RailCornerRadiusProperty, value);
    }
}
