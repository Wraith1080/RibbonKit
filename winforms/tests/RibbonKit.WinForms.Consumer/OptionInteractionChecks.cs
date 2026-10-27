using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class OptionInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool flat)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var results = new List<string>(); Exception? error = null; bool completed = false;
        using var form = new WF.Form { Text = "Public check and radio options", Width = 900, Height = 450, StartPosition = WF.FormStartPosition.CenterScreen, AutoScaleMode = WF.AutoScaleMode.Dpi };
        var bounds = new Dictionary<RibbonItem, Rectangle>(); var indicators = new Dictionary<RibbonItem, Rectangle>();
        var collapsed = new Dictionary<RibbonGroup, Rectangle>();
        using RibbonRenderer renderer = flat ? new FlatRecorder(bounds, indicators, collapsed) : new OfficeRecorder(bounds, indicators, collapsed);
        var ribbon = new RibbonControl { Renderer = renderer };
        var tab = new RibbonTab { Text = "Options", KeyTip = "O" };
        var flags = new RibbonGroup { Text = "Show", KeyTip = "S" }; var modes = new RibbonGroup { Text = "View", KeyTip = "V" }; var menus = new RibbonGroup { Text = "More", KeyTip = "M" };
        var ruler = new RibbonCheckBoxItem { Text = "Ruler", KeyTip = "R" };
        var grid = new RibbonCheckBoxItem { Text = "Gridlines", ThreeState = true, CheckState = WF.CheckState.Indeterminate, KeyTip = "G" };
        var disabled = new RibbonCheckBoxItem { Text = "Unavailable", Enabled = false };
        var print = new RibbonRadioButtonItem { Text = "Print layout", GroupName = "layout", KeyTip = "P" };
        var web = new RibbonRadioButtonItem { Text = "Web layout", GroupName = "layout", KeyTip = "W" };
        var menu = new RibbonDropDownItem { Text = "More options", KeyTip = "C" };
        var popupRuler = new RibbonCheckBoxItem { Text = "Ruler", KeyTip = "R" };
        var nested = new RibbonDropDownItem { Text = "Other views", KeyTip = "V" };
        var draft = new RibbonRadioButtonItem { Text = "Draft", GroupName = "layout", KeyTip = "D" };
        nested.Items.Add(draft); menu.Items.AddRange(popupRuler, nested);
        flags.Items.AddRange(ruler, grid, disabled); modes.Items.AddRange(print, web); menus.Items.Add(menu);
        tab.Groups.AddRange(flags, modes, menus); ribbon.Tabs.Add(tab);
        using var source = new WF.BindingSource(); var state = new State(); source.DataSource = state;
        foreach (var check in new[] { ruler, popupRuler }) check.DataBindings.Add(new WF.Binding(nameof(check.Checked), source, nameof(state.Ruler), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        print.DataBindings.Add(new WF.Binding(nameof(print.Checked), source, nameof(state.Print), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        web.DataBindings.Add(new WF.Binding(nameof(web.Checked), source, nameof(state.Web), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        grid.DataBindings.Add(new WF.Binding(nameof(grid.CheckState), source, nameof(state.Grid), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Independent check/radio consumer" };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        int clicks = 0; ruler.Click += (_, _) => clicks++;
        var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(220);
                Check(GetForegroundWindow() == form.Handle, "Validation Form owns foreground focus");
                document.Focus(); ribbon.Refresh(); Capture(ribbon, "idle");
                Symmetric(ruler); Symmetric(print);
                Check(grid.CheckState == WF.CheckState.Indeterminate, "Indeterminate state survives enum binding");
                CenteredMark(grid);
                await Click(ribbon, bounds[ruler]);
                Check(ruler.Checked && popupRuler.Checked && state.Ruler && clicks == 1, "Pointer activation updates the shared boolean binding exactly once");
                CenteredMark(ruler);
                await Key(" "); Check(!ruler.Checked && !state.Ruler && clicks == 2,
                    $"Space follows the same check and click path (checked={ruler.Checked}, source={state.Ruler}, clicks={clicks}, focus={ribbon.Focused}, active={form.ActiveControl?.GetType().Name}, modifiers={WF.Control.ModifierKeys})");
                await Click(ribbon, bounds[grid]); Check(state.Grid == WF.CheckState.Unchecked, "Three-state checkbox cycles indeterminate to unchecked");
                await Key(" "); Check(state.Grid == WF.CheckState.Checked, "Space checks the three-state option");
                await Key(" "); Check(state.Grid == WF.CheckState.Indeterminate, "Space reaches indeterminate without losing binding");
                await Click(ribbon, bounds[disabled]); Check(!disabled.Checked, "Disabled pointer input leaves the option unchanged");
                await Click(ribbon, bounds[web]);
                Check(web.Checked && !print.Checked && state.Web && !state.Print, "Selecting a radio clears its peer and both bound values stay consistent");
                await Click(ribbon, bounds[web]); Check(web.Checked, "Clicking the selected radio retains selection");
                await Key("{LEFT} "); Check(print.Checked && !web.Checked, "Arrow navigation and Space select the other radio");
                await Key("{F10}or"); Check(ruler.Checked && state.Ruler, "Checkbox KeyTip invokes the same bindable state path");
                await Click(ribbon, bounds[menu]); var surface = Popup(); Capture(surface, "menu");
                await Click(surface, bounds[popupRuler]); Check(!ruler.Checked && !state.Ruler, "Popup checkbox updates the source and its ribbon counterpart");
                menu.ShowDropDown(); await Task.Delay(50); surface = Popup();
                await Click(surface, bounds[nested]); surface = Popup(); Capture(surface, "nested-radio");
                await Click(surface, bounds[draft]);
                Check(draft.Checked && !print.Checked && !web.Checked && !state.Print && !state.Web, "Named radio groups retain exclusivity across nested popups");
                form.Width = 240; await Task.Delay(100); ribbon.Refresh();
                Check(collapsed.ContainsKey(flags), "Narrow layout collapses the option group");
                await Click(ribbon, collapsed[flags]); surface = Popup(); Capture(surface, "collapsed-options");
                bool before = ruler.Checked; await Click(surface, bounds[ruler]);
                Check(ruler.Checked != before && state.Ruler == ruler.Checked && popupRuler.Checked == ruler.Checked,
                    "Collapsed group uses the original option state and bindings");
                await Key("{ESC}");
                form.Width = 900; await Task.Delay(80); ribbon.Refresh(); Capture(ribbon, "returned");
                Check(draft.Checked && ruler.Checked == state.Ruler, "Resize return preserves independent option state");
                results.Add($"PASS {(flat ? "neutral" : "Office")} options at {ribbon.DeviceDpi} DPI");
                completed = true;
            }
            catch (Exception ex) { error = ex; results.Add(ex.ToString()); }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        WF.Application.Run(form); File.WriteAllLines(Path.Combine(directory, "result.log"), results);
        return error is null && completed ? 0 : 1;

        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); results.Add("PASS " + message); }
        async Task Key(string keys)
        {
            Check(GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown,
                "Native input retains the validation window or its popup");
            Check(WF.Control.ModifierKeys == WF.Keys.None, "Keyboard modifiers are released before native input");
            WF.SendKeys.SendWait(keys); await Task.Delay(60);
        }
        async Task Click(WF.Control control, Rectangle rect)
        {
            WF.Cursor.Position = control.PointToScreen(new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
            await Task.Delay(40);
            Check(GetForegroundWindow() == form.Handle || WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown,
                "Pointer input retains the validation window or its popup");
            Capture(control, "hover"); mouse_event(2, 0, 0, 0, 0); await Task.Delay(30);
            Capture(control, "pressed"); mouse_event(4, 0, 0, 0, 0); await Task.Delay(60);
        }
        void Capture(WF.Control control, string name)
        {
            using var bitmap = new Bitmap(control.Width, control.Height);
            using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(control.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
            bitmap.Save(Path.Combine(directory, name + ".png"));
        }
        void Symmetric(RibbonItem item)
        {
            var rect = indicators[item]; using var bitmap = new Bitmap(rect.Width, rect.Height);
            using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(ribbon.PointToScreen(rect.Location), Point.Empty, bitmap.Size);
            bitmap.Save(Path.Combine(directory, item is RibbonCheckBoxItem ? "check-indicator.png" : "radio-indicator.png"));
            bool symmetric = true;
            for (int y = 0; y < rect.Height; y++) for (int x = 0; x < rect.Width; x++)
                symmetric &= bitmap.GetPixel(x, y) == bitmap.GetPixel(rect.Width - 1 - x, y) && bitmap.GetPixel(x, y) == bitmap.GetPixel(x, rect.Height - 1 - y);
            Check(symmetric, "Indicator edge coverage mirrors across all four corners");
        }
        void CenteredMark(RibbonCheckBoxItem item)
        {
            var rect = indicators[item]; using var bitmap = new Bitmap(rect.Width, rect.Height);
            using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(ribbon.PointToScreen(rect.Location), Point.Empty, bitmap.Size);
            bitmap.Save(Path.Combine(directory, item.CheckState + "-indicator.png"));
            int inset = (int)Math.Round(2 * ribbon.DeviceDpi / 96d);
            double ink = 0, horizontalMoment = 0, verticalMoment = 0;
            for (int y = inset; y < rect.Height - inset; y++) for (int x = inset; x < rect.Width - inset; x++)
            {
                double coverage = Math.Max(0, bitmap.GetPixel(x, y).R - ribbon.Theme.Accent.R);
                ink += coverage; horizontalMoment += x * coverage; verticalMoment += y * coverage;
            }
            double center = ink == 0 ? double.NaN : verticalMoment / ink;
            double centerX = ink == 0 ? double.NaN : horizontalMoment / ink;
            Check(Math.Abs(center - (rect.Height - 1) / 2d) <= .35 && Math.Abs(centerX - (rect.Width - 1) / 2d) <= .35,
                $"Native {item.CheckState} mark is centred on both axes (ink={centerX:F3},{center:F3}; box={(rect.Width - 1) / 2d:F3},{(rect.Height - 1) / 2d:F3}; DPI={ribbon.DeviceDpi})");
        }
    }
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup
        ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a ribbon popup");
    private sealed class OfficeRecorder(Dictionary<RibbonItem, Rectangle> bounds, Dictionary<RibbonItem, Rectangle> indicators, Dictionary<RibbonGroup, Rectangle> collapsed) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { bounds[i] = c.Bounds; base.DrawItem(c, i, s, menu); }
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem i) { indicators[i] = c.Bounds; base.DrawCheckBoxIndicator(c, i); }
        public override void DrawRadioButtonIndicator(RibbonPaintContext c, RibbonRadioButtonItem i) { indicators[i] = c.Bounds; base.DrawRadioButtonIndicator(c, i); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool closed) { if (closed) collapsed[g] = c.Bounds; else collapsed.Remove(g); base.DrawGroup(c, g, closed); }
    }
    private sealed class FlatRecorder(Dictionary<RibbonItem, Rectangle> bounds, Dictionary<RibbonItem, Rectangle> indicators, Dictionary<RibbonGroup, Rectangle> collapsed) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { bounds[i] = c.Bounds; base.DrawItem(c, i, s, menu); }
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem i) { indicators[i] = c.Bounds; base.DrawCheckBoxIndicator(c, i); }
        public override void DrawRadioButtonIndicator(RibbonPaintContext c, RibbonRadioButtonItem i) { indicators[i] = c.Bounds; base.DrawRadioButtonIndicator(c, i); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool closed) { if (closed) collapsed[g] = c.Bounds; else collapsed.Remove(g); base.DrawGroup(c, g, closed); }
    }
    private sealed class State : INotifyPropertyChanged
    {
        private bool _ruler, _print = true, _web; private WF.CheckState _grid = WF.CheckState.Indeterminate;
        public bool Ruler { get => _ruler; set { if (_ruler == value) return; _ruler = value; PropertyChanged?.Invoke(this, new(nameof(Ruler))); } }
        public bool Print { get => _print; set { if (_print == value) return; _print = value; PropertyChanged?.Invoke(this, new(nameof(Print))); } }
        public bool Web { get => _web; set { if (_web == value) return; _web = value; PropertyChanged?.Invoke(this, new(nameof(Web))); } }
        public WF.CheckState Grid { get => _grid; set { if (_grid == value) return; _grid = value; PropertyChanged?.Invoke(this, new(nameof(Grid))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
