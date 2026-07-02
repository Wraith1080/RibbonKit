using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Layout;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonButtonAutomationPeer = RibbonKit.Automation.RibbonButtonAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A ribbon push button that renders in three sizes: Large (32px icon, label below),
/// Medium (16px icon, label right), and Small (16px icon only).
/// </summary>
/// <remarks>
/// Set <see cref="Size"/> for a fixed size. To let the adaptive sizing engine resize
/// the button as its group shrinks, set <see cref="SizeDefinition"/> (for example
/// <c>"Large, Medium, Small"</c>); the engine then overwrites <see cref="Size"/> as
/// the group's state changes. Buttons without a SizeDefinition keep their declared size.
/// </remarks>
public class RibbonButton : Button, IRibbonSizeAware
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(ImageSource),
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(
                RibbonControlSize.Large,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(string),
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="ScreenTipTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTitleProperty =
        DependencyProperty.Register(
            nameof(ScreenTipTitle),
            typeof(string),
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    /// <summary>Identifies the <see cref="ScreenTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTextProperty =
        DependencyProperty.Register(
            nameof(ScreenTipText),
            typeof(string),
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    static RibbonButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonButton),
            new FrameworkPropertyMetadata(typeof(RibbonButton)));
    }

    /// <summary>The button's label text.</summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The 16px icon used by the Medium and Small layouts.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>The 32px icon used by the Large layout. Falls back to <see cref="Icon"/> when unset.</summary>
    public ImageSource? LargeIcon
    {
        get => (ImageSource?)GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>The size the button currently renders at.</summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Comma-separated sizes for the group states Large, Medium, Small — e.g.
    /// <c>"Large, Medium, Small"</c>. When set, the sizing engine drives <see cref="Size"/>.
    /// </summary>
    public string? SizeDefinition
    {
        get => (string?)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Bold first line of the ScreenTip (rich tooltip).</summary>
    public string? ScreenTipTitle
    {
        get => (string?)GetValue(ScreenTipTitleProperty);
        set => SetValue(ScreenTipTitleProperty, value);
    }

    /// <summary>Descriptive body of the ScreenTip.</summary>
    public string? ScreenTipText
    {
        get => (string?)GetValue(ScreenTipTextProperty);
        set => SetValue(ScreenTipTextProperty, value);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonButtonAutomationPeer(this);

    void IRibbonSizeAware.ApplySizeState(RibbonGroupSizeState state) => ApplySizeState(state);

    internal void ApplySizeState(RibbonGroupSizeState state)
    {
        string? definition = SizeDefinition;
        if (string.IsNullOrWhiteSpace(definition))
        {
            return; // Fixed-size button: not managed by the engine.
        }

        // Inside a collapsed group's flyout, controls render at their full Large layout.
        RibbonGroupSizeState effectiveState =
            state == RibbonGroupSizeState.Collapsed ? RibbonGroupSizeState.Large : state;

        try
        {
            Size = RibbonSizeDefinition.SizeFor(definition, effectiveState);
        }
        catch (ArgumentException)
        {
            // Invalid definitions are ignored during layout; validation surfaces
            // through unit tests and design-time usage rather than layout crashes.
        }
    }

    private static void OnScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var button = (RibbonButton)d;
        ScreenTipHelper.Update(button, button.ScreenTipTitle, button.ScreenTipText);
    }
}
