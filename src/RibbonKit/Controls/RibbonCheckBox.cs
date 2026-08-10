using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using RibbonKit.Animation;
using RibbonCheckBoxAutomationPeer = RibbonKit.Automation.RibbonCheckBoxAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A compact ribbon check box for independent on/off options such as View tab settings.
/// It uses the active RibbonKit theme while retaining the standard WPF
/// <see cref="CheckBox"/> command, three-state, keyboard, and routed-event behavior.
/// </summary>
public class RibbonCheckBox : CheckBox
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonCheckBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="ScreenTipTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTitleProperty =
        DependencyProperty.Register(
            nameof(ScreenTipTitle),
            typeof(string),
            typeof(RibbonCheckBox),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    /// <summary>Identifies the <see cref="ScreenTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTextProperty =
        DependencyProperty.Register(
            nameof(ScreenTipText),
            typeof(string),
            typeof(RibbonCheckBox),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    private FrameworkElement? _hoverWash;
    private FrameworkElement? _pressWash;

    static RibbonCheckBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonCheckBox),
            new FrameworkPropertyMetadata(typeof(RibbonCheckBox)));
    }

    /// <summary>The option label shown beside the check indicator.</summary>
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
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonCheckBoxAutomationPeer(this);

    /// <summary>Activates through the native CheckBox click/toggle/command path.</summary>
    internal void InvokeFromKeyTip() => OnClick();

    private void UpdateWashes()
    {
        RibbonMotion.FadeWash(_hoverWash, IsMouseOver, RibbonAnimationAction.Hover);
        RibbonMotion.FadeWash(_pressWash, IsPressed, RibbonAnimationAction.Hover);
    }

    private static void OnScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var checkBox = (RibbonCheckBox)d;
        ScreenTipHelper.Update(checkBox, checkBox.ScreenTipTitle, checkBox.ScreenTipText);
    }
}
