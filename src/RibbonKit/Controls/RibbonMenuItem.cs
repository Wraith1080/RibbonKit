using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// A menu row inside a <see cref="RibbonDropDownButton"/> or
/// <see cref="RibbonSplitButton"/> dropdown: 16px icon + text with hover highlight.
/// Clicking it raises Click/Command like a normal button and closes the dropdown.
/// Submenus arrive in a later phase.
/// </summary>
public class RibbonMenuItem : Button
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonMenuItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonMenuItem),
            new FrameworkPropertyMetadata(null));

    static RibbonMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonMenuItem),
            new FrameworkPropertyMetadata(typeof(RibbonMenuItem)));
    }

    /// <summary>The menu row's text.</summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The 16px icon shown left of the text.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
