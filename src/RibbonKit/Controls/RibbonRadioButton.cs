using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using RibbonKit.Animation;
using RibbonRadioButtonAutomationPeer = RibbonKit.Automation.RibbonRadioButtonAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A compact ribbon radio button for choosing one option from a mutually exclusive group.
/// It uses the active RibbonKit theme while retaining the standard WPF
/// <see cref="RadioButton"/> grouping, command, keyboard, and routed-event behavior.
/// </summary>
public class RibbonRadioButton : RadioButton
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonRadioButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="ScreenTipTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTitleProperty =
        DependencyProperty.Register(
            nameof(ScreenTipTitle),
            typeof(string),
            typeof(RibbonRadioButton),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    /// <summary>Identifies the <see cref="ScreenTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTextProperty =
        DependencyProperty.Register(
            nameof(ScreenTipText),
            typeof(string),
            typeof(RibbonRadioButton),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    private FrameworkElement? _hoverWash;
    private FrameworkElement? _pressWash;

    static RibbonRadioButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonRadioButton),
            new FrameworkPropertyMetadata(typeof(RibbonRadioButton)));
    }

    /// <summary>The option label shown beside the radio indicator.</summary>
    /// <remarks>
    /// When unset, the template falls back to the inherited <see cref="ContentControl.Content"/>
    /// property so ordinary WPF content syntax remains usable.
    /// </remarks>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
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
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _hoverWash = GetTemplateChild("HoverWash") as FrameworkElement;
        _pressWash = GetTemplateChild("PressWash") as FrameworkElement;
        UpdateWashes();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IsMouseOverProperty || e.Property == IsPressedProperty)
        {
            UpdateWashes();
        }
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonRadioButtonAutomationPeer(this);

    /// <summary>Activates through the native RadioButton click/selection/command path.</summary>
    internal void InvokeFromKeyTip() => OnClick();

    private void UpdateWashes()
    {
        RibbonMotion.FadeWash(_hoverWash, IsMouseOver, RibbonAnimationAction.Hover);
        RibbonMotion.FadeWash(_pressWash, IsPressed, RibbonAnimationAction.Hover);
    }

    private static void OnScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var radioButton = (RibbonRadioButton)d;
        ScreenTipHelper.Update(radioButton, radioButton.ScreenTipTitle, radioButton.ScreenTipText);
    }
}
