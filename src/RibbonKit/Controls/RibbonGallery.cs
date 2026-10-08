using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// Event data for gallery live-preview events.
/// </summary>
public class RibbonGalleryPreviewEventArgs : RoutedEventArgs
{
    /// <summary>Creates the event data.</summary>
    public RibbonGalleryPreviewEventArgs(RoutedEvent routedEvent, object source, object? previewedItem)
        : base(routedEvent, source)
    {
        PreviewedItem = previewedItem;
    }

    /// <summary>The item currently under the mouse (a <see cref="RibbonGalleryItem"/> for direct XAML items).</summary>
    public object? PreviewedItem { get; }
}

/// <summary>Handler for gallery live-preview events.</summary>
public delegate void RibbonGalleryPreviewEventHandler(object sender, RibbonGalleryPreviewEventArgs e);

/// <summary>
/// A gallery of selectable tiles with Office-style live preview: hovering an item
/// raises <see cref="ItemPreview"/>, leaving the gallery raises
/// <see cref="ItemPreviewCancelled"/>, and clicking commits via the inherited
/// SelectionChanged. Items wrap into rows.
/// </summary>
public class RibbonGallery : ListBox
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = RibbonDropDownButton.HeaderProperty.AddOwner(typeof(RibbonGallery));

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty = RibbonDropDownButton.IconProperty.AddOwner(typeof(RibbonGallery));

    // Registered properties also resolve through WPF PropertyPath in shared templates.
    // This presentation state is neither persisted nor a public extension point.
    internal static readonly DependencyProperty IsQuickAccessOpenProperty = DependencyProperty.Register(
        "IsQuickAccessOpen", typeof(bool), typeof(RibbonGallery), new FrameworkPropertyMetadata(false));
    private ItemsPresenter? _quickAccessPresenter;
    private ScrollViewer? _originalViewport;
    private Action? _closeQuickAccess;
    internal ScrollViewer? QuickAccessViewport { get; private set; }
    internal bool IsQuickAccessOpen => (bool)GetValue(IsQuickAccessOpenProperty);

    /// <summary>Caption used by customization pages and gallery dropdown copies.</summary>
    public string? Header { get => (string?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Optional icon used by quick-access and custom-group dropdown copies.</summary>
    public ImageSource? Icon { get => (ImageSource?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        _closeQuickAccess?.Invoke();
        base.OnApplyTemplate();
        if (GroupStyle.Count == 0 && TryFindResource("RibbonKit.GalleryGroupStyle") is Style groupStyle)
            GroupStyle.Add(new GroupStyle { ContainerStyle = groupStyle });
    }

    internal bool BeginQuickAccess(ScrollViewer viewport, Action close)
    {
        _closeQuickAccess?.Invoke();
        if (this is InRibbonGallery gallery) gallery.SetCurrentValue(InRibbonGallery.IsDropDownOpenProperty, false);
        ApplyTemplate();
        if (!IsLoaded)
        {
            Measure(new Size(double.IsFinite(Width) && Width > 0 ? Width : 320, double.PositiveInfinity));
            ApplyTemplate();
        }
        var presenter = Template?.FindName("PART_ItemsPresenter", this) as ItemsPresenter ?? FindPresenter(this);
        if (presenter?.Parent is not ScrollViewer original) return false;
        _quickAccessPresenter = presenter;
        _originalViewport = original;
        QuickAccessViewport = viewport;
        _closeQuickAccess = close;
        if (original.TryFindResource(typeof(ScrollBar)) is Style scrollBarStyle)
            viewport.Resources[typeof(ScrollBar)] = scrollBarStyle;
        // Keep the ribbon's visible strip painted while its live presenter is in
        // the QAT popup. Capture before expanded layout changes headings/wrapping.
        // A DrawingImage retains vector glyphs/geometry at the destination DPI.
        var preview = IsVisible ? GalleryStripPreview.Create(original) : null;
        SetValue(IsQuickAccessOpenProperty, true);
        original.Content = preview;
        viewport.Content = presenter;
        return true;
    }

    internal void CloseQuickAccess() => _closeQuickAccess?.Invoke();

    internal void EndQuickAccess(ScrollViewer viewport)
    {
        if (!ReferenceEquals(viewport, QuickAccessViewport)) return;
        if (ReferenceEquals(viewport.Content, _quickAccessPresenter)) viewport.Content = null;
        if (_originalViewport is not null) _originalViewport.Content = _quickAccessPresenter;
        _quickAccessPresenter = null;
        _originalViewport = null;
        QuickAccessViewport = null;
        _closeQuickAccess = null;
        SetValue(IsQuickAccessOpenProperty, false);
        if (this is InRibbonGallery gallery) gallery.RefreshAfterQuickAccess();
    }

    private static ItemsPresenter? FindPresenter(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ItemsPresenter presenter) return presenter;
            if (FindPresenter(child) is { } found) return found;
        }
        return null;
    }

    /// <summary>Identifies the <see cref="ItemPreview"/> routed event.</summary>
    public static readonly RoutedEvent ItemPreviewEvent = EventManager.RegisterRoutedEvent(
        nameof(ItemPreview),
        RoutingStrategy.Bubble,
        typeof(RibbonGalleryPreviewEventHandler),
        typeof(RibbonGallery));

    /// <summary>Identifies the <see cref="ItemPreviewCancelled"/> routed event.</summary>
    public static readonly RoutedEvent ItemPreviewCancelledEvent = EventManager.RegisterRoutedEvent(
        nameof(ItemPreviewCancelled),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RibbonGallery));

    static RibbonGallery()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonGallery),
            new FrameworkPropertyMetadata(typeof(RibbonGallery)));
    }

    /// <summary>Raised when the mouse enters an item — apply the live preview.</summary>
    public event RibbonGalleryPreviewEventHandler ItemPreview
    {
        add => AddHandler(ItemPreviewEvent, value);
        remove => RemoveHandler(ItemPreviewEvent, value);
    }

    /// <summary>Raised when the mouse leaves the gallery — revert the live preview.</summary>
    public event RoutedEventHandler ItemPreviewCancelled
    {
        add => AddHandler(ItemPreviewCancelledEvent, value);
        remove => RemoveHandler(ItemPreviewCancelledEvent, value);
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonGalleryItem;

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() => new RibbonGalleryItem();

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is UIElement container)
        {
            container.MouseEnter += OnItemContainerMouseEnter;
        }
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        base.ClearContainerForItemOverride(element, item);
        if (element is UIElement container)
        {
            container.MouseEnter -= OnItemContainerMouseEnter;
        }
    }

    /// <summary>Leaving the gallery cancels the live preview.</summary>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        RaiseEvent(new RoutedEventArgs(ItemPreviewCancelledEvent, this));
    }

    private void OnItemContainerMouseEnter(object sender, MouseEventArgs e)
    {
        object? item = ItemContainerGenerator.ItemFromContainer((DependencyObject)sender);
        if (item == DependencyProperty.UnsetValue)
        {
            item = sender;
        }

        RaiseEvent(new RibbonGalleryPreviewEventArgs(ItemPreviewEvent, this, item));
    }
}
