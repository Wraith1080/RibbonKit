using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// A fresh, DPI-aware native process is required for desktop pixel comparisons.
internal static class BackstageRichTextChecks
{
    internal static int Run(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = []; int exit = 0;
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        using var host = new RibbonForm { Text = "Solid Backstage native RichTextBox review", StartPosition = WF.FormStartPosition.Manual, Bounds = new(220, 120, 1000, 720) };
        using var page = new WF.TextBox { Text = "Retained page field" };
        var document = new TrackedRichTextBox { Dock = WF.DockStyle.Fill, Text = string.Join(Environment.NewLine,
            Enumerable.Range(1, 24).Select(i => $"Native document line {i:D2}: retained rich text, selection, scroll and editor ownership. Native document line continues.")) };
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(document); host.Controls.Add(ribbon); host.Ribbon = ribbon;
        host.Shown += async (_, _) =>
        {
            try
            {
                host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(180);
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = density; ribbon.Focus(); document.Select(4, 12);
                    host.Refresh(); ribbon.Refresh(); document.Refresh(); await Task.Delay(60); Guard();
                    int top = Math.Max(20, ribbon.Bottom - document.Top + 20);
                    var area = document.RectangleToScreen(new Rectangle(260, top, 420, Math.Min(240, document.ClientSize.Height - top - 10)));
                    nint handle = document.Handle; var parent = document.Parent; var bounds = document.Bounds;
                    var textOrigin = document.GetPositionFromCharIndex(0); string text = document.Text;
                    int controls = host.Controls.Count;
                    document.Resizes.Clear(); using var before = Capture(area);
                    ribbon.AnimationLevel = density == RibbonDensity.Compact ? RibbonAnimationLevel.Subtle : RibbonAnimationLevel.Expressive;
                    ribbon.IsBackstageOpen = true; DwmFlush(); using var opening = Capture(area); int changed = 0;
                    for (int y = 0; y < before.Height; y++) for (int x = 0; x < before.Width; x++)
                    {
                        var a = before.GetPixel(x, y); var b = opening.GetPixel(x, y);
                        if (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) > 12) changed++;
                    }
                    before.Save(Path.Combine(directory, density + "-before.png")); opening.Save(Path.Combine(directory, density + "-opening.png"));
                    Require(ribbon.IsAnimating, "System animation policy did not permit the requested playback.");
                    Require(changed < before.Width * before.Height / 100, $"Opening changed {changed} native document pixels before the fade began.");
                    Require(document.Bounds == bounds && document.Resizes.Count == 0 && document.GetPositionFromCharIndex(0) == textOrigin, "Opening changed document geometry or scroll.");
                    await Finish(); ribbon.IsBackstageOpen = false; await Finish();
                    Require(document.Handle == handle && ReferenceEquals(parent, document.Parent) && document.Text == text && document.SelectionStart == 4 && document.SelectionLength == 12, "Native document state or ownership changed.");
                    Require(host.Controls.Count == (density == RibbonDensity.Compact ? controls + 1 : controls), "An opening drawing leaked into the host.");
                    log.Add($"PASS: {density}; changed initial document pixels={changed}; no native resize/scroll, retained handle/parent/text/selection; DPI={host.DeviceDpi}.");
                }
            }
            catch (Exception error) { exit = 1; log.Add("FAIL: " + error); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); host.Close(); }
        };
        WF.Application.Run(host); return exit;

        void Guard() { Require(GetForegroundWindow() == host.Handle, "Native review lost foreground; capture stopped."); }
        Bitmap Capture(Rectangle area)
        { Guard(); var image = new Bitmap(area.Width, area.Height); using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(area.Location, Point.Empty, area.Size); return image; }
        async Task Finish() { var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) await Task.Delay(5); Require(!ribbon.IsAnimating, "Motion did not finish."); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class TrackedRichTextBox : WF.RichTextBox
    {
        internal List<Size> Resizes { get; } = [];
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); Resizes.Add(Size); }
    }
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
}
