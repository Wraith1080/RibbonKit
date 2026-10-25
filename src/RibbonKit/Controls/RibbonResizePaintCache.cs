using System.Windows;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Native interactive resize waits for WPF's rendered frame. Reuse the ribbon's
// device-resolution paint during that loop; the live visual/input tree stays intact.
internal sealed class RibbonResizePaintCache : IDisposable
{
    private readonly List<(FrameworkElement Element, BitmapCache Cache)> _entries = new();

    internal RibbonResizePaintCache(RibbonWindow window)
    {
        foreach (var ribbon in Ribbons(window))
        {
            Cache(ribbon);
            if (ribbon.ActiveQuickAccessToolBar is { } qat && !ribbon.IsAncestorOf(qat)) Cache(qat);
        }
    }

    private void Cache(FrameworkElement element)
    {
        var source = DependencyPropertyHelper.GetValueSource(element, UIElement.CacheModeProperty);
        if (!element.IsVisible || !element.IsLoaded || element.CacheMode is not null
            || source.BaseValueSource != BaseValueSource.Default || source.IsAnimated || source.IsCurrent
            || element.ReadLocalValue(UIElement.CacheModeProperty) != DependencyProperty.UnsetValue) return;

        var cache = new BitmapCache { EnableClearType = true, SnapsToDevicePixels = true };
        element.SetCurrentValue(UIElement.CacheModeProperty, cache);
        _entries.Add((element, cache));
    }

    public void Dispose()
    {
        foreach (var (element, cache) in _entries)
            if (ReferenceEquals(element.CacheMode, cache)
                && DependencyPropertyHelper.GetValueSource(element, UIElement.CacheModeProperty).IsCurrent)
                element.ClearValue(UIElement.CacheModeProperty);
        _entries.Clear();
    }

    private static IEnumerable<Ribbon> Ribbons(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Ribbon ribbon) yield return ribbon;
            else foreach (var nested in Ribbons(child)) yield return nested;
        }
    }
}
