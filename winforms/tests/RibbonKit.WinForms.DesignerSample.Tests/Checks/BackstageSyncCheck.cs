using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Package-only public API check of actual compositor frames, not settled bitmaps.
internal static class BackstageSyncCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        var results = new List<object>();
        foreach (var theme in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        {
            using var form = new RibbonForm { Text = "Backstage synchronization", TitleBarColor = theme == RibbonBuiltInTheme.Office2019Light ? Color.Navy : Color.Empty, ClientSize = new(1050, 580), StartPosition = FormStartPosition.Manual, Location = new(30, 30) };
            using var painter = new ProbeRenderer();
            using var ribbon = new RibbonControl { BuiltInTheme = theme, Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage };
            using var page = new Panel { BackColor = Color.White };
            var field = new TextBox { Text = "Retained native value", Location = new(20, 150) }; page.Controls.Add(field);
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            Exception? failure = null, threadFailure = null;
            ThreadExceptionEventHandler failed = (_, args) => threadFailure ??= args.Exception;
            Application.ThreadException += failed;
            var frames = new List<object>(); int measured = 0, maximumDelta = 0;
            var policyFrames = new List<object>();
            bool fullHeight = ribbon.Theme.BackstageExtendsIntoTitleBar;
            form.Shown += async (_, _) =>
            {
                try
                {
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        form.Activate(); form.BringToFront(); SetForegroundWindow(form.Handle); await Task.Delay(80);
                        GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
                        if (foreground == Environment.ProcessId) break;
                    }
                    nint handle = field.Handle; var parent = field.Parent;
                    // Warm native page/font painting before measuring the same
                    // repeat opening/closing sequence shown in the user's clip.
                    ribbon.AnimationLevel = RibbonAnimationLevel.None;
                    ribbon.IsBackstageOpen = true; form.Refresh(); await Task.Delay(60);
                    ribbon.IsBackstageOpen = false; form.Refresh(); await Task.Delay(40);
                    ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
                    foreach (bool closing in new[] { false, true })
                    {
                        string phase = closing ? "closing" : "opening";
                        ribbon.IsBackstageOpen = !closing;
                        Require(ribbon.IsAnimating, "System animation policy did not enable this real-time check.");
                        var surface = form.Controls.Cast<Control>().Single(c => c.AccessibleName == "Backstage");
                        var watch = Stopwatch.StartNew(); int index = 0, phaseMeasured = 0; bool moved = false;
                        do
                        {
                            await Task.Delay(16);
                            if (threadFailure is { } uiError) throw new InvalidOperationException("Native paint failed.", uiError);
                            if (!ribbon.IsAnimating) break;
                            Require((fullHeight ? surface.Top == 0 : surface.Top > 0) && surface.Bottom == form.ClientSize.Height,
                                "Backstage does not respect its theme's title-bar boundary.");
                            moved |= surface.Left < 0;
                            GetWindowThreadProcessId(GetForegroundWindow(), out uint process);
                            Require(process == Environment.ProcessId, "Capture stopped because the fixture lost foreground ownership.");
                            // Below the ribbon, the underlay is uniform. A tab
                            // revealed by fading can otherwise masquerade as a
                            // second pane edge even when the real edges agree.
                            int captionRow = 8, paneRow = Math.Min(form.ClientSize.Height - 8,
                                Math.Max(surface.Top + (int)Math.Round(48 * ribbon.DeviceDpi / 96d), ribbon.Bottom + (int)Math.Round(8 * ribbon.DeviceDpi / 96d)));
                            DwmFlush(); // Read one committed desktop frame, not rows straddling a compositor refresh.
                            using var image = new Bitmap(form.ClientSize.Width, paneRow + 8);
                            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, image.Size);
                            int expected = Math.Clamp(surface.Left + painter.NavigationWidth, 0, image.Width);
                            int? caption = fullHeight ? Edge(image, captionRow, expected) : null, body = Edge(image, paneRow, expected);
                            int? delta = fullHeight ? caption is { } a && body is { } b ? Math.Abs(a - b) : null : 0;
                            if (!fullHeight)
                                foreach (int x in new[] { 100, image.Width / 2, image.Width - 100 })
                                {
                                    var pixel = image.GetPixel(x, captionRow); var grey = ribbon.Theme.BackstageNavigationBackground;
                                    bool tinted = x < surface.Right;
                                    Require(pixel.R >= (tinted ? grey.R : 255) && pixel.R <= 255 &&
                                        pixel.G >= (tinted ? grey.G : 255) && pixel.G <= 255 &&
                                        pixel.B >= (tinted ? grey.B : 255) && pixel.B <= 255,
                                        "Office 2024's neutral caption tint did not respect the moving edge.");
                                }
                            if (delta is { } difference) { measured++; phaseMeasured++; maximumDelta = Math.Max(maximumDelta, difference); }
                            frames.Add(new { phase, index, elapsedMs = watch.ElapsedMilliseconds, caption, body, delta, surface.Left, surface.Top });
                            image.Save(Path.Combine(output, $"{theme}-{phase}-{index++:D2}.png"));
                            Require(delta is null or <= 2, $"{theme} {phase}: caption/pane edge mismatch of {delta} pixels.");
                            Require(field.Handle == handle && ReferenceEquals(field.Parent, parent) && field.Text == "Retained native value", "Native page state changed during motion.");
                        } while (watch.ElapsedMilliseconds < 800 && ribbon.IsAnimating);
                        Require(!ribbon.IsAnimating, "Backstage did not finish its bounded transition.");
                        Require(phaseMeasured >= 2, $"Too few visible {phase} frames were measured.");
                        Require(moved, $"The {phase} transition did not slide.");
                        Require(ribbon.IsBackstageOpen == !closing, "Backstage lifecycle did not finish correctly.");
                        form.Refresh(); DwmFlush();
                        using var settled = new Bitmap(form.ClientSize.Width, Math.Max(16, surface.Top));
                        using (var graphics = Graphics.FromImage(settled)) graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, settled.Size);
                        settled.Save(Path.Combine(output, $"{theme}-{phase}-settled.png"));
                        if (!fullHeight)
                        {
                            var expected = closing ? ribbon.Theme.Background : ribbon.Theme.BackstageNavigationBackground;
                            Require(settled.GetPixel(settled.Width / 2, 8).ToArgb() == expected.ToArgb(),
                                "Office 2024's settled neutral caption or close restoration is incorrect.");
                        }
                    }
                    Require(measured >= 4, "Too few visible transition frames were measured.");
                    var retainedSurface = form.Controls.Cast<Control>().Single(c => c.AccessibleName == "Backstage");
                    Bitmap CapturePolicy(string phase)
                    {
                        if (threadFailure is { } uiError) throw new InvalidOperationException("Native paint failed.", uiError);
                        GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
                        Require(foreground == Environment.ProcessId, "Policy capture stopped because the fixture lost foreground ownership.");
                        Require(field.Handle == handle && ReferenceEquals(field.Parent, parent) && field.Text == "Retained native value", "Native page state changed during policy checks.");
                        form.Refresh(); DwmFlush();
                        var image = new Bitmap(form.ClientSize.Width, Math.Min(form.ClientSize.Height, Math.Max(128, ribbon.Bottom + 64)));
                        try
                        {
                            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, image.Size);
                            image.Save(Path.Combine(output, $"{theme}-{phase}.png"));
                            policyFrames.Add(new { phase, form.ClientSize.Width, form.ClientSize.Height, retainedSurface.Left, retainedSurface.Top, ribbon.IsAnimating, ribbon.IsBackstageOpen });
                            return image;
                        }
                        catch { image.Dispose(); throw; }
                    }
                    void CheckPolicyCaption(Bitmap frame, Color authored, bool open)
                    {
                        Color navigation = ribbon.Theme.BackstageNavigationUsesAccent ? ribbon.Theme.Accent : ribbon.Theme.BackstageNavigationBackground;
                        Require(frame.GetPixel(100, 8).ToArgb() == (open && fullHeight ? navigation : authored).ToArgb(), "Navigation-side authored caption policy changed.");
                        Require(frame.GetPixel(frame.Width / 2, 8).ToArgb() == (open && fullHeight ? ribbon.Theme.Surface : authored).ToArgb(), "Page-side authored caption policy changed.");
                        if (open) Require(retainedSurface.Left == 0 && (fullHeight ? retainedSurface.Top == 0 : retainedSurface.Top > 0) && retainedSurface.Bottom == form.ClientSize.Height,
                            "Policy checks changed the settled pane/caption bounds.");
                    }
                    async Task FinishMotion()
                    {
                        var watch = Stopwatch.StartNew();
                        while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 800) await Task.Delay(16);
                        Require(!ribbon.IsAnimating, "Policy transition did not finish within its bounded duration.");
                    }
                    foreach (bool formColor in new[] { true, false })
                    {
                        ribbon.AnimationLevel = RibbonAnimationLevel.None;
                        ribbon.Theme = ribbon.Theme with { TitleBarBackground = Color.DarkGreen };
                        form.TitleBarColor = formColor ? Color.Navy : Color.Empty;
                        Color authored = formColor ? Color.Navy : Color.DarkGreen;
                        string source = formColor ? "form" : "theme";
                        ribbon.IsBackstageOpen = true;
                        using (var open = CapturePolicy(source + "-caption-open")) CheckPolicyCaption(open, authored, true);
                        form.ClientSize = formColor ? new(940, 540) : new(1120, 600);
                        using (var resized = CapturePolicy(source + "-caption-resized")) CheckPolicyCaption(resized, authored, true);
                        ribbon.IsBackstageOpen = false;
                        using (var closed = CapturePolicy(source + "-caption-closed")) CheckPolicyCaption(closed, authored, false);
                    }
                    ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
                    ribbon.IsBackstageOpen = true; await FinishMotion();
                    int closeEvents = 0; ribbon.Backstage.Closed += (_, _) => closeEvents++;
                    ribbon.IsBackstageOpen = false; await Task.Delay(16);
                    Require(ribbon.IsAnimating && ribbon.IsBackstageOpen, "The reversal check did not observe a pending animated close.");
                    using (var reversing = CapturePolicy("reversal-closing")) { }
                    Require(ribbon.IsAnimating && ribbon.IsBackstageOpen, "The close completed before the reversal request.");
                    ribbon.IsBackstageOpen = true; await FinishMotion();
                    Require(ribbon.IsBackstageOpen && closeEvents == 0, "Reversing the close raised Closed or hid Backstage.");
                    using (var reversed = CapturePolicy("reversal-open")) CheckPolicyCaption(reversed, Color.DarkGreen, true);
                    ribbon.IsBackstageOpen = false; await FinishMotion();
                    Require(!ribbon.IsBackstageOpen && closeEvents == 1, "The final close did not finish exactly once.");
                    using (var final = CapturePolicy("reversal-closed")) CheckPolicyCaption(final, Color.DarkGreen, false);
                    results.Add(new { theme = theme.ToString(), fullHeight, dpi = ribbon.DeviceDpi, frames = frames.Count, measured, maximumDelta,
                        policyFrames = policyFrames.Count, authoredCaptions = true, resize = true, reversal = true, passed = true });
                }
                catch (Exception error) { failure = error; }
                finally { form.Close(); }
            };
            try { Application.Run(form); }
            finally
            {
                Application.ThreadException -= failed;
                File.WriteAllText(Path.Combine(output, theme + "-frames.json"), JsonSerializer.Serialize(frames, new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(output, theme + "-policy-frames.json"), JsonSerializer.Serialize(policyFrames, new JsonSerializerOptions { WriteIndented = true }));
            }
            if (failure is not null) throw new InvalidOperationException(theme + " actual-frame check failed.", failure);
            File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: two package-only light-theme scopes; Office 2019 panes slide behind the transparent caption and Office 2024 stays below its separate title bar with animated neutral grey tint and close restoration. Opening/closing frames, authored Form/theme caption precedence, resizing and close reversal retain native page handles/parent/text. See results.json and frame logs for measured scope.");
    }

    private static int? Edge(Bitmap image, int y, int expected)
    {
        var left = image.GetPixel(expected / 2, y); var right = image.GetPixel(Math.Min(image.Width - 20, expected + 100), y);
        static int Distance(Color a, Color b) => (a.R - b.R) * (a.R - b.R) + (a.G - b.G) * (a.G - b.G) + (a.B - b.B) * (a.B - b.B);
        if (Distance(left, right) < 36) return null; // Near-transparent frames do not expose a measurable color edge.
        int edge = -1;
        for (int x = Math.Max(40, expected - 45); x < Math.Min(image.Width - 20, expected + 45); x++)
            if (Distance(image.GetPixel(x, y), left) < Distance(image.GetPixel(x, y), right)) edge = x;
        return edge < 0 ? null : edge + 1;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class ProbeRenderer : RibbonOffice2024Renderer
    {
        internal int NavigationWidth;
        public override void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
        { NavigationWidth = navigationWidth; base.DrawBackstageBackground(context, navigationWidth); }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
