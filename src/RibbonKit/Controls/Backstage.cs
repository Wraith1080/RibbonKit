using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using RibbonKit.Animation;

namespace RibbonKit.Controls;

/// <summary>Where a <see cref="BackstageTabItem"/> sits in the nav column.</summary>
public enum BackstageItemPlacement
{
    /// <summary>Packed from the TOP of the nav column (the default), in declared order.</summary>
    Top,

    /// <summary>Pinned to the BOTTOM of the nav column (like Word's Account / Options), in declared order.</summary>
    Bottom,
}

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
/// <para>
/// <b>Design-time preview.</b> <see cref="Backstage"/> is a <see cref="TabControl"/>, so its
/// <see cref="Selector.SelectedIndex"/> selects the previewed page. Pair
/// <c>d:IsBackstageOpen="True"</c> on the ribbon (which renders the backstage into the designer
/// via its design-time host) with <c>d:SelectedIndex="2"</c> on the backstage to design a
/// specific page on the XAML surface — no runtime value is set.
/// </para>
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

    private FrameworkElement? _contentArea;

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

    /// <summary>Initializes a new <see cref="Backstage"/>.</summary>
    public Backstage()
    {
        // Focus trap. The backstage is shown as a full-window overlay in the WINDOW'S ADORNER
        // LAYER — a separate visual branch that paints on top of the ribbon but does NOT sit
        // between it and the keyboard-focus tree. Without a trap, Tab walks straight past the
        // overlay into the ribbon/document controls sitting behind it (they're visually covered
        // but still in the tab order), which is the "Tab leaks to the ribbon" bug.
        //
        // Cycle contains Tab (and Shift+Tab) within this element's subtree and wraps at the
        // ends, so once focus is inside the backstage (the host Focus()es it on open) keyboard
        // focus can never escape while it's open — matching Office. When the overlay closes the
        // element leaves the tree, so the setting is inert the rest of the time; safe to apply
        // unconditionally because a Backstage is only ever used as this overlay.
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
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

        _contentArea = GetTemplateChild("ContentArea") as FrameworkElement;

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

    private bool _revertingSelection;

    /// <inheritdoc />
    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        // A button item (Options / Exit …) is an ACTION, never a page: if selection lands on one
        // (e.g. via keyboard), bounce it straight back to the previous page. Actual invocation
        // happens on click/Enter in BackstageTabItem, not here — so arrowing past one does nothing.
        if (!_revertingSelection && SelectedItem is BackstageTabItem { IsButton: true })
        {
            _revertingSelection = true;
            SelectedItem = e.RemovedItems.Count > 0 ? e.RemovedItems[0] : null;
            _revertingSelection = false;
            e.Handled = true;
            return;
        }

        base.OnSelectionChanged(e);

        // Slide the newly-selected page in (no fade — it's freshly shown at full opacity, so a fade
        // would flash it). Skipped while bouncing off a button item.
        if (!_revertingSelection)
        {
            RibbonMotion.PlaySlideIn(_contentArea, RibbonAnimationAction.TabSwitch, RibbonSlideFrom.Bottom);
        }
    }

    private void OnBackButtonClick(object sender, RoutedEventArgs e) => RaiseBackRequested();

    private void RaiseBackRequested() => BackRequested?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// A navigation entry inside a <see cref="Backstage"/>. By default it's a PAGE (its content
/// shows when selected). Set <see cref="IsButton"/> to make it an ACTION instead (Options, Exit
/// …): clicking raises <see cref="Click"/> / runs <see cref="Command"/> and it never becomes the
/// selected page. <see cref="Placement"/> pins it to the top (default) or bottom of the column.
/// </summary>
public class BackstageTabItem : TabItem
{
    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Placement"/> dependency property.</summary>
    public static readonly DependencyProperty PlacementProperty =
        DependencyProperty.Register(
            nameof(Placement),
            typeof(BackstageItemPlacement),
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(
                BackstageItemPlacement.Top,
                FrameworkPropertyMetadataOptions.AffectsParentArrange));

