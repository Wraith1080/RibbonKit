using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class OptionsPopupInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool WindowVisitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowVisitor visit, nint state);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? failure = null; var record = new Recording();
        using RibbonRenderer renderer = neutral ? new NeutralRecorder(record) : new OfficeRecorder(record);
        using var host = new WF.Form { Text = "Independent Options popup checks", Width = 1300, Height = 780, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, LayoutMode = RibbonLayoutMode.Simplified };
        var home = new RibbonTab { Id = "home", Text = "Home" }; var insert = new RibbonTab { Id = "insert", Text = "Insert" }; var group = new RibbonGroup { Id = "clipboard", Text = "Clipboard" };
        group.Items.Add(new RibbonButtonItem { Id = "copy", Text = "Copy", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); home.Groups.Add(group); ribbon.Tabs.AddRange(home, insert); ribbon.CommandCatalog.CaptureDefaults(); host.Controls.Add(ribbon);
        host.Shown += async (_, _) =>
        {
            try
            {
                await Activate(host);
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density; using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.options"); dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "CustomizeSimplifiedOptionsPage");
                    using var timer = new WF.Timer { Interval = 100 }; Exception? dialogFailure = null; var finished = new TaskCompletionSource();
                    timer.Tick += async (_, _) =>
                    {
                        timer.Stop();
                        try
                        {
                            await Activate(dialog); Check(!IsWindowEnabled(host.Handle), "Native modal Options disables its main window");
                            var page = dialog.SimplifiedEditor; var source = Child<WF.ComboBox>(page, "SimplifiedSourceTab"); var target = Child<WF.ComboBox>(page, "SimplifiedTargetTab");
                            await Capture(dialog, density + "-options"); int before = source.SelectedIndex, targetBefore = target.SelectedIndex;
                            source.Focus(); SendMessage(source.Handle, 0x14F, 1, 0); await Task.Delay(80); var popup = Popup();
                            Check(popup.Items.OfType<WF.ToolStripControlHost>().Single().Control is WF.ListBox, "Selector opens the ribbon's hosted choice list"); await Capture(popup, density + "-choices");
                            WF.SendKeys.SendWait("{DOWN}{ENTER}"); await Task.Delay(80);
                            Check(source.SelectedIndex == before + 1 && target.SelectedIndex == targetBefore && PopupOrNull() is null, "Choice keyboard commit changes only the independent source selector");
                            SendMessage(source.Handle, 0x14F, 1, 0); await Task.Delay(50); WF.SendKeys.SendWait("{ESC}"); await Task.Delay(50);
                            Check(source.SelectedIndex == before + 1 && PopupOrNull() is null, "Escape closes the choice popup without changing selection");
                            Child<WF.ListBox>(page, "SimplifiedSelectedCommands").SelectedIndex = 1; var edit = Child<WF.Button>(page, "SimplifiedEdit"); record.Checkboxes = record.Headings = 0;
                            edit.PerformClick(); await Task.Delay(70); popup = Popup(); var surface = popup.Items.OfType<WF.ToolStripControlHost>().Single().Control; surface.Refresh();
                            Check(surface.GetType().Name == "RibbonPopupSurface", "Edit uses the actual shared ribbon menu surface while the main form is disabled");
                            Check(record.Headings == 0, "Customization menu begins with commands and has no heading");
                            Check(record.Checkboxes == 0 && record.Marked?.Icon is not null, "Checked Show label paints a plain checkmark without a checkbox"); await Capture(popup, density + "-edit");
                            WF.SendKeys.SendWait("{ESC}"); await Task.Delay(60); Check(PopupOrNull() is null && edit.Focused, "Escape returns focus to the Edit button");
                            bool cancel = true; dialog.FormClosing += (_, e) => { if (cancel) { cancel = false; e.Cancel = true; } };
                            edit.PerformClick(); await Task.Delay(60); WF.SendKeys.SendWait("%{F4}"); await Task.Delay(80);
                            Check(!dialog.IsDisposed && dialog.Visible && PopupOrNull() is null, "Menu Alt+F4 targets Options and respects a canceled form close");
                            source.Focus(); SendMessage(source.Handle, 0x14F, 1, 0); await Task.Delay(60); WF.SendKeys.SendWait("%{F4}"); await Task.Delay(80);
                            Check(!dialog.Visible && !host.IsDisposed, "Choice Alt+F4 closes Options and preserves the main form");
                        }
                        catch (Exception error) { dialogFailure = error; dialog.Close(); }
                        finally { finished.TrySetResult(); }
                    };
                    timer.Start(); dialog.ShowDialog(host); await finished.Task; if (dialogFailure is not null) throw dialogFailure; await Activate(host);
                }
                log.Add($"PASS: native DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}; focused Options popup checks only");
            }
            catch (Exception error) { failure = error; log.Add(error.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); host.Close(); }
        };
        WF.Application.Run(host); return failure is null ? 0 : 1;
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        async Task Activate(WF.Form form) { for (int n = 0; n < 4 && GetForegroundWindow() != form.Handle; n++) { form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150); } Check(GetForegroundWindow() == form.Handle, "Native target has foreground"); }
        async Task Capture(WF.Control control, string name) { await Task.Delay(50); control.Refresh(); var bounds = control.RectangleToScreen(control.ClientRectangle); using var image = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
    private static T Child<T>(WF.Control root, string name) where T : WF.Control => (T)root.Controls.Find(name, true).Single();
    private static WF.ToolStripDropDown? PopupOrNull()
    {
        WF.ToolStripDropDown? popup = null; EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (WF.Control.FromHandle(window) is WF.ToolStripDropDown { Visible: true } candidate) popup = candidate; return true; }, 0); return popup;
    }
    private static WF.ToolStripDropDown Popup() => PopupOrNull() ?? throw new InvalidOperationException("Expected a ribbon popup");
    private sealed class Recording { internal int Checkboxes, Headings; internal RibbonItem? Marked; }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { r.Headings++; base.DrawMenuHeader(c, text); }
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item) { r.Checkboxes++; base.DrawCheckBoxIndicator(c, item); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (menu && item.Text == "Show label") r.Marked = item; base.DrawItem(c, item, size, menu); }
    }
    private sealed class NeutralRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { r.Headings++; base.DrawMenuHeader(c, text); }
        public override void DrawCheckBoxIndicator(RibbonPaintContext c, RibbonCheckBoxItem item) { r.Checkboxes++; base.DrawCheckBoxIndicator(c, item); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (menu && item.Text == "Show label") r.Marked = item; base.DrawItem(c, item, size, menu); }
    }
}
