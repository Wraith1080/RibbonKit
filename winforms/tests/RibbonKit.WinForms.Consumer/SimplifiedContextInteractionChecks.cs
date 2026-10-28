using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class SimplifiedContextInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool Visitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, Visitor visitor, nint state);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? error = null; var record = new Recording();
        using RibbonRenderer renderer = neutral ? new NeutralRecorder(record) : new OfficeRecorder(record);
        using var host = new WF.Form { Text = "Independent context layout toggle", Width = 1150, Height = 650, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer }; var home = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "group", Text = "Clipboard" };
        var copy = new RibbonButtonItem { Id = "copy", Text = "Copy", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; group.Items.Add(copy); home.Groups.Add(group); ribbon.Tabs.Add(home); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = copy }); host.Controls.Add(ribbon);
        host.Shown += async (_, _) =>
        {
            try
            {
                await Activate();
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density;
                    foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
                    {
                        ribbon.QuickAccessPosition = position; ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.Refresh(); await Context(record.Body);
                        Check(record.Mode is { Icon: null }, "Classic body context offers an unchecked Simplified Ribbon command"); await Capture(density + "-" + position + "-unchecked"); ClickMode(); await Task.Delay(60);
                        Check(ribbon.LayoutMode == RibbonLayoutMode.Simplified, "Context command switches to Simplified mode"); ribbon.Refresh(); await Context(record.Body);
                        Check(record.Mode?.Icon is not null, "Simplified body context displays the plain checkmark"); await Capture(density + "-" + position + "-checked"); CloseContext();
                        await Context(new Rectangle(record.Panel.Left, record.Panel.Top, 1, 1)); Check(record.Mode is null, "Blank QAT panel omits the layout toggle"); CloseContext();
                        await Context(record.QatItem); Check(record.Mode is null, "QAT item omits the layout toggle"); CloseContext();
                        ribbon.Refresh(); await Context(record.Body); ClickMode(); await Task.Delay(60); Check(ribbon.LayoutMode == RibbonLayoutMode.Classic && ribbon.Density == density && ribbon.QuickAccessPosition == position, "Context command returns to Classic while retaining density and QAT placement");
                    }
                }
                log.Add($"PASS: native DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}; focused context toggle only");
            }
            catch (Exception failure) { error = failure; log.Add(failure.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); host.Close(); }
        };
        WF.Application.Run(host); return error is null ? 0 : 1;
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        async Task Activate() { for (int n = 0; n < 4 && GetForegroundWindow() != host.Handle; n++) { host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(150); } Check(GetForegroundWindow() == host.Handle, "Native host has foreground"); }
        async Task Context(Rectangle bounds)
        {
            record.Mode = null; var point = ribbon.PointToScreen(new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
            SendMessage(ribbon.Handle, 0x7B, ribbon.Handle, (nint)((point.Y << 16) | (point.X & 0xffff))); await Task.Delay(60);
            Check(Popup() is not null, "Native ribbon context popup opens");
        }
        void ClickMode() { var surface = Popup()!.Items.OfType<WF.ToolStripControlHost>().Single().Control; var bounds = record.ModeBounds; nint point = (nint)(((bounds.Top + bounds.Height / 2) << 16) | (bounds.Left + bounds.Width / 2)); SendMessage(surface.Handle, 0x201, 1, point); SendMessage(surface.Handle, 0x202, 0, point); }
        void CloseContext() { WF.SendKeys.SendWait("{ESC}"); WF.Application.DoEvents(); }
        async Task Capture(string name) { await Task.Delay(40); var popup = Popup()!; popup.Refresh(); var bounds = popup.Bounds; using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var g = Graphics.FromImage(bitmap); g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png")); }
    }
    private static WF.ToolStripDropDown? Popup() { WF.ToolStripDropDown? result = null; EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (WF.Control.FromHandle(window) is WF.ToolStripDropDown { Visible: true } popup) result = popup; return true; }, 0); return result; }
    private sealed class Recording
    {
        internal Rectangle Body, Panel, QatItem, ModeBounds; internal RibbonItem? Mode;
        internal void Item(RibbonPaintContext c, RibbonItem item, bool menu) { if (!menu && item.Id == "copy") Body = c.Bounds; if (menu && item.Id == "ToggleSimplifiedRibbon") { Mode = item; ModeBounds = c.Bounds; } }
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item, menu); base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { r.Panel = c.Bounds; base.DrawQuickAccessToolbar(c, position); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.QatItem = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
    }
    private sealed class NeutralRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item, menu); base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { r.Panel = c.Bounds; base.DrawQuickAccessToolbar(c, position); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.QatItem = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
    }
}
