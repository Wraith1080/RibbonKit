using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Controls;

/// <summary>
/// The Office-style rich tooltip shown for ribbon controls: a bold title line and a
/// wrapped description. Created automatically when a control's ScreenTip properties
/// are set; can also be assigned to any ToolTip property directly.
/// </summary>
public class RibbonScreenTip : ToolTip
{
    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(RibbonScreenTip),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(RibbonScreenTip),
            new FrameworkPropertyMetadata(null));

    static RibbonScreenTip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonScreenTip),
            new FrameworkPropertyMetadata(typeof(RibbonScreenTip)));
    }

    /// <summary>Bold first line of the tip.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Wrapped descriptive text under the title.</summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
}
