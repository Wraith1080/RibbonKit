using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonTextBoxAutomationPeer = RibbonKit.Automation.RibbonTextBoxAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A compact ribbon text box with an optional label. All editing, selection,
/// validation, command, keyboard, and IME behavior comes from <see cref="TextBox"/>.
/// </summary>
public class RibbonTextBox : TextBox
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="InputWidth"/> dependency property.</summary>
    public static readonly DependencyProperty InputWidthProperty =
        DependencyProperty.Register(
            nameof(InputWidth),
            typeof(double),
            typeof(RibbonTextBox),
            new FrameworkPropertyMetadata(130d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="ScreenTipTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTitleProperty =
        DependencyProperty.Register(
            nameof(ScreenTipTitle),
            typeof(string),
            typeof(RibbonTextBox),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    /// <summary>Identifies the <see cref="ScreenTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTextProperty =
        DependencyProperty.Register(
            nameof(ScreenTipText),
            typeof(string),
            typeof(RibbonTextBox),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    static RibbonTextBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonTextBox),
            new FrameworkPropertyMetadata(typeof(RibbonTextBox)));
    }

    /// <summary>Optional label text shown to the left of the input box.</summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Width of the input box part (excluding the label). Default 130.</summary>
    public double InputWidth
    {
        get => (double)GetValue(InputWidthProperty);
        set => SetValue(InputWidthProperty, value);
    }

    /// <summary>Bold first line of the ScreenTip.</summary>
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
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTextBoxAutomationPeer(this);

    private static void OnScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBox = (RibbonTextBox)d;
        ScreenTipHelper.Update(textBox, textBox.ScreenTipTitle, textBox.ScreenTipText);
    }
}
