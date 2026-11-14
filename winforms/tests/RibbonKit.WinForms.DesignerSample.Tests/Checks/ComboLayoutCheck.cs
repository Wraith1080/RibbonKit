using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Package-only public API check. This Form is separate from the user's authored
// DesignerForm and uses the library's retained native field and choice popup.
internal static class ComboLayoutCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var trace = new StreamWriter(Path.Combine(output, "native-state.log")) { AutoFlush = true };
        using var painter = new Recorder(); using var form = new QuietForm
        { ClientSize = new(850, 350), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000) };
        var ribbon = new RibbonControl { Renderer = painter, QuickAccessVisible = false, AnimationLevel = RibbonAnimationLevel.None, BindingContext = new BindingContext() }; form.Controls.Add(ribbon);
        var tab = new RibbonTab { Id = "layout.home", Text = "ComboBox layout" }; var group = new RibbonGroup { Id = "layout.font", Text = "Font", ReductionMode = RibbonGroupReductionMode.Resize };
        var combo = new RibbonComboBoxItem { Id = "layout.family", Text = "Font family", ShowLabel = false, InputWidth = 280, IsEditable = true,
            SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Medium, RibbonItemSize.Small, RibbonItemSize.Small) };
        combo.Items.AddRange("Segoe UI", "Arial", "Blocked"); combo.SelectedIndex = 0; group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        combo.ValueValidating += (_, args) => args.Cancel = combo.Value == "Blocked";
        int committed = 0; combo.ValueCommitted += (_, _) => committed++;
        form.Show(); Application.DoEvents();
        var native = Descendants(ribbon).OfType<ComboBox>().Single();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; form.ClientSize = new(850, 350); Application.DoEvents();
            // Density can change native font/row metrics. The regression scope
            // is the subsequent field -> button -> field round trip.
            var handle = native.Handle; trace.WriteLine($"{density}: field handle={handle}, value={combo.Value}, native={native.Text}");
            Capture("field-hidden-label", density); Check(native.Visible && !painter.Labels.Contains("Font family"), "Hidden field label");
            combo.ShowLabel = true; Capture("field-visible-label", density); Check(painter.Labels.Contains("Font family"), "Visible field label"); combo.ShowLabel = false;
            form.ClientSize = new(180, 350); Application.DoEvents(); Capture("compact", density);
            Check(!native.Visible && painter.Compact == combo, "Small reduction becomes a compact button");
            combo.ShowDropDown(); Check(combo.IsDropDownOpen, "Compact chooser opens"); Key(Keys.Home); Key(Keys.Down); Key(Keys.Escape);
            Check(combo.Value == "Segoe UI" && !combo.IsDropDownOpen, "Escape restores the previous value");
            combo.ShowDropDown(); Key(Keys.Down); Key(Keys.Enter); Check(combo.Value == "Arial" && committed == 1, "Accepted selection commits once");
            combo.ShowDropDown(); Key(Keys.End); Key(Keys.Enter); Check(combo.IsDropDownOpen && committed == 1, "Validation keeps a rejected choice open"); Key(Keys.Escape);
            combo.ShowDropDown(); Key(Keys.Home); form.ClientSize = new(850, 350); Application.DoEvents();
            trace.WriteLine($"{density}: restored handle={native.Handle}, open={combo.IsDropDownOpen}, value={combo.Value}, native={native.Text}");
            Check(!combo.IsDropDownOpen && combo.Value == "Arial" && native.Handle == handle && native.Text == "Arial",
                $"Restored field retains handle and accepted value: density={density}, open={combo.IsDropDownOpen}, value={combo.Value}, native={native.Text}, handle={native.Handle}, original={handle}");
            combo.SelectedIndex = 0; committed = 0;
        }
        ribbon.CommandCatalog.CaptureDefaults(); using var session = ribbon.CommandCatalog.BeginCustomization("sample.combo.layout", RibbonCustomizationTarget.Ribbon);
        session.Draft.Ribbon!.Tabs.Add(new RibbonCustomizationTab { Label = "My commands", Groups = [new RibbonCustomizationGroup { Label = "Formatting",
            Commands = [new RibbonCustomizationCommand { CommandId = combo.Id, Size = RibbonItemSize.Small }] }] });
        Check(session.Apply(), "Custom group applies"); var copy = (RibbonComboBoxItem)ribbon.Tabs.Last().Groups.Single().Items.Single();
        Check(copy.Text == "Font family" && !copy.ShowLabel, "Custom copy retains its name");
        ribbon.SelectedTab = ribbon.Tabs.Last(); Application.DoEvents(); Capture("custom-copy", ribbon.Density);
        copy.ShowDropDown(); Key(Keys.Down); Key(Keys.Enter); Check(combo.Value == "Arial" && copy.Value == "Arial", "Copy commits to its source");
        File.WriteAllText(Path.Combine(output, "result.log"), $"PASS: package-only public API at {ribbon.DeviceDpi} DPI; hidden/visible label, actual Compact/Touch reduction, choice preview/Escape/validation/commit, native field handle restoration, named custom-group copy and shared selection. Actual VS review and mixed-monitor checks remain separate.");

        void Key(Keys key)
        {
            var message = Message.Create(ribbon.Handle, 0x100, (nint)(int)key, 0);
            Check(Application.FilterMessage(ref message), "Chooser handled " + key);
            Application.DoEvents();
        }
        void Capture(string name, RibbonDensity density)
        {
            painter.Labels.Clear(); painter.Compact = null;
            using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle);
            pixels.Save(Path.Combine(output, $"combo-{name}-{density}-{ribbon.DeviceDpi}dpi.png"));
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private sealed class QuietForm : Form { protected override bool ShowWithoutActivation => true; }
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal List<string> Labels { get; } = [];
        internal RibbonComboBoxItem? Compact;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Labels.Add(text); base.DrawText(context, text, color, flags); }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
        { if (item is RibbonComboBoxItem combo && size == RibbonItemSize.Small && !menu) Compact = combo; base.DrawItem(context, item, size, menu); }
    }
}
