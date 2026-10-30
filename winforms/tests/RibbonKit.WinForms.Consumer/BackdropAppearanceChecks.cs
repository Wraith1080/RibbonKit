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
    internal static int Run(string directory, bool closingOnly = false, bool shadowOnly = false, bool captionOnly = false)
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
                foreach (var material in closingOnly || shadowOnly || captionOnly ? new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic }
                    : new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                {
                    form.Backdrop = material; Require(form.EffectiveBackdrop == material, "Material did not apply.");
                    foreach (int state in closingOnly || shadowOnly || captionOnly ? [] : new[] { 0, 1, 2 })
                    {
                        renderer.State = state; ribbon.Invalidate(); await Task.Delay(65);
                        using var shot = Capture(); shot.Save(Path.Combine(directory, $"{material}-state-{state}.png"));
                    }
                    renderer.State = 0; ribbon.Invalidate(); ribbon.AnimationLevel = RibbonAnimationLevel.None;
                    ribbon.IsBackstageOpen = true; await Task.Delay(40);
                    if (captionOnly)
                    {
                        foreach (float opacity in new[] { 1f, .65f, 0f })
                        {
                            ribbon.Backstage.NavigationBackgroundOpacity = opacity; await Task.Delay(60); using var shot = Capture();
                            var header = shot.GetPixel(S(150), S(3));
                            if (material == RibbonWindowBackdrop.None || opacity == 1)
                            {
                                Require(header.ToArgb() == ribbon.Theme.BackstageNavigationBackground.ToArgb(), "Caption did not match the opaque rail color.");
                                Require(shot.GetPixel(S(100), shot.Height - S(35)).ToArgb() == header.ToArgb(), "Opaque caption/rail colors differ.");
                            }
                            shot.Save(Path.Combine(directory, $"{material}-caption-{opacity}.png"));
                        }
                        ribbon.IsBackstageOpen = false; await Task.Delay(45);
                        using var closed = Capture();
                        if (material == RibbonWindowBackdrop.None) Require(closed.GetPixel(S(150), S(3)).ToArgb() == ribbon.Theme.Background.ToArgb(), "Normal caption did not restore after closing.");
                        log.Add($"PASS: {material}; Backstage caption matches rail, opacity variants captured, normal caption restored.");
                        continue;
                    }
                    var entry = Descendants(form).OfType<WF.Button>().Single(button => button.Text == "Info");
                    int S(int n) => (int)Math.Round(n * form.DeviceDpi / 96d);
                    var icon = form.PointToClient(entry.PointToScreen(new(S(24), entry.Height / 2)));
                    var paper = form.PointToClient(page.PointToScreen(new(page.Width - S(30), S(300))));
                    if (shadowOnly)
                    {
                        await Task.Delay(60); using var shot = Capture();
                        var origin = form.PointToClient(page.PointToScreen(Point.Empty));
                        {
                            var surface = Descendants(form).Single(control => control.AccessibleName == "Backstage");
                            int border = renderer.GetBackstagePageBorderWidth(ribbon.Theme, form.DeviceDpi);
                            int top = form.PointToClient(surface.PointToScreen(Point.Empty)).Y;
                            Require(origin.Y == top + border, "Page retained a top shadow band.");
                            Require(shot.GetPixel(origin.X + S(100), top).ToArgb() == ribbon.Theme.Border.ToArgb(), "Top border was hidden by native content.");
                            Require(shot.GetPixel(origin.X - border, origin.Y + S(100)).ToArgb() == ribbon.Theme.Border.ToArgb(), "Left border was hidden by native content.");
                            var corner = shot.GetPixel(origin.X - border, top);
                            if (material == RibbonWindowBackdrop.None) Require(corner.ToArgb() == ribbon.Theme.BackstageNavigationBackground.ToArgb(), "The top-left page corner remained square.");
                            else Require(corner.ToArgb() != ribbon.Theme.Surface.ToArgb(), "The native material page covered its rounded corner.");
                            shot.Save(Path.Combine(directory, material + "-page-frame.png"));
                            log.Add($"PASS: {material}; no reserved top band; grey top/left border and rounded top-left corner visible.");
                            ribbon.IsBackstageOpen = false; continue;
                        }
                    }
                    nint handle = field.Handle; var parent = page.Parent;
                    ribbon.IsBackstageOpen = false;
                    foreach (var opacity in material == RibbonWindowBackdrop.None ? new[] { 1f } : new[] { 1f, .65f, 0f })
                    foreach (var level in opacity == 1 ? new[] { RibbonAnimationLevel.Subtle, RibbonAnimationLevel.Expressive } : new[] { RibbonAnimationLevel.Expressive })
                    {
                        ribbon.Backstage.NavigationBackgroundOpacity = opacity;
                        string label = $"{material}-{level}-{opacity}";
                        ribbon.Density = level == RibbonAnimationLevel.Subtle ? RibbonDensity.Compact : RibbonDensity.Touch;
                        ribbon.AnimationLevel = level; ribbon.Refresh();
                        renderer.ToolbarPaints = 0; renderer.ToolbarItemPaints = 0; ribbon.IsBackstageOpen = true;
                        var surface = Descendants(form).Single(control => control.AccessibleName == "Backstage");
                        Require(surface.Right / (double)surface.Width is > .849 and < .851 && ribbon.IsAnimating, "Opening did not start 85% visible.");
                        await TraceSlide(false); Require(ribbon.IsBackstageOpen && surface.Left == 0, "Opening did not reach its final location.");
                        Require(renderer.ToolbarPaints == 0 && renderer.ToolbarItemPaints == 0, "QAT remained visible during Backstage opening.");
                        using (var ready = Capture())
                        {
                            ready.Save(Path.Combine(directory, $"{label}-open.png"));
                            var bounds = form.PointToClient(field.PointToScreen(Point.Empty)); int ink = 0;
                            for (int y = bounds.Y + 2; y < bounds.Y + field.Height - 2; y++)
                            for (int x = bounds.X + 3; x < bounds.X + Math.Min(S(200), field.Width - 3); x++)
                            { var pixel = ready.GetPixel(x, y); if (pixel.R < 90 && pixel.G < 90 && pixel.B < 90) ink++; }
                            Require(ink > 40, "The native field's text was missing after opening.");
                        }
                        ribbon.IsBackstageOpen = false; Require(ribbon.IsBackstageOpen && ribbon.IsAnimating, "Closing did not retain its lifecycle.");
                        await TraceSlide(true); Require(!ribbon.IsBackstageOpen && surface.Left == 0, "Closing did not complete and park the surface.");
                        Require(field.Handle == handle && ReferenceEquals(page.Parent, parent) && field.Text == "Retained native page field", "Native field ownership/value changed.");
                        ribbon.Refresh(); Require(renderer.ToolbarPaints > 0 && renderer.ToolbarItemPaints > 0 && ribbon.QuickAccessVisible, "QAT did not return after closing.");
                        log.Add($"PASS: {label}; complete live slide and caption edge, retained native field; DPI={form.DeviceDpi}.");

                        async Task TraceSlide(bool closing)
                        {
                            var watch = Stopwatch.StartNew(); int previous = surface.Left, intermediate = 0;
                            while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500)
                            {
                                int edge = surface.Right, left = surface.Left;
                                Require(surface.Right / (double)surface.Width >= .849, "Slide exceeded its 15% travel.");
                                Require(renderer.ToolbarPaints == 0 && renderer.ToolbarItemPaints == 0, "QAT appeared before Backstage finished closing.");
                                Require(closing ? left <= previous : left >= previous, "Surface reversed direction."); previous = left;
                                using var frame = Capture();
                                if (edge > S(20) && edge < frame.Width - S(20))
                                {
                                    intermediate++;
                                    frame.Save(Path.Combine(directory, $"{label}-{(closing ? "close" : "open")}-{intermediate}.png"));
                                    if (opacity == 1)
                                    {
                                        Require(frame.GetPixel(edge + S(4), S(3)).ToArgb() != ribbon.Theme.BackstageNavigationBackground.ToArgb(), "Caption tint crossed the moving surface edge.");
                                    }
                                    var pagePixel = frame.GetPixel(edge - S(8), surface.Top + S(300));
                                    Require(pagePixel.R + pagePixel.G + pagePixel.B > 90, "The fading native page exposed an opaque black surface.");
                                }
                                await Task.Delay(12);
                            }
                            Require(!ribbon.IsAnimating && intermediate > 0, "Slide did not present an intermediate frame and finish autonomously.");
                        }
                    }
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
    }
    private static IEnumerable<WF.Control> Descendants(WF.Control root)
    { foreach (WF.Control child in root.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class SquareIcon(Color color) : IRibbonIcon
    { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(color); graphics.FillRectangle(brush, bounds); } }
    private sealed class StateProbe : RibbonOffice2024Renderer
    {
        internal int State;
        internal int ToolbarPaints, ToolbarItemPaints;
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
        { ToolbarPaints++; base.DrawQuickAccessToolbar(context, position); }
        public override void DrawQuickAccessItem(RibbonPaintContext context, RibbonElement entry, bool menu)
        { ToolbarItemPaints++; base.DrawQuickAccessItem(context with { Hot = State == 1, Pressed = State == 2 }, entry, menu); }
        public override void DrawMinimizeButton(RibbonPaintContext context, bool minimized) => base.DrawMinimizeButton(context with { Hot = State == 1, Pressed = State == 2 }, minimized);
        public override void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu) => base.DrawFileButton(context with { Hot = State == 1, Pressed = State == 2 }, menu);
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected) => base.DrawTab(context with { Hot = State == 1 && tab.Text == "Insert", Pressed = State == 2 && tab.Text == "Insert" }, tab, selected);
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
}
