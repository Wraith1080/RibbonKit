using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Controls;

/// <summary>
/// A selectable tile inside a <see cref="RibbonGallery"/> or
/// <see cref="InRibbonGallery"/>. Put any content inside (a style preview, a color
/// swatch, a thumbnail).
/// </summary>
public class RibbonGalleryItem : ListBoxItem
{
    static RibbonGalleryItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonGalleryItem),
            new FrameworkPropertyMetadata(typeof(RibbonGalleryItem)));
    }
}
