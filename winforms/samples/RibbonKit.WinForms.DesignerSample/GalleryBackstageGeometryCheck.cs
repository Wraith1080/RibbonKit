using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Package-only integration check: public callbacks and native Controls/HWNDs.
// Sends no global input and uses no library internals or Showcase helpers.
internal static class GalleryBackstageGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
            using var form = new RibbonForm { Width = 1500, Height = 800 };
            var ribbon = new RibbonControl { Renderer = painter, Density = density, AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon); form.Ribbon = ribbon;
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Built-in", CanResize = false }; tab.Groups.Add(group);
            var gallery = Gallery("Built-in"); group.Items.Add(gallery);
            var nativeGroup = new RibbonGroup { Text = "Native cards", CanResize = false }; tab.Groups.Add(nativeGroup);
            var cards = Gallery("Native cards"); cards.InRibbonPresenterType = typeof(GalleryStylesStripControl); cards.DropDownPresenterType = typeof(GalleryStylesControl); cards.UseDropDownPresenterInQuickAccess = true; nativeGroup.Items.Add(cards);
            ribbon.Tabs.Add(tab); var reference = new RibbonQuickAccessDropDownReference { Source = cards }; ribbon.QuickAccessItems.Add(reference);
            var body = new UserControl { BackColor = ribbon.Theme.Surface }; var field = new TextBox { Text = "Retained package draft", Bounds = new(20, 20, 220, 30) }; body.Controls.Add(field);
            var page = new RibbonBackstagePageItem { Text = "Document information", Content = body }; ribbon.Backstage.Items.Add(page);
            ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Print" }); ribbon.FileMenu.Visible = true; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            form.Show(); Application.DoEvents(); int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            var view = Descendants(ribbon).OfType<GalleryStylesStripControl>().Single(); nint viewHandle = view.Handle;
            var cardHandles = Descendants(view).OfType<GalleryStyleTileControl>().Where(c => c.Visible).Select(c => c.Handle).ToArray();
            var original = ribbon.Theme; var selection = cards.SelectedItem; var before = Draw("default"); int oldUtility = record.Utility.Width;
            ribbon.Theme = GalleryBackstageGeometrySettings.Navigation(GalleryBackstageGeometrySettings.Gallery(original)) with { BackstagePagePadding = 18 };
            _ = Draw("override"); Require(record.Utility.Width == S(40) && record.Utility.Width >= oldUtility, "Automatic inline utility width");
            Require(viewHandle == view.Handle && cardHandles.SequenceEqual(Descendants(view).OfType<GalleryStyleTileControl>().Where(c => c.Visible).Select(c => c.Handle)), "Native cards and handles retained");
            Require(ReferenceEquals(cards.SelectedItem, selection) && ReferenceEquals(reference.Source, cards), "Selection and QAT source identity");
            foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>()) { ribbon.QuickAccessPosition = placement; Application.DoEvents(); Require(viewHandle == view.Handle, "QAT relocation retains inline owner"); }
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            gallery.ShowDropDown(); Application.DoEvents(); Require(gallery.IsDropDownOpen, "Built-in popup opened");
            CaptureOwned("builtin-popup"); gallery.CloseDropDown();
            cards.ShowDropDown(); Application.DoEvents(); var popupView = OwnedControls(form).OfType<GalleryStylesControl>().Single();
            Require(popupView.Parent!.Left == S(10), "Native popup content inset uses gallery spacing");
            CaptureOwned("native-popup"); cards.CloseDropDown();
            ribbon.Theme = original; var reset = Draw("reset"); Require(before.SequenceEqual(reset), "Exact default/reset ribbon pixels");
            ribbon.Theme = GalleryBackstageGeometrySettings.Navigation(GalleryBackstageGeometrySettings.Gallery(original)) with { BackstagePagePadding = 18 };
            ribbon.IsBackstageOpen = true; Application.DoEvents(); Require(ribbon.IsBackstageOpen, "Backstage opened");
            nint bodyHandle = body.Handle, fieldHandle = field.Handle; var parent = body.Parent; var insetBounds = body.Parent!.Parent!.Bounds;
            field.Focus(); field.Select(2, 4); int validations = 0; field.Validating += (_, e) => { validations++; e.Cancel = true; };
            // Capture the native surface itself: Form.DrawToBitmap paints child
            // controls in reverse z-order and can draw the covered ribbon last.
            var backstageSurface = body.Parent!.Parent!.Parent!;
            using (var image = new Bitmap(backstageSurface.Width, backstageSurface.Height)) { backstageSurface.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, $"backstage-{office}-{density}.png")); }
            ribbon.Theme = ribbon.Theme with { BackstagePagePadding = null }; Application.DoEvents(); var expanded = body.Parent!.Parent!.Bounds;
            Require(insetBounds.Left - expanded.Left == S(18) && expanded.Right - insetBounds.Right == S(18), "Native page host uses symmetric content inset");
            ribbon.Theme = original; Application.DoEvents();
            Require(ReferenceEquals(body.Parent, parent) && body.Handle == bodyHandle && field.Handle == fieldHandle && field.Text == "Retained package draft" && field.SelectionStart == 2 && field.SelectionLength == 4 && field.Focused && validations == 0, "Native page HWND/text/focus/validation retention");
            Require(ReferenceEquals(page, ribbon.Backstage.SelectedPage), "Backstage selection retained");
            evidence.Add($"PASS renderer={office} density={density} nativeDpi={ribbon.DeviceDpi}: automatic inline utility, native cards, QAT source/placements, built-in and native popup, page inset/retention and exact reset.");
            form.Close(); body.Dispose();

            byte[] Draw(string state)
            {
                Application.DoEvents(); using var bitmap = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(bitmap, new(Point.Empty, bitmap.Size));
                using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png); var png = stream.ToArray();
                File.WriteAllBytes(Path.Combine(output, $"ribbon-{office}-{density}-{state}.png"), png); return png;
            }
            void CaptureOwned(string name)
            {
                foreach (var root in OwnedRoots(form))
                { using var bitmap = new Bitmap(root.Width, root.Height); root.DrawToBitmap(bitmap, new(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(output, $"{name}-{office}-{density}.png")); }
            }
        }
        using (var form = new DesignerForm()) form.VerifyGalleryBackstageDemoContract();
        evidence.Add("PASS permanent sample independent gallery/navigation/page reset, earlier geometry, QAT sources and authored pages/File routing retained. Shown automatic fixtures; no global input. VS, external UIA, mixed-monitor and physical-input acceptance remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static RibbonGalleryItem Gallery(string text)
    {
        var gallery = new RibbonGalleryItem { Text = text, IsInRibbon = true, InRibbonColumnCount = 2, ColumnCount = 3, MaxVisibleRows = 2, TileWidth = 100 };
        for (int i = 0; i < 18; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i, GroupName = i < 9 ? "Document styles" : "Other styles", Icon = RibbonStockIcons.Create("open") });
        gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; return gallery;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(Descendants));
    private static List<Control> OwnedRoots(Form form)
    {
        var roots = new List<Control>(); EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (GetAncestor(window, 3) == form.Handle && window != form.Handle && Control.FromHandle(window) is { } control) roots.Add(control); return true; }, 0); return roots;
    }
    private static IEnumerable<Control> OwnedControls(Form form) => OwnedRoots(form).SelectMany(Descendants);
    private sealed class Record { internal Rectangle Utility; }
    private sealed class Neutral(Record record) : RibbonRenderer
    { public override void DrawGalleryStripButton(RibbonPaintContext c, RibbonGalleryStripButton b, bool enabled) { record.Utility = c.Bounds; base.DrawGalleryStripButton(c, b, enabled); } }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    { public override void DrawGalleryStripButton(RibbonPaintContext c, RibbonGalleryStripButton b, bool enabled) { record.Utility = c.Bounds; base.DrawGalleryStripButton(c, b, enabled); } }
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    private delegate bool EnumWindow(nint window, nint parameter);
}
