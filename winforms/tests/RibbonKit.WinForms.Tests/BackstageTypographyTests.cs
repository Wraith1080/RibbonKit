using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageTypographyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Navigation_uses_its_font_minimum_for_paint_and_measurement_without_changing_native_page_text(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer renderer = office ? new Office(record) : new Neutral(record);
        using var small = new Font("Segoe UI", 9); using var large = new Font("Segoe UI", 16, FontStyle.Bold);
        using var form = new Form { Width = 1200, Height = 700 };
        var ribbon = new RibbonControl { Renderer = renderer, Font = small, FileMenuMode = RibbonFileMenuMode.Backstage };
        using var field = new TextBox { Text = "Retained page text", Font = small };
        var page = new RibbonBackstagePageItem { Text = "Document settings", Content = field }; ribbon.Backstage.Items.Add(page);
        ribbon.FileMenu.Visible = true; form.Controls.Add(ribbon); form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        field.Focus(); field.Select(2, 4); nint handle = field.Handle; var parent = field.Parent;
        Assert.Equal(14, ribbon.Theme.BackstageNavigationFontSize);
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (var source in new[] { small, large })
        foreach (float minimum in new[] { 14f, 22f })
        {
            ribbon.Density = density; ribbon.Font = source;
            ribbon.Theme = ribbon.Theme with { BackstageNavigationFontSize = minimum };
            var surface = ribbon.BackstageSurface!;
            using var bitmap = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(bitmap, surface.ClientRectangle);
            Assert.Equal(Math.Max(source.SizeInPoints, minimum * 72 / 96), record.NavigationPoints);
            Assert.Equal(source.Style, record.Style); Assert.Equal(source.SizeInPoints, record.BackPoints);
            int S(int n) => (int)Math.Round(n * surface.DeviceDpi / 96d);
            Assert.True(record.Bounds.Height >= record.Height + S(16));
            Assert.True(record.Bounds.Right - record.Bounds.Left >= record.TextWidth + S(44 + 8));
            Assert.Same(parent, field.Parent); Assert.Equal(handle, field.Handle); Assert.Same(small, field.Font);
            Assert.Equal("Retained page text", field.Text); Assert.Equal(2, field.SelectionStart); Assert.Equal(4, field.SelectionLength);
            Assert.True(field.Focused); Assert.Same(page, ribbon.Backstage.SelectedPage);
        }
        foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = ribbon.Theme with { BackstageNavigationFontSize = invalid });
    });

    private sealed class Record
    {
        internal float NavigationPoints, BackPoints; internal FontStyle Style; internal int Height, TextWidth; internal Rectangle Bounds;
        internal void Entry(RibbonPaintContext c, RibbonItem? item)
        {
            if (item is null) { BackPoints = c.Font.SizeInPoints; return; }
            NavigationPoints = c.Font.SizeInPoints; Style = c.Font.Style; Height = c.Font.Height; Bounds = c.Bounds;
            TextWidth = TextRenderer.MeasureText(item.Text, c.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
        }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawBackstageEntry(RibbonPaintContext c, RibbonItem? item, bool selected) { record.Entry(c, item); base.DrawBackstageEntry(c, item, selected); }
    }
    private sealed class Neutral(Record record) : RibbonRenderer
    {
        public override void DrawBackstageEntry(RibbonPaintContext c, RibbonItem? item, bool selected) { record.Entry(c, item); base.DrawBackstageEntry(c, item, selected); }
    }
}
