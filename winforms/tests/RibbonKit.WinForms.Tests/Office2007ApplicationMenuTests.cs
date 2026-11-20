using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007ApplicationMenuTests
{
    private static Color C(int rgb) => Color.FromArgb(255, rgb >> 16 & 255, rgb >> 8 & 255, rgb & 255);
    private static void Stops(RibbonFill fill, params (double Offset, int Rgb)[] expected)
    {
        var stops = new[] { new RibbonGradientStop(0, fill.Start) }.Concat(fill.IntermediateStops?.AsEnumerable() ?? []).Append(new(1, fill.End)).ToArray();
        Assert.Equal(expected.Select(s => s.Offset), stops.Select(s => s.Offset));
        Assert.Equal(expected.Select(s => C(s.Rgb).ToArgb()), stops.Select(s => s.Color.ToArgb()));
    }
    [Fact]
    public void Menu_palette_uses_exact_blue_bands_and_independent_idle_footer_ink()
    {
        var t = RibbonThemes.Office2007Blue; var a = t.ApplicationMenuAppearance!;
        Assert.Equal(C(0x9BAFCA), a.OuterBorderColor); Assert.Equal(C(0xBED3EC), a.SurroundBackground!.Value.Start);
        Stops(a.TopBandBackground!.Value, (0, 0xD7E5F7), (.44, 0xD5E2F3), (.44, 0xC6D7EE), (1, 0xB7CBE6));
        Stops(a.FooterSurfaceBackground!.Value, (0, 0xBDD3EF), (.47, 0xBFD5F1), (.47, 0xB1CAEA), (.96, 0xCFE0F6), (1, 0xC9EEFF));
        Stops(a.FooterBackground!.Value, (0, 0xF4F9FE), (.45, 0xE2EDFB), (.45, 0xD3E4F8), (1, 0xE8F2FD));
        Assert.Equal(C(0xFAFAFA), a.NavigationBackground!.Value.Start); Assert.Equal(C(0xE9EAEE), a.DefaultPaneBackground!.Value.Start);
        Assert.Equal(C(0xFAFAFA), a.ActivePaneBackground!.Value.Start); Assert.Equal(C(0x9F9F9F), a.InnerFrameBorderColor);
        Assert.Equal(C(0x7793B9), a.FooterIdleBorderColor); Assert.True(a.FooterBorderColor.IsEmpty);
        Assert.Equal(C(0xC5C5C5), t.ApplicationMenuContent!.SeparatorColor);
        Assert.Equal(t.Hover, a.OpenBackground); Assert.Equal(82, a.SelectedPrimaryBackground!.Value.Start.A);
        Assert.Equal(a, (t with { Accent = Color.Purple }).ApplicationMenuAppearance);
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Top_and_footer_gradients_use_complete_measured_bands_under_partial_clips(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(300), S(260)); using var g = Graphics.FromImage(image);
            var t = RibbonThemes.Office2007Blue; var a = t.ApplicationMenuAppearance!;
            var nav = new Rectangle(S(6) + 1, S(30) + 1, S(100), S(184) - 2); var pane = new Rectangle(S(120), S(60), S(170), S(150));
            var footer = new Rectangle(S(6), S(220), S(288), S(32)); var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), t, Control.DefaultFont, dpi) { IsApplicationMenu = true, ApplicationMenuDefaultPane = true };
            g.Clear(Color.Magenta); painter.DrawApplicationMenuBackground(c, nav, pane, footer);
            var frame = RibbonRenderer.ApplicationFrame(c.Bounds, nav); int x = image.Width / 2;
            foreach (int y in new[] { S(6), S(12), S(15), S(24) })
                Assert.Equal(RibbonFillColors.At(a.TopBandBackground!.Value, y / (double)(frame.Top - 1)).ToArgb(), image.GetPixel(x, y).ToArgb());
            foreach (int delta in new[] { S(6), S(20), S(28) })
                Assert.Equal(RibbonFillColors.At(a.FooterSurfaceBackground!.Value, delta / (double)(image.Height - frame.Bottom - 1)).ToArgb(), image.GetPixel(x, frame.Bottom + delta).ToArgb());
            Assert.Equal(a.SurroundBackground!.Value.Start.ToArgb(), image.GetPixel(S(3), S(90)).ToArgb());
            Assert.Equal(a.NavigationBackground!.Value.Start.ToArgb(), image.GetPixel(S(20), S(90)).ToArgb());
            Assert.Equal(a.DefaultPaneBackground!.Value.Start.ToArgb(), image.GetPixel(S(240), S(90)).ToArgb());
            using var full = (Bitmap)image.Clone();
            var clip = new Rectangle(S(15), S(10), S(260), S(238)); g.Clear(Color.Magenta); g.SetClip(clip);
            painter.DrawApplicationMenuBackground(c, nav, pane, footer);
            for (int y = 0; y < image.Height; y++) for (int px = 0; px < image.Width; px++)
                Assert.Equal((clip.Contains(px, y) ? full.GetPixel(px, y) : Color.Magenta).ToArgb(), image.GetPixel(px, y).ToArgb());
            g.ResetClip(); painter.DrawApplicationMenuBackground(c with { Theme = t with { ApplicationMenuAppearance = a with { TopBandBackground = null } } }, nav, pane, footer);
            Assert.Equal(a.SurroundBackground.Value.Start.ToArgb(), image.GetPixel(x, S(10)).ToArgb());
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Outer_menu_border_is_scoped_to_root_and_explicit_transparent_and_reset_remain_independent(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(160), S(80)); using var g = Graphics.FromImage(image);
            var t = RibbonThemes.Office2007Blue; var bounds = new Rectangle(S(5), S(5), S(150), S(70));
            var c = new RibbonPaintContext(g, bounds, t, Control.DefaultFont, dpi); int x = bounds.Left + bounds.Width / 2;
            painter.DrawPopupFrame(c); Assert.Equal(t.PopupBorder.ToArgb(), image.GetPixel(x, bounds.Top).ToArgb());
            painter.DrawPopupFrame(c with { IsApplicationMenu = true }); Assert.Equal(C(0x9BAFCA).ToArgb(), image.GetPixel(x, bounds.Top).ToArgb());
            painter.DrawPopupFrame(c with { IsApplicationMenu = true, Theme = t with { ApplicationMenuAppearance = t.ApplicationMenuAppearance! with { OuterBorderColor = Color.Empty } } });
            Assert.Equal(t.PopupBorder.ToArgb(), image.GetPixel(x, bounds.Top).ToArgb());
            g.Clear(Color.Magenta); painter.DrawPopupFrame(c with { IsApplicationMenu = true, Theme = t with { ApplicationMenuAppearance = t.ApplicationMenuAppearance! with { OuterBorderColor = Color.Transparent } } });
            Assert.NotEqual(C(0x9BAFCA).ToArgb(), image.GetPixel(x, bounds.Top).ToArgb());
            foreach (var old in new[] { RibbonThemes.Office2024Light, RibbonThemes.Office2019Light, RibbonThemes.Office2013Light, RibbonThemes.Office2010Blue })
            {
                g.Clear(Color.Magenta); painter.DrawPopupFrame(c with { Theme = old }); using var ordinary = (Bitmap)image.Clone();
                g.Clear(Color.Magenta); painter.DrawPopupFrame(c with { Theme = old, IsApplicationMenu = true });
                for (int y = 0; y < image.Height; y++) for (int px = 0; px < image.Width; px++) Assert.Equal(ordinary.GetPixel(px, y), image.GetPixel(px, y));
            }
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Footer_has_blue_idle_and_disabled_ink_but_gold_active_ink_and_exact_creases(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var painter = new InkOnly(); using var item = new RibbonButtonItem(); using var image = new Bitmap(S(200), S(70)); using var g = Graphics.FromImage(image);
        var t = RibbonThemes.Office2007Blue; var bounds = new Rectangle(S(5), S(5), S(190), S(60));
        var c = new RibbonPaintContext(g, bounds, t, Control.DefaultFont, dpi) { IsApplicationMenu = true, ApplicationMenuPart = RibbonApplicationMenuPart.Footer };
        foreach (int state in new[] { 0, 1, 2, 3 })
        {
            item.Enabled = state != 3; g.Clear(Color.Magenta); painter.DrawItem(c with { Hot = state is 1 or 3, Pressed = state == 2 }, item, RibbonItemSize.Medium, true);
            Assert.Equal(C(state == 1 ? 0xC1A877 : state == 2 ? 0xB6A892 : 0x7793B9).ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + 1).ToArgb());
        }
        item.Enabled = true;
        painter.DrawItem(c with { Hot = true, Theme = t with { ApplicationMenuAppearance = t.ApplicationMenuAppearance! with { FooterBorderColor = Color.Red } } }, item, RibbonItemSize.Medium, true);
        Assert.Equal(Color.Red.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + 1).ToArgb());
    });
    private sealed class InkOnly : RibbonOffice2024Renderer
    { public override void DrawText(RibbonPaintContext c, string text, Color ink, TextFormatFlags flags) { } protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { } }
}
