using RibbonKit.WinForms;
using System.ComponentModel;

namespace RibbonKitDesignerConsumer;

// Public-only package API/paint verification. Runtime container/scroll geometry
// is covered by the STA tests; this check opens no windows or native popups.
internal static class MenuContainerGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using (var field = new RibbonTextBoxItem())
        using (var combo = new RibbonComboBoxItem())
        foreach (var model in new RibbonItem[] { field, combo })
        {
            var property = TypeDescriptor.GetProperties(model)["FillMenuWidth"] ?? throw new InvalidOperationException("Missing fill property in package.");
            Require(property.Category == "Layout" && !property.ShouldSerializeValue(model) && Equals(property.GetValue(model), false), "Fill metadata/default");
            property.SetValue(model, true); Require(property.ShouldSerializeValue(model) && Equals(property.GetValue(model), true), "Fill assignment");
            property.ResetValue(model); Require(!property.ShouldSerializeValue(model) && Equals(property.GetValue(model), false), "Fill reset");
        }
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var ribbon = new RibbonControl { Density = density };
            var original = ribbon.Theme;
            var custom = original with { MenuMinimumWidth = 380, MenuDescriptionMinimumWidth = 520, MenuHeaderHorizontalPadding = 24,
                MenuHeaderVerticalPadding = 16, MenuHeaderBottomGap = 12, MenuOverflowScrollButtonHeight = 44,
                MenuSubmenuArrowWidth = 52, MenuSplitArrowWidth = 64, MenuChooserArrowWidth = 44, MenuSeparatorHorizontalPadding = 24 };
            ribbon.Theme = custom;
            int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            int width = S(520), headerHeight = ribbon.Font.Height + 2 * S(16), rowHeight = S(density == RibbonDensity.Touch ? 44 : 32), band = S(44);
            using var bitmap = new Bitmap(width, headerHeight + 5 * rowHeight + 2 * band + S(36)); using var graphics = Graphics.FromImage(bitmap);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, bitmap.Size), custom, ribbon.Font, ribbon.DeviceDpi)
            { Density = density };
            var header = new Rectangle(S(4), S(4), width - S(8), headerHeight);
            painter.DrawSurface(context, true); painter.DrawPopupFrame(context);
            painter.DrawMenuHeader(context with { Bounds = header }, "Menu container geometry");
            Require(record.Text.Left == header.Left + S(24) && record.Text.Width == header.Width - 2 * S(24), "Public heading override");
            int y = header.Bottom + S(12);
            using var command = new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open") };
            using var option = new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true };
            painter.DrawItem(context with { Bounds = new(S(4), y, width - S(8), rowHeight) }, command, RibbonItemSize.Medium, true); y += rowHeight;
            painter.DrawItem(context with { Bounds = new(S(4), y, width - S(8), rowHeight) }, option, RibbonItemSize.Medium, true); y += rowHeight;
            var tab = new RibbonTab(); var group = new RibbonGroup(); var submenu = new RibbonDropDownItem { Text = "Export" };
            var split = new RibbonSplitButtonItem { Text = "Paste" }; submenu.Items.Add(new RibbonButtonItem { Text = "PDF" }); split.Items.Add(new RibbonButtonItem { Text = "Text only" });
            group.Items.AddRange(submenu, split); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            using var reference = new RibbonQuickAccessDropDownReference { Source = submenu };
            var arrowRow = new Rectangle(S(4), y, width - S(8), rowHeight);
            painter.DrawItem(context with { Bounds = arrowRow }, submenu, RibbonItemSize.Medium, true); Require(record.Arrow.Width == S(64), "Public submenu uses the split visual column");
            int arrowLeft = record.Arrow.Left, arrowRight = record.Arrow.Right; y += rowHeight;
            arrowRow.Y = y; painter.DrawQuickAccessItem(context with { Bounds = arrowRow }, reference, true);
            Require(record.Arrow.Left == arrowLeft && record.Arrow.Right == arrowRight, "Public chooser shares the submenu column"); y += rowHeight;
            arrowRow.Y = y;
            var primary = arrowRow with { Width = arrowRow.Width - S(64) }; var secondary = new Rectangle(primary.Right, y, S(64), rowHeight);
            painter.DrawSplitItem(context with { Bounds = primary }, context with { Bounds = secondary }, split, RibbonItemSize.Medium, true);
            Require(record.Arrow == secondary && secondary.Left == arrowLeft && secondary.Right == arrowRight, "Public split shares the visual column and paints its measured region"); y += rowHeight;
            using var separator = new RibbonMenuSeparatorItem(); painter.DrawMenuSeparator(context with { Bounds = new(S(4), y, width - S(8), S(9)) }, separator); y += S(9);
            painter.DrawOverflowScrollButton(context with { Bounds = new(S(4), y, width - S(8), band) }, true, false); y += band;
            painter.DrawOverflowScrollButton(context with { Bounds = new(S(4), y, width - S(8), band), Hot = true }, false, true);
            bitmap.Save(Path.Combine(output, $"container-{(office ? "office" : "flat")}-{density}-{ribbon.DeviceDpi}dpi.png"));
            ribbon.Theme = custom with { MenuMinimumWidth = null, MenuDescriptionMinimumWidth = null, MenuHeaderHorizontalPadding = null,
                MenuHeaderVerticalPadding = null, MenuHeaderBottomGap = null, MenuOverflowScrollButtonHeight = null,
                MenuSubmenuArrowWidth = null, MenuSplitArrowWidth = null, MenuChooserArrowWidth = null, MenuSeparatorHorizontalPadding = null };
            Require(ribbon.Theme == original, "Container/arrow public properties reset");
            painter.DrawMenuHeader(context with { Theme = ribbon.Theme, Bounds = header }, "Heading");
            Require(record.Text.Left == header.Left + S(6), "Default heading inset restored");
            try { ribbon.Theme = custom with { MenuMinimumWidth = 0 }; throw new InvalidOperationException("Invalid width accepted"); }
            catch (ArgumentOutOfRangeException) { Require(ribbon.Theme == original, "Rejected theme is atomic"); }
        }
        using (var form = new DesignerForm()) form.VerifyContainerDemoContract();
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: public-only package API/paint; fill metadata/default/reset, container/arrow theme roles and reset, submenu/chooser/split paint slots, both renderers and Compact/Touch; permanent demo fill/container/arrow actions and temporary overflow references preserve authored QAT sources. No shown windows, global input or Showcase helpers. Runtime hit/keyboard/UIA/retention/reveal and actual VS/material/hardware acceptance remain separate checks.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Record { internal Rectangle Text, Arrow; }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawSubmenuArrow(c, color); }
    }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text = c.Bounds; base.DrawText(c, text, color, flags); }
        public override void DrawSubmenuArrow(RibbonPaintContext c, Color color) { record.Arrow = c.Bounds; base.DrawSubmenuArrow(c, color); }
    }
}
