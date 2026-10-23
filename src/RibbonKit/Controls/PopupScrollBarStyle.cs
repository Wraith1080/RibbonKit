using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace RibbonKit.Controls;

// Keep generated native bars on the shared chrome without replacing scrolling or custom styles.
internal static class PopupScrollBarStyle
{
    private const string ResourceKey = "RibbonKit.ScrollBarStyle";
    private static readonly Uri ResourcesUri = new(
        "/RibbonKit;component/Themes/Controls.ScrollBars.xaml", UriKind.RelativeOrAbsolute);

    internal static void Apply(ScrollViewer? viewport)
    {
        if (viewport is null || viewport.Resources.Contains(typeof(ScrollBar))) return;

        Style? style = viewport.TryFindResource(ResourceKey) as Style;
        if (style is null)
        {
            // Deferred popup scopes cannot always see aggregate sibling dictionaries.
            // Import the existing resources locally; their DynamicResources still follow the host.
            var resources = new ResourceDictionary { Source = ResourcesUri };
            viewport.Resources.MergedDictionaries.Add(resources);
            style = resources[ResourceKey] as Style;
        }

        if (style is not null) viewport.Resources[typeof(ScrollBar)] = style;
    }
}
