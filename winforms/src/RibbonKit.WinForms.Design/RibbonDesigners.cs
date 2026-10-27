using System.ComponentModel;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Editors;

namespace RibbonKit.WinForms.Design;

public sealed class RibbonControlDesigner : ControlDesigner
{
    public override IReadOnlyCollection<IComponent> AssociatedComponents => ((RibbonControl)Component).Tabs.SelectMany(t => t.TreeForDesigner()).ToArray();
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
            _ => []
        };
        foreach (var child in children)
            foreach (var descendant in child.TreeForDesigner())
                yield return descendant;
    }
}
public sealed class TabCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonTab)];
}
public sealed class GroupCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonGroup)];
}
public sealed class ItemCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonButtonItem), typeof(RibbonToggleItem), typeof(RibbonCheckBoxItem), typeof(RibbonRadioButtonItem), typeof(RibbonDropDownItem), typeof(RibbonSplitButtonItem), typeof(RibbonTextBoxItem), typeof(RibbonComboBoxItem)];
}
