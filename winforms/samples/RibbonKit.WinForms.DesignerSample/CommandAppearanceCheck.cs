using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class CommandAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var painter = new Probe(Path.Combine(output, scope));
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1100, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained D1 input", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var split = new RibbonSplitButtonItem { Text = "Split", CheckOnClick = true, Checked = true, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var child = new RibbonButtonItem { Text = "Menu action" }; split.Items.Add(child);
            var group = new RibbonGroup { Text = "Commands", CanResize = false }; group.Items.AddRange(field, split); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var reference = new RibbonQuickAccessDropDownReference { Source = split, KeyTip = "KP", DropDownKeyTip = "KM" }; ribbon.QuickAccessItems.Add(reference);
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            var editor = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == field.Value); nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 5);
            var original = ribbon.Theme;
            var settings = new RibbonCommandAppearanceSettings { CornerRadius = 0, Inset = 0, BorderWidth = 2, BorderColor = Color.DarkBlue,
                SplitSeparatorWidth = 3, SplitSeparatorInset = 5, SplitSeparatorColor = Color.Magenta, SplitCompanionBackground = new(Color.LightSkyBlue) };
            int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; Application.DoEvents();
                using (var baseline = Print(ribbon)) { }
                var bounds = painter.SourceBounds;
                ribbon.Theme = original with { CommandAppearance = settings }; painter.Check = true; painter.SourcePaints = painter.QatPaints = 0;
                using var image = Print(ribbon); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}.png"));
                Require(painter.Failure is null && painter.SourcePaints > 0 && painter.QatPaints > 0, "Native split state check failed: " + painter.Failure);
                Require(bounds == painter.SourceBounds, "Command decoration changed native action geometry."); painter.Check = false;
                Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == field.Value && editor.SelectionStart == 2 && editor.SelectionLength == 5,
                    "Command theme changed native editor identity, value or selection."); states++;
            }
            int clicks = 0, openings = 0; split.Click += (_, _) => clicks++; split.DropDownOpening += (_, _) => openings++;
            bool old = split.Checked; split.PerformClick(); Require(clicks == 1 && split.Checked != old && openings == 0, "Primary action did not remain separate from opening.");
            split.ShowDropDown(); Require(split.IsDropDownOpen && clicks == 1 && openings == 1 && split.Items.Contains(child), "Dropdown did not retain its source or separate action."); split.CloseDropDown();
            // Exercise the real KeyTip route through WinForms' public message
            // preprocessing API, without global input or runtime internals.
            foreach (var key in new[] { Keys.F10, Keys.K, Keys.M })
            {
                var input = Message.Create(ribbon.Handle, 0x100, (nint)(int)key, 0);
                Require(Application.FilterMessage(ref input) || ribbon.PreProcessMessage(ref input), "Projected KeyTip was not handled: " + key);
            }
            Require(reference.IsDropDownOpen && clicks == 1 && openings == 2 && ReferenceEquals(reference.Source, split), $"Projected dropdown state: open={reference.IsDropDownOpen}, clicks={clicks}, openings={openings}, source={ReferenceEquals(reference.Source, split)}.");
            ribbon.Theme = ribbon.Theme with { CommandAppearance = settings with { Inset = null } }; Require(ribbon.Theme.CommandAppearance!.SplitSeparatorWidth == 3, "Member reset cleared another command setting.");
            ribbon.Theme = original; Require(ribbon.Theme.CommandAppearance is null && editor.Handle == handle && ReferenceEquals(reference.Source, split), "Group reset replaced editor or source.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual root/QAT split callbacks, border/seam settings, unchanged action geometry, independent primary/menu actions, projected source, reset and native editor state."); form.Close();
        }
        using (var demo = new DesignerForm()) demo.VerifyCommandAppearanceDemo();
        evidence.Add("PASS demo: Theme effects 3 created once; previous tabs retain seven/five groups; preview commands, public effect buttons and reset checked.");
        evidence.Add("Offscreen package/print checks; actual VS, visible frame pacing, physical input, IME and mixed-monitor review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static Bitmap Print(RibbonControl ribbon) { var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); return image; }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Probe(string output) : RibbonOffice2024Renderer
    {
        internal bool Check;
        internal int SourcePaints, QatPaints;
        internal Rectangle SourceBounds;
        internal Exception? Failure;
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
        {
            if (item.Text == "Split" && !menu)
            {
                SourceBounds = Rectangle.Union(primary.Bounds, dropDown.Bounds);
                if (Check) Safely(() => { Verify(primary, dropDown, (a, b) => base.DrawSplitItem(a, b, item, size, false), "source"); SourcePaints++; });
            }
            base.DrawSplitItem(primary, dropDown, item, size, menu);
        }
        public override void DrawQuickAccessSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonQuickAccessDropDownReference entry, bool menu)
        {
            if (Check && !menu) Safely(() => { Verify(primary, dropDown, (a, b) => base.DrawQuickAccessSplitItem(a, b, entry, false), "qat"); QatPaints++; });
            base.DrawQuickAccessSplitItem(primary, dropDown, entry, menu);
        }
        private void Verify(RibbonPaintContext primary, RibbonPaintContext dropDown, Action<RibbonPaintContext, RibbonPaintContext> draw, string kind)
        {
            var whole = Rectangle.Union(primary.Bounds, dropDown.Bounds);
            using var image = new Bitmap(whole.Width, whole.Height); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
            Rectangle Local(Rectangle bounds) => new(bounds.X - whole.X, bounds.Y - whole.Y, bounds.Width, bounds.Height);
            draw(primary with { Graphics = graphics, Bounds = Local(primary.Bounds), Focused = false }, dropDown with { Graphics = graphics, Bounds = Local(dropDown.Bounds), Focused = false });
            int inset = (int)Math.Round(5 * primary.DeviceDpi / 96d); int seam = dropDown.Bounds.Left - whole.Left;
            Require(image.GetPixel(0, image.Height / 2).ToArgb() == Color.DarkBlue.ToArgb(), "Authored outline was not consumed.");
            Require(image.GetPixel(seam, inset + 1).ToArgb() == Color.Magenta.ToArgb(), "Authored separator was not consumed.");
            if (kind == "source" && SourcePaints == 0 || kind == "qat" && QatPaints == 0) image.Save(output + "-" + kind + ".png");
        }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
