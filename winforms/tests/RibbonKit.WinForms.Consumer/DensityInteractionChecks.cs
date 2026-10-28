using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public-only target geometry, native editing and pointer/keyboard checks. No
// Showcase resource, touch hardware, WPF hosting or application-owned layout glue.
internal static class DensityInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer painter = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent Compact/Touch consumer", Width = 1320, Height = 700, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage }; ribbon.FileMenu.Visible = true;
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; var fields = new RibbonGroup { Text = "Fields", CanResize = false };
        var name = new RibbonTextBoxItem { Id = "density.name", Text = "Name", KeyTip = "N" }; var combo = new RibbonComboBoxItem { Id = "density.font", Text = "Font", IsEditable = true, KeyTip = "F" }; combo.Items.AddRange(Enumerable.Range(0, 12).Select(i => "Font " + i).ToArray()); combo.SelectedIndex = 3;
        name.ValueValidating += (_, e) => e.Cancel = name.Value == "";
        var menu = new RibbonDropDownItem { Text = "Commands", KeyTip = "M", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) }; var choice = new RibbonButtonItem { Text = "Apply", KeyTip = "A" }; menu.Items.Add(choice);
        fields.Items.AddRange(name, combo, menu); home.Groups.Add(fields);
        var styles = new RibbonGroup { Text = "Gallery", CanResize = false }; var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true, InRibbonColumnCount = 2, ColumnCount = 2, MaxVisibleRows = 2, TileWidth = 64, TileHeight = 72 }; gallery.Items.AddRange(Enumerable.Range(0, 10).Select(i => new RibbonGalleryTile { Text = "Style " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0]; styles.Items.Add(gallery); home.Groups.Add(styles);
        var actions = new RibbonGroup { Text = "Three rows", CanResize = false }; var rows = Enumerable.Range(0, 6).Select(i => new RibbonButtonItem { Text = "Action " + i, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) }).ToArray(); actions.Items.AddRange(rows); home.Groups.Add(actions); ribbon.Tabs.Add(home);
        using var splitImage = new Bitmap(32, 32); using (var g = Graphics.FromImage(splitImage)) { using var pen = new Pen(Color.SteelBlue, 2); g.DrawRectangle(pen, 5, 3, 22, 26); }
        var splitGroup = new RibbonGroup { Text = "Vertical split", CanResize = false }; var split = new RibbonSplitButtonItem { Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical, Image = splitImage, KeyTip = "V", DropDownKeyTip = "P" }; var splitChoice = new RibbonButtonItem { Text = "Paste special" }; split.Items.Add(splitChoice); splitGroup.Items.Add(split); home.Groups.Add(splitGroup);
        var densityMenu = new RibbonDropDownItem { Text = "Input mode", DropDownHeader = "Optimize spacing between commands", MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
        var mouseMode = new RibbonRadioButtonItem { Text = "Mouse", Icon = new ModeIcon(), MenuDescription = "Standard ribbon and commands.\nOptimized for use with mouse." };
        var touchMode = new RibbonRadioButtonItem { Text = "Touch", Icon = new ModeIcon(), MenuDescription = "More space between commands.\nOptimized for use with touch." }; densityMenu.Items.AddRange(mouseMode, touchMode);
        var densityGroup = new RibbonGroup { Text = "Input mode", CanResize = false }; densityGroup.Items.Add(densityMenu); home.Groups.Add(densityGroup);
        void SyncDensity() { mouseMode.Checked = ribbon.Density == RibbonDensity.Compact; touchMode.Checked = !mouseMode.Checked; }
        mouseMode.Click += (_, _) => { ribbon.Density = RibbonDensity.Compact; SyncDensity(); }; touchMode.Click += (_, _) => { ribbon.Density = RibbonDensity.Touch; SyncDensity(); }; ribbon.DensityChanged += (_, _) => SyncDensity(); SyncDensity();
        var comboRef = new RibbonQuickAccessComboBoxReference { Source = combo }; var galleryRef = new RibbonQuickAccessDropDownReference { Source = gallery }; var commandRef = new RibbonQuickAccessReference { Source = rows[0] }; ribbon.QuickAccessItems.AddRange(comboRef, galleryRef, commandRef);
        var densityRef = new RibbonQuickAccessDropDownReference { Source = densityMenu }; ribbon.QuickAccessItems.Add(densityRef);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Density changes target geometry and spacing. Text size, commands, selection, bindings and native editor identity remain the same. Application content is separate." }; form.Controls.Add(document); form.Controls.Add(ribbon);
        using var binding = new WF.BindingSource(); var state = new State(); binding.DataSource = state; name.DataBindings.Add(new WF.Binding(nameof(name.Value), binding, nameof(State.Name), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        using var page = new WF.Panel(); var pageField = new WF.TextBox { Text = "Application page", Location = new(30, 90), Width = 220 }; page.Controls.Add(pageField); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        int commits = 0, commands = 0, unavailable = 0, splitClicks = 0, splitMenuClicks = 0; gallery.SelectionCommitted += (_, _) => commits++; choice.Click += (_, _) => commands++; rows[0].Click += (_, _) => commands++; split.Click += (_, _) => splitClicks++; splitChoice.Click += (_, _) => splitMenuClicks++;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); ribbon.Refresh(); int compactHeight = ribbon.Height; var font = ribbon.Font; await Capture("compact");
                name.PerformClick(); var editor = Tree(ribbon).OfType<WF.TextBox>().Single(c => c.Name == name.Id); var nativeCombo = Tree(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint nameHandle = editor.Handle, comboHandle = nativeCombo.Handle;
                await Key("^a{BACKSPACE}"); ribbon.Density = RibbonDensity.Touch; Check(ribbon.Density == RibbonDensity.Compact && editor.Focused, "Invalid native input blocks a density change without losing focus");
                await Key("Density document"); editor.Select(2, 4); ribbon.Density = RibbonDensity.Touch; ribbon.Refresh();
                Check(ribbon.Height > compactHeight && ribbon.Font.Equals(font) && ribbon.SelectedTab == home, "Touch enlarges geometry without changing text size or selection");
                Check(editor.Handle == nameHandle && nativeCombo.Handle == comboHandle && editor.SelectionStart == 2 && editor.SelectionLength == 4 && editor.Focused && state.Name == "Density document", "Density preserves native handles, caret, focus and binding");
                Check(record.Rows[rows[0]].Height >= Scale(44) && record.Rows[rows[0]].Left == record.Rows[rows[2]].Left && record.Rows[rows[3]].Left > record.Rows[rows[0]].Left, "Touch keeps exactly three full command rows per column");
                Check(record.LastDensity == RibbonDensity.Touch, "Borrowed renderer contexts carry the owning density"); await Capture("touch");
                Check(record.SplitPrimary.Bottom <= record.SplitCaption.Top && record.SplitDropDown.Contains(record.SplitCaption) && record.SplitDropDown.Contains(record.SplitIndicator), "Touch vertical split keeps its label and centered indicator inside the lower action");
                await Click(ribbon, record.SplitPrimary); Check(splitClicks == 1 && !split.IsDropDownOpen, "Touch vertical split icon executes only the primary action");
                await Click(ribbon, record.SplitCaption); Check(split.IsDropDownOpen && splitClicks == 1, "Touch vertical split label opens only the menu"); await Key("{ENTER}"); Check(splitMenuClicks == 1 && !split.IsDropDownOpen, "Touch split menu commits its original command once");
                ribbon.Refresh(); await Click(ribbon, record.SplitIndicator); Check(split.IsDropDownOpen && splitClicks == 1, "Touch vertical split indicator opens only the menu"); await Key("{ESC}");
                combo.PerformClick(); nativeCombo.Select(1, 3); ribbon.Density = RibbonDensity.Compact; Check(nativeCombo.Handle == comboHandle && nativeCombo.SelectionStart == 1 && nativeCombo.SelectionLength == 3, "Editable ComboBox selection survives a return to Compact"); ribbon.Density = RibbonDensity.Touch; ribbon.Refresh();
                await Click(ribbon, record.Qat[comboRef]); Check(comboRef.IsDropDownOpen && record.ChoiceRow >= Scale(44), "QAT chooser uses Touch native list rows"); await Key("{END}{ENTER}"); Check(combo.SelectedIndex == 11 && !comboRef.IsDropDownOpen, "Touch QAT chooser commits the original source once");
                menu.ShowDropDown(); await Task.Delay(80); Popup().Refresh(); Check(record.Rows[choice].Height >= Scale(44), "Command menus use Touch row geometry"); await Key("{ENTER}"); Check(commands == 1 && !menu.IsDropDownOpen, "Touch menu pointer/keyboard routes to its original command");
                ribbon.Refresh(); await Click(ribbon, record.Qat[galleryRef]); await Key("{END}{ENTER}"); Check(commits == 1 && gallery.SelectedItem == gallery.Items[9], "Touch QAT gallery keeps source selection and exact-once commit");
                menu.ShowDropDown(); await Task.Delay(60); ribbon.Density = RibbonDensity.Compact; Check(!menu.IsDropDownOpen, "Changing density closes stale popup geometry");
                ribbon.Density = RibbonDensity.Touch; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.Refresh(); await Capture("touch-below-qat"); Check(record.Qat[commandRef].Height >= Scale(44), "BelowRibbon QAT follows Touch density");
                await Click(ribbon, record.Qat[commandRef]); Check(commands == 2, "Touch toolbar retains its original command routing");
                ribbon.IsBackstageOpen = true; await Task.Delay(80); pageField.Focus(); nint pageHandle = pageField.Handle; int touchRow = record.BackstageRow;
                ribbon.Density = RibbonDensity.Compact; await Task.Delay(70); Check(ribbon.IsBackstageOpen && pageField.Handle == pageHandle && pageField.Focused && pageField.Text == "Application page" && record.BackstageRow < touchRow, "Backstage navigation updates while its application-owned page stays intact"); ribbon.IsBackstageOpen = false;
                ribbon.Density = RibbonDensity.Touch; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 28; ribbon.Refresh(); await Click(ribbon, record.Overflow); var overflow = Popup(); overflow.Refresh();
                Check(record.MenuQat[galleryRef].Height >= Scale(44), "QAT overflow rows use the same Touch metrics"); await Click(overflow, record.MenuQat[galleryRef]); await Key("{HOME}{ENTER}"); Check(commits == 2 && gallery.SelectedItem == gallery.Items[0] && !overflow.Visible, "Touch overflow gallery dismisses both levels after source commit");
                ribbon.Density = RibbonDensity.Compact; Check(ribbon.Height == compactHeight && name.Value == state.Name && nativeCombo.Handle == comboHandle, "Returning to Compact restores original extent and retained data");
                ribbon.QuickAccessMaxWidth = 320; ribbon.Refresh(); await Click(ribbon, record.Qat[densityRef]); var chooser = Popup(); chooser.Refresh();
                Check(record.MenuHeader.Height > 0 && record.Rows[mouseMode].Top >= record.MenuHeader.Bottom && mouseMode.Checked && !touchMode.Checked, "Illustrated QAT chooser keeps a noninteractive heading above its selected Mouse row");
                await Click(chooser, record.MenuHeader); Check(densityRef.IsDropDownOpen && ribbon.Density == RibbonDensity.Compact, "Clicking the chooser heading performs no action");
                await Click(chooser, record.Rows[touchMode]); Check(ribbon.Density == RibbonDensity.Touch && touchMode.Checked && !mouseMode.Checked && !densityRef.IsDropDownOpen, "Illustrated Touch choice switches geometry and selected state together");
                ribbon.Refresh(); await Click(ribbon, record.Qat[densityRef]); chooser = Popup(); chooser.Refresh(); await Capture("illustrated-touch-chooser");
                Check(record.Rows[touchMode].Height >= Scale(64), "Illustrated choice rows retain room for artwork and two description lines");
                await Key("{HOME}{ENTER}"); Check(ribbon.Density == RibbonDensity.Compact && mouseMode.Checked && !touchMode.Checked && !densityRef.IsDropDownOpen, "Keyboard skips the heading and restores Mouse density through the original source");
                Check(unavailable == 0, "Density changes and native popup input raise zero unavailable-element exceptions"); log.Add($"PASS: {(flat ? "Neutral" : "Office")} density at {form.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception e) { error = e; log.Add("FAIL: " + e); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        try { WF.Application.Run(form); } finally { AppDomain.CurrentDomain.FirstChanceException -= exceptions; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log); if (error is not null) Console.Error.WriteLine(error); else Console.WriteLine(string.Join(Environment.NewLine, log)); return completed && error is null ? 0 : 1;
        int Scale(int value) => (int)Math.Round(value * ribbon.DeviceDpi / 96d);
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown) throw new InvalidOperationException("Validation Form lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(110); }
        async Task Click(WF.Control c, Rectangle bounds)
        {
            Guard(); bounds = Rectangle.Intersect(c.ClientRectangle, bounds); Check(!bounds.IsEmpty, "Pointer target is visible"); WF.Cursor.Position = c.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(60); Guard(); mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); var bounds = form.Bounds; using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control c) => c.Controls.Cast<WF.Control>().SelectMany(child => new[] { child }.Concat(Tree(child)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library popup.");
    private sealed class State : INotifyPropertyChanged
    {
        private string _name = "Document"; public string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); } } public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonItem, Rectangle> Rows = [];
        internal readonly Dictionary<RibbonElement, Rectangle> Qat = [], MenuQat = [];
        internal Rectangle Overflow, SplitPrimary, SplitDropDown, SplitCaption, SplitIndicator, MenuHeader; internal int BackstageRow, ChoiceRow; internal RibbonDensity LastDensity; internal bool DrawingSplit;
        internal void Item(RibbonPaintContext c, RibbonItem item) { Rows[item] = c.Bounds; LastDensity = c.Density; }
        internal void Entry(RibbonPaintContext c, RibbonElement entry, bool menu) => (menu ? MenuQat : Qat)[entry] = c.Bounds;
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { r.MenuHeader = c.Bounds; base.DrawMenuHeader(c, text); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.SplitPrimary = primary.Bounds; r.SplitDropDown = dropDown.Bounds; r.DrawingSplit = true; try { base.DrawSplitItem(primary, dropDown, item, size, menu); } finally { r.DrawingSplit = false; } }
        public override void DrawText(RibbonPaintContext c, string text, Color color, WF.TextFormatFlags flags) { if (r.DrawingSplit) r.SplitCaption = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { if (r.DrawingSplit) r.SplitIndicator = c.Bounds; base.DrawDropDownArrow(c, color); }
        public override void DrawComboBoxItem(RibbonPaintContext c, RibbonComboBoxItem item, int index, bool selected) { r.ChoiceRow = c.Bounds.Height; base.DrawComboBoxItem(c, item, index, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item); base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawBackstageEntry(RibbonPaintContext c, RibbonItem? item, bool selected) { if (item is not null) r.BackstageRow = c.Bounds.Height; base.DrawBackstageEntry(c, item, selected); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { r.MenuHeader = c.Bounds; base.DrawMenuHeader(c, text); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.SplitPrimary = primary.Bounds; r.SplitDropDown = dropDown.Bounds; r.DrawingSplit = true; try { base.DrawSplitItem(primary, dropDown, item, size, menu); } finally { r.DrawingSplit = false; } }
        public override void DrawText(RibbonPaintContext c, string text, Color color, WF.TextFormatFlags flags) { if (r.DrawingSplit) r.SplitCaption = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { if (r.DrawingSplit) r.SplitIndicator = c.Bounds; base.DrawDropDownArrow(c, color); }
        public override void DrawComboBoxItem(RibbonPaintContext c, RibbonComboBoxItem item, int index, bool selected) { r.ChoiceRow = c.Bounds.Height; base.DrawComboBoxItem(c, item, index, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item); base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawBackstageEntry(RibbonPaintContext c, RibbonItem? item, bool selected) { if (item is not null) r.BackstageRow = c.Bounds.Height; base.DrawBackstageEntry(c, item, selected); }
    }
    private sealed class ModeIcon : IRibbonIcon { public void Draw(Graphics g, Rectangle bounds, RibbonIconContext context) { using var b = new SolidBrush(Color.SteelBlue); g.FillEllipse(b, bounds); } }
}
