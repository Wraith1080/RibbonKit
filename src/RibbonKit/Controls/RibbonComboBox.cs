using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using RibbonKit.Animation;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonComboBoxAutomationPeer = RibbonKit.Automation.RibbonComboBoxAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// A ribbon combo box: an optional label followed by a compact selection box —
/// the Office font-family/font-size pattern. Supports everything
/// <see cref="ComboBox"/> does, including <see cref="ComboBox.IsEditable"/>.
/// </summary>
/// <remarks>
/// The dropdown relies on <see cref="ComboBox"/>'s own built-in mouse-capture
/// management, which correctly handles open/close on the chevron.
/// </remarks>
[TemplatePart(Name = PopupRootPartName, Type = typeof(FrameworkElement))]
public class RibbonComboBox : ComboBox
{
    private const string PopupRootPartName = "PART_PopupRoot";

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="InputWidth"/> dependency property.</summary>
    public static readonly DependencyProperty InputWidthProperty =
        DependencyProperty.Register(
            nameof(InputWidth),
            typeof(double),
            typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(130d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the <see cref="ScreenTipTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTitleProperty =
        DependencyProperty.Register(
            nameof(ScreenTipTitle),
            typeof(string),
            typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    /// <summary>Identifies the <see cref="ScreenTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTextProperty =
        DependencyProperty.Register(
            nameof(ScreenTipText),
            typeof(string),
            typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(null, OnScreenTipChanged));

    private FrameworkElement? _popupRoot;

    static RibbonComboBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(typeof(RibbonComboBox)));
    }

    /// <summary>Optional label text shown to the left of the selection box.</summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Width of the selection box part (excluding the label). Default 130.</summary>
    public double InputWidth
    {
        get => (double)GetValue(InputWidthProperty);
        set => SetValue(InputWidthProperty, value);
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
        _popupRoot = GetTemplateChild(PopupRootPartName) as FrameworkElement;
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonComboBoxAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnDropDownOpened(EventArgs e)
    {
        base.OnDropDownOpened(e);

        // Fade + short slide down on the DropdownMenu timing (130ms, 8px). That action was
        // declared for exactly this and had NO consumers until now — RibbonDropDownButton,
        // RibbonSplitButton and RibbonMenuItem still open instantly and can be wired up the same
        // way. Driven from code rather than a template storyboard because the duration comes from
        // RibbonAnimation via DynamicResource, and a templated storyboard referencing one cannot
        // be frozen.
        //
        // Open only, deliberately: closing would have to hold the popup alive for the length of
        // the fade-out, and ComboBox's built-in mouse-capture handling (see the class remarks)
        // assumes the popup closes when it says so. Not worth destabilising for an exit frame.
        RibbonMotion.PlayOpen(_popupRoot, RibbonAnimationAction.DropdownMenu, RibbonSlideFrom.Top);
    }

    private static void OnScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var comboBox = (RibbonComboBox)d;
        ScreenTipHelper.Update(comboBox, comboBox.ScreenTipTitle, comboBox.ScreenTipText);
    }
}
