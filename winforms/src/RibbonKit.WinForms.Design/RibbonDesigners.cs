using System.ComponentModel;
using System.Collections;
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Designers.Actions;
using Microsoft.DotNet.DesignTools.Editors;
using Microsoft.DotNet.DesignTools.Designers.Behaviors;
using System.ComponentModel.Design;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

public sealed class RibbonControlDesigner : ControlDesigner
{
    private RibbonDesignSelection? _selection;
    private RibbonSurfaceAdornment? _adornment;
    private RibbonFileDesignSettings? _fileSettings;
    internal RibbonDesignSelection Selection => _selection!;
    internal RibbonFileDesignSettings FileSettings => _fileSettings!;
    internal void EditRibbon() => this.InvokePropertyEditor(nameof(RibbonControl.Tabs));
    internal void EditBackstage() => this.InvokePropertyEditor(nameof(RibbonControl.Backstage));
    public override void Initialize(IComponent component)
    {
        base.Initialize(component);
        _fileSettings = new((RibbonControl)component, GetService);
        _selection = new((RibbonControl)component, GetService(typeof(ISelectionService)) as ISelectionService,
            (GetService(typeof(IDesignerHost)) as IDesignerHost)?.RootComponent as Control);
        if (GetService(typeof(BehaviorService)) is BehaviorService service)
            _adornment = new((RibbonControl)component, _selection, service);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _adornment?.Dispose(); _selection?.Dispose(); }
        base.Dispose(disposing);
    }
    public override DesignerActionListCollection ActionLists => new() { new RibbonSurfaceActions(Component, this) };
    protected override void PreFilterProperties(IDictionary properties)
    {
        base.PreFilterProperties(properties);
        if (properties[nameof(RibbonControl.Tabs)] is PropertyDescriptor tabs)
            properties[nameof(RibbonControl.Tabs)] = TypeDescriptor.CreateProperty(typeof(RibbonControl), tabs,
                new EditorAttribute(RibbonDesignEndpoint.EditorName, "System.Drawing.Design.UITypeEditor, System.Drawing"));
        if (properties[nameof(RibbonControl.Backstage)] is PropertyDescriptor backstage)
            properties[nameof(RibbonControl.Backstage)] = TypeDescriptor.CreateProperty(typeof(RibbonControl), backstage,
                new EditorAttribute(RibbonBackstageEndpoint.EditorName, "System.Drawing.Design.UITypeEditor, System.Drawing"));
    }
    public override IReadOnlyCollection<IComponent> AssociatedComponents => ((RibbonControl)Component).Tabs.Cast<RibbonElement>().Concat(((RibbonControl)Component).QuickAccessItems).Append(((RibbonControl)Component).FileMenu).Append(((RibbonControl)Component).Backstage).SelectMany(t => t.TreeForDesigner()).ToArray();
}
public sealed class RibbonElementDesigner : ComponentDesigner
{
    protected override void PreFilterProperties(IDictionary properties)
    {
        base.PreFilterProperties(properties);
        if (Component is RibbonBackstagePageItem && properties[nameof(RibbonBackstagePageItem.ContentType)] is PropertyDescriptor contentType)
            properties[contentType.Name] = TypeDescriptor.CreateProperty(typeof(RibbonBackstagePageItem), contentType,
                new TypeConverterAttribute(typeof(RibbonBackstageContentTypeConverter)));
        if (Component is RibbonBackstage)
            foreach (string name in new[] { nameof(RibbonBackstage.Items), nameof(RibbonBackstage.FooterItems) })
                if (properties[name] is PropertyDescriptor items)
                    properties[name] = TypeDescriptor.CreateProperty(typeof(RibbonBackstage), items,
                        new EditorAttribute(RibbonBackstageEndpoint.EditorName, "System.Drawing.Design.UITypeEditor, System.Drawing"));
    }
    public override DesignerActionListCollection ActionLists => ((RibbonElement)Component).Ribbon is { } ribbon
        && (GetService(typeof(IDesignerHost)) as IDesignerHost)?.GetDesigner(ribbon) is RibbonControlDesigner designer
            ? new() { new RibbonSurfaceActions(Component, designer) } : new();
    public override IReadOnlyCollection<IComponent> AssociatedComponents => ((RibbonElement)Component).TreeForDesigner().Skip(1).ToArray();
}
internal sealed class RibbonSurfaceActions(IComponent component, RibbonControlDesigner designer) : DesignerActionList(component)
{
    internal RibbonDesignSelection Selection => designer.Selection;
    public bool ShowFileButton { get => designer.FileSettings.Visible; set => designer.FileSettings.Visible = value; }
    [TypeConverter(typeof(RibbonFileModeConverter))]
    public string FileOpens { get => designer.FileSettings.Opens; set => designer.FileSettings.Opens = value; }
    [TypeConverter(typeof(RibbonPreviewTabConverter))]
    public string PreviewTab { get => Selection.PreviewTab; set => Selection.PreviewTab = value; }
    public void EditRibbon() => designer.EditRibbon();
    public void EditBackstage() => designer.EditBackstage();
    public void PreviewBackstage() => Selection.Backstage.Open();
    public void SelectRibbon() => Selection.Select(designer.Component);
    public override DesignerActionItemCollection GetSortedActionItems() => new()
    {
        new DesignerActionPropertyItem(nameof(ShowFileButton), "Show File button", "File", "Show or hide the ribbon's existing File header."),
        new DesignerActionPropertyItem(nameof(FileOpens), "File opens", "File", "Choose a command menu or Backstage. Showing File does not change its destination."),
        new DesignerActionPropertyItem(nameof(PreviewTab), "Preview tab", "Preview", "Show any authored tab without changing visibility, enabled values or startup selection."),
        new DesignerActionMethodItem(this, nameof(PreviewBackstage), "Preview Backstage", "Preview", "Open an inert designer preview. Its Back button returns to the ribbon."),
        new DesignerActionMethodItem(this, nameof(EditRibbon), "Edit Ribbon…", "Ribbon", "Edit tabs, groups and commands.", true),
        new DesignerActionMethodItem(this, nameof(EditBackstage), "Edit Backstage…", "Ribbon", "Edit Backstage pages, commands and File routing.", true),
        new DesignerActionMethodItem(this, nameof(SelectRibbon), "Select Ribbon", "Ribbon", "Select the ribbon control in Properties.")
    };
}
internal sealed class RibbonPreviewTabConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(
        context?.Instance is RibbonSurfaceActions actions ? actions.Selection.TabChoices : new[] { "(Startup tab)" });
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
            RibbonBackstage backstage => backstage.NavigationItems,
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
    protected override Type[] CreateNewItemTypes() => [typeof(RibbonBackstagePageItem), typeof(RibbonButtonItem), typeof(RibbonMenuSeparatorItem)];
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
