using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

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

    /// <summary>
    /// Identifies the <see cref="Design"/> attached dependency property. Registered as
    /// inheriting so the value set on a <see cref="Backstage"/> flows to its
    /// <see cref="BackstageTabItem"/>s, letting both restyle from a single setting.
    /// </summary>
    public static readonly DependencyProperty DesignProperty =
        DependencyProperty.RegisterAttached(
            "Design",
            typeof(RibbonBackstageDesign),
            typeof(Backstage),
            new FrameworkPropertyMetadata(
                RibbonBackstageDesign.Classic,
                FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Sets the backstage <see cref="Design"/> for an element (and its subtree).</summary>
    public static void SetDesign(DependencyObject element, RibbonBackstageDesign value) =>
        element.SetValue(DesignProperty, value);

    /// <summary>Gets the backstage <see cref="Design"/> for an element.</summary>
    public static RibbonBackstageDesign GetDesign(DependencyObject element) =>
        (RibbonBackstageDesign)element.GetValue(DesignProperty);

    /// <summary>Identifies the <see cref="Translucent"/> dependency property.</summary>
    public static readonly DependencyProperty TranslucentProperty =
        DependencyProperty.Register(
            nameof(Translucent),
            typeof(bool),
            typeof(Backstage),
            new FrameworkPropertyMetadata(false));

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
    /// The backstage chrome design: <see cref="RibbonBackstageDesign.Classic"/> (the
    /// accent-colored 2013 column, default) or <see cref="RibbonBackstageDesign.Modern"/>
    /// (the light 2024 rail). Inherited by the nav items.
    /// </summary>
    public RibbonBackstageDesign Design
    {
        get => GetDesign(this);
        set => SetDesign(this, value);
    }

    /// <summary>
    /// When <see langword="true"/>, the backstage renders its background and (modern) nav rail
    /// semi-transparent so a window system backdrop such as Mica shows through. Requires the
    /// host window and the content behind the backstage to be transparent as well. Default
    /// <see langword="false"/>.
    /// </summary>
    public bool Translucent
    {
        get => (bool)GetValue(TranslucentProperty);
        set => SetValue(TranslucentProperty, value);
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
    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(null));

    static BackstageTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(typeof(BackstageTabItem)));
    }

    /// <summary>
    /// Optional glyph shown to the left of the header. Rendered as a silhouette tinted to
    /// the item's foreground (so it follows the design and the selection accent). The icon
    /// column is always reserved, so items without an icon stay aligned with those that have one.
    /// </summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
