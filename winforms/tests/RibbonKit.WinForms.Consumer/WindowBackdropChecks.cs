using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public-only window/material consumer. It references no Showcase code/resources.
internal static class WindowBackdropChecks
{
    internal static int Run(string directory, bool flat = false, bool simplified = false)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        using var traceFile = new StreamWriter(Path.Combine(directory, "native-trace.log")) { AutoFlush = true };
        using var trace = new TextWriterTraceListener(traceFile); Trace.Listeners.Add(trace);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        using var behind = new WF.Form { StartPosition = WF.FormStartPosition.Manual, Bounds = new(200, 150, 1150, 800), FormBorderStyle = WF.FormBorderStyle.None, ShowInTaskbar = false, BackColor = Color.CornflowerBlue };
        behind.Controls.Add(new WF.Panel { Dock = WF.DockStyle.Right, Width = 500, BackColor = Color.IndianRed }); behind.Show();
        using var form = new ReviewForm { StartPosition = WF.FormStartPosition.Manual, Bounds = new(240, 190, 1000, 650), Text = "Independent native backdrop consumer" };
        using var renderer = new RibbonRenderer();
        using var ribbon = new RibbonControl { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, AnimationLevel = RibbonAnimationLevel.Expressive, Renderer = flat ? renderer : null, FileMenuMode = RibbonFileMenuMode.Backstage };
        ribbon.FileMenu.Visible = true;
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Native editors" };
        var field = new RibbonTextBoxItem { Id = "field", Text = "Name", Value = "Opaque native text field", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var choice = new RibbonComboBoxItem { Id = "choice", Text = "Font", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; choice.Items.AddRange("Calibri", "Segoe UI"); choice.SelectedIndex = 0;
        group.Items.AddRange(field, choice); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.Tabs.Add(new RibbonTab { Text = "View" });
        var save = new RibbonButtonItem { Text = "Save" }; ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = save });
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        using var page = new WF.Panel { BackColor = Color.White };
        page.Controls.Add(new WF.TextBox { Text = "Backstage native field", Bounds = new(30, 35, 400, 30) });
        page.Controls.Add(new WF.TextBox { Multiline = true, Text = "Application-owned notes", Bounds = new(30, 95, 500, 90) });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        var content = new RibbonBackdropPanel { Bounds = new(40, 290, 700, 160), BackColor = Color.White };
        content.Controls.Add(new WF.TextBox { Text = "Application-owned content stays opaque", Bounds = new(20, 20, 450, 30) }); form.Controls.Add(content);
        form.Shown += async (_, _) =>
        {
            try
            {
                Assert(form.Backdrop == RibbonWindowBackdrop.None && form.EffectiveBackdrop == RibbonWindowBackdrop.None, "Solid default");
                var theme = ribbon.Theme;
                nint fieldHandle = Descendants(ribbon).OfType<WF.TextBox>().Single(c => c.Name == field.Id).Handle;
                nint choiceHandle = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == choice.Id).Handle;
                if (simplified) { ribbon.LayoutMode = RibbonLayoutMode.Simplified; await Finish(); }
                log.Add($"OS={Environment.OSVersion.Version}; DPI={form.DeviceDpi}; renderer={(flat ? "neutral" : "Office")}; layout={ribbon.LayoutMode}");
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density; await Finish(); content.Top = ribbon.Bottom + 45;
                    foreach (var material in new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic, RibbonWindowBackdrop.None })
                    {
                        form.Backdrop = material; await Task.Delay(220);
                        Assert(form.Backdrop == material, "Requested preference retained");
                        Assert(ReferenceEquals(theme, ribbon.Theme) && form.Opacity == 1 && form.TransparencyKey == Color.Empty && content.BackColor == Color.White, "Authored theme/content/opacity retained");
                        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) && !WF.SystemInformation.HighContrast)
                            Assert(form.EffectiveBackdrop == material, "Native backdrop setting applied");
                        var header = Capture($"{density}-{material}");
                        ribbon.SelectedTab = ribbon.Tabs[1]; Assert(ribbon.IsAnimating, "Tab motion starts with material"); await Task.Delay(50); var movingHeader = Capture($"{density}-{material}-tab-mid"); await Finish();
                        Assert(Math.Abs(header.R - movingHeader.R) <= 12 && Math.Abs(header.G - movingHeader.G) <= 12 && Math.Abs(header.B - movingHeader.B) <= 12, "Material header must not become black or opaque during body motion");
                        ribbon.SelectedTab = tab; await Finish();
                        ribbon.IsBackstageOpen = true; Assert(ribbon.IsBackstageOpen, "Backstage must actually open"); await Finish(); Capture($"{density}-{material}-backstage"); ribbon.IsBackstageOpen = false; await Finish();
                        Assert(Descendants(ribbon).OfType<WF.TextBox>().Single(c => c.Name == field.Id).Handle == fieldHandle && Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == choice.Id).Handle == choiceHandle, "Native editor handles retained");
                        Assert(field.Value == "Opaque native text field" && choice.SelectedIndex == 0, "Native editor values retained");
                        log.Add($"PASS: {density}/{material}; effective={form.EffectiveBackdrop}; native fields, Backstage and tab motion");
                    }
                }
                form.Backdrop = RibbonWindowBackdrop.Mica; form.Opacity = .9; await Task.Delay(50);
                Assert(form.EffectiveBackdrop == RibbonWindowBackdrop.None && form.Backdrop == RibbonWindowBackdrop.Mica, "Application opacity uses solid fallback with preference retained");
                form.Opacity = 1; await Task.Delay(50); Assert(form.EffectiveBackdrop == RibbonWindowBackdrop.Mica, "Opacity reset recovers requested material");
                form.TransparencyKey = Color.Magenta; await Task.Delay(50); Assert(form.EffectiveBackdrop == RibbonWindowBackdrop.None, "Application transparency key uses solid fallback");
                form.TransparencyKey = Color.Empty; await Task.Delay(50); Assert(form.EffectiveBackdrop == RibbonWindowBackdrop.Mica, "Transparency reset recovers material");
                form.FormBorderStyle = WF.FormBorderStyle.None; await Task.Delay(50); Assert(form.EffectiveBackdrop == RibbonWindowBackdrop.None, "Borderless fallback");
                form.FormBorderStyle = WF.FormBorderStyle.Sizable; await Task.Delay(50); Assert(form.EffectiveBackdrop == RibbonWindowBackdrop.Mica, "Border restoration recovers material");
                nint old = form.Handle; form.Recreate(); await Task.Delay(80); Assert(old != form.Handle && form.EffectiveBackdrop == RibbonWindowBackdrop.Mica, $"Handle recreation reapplies requested material (old={old}; new={form.Handle}; effective={form.EffectiveBackdrop})");
                form.WindowState = WF.FormWindowState.Maximized; await NativeWindowSettled(); Capture("maximized"); form.WindowState = WF.FormWindowState.Normal; await NativeWindowSettled(); Capture("restored");
                form.Backdrop = RibbonWindowBackdrop.None; await Task.Delay(120); Capture("solid-final");
                log.Add("PASS: public-only backdrop lifecycle completed");
            }
            catch (Exception error) { log.Add("FAIL: " + error); Environment.ExitCode = 1; }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); Trace.Listeners.Remove(trace); return Environment.ExitCode;
        void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        async Task Finish() { var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1800) await Task.Delay(5); Assert(!ribbon.IsAnimating, "Motion must finish without host acknowledgement"); await Task.Delay(30); }
        async Task NativeWindowSettled()
        {
            // HWND bounds commit before the Windows maximize/restore animation
            // finishes. Wait for the actual opaque content ROI to stop changing.
            int[]? previous = null; int stable = 0;
            for (int attempt = 0; attempt < 35; attempt++)
            {
                await Task.Delay(35); var area = content.Controls[0].RectangleToScreen(content.Controls[0].ClientRectangle);
                using var image = new Bitmap(area.Width, area.Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                List<int> values = []; int dark = 0;
                for (int y = 2; y < image.Height - 2; y += 2) for (int x = 2; x < image.Width - 2; x += 2)
                { var color = image.GetPixel(x, y); values.Add(color.ToArgb()); if (color.R < 80 && color.G < 80 && color.B < 80) dark++; }
                var current = values.ToArray(); stable = dark > 10 && previous is not null && current.SequenceEqual(previous) ? stable + 1 : 0;
                if (stable >= 3) return; previous = current;
            }
            throw new InvalidOperationException("Native window compositor did not settle within its bounded capture wait.");
        }
        Color Capture(string name)
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint process);
            Assert(process == Environment.ProcessId, "Consumer lost foreground; capture stopped.");
            using var image = new Bitmap(form.Width, form.Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(form.Location, Point.Empty, image.Size);
            image.Save(Path.Combine(directory, name + ".png"));
            if (!ribbon.IsBackstageOpen && !name.EndsWith("-tab-mid", StringComparison.Ordinal))
            {
                var control = content.Controls[0]; var area = control.RectangleToScreen(control.ClientRectangle); area.Offset(-form.Left, -form.Top);
                int dark = 0, white = 0;
                for (int y = area.Top + 2; y < area.Bottom - 2; y++) for (int x = area.Left + 2; x < area.Right - 2; x++)
                {
                    var color = image.GetPixel(x, y); if (color.R < 80 && color.G < 80 && color.B < 80) dark++;
                    if (color.R > 235 && color.G > 235 && color.B > 235) white++;
                }
                Assert(dark > 40 && white > area.Width * area.Height / 2, $"Opaque GDI application content lost text/background pixels: {name}, dark={dark}, white={white}");
            }
            return image.GetPixel(image.Width - 200, (int)Math.Round((ribbon.Density == RibbonDensity.Touch ? 24 : 18) * form.DeviceDpi / 96d));
        }
    }
    private sealed class ReviewForm : RibbonForm { internal void Recreate() => RecreateHandle(); }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    private static IEnumerable<WF.Control> Descendants(WF.Control parent) => parent.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
}
