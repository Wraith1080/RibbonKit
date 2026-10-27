using System.ComponentModel;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Editors;

namespace RibbonKit.WinForms.Design;

public sealed class RibbonControlDesigner : ControlDesigner
{
    public override IReadOnlyCollection<IComponent> AssociatedComponents => ((RibbonControl)Component).Tabs.Cast<RibbonElement>().Concat(((RibbonControl)Component).QuickAccessItems).Append(((RibbonControl)Component).FileMenu).Append(((RibbonControl)Component).Backstage).SelectMany(t => t.TreeForDesigner()).ToArray();
}
public sealed class RibbonElementDesigner : ComponentDesigner
{
    public override IReadOnlyCollection<IComponent> AssociatedComponents => ((RibbonElement)Component).TreeForDesigner().Skip(1).ToArray();
}
internal static class ComponentTree
{
    internal static IEnumerable<RibbonElement> TreeForDesigner(this RibbonElement element)
    {
        yield return element;
        IEnumerable<RibbonElement> children = element switch
        {
            RibbonTab tab => tab.Groups,
            RibbonGroup group => group.Items,
            RibbonDropDownItem drop => drop.Items,
            RibbonBackstage backstage => backstage.Items,
            _ => []
        };
        foreach (var child in children)
            foreach (var descendant in child.TreeForDesigner())
                yield return descendant;
    }
}
public sealed class BackstageCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonBackstagePageItem), typeof(RibbonButtonItem)];
}
public sealed class TabCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonTab)];
}
public sealed class QuickAccessCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonButtonItem), typeof(RibbonToggleItem), typeof(RibbonQuickAccessReference), typeof(RibbonQuickAccessComboBoxReference), typeof(RibbonQuickAccessDropDownReference), typeof(RibbonQuickAccessGroupReference)];
}
public sealed class GroupCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonGroup)];
}
public sealed class ItemCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonButtonItem), typeof(RibbonToggleItem), typeof(RibbonCheckBoxItem), typeof(RibbonRadioButtonItem), typeof(RibbonDropDownItem), typeof(RibbonSplitButtonItem), typeof(RibbonTextBoxItem), typeof(RibbonComboBoxItem)];
}
public sealed class MenuItemCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonButtonItem), typeof(RibbonToggleItem), typeof(RibbonCheckBoxItem), typeof(RibbonRadioButtonItem), typeof(RibbonDropDownItem), typeof(RibbonSplitButtonItem), typeof(RibbonTextBoxItem), typeof(RibbonComboBoxItem), typeof(RibbonMenuSeparatorItem)];
}
