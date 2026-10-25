using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
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
[TemplatePart(Name = PopupScrollViewerPartName, Type = typeof(ScrollViewer))]
public class RibbonComboBox : ComboBox
{
    private const string PopupRootPartName = "PART_PopupRoot";
    private const string PopupScrollViewerPartName = "PART_PopupScrollViewer";
    private static readonly DependencyProperty DetachedGeneratedContainerProperty = DependencyProperty.RegisterAttached(
        "DetachedGeneratedContainer", typeof(bool), typeof(RibbonComboBox), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(null));

    /// <summary>
    /// Icon used by the combo box's quick-access and custom-group dropdown copies.
    /// The original combo box retains its label and input layout.
    /// </summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

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
        RibbonTextEditMenu.EnsureRegistered();
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
        DropDownGrouping.ApplyDefault(this);
        _popupRoot = GetTemplateChild(PopupRootPartName) as FrameworkElement;
        PopupScrollBarStyle.Apply(GetTemplateChild(PopupScrollViewerPartName) as ScrollViewer);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonComboBoxAutomationPeer(this);

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        // WPF clears the generated style during recycling. Its system ComboBoxItem
        // style then activates ancestor alignment bindings on the detached container.
        // Suppress only that fallback while detached; authored containers/local values
        // and the normal attached item style retain their own behavior.
        if (!ReferenceEquals(element, item) && element is ComboBoxItem container
            && container.ReadLocalValue(OverridesDefaultStyleProperty) == DependencyProperty.UnsetValue)
        {
            container.SetValue(OverridesDefaultStyleProperty, true);
            container.SetValue(DetachedGeneratedContainerProperty, true);
        }
        base.ClearContainerForItemOverride(element, item);
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        if ((bool)element.GetValue(DetachedGeneratedContainerProperty))
        {
            element.ClearValue(OverridesDefaultStyleProperty);
            element.ClearValue(DetachedGeneratedContainerProperty);
        }
        base.PrepareContainerForItemOverride(element, item);
    }

    /// <inheritdoc />
    protected override void OnDropDownOpened(EventArgs e)
    {
        base.OnDropDownOpened(e);

        // Fade + inner-content slide on the configured DropdownMenu timing. Driven from code rather than a
        // template storyboard because the duration comes from RibbonAnimation via DynamicResource,
        // and a templated storyboard referencing one cannot be frozen.
        //
        // ComboBox manages PART_Popup and does not compensate for its child's margin like a
        // plain Popup. Keep the surface fixed and move only its content inside the existing
        // padding; extra headroom/offset compensation broke anchors across popup kinds (§3.42).
        RibbonMotion.PlayFlyoutOpen(_popupRoot, RibbonAnimationAction.DropdownMenu);
    }

    private static void OnScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var comboBox = (RibbonComboBox)d;
        ScreenTipHelper.Update(comboBox, comboBox.ScreenTipTitle, comboBox.ScreenTipText);
    }
}
