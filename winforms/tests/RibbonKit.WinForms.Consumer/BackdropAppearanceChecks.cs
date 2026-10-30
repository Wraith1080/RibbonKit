using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public-only controlled painting and real closing frames. No pointer/keyboard input.
internal static class BackdropAppearanceChecks
{
    internal static int Run(string directory, bool closingOnly = false, bool shadowOnly = false)
    {
        Directory.CreateDirectory(directory); List<string> log = []; int exit = 0;
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles();
        using var form = new RibbonForm { Text = "Backdrop appearance review - closes automatically", StartPosition = WF.FormStartPosition.Manual, Bounds = new(240, 150, 1000, 720) };
        using var renderer = new StateProbe();
        var ribbon = new RibbonControl { Renderer = renderer, FileMenuMode = RibbonFileMenuMode.Backstage };
        var source = new RibbonToggleItem { Text = "Action", Icon = new SquareIcon(Color.Black), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var group = new RibbonGroup { Text = "Commands" }; group.Items.Add(source); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group);
        ribbon.Tabs.AddRange(tab, new RibbonTab { Text = "Insert" }); ribbon.FileMenu.Visible = true;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = source });
        var content = new RibbonBackdropPanel { Dock = WF.DockStyle.Fill, BackColor = Color.FromArgb(30, 110, 90) };
        content.Controls.Add(new WF.RichTextBox { Dock = WF.DockStyle.Bottom, Height = 120, Text = "Application-owned document retained during Backstage closing." });
        form.Controls.Add(content); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        using var page = new WF.Panel { BackColor = Color.White };
        var field = new WF.TextBox { Bounds = new(30, 40, 350, 30), Text = "Retained native page field" }; page.Controls.Add(field);
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Icon = new SquareIcon(Color.Magenta), Content = page });
        ribbon.Backstage.NavigationBackgroundOpacity = 0;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Delay(260);
                foreach (var material in shadowOnly ? new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic }
                    : new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                {
                    form.Backdrop = material; Require(form.EffectiveBackdrop == material, "Material did not apply.");
                    foreach (int state in closingOnly || shadowOnly ? [] : new[] { 0, 1, 2 })
                    {
                        renderer.State = state; ribbon.Invalidate(); await Task.Delay(65);
                        using var shot = Capture(); shot.Save(Path.Combine(directory, $"{material}-state-{state}.png"));
                    }
                    renderer.State = 0; ribbon.Invalidate(); ribbon.AnimationLevel = RibbonAnimationLevel.None;
                    ribbon.IsBackstageOpen = true; await Task.Delay(40);
                    var entry = Descendants(form).OfType<WF.Button>().Single(button => button.Text == "Info");
                    int S(int n) => (int)Math.Round(n * form.DeviceDpi / 96d);
                    var icon = form.PointToClient(entry.PointToScreen(new(S(24), entry.Height / 2)));
                    var paper = form.PointToClient(page.PointToScreen(new(page.Width - S(30), S(300))));
                    if (shadowOnly)
                    {
                        await Task.Delay(60); using var shot = Capture();
                        var origin = form.PointToClient(page.PointToScreen(Point.Empty)); int inset = renderer.GetBackstagePageShadowInset(ribbon.Theme, form.DeviceDpi);
                        int Light(Color color) => color.R + color.G + color.B;
                        var nearLeft = shot.GetPixel(origin.X - 1, origin.Y + S(200)); var farLeft = shot.GetPixel(origin.X - inset - S(2), origin.Y + S(200));
                        var nearTop = shot.GetPixel(origin.X + S(100), origin.Y - 1); var farTop = shot.GetPixel(origin.X + S(100), origin.Y - inset);
                        shot.Save(Path.Combine(directory, material + "-page-shadow.png"));
                        Require(Light(nearLeft) < Light(farLeft) && Light(nearTop) < Light(farTop), "The native page shadow is not visible on both edges.");
                        log.Add($"PASS: {material}; left/top shadow visible; inset={inset}px; native page opaque.");
                        ribbon.IsBackstageOpen = false; continue;
                    }
                    nint handle = field.Handle; var parent = page.Parent;
                    ribbon.IsBackstageOpen = false; await Task.Delay(60);
                    using var behind = Capture(); var behindPage = behind.GetPixel(paper.X, paper.Y); var behindIcon = behind.GetPixel(icon.X, icon.Y);
                    Require(behindPage.R < 80 && behindIcon.G > 180, "Closing probes lack distinct underlying colors.");
                    ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.IsBackstageOpen = true;
                    await Finish(); await Task.Delay(45);
                    using (var ready = Capture()) ready.Save(Path.Combine(directory, material + "-backstage.png"));
                    ribbon.IsBackstageOpen = false; var watch = Stopwatch.StartNew(); int samples = 0, synchronized = 0; bool saved = false;
                    while (ribbon.IsBackstageOpen && watch.ElapsedMilliseconds < 1200)
                    {
                        using var frame = Capture(); var pagePixel = frame.GetPixel(paper.X, paper.Y); var iconPixel = frame.GetPixel(icon.X, icon.Y);
                        double pageOpacity = (pagePixel.R - behindPage.R) / (255d - behindPage.R);
                        double iconOpacity = (behindIcon.G - iconPixel.G) / (double)behindIcon.G;
                        log.Add($"{material}: t={watch.ElapsedMilliseconds}; page={pageOpacity:F3}; navigation={iconOpacity:F3}"); samples++;
                        if (samples <= 3) frame.Save(Path.Combine(directory, $"{material}-closing-frame-{samples}.png"));
                        var rail = frame.GetPixel(S(120), frame.Height - S(35));
                        Require(rail.R > 30 || rail.G > 30 || rail.B > 30, "Closing exposed an opaque black navigation rail.");
                        if (pageOpacity is > .1 and < .9 && iconOpacity is > .1 and < .9)
                        {
                            Require(Math.Abs(pageOpacity - iconOpacity) < .25, "Navigation and page did not fade together."); synchronized++;
                            if (!saved) { frame.Save(Path.Combine(directory, material + "-closing-mid.png")); saved = true; }
                        }
                        else Require(!(iconOpacity < .5 && pageOpacity > .95), "The white page underlay survived after navigation faded.");
                        await Task.Delay(12);
                    }
                    Require(!ribbon.IsBackstageOpen && synchronized > 0, "Closing did not present a synchronized intermediate frame.");
                    Require(field.Handle == handle && ReferenceEquals(page.Parent, parent) && field.Text == "Retained native page field", "Native field ownership/value changed.");
                    log.Add($"PASS: {material}; {samples} real closing frames, {synchronized} synchronized intermediate frames; field retained.");
                }
            }
            catch (Exception error) { exit = 1; log.Add("FAIL: " + error); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); form.Close(); }
        };
        WF.Application.Run(form); return exit;

        Bitmap Capture()
        {
            GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
            Require(foreground == Environment.ProcessId, "Foreground lost; capture stopped.");
            var image = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
            using var graphics = Graphics.FromImage(image); graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, image.Size); return image;
        }
        async Task Finish() { var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1200) await Task.Delay(10); Require(!ribbon.IsAnimating, "Opening did not finish."); }
    }
    private static IEnumerable<WF.Control> Descendants(WF.Control root)
    { foreach (WF.Control child in root.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class SquareIcon(Color color) : IRibbonIcon
    { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(color); graphics.FillRectangle(brush, bounds); } }
    private sealed class StateProbe : RibbonOffice2024Renderer
    {
        internal int State;
        public override void DrawQuickAccessItem(RibbonPaintContext context, RibbonElement entry, bool menu) => base.DrawQuickAccessItem(context with { Hot = State == 1, Pressed = State == 2 }, entry, menu);
        public override void DrawMinimizeButton(RibbonPaintContext context, bool minimized) => base.DrawMinimizeButton(context with { Hot = State == 1, Pressed = State == 2 }, minimized);
        public override void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu) => base.DrawFileButton(context with { Hot = State == 1, Pressed = State == 2 }, menu);
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected) => base.DrawTab(context with { Hot = State == 1 && tab.Text == "Insert", Pressed = State == 2 && tab.Text == "Insert" }, tab, selected);
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
}
