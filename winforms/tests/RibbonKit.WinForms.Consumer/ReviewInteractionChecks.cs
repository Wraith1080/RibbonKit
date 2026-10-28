using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class ReviewInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint relation);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool Visitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, Visitor visitor, nint state);
    internal static int Run(string directory, bool neutral)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); List<string> log = []; Exception? error = null;
        using RibbonRenderer renderer = neutral ? new RibbonRenderer() : new RibbonOffice2024Renderer();
        using var host = new TracedForm { Width = 1300, Height = 780, Text = "Independent review fixes", StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, CustomizationApplicationId = "consumer.review" }; var home = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "clipboard", Text = "Clipboard" };
        var menu = new RibbonDropDownItem { Id = "paste", Text = "Paste", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var copy = new RibbonButtonItem { Id = "copy", Text = "Copy", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; menu.Items.Add(new RibbonButtonItem { Id = "paste.text", Text = "Paste text" }); group.Items.AddRange(menu, copy); home.Groups.Add(group); ribbon.Tabs.Add(home); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = copy }); ribbon.CommandCatalog.CaptureDefaults(); host.Controls.Add(ribbon);
        for (int i = 0; i < 24; i++) group.Items.Add(new RibbonButtonItem { Id = "demo." + i, Text = "Demo " + i, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small) });
        ribbon.CommandCatalog.CaptureDefaults();
        host.Shown += async (_, _) =>
        {
            try
            {
                await Activate(host); int before = host.InactiveFrames;
                for (int i = 0; i < 8; i++)
                {
                    menu.ShowDropDown(); await Task.Delay(60); var popup = Popup(); nint owner = GetWindow(popup.Handle, 4);
                    log.Add($"DIAGNOSTIC: iteration={i}; owner={owner:X}; host={host.Handle:X}; ownerType={WF.Control.FromHandle(owner)?.GetType().Name}; inactiveFrames={host.InactiveFrames - before}");
                    menu.CloseDropDown(); await Task.Delay(40);
                }
                log.Add($"DIAGNOSTIC: total inactive title-bar messages={host.InactiveFrames - before}");
                Check(host.InactiveFrames == before, "Eight popup open/close cycles do not paint the host title bar inactive");
                menu.ShowDropDown(); using (var other = new WF.Form { Text = "Other application content", Size = new(400, 250) })
                {
                    other.Show(); await Activate(other); await Task.Delay(100); Check(!menu.IsDropDownOpen && GetForegroundWindow() == other.Handle, "Real activation changes dismiss popups without reclaiming focus"); other.Close();
                }
                await Activate(host);
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density; await Capture(host, density + "-header");
                    menu.ShowDropDown(); await Task.Delay(50); var popup = Popup(); var surface = popup.Items.OfType<WF.ToolStripControlHost>().Single().Control;
                    var point = surface.PointToScreen(new Point(45, 18)); SendMessage(surface.Handle, 0x7B, surface.Handle, (nint)((point.Y << 16) | (point.X & 0xffff))); await Task.Delay(70);
                    Check(Popups().Count == 2, "Right-click keeps the source popup alive beneath customization"); await CaptureStack(density + "-context");
                    WF.SendKeys.SendWait("{ESC}"); await Task.Delay(60); Check(Popups().Count == 1 && menu.IsDropDownOpen, "Escape dismisses only the customization level"); menu.CloseDropDown();
                    foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
                    {
                        ribbon.LayoutMode = mode;
                        using var options = new RibbonQuickAccessCustomizationForm(ribbon, "consumer.review"); options.SelectedPage = options.Pages.Single(p => p.Text == "Customize Ribbon"); options.Show(host); await Activate(options);
                        WF.Control page = mode == RibbonLayoutMode.Classic ? options.RibbonEditor : options.SimplifiedEditor;
                        Exception? groupFailure = null; var finished = new TaskCompletionSource(); using var create = new WF.Timer { Interval = 60 };
                        create.Tick += async (_, _) =>
                        {
                            if (WF.Application.OpenForms.Cast<WF.Form>().FirstOrDefault(f => f.Name == "RibbonGroupDialog") is not { } edit) return; create.Stop();
                            try
                            {
                                await Activate(edit); var layout = Child<WF.ComboBox>(edit, "RibbonGroupLayout"); Check(Equals(layout.SelectedItem, RibbonCustomizationGroupLayout.Stacked), mode + " group creation defaults to Stacked");
                                Child<WF.TextBox>(edit, "RibbonGroupName").Text = mode + " tools"; Child<WF.ListView>(edit, "RibbonGroupIcons").Items["copy"]!.Selected = true;
                                layout.SelectedItem = mode == RibbonLayoutMode.Classic ? RibbonCustomizationGroupLayout.Large : RibbonCustomizationGroupLayout.Stacked;
                                await Capture(edit, density + "-" + mode + "-group"); Child<WF.Button>(edit, "AcceptGroup").PerformClick();
                            }
                            catch (Exception failure) { groupFailure = failure; edit.Close(); }
                            finally { finished.TrySetResult(); }
                        };
                        create.Start();
                        if (mode == RibbonLayoutMode.Classic) Child<WF.Button>(page, "NewRibbonGroup").PerformClick();
                        else
                        {
                            await Capture(options, density + "-" + mode + "-before-new");
                            var newButton = Child<WF.Button>(page, "SimplifiedNew"); var target = Child<WF.ComboBox>(page, "SimplifiedTargetTab"); log.Add($"DIAGNOSTIC: new Enabled={newButton.Enabled}; visible={newButton.Visible}; target={target.SelectedIndex}/{target.Items.Count}; layout={ribbon.LayoutMode}");
                            Child<WF.Button>(page, "SimplifiedNew").PerformClick(); await Task.Delay(50); var actions = Popup().Items.OfType<WF.ToolStripControlHost>().Single().Control;
                            nint groupPoint = (nint)((actions.Height * 3 / 4 << 16) | (actions.Width / 2)); SendMessage(actions.Handle, 0x201, 1, groupPoint); SendMessage(actions.Handle, 0x202, 0, groupPoint);
                        }
                        await finished.Task.WaitAsync(TimeSpan.FromSeconds(5)); if (groupFailure is not null) throw groupFailure;
                        var session = mode == RibbonLayoutMode.Classic ? options.RibbonEditor.Session : options.SimplifiedEditor.Session;
                        var added = session.Draft.Ribbon!.Tabs.Single(t => t.Id == "home").Groups.Last(); added.Commands.Add(new() { CommandId = "copy" });
                        Child<WF.Button>(options, "ApplyCustomization").PerformClick();
                        Check(Child<WF.Label>(page, mode == RibbonLayoutMode.Classic ? "RibbonCustomizationStatus" : "SimplifiedCustomizationStatus").Text.Length == 0, "Successful Apply leaves no confirmation/status row");
                        var live = home.Groups.Single(g => g.Id == added.Id); Check(ReferenceEquals(live.Icon, copy.Icon), "Applied custom group borrows the selected command icon");
                        Check(live.Items[0].SizePolicy.Large == (mode == RibbonLayoutMode.Classic ? RibbonItemSize.Large : RibbonItemSize.Medium), "Custom group layout synchronizes to ordinary command presentation");
                        if (mode == RibbonLayoutMode.Classic)
                        {
                            var tree = Child<WF.TreeView>(page, "RibbonHierarchy"); Check(((int)SendMessage(tree.Handle, 0x112D, 0, 0) & 4) != 0, "Native hierarchy double buffering is active");
                            SendMessage(tree.Handle, 0x200, 0, (nint)((tree.ItemHeight / 2 << 16) | 12)); SendMessage(tree.Handle, 0x20A, (nint)(-120 << 16), 0); tree.Update();
                        }
                        await Capture(options, density + "-" + mode + "-options"); File.WriteAllText(Path.Combine(directory, density + "-" + mode + ".json"), session.ExportJson()); options.Close(); await Activate(host);
                    }
                    ribbon.LayoutMode = RibbonLayoutMode.Classic;
                }
                log.Add($"PASS: native DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}");
            }
            catch (Exception failure) { error = failure; log.Add(failure.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); host.Close(); }
        };
        WF.Application.Run(host); return error is null ? 0 : 1;
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        async Task Activate(WF.Form form) { for (int n = 0; n < 4 && GetForegroundWindow() != form.Handle; n++) { form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150); } Check(GetForegroundWindow() == form.Handle, "Native target has foreground"); }
        async Task Capture(WF.Control control, string name) { await Task.Delay(50); control.Refresh(); Save(control.RectangleToScreen(control.ClientRectangle), name); }
        async Task CaptureStack(string name) { await Task.Delay(50); var bounds = Popups().Select(p => p.Bounds).Aggregate(Rectangle.Union); Save(bounds, name); }
        void Save(Rectangle bounds, string name) { using var image = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); image.Save(Path.Combine(directory, name + ".png")); }
    }
    private static List<WF.ToolStripDropDown> Popups()
    { List<WF.ToolStripDropDown> windows = []; EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (WF.Control.FromHandle(window) is WF.ToolStripDropDown { Visible: true } popup) windows.Add(popup); return true; }, 0); return windows; }
    private static WF.ToolStripDropDown Popup() => Popups().First();
    private static T Child<T>(WF.Control root, string name) where T : WF.Control => (T)root.Controls.Find(name, true).Single();
    private sealed class TracedForm : WF.Form
    { internal int InactiveFrames; protected override void WndProc(ref WF.Message m) { if (m.Msg == 0x86 && m.WParam == 0) InactiveFrames++; base.WndProc(ref m); } }
    private sealed class SampleIcon : IRibbonIcon
    { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { using var pen = new Pen(c.Foreground, Math.Max(1, c.DeviceDpi / 96f)); b.Inflate(-b.Width / 5, -b.Height / 5); g.DrawRectangle(pen, b); g.DrawLine(pen, b.Left, b.Top + b.Height / 2, b.Right, b.Top + b.Height / 2); } }
}
