using System.Diagnostics;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class CaptionAppearanceCheck
{
    internal static void Verify(string output, bool observeHandoff = false)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = $"{family}-{backdrop}";
            using var host = new RibbonForm { Text = "Retained caption/title consumer", ShowIcon = false, StartPosition = FormStartPosition.Manual,
                Bounds = new(observeHandoff ? 140 : -16000, observeHandoff ? 120 : -16000, 1000, 620), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new CaptionProbeRibbon { BuiltInTheme = family, FileMenuMode = RibbonFileMenuMode.Backstage };
            using var page = new Panel(); var field = new TextBox { Text = "Retained native caption review", Bounds = new(20, 25, 260, 28) }; page.Controls.Add(field);
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
            var appearance = new RibbonBackstageAppearanceSettings { CaptionTintEnabled = true, CaptionTintColor = Color.FromArgb(128, Color.Navy), CaptionTintOpacity = .5, MarkerWidth = 7 };
            ribbon.Theme = ribbon.Theme with { BackstageAppearance = appearance, BackstageMotion = new() { PaneDuration = 160, PaneEasing = RibbonBackstageEasing.Linear, TitleEasing = RibbonBackstageEasing.Linear } };
            host.Controls.Add(ribbon); host.Ribbon = ribbon; Exception? failure = null;
            host.Shown += async (_, _) =>
            {
                try
                {
                    int samples = 0, profiles = 0, handbacks = 0, screens = 0, skippedScreens = 0;
                    foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
                    foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
                    {
                        ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
                        ribbon.IsBackstageOpen = true; Require(ribbon.IsAnimating, "The real pane timer did not start.");
                        samples += await Finish(ribbon);
                        var surface = host.Controls.Cast<Control>().Single(c => c.AccessibleName == "Backstage");
                        var handle = field.Handle; var parent = field.Parent; field.Select(2, 5);
                        Require(ribbon.Theme.BackstageExtendsIntoTitleBar ? surface.Top == 0 : surface.Top > 0, "Tint changed pane/caption ownership.");
                        ribbon.IsBackstageOpen = false; await Task.Delay(32); Require(ribbon.IsAnimating && ribbon.IsBackstageOpen, "Animated close did not retain live ownership.");
                        ribbon.IsBackstageOpen = true; samples += await Finish(ribbon);
                        Require(field.Handle == handle && ReferenceEquals(parent, field.Parent) && field.Text == "Retained native caption review" && field.SelectionStart == 2 && field.SelectionLength == 5, "Close reversal changed native state.");
                        ribbon.WatchedPlane = FindWindowEx(host.Handle, 0, null, "Ribbon motion"); ribbon.HandbackPaints = 0;
                        ribbon.IsBackstageOpen = false; samples += await Finish(ribbon);
                        Require(!ribbon.IsBackstageOpen && !ribbon.IsAnimating, "Close did not restore normal caption state."); profiles++;
                        if (observeHandoff && ribbon.WatchedPlane != 0)
                        {
                            Require(ribbon.HandbackPaints > 0 && !IsWindow(ribbon.WatchedPlane), "Caption plane retired before its normal replacement painted."); handbacks++;
                        }
                        if (observeHandoff && GetForegroundWindow() == host.Handle)
                        {
                            DwmFlush();
                            using var screen = new Bitmap(ribbon.Width, Math.Min(64, ribbon.Height));
                            using (var graphics = Graphics.FromImage(screen)) graphics.CopyFromScreen(ribbon.PointToScreen(Point.Empty), Point.Empty, screen.Size);
                            screen.Save(Path.Combine(output, $"{scope}-{mode}-{density}-closed-screen.png"));
                            screens++;
                        }
                        else if (observeHandoff) skippedScreens++;
                    }
                    ribbon.AnimationLevel = RibbonAnimationLevel.None;
                    ribbon.Theme = ribbon.Theme with { BackstageExtendsIntoTitleBar = false }; ribbon.IsBackstageOpen = true;
                    using (var image = Print(ribbon)) image.Save(Path.Combine(output, scope + "-authored-tint.png"));
                    ribbon.Theme = ribbon.Theme with { BackstageAppearance = appearance with { CaptionTintEnabled = false } };
                    using (var image = Print(ribbon)) image.Save(Path.Combine(output, scope + "-disabled-tint.png"));
                    // Direct native print is deterministic in solid mode. DWM
                    // material captures do not establish desktop composition.
                    if (host.EffectiveBackdrop == RibbonWindowBackdrop.None)
                    {
                        using var disabled = Print(ribbon);
                        ribbon.Theme = ribbon.Theme with { BackstageAppearance = appearance };
                        using var tinted = Print(ribbon);
                        var point = new Point(ribbon.Width / 2, 4);
                        Require(disabled.GetPixel(point.X, point.Y) != tinted.GetPixel(point.X, point.Y), "Public tint settings did not reach caption paint.");
                        host.TitleBarColor = Color.Purple;
                        using var authored = Print(ribbon);
                        Require(authored.GetPixel(point.X, point.Y).ToArgb() == Color.Purple.ToArgb(), "Authored Form caption lost precedence."); host.TitleBarColor = Color.Empty;
                    }
                    var selected = ribbon.Theme; var selectedBuiltIn = ribbon.BuiltInTheme;
                    foreach (double invalid in new[] { -.1, 1.1, double.NaN, double.PositiveInfinity })
                    {
                        bool rejected = false;
                        try { ribbon.Theme = selected with { BackstageAppearance = appearance with { CaptionTintOpacity = invalid } }; }
                        catch (ArgumentOutOfRangeException) { rejected = true; }
                        Require(rejected && ReferenceEquals(ribbon.Theme, selected) && ribbon.BuiltInTheme == selectedBuiltIn, "Invalid tint changed theme/selection.");
                    }
                    ribbon.Theme = selected with { BackstageAppearance = appearance with { CaptionTintEnabled = null, CaptionTintColor = Color.Empty, CaptionTintOpacity = null } };
                    Require(ribbon.Theme.BackstageAppearance.MarkerWidth == 7, "Tint Reset cleared navigation appearance.");
                    evidence.Add($"PASS {scope}: effective={host.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {profiles} Classic/Simplified x Compact/Touch profiles, {samples} real timer samples, {handbacks} observed native caption handbacks; foreground screen captures={screens}, skipped (owner not foreground)={skippedScreens}; opening/close/reversal, caption ownership, native HWND/parent/value/selection, public alpha/strength/disable paint, authored Form precedence, atomic validation and independent Reset.");
                }
                catch (Exception error) { failure = error; }
                finally { host.Close(); }
            };
            Application.Run(host); if (failure is not null) throw new InvalidOperationException(scope + " failed.", failure);
        }
        evidence.Add(observeHandoff ? "Public-package native handback observation; endpoint screen capture requires the owner to be foreground. This observes owner paint before HWND retirement; it is not a continuous video/frame-pacing acceptance. Actual VS, physical input/IME and mixed-monitor review remain separate." : "Offscreen public-package native evidence. Direct print cannot verify DWM material pixels or transparent caption-plane presentation; opaque title glyph/clip frames are covered by focused runtime tests. Visible pacing/flicker, actual VS, physical input/IME and mixed-monitor review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static async Task<int> Finish(RibbonControl ribbon)
    {
        int frames = 0; var watch = Stopwatch.StartNew();
        while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) { await Task.Delay(16); frames++; }
        Require(!ribbon.IsAnimating, "Caption motion did not complete within its configured duration."); return frames;
    }
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class CaptionProbeRibbon : RibbonControl
    {
        internal nint WatchedPlane;
        internal int HandbackPaints;
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!IsBackstageOpen && !IsAnimating && WatchedPlane != 0 && IsWindow(WatchedPlane)) HandbackPaints++;
        }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern nint FindWindowEx(nint parent, nint after, string? className, string caption);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool IsWindow(nint window);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
