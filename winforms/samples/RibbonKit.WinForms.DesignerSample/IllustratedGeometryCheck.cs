using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public-package paint/API check; no shown windows, native input or internal layout APIs.
internal static class IllustratedGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            using var ribbon = new RibbonControl { Density = density }; var original = ribbon.Theme;
            var custom = original with { IllustratedMenuMinimumWidth = 540, IllustratedMenuMinimumRowHeight = 88, IllustratedMenuIconSize = 48,
                IllustratedMenuLeadingPadding = 18, IllustratedMenuTrailingPadding = 22, IllustratedMenuIconTextGap = 20, IllustratedMenuDescriptionGap = 8 };
            ribbon.Theme = custom;
            var tab = new RibbonTab(); var group = new RibbonGroup(); var menu = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices };
            var choice = new RibbonToggleItem { Text = "Keep source formatting", MenuDescription = "Keep the original fonts and paragraph spacing.", Checked = true, Icon = RibbonStockIcons.Create("open") };
            menu.Items.Add(choice); group.Items.Add(menu); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            using var pixels = new Bitmap(S(540), S(104)); using var graphics = Graphics.FromImage(pixels);
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            var context = new RibbonPaintContext(graphics, new(Point.Empty, pixels.Size), custom, font, dpi) { Density = density };
            painter.DrawSurface(context, true); painter.DrawPopupFrame(context);
            var row = new Rectangle(S(4), S(8), S(532), S(88)); painter.DrawItem(context with { Bounds = row }, choice, RibbonItemSize.Medium, true);
            Require(record.Image == new Rectangle(row.Left + S(18), row.Top + (row.Height - S(48)) / 2, S(48), S(48)), "Public artwork slot");
            var title = record.Text[choice.Text]; var description = record.Text[choice.MenuDescription];
            Require(title.Left == record.Image.Right + S(20) && title.Right == row.Right - S(22), "Public title slot");
            Require(description.Left == title.Left && description.Top == title.Bottom + S(8) && description.Height == 2 * font.Height, "Public description slot");
            pixels.Save(Path.Combine(output, $"public-illustrated-{office}-{density}-{dpi}.png"));
            ribbon.Theme = custom with { IllustratedMenuMinimumWidth = null, IllustratedMenuMinimumRowHeight = null, IllustratedMenuIconSize = null,
                IllustratedMenuLeadingPadding = null, IllustratedMenuTrailingPadding = null, IllustratedMenuIconTextGap = null, IllustratedMenuDescriptionGap = null };
            Require(ribbon.Theme == original, "Seven-role reset");
            painter.DrawItem(context with { Theme = original, Bounds = row }, choice, RibbonItemSize.Medium, true);
            Require(record.Image.Width == S(36) && record.Image.Left == row.Left + S(8), "Default artwork reset");
            try { ribbon.Theme = custom with { IllustratedMenuIconSize = 0 }; throw new InvalidOperationException("Invalid artwork size accepted"); }
            catch (ArgumentOutOfRangeException) { Require(ribbon.Theme == original, "Atomic rejection"); }
        }
        using (var form = new DesignerForm()) form.VerifyIllustratedDemoContract();
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: public-package illustrated menu API/reset/validation and artwork/title/description paint, both renderers/densities and synthetic 96/120/144/192 DPI. Permanent demo apply/reset, toggle state and QAT source ownership preserve prior editor/menu geometry and File mode. No shown windows, popup, global input, internals or Showcase helpers. Library STA tests separately cover native menu measurement, source/QAT/nested openers, inert preview, scrolling, UIA and retained mixed native editors. Actual VS/input/material/hardware acceptance remains separate.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Record { internal Rectangle Image; internal readonly Dictionary<string, Rectangle> Text = []; }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { record.Image = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { record.Text[text] = c.Bounds; base.DrawText(c, text, color, flags); }
    }
}
