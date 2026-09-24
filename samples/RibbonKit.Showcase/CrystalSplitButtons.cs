using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Gives the hovered split half a stronger wash than its companion.</summary>
internal static class CrystalSplitButtons
{
    private const string HoverKey = "RibbonKit.Brushes.Control.HoverBackground";
    private const string SplitActiveHoverKey = "RibbonKit.Brushes.Control.SplitActiveHover";

    public static void Apply(Ribbon ribbon, bool enabled)
    {
        var hover = enabled ? ribbon.TryFindResource(SplitActiveHoverKey) : null;
        foreach (RibbonTab tab in ribbon.Tabs)
            foreach (RibbonGroup group in tab.Groups)
                foreach (object item in group.Items)
                    if (item is RibbonSplitButton split) Apply(split, enabled, hover);
    }

    public static void Apply(RibbonSplitButton split, bool enabled)
        => Apply(split, enabled, enabled ? split.TryFindResource(SplitActiveHoverKey) : null);

    private static void Apply(RibbonSplitButton split, bool enabled, object? hover)
    {
        if (enabled)
        {
            if (hover != null) split.Resources[HoverKey] = hover;
        }
        else
            split.Resources.Remove(HoverKey);
    }
}
