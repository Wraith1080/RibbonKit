using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class EditorAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1300, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None };
            var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained editor", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var readOnly = new RibbonTextBoxItem { Text = "Read only", Value = "Read-only value", ReadOnly = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var disabled = new RibbonTextBoxItem { Text = "Disabled", Value = "Unavailable", Enabled = false, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var choice = new RibbonComboBoxItem { Text = "Choice", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; choice.Items.AddRange("Alpha", "Beta", "Gamma"); choice.SelectedIndex = 0;
            var editable = new RibbonComboBoxItem { Text = "Editable", IsEditable = true, Value = "Free input", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; editable.Items.AddRange("One", "Two");
            var group = new RibbonGroup { Text = "Editors", CanResize = false }; group.Items.AddRange(text, readOnly, disabled, choice, editable);
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon); form.Ribbon = ribbon;
            form.Show(); Application.DoEvents();
            var input = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == text.Value);
            var locked = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == readOnly.Value);
            var list = Descendants(ribbon).OfType<ComboBox>().Single(control => control.Text == choice.Value);
            var free = Descendants(ribbon).OfType<ComboBox>().Single(control => control.Text == editable.Value);
            var controls = new Control[] { input, locked, list, free }; var handles = controls.Select(control => control.Handle).ToArray(); var parents = controls.Select(control => control.Parent).ToArray();
            var original = ribbon.Theme;
            var settings = new RibbonEditorAppearanceSettings { CornerRadius = 8, BorderWidth = 2, Inset = 3, ActiveBorderColor = Color.DarkOrange, ChoiceCornerRadius = 8, ChoiceInset = 3, ChoiceTextHorizontalPadding = 16 };
            int states = 0, focusedStates = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original; Application.DoEvents();
                text.PerformClick(); Application.DoEvents(); input.Select(2, 5); free.Select(1, 3);
                using (var beforeImage = Print(ribbon)) { }
                bool focused = input.ContainsFocus;
                if (focused) focusedStates++;
                var bounds = controls.Select(control => control.Bounds).ToArray(); var fields = painter.Fields.ToDictionary(pair => pair.Key, pair => pair.Value);
                ribbon.Theme = original with { EditorAppearance = settings }; painter.Check = true; painter.FieldPaints = 0;
                using var after = Print(ribbon); after.Save(Path.Combine(output, $"{scope}-{mode}-{density}.png")); painter.Check = false;
                Require(painter.Failure is null && painter.FieldPaints > 0, "Actual field paint failed: " + painter.Failure);
                Require(fields.All(pair => painter.Fields.TryGetValue(pair.Key, out var current) && current == pair.Value), "Field paint changed measured slots.");
                for (int index = 0; index < controls.Length; index++) Require(controls[index].Handle == handles[index] && ReferenceEquals(controls[index].Parent, parents[index]) && controls[index].Bounds == bounds[index], "Decoration replaced or moved a native input.");
                Require(input.ContainsFocus == focused && input.SelectionStart == 2 && input.SelectionLength == 5 && input.Text == text.Value && locked.ReadOnly && locked.Text == readOnly.Value, $"TextBox focus/value/selection/readonly state changed ({mode}/{density}): focus={focused}->{input.ContainsFocus}, selection={input.SelectionStart}/{input.SelectionLength}, text={input.Text}/{text.Value}, readonly={locked.ReadOnly}, locked={locked.Text}/{readOnly.Value}.");
                Require(free.Text == "Free input" && free.SelectionStart == 1 && free.SelectionLength == 3 && list.SelectedIndex == 0, "ComboBox value/selection changed."); states++;
            }
            painter.Check = true; painter.ChoicePaints = 0;
            choice.ShowDropDown(); Application.DoEvents();
            Require(choice.IsDropDownOpen && painter.Failure is null && painter.ChoicePaints > 0, "Native choice-list callback failed: " + painter.Failure); painter.Check = false; choice.CloseDropDown();
            Require(list.Handle == handles[2] && choice.Value == "Alpha", "Choice-list paint changed its native owner/value.");
            int validations = 0, commits = 0;
            text.ValueValidating += (_, e) => { validations++; e.Cancel = text.Value == "blocked"; }; text.ValueCommitted += (_, _) => commits++;
            text.PerformClick(); bool validationFocus = input.ContainsFocus; input.Text = "blocked"; Key(input, Keys.Enter);
            Require(validations > 0 && commits == 0 && input.ContainsFocus == validationFocus, $"Validation rejection changed native input state: validations={validations}, commits={commits}, focus={validationFocus}->{input.ContainsFocus}.");
            input.Text = "Accepted D1c"; Key(input, Keys.Enter); Require(text.Value == "Accepted D1c" && commits == 1, "Editor appearance changed commit routing.");
            ribbon.Theme = ribbon.Theme with { EditorAppearance = settings with { CornerRadius = null } }; Require(ribbon.Theme.EditorAppearance!.ChoiceInset == 3, "Member reset cleared a choice setting.");
            ribbon.Theme = original; Require(ribbon.Theme.EditorAppearance is null && input.Handle == handles[0], "Group reset replaced input.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states (foreground-focused samples={focusedStates}); actual TextBox/ComboBox/choice callbacks, frame/padding settings, unchanged slots and HWND/parent/bounds, retained text/focus-state/selection/readonly state, choice owner, validation/commit and reset."); form.Close();
        }
        evidence.Add("Offscreen public-package checks. Actual VS, physical input/IME, visible frame pacing and mixed-monitor acceptance remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Key(Control control, Keys key) { var message = Message.Create(control.Handle, 0x100, (nint)(int)key, 0); Require(control.PreProcessMessage(ref message), "Native editor key was not handled: " + key); }
    private static Bitmap Print(RibbonControl ribbon) { var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); return image; }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check;
        internal int FieldPaints, ChoicePaints;
        internal Exception? Failure;
        internal readonly Dictionary<string, Rectangle> Fields = [];
        private bool _choiceText;
        private Rectangle _textBounds;
        public override void DrawTextBox(RibbonPaintContext c, RibbonTextBoxItem item, Rectangle field)
        {
            Fields[item.Text] = field;
            if (Check) Safely(() => VerifyField(c, field, item.Enabled, local => base.DrawTextBox(local, item, new(4, 4, field.Width, field.Height))));
            base.DrawTextBox(c, item, field);
        }
        public override void DrawComboBox(RibbonPaintContext c, RibbonComboBoxItem item, Rectangle field)
        {
            Fields[item.Text] = field;
            if (Check) Safely(() => VerifyField(c, field, item.Enabled, local => base.DrawComboBox(local, item, new(4, 4, field.Width, field.Height))));
            base.DrawComboBox(c, item, field);
        }
        private void VerifyField(RibbonPaintContext c, Rectangle field, bool enabled, Action<RibbonPaintContext> draw)
        {
            if (field.Width <= 0 || field.Height <= 0) return;
            using var image = new Bitmap(field.Width + 8, field.Height + 8); using var g = Graphics.FromImage(image); g.Clear(Color.Transparent);
            var content = c.Bounds; content.Offset(4 - field.Left, 4 - field.Top);
            draw(c with { Graphics = g, Bounds = content, Hot = false, Pressed = false, Focused = true });
            int S(int n) => (int)Math.Round(n * c.DeviceDpi / 96d);
            var expected = enabled ? Color.DarkOrange : c.Theme.EditorBorder.IsEmpty ? c.Theme.Border : c.Theme.EditorBorder;
            Require(image.GetPixel(4 + S(3), 4 + field.Height / 2).ToArgb() == expected.ToArgb(), "Field border setting was bypassed.");
            Require(image.GetPixel(4 + S(3), 4 + S(3)).A == 0, "Field radius/inset was bypassed."); FieldPaints++;
        }
        public override void DrawComboBoxItem(RibbonPaintContext c, RibbonComboBoxItem item, int index, bool selected)
        {
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(c.Bounds.Width + 8, c.Bounds.Height + 8); using var g = Graphics.FromImage(image);
                var bounds = new Rectangle(4, 4, c.Bounds.Width, c.Bounds.Height);
                _choiceText = true;
                try { base.DrawComboBoxItem(c with { Graphics = g, Bounds = bounds, Hot = false, Pressed = false }, item, index, true); }
                finally { _choiceText = false; }
                int inset = Math.Min((int)Math.Round(16 * c.DeviceDpi / 96d), bounds.Width / 2);
                Require(_textBounds == Rectangle.Inflate(bounds, -inset, 0), "Choice text padding was bypassed."); ChoicePaints++;
            });
            base.DrawComboBoxItem(c, item, index, selected);
        }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { if (_choiceText) _textBounds = c.Bounds; base.DrawText(c, text, color, flags); }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