    /// <summary>Identifies the <see cref="IsButton"/> dependency property.</summary>
    public static readonly DependencyProperty IsButtonProperty =
        DependencyProperty.Register(
            nameof(IsButton),
            typeof(bool),
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(false));

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Click"/> routed event.</summary>
    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(BackstageTabItem));

    static BackstageTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BackstageTabItem),
            new FrameworkPropertyMetadata(typeof(BackstageTabItem)));
    }

    /// <summary>Raised when a <see cref="IsButton"/> item is activated (click or Enter/Space).</summary>
    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
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

    /// <summary>Whether this item sits at the top (default) or bottom of the nav column.</summary>
    public BackstageItemPlacement Placement
    {
        get => (BackstageItemPlacement)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <summary>
    /// When <see langword="true"/>, this item is an ACTION rather than a page: clicking it raises
    /// <see cref="Click"/> and runs <see cref="Command"/> without selecting it (no page switch).
    /// </summary>
    public bool IsButton
    {
        get => (bool)GetValue(IsButtonProperty);
        set => SetValue(IsButtonProperty, value);
    }

    /// <summary>Command run when a <see cref="IsButton"/> item is activated.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Parameter passed to <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        // Handling the tunnelling preview marks the input handled, so the bubbling
        // MouseLeftButtonDown that TabItem uses to select this item is suppressed — the click
        // fires the action instead of switching pages.
        if (IsButton && !e.Handled)
        {
            Activate();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsButton && (e.Key == Key.Enter || e.Key == Key.Space))
        {
            Activate();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void Activate()
    {
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }
}

/// <summary>
/// The nav column's items host: packs <see cref="BackstageItemPlacement.Top"/> items from the top
/// and <see cref="BackstageItemPlacement.Bottom"/> items from the bottom (Word's Account / Options
/// footer), with a subtle divider above the footer block. Used as the <c>IsItemsHost</c> panel in
/// the <see cref="Backstage"/> template so all items stay in the one <see cref="TabControl"/>
/// (selection keeps working); only their vertical arrangement differs.
/// </summary>
public class BackstageNavPanel : Panel
{
    /// <summary>Identifies the <see cref="SeparatorBrush"/> dependency property.</summary>
    public static readonly DependencyProperty SeparatorBrushProperty =
        DependencyProperty.Register(
            nameof(SeparatorBrush),
            typeof(Brush),
            typeof(BackstageNavPanel),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    // Gap left between the divider line and the first footer item so the item's hover fill
    // doesn't blend into the line.
    private const double FooterDividerGap = 9;

    private double _dividerY = -1;

    /// <summary>Brush for the 1px divider drawn above the footer block (drawn at low opacity).</summary>
    public Brush? SeparatorBrush
    {
        get => (Brush?)GetValue(SeparatorBrushProperty);
        set => SetValue(SeparatorBrushProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        double childWidth = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width;
        double maxWidth = 0;
        double totalHeight = 0;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(childWidth, double.PositiveInfinity));
            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
            totalHeight += child.DesiredSize.Height;
        }

        return new Size(double.IsInfinity(availableSize.Width) ? maxWidth : availableSize.Width, totalHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        double top = 0;
        double bottom = finalSize.Height;
        var bottomItems = new List<UIElement>();

        foreach (UIElement child in InternalChildren)
        {
            if (child is BackstageTabItem { Placement: BackstageItemPlacement.Bottom })
            {
                bottomItems.Add(child);
            }
            else
            {
                child.Arrange(new Rect(0, top, finalSize.Width, child.DesiredSize.Height));
                top += child.DesiredSize.Height;
            }
        }

        // Bottom items keep declared order: arrange from the bottom up in reverse.
        for (int i = bottomItems.Count - 1; i >= 0; i--)
        {
            UIElement child = bottomItems[i];
            bottom -= child.DesiredSize.Height;
            child.Arrange(new Rect(0, bottom, finalSize.Width, child.DesiredSize.Height));
        }

        _dividerY = bottomItems.Count > 0 ? bottom - FooterDividerGap : -1;
        return finalSize;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (_dividerY > 0 && SeparatorBrush is { } brush)
        {
            var pen = new Pen(brush, 1);
            pen.Freeze();
            double y = Math.Round(_dividerY) + 0.5; // crisp 1px line
            drawingContext.PushOpacity(0.25);
            drawingContext.DrawLine(pen, new Point(12, y), new Point(Math.Max(12, ActualWidth - 12), y));
            drawingContext.Pop();
        }
    }
}
