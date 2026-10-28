using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Uses public models, renderer observations and real mouse/keyboard input.
internal static class PopupRefinementChecks
{
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct ComboInfo { internal int Size; internal NativeRect Item, Button; internal int State; internal nint Combo, Edit, List; }
    [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(nint window, ref ComboInfo info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extra);
    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer painter = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Width = 1100, Height = 680, Text = "Independent popup refinements", StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter, QuickAccessMaxWidth = 320 }; var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands", CanResize = false };
        var menu = new RibbonDropDownItem { Text = "Paste", DropDownHeader = "Paste options" };
        var shortRow = new RibbonButtonItem { Text = "Select all", MenuDescription = "Select the current content." };
        var tallRow = new RibbonButtonItem { Text = "Paste text only", MenuDescription = "Insert clipboard text without retaining its original document formatting." };
        var separator = new RibbonMenuSeparatorItem(); var disabled = new RibbonButtonItem { Text = "Unavailable", Enabled = false };
        var nested = new RibbonDropDownItem { Text = "More options", DropDownHeader = "More paste options" }; nested.Items.AddRange(new RibbonButtonItem { Text = "First" }, new RibbonButtonItem { Text = "Second", MenuDescription = "A longer explanation for the second command." });
        var field = new RibbonTextBoxItem { Id = "popup.name", Text = "Name", Value = "Document" };
        var combo = new RibbonComboBoxItem { Id = "popup.font", Text = "Font", IsEditable = true }; combo.Items.AddRange("Original", "Second", "Third"); combo.SelectedIndex = 0;
        var selectionOnly = new RibbonComboBoxItem { Id = "popup.choice", Text = "Choice" }; selectionOnly.Items.AddRange("Alpha", "Beta"); selectionOnly.SelectedIndex = 1;
        menu.Items.AddRange(shortRow, tallRow, separator, disabled, nested, field, combo, selectionOnly); group.Items.Add(menu); home.Groups.Add(group);
        var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true, TileWidth = 64, InRibbonColumnCount = 2 }; gallery.Items.AddRange(Enumerable.Range(0, 8).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; var styles = new RibbonGroup { Text = "Styles" }; styles.Items.Add(gallery); home.Groups.Add(styles);
        var borrowed = new RibbonGroup { Text = "Fields" }; var retained = new RibbonTextBoxItem { Id = "popup.retained", Text = "Retained", Value = "Retained native field" }; borrowed.Items.Add(retained); home.Groups.Add(borrowed); ribbon.Tabs.Add(home);
        var menuRef = new RibbonQuickAccessDropDownReference { Source = menu }; var comboRef = new RibbonQuickAccessComboBoxReference { Source = combo }; var groupRef = new RibbonQuickAccessGroupReference { Source = borrowed }; var galleryRef = new RibbonQuickAccessDropDownReference { Source = gallery }; ribbon.QuickAccessItems.AddRange(menuRef, comboRef, groupRef, galleryRef);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application-owned document" }; form.Controls.Add(document); form.Controls.Add(ribbon);
        int closed = 0, commits = 0, unavailable = 0, previews = 0, cancellations = 0; menu.DropDownClosed += (_, _) => closed++; gallery.SelectionCommitted += (_, _) => commits++; gallery.ItemPreview += (_, _) => previews++; gallery.ItemPreviewCancelled += (_, _) => cancellations++;
        var log = new List<string>(); Exception? failure = null; bool completed = false; var cursor = WF.Cursor.Position;
        bool allowClose = false, awaitingHostClose = false; int hostCloseRequests = 0, finalCloseCount = 0;
        form.FormClosing += (_, e) => { hostCloseRequests++; e.Cancel = !allowClose; };
        using var closeDeadline = new WF.Timer { Interval = 3000 };
        closeDeadline.Tick += (_, _) => { closeDeadline.Stop(); completed = false; failure = new InvalidOperationException("Permitted host close did not complete."); log.Add("FAIL: " + failure); allowClose = true; form.Close(); };
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; }; AppDomain.CurrentDomain.FirstChanceException += exceptions;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield();
                for (int i = 0; i < 40; i++) { form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(50); if (GetForegroundWindow() == form.Handle) break; }
                Guard(); ribbon.Refresh();
                await OpenMenu(); var surface = Popup(); var window = (WF.ToolStripDropDown)surface.TopLevelControl!; var native = Descendants(surface).OfType<WF.TextBox>().Single(c => c.Name == field.Id); nint handle = native.Handle;
                await RequestHostClose("command menu"); Check(!menu.IsDropDownOpen && !window.Visible && form.Visible && !native.IsDisposed,
                    "Cancelled host close leaves no empty menu wrapper or destroyed editor");
                Check(closed == 1, "Menu closed event occurs once"); await OpenMenu(); surface = Popup(); Check(Descendants(surface).OfType<WF.TextBox>().Single(c => c.Name == field.Id).Handle == handle, "Menu reopening reuses its native editor");
                var compact = surface.Size; int compactRow = record.Rows[tallRow].Height; EqualRows(); await Capture("compact-menu"); await Key("{ESC}");
                ribbon.Density = RibbonDensity.Touch; await OpenMenu(); surface = Popup(); EqualRows();
                Check(surface.Width > compact.Width && record.Rows[tallRow].Height > compactRow, "Touch expands described menu width and uniform row height");
                foreach (var model in new[] { combo, selectionOnly })
                {
                    var nativeField = Descendants(surface).OfType<WF.ComboBox>().Single(c => c.Name == model.Id); var painted = nativeField.Parent!.RectangleToScreen(record.Fields[model]);
                    using var dc = nativeField.CreateGraphics(); var text = nativeField.RectangleToScreen(Rectangle.Round(nativeField.Region!.GetBounds(dc)));
                    Check(Math.Abs((text.Top + text.Bottom - painted.Top - painted.Bottom) / 2d) <= 1, $"Touch {model.Text} text is centered in its painted field");
                    if (model.IsEditable) { var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() }; Check(GetComboBoxInfo(nativeField.Handle, ref info) && GetWindowRect(info.Edit, out var editBounds) && Math.Abs((editBounds.Top + editBounds.Bottom - painted.Top - painted.Bottom) / 2d) <= 1, "Native editable ComboBox child is vertically centered"); }
                }
                await Capture("touch-menu");
                await Click(surface, record.Rows[nested]); Check(nested.IsDropDownOpen, "Nested menu opens"); await RequestHostClose("nested menu"); Check(!nested.IsDropDownOpen && !menu.IsDropDownOpen, "Host close request dismisses the entire nested menu chain");
                await OpenMenu(); surface = Popup(); native = Descendants(surface).OfType<WF.TextBox>().Single(c => c.Name == field.Id); await Click(native, native.ClientRectangle); await Key("^aPending document"); await RequestHostClose("hosted native field"); Check(!menu.IsDropDownOpen && !native.IsDisposed && native.Handle == handle, "Cancelled host close preserves its native field HWND");
                await OpenMenu(); surface = Popup(); var nativeCombo = Descendants(surface).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint comboHandle = nativeCombo.Handle;
                Check(Descendants(surface).OfType<WF.TextBox>().Single(c => c.Name == field.Id).Text == "Pending document", "Cancelled host close retains typed field content when the menu reopens");
                await Click(nativeCombo, nativeCombo.ClientRectangle); await Key("{F4}{END}"); Check(combo.IsDropDownOpen, "Native field choice list opens"); await RequestHostClose("nested native chooser"); Check(!combo.IsDropDownOpen && !menu.IsDropDownOpen && combo.SelectedIndex == 0, "Host close request cancels native choice preview and its parent menu");
                ribbon.Refresh(); await Click(ribbon, record.Qat[comboRef]); await Key("{END}"); await RequestHostClose("QAT chooser"); Check(!comboRef.IsDropDownOpen && combo.SelectedIndex == 0 && nativeCombo.Handle == comboHandle, "QAT chooser cancellation preserves the original selection and native editor");
                ribbon.Refresh(); await Click(ribbon, record.Qat[groupRef]); surface = Popup(); var retainedEditor = Descendants(surface).OfType<WF.TextBox>().Single(c => c.Name == retained.Id); nint retainedHandle = retainedEditor.Handle;
                await Click(retainedEditor, retainedEditor.ClientRectangle); await RequestHostClose("borrowed group field"); Check(!groupRef.IsDropDownOpen && !retainedEditor.IsDisposed, "Cancelled host close releases a borrowed group without disposing its native field");
                ribbon.Refresh(); await Click(ribbon, record.Qat[groupRef]); Check(Descendants(Popup()).OfType<WF.TextBox>().Single(c => c.Name == retained.Id).Handle == retainedHandle, "Borrowed group reopens with the same native field"); await RequestHostClose("reopened borrowed group");
                int beforePreview = previews, beforeCancellation = cancellations;
                ribbon.Refresh(); await Click(ribbon, record.Qat[galleryRef]); Check(galleryRef.IsDropDownOpen, "The gallery reference opens its own popup"); await Key("{END}"); Check(previews > beforePreview, "Gallery keyboard navigation starts a real preview");
                await RequestHostClose("gallery popup"); Check(!galleryRef.IsDropDownOpen && gallery.SelectedItem == gallery.Items[0] && commits == 0 && cancellations > beforeCancellation, "Host close request cancels gallery preview without committing selection");
                ribbon.QuickAccessMaxWidth = 28; ribbon.Refresh(); await Click(ribbon, record.Overflow); window = (WF.ToolStripDropDown)Popup().TopLevelControl!; await RequestHostClose("QAT overflow"); Check(!window.Visible, "Cancelled host close leaves no QAT overflow window");
                ribbon.IsMinimized = true; ribbon.Refresh(); await Click(ribbon, record.Tab); window = (WF.ToolStripDropDown)Popup().TopLevelControl!; await RequestHostClose("temporary ribbon body"); Check(!window.Visible && ribbon.IsMinimized && form.Visible, "Cancelled host close preserves minimized ribbon state");
                Check(unavailable == 0, "Popup refinement input raises zero unavailable-element exceptions");
                ribbon.IsMinimized = false; ribbon.QuickAccessMaxWidth = 320; await OpenMenu();
                finalCloseCount = hostCloseRequests + 1; allowClose = true; awaitingHostClose = true; completed = true; closeDeadline.Start(); Guard(); WF.SendKeys.SendWait("%{F4}");
            }
            catch (Exception error) { failure = error; log.Add("FAIL: " + error); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; if (!awaitingHostClose || failure is not null) { allowClose = true; form.Close(); } }
        };
        try { WF.Application.Run(form); } finally { AppDomain.CurrentDomain.FirstChanceException -= exceptions; }
        closeDeadline.Stop();
        if (completed && failure is null)
        {
            if (form.IsDisposed && hostCloseRequests == finalCloseCount) log.Add($"PASS: {(flat ? "Neutral" : "Office")} popup refinements and permitted host close at {ribbon.DeviceDpi} DPI");
            else { completed = false; failure = new InvalidOperationException("Host close did not complete exactly once."); log.Add("FAIL: " + failure); }
        }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log); if (failure is not null) Console.Error.WriteLine(failure); else Console.WriteLine(string.Join(Environment.NewLine, log)); return completed && failure is null ? 0 : 1;
        void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        void Guard() { if (GetForegroundWindow() != form.Handle && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown) throw new InvalidOperationException("Popup fixture lost foreground."); }
        void EqualRows() { Check(menu.Items.Where(i => i.Visible && i is not RibbonMenuSeparatorItem).Select(i => record.Rows[i].Height).Distinct().Count() == 1, "All interactive menu rows use the tallest measured height"); Check(record.Rows[separator].Height < record.Rows[tallRow].Height && record.Header.Bottom <= record.Rows[shortRow].Top, "Separator and header stay separate from uniform command rows"); }
        async Task OpenMenu() { ribbon.Refresh(); await Click(ribbon, record.Rows[menu]); Check(menu.IsDropDownOpen, "Source menu opens by pointer"); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(100); }
        async Task RequestHostClose(string scope)
        {
            int before = hostCloseRequests; var active = WF.Control.FromHandle(GetForegroundWindow()) as WF.ToolStripDropDown;
            await Key("%{F4}"); Check(hostCloseRequests == before + 1 && form.Visible && (active is null || !active.Visible), $"{scope}: Alt+F4 reaches the cancellable host close exactly once and dismisses its popup");
        }
        async Task Click(WF.Control control, Rectangle row) { Guard(); row = Rectangle.Intersect(row, control.ClientRectangle); if (row.IsEmpty) throw new InvalidOperationException("Pointer target is outside the viewport."); WF.Cursor.Position = control.PointToScreen(new(row.Left + row.Width / 2, row.Top + row.Height / 2)); await Task.Delay(40); mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(90); }
        async Task Capture(string name) { await Task.Delay(60); Guard(); var bounds = form.Bounds; if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds); using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var g = Graphics.FromImage(bitmap); g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png")); }
    }
    private static IEnumerable<WF.Control> Descendants(WF.Control root) => root.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a popup.");
    private sealed class Recording { internal readonly Dictionary<RibbonItem, Rectangle> Rows = []; internal readonly Dictionary<RibbonComboBoxItem, Rectangle> Fields = []; internal readonly Dictionary<RibbonElement, Rectangle> Qat = []; internal Rectangle Header, Overflow, Tab; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize size, bool menu) { r.Rows[i] = c.Bounds; base.DrawItem(c, i, size, menu); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem i, Rectangle field) { r.Rows[i] = c.Bounds; base.DrawTextBox(c, i, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem i, Rectangle field) { r.Rows[i] = c.Bounds; r.Fields[i] = field; base.DrawComboBox(c, i, field); }
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { r.Header = c.Bounds; base.DrawMenuHeader(c, text); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { if (!menu) r.Qat[e] = c.Bounds; base.DrawQuickAccessItem(c, e, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize size, bool menu) { r.Rows[i] = c.Bounds; base.DrawItem(c, i, size, menu); }
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem i, Rectangle field) { r.Rows[i] = c.Bounds; base.DrawTextBox(c, i, field); }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem i, Rectangle field) { r.Rows[i] = c.Bounds; r.Fields[i] = field; base.DrawComboBox(c, i, field); }
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { r.Header = c.Bounds; base.DrawMenuHeader(c, text); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { if (!menu) r.Qat[e] = c.Bounds; base.DrawQuickAccessItem(c, e, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tab = c.Bounds; base.DrawTab(c, tab, selected); }
    }
}
