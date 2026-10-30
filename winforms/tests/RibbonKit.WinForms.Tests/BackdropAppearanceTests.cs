using System.Drawing.Imaging;
using Xunit;

namespace RibbonKit.WinForms.Tests;

// Pixel checks on the actual premultiplied paint path, without desktop input.
public sealed class BackdropAppearanceTests
{
    [Theory]
    [InlineData("qat", false)] [InlineData("qat", true)]
    [InlineData("split", false)] [InlineData("split", true)]
    [InlineData("overflow", false)] [InlineData("overflow", true)]
    [InlineData("minimize", false)] [InlineData("minimize", true)]
    [InlineData("checked", false)]
    public void Header_state_tints_keep_ink_opaque_and_leave_solid_fills_authored(string kind, bool pressed) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        using RibbonItem item = kind == "split" ? new RibbonSplitButtonItem { Icon = new SolidIcon() }
            : kind == "checked" ? new RibbonToggleItem { Checked = true, Icon = new SolidIcon() } : new RibbonButtonItem { Icon = new SolidIcon() };
        var group = new RibbonGroup(); group.Items.Add(item); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        RibbonElement entry = kind == "split" ? new RibbonQuickAccessDropDownReference { Source = (RibbonSplitButtonItem)item } : new RibbonQuickAccessReference { Source = (RibbonButtonItem)item };
        ribbon.QuickAccessItems.Add(entry);
        foreach (bool office in new[] { false, true })
        foreach (bool material in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var buffer = new RibbonBackdropBuffer(); using var image = new Bitmap(S(60), S(44)); using var graphics = Graphics.FromImage(image);
            var theme = ribbon.Theme with { Background = material ? Color.Transparent : Color.White };
            buffer.Paint(graphics, image.Size, image.Height, Color.Transparent, canvas =>
            {
                var context = new RibbonPaintContext(canvas, new(Point.Empty, image.Size), theme, ribbon.Font, dpi, Hot: !pressed && kind != "checked", Pressed: pressed) { CompositedText = material };
                if (kind == "overflow") renderer.DrawQuickAccessOverflow(context);
                else if (kind == "minimize") renderer.DrawMinimizeButton(context, false);
                else if (kind == "split") renderer.DrawQuickAccessSplitItem(context with { Bounds = new(0, 0, S(40), S(44)) },
                    context with { Bounds = new(S(40), 0, S(20), S(44)), Hot = false, Pressed = false }, (RibbonQuickAccessDropDownReference)entry, false);
                else renderer.DrawQuickAccessItem(context, entry, false);
            });
            int alpha = material ? pressed || kind == "checked" ? 24 : 14 : 255;
            Assert.InRange(buffer.Image.GetPixel(S(18), S(10)).A, alpha - 1, Math.Min(255, alpha + 1));
            if (kind == "split" && material) Assert.Equal(0, buffer.Image.GetPixel(S(55), S(10)).A);
            int ink = 0;
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            { var pixel = buffer.Image.GetPixel(x, y); if (pixel.A == 255 && pixel.R < 80) ink++; }
            if (kind == "minimize")
            {
                // A thin antialiased chevron need not contain alpha-255 pixels.
                // Its coverage must survive unchanged by the new state fill.
                using var glyph = new RibbonBackdropBuffer();
                glyph.Paint(graphics, image.Size, image.Height, Color.Transparent, canvas => renderer.DrawMinimizeButton(
                    new(canvas, new(Point.Empty, image.Size), theme with { Background = Color.Transparent }, ribbon.Font, dpi) { CompositedText = material }, false));
                int coverage = 0;
                for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                    if (glyph.Image.GetPixel(x, y).A > 128) { Assert.True(buffer.Image.GetPixel(x, y).A >= glyph.Image.GetPixel(x, y).A); coverage++; }
                Assert.True(coverage > 3);
            }
            else Assert.True(ink > 3, $"{kind}/{pressed}/{material}/{dpi}: artwork became translucent.");
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void File_uses_the_same_office_hover_indicator_as_a_tab_in_both_material_modes(bool material) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var file = new RibbonDropDownItem { Text = "File" }; using var tab = new RibbonTab { Text = "File" };
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int state in new[] { 0, 1, 2, 3 })
        {
            var size = new Size(100 * dpi / 96, 40 * dpi / 96);
            using var actual = Render(fileButton: true); using var expected = Render(fileButton: false);
            for (int y = 0; y < size.Height; y++) for (int x = 0; x < size.Width; x++)
                Assert.Equal(expected.GetPixel(x, y), actual.GetPixel(x, y));
            Bitmap Render(bool fileButton)
            {
                var image = new Bitmap(size.Width, size.Height); using var graphics = Graphics.FromImage(image); using var buffer = new RibbonBackdropBuffer();
                buffer.Paint(graphics, size, size.Height, Color.Transparent, canvas =>
                {
                    var context = new RibbonPaintContext(canvas, new(Point.Empty, size), RibbonThemes.Office2024Light, SystemFonts.MessageBoxFont!, dpi,
                        Hot: state == 0, Pressed: state == 1, Focused: state == 2) { CompositedText = material };
                    file.Enabled = tab.Enabled = state != 3;
                    if (fileButton) renderer.DrawFileButton(context, file); else renderer.DrawTab(context, tab, false);
                });
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy; graphics.DrawImageUnscaled(buffer.Image, Point.Empty);
                return image;
            }
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Only_unselected_material_indicators_lower_their_coverage(bool material) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var tab = new RibbonTab { Text = "Home" };
        foreach (bool selected in new[] { false, true })
        {
            using var buffer = new RibbonBackdropBuffer(); using var image = new Bitmap(100, 8); using var graphics = Graphics.FromImage(image);
            buffer.Paint(graphics, image.Size, image.Height, Color.Transparent, canvas => renderer.DrawTabIndicator(
                new(canvas, new(0, 0, 100, 8), RibbonThemes.Office2024Light, SystemFonts.MessageBoxFont!, 96) { CompositedText = material }, tab, selected));
            int alpha = material && !selected ? 115 : 255;
            Assert.InRange(buffer.Image.GetPixel(50, 4).A, alpha - 1, Math.Min(255, alpha + 1));
        }
    });

    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void Backstage_selected_entry_and_back_button_share_translucent_state_fills(bool back, bool pressed) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var item = new RibbonButtonItem { Text = "Info" };
        foreach (bool material in new[] { false, true })
        {
            using var buffer = new RibbonBackdropBuffer(); using var image = new Bitmap(250, 80); using var graphics = Graphics.FromImage(image);
            var theme = RibbonThemes.Office2024Light with { BackstageNavigationBackground = material ? Color.Transparent : Color.White };
            buffer.Paint(graphics, image.Size, image.Height, Color.Transparent, canvas => renderer.DrawBackstageEntry(
                new(canvas, new(10, 10, 230, 52), theme, SystemFonts.MessageBoxFont!, 96, Hot: true, Pressed: pressed) { CompositedText = material }, back ? null : item, !back));
            int alpha = material ? pressed ? 40 : 24 : 255;
            Assert.InRange(buffer.Image.GetPixel(back ? 135 : 225, 36).A, alpha - 1, Math.Min(255, alpha + 1));
        }
    });

    [Fact]
    public void Authored_backdrop_fills_keep_their_alpha_multiplier_and_indicator_opacity_is_bounded() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        foreach (float value in new[] { -.1f, 1.1f, float.NaN, float.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = ribbon.Theme with { BackdropTabHoverOpacity = value });
        var theme = ribbon.Theme with { BackdropHover = new RibbonFill(Color.FromArgb(80, Color.Red)), QuickAccessBackdropOpacity = .5f };
        using var renderer = new RibbonRenderer(); using var buffer = new RibbonBackdropBuffer(); using var image = new Bitmap(60, 44); using var graphics = Graphics.FromImage(image);
        buffer.Paint(graphics, image.Size, image.Height, Color.Transparent, canvas => renderer.DrawQuickAccessOverflow(
            new(canvas, new(Point.Empty, image.Size), theme, ribbon.Font, 96, Hot: true) { CompositedText = true }));
        Assert.InRange(buffer.Image.GetPixel(8, 8).A, 39, 41);
    });
    private sealed class SolidIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(context.Foreground); graphics.FillRectangle(brush, bounds); }
    }
}
