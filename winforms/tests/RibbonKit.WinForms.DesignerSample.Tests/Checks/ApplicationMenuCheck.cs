using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void VerifyApplicationMenu(string? captureDirectory = null)
    {
        Require(applicationOpen.ContentType == typeof(PopupOptionsControl) && applicationOpen.Content is PopupOptionsControl, "Generated ContentType initialized before Form.Load");
        Require(ribbon.FileMenu.Items.Contains(applicationOpen) && ribbon.FileMenu.Items.Contains(applicationSave) && ribbon.FileMenu.Items.IndexOf(applicationOpen) < ribbon.FileMenu.Items.IndexOf(applicationSave), "Generated File sources and order");
        Require(ribbon.ApplicationMenu.DefaultPane.Items[0] == applicationRecent && ribbon.ApplicationMenu.FooterItems[0] == applicationOptions, "Generated default pane and footer ownership");
        var body = (PopupOptionsControl)applicationOpen.Content!;
        Require(!ReferenceEquals(body, selectDropdown.Content) && !ReferenceEquals(body, pasteSplitButton.Content), "One retained instance per declaration");
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        Show(); Application.DoEvents(); ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
        ribbon.FileMenu.ShowDropDown(); Application.DoEvents(); Capture("default");
        applicationPrint.ShowDropDown(); Application.DoEvents(); Capture("print");
        applicationOpen.ShowDropDown(); Application.DoEvents(); Capture("native-pane");
        Require(ribbon.FileMenu.IsDropDownOpen && applicationOpen.IsDropDownOpen && body.Parent is not null, "Generated native pane opens within File");
        body.ValueEditor.Text = "Application Menu retained value"; body.ValueEditor.Select(2, 5); var handle = body.ValueEditor.Handle;
        applicationSave.ShowDropDown(); Application.DoEvents(); Capture("save-as");
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
        void Capture(string name)
        {
            if (captureDirectory is null) return;
            Application.DoEvents(); Thread.Sleep(80); Application.DoEvents();
            var window = Control.FromHandle(GetForegroundWindow()) as ToolStripDropDown ?? throw new InvalidOperationException("Sample lost foreground during capture.");
            var area = window.Bounds; int halo = (int)Math.Round(8d * DeviceDpi / 96); area.Inflate(halo, halo); area.Intersect(Screen.FromControl(window).WorkingArea);
            using var bitmap = new Bitmap(area.Width, area.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(area.Location, Point.Empty, area.Size); bitmap.Save(Path.Combine(captureDirectory, name + ".png"));
        }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern nint GetForegroundWindow();
}
