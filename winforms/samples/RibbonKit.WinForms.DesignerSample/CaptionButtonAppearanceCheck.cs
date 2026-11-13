using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class CaptionButtonAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = $"{family}-{backdrop}";
            using var form = new StateForm { Text = "Independent caption-button consumer", ShowIcon = false, StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 620), Backdrop = backdrop, ShowInTaskbar = false };
            using var ribbon = new RibbonControl { BuiltInTheme = family }; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
            using var field = new TextBox { Text = "Retained native caption field", Location = new(20, 280) }; form.Controls.Add(field);
            form.Show(); Application.DoEvents(); var handle = field.Handle; var parent = field.Parent; field.Select(2, 5);
            int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            var original = ribbon.Theme;
            var a = new RibbonCaptionButtonAppearanceSettings { HoverBackground = new(Color.Orange), PressedBackground = new(Color.Blue), CloseHoverBackground = new(Color.Purple), ClosePressedBackground = new(Color.DarkRed), GlyphSize = 0, StateInset = 3, CornerRadius = 8 };
            int profiles = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density;
                var before = Targets(form, S(18)); Require(before.Count == 3, "Caption fixture did not expose all native hit targets.");
                ribbon.Theme = original with { CaptionButtonAppearance = a };
                foreach (var (hit, pressed, color) in new[] { (8, false, Color.Orange), (8, true, Color.Blue), (20, false, Color.Purple), (20, true, Color.DarkRed) })
                {
                    form.State(hit, pressed); using var image = Print(ribbon); var bounds = before[hit];
                    Require(image.GetPixel(bounds.Left + bounds.Width / 2, S(6)).ToArgb() == color.ToArgb(), "Public caption state override did not reach native paint.");
                }
                form.State(20, false);
                Color opaque;
                using (var image = Print(ribbon)) { var bounds = before[20]; opaque = image.GetPixel(bounds.Left + bounds.Width / 2, S(6)); }
                ribbon.Theme = original with { CaptionButtonAppearance = a with { CloseHoverBackground = new(Color.FromArgb(128, Color.Purple)) } };
                using (var image = Print(ribbon))
                {
                    var bounds = before[20]; var pixel = image.GetPixel(bounds.Left + bounds.Width / 2, S(6));
                    // Native DrawToBitmap can flatten material alpha. Verify
                    // its changed paint response here; exact alpha is checked
                    // in the runtime DIB tests, not inferred from this facade.
                    Require(pixel.ToArgb() != opaque.ToArgb(), "Authored caption alpha did not change native paint response.");
                }
                ribbon.Theme = original with { CaptionButtonAppearance = a with { CloseHoverBackground = new(Color.Transparent) } };
                using (var hidden = Print(ribbon)) hidden.Save(Path.Combine(output, $"{scope}-{mode}-{density}-hidden.png"));
                var glyphs = a with { GlyphSize = 18, GlyphStrokeWidth = 2, RestoreOffset = 6, GlyphColor = Color.DarkOrange, CloseHotGlyphColor = Color.Gold };
                ribbon.Theme = original with { CaptionButtonAppearance = glyphs };
                using (var image = Print(ribbon)) image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-glyphs.png"));
                Require(before.SequenceEqual(Targets(form, S(18))), "Caption decoration changed native hit targets.");
                form.WindowState = FormWindowState.Maximized; form.State(9, false);
                using (var image = Print(ribbon)) image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-restore.png"));
                form.WindowState = FormWindowState.Normal;
                var selected = ribbon.Theme; var builtIn = ribbon.BuiltInTheme;
                foreach (double invalid in new[] { -1d, 33d, double.NaN, double.PositiveInfinity })
                {
                    bool rejected = false; try { ribbon.Theme = selected with { CaptionButtonAppearance = a with { GlyphStrokeWidth = invalid } }; } catch (ArgumentOutOfRangeException) { rejected = true; }
                    Require(rejected && ReferenceEquals(selected, ribbon.Theme) && ribbon.BuiltInTheme == builtIn, "Invalid caption settings mutated selection.");
                }
                ribbon.Theme = selected with { CaptionButtonAppearance = glyphs with { HoverBackground = null, PressedBackground = null, CloseHoverBackground = null, ClosePressedBackground = null } };
                Require(ribbon.Theme.CaptionButtonAppearance.GlyphSize == 18 && ribbon.Theme.CaptionButtonAppearance.StateInset == 3, "Fill Reset cleared glyph/shape.");
                ribbon.Theme = selected with { CaptionButtonAppearance = null }; Require(ribbon.Theme.CaptionButtonAppearance is null, "Caption group did not reset.");
                Require(field.Handle == handle && ReferenceEquals(parent, field.Parent) && field.Text == "Retained native caption field" && field.SelectionStart == 2 && field.SelectionLength == 5, "Caption changes altered native state."); profiles++;
            }
            var effective = form.EffectiveBackdrop; int dpi = ribbon.DeviceDpi; form.State(0, false); form.Close();
            evidence.Add($"PASS {scope}: effective={effective}, dpi={dpi}; {profiles} Classic/Simplified x Compact/Touch profiles; native hover/pressed/close fill callbacks, authored alpha, glyph/restore captures, unchanged WM_NCHITTEST targets, atomic validation, independent/group Reset and retained HWND/parent/value/selection.");
        }
        using (var demo = new DesignerForm()) demo.VerifyCaptionButtonDemo(output);
        evidence.Add("PASS new Theme effects 11: four caption-button review groups; tab 10 retains five and earlier tabs retain seven/five/four/seven/six/three/eight/five/six groups. Presets, alpha/hide/independent/all Reset, isolated preview model and idempotence pass.");
        evidence.Add("Public-package native/direct-print checks at actual device DPI. Synthetic nonclient state messages do not claim physical window actions, desktop material pixels, visible pacing, actual VS, IME or mixed-monitor acceptance.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static Dictionary<int, Rectangle> Targets(Form form, int y)
    {
        var result = new Dictionary<int, Rectangle>();
        for (int x = form.ClientSize.Width / 2; x < form.ClientSize.Width; x++)
        {
            var point = form.PointToScreen(new(x, y)); int hit = (int)SendMessage(form.Handle, 0x84, 0, (nint)((point.Y << 16) | (point.X & 0xffff)));
            if (hit is not (8 or 9 or 20)) continue;
            result[hit] = result.TryGetValue(hit, out var bounds) ? Rectangle.FromLTRB(bounds.Left, 0, x + 1, y * 2) : new(x, 0, 1, y * 2);
        }
        return result;
    }
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class StateForm : RibbonForm
    {
        internal void State(int hit, bool pressed)
        {
            Capture = false; var cancel = Message.Create(Handle, 0x1F, 0, 0); WndProc(ref cancel);
            var hover = Message.Create(Handle, 0xA0, hit, 0); WndProc(ref hover);
            if (pressed) { var down = Message.Create(Handle, 0xA1, hit, 0); WndProc(ref down); }
        }
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
