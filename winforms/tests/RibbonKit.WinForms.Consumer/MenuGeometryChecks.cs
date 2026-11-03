using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Copied to an isolated folder and compiled against only the packed public API.
internal static class MenuGeometryChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, string mode, bool verticalSpacing = false)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var log = new List<string>(); var record = new Record();
        using RibbonRenderer renderer = mode.StartsWith("Flat") ? new Flat(record) : new Office(record);
        var material = mode.Contains("Mica") ? RibbonWindowBackdrop.Mica : mode.Contains("Acrylic") ? RibbonWindowBackdrop.Acrylic : RibbonWindowBackdrop.None;
        var density = mode.EndsWith("Touch") ? RibbonDensity.Touch : RibbonDensity.Compact;
        using var form = new RibbonForm { Text = "Independent native menu geometry", Size = new(1000, 720), StartPosition = FormStartPosition.CenterScreen, Backdrop = material };
        var ribbon = new RibbonControl { Density = density, Renderer = renderer, AnimationLevel = RibbonAnimationLevel.Subtle, QuickAccessMaxWidth = 28 };
        form.Controls.Add(new TextBox { Dock = DockStyle.Fill, Multiline = true, Text = "Application-owned document" }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var tab = new RibbonTab { Text = "Home", KeyTip = "H" }; var group = new RibbonGroup { Text = "Commands" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var menu = new RibbonDropDownItem { Text = "Geometry menu", KeyTip = "M", SimplifiedPlacement = RibbonSimplifiedPlacement.Overflow };
        var action = new RibbonButtonItem { Text = "Open document", KeyTip = "O", Icon = RibbonStockIcons.Create("open") };
        var check = new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true };
        var editorItem = new RibbonTextBoxItem { Text = "Name", Value = "Retained", InputWidth = 150 };
        var nested = new RibbonDropDownItem { Text = "Export", KeyTip = "X" }; var leaf = new RibbonButtonItem { Text = "PDF document", KeyTip = "P", Icon = RibbonStockIcons.Create("save") }; nested.Items.Add(leaf);
        var separator = new RibbonMenuSeparatorItem();
        if (verticalSpacing)
        {
            action.MenuDescription = "First line\nSecond line"; check.MenuDescription = "Keep fonts and paragraph spacing.";
            menu.Items.AddRange(action, separator, check, editorItem, nested);
        }
        else menu.Items.AddRange(action, check, editorItem, nested);
        group.Items.Add(menu);
        var moreAction = new RibbonButtonItem { Text = "More action", Icon = RibbonStockIcons.Create("save"), SimplifiedPlacement = RibbonSimplifiedPlacement.Overflow }; group.Items.Add(moreAction);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = action });
        for (int i = 0; i < 22; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Overflow command " + i });
        int calls = 0; action.Click += (_, _) => calls++; leaf.Click += (_, _) => calls++;
        bool complete = false; var pointer = Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(100); Guard();
                Check(form.EffectiveBackdrop == material, "Requested material is effective: " + form.EffectiveBackdrop);
                await Open(); var first = Surface(); var defaultSize = first.Size; var defaultRow = record.Items[action];
                await Capture("default"); var editor = Controls(first).OfType<TextBox>().Single(); var handle = editor.Handle;
                editor.Focus(); editor.Select(2, 3); await Key("{END}!"); Check(editor.Text == "Retained!", "Native editor accepts input");
                ribbon.Theme = ribbon.Theme with { MenuMinimumRowHeight = 70, MenuIconSize = 36, MenuLeadingPadding = 13, MenuTrailingPadding = 17, MenuIconTextGap = 15 };
                Check(!menu.IsDropDownOpen, "Theme changes dismiss stale popup geometry"); await Open(); var custom = Surface();
                Check((verticalSpacing ? custom.Height >= defaultSize.Height : custom.Height > defaultSize.Height) && record.Items[action].Height >= Scale(70), "Overrides respect row minima and content-driven popup extent");
                Check(editor.Handle == handle && editor.Text == "Retained!" && Controls(custom).Contains(editor), "Override retains native HWND, text and owner");
                var row = record.Items[action]; var icon = record.Images[action]; var title = record.Texts[action.Text];
                Check(icon.Left == row.Left + Scale(13) && icon.Width == Scale(36) && title.Left == row.Left + Scale(64), "Actual paint uses the resolved icon and text slots");
                Check(title.Width == row.Width - Scale(81), "Trailing padding participates in text bounds");
                Check(custom.ClientRectangle.Contains(editor.Parent!.Bounds) && editor.Parent.Height == row.Height && editor.Parent.ClientRectangle.Contains(editor.Bounds), "Native field stays inside its measured row"); await Capture("override");
                await Click(custom, record.Items[check]);
                Check(!check.Checked && ReferenceEquals(Surface(), custom), "Checkbox pointer activation changes state and retains the same dropdown");
                await Capture("checkbox-pointer");
                await Key(" "); Check(check.Checked && ReferenceEquals(Surface(), custom), "Checkbox Space activation toggles back without closing the dropdown");
                await Capture("checkbox-keyboard");
                Check(editor.Handle == handle && editor.Text == "Retained!", "Checkbox activation retains native editor HWND and text");
                if (verticalSpacing)
                {
                    var horizontalTheme = ribbon.Theme; var horizontalSize = custom.Size;
                    ribbon.Theme = WithVertical(ribbon.Theme); Check(!menu.IsDropDownOpen, "Vertical metrics dismiss stale popup geometry");
                    await Open(); custom = Surface(); row = record.Items[action]; title = record.Texts[action.Text]; var description = record.Texts[action.MenuDescription];
                    Check(custom.Height > horizontalSize.Height, "Vertical overrides change measured rows and popup extent");
                    Check(title.Top == row.Top + Scale(14) && description.Top == title.Bottom + Scale(10) && description.Bottom <= row.Bottom - Scale(14), "Title and description paint share scaled padding and gap");
                    Check(record.Items[separator].Height == Scale(20), "Decorative separator has its resolved row height");
                    await Click(custom, record.Items[separator]); Check(menu.IsDropDownOpen && calls == 0 && check.Checked, "Separator remains noninteractive and keeps the menu open");
                    await Click(custom, record.Items[check]); Check(!check.Checked && ReferenceEquals(Surface(), custom), "Described checkbox toggles inside the same resized popup");
                    await Key(" "); Check(check.Checked && ReferenceEquals(Surface(), custom), "Described checkbox Space toggles back without dismissal");
                    Check(editor.Handle == handle && editor.Text == "Retained!" && editor.Parent!.Height == row.Height, "Vertical metrics retain native editor HWND, text and measured row");
                    await Capture("vertical-override");
                    ribbon.Theme = ribbon.Theme with { MenuIconVerticalPadding = null, MenuDescriptionVerticalPadding = null, MenuDescriptionGap = null, MenuSeparatorHeight = null };
                    await Open(); Check(ribbon.Theme == horizontalTheme && Surface().Size == horizontalSize && record.Items[separator].Height == Scale(9), "Vertical-only reset restores exact bounds and preserves horizontal values");
                    await Capture("vertical-reset"); ribbon.Theme = WithVertical(ribbon.Theme); await Open();
                }
                nested.ShowDropDown(); await Task.Delay(170); Surface().Refresh();
                Check(record.Items[leaf].Height >= Scale(70), "Nested submenu uses the same theme metrics"); await Capture("nested");
                await Key("{ENTER}"); Check(calls == 1 && !menu.IsDropDownOpen, "Nested keyboard action invokes once and dismisses the stack");
                await Open(); await Click(Surface(), record.Items[action]); Check(calls == 2 && !menu.IsDropDownOpen, "Pointer hit in the resized row invokes once");
                await Key("{F10}"); await Key("h"); await Key("m"); Check(menu.IsDropDownOpen, "KeyTips open the same measured menu"); await Key("{ESC}");
                ribbon.Theme = ribbon.Theme with { MenuMinimumRowHeight = null, MenuIconSize = null, MenuLeadingPadding = null, MenuTrailingPadding = null, MenuIconTextGap = null,
                    MenuIconVerticalPadding = null, MenuDescriptionVerticalPadding = null, MenuDescriptionGap = null, MenuSeparatorHeight = null };
                await Open(); Check(Surface().Size == defaultSize && record.Items[action] == defaultRow, "Null reset restores exact default geometry");
                Check(editor.Handle == handle && editor.Text == "Retained!", "Reset retains native editor state"); await Capture("reset"); await Key("{ESC}");
                ribbon.Theme = ribbon.Theme with { MenuMinimumRowHeight = 70, MenuIconSize = 36, MenuLeadingPadding = 13, MenuTrailingPadding = 17, MenuIconTextGap = 15 };
                if (verticalSpacing) ribbon.Theme = WithVertical(ribbon.Theme);
                ribbon.Refresh(); await Click(ribbon, record.Overflow); var overflow = Surface(); overflow.Refresh();
                Check(record.QatRows.Values.Any(r => r.Height >= Scale(70)), "QAT overflow uses resolved row heights");
                Check(Screen.FromControl(overflow).WorkingArea.Contains(overflow.TopLevelControl!.Bounds), "Overflow popup is constrained to the work area");
                await Key("{END}"); await Capture("qat-last"); await Key("{HOME}"); await Key("{ESC}");
                ribbon.QuickAccessVisible = false; ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.Refresh(); record.QatRows.Clear();
                await Click(ribbon, record.Overflow); Surface().Refresh();
                Check(record.QatRows.Values.Any(r => r.Height >= Scale(70)), "Simplified More Commands uses the same resolver"); await Capture("simplified-more"); await Key("{ESC}");
                Check(editor.Handle == handle && editor.Text == "Retained!", "Layout changes keep the one native editor");
                log.Add($"PASS: public package; actual DPI={ribbon.DeviceDpi}; {mode}; verticalSpacing={verticalSpacing}; Classic/Simplified, nested menu, QAT/More, reset, native input and retained editor"); complete = true;
            }
            catch (Exception error) { log.Add("FAIL: " + error); }
            finally { menu.CloseDropDown(); form.Close(); }
        };
        try { Application.Run(form); } finally { Cursor.Position = pointer; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log); return complete ? 0 : 1;
        int Scale(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && Control.FromHandle(GetForegroundWindow()) is not ToolStripDropDown) throw new InvalidOperationException("Fixture lost foreground; input stopped."); }
        async Task Key(string keys) { Guard(); SendKeys.SendWait(keys); await Task.Delay(100); Guard(); }
        async Task Open() { Guard(); menu.ShowDropDown(); await Task.Delay(170); Surface().Refresh(); }
        async Task Click(Control control, Rectangle bounds)
        {
            Guard(); bounds.Intersect(control.ClientRectangle); Check(bounds.Width > 0 && bounds.Height > 0, "Pointer target is visible");
            Cursor.Position = control.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(40); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(170); Guard();
        }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); var surface = Surface(); var bounds = Rectangle.Union(form.Bounds, surface.TopLevelControl!.Bounds);
            using var pixels = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(pixels);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); pixels.Save(Path.Combine(directory, name + ".png"));
        }
        Control Surface() => Control.FromHandle(GetForegroundWindow()) is ToolStripDropDown window ? window.Items.OfType<ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected native menu popup");
    }
    private static RibbonTheme WithVertical(RibbonTheme theme) => theme with
    { MenuIconVerticalPadding = 14, MenuDescriptionVerticalPadding = 14, MenuDescriptionGap = 10, MenuSeparatorHeight = 20 };
    private static IEnumerable<Control> Controls(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Controls(c)));
    private sealed class Record
    {
        internal Rectangle Overflow;
        internal readonly Dictionary<RibbonItem, Rectangle> Items = [], Images = [];
        internal readonly Dictionary<string, Rectangle> Texts = [];
        internal readonly Dictionary<RibbonElement, Rectangle> QatRows = [];
    }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { if (menu) r.Items[i] = c.Bounds; base.DrawItem(c, i, s, menu); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem i, RibbonItemSize s) { r.Images[i] = c.Bounds; base.DrawItemImage(c, i, s); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Texts[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement i, bool menu) { if (menu) r.QatRows[i] = c.Bounds; base.DrawQuickAccessItem(c, i, menu); }
    }
    private sealed class Flat(Record r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { if (menu) r.Items[i] = c.Bounds; base.DrawItem(c, i, s, menu); }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem i, RibbonItemSize s) { r.Images[i] = c.Bounds; base.DrawItemImage(c, i, s); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Texts[text] = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement i, bool menu) { if (menu) r.QatRows[i] = c.Bounds; base.DrawQuickAccessItem(c, i, menu); }
    }
}
