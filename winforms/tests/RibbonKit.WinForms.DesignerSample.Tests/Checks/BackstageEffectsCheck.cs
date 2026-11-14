using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Exercises the public package with native pages and real timers; no runtime
// internals, Showcase resources, global input or page screenshot presenters.
internal static class BackstageEffectsCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var form = new RibbonForm { Text = "Backstage adjustable effects", StartPosition = FormStartPosition.Manual,
                Bounds = new(-16000, -16000, 1000, 650), ShowInTaskbar = false, Backdrop = backdrop };
            using var painter = new Probe(Path.Combine(output, scope + "-selected-fill.png"));
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage };
            using var first = new Panel(); using var second = new Panel();
            var field = new TextBox { Text = "Retained native effects", Bounds = new(20, 25, 260, 28) }; first.Controls.Add(field);
            second.Controls.Add(new Label { Text = "Print settings", AutoSize = true, Location = new(20, 25) });
            var info = new RibbonBackstagePageItem { Text = "Info", Content = first }; var print = new RibbonBackstagePageItem { Text = "Print", Content = second };
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.Backstage.Items.AddRange(info, print);
            var options = new RibbonBackstageMotionSettings { PaneDuration = 180, PageDuration = 120, PageSlideDistance = 18,
                PaneFadeAmount = 0, PaneEasing = RibbonBackstageEasing.Linear, PageEasing = RibbonBackstageEasing.Linear };
            ribbon.Theme = ribbon.Theme with { BackstageMotion = options };
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            Exception? failure = null, threadFailure = null;
            ThreadExceptionEventHandler failed = (_, args) => threadFailure ??= args.Exception; Application.ThreadException += failed;
            form.Shown += async (_, _) =>
            {
                try
                {
                    form.Activate(); ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
                    ribbon.IsBackstageOpen = true; Require(ribbon.IsAnimating, "Real pane motion did not start under the current system policy.");
                    var surface = form.Controls.Cast<Control>().Single(control => control.AccessibleName == "Backstage");
                    Require(NearestAlpha(first) == 255, "Slide-only preparation seeded a transparent native pane.");
                    int start = surface.Left; int paneFrames = await FinishMotion(ribbon);
                    Require(start < surface.Left && surface.Left == 0 && paneFrames >= 2, "The custom pane transition did not move on real timer frames.");
                    nint handle = field.Handle; var parent = first.Parent; field.Select(2, 4);

                    ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; ribbon.Backstage.SelectedPage = print;
                    Require(ribbon.IsAnimating && second.Parent!.Top == -(int)Math.Round(18 * ribbon.DeviceDpi / 96d), "The authored page travel did not reach its native slot.");
                    Require(NearestAlpha(second) == 255, "Opaque page travel unexpectedly faded.");
                    int pageFrames = await FinishMotion(ribbon); Require(second.Parent!.Top == 0 && pageFrames >= 2, "The page slide did not finish on real timer frames.");
                    ribbon.TabContentAnimation = RibbonTabContentAnimation.Fade; ribbon.Backstage.SelectedPage = info;
                    Require(ribbon.IsAnimating && NearestAlpha(first) == 1, "Pane fade strength incorrectly disabled the independent page Fade style.");
                    await FinishMotion(ribbon); Require(NearestAlpha(first) == 255, "The native page did not finish at full opacity.");

                    var selected = Color.FromArgb(160, 35, 125, 215);
                    painter.Expected = selected;
                    ribbon.Theme = ribbon.Theme with { BackstageFullWidthNavigation = false,
                        BackstageNavigationSelected = new(selected), BackstageNavigationHover = new(Color.FromArgb(128, 210, 40, 100)),
                        BackdropHover = new(Color.Lime), BackdropPressed = new(Color.Yellow), BackstageSelectionIndicatorVisible = false };
                    form.Refresh();
                    // DWM need not paint an offscreen rail. Print only the
                    // library-owned button; application pages stay untouched.
                    var entry = Descendants(surface).OfType<Button>().Single(button => button.Text == "Info");
                    using (var frame = new Bitmap(entry.Width, entry.Height)) entry.DrawToBitmap(frame, entry.ClientRectangle);
                    Require(painter.Matches + painter.Mismatches > 0, "The native selected-row callback was not captured.");
                    Require(painter.Matches > 0 && painter.Mismatches == 0, "The native callback's inset state ignored its authored navigation fill or alpha: " + painter.LastPixel);
                    Require(field.Handle == handle && ReferenceEquals(first.Parent, parent) && field.Text == "Retained native effects" && field.SelectionStart == 2 && field.SelectionLength == 4,
                        "Authored effects changed native editor handle, parent, value or selection.");
                    ribbon.Theme = ribbon.Theme with { BackstageMotion = options with { PaneDuration = 0, PageDuration = 0 } };
                    int closed = 0; ribbon.Backstage.Closed += (_, _) => closed++;
                    ribbon.IsBackstageOpen = false; Require(!ribbon.IsBackstageOpen && !ribbon.IsAnimating && closed == 1, "Zero-duration close did not finish synchronously once.");
                    ribbon.IsBackstageOpen = true; Require(ribbon.IsBackstageOpen && !ribbon.IsAnimating, "Zero-duration opening started motion.");
                    ribbon.Backstage.SelectedPage = print; Require(!ribbon.IsAnimating, "Zero-duration page selection started motion.");
                    ribbon.IsBackstageOpen = false;
                    ribbon.Theme = ribbon.Theme with { BackstageMotion = null }; Require(ribbon.Theme.BackstageMotion is null, "Motion group did not reset independently.");
                    Require(threadFailure is null, "A native paint callback failed: " + threadFailure);
                    evidence.Add($"PASS {scope}: requested={backdrop}, effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; pane/page timer samples={paneFrames}/{pageFrames}; opaque preparation, custom page distance, independent page Fade, authored inset fill/alpha, zero durations, reset and retained native state.");
                }
                catch (Exception error) { failure = error; }
                finally { form.Close(); }
            };
            try { Application.Run(form); }
            finally { Application.ThreadException -= failed; }
            if (failure is not null) throw new InvalidOperationException(scope + " failed.", failure);
        }
        evidence.Add("Public-package offscreen native checks. Visible flicker/frame pacing, actual VS, physical input, IME and mixed-monitor acceptance are separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static async Task<int> FinishMotion(RibbonControl ribbon)
    {
        int frames = 0; var watch = Stopwatch.StartNew();
        while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1600) { await Task.Delay(16); frames++; }
        Require(!ribbon.IsAnimating, "Authored motion did not complete within its bounded duration."); return frames;
    }
    private static byte NearestAlpha(Control page)
    {
        for (Control? control = page; control is not null; control = control.Parent)
            if (GetLayeredWindowAttributes(control.Handle, out _, out byte alpha, out _)) return alpha;
        throw new InvalidOperationException("The native motion subtree has no opacity owner.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private sealed class Probe(string output) : RibbonOffice2024Renderer
    {
        internal Color? Expected;
        internal int Matches, Mismatches;
        internal string? LastPixel;
        public override void DrawBackstageEntry(RibbonPaintContext context, RibbonItem? item, bool selected)
        {
            if (Expected is { } color && selected && item?.Text == "Info")
            {
                // Replay the real native callback's resolved context through the
                // same public renderer, so alpha can be checked independently of DWM.
                using var image = new Bitmap(240, 52); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
                base.DrawBackstageEntry(context with { Graphics = graphics, Bounds = new(0, 0, 240, 52) }, item, true);
                using var expected = new Bitmap(1, 1); using var reference = Graphics.FromImage(expected);
                reference.Clear(context.Theme.BackstageNavigationUsesAccent ? context.Theme.Accent : context.Theme.BackstageNavigationBackground);
                using var fill = new SolidBrush(color); reference.FillRectangle(fill, 0, 0, 1, 1);
                var actualColor = image.GetPixel(210, 26); var expectedColor = expected.GetPixel(0, 0);
                LastPixel = $"actual={actualColor}, expected={expectedColor}";
                if (actualColor.ToArgb() == expectedColor.ToArgb()) Matches++; else Mismatches++;
                if (Matches + Mismatches == 1) image.Save(output);
            }
            base.DrawBackstageEntry(context, item, selected);
        }
    }
    [DllImport("user32.dll")] private static extern bool GetLayeredWindowAttributes(nint window, out uint color, out byte alpha, out uint flags);
}
