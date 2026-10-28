using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class CustomizationInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extra);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); Directory.CreateDirectory(directory);
        var recording = new Recording(); using RibbonRenderer painter = neutral ? new Neutral(recording) : new Office(recording);
        using var form = new WF.Form { Text = "Independent QAT customization foundation", Width = 1100, Height = 550, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter };
        var home = new RibbonTab { Id = "tab.home", Text = "Home" }; var group = new RibbonGroup { Id = "group.font", Text = "Font" };
        var bold = new RibbonToggleItem { Id = "cmd.bold", Text = "Bold" }; var field = new RibbonTextBoxItem { Id = "cmd.name", Text = "Name", Value = "Accepted" };
        var undo = new RibbonButtonItem { Id = "cmd.undo", Text = "Undo" }; var redo = new RibbonButtonItem { Id = "cmd.redo", Text = "Redo" };
        group.Items.AddRange(bold, field); home.Groups.Add(group); ribbon.Tabs.Add(home); ribbon.CommandCatalog.Commands.Add(redo);
        ribbon.QuickAccessItems.AddRange(undo, new RibbonQuickAccessReference { Id = "qat.bold", Source = bold });
        using var state = new WF.BindingSource { DataSource = new State() };
        bold.DataBindings.Add(new WF.Binding(nameof(bold.Checked), state, nameof(State.Bold), false, WF.DataSourceUpdateMode.OnPropertyChanged));
        field.ValueValidating += (_, e) => e.Cancel = field.Value.Length == 0;
        form.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application-owned document. The shared engine owns catalog/draft/Apply, and the application owns storage." }); form.Controls.Add(ribbon);
        int undos = 0, applied = 0; undo.Click += (_, _) => undos++;
        Exception? failure = null; var log = new List<string>(); var cursor = WF.Cursor.Position;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 30 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); ribbon.Refresh(); using var edit = ribbon.CommandCatalog.BeginCustomization("consumer.app"); edit.Applied += (_, _) => applied++;
                field.PerformClick(); var native = Tree(ribbon).OfType<WF.TextBox>().Single(); nint handle = native.Handle;
                var original = ribbon.QuickAccessItems.ToArray(); await Capture("before-draft");
                edit.Draft.QuickAccess.RemoveAt(0); edit.Draft.QuickAccess.Add(new() { CommandId = redo.Id }); edit.Draft.QuickAccess.Add(new() { CommandId = group.Id }); edit.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                Check(original.SequenceEqual(ribbon.QuickAccessItems), "Detached edits leave the live QAT unchanged");
                native.Text = ""; Check(!edit.Apply() && original.SequenceEqual(ribbon.QuickAccessItems) && native.Focused && applied == 0, "Invalid native input blocks Apply without changing live customization");
                native.Text = "Committed"; Check(edit.Apply(), "Valid Apply succeeds"); ribbon.Refresh();
                Check(native.Handle == handle && field.Value == "Committed", "Apply retains the native editor HWND and committed model value");
                Check(ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon).Any(e => ReferenceEquals(e.Source, undo)) && ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess).Any(e => ReferenceEquals(e.Source, undo)), "Removed QAT-only commands remain available in both catalogs");
                Check(!ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon).Any(e => e.IsGroup), "Whole groups are offered only to QAT customization"); await Capture("applied");
                var boldRef = ribbon.QuickAccessItems.OfType<RibbonQuickAccessReference>().Single(e => e.Source == bold);
                await Click(ribbon, recording.Items[boldRef]); Check(bold.Checked && ((State)state.Current!).Bold, "Applied QAT references retain source binding and toggle state");
                var groupRef = ribbon.QuickAccessItems.OfType<RibbonQuickAccessGroupReference>().Single(); await Click(ribbon, recording.Items[groupRef]); var popup = Popup();
                Check(native.Handle == handle && native.Visible && Tree(ribbon).Concat(Tree(popup)).OfType<WF.TextBox>().Distinct().Count() == 1, "Customized whole-group QAT borrows the existing native editor exactly once");
                await Capture("borrowed-group"); await Key("{ESC}");
                string json = edit.ExportJson(); File.WriteAllText(Path.Combine(directory, "profile.json"), json);
                var appliedModels = ribbon.QuickAccessItems.ToArray(); edit.Draft.QuickAccess.Clear(); edit.Cancel();
                Check(appliedModels.SequenceEqual(ribbon.QuickAccessItems) && edit.Draft.QuickAccess.Count == appliedModels.Length, "Apply then edit then Cancel keeps the latest applied checkpoint");
                edit.Reset(); Check(appliedModels.SequenceEqual(ribbon.QuickAccessItems), "Reset edits only the draft"); Check(edit.Apply(), "Default reset applies"); ribbon.Refresh();
                Check(ReferenceEquals(ribbon.QuickAccessItems[0], undo) && native.Handle == handle && field.Value == "Committed" && bold.Checked, "Reset restores the original QAT-only model without resetting command/editor state");
                undo.PerformClick(); Check(undos == 1, "Restored QAT-only commands retain their original handlers");
                edit.ImportJson(json); Check(ReferenceEquals(ribbon.QuickAccessItems[0], undo), "JSON import changes only the draft"); Check(edit.Apply(), "Validated JSON applies");
                Check(applied == 3, "Applied is raised exactly once per successful Apply"); await Capture("restored-json");
                log.Add($"PASS: {(neutral ? "neutral" : "Office2024")} public customization foundation at actual {ribbon.DeviceDpi} DPI");
            }
            catch (Exception e) { failure = e; log.Add(e.ToString()); }
            finally { WF.Cursor.Position = cursor; File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); foreach (var line in log) Console.WriteLine(line); return failure is null ? 0 : 1;
        void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        void Guard() { var foreground = GetForegroundWindow(); if (foreground != form.Handle && WF.Control.FromHandle(foreground) is not WF.ToolStripDropDown) throw new InvalidOperationException("Customization consumer lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(70); ribbon.Refresh(); }
        async Task Click(WF.Control surface, Rectangle bounds)
        {
            Guard(); WF.Cursor.Position = surface.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(30);
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(70); ribbon.Refresh();
        }
        async Task Capture(string name)
        {
            await Task.Delay(80); Guard(); var bounds = form.Bounds;
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown drop) bounds = Rectangle.Union(bounds, drop.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var g = Graphics.FromImage(bitmap); g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control parent) { foreach (WF.Control child in parent.Controls) { yield return child; foreach (var nested in Tree(child)) yield return nested; } }
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown drop ? drop.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected the borrowed group popup.");
    private sealed class State : INotifyPropertyChanged
    {
        private bool _bold;
        public bool Bold { get => _bold; set { if (_bold == value) return; _bold = value; PropertyChanged?.Invoke(this, new(nameof(Bold))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording { internal readonly Dictionary<RibbonElement, Rectangle> Items = []; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.Items[item] = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
    }
    private sealed class Neutral(Recording r) : RibbonRenderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.Items[item] = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
    }
}
