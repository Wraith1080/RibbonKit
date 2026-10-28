using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    /// <summary>Gets the owned File command menu. Set Visible to true to show its header; configure its Items, events and metadata through this stable declaration.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonDropDownItem FileMenu { get; }

    internal bool HasFileMenu => !IsModal && !FileMenu.IsDisposed && FileMenu.Visible;
}
