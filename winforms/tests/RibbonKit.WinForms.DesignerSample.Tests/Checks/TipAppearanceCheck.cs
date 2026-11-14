using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class TipAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            var scope = family + "-" + backdrop; using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1300, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.Backstage };
            var tab = new RibbonTab { Text = "Home", KeyTip = "H" }; var group = new RibbonGroup { Text = "Commands" }; var command = new RibbonButtonItem { Text = "Search document", KeyTip = "AB", ScreenTipDescription = "Keep the full description", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; var field = new RibbonTextBoxItem { Text = "Native name", Value = "Retained root draft", InputWidth = 160 }; group.Items.AddRange(command, field); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.FileMenu.Visible = true;
            using var body = new UserControl(); var editor = new TextBox { Text = "Retained page draft", Bounds = new(15, 15, 200, 24) }; body.Controls.Add(editor); var page = new RibbonBackstagePageItem { Text = "Information", KeyTip = "PP", Content = body }; ribbon.Backstage.Items.Add(page); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            nint handle = editor.Handle; var original = ribbon.Theme; var settings = new RibbonTipAppearanceSettings { ScreenTipTitleBold = false, KeyTipInset = 2, KeyTipBorderWidth = 3, BackstageKeyTipRightClearance = 20, BackstageKeyTipHorizontalClearance = 24, BackstageKeyTipVerticalClearance = 10 }; int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; form.Activate(); Application.DoEvents(); Key(Keys.Escape, false); Key(Keys.Escape, false); Key(Keys.F10); Key(Keys.H); Print(ribbon); var root = painter.Badges["AB"]; var slots = painter.Commands.ToDictionary(p => p.Key, p => p.Value); Key(Keys.Escape, false); Key(Keys.Escape, false);
                ribbon.IsBackstageOpen = true; Application.DoEvents(); editor.Select(2, 5); var parent = body.Parent; var pageBounds = body.Bounds; Key(Keys.F10); Print(form); var before = painter.Badges["PP"];
                ribbon.Theme = original with { TipAppearance = settings, KeyTipBorder = Color.Navy }; painter.Check = true; painter.Failure = null; painter.OutlineChecks = 0; Control stage = body; while (stage.Parent is { } container && container is not Form) stage = container; Print(stage, "backstage"); var after = painter.Badges["PP"];
                int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
                Require(after.Left == before.Left - S(16) && after.Size == before.Size, "Actual Backstage badge placement bypassed settings.");
                Require(editor.Handle == handle && ReferenceEquals(body.Parent, parent) && body.Bounds == pageBounds && editor.Text == "Retained page draft" && editor.SelectionStart == 2 && editor.SelectionLength == 5 && ReferenceEquals(page, ribbon.Backstage.SelectedPage), "Tip settings replaced page/editor state.");
                ribbon.IsBackstageOpen = false; Key(Keys.Escape, false); Key(Keys.Escape, false); Key(Keys.F10); Key(Keys.H); Print(ribbon, "root"); Require(painter.Badges["AB"] == root && slots.All(p => painter.Commands.TryGetValue(p.Key, out var bounds) && bounds == p.Value), "Tip paint changed root badges or command targets.");
                using var image = new Bitmap(S(420), S(160)); using var g = Graphics.FromImage(image); g.Clear(ribbon.Theme.Surface); var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = density };
                painter.DrawPopupFrame(c); painter.DrawScreenTip(c, "Search document", command.ScreenTipDescription, new(S(12), S(12), S(390), S(40)), new(S(12), S(58), S(390), S(80))); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-content.png"));
                Require(painter.Failure is null && painter.TitleWasRegular && painter.OutlineChecks > 0, "Actual tip/text callbacks failed: " + painter.Failure); painter.Check = false; Key(Keys.Escape, false); Key(Keys.Escape, false);
                ribbon.IsBackstageOpen = true; Application.DoEvents(); ribbon.Theme = original; Key(Keys.F10); Print(form); Require(painter.Badges["PP"] == before && editor.Handle == handle && ReferenceEquals(body.Parent, parent), "Tip reset changed placement/native ownership."); ribbon.IsBackstageOpen = false; states++;
                void Print(Control control, string? name = null) { painter.Badges.Clear(); painter.Commands.Clear(); using var pixels = new Bitmap(control.Width, control.Height); control.DrawToBitmap(pixels, control.ClientRectangle); if (name is not null) pixels.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{name}.png")); }
                void Key(Keys key, bool required = true) { if (key == Keys.F10) { form.Activate(); if (ribbon.IsBackstageOpen) editor.Focus(); else ribbon.Focus(); } var message = Message.Create(ribbon.Handle, 0x100, (nint)(int)key, 0); bool handled = Application.FilterMessage(ref message) || ribbon.PreProcessMessage(ref message); if (required) Require(handled, "Keyboard route did not handle " + key); }
            }
            ribbon.Theme = original with { TipAppearance = settings with { BackstageKeyTipRightClearance = null } }; Require(ribbon.Theme.TipAppearance!.KeyTipBorderWidth == 3, "Member reset cleared badge outline."); ribbon.Theme = ribbon.Theme with { TipAppearance = null }; Require(ribbon.Theme.TipAppearance is null && command.ScreenTipDescription == "Keep the full description", "Group reset changed metadata.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual root and Backstage KeyTip/text hooks, authored outline and right clearance, unchanged command/badge/page bounds, retained editor HWND/parent/value/selection and selected source page, exact placement reset and independent/group Reset."); form.Close();
        }
        evidence.Add("Synchronous offscreen public-package rendering and library keyboard routes. ScreenTip content uses public rendering hooks here; real passive-window focus/ownership is covered by the STA checks. Physical hover/keys, visible pacing, external UIA, actual VS and mixed-monitor acceptance remain separate."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check, TitleWasRegular; internal int OutlineChecks; internal Exception? Failure;
        internal readonly Dictionary<string, Rectangle> Badges = [], Commands = [];
        public override void DrawKeyTip(RibbonPaintContext c, string keys)
        {
            Badges[keys] = c.Bounds;
            if (Check && c.Bounds.Width > 10 && c.Bounds.Height > 10)
            {
                try { using var image = new Bitmap(c.Bounds.Width, c.Bounds.Height); using var g = Graphics.FromImage(image); g.Clear(Color.White); base.DrawKeyTip(c with { Graphics = g, Bounds = new(Point.Empty, image.Size) }, ""); int inset = (int)Math.Round(2 * c.DeviceDpi / 96d); Require(image.GetPixel(inset, image.Height / 2).ToArgb() == Color.Navy.ToArgb(), "Badge outline settings were bypassed."); OutlineChecks++; }
                catch (Exception error) { Failure ??= error; }
            }
            base.DrawKeyTip(c, keys);
        }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { Commands[item.Text] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { if (Check && text == "Search document") TitleWasRegular = !c.Font.Bold; base.DrawText(c, text, ink, flags); }
    }
}
