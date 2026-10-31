using System.ComponentModel;
using System.Collections;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Designers.Actions;
using Microsoft.DotNet.DesignTools.Editors;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

public sealed class RibbonControlDesigner : ControlDesigner
{
    public override DesignerActionListCollection ActionLists => new() { new RibbonActions(this) };
    protected override void PreFilterProperties(IDictionary properties)
    {
        base.PreFilterProperties(properties);
        if (properties[nameof(RibbonControl.Tabs)] is PropertyDescriptor tabs)
            properties[nameof(RibbonControl.Tabs)] = TypeDescriptor.CreateProperty(typeof(RibbonControl), tabs,
                new EditorAttribute(RibbonDesignEndpoint.EditorName, "System.Drawing.Design.UITypeEditor, System.Drawing"));
    }
    public override IReadOnlyCollection<IComponent> AssociatedComponents => ((RibbonControl)Component).Tabs.Cast<RibbonElement>().Concat(((RibbonControl)Component).QuickAccessItems).Append(((RibbonControl)Component).FileMenu).Append(((RibbonControl)Component).Backstage).SelectMany(t => t.TreeForDesigner()).ToArray();
    private sealed class RibbonActions(RibbonControlDesigner designer) : DesignerActionList(designer.Component)
    {
        public void EditRibbon() => designer.InvokePropertyEditor(nameof(RibbonControl.Tabs));
        public override DesignerActionItemCollection GetSortedActionItems() => new()
        { new DesignerActionMethodItem(this, nameof(EditRibbon), "Edit Ribbon…", "Ribbon", "Edit tabs, groups and commands.", true) };
    }
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
            RibbonMergeSource source => source.Tabs.Cast<RibbonElement>().Concat(source.Groups),
            RibbonGroupContribution contribution => [contribution.Group],
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
public sealed class MessageCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonMessage)];
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
public sealed class GroupContributionCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonGroupContribution)];
}
public sealed class ItemCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonButtonItem), typeof(RibbonToggleItem), typeof(RibbonCheckBoxItem), typeof(RibbonRadioButtonItem), typeof(RibbonDropDownItem), typeof(RibbonGalleryItem), typeof(RibbonSplitButtonItem), typeof(RibbonTextBoxItem), typeof(RibbonComboBoxItem)];
}
public sealed class GalleryTileCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonGalleryTile)];
}
public sealed class MenuItemCollectionEditor(IServiceProvider serviceProvider, Type type) : CollectionEditor(serviceProvider, type)
{
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonButtonItem), typeof(RibbonToggleItem), typeof(RibbonCheckBoxItem), typeof(RibbonRadioButtonItem), typeof(RibbonDropDownItem), typeof(RibbonGalleryItem), typeof(RibbonSplitButtonItem), typeof(RibbonTextBoxItem), typeof(RibbonComboBoxItem), typeof(RibbonMenuSeparatorItem)];
}
