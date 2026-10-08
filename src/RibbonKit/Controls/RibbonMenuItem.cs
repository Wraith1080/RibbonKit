using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonMenuItemAutomationPeer = RibbonKit.Automation.RibbonMenuItemAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A menu row inside a <see cref="RibbonDropDownButton"/> or
/// <see cref="RibbonSplitButton"/> dropdown: an icon and label with hover highlight,
/// optionally a larger icon and wrapped description.
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

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon), typeof(ImageSource), typeof(RibbonMenuItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(RibbonMenuItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

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

    /// <summary>
    /// Optional 32-DIP icon for a descriptive menu row; it takes precedence over <see cref="Icon"/>.
    /// Touch density uses the theme's large-icon metric. Null retains the compact 16-DIP layout.
    /// </summary>
    public ImageSource? LargeIcon
    {
        get => (ImageSource?)GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>
    /// Optional wrapped text below the emphasized <see cref="Header"/>. Null or empty retains
    /// the ordinary single-line row. Also supplies the default UI Automation help text.
    /// </summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMenuItemAutomationPeer(this);
}
