using System.Windows;

namespace RibbonKit.Controls;

/// <summary>
/// A window with Office-style chrome: a custom title bar hosting the window title,
/// optional <see cref="TitleBarContent"/> (quick access buttons live well there),
/// and themed caption buttons — while keeping native behaviors (drag, double-click
/// maximize, resize borders, system menu) via <see cref="System.Windows.Shell.WindowChrome"/>.
/// <code language="xaml">
/// &lt;rk:RibbonWindow ...&gt;
///     &lt;rk:RibbonWindow.TitleBarContent&gt;
///         &lt;StackPanel Orientation="Horizontal"&gt; ...quick access buttons... &lt;/StackPanel&gt;
///     &lt;/rk:RibbonWindow.TitleBarContent&gt;
///     ...
/// &lt;/rk:RibbonWindow&gt;
/// </code>
/// </summary>
public class RibbonWindow : Window
{
    /// <summary>Identifies the <see cref="TitleBarContent"/> dependency property.</summary>
    public static readonly DependencyProperty TitleBarContentProperty =
        DependencyProperty.Register(
            nameof(TitleBarContent),
            typeof(object),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsTitleBarContentVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsTitleBarContentVisibleProperty =
        DependencyProperty.Register(
            nameof(IsTitleBarContentVisible),
            typeof(bool),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(true));

    static RibbonWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(typeof(RibbonWindow)));
    }

    /// <summary>Initializes the window and wires the caption button commands.</summary>
    public RibbonWindow()
    {
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.MinimizeWindowCommand,
            (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.MaximizeWindowCommand,
            (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.RestoreWindowCommand,
            (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.CloseWindowCommand,
            (_, _) => SystemCommands.CloseWindow(this)));
    }

    /// <summary>
    /// Content shown in the title bar between the window edge and the centered title —
    /// the natural home for quick access buttons.
    /// </summary>
    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    /// <summary>
    /// Whether <see cref="TitleBarContent"/> is currently shown. The hosted
    /// <see cref="Ribbon"/> sets this false while its backstage is open, matching
    /// Office.
    /// </summary>
    public bool IsTitleBarContentVisible
    {
        get => (bool)GetValue(IsTitleBarContentVisibleProperty);
        set => SetValue(IsTitleBarContentVisibleProperty, value);
    }
}
