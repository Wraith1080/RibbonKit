using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void VerifyApplicationMenu()
    {
        Require(applicationOpen.ContentType == typeof(PopupOptionsControl) && applicationOpen.Content is PopupOptionsControl, "Generated ContentType initialized before Form.Load");
        Require(ribbon.FileMenu.Items.SequenceEqual(new RibbonItem[] { applicationOpen, applicationSave }), "Generated File sources and order");
        Require(ribbon.ApplicationMenu.DefaultPane.Items.Single() == applicationRecent && ribbon.ApplicationMenu.FooterItems.Single() == applicationOptions, "Generated default pane and footer ownership");
        var body = (PopupOptionsControl)applicationOpen.Content!;
        Require(!ReferenceEquals(body, selectDropdown.Content) && !ReferenceEquals(body, pasteSplitButton.Content), "One retained instance per declaration");
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        Show(); Application.DoEvents(); ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
        ribbon.FileMenu.ShowDropDown(); applicationOpen.ShowDropDown(); Application.DoEvents();
        Require(ribbon.FileMenu.IsDropDownOpen && applicationOpen.IsDropDownOpen && body.Parent is not null, "Generated native pane opens within File");
        body.ValueEditor.Text = "Application Menu retained value"; body.ValueEditor.Select(2, 5); var handle = body.ValueEditor.Handle;
        applicationSave.ShowDropDown(); Application.DoEvents();
        Require(body.Parent is null && applicationSave.IsDropDownOpen, "Split arrow selects its generated pane without disposing native content");
        applicationOpen.ShowDropDown(); Application.DoEvents();
        Require(body.ValueEditor.Handle == handle && body.ValueEditor.Text == "Application Menu retained value", "Generated pane keeps native HWND and text");
        ribbon.FileMenu.CloseDropDown(); ribbon.FileMenu.ShowDropDown(); applicationOpen.ShowDropDown(); Application.DoEvents();
        Require(body.ValueEditor.Handle == handle && ReferenceEquals(applicationOpen.Content, body), "Generated pane survives close/reopen");
        var popup = body.TopLevelControl as ToolStripDropDown ?? throw new InvalidOperationException("Expected library popup host.");
        var surface = popup.Items.OfType<ToolStripControlHost>().Single().Control;
        var root = surface.AccessibilityObject;
        var options = Enumerable.Range(0, root.GetChildCount()).Select(root.GetChild).Single(a => a?.Name == "Options")!;
        options.DoDefaultAction(); Application.DoEvents();
        Require(!ribbon.FileMenu.IsDropDownOpen && statusLabel.Text == "Application Menu: Options", "Generated footer event and library dismissal");
        Close();
    }
}
