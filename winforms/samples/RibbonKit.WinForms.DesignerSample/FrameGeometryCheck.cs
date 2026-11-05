using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public-package consumer. Owns its test windows; sends no global input and uses no runtime internals.
internal static class FrameGeometryCheck
{
    internal static void CaptureNativeSurfaces(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Location = new(80, 60), Width = 1100, Height = 720, Text = "Frame surface capture" };
            var ribbon = new RibbonControl { Renderer = painter, Density = density, AnimationLevel = RibbonAnimationLevel.None,
                Theme = FrameGeometrySettings.PopupShadow(FrameGeometrySettings.BodyShadow(FrameGeometrySettings.Corners(RibbonThemes.Office2024Light))) };
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Native popup", CanResize = false }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var menu = new RibbonDropDownItem { Text = "Popup" }; menu.Items.AddRange(new RibbonTextBoxItem { Text = "Draft", Value = "Native retained content" }, new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true }, new RibbonButtonItem { Text = "Apply" }); group.Items.Add(menu);
            using var page = new UserControl(); page.Controls.Add(new TextBox { Text = "Native Backstage page", Bounds = new(20, 30, 260, 30) }); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Information", Content = page });
            ribbon.FileMenu.Visible = true; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; form.Show(); Application.DoEvents(); menu.ShowDropDown(); Application.DoEvents();
            var popup = OwnedRoots(form).Single(c => c.Controls.Count > 0); int padding = painter.GetPopupShadowPadding(ribbon.Theme, ribbon.DeviceDpi);
            Capture(Rectangle.Inflate(popup.Bounds, padding, padding), "popup"); menu.CloseDropDown(); ribbon.IsBackstageOpen = true; Application.DoEvents(); Capture(form.RectangleToScreen(form.ClientRectangle), "backstage");
            evidence.Add($"PASS composed renderer={office} density={density} nativeDpi={ribbon.DeviceDpi}; popup/frame shadow extent and native page region; no global input."); form.Close();
            void Capture(Rectangle bounds, string name)
            {
                // Let the OS finish presenting a newly shown native menu before screen capture.
                var clock = System.Diagnostics.Stopwatch.StartNew();
                while (clock.ElapsedMilliseconds < 400) { Application.DoEvents(); Thread.Sleep(10); }
                Require(GetAncestor(GetForegroundWindow(), 3) == form.Handle, "Capture stopped: fixture lost foreground.");
                Require(Screen.FromHandle(form.Handle).Bounds.Contains(bounds), "Capture stopped: surface extends beyond the display.");
                DwmFlush(); using var image = new Bitmap(bounds.Width, bounds.Height); using (var g = Graphics.FromImage(image)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                image.Save(Path.Combine(output, $"{name}-{office}-{density}.png"));
            }
        }
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
            using var form = new RibbonForm { Width = 1300, Height = 800, Text = "Native frame geometry" };
            var ribbon = new RibbonControl { Renderer = painter, Density = density, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); form.Ribbon = ribbon;
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Retained native content", CanResize = false }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained inline field" };
            var menu = new RibbonDropDownItem { Text = "Popup example", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var popupField = new RibbonTextBoxItem { Text = "Draft", Value = "Retained popup field" }; menu.Items.Add(popupField);
            var gallery = new RibbonGalleryItem { Text = "Styles", ColumnCount = 3, MaxVisibleRows = 2, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            for (int i = 0; i < 24; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i, Icon = RibbonStockIcons.Create("open") });
            gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; group.Items.AddRange(field, menu, gallery);
            var reference = new RibbonQuickAccessDropDownReference { Source = menu }; ribbon.QuickAccessItems.Add(reference); ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Frame review", Text = "Connected body, QAT and message" });
            using var page = new UserControl(); var pageField = new TextBox { Text = "Retained authored page", Bounds = new(20, 20, 260, 30) }; page.Controls.Add(pageField);
            var model = new RibbonBackstagePageItem { Text = "Information", Content = page }; ribbon.Backstage.Items.Add(model); ribbon.FileMenu.Visible = true; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            form.Show(); Application.DoEvents(); var native = Descendants(ribbon).OfType<TextBoxBase>().Single(t => t.Text == field.Value); nint handle = native.Handle;
            // Simplified retains the native field in its editor parking owner and presents a launcher.
            ribbon.LayoutMode = mode; var original = ribbon.Theme; var selection = gallery.SelectedItem;
            var before = Draw("default"); var beforeBounds = (record.Body, record.Qat, record.Message, ribbon.Height);
            nint popupHandle = OpenMenu("default"); menu.CloseDropDown();
            ribbon.Theme = FrameGeometrySettings.PopupShadow(FrameGeometrySettings.BodyShadow(FrameGeometrySettings.Corners(original)));
            _ = Draw("override"); int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            if (office)
            {
                Require(record.Body.Left >= S(12) && ribbon.Height - record.Message.Bottom >= S(16), "Explicit panel shadow clearance");
                Require(record.Body.Left == record.Qat.Left && record.Body.Right == record.Qat.Right && record.Qat.Bottom == record.Message.Top, "Joined body/QAT/message bounds");
                Require(record.MessageConnected, "Message shares the preceding panel edge");
            }
            Require(ribbon.Theme.BodyMargin == original.BodyMargin && native.Handle == handle && field.Value == "Retained inline field", "Authored margins and native field identity retained");
            Require(OpenMenu("override") == popupHandle, "Popup editor HWND retained across theme application"); menu.CloseDropDown();
            gallery.ShowDropDown(); Application.DoEvents(); Require(gallery.IsDropDownOpen && ReferenceEquals(selection, gallery.SelectedItem), "Gallery popup and selection"); CapturePopup("gallery"); gallery.CloseDropDown();
            ribbon.IsMinimized = true; _ = Draw("minimized");
            if (office) Require(record.Qat.Left == record.Message.Left && record.Qat.Right == record.Message.Right && record.Qat.Bottom == record.Message.Top, "Standalone QAT/message joined card");
            ribbon.IsMinimized = false; ribbon.Theme = original; var reset = Draw("reset"); Require(before.SequenceEqual(reset) && beforeBounds == (record.Body, record.Qat, record.Message, ribbon.Height), "Exact default/reset pixels and geometry");
            Require(native.Handle == handle && ReferenceEquals(reference.Source, menu), "Field and QAT source identity after reset");
            ribbon.IsBackstageOpen = true; Application.DoEvents(); var pageParent = page.Parent; var frame = page.Parent!.Parent!; nint pageHandle = page.Handle, pageFieldHandle = pageField.Handle; var frameBounds = frame.Bounds;
            ribbon.Theme = FrameGeometrySettings.Corners(original); Application.DoEvents();
            Require(page.Handle == pageHandle && pageField.Handle == pageFieldHandle && ReferenceEquals(page.Parent, pageParent) && frame.Bounds == frameBounds && ReferenceEquals(model, ribbon.Backstage.SelectedPage), "Backstage native page/selection retained");
            Require(office ? frame.Region is not null && !frame.Region.IsVisible(0, 0) : frame.Region is null, "Renderer page clip policy");
            var surface = frame.Parent!; using (var image = new Bitmap(surface.Width, surface.Height)) { surface.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, $"page-{office}-{density}-{mode}.png")); }
            ribbon.Theme = original; Require(pageField.Text == "Retained authored page", "Page value retained after reset"); ribbon.IsBackstageOpen = false;
            evidence.Add($"PASS renderer={office} density={density} mode={mode} nativeDpi={ribbon.DeviceDpi}: exact reset, panel joins/clearance, native inline/popup/page HWNDs, popup region/chrome, gallery selection and Backstage clip."); form.Close();

            byte[] Draw(string state)
            {
                Application.DoEvents(); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                using var stream = new MemoryStream(); image.Save(stream, System.Drawing.Imaging.ImageFormat.Png); byte[] png = stream.ToArray();
                File.WriteAllBytes(Path.Combine(output, $"ribbon-{office}-{density}-{mode}-{state}.png"), png); return png;
            }
            nint OpenMenu(string state)
            {
                menu.ShowDropDown(); Application.DoEvents(); var popup = Popup(); var editor = Descendants(popup).OfType<TextBoxBase>().Single(t => t.Text == popupField.Value);
                Require(painter.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi) > 0 ? popup.Region is not null && !popup.Region.IsVisible(0, 0) : popup.Region is null, "Opaque popup region follows renderer radius");
                CapturePopup("menu-" + state); return editor.Handle;
            }
            Control Popup() => OwnedRoots(form).Single(c => c.Controls.Count > 0);
            void CapturePopup(string name)
            {
                var popup = Popup(); int padding = painter.GetPopupShadowPadding(ribbon.Theme, ribbon.DeviceDpi);
                nint chrome = GetWindow(popup.Handle, 2);
                // Neutral square chrome is optional when Windows disables drop shadows.
                if (office || SystemInformation.IsDropShadowEnabled)
                {
                    Require(GetWindowRect(chrome, out var rectangle) && rectangle.Rectangle == Rectangle.Inflate(popup.Bounds, padding, padding), "Native chrome uses renderer shadow footprint");
                    Require((GetWindowLongPtrW(chrome, -20).ToInt64() & 0x080800A0L) == 0x080800A0L, "Native chrome is transparent to activation/input");
                }
                using var image = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, $"{name}-{office}-{density}-{mode}.png"));
            }
        }
        using (var form = new DesignerForm()) form.VerifyFrameDemoContract();
        evidence.Add("PASS permanent sample independent resets and original authored popup radius/earlier geometry/owners. No global input. Synthetic DPI/corner/motion/cache checks are separate unit evidence; actual VS, external UIA, physical input and mixed-monitor acceptance remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private sealed class Record { internal Rectangle Body, Qat, Message; internal bool MessageConnected; }
    private sealed class Neutral(Record record) : RibbonRenderer
    {
        public override void DrawBody(RibbonPaintContext c) { record.Body = c.Bounds; base.DrawBody(c); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition p) { record.Qat = c.Bounds; base.DrawQuickAccessToolbar(c, p); }
        public override void DrawMessageBar(RibbonPaintContext c) { record.Message = c.Bounds; record.MessageConnected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawBody(RibbonPaintContext c) { record.Body = c.Bounds; base.DrawBody(c); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition p) { record.Qat = c.Bounds; base.DrawQuickAccessToolbar(c, p); }
        public override void DrawMessageBar(RibbonPaintContext c) { record.Message = c.Bounds; record.MessageConnected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(Descendants));
    private static List<Control> OwnedRoots(Form form)
    {
        var roots = new List<Control>(); EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (GetAncestor(window, 3) == form.Handle && window != form.Handle && Control.FromHandle(window) is { } control) roots.Add(control); return true; }, 0); return roots;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; internal readonly Rectangle Rectangle => Rectangle.FromLTRB(Left, Top, Right, Bottom); }
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeRect rectangle);
    [DllImport("user32.dll")] private static extern nint GetWindowLongPtrW(nint window, int index);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    private delegate bool EnumWindow(nint window, nint parameter);
}
