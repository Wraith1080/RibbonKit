using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Observe the real material ribbon after each native WM_PAINT has returned.
// A compositor readback at this boundary detects a clear frame even when the
// later timer frames look correct. All observed windows belong to this fixture.
internal static class TabMotionHandoffCheck
{
    internal static void Verify(string output, bool solidOnly = false, bool shadow = false)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var background = new Form { StartPosition = FormStartPosition.Manual, Bounds = new(140, 100, 1180, 850),
            BackColor = Color.LightSteelBlue, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
        Exception? failure = null;
        background.Shown += async (_, _) =>
        {
            try
            {
                foreach (var material in solidOnly ? new[] { RibbonWindowBackdrop.None } : new[] { RibbonWindowBackdrop.Mica, RibbonWindowBackdrop.Acrylic })
                foreach (var style in Enum.GetValues<RibbonTabContentAnimation>())
                foreach (bool addedCombos in new[] { false, true })
                {
                    using var form = new DesignerForm { Text = "Tab motion handoff check", StartPosition = FormStartPosition.Manual, Location = new(165, 125), Backdrop = material };
                    var ribbon = form.Controls.OfType<RibbonControl>().Single(); ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.TabContentAnimation = style;
                    var tabs = ribbon.Tabs.Where(tab => tab.Visible && tab.Enabled).Take(2).ToArray(); Require(tabs.Length == 2, "Keep two sample tabs");
                    if (!addedCombos)
                        foreach (var group in tabs[0].Groups)
                            foreach (var combo in group.Items.OfType<RibbonComboBoxItem>().ToArray()) group.Items.Remove(combo);
                    form.Show(background); form.Activate(); await Task.Delay(shadow ? 500 : 120);
                    Require(form.EffectiveBackdrop == material && GetForegroundWindow() == form.Handle, "Unavailable material or foreground loss");
                    using var probe = new PaintProbe(form, ribbon); probe.Capture("baseline");
                    Require(probe.Frames[0].White >= 95, "Probe must start inside the opaque white ribbon body");
                    var editors = Descendants(ribbon).Where(control => control is TextBox or ComboBox).ToArray();
                    var handles = editors.ToDictionary(control => control, control => control.Handle);
                    var tag = $"{material}-{style}-combos{addedCombos}";
                    probe.Enabled = true;
                    for (int change = 0; change < 4; change++)
                    {
                        if (shadow)
                        {
                            probe.Capture("departure" + change);
                            probe.ShadowReference = probe.Frames.Last().Image;
                        }
                        probe.Stage = "switch" + change;
                        ribbon.SelectedTab = ReferenceEquals(ribbon.SelectedTab, tabs[0]) ? tabs[1] : tabs[0];
                        Require(ribbon.IsAnimating, "Native client animation must be enabled for this check");
                        var clock = Stopwatch.StartNew();
                        while (ribbon.IsAnimating && clock.ElapsedMilliseconds < 2000) await Task.Delay(8);
                        Require(!ribbon.IsAnimating, "Animation failed to complete");
                        probe.Capture("settled" + change);
                    }
                    probe.Enabled = false; Require(probe.Failure is null, "Native readback failed: " + probe.Failure);
                    int gaps = 0, shadowChanges = 0, worstShadow = 0;
                    for (int i = 0; i < probe.Frames.Count; i++)
                    {
                        var frame = probe.Frames[i]; if (frame.White < 95) gaps++;
                        if (frame.ShadowDelta > 6) shadowChanges++; worstShadow = Math.Max(worstShadow, frame.ShadowDelta);
                        frame.Image.Save(Path.Combine(output, $"{tag}-{i:00}.png"));
                        log.WriteLine($"{tag} frame={i:00} stage={frame.Stage} white={frame.White}% shadowDelta={frame.ShadowDelta}");
                    }
                    Require(handles.All(pair => !pair.Key.IsDisposed && pair.Key.Handle == pair.Value), "Retained native editor handles changed");
                    bool passed = gaps == 0 && (!shadow || shadowChanges == 0);
                    log.WriteLine($"{(passed ? "PASS" : "FAIL")}: {tag}; DPI={form.DeviceDpi}; native body-paint observations={probe.Frames.Count}; clear-body frames={gaps}; shadow changes={shadowChanges}, max channel delta={worstShadow}; editor handles retained.");
                    if (!passed) failure ??= new InvalidOperationException("The ribbon body or shadow changed during its animation handoff: " + tag);
                    form.Close();
                }
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { background.Close(); }
        };
        Application.Run(background); if (failure is not null) throw failure;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private sealed class PaintProbe : NativeWindow, IDisposable
    {
        private readonly Form _form; private readonly RibbonControl _ribbon; private bool _capturing;
        internal bool Enabled; internal string Stage = ""; internal Exception? Failure;
        internal Bitmap? ShadowReference;
        internal readonly List<(string Stage, int White, int ShadowDelta, Bitmap Image)> Frames = [];
        internal PaintProbe(Form form, RibbonControl ribbon) { _form = form; _ribbon = ribbon; AssignHandle(ribbon.Handle); }
        internal void Capture(string stage)
        {
            Require(GetForegroundWindow() == _form.Handle, "Foreground changed during owned-window capture");
            _ = DwmFlush(); var area = _ribbon.RectangleToScreen(_ribbon.ClientRectangle); var image = new Bitmap(area.Width, area.Height);
            using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
            int S(int value) => (int)Math.Round(value * _ribbon.DeviceDpi / 96d);
            int white = 0, total = 0;
            for (int y = S(96); y < S(112); y += 2)
            for (int x = area.Width - S(120); x < area.Width - S(80); x += 2)
            { var p = image.GetPixel(x, y); if (p.R >= 250 && p.G >= 250 && p.B >= 250) white++; total++; }
            int shadowDelta = 0;
            if (ShadowReference is { } reference)
            {
                int left = S(_ribbon.Theme.BodyMargin.Left), right = image.Width - S(_ribbon.Theme.BodyMargin.Right);
                int bottom = image.Height - S(_ribbon.Theme.BodyMargin.Bottom);
                void Compare(int x, int y)
                {
                    var previous = reference.GetPixel(x, y); var current = image.GetPixel(x, y);
                    shadowDelta = Math.Max(shadowDelta, Math.Max(Math.Abs(previous.R - current.R), Math.Max(Math.Abs(previous.G - current.G), Math.Abs(previous.B - current.B))));
                }
                for (int y = S(100); y < bottom - S(20); y += 2)
                { for (int x = 1; x < left - 1; x++) Compare(x, y); for (int x = right + 1; x < image.Width - 1; x++) Compare(x, y); }
                for (int y = bottom + 1; y < image.Height - 1; y++)
                for (int x = left + S(20); x < right - S(20); x += 3) Compare(x, y);
            }
            Frames.Add((stage, 100 * white / total, shadowDelta, image));
        }
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if (!Enabled || _capturing || message.Msg != 0xF) return;
            _capturing = true;
            try { Capture(Stage); }
            catch (Exception error) { Failure = error; Enabled = false; }
            finally { _capturing = false; }
        }
        public void Dispose() { ReleaseHandle(); foreach (var frame in Frames) frame.Image.Dispose(); }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
