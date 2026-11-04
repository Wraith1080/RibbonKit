using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public-package paint/API verification. Does not show a Form or open a popup.
internal static class EditorGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var ribbon = new RibbonControl { Density = density }; var original = ribbon.Theme;
            var custom = original with { EditorLabelGap = 18, EditorTextInset = 10, EditorMinimumHeight = 54, EditorVerticalPadding = 10, ComboBoxButtonWidth = 48, ComboBoxMinimumChoiceHeight = 60, ComboBoxChoiceVerticalPadding = 14 };
            ribbon.Theme = custom;
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            using var image = new Bitmap(S(480), S(300));
            using var graphics = Graphics.FromImage(image);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, image.Size), custom, font, dpi) { Density = density };
            painter.DrawSurface(context, true); painter.DrawPopupFrame(context);
            using var text = new RibbonTextBoxItem { Text = "Name", Value = "Document" };
            using var combo = new RibbonComboBoxItem { Text = "Font family" }; combo.Items.AddRange("Aptos", "Segoe UI"); combo.SelectedIndex = 0;
            var row = new Rectangle(S(8), S(8), S(460), S(54)); var field = new Rectangle(S(140), row.Top, S(320), row.Height);
            painter.DrawTextBox(context with { Bounds = row }, text, field);
            Require(record.Text.Left == row.Left && record.Text.Right == field.Left - S(18), "TextBox label gap");
            row.Y += S(64); field.Y = row.Top;
            painter.DrawComboBox(context with { Bounds = row, Hot = true }, combo, field);
            Require(record.Text.Right == field.Left - S(18), "ComboBox label gap");
            Require(record.Arrow == new Rectangle(field.Right - S(48), field.Top, S(48), field.Height), "ComboBox painted arrow slot");
            combo.ShowLabel = false; row.Y += S(64); field.Y = row.Top; record.LabelCalls = 0;
            painter.DrawComboBox(context with { Bounds = row }, combo, field); Require(record.LabelCalls == 0, "Hidden label reserves no painted caption");
            int y = field.Bottom + S(8);
            painter.DrawComboBoxItem(context with { Bounds = new(S(8), y, S(460), S(60)) }, combo, 0, true);
            image.Save(Path.Combine(output, $"public-editors-{office}-{density}-{dpi}.png"));
            ribbon.Theme = custom with { EditorLabelGap = null, EditorTextInset = null, EditorMinimumHeight = null, EditorVerticalPadding = null, ComboBoxButtonWidth = null, ComboBoxMinimumChoiceHeight = null, ComboBoxChoiceVerticalPadding = null };
            Require(ribbon.Theme == original, "All seven roles reset independently");
            painter.DrawComboBox(context with { Theme = ribbon.Theme, Bounds = row }, combo, field);
            Require(record.Arrow.Width == S(density == RibbonDensity.Touch ? 32 : 24), "Default density button width");
            try { ribbon.Theme = custom with { ComboBoxButtonWidth = 0 }; throw new InvalidOperationException("Invalid button width accepted"); }
            catch (ArgumentOutOfRangeException) { Require(ribbon.Theme == original, "Rejected theme assignment is atomic"); }
        }
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            using var ribbon = new RibbonControl { Density = density, LayoutMode = mode, Width = 1400, AnimationLevel = RibbonAnimationLevel.None };
            var tab = new RibbonTab(); var group = new RibbonGroup();
            var combo = new RibbonComboBoxItem { Text = "Font", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedSize = RibbonItemSize.Medium };
            group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab); int oldHeight = ribbon.Height; var original = ribbon.Theme;
            ribbon.Theme = original with { EditorMinimumHeight = 54, EditorVerticalPadding = 10 };
            Require(ribbon.Height > oldHeight, "Public Classic/Simplified body grows for inline field height");
            ribbon.Theme = ribbon.Theme with { EditorMinimumHeight = null, EditorVerticalPadding = null };
            Require(ribbon.Height == oldHeight && ribbon.Theme == original, "Public field-height reset restores body dimensions");
        }
        using (var form = new DesignerForm()) form.VerifyContainerDemoContract();
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: public-package editor geometry API/reset/validation and label/arrow/hidden-label paint; both renderers, Compact/Touch, synthetic 96/120/144/192 DPI. Public Classic/Simplified ribbon height grows/resets for taller inline fields in both densities. Permanent demo applies/resets seven roles and preserves prior geometry, fill, File mode and QAT identities. No shown windows, popup, internals or Showcase helpers. Native field text/row centering, clipping, choice heights and retention are covered by separate library STA tests; actual VS/input/material/hardware acceptance remains separate.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Record { internal Rectangle Text, Arrow; internal int LabelCalls; }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; record.LabelCalls++; base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; record.LabelCalls++; base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
}
