using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace RibbonKit.Controls;

/// <summary>
/// The Office 2013+ style backstage view: a full-window overlay with an accent-colored
/// navigation column (back button + tabs) and a content area. Assign one to
/// <see cref="Ribbon.Backstage"/>; the ribbon's File button opens it.
/// <code language="xaml">
/// &lt;rk:Ribbon.Backstage&gt;
///     &lt;rk:Backstage&gt;
///         &lt;rk:BackstageTabItem Header="Info"&gt; ... &lt;/rk:BackstageTabItem&gt;
///     &lt;/rk:Backstage&gt;
/// &lt;/rk:Ribbon.Backstage&gt;
/// </code>
/// </summary>
[TemplatePart(Name = BackButtonPartName, Type = typeof(ButtonBase))]
public class Backstage : TabControl
{
    private const string BackButtonPartName = "PART_BackButton";

    private ButtonBase? _backButton;

    static Backstage()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(Backstage),
            new FrameworkPropertyMetadata(typeof(Backstage)));

        // The nav column is vertical, like Office.
        TabStripPlacementProperty.OverrideMetadata(
            typeof(Backstage),
            new FrameworkPropertyMetadata(Dock.Left));
    }

    /// <summary>
    /// Raised when the user asks to leave the backstage (back button or Esc).
    /// The hosting <see cref="Ribbon"/> subscribes and closes the overlay.
    /// </summary>
    public event EventHandler? BackRequested;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_backButton is not null)
        {
            _backButton.Click -= OnBackButtonClick;
        }

        base.OnApplyTemplate();

        _backButton = GetTemplateChild(BackButtonPartName) as ButtonBase;
        if (_backButton is not null)
        {
            _backButton.Click += OnBackButtonClick;
        }
    }

    /// <summary>Esc leaves the backstage, matching Office.</summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape)
        {
            RaiseBackRequested();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is BackstageTabItem;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new BackstageTabItem();

    private void OnBackButtonClick(object sender, RoutedEventArgs e) => RaiseBackRequested();

    private void RaiseBackRequested() => BackRequested?.Invoke(this, EventArgs.Empty);
}

/// <summary>A navigation entry (and its page) inside a <see cref="Backstage"/>.</summary>
public class BackstageTabItem : TabItem
{
    static BackstageTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(typeof(BackstageTabItem)));
    }
}
