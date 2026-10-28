using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    /// <summary>Gets the stable owned command catalog and application command declarations used by native customization.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonCommandCatalog CommandCatalog { get; }
}
