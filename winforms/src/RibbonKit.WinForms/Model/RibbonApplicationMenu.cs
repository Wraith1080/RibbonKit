using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>Owns the default right pane and footer of File's native Application Menu. Left entries remain in FileMenu.Items.</summary>
public sealed class RibbonApplicationMenu : RibbonElement
{
    /// <summary>Initializes an empty Application Menu declaration.</summary>
    public RibbonApplicationMenu()
    {
        DefaultPane = new() { Parent = this };
        FooterItems = new(this);
    }
    /// <summary>Gets the stable default right pane. Use Items for commands, DropDownHeader for its heading, or Content/ContentType for retained native content. It is displayed on every opening until a left dropdown is selected.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public RibbonDropDownItem DefaultPane { get; }
    /// <summary>Gets the owned footer actions and separators. The footer has its own reserved, scrollable viewport when its actions cannot fit. Empty by default.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonItemCollection FooterItems { get; }
    internal override IEnumerable<RibbonElement> Children => new RibbonElement[] { DefaultPane }.Concat(FooterItems);
}
