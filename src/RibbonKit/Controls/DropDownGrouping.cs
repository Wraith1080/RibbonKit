using System.Windows;
using System.Windows.Controls;

namespace RibbonKit.Controls;

internal static class DropDownGrouping
{
    internal static void ApplyDefault(ItemsControl owner)
    {
        if (owner.GroupStyle.Count == 0 && owner.TryFindResource("RibbonKit.DropDownGroupStyle") is Style style)
            owner.GroupStyle.Add(new GroupStyle { ContainerStyle = style, Panel = owner.ItemsPanel });
    }
}
