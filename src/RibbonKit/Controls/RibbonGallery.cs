using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
