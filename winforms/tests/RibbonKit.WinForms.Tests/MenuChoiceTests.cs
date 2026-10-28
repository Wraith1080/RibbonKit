using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MenuChoiceTests
{
    [Fact]
    public void Header_and_illustrated_choice_metadata_are_opt_in_and_qat_reuses_the_same_noninteractive_heading() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl { Width = 1400 }; var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
        var menu = new RibbonDropDownItem(); group.Items.Add(menu);
        var first = new RibbonRadioButtonItem { Text = "Mouse", MenuDescription = "Standard ribbon and commands.\nOptimized for use with mouse." }; var second = new RibbonRadioButtonItem { Text = "Touch" }; menu.Items.AddRange(first, second);
        Assert.Equal(RibbonMenuPresentation.Standard, menu.MenuPresentation); Assert.Equal("", menu.DropDownHeader);
        Assert.True(TypeDescriptor.GetProperties(menu)[nameof(menu.DropDownHeader)]!.IsLocalizable);
        int headers = 0, presentations = 0; menu.DropDownHeaderChanged += (_, _) => headers++; menu.MenuPresentationChanged += (_, _) => presentations++;
        menu.DropDownHeader = "Optimize spacing between commands"; menu.MenuPresentation = RibbonMenuPresentation.IllustratedChoices;
        menu.DropDownHeader = menu.DropDownHeader; menu.MenuPresentation = menu.MenuPresentation; Assert.Equal(1, headers); Assert.Equal(1, presentations);
        Assert.Throws<ArgumentOutOfRangeException>(() => menu.MenuPresentation = (RibbonMenuPresentation)99);
        var reference = new RibbonQuickAccessDropDownReference { Source = menu }; host.QuickAccessItems.Add(reference);
        using var sourcePopup = new RibbonPopupSurface(host, menu, new(1200, 900)); using var qatPopup = new RibbonPopupSurface(host, reference, new(1200, 900));
        Assert.Equal(2, sourcePopup.Targets.Count); Assert.Equal(sourcePopup.Size, qatPopup.Size);
        Assert.Equal(sourcePopup.Targets.Select(t => t.Bounds), qatPopup.Targets.Select(t => t.Bounds));
        Assert.Equal(new RibbonElement[] { first, second }, sourcePopup.Targets.Select(t => t.Element));
        Assert.True(sourcePopup.Targets[0].Bounds.Top > host.Font.Height + RibbonLayoutEngine.Scale(12, host.DeviceDpi));
        Assert.All(sourcePopup.Targets, t => Assert.True(t.Bounds.Height >= RibbonLayoutEngine.Scale(64, host.DeviceDpi)));
        var individual = new RibbonQuickAccessReference { Source = first }; host.QuickAccessItems.Add(individual); host.QuickAccessMaxWidth = 28;
        using var overflow = new RibbonPopupSurface(host, host.QuickAccessOverflow, new(1200, 900));
        Assert.True(overflow.Targets.Single(t => t.Element == individual).Bounds.Height < RibbonLayoutEngine.Scale(64, host.DeviceDpi));
        using var editor = new RibbonTextBoxItem(); menu.Items.Add(editor); Assert.False(RibbonMenuLayout.IsIllustratedChoice(editor));
        menu.MenuPresentation = RibbonMenuPresentation.Standard; Assert.False(RibbonMenuLayout.IsIllustratedChoice(first));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Illustrated_rows_keep_artwork_bold_titles_descriptions_and_selected_fill_in_shared_slots(bool office) => FoundationTests.Sta(() =>
    {
        using var probe = new Control(); using var host = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); host.Tabs.Add(tab);
        var menu = new RibbonDropDownItem { MenuPresentation = RibbonMenuPresentation.IllustratedChoices }; group.Items.Add(menu);
        var item = new RibbonRadioButtonItem { Text = "Touch", MenuDescription = "More space between commands.\nOptimized for use with touch.", Icon = new Icon() }; menu.Items.Add(item);
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (bool selected in new[] { false, true })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            using var font = new Font("Segoe UI", points * dpi / (float)probe.DeviceDpi); item.Checked = selected;
            var row = new Rectangle(0, 0, S(300), RibbonMenuLayout.RowHeight(item, S(300), font, dpi));
            using var bitmap = new Bitmap(row.Width, row.Height); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(host.Theme.Surface); record.Texts.Clear();
            painter.DrawItem(new(graphics, row, host.Theme, font, dpi), item, RibbonItemSize.Medium, true);
            Assert.Equal(new Size(S(36), S(36)), record.Image.Size); Assert.True(row.Contains(record.Image));
            var title = Assert.Single(record.Texts, t => t.Text == item.Text); var description = Assert.Single(record.Texts, t => t.Text == item.MenuDescription);
            Assert.True(title.Bold); Assert.False(description.Bold); Assert.Equal(title.Bounds.Left, description.Bounds.Left); Assert.True(record.Image.Right < title.Bounds.Left);
            Assert.True(row.Contains(description.Bounds)); Assert.Equal(2 * font.Height, description.Bounds.Height);
            Assert.Equal((selected ? host.Theme.Checked.Start : host.Theme.Surface).ToArgb(), bitmap.GetPixel(row.Right - S(8), row.Bottom - S(8)).ToArgb());
        }
    });
    private sealed class Icon : IRibbonIcon { public void Draw(Graphics g, Rectangle r, RibbonIconContext c) { using var b = new SolidBrush(Color.SteelBlue); g.FillRectangle(b, r); } }
    private sealed class Record { internal Rectangle Image; internal readonly List<(string Text, Rectangle Bounds, bool Bold)> Texts = []; }
    private sealed class Neutral(Record r) : RibbonRenderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem i, RibbonItemSize size) { r.Image = c.Bounds; base.DrawItemImage(c, i, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Texts.Add((text, c.Bounds, c.Font.Bold)); base.DrawText(c, text, color, flags); }
    }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem i, RibbonItemSize size) { r.Image = c.Bounds; base.DrawItemImage(c, i, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Texts.Add((text, c.Bounds, c.Font.Bold)); base.DrawText(c, text, color, flags); }
    }
}
