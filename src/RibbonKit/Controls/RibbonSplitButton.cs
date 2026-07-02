using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RibbonKit.Controls;

/// <summary>
/// A ribbon split button: a primary command part (icon + label) plus a chevron part
/// that opens a dropdown of <see cref="RibbonMenuItem"/>s — like Office's Paste.
/// </summary>
[TemplatePart(Name = PrimaryPartName, Type = typeof(ButtonBase))]
public class RibbonSplitButton : RibbonDropDownButton
{
    private const string PrimaryPartName = "PART_Primary";

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

    private ButtonBase? _primary;

    static RibbonSplitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(typeof(RibbonSplitButton)));
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
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
    }
}
