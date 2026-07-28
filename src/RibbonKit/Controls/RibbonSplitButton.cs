using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
// Alias: WPF's legacy Microsoft ribbon declares identically-named peers in
// System.Windows.Automation.Peers, so the reference must be disambiguated.
using RibbonSplitButtonAutomationPeer = RibbonKit.Automation.RibbonSplitButtonAutomationPeer;

namespace RibbonKit.Controls;

/// <summary>
/// How a <see cref="RibbonSplitButton"/> arranges its two halves.
/// </summary>
public enum RibbonSplitButtonLayout
{
    /// <summary>
    /// Command part on the left, chevron on the right. The default, and the only arrangement
    /// available below <see cref="RibbonControlSize.Large"/>.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Icon on top (the command part), caption and chevron stacked beneath it (the drop-down
    /// part) — Office's large Paste button. Only honoured at
    /// <see cref="RibbonControlSize.Large"/>; the button falls back to
    /// <see cref="Horizontal"/> at every smaller size, which is what the sizing engine reduces
    /// it to as its group narrows.
    /// </summary>
    Vertical,
}

/// <summary>
/// A ribbon split button: a primary command part (icon + label) plus a chevron part
/// that opens a dropdown of <see cref="RibbonMenuItem"/>s — like Office's Paste.
/// </summary>
[TemplatePart(Name = PrimaryPartName, Type = typeof(ButtonBase))]
public class RibbonSplitButton : RibbonDropDownButton
{
    private const string PrimaryPartName = "PART_Primary";
    private const string TogglePartName = "PART_Toggle";

    /// <summary>Identifies the <see cref="Click"/> routed event (primary part clicked).</summary>
    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RibbonSplitButton));

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Layout"/> dependency property.</summary>
    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(
            nameof(Layout),
            typeof(RibbonSplitButtonLayout),
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(
                RibbonSplitButtonLayout.Horizontal,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnLayoutInputChanged));

    private static readonly DependencyPropertyKey IsVerticalLayoutPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsVerticalLayout),
            typeof(bool),
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Identifies the read-only <see cref="IsVerticalLayout"/> dependency property.</summary>
    public static readonly DependencyProperty IsVerticalLayoutProperty =
        IsVerticalLayoutPropertyKey.DependencyProperty;

    private ButtonBase? _primary;
    private ToggleButton? _toggle;

    static RibbonSplitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(typeof(RibbonSplitButton)));

        // Size is declared by RibbonDropDownButton and driven by the sizing engine, so the
        // vertical/horizontal decision has to re-run whenever it changes — a Large button that
        // reduces to Medium must drop back to the horizontal arrangement on the same pass.
        // The base metadata is re-stated here (default Large, AffectsMeasure) because
        // OverrideMetadata REPLACES it rather than merging.
        SizeProperty.OverrideMetadata(
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(
                RibbonControlSize.Large,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnLayoutInputChanged));
    }

    /// <summary>Raised when the primary (command) part is clicked.</summary>
    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>The command executed by the primary part.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The parameter passed to <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>
    /// How the two halves are arranged. <see cref="RibbonSplitButtonLayout.Vertical"/> is only
    /// honoured at <see cref="RibbonControlSize.Large"/> — read <see cref="IsVerticalLayout"/>
    /// for what is actually being rendered.
    /// </summary>
    public RibbonSplitButtonLayout Layout
    {
        get => (RibbonSplitButtonLayout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    /// <summary>
    /// Whether the vertical arrangement is in effect right now — i.e. <see cref="Layout"/> is
    /// <see cref="RibbonSplitButtonLayout.Vertical"/> AND <see cref="RibbonDropDownButton.Size"/>
    /// is <see cref="RibbonControlSize.Large"/>.
    /// </summary>
    /// <remarks>
    /// The template keys every vertical-only difference off this ONE flag rather than re-testing
    /// the pair. That matters because those differences live in three separate namescopes — the
    /// outer template and the nested template of each half — so the alternative was the same
    /// two-condition MultiDataTrigger repeated six times, with six chances to let the two halves
    /// disagree about which way round the button is.
    /// </remarks>
    public bool IsVerticalLayout => (bool)GetValue(IsVerticalLayoutProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_primary is not null)
        {
            _primary.Click -= OnPrimaryClick;
        }

        base.OnApplyTemplate();

        _primary = GetTemplateChild(PrimaryPartName) as ButtonBase;
        if (_primary is not null)
        {
            _primary.Click += OnPrimaryClick;
        }

        _toggle = GetTemplateChild(TogglePartName) as ToggleButton;
    }

    /// <summary>The primary (command) part; the KeyTip service badges it separately.</summary>
    internal ButtonBase? PrimaryPart => _primary;

    /// <summary>The chevron (menu) part; the KeyTip service badges it separately.</summary>
    internal ToggleButton? TogglePart => _toggle;

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonSplitButtonAutomationPeer(this);

    /// <summary>
    /// Performs the primary action on behalf of UI Automation's Invoke pattern:
    /// raises <see cref="Click"/> and executes <see cref="Command"/>.
    /// </summary>
    internal void AutomationInvokePrimary()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }

    private static void OnLayoutInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((RibbonSplitButton)d).UpdateVerticalLayout();

    private void UpdateVerticalLayout() =>
        SetValue(
            IsVerticalLayoutPropertyKey,
            Layout == RibbonSplitButtonLayout.Vertical && Size == RibbonControlSize.Large);

    private void OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }
}
