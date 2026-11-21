using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2007BackstageAccentKeyTipTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Header_keeps_exact_default_blue_and_rebases_its_complete_crease_without_recoloring_gold(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var theme = RibbonThemes.Office2007Blue; var recipe = theme.IdentityAccent!;
        Assert.Null(theme.BackstageAppearance!.Workspace!.HeaderBackground);
        var blue = RibbonBackstage2007Renderer.HeaderFill(theme);
        Assert.Equal(recipe.FileBackground, blue);
        Assert.Equal(Color.FromArgb(0x6F, 0xB4, 0xE4), blue.Start); Assert.Equal(Color.FromArgb(0x4C, 0x93, 0xCD), blue.End);
        Assert.Equal(new[] { .45, .45 }, blue.IntermediateStops!.Select(s => s.Offset));
        foreach (RibbonRenderer painter in new RibbonRenderer[] { new(), new RibbonOffice2024Renderer() })
        using (painter)
        using (var image = new Bitmap(S(800), S(480)))
        using (var g = Graphics.FromImage(image))
        {
            var bounds = new Rectangle(Point.Empty, image.Size); int header = S(48);
            var context = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            painter.DrawBackstageBackground(context, S(230));
            int body = image.GetPixel(S(400), S(150)).ToArgb();
            foreach (var accent in new[] { theme.Accent, Color.FromArgb(35, 145, 75), Color.Firebrick, Color.Purple })
            {
                var colored = theme with { Accent = accent }; var fill = RibbonBackstage2007Renderer.HeaderFill(colored);
                Assert.Equal(recipe.AdaptFill(recipe.FileBackground, accent, recipe.FileStrength), fill);
                Assert.Equal(blue.IntermediateStops!.Select(s => s.Offset), fill.IntermediateStops!.Select(s => s.Offset));
                Assert.NotEqual(fill.IntermediateStops![0].Color, fill.IntermediateStops![1].Color);
                painter.DrawBackstageBackground(context with { Theme = colored }, S(230));
                foreach (int y in new[] { 0, (int)Math.Ceiling((header - 1) * .45) - 1, (int)Math.Ceiling((header - 1) * .45), header - 1 })
                    Assert.Equal(RibbonFillColors.At(fill, y / (double)(header - 1)).ToArgb(), image.GetPixel(S(400), y).ToArgb());
                Assert.Equal(body, image.GetPixel(S(400), S(150)).ToArgb());
                Assert.Equal(theme.BackstageNavigationSelected, colored.BackstageNavigationSelected);
                Assert.Equal(theme.Hover, colored.Hover); Assert.Equal(theme.Checked, colored.Checked); Assert.Equal(theme.ApplicationButtonAppearance, colored.ApplicationButtonAppearance);
            }
        }
    });

    [Fact]
    public void Header_explicit_fill_and_reset_keep_file_reference_strength_and_independent_caption_authority()
    {
        var theme = RibbonThemes.Office2007Blue with { Accent = Color.SeaGreen };
        var reference = theme.IdentityAccent!.FileBackground;
        Assert.Equal(reference, RibbonBackstage2007Renderer.HeaderFill(theme with { IdentityAccent = theme.IdentityAccent with { FileStrength = 0 } }));
        var file = new RibbonFill(Color.Orange, Color.Brown);
        var authored = theme with { FileButtonAppearance = theme.FileButtonAppearance! with { Background = file } };
        Assert.Equal(file, RibbonBackstage2007Renderer.HeaderFill(authored));
        var header = new RibbonFill(Color.FromArgb(128, Color.Aqua), Color.FromArgb(90, Color.Blue)) { Direction = RibbonGradientDirection.Horizontal };
        authored = authored with { BackstageAppearance = theme.BackstageAppearance! with { Workspace = theme.BackstageAppearance.Workspace! with { HeaderBackground = header } } };
        Assert.Equal(header, RibbonBackstage2007Renderer.HeaderFill(authored));
        var reset = authored with { BackstageAppearance = authored.BackstageAppearance! with { Workspace = authored.BackstageAppearance.Workspace! with { HeaderBackground = null } } };
        Assert.Equal(file, RibbonBackstage2007Renderer.HeaderFill(reset));
        Assert.Equal(theme.TitleBarBackgroundFill, reset.TitleBarBackgroundFill);
        Assert.Equal(theme.OptionsAppearance, reset.OptionsAppearance);
        Assert.Equal(new RibbonFill(Color.Red), RibbonBackstage2007Renderer.HeaderFill(new RibbonTheme { Accent = Color.Red }));
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Orb_badge_is_centered_at_the_visible_foot_and_clamps_padding_to_the_header(int dpi)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var orb = new Rectangle(S(6), -S(18), S(46), S(46)); var header = new Rectangle(0, 0, S(800), S(48));
        var theme = RibbonThemes.Office2007Blue; var metrics = new RibbonTipMetrics(theme);
        var badge = metrics.BackstageFileBadgeBounds(orb, header, new(S(15), S(12)), dpi);
        Assert.Equal(orb.Bottom, badge.Bottom); Assert.InRange(Math.Abs((badge.Left + badge.Right) - (orb.Left + orb.Right)), 0, 1);
        Assert.True(Rectangle.Intersect(orb, header).Contains(badge));
        var wide = new RibbonTipMetrics(theme with { KeyTipHorizontalPadding = 100, KeyTipVerticalPadding = 100 }).BackstageFileBadgeBounds(orb, header, new(S(15), S(12)), dpi);
        Assert.Equal(Rectangle.Intersect(orb, header), wide);
        Assert.True(metrics.BackstageFileBadgeBounds(orb, new(0, S(100), S(800), S(48)), new(15, 12), dpi).IsEmpty);
    }

    [Fact]
    public void Native_orb_keytip_resolves_authored_prefix_collisions_filters_and_validation_without_a_back_button() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.Backstage };
        using var painter = new TipProbe(); ribbon.Renderer = painter;
        ribbon.FileMenu.Visible = true; ribbon.FileMenu.KeyTip = "ZF"; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var body = new Panel(); using var field = new TextBox { Text = "Retained", Bounds = new(10, 10, 200, 30) }; body.Controls.Add(field);
        int commandCalls = 0, closed = 0; bool invalid = false;
        field.Validating += (_, e) => e.Cancel = invalid;
        var other = new RibbonButtonItem { Text = "Other", KeyTip = "Z" }; other.Click += (_, _) => commandCalls++;
        ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Info", KeyTip = "I", Content = body }, other);
        ribbon.Backstage.Closed += (_, _) => closed++;
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!; var handle = field.Handle; var parent = body.Parent;
        Assert.False(surface.Controls.OfType<Button>().Single().Visible);
        var f10 = Message.Create(surface.Handle, 0x100, (nint)Keys.F10, 0);
        Assert.True(surface.PreProcessMessage(ref f10)); Capture();
        Assert.Contains(painter.Tips, tip => tip.Keys == "ZF"); Assert.Contains(painter.Tips, tip => tip.Keys == "I");
        var orb = surface.RectangleToClient(ribbon.RectangleToScreen(ribbon.ApplicationButtonBounds));
        var file = painter.Tips.First(t => t.Keys == "ZF"); Assert.True(orb.Contains(file.Bounds)); Assert.True(file.Bounds.Top >= 0);
        Assert.DoesNotContain(painter.Tips, tip => tip.Keys == "Z");
        ribbon.ExitKeyTips(false); Capture(); Assert.Empty(painter.Tips);
        surface.ToggleKeyTips();
        Assert.True(ribbon.ProcessBackstageKey(Keys.Z)); Capture();
        Assert.All(painter.Tips, tip => Assert.Equal("ZF", tip.Keys)); Assert.True(ribbon.IsBackstageOpen); Assert.Equal(0, commandCalls);
        field.Focus(); invalid = true; ribbon.ProcessBackstageKey(Keys.F);
        Assert.True(ribbon.IsBackstageOpen); Assert.Equal(0, closed); Capture(); Assert.Empty(painter.Tips);
        invalid = false; surface.ToggleKeyTips(); ribbon.ProcessBackstageKey(Keys.Z); ribbon.ProcessBackstageKey(Keys.F);
        Assert.False(ribbon.IsBackstageOpen); Assert.Equal(1, closed); Assert.Equal(0, commandCalls);
        ribbon.IsBackstageOpen = true; ribbon.FileMenu.KeyTip = "F"; surface.ToggleKeyTips(); Capture(); Assert.Contains(painter.Tips, t => t.Keys == "F");
        ribbon.ProcessBackstageKey(Keys.Tab); Capture(); Assert.Empty(painter.Tips);
        Assert.Equal(handle, field.Handle); Assert.Same(parent, body.Parent); Assert.Equal("Retained", field.Text);
        ribbon.Theme = ribbon.Theme with { BackstageAppearance = ribbon.Theme.BackstageAppearance! with { Workspace = ribbon.Theme.BackstageAppearance.Workspace! with { HeaderHeight = 0 } } };
        surface.ToggleKeyTips(); Capture(); Assert.DoesNotContain(painter.Tips, t => t.Keys == "F");
        ribbon.IsBackstageOpen = false;
        void Capture()
        {
            painter.Tips.Clear(); using var image = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(image, new(Point.Empty, image.Size));
        }
    });

    private sealed class TipProbe : RibbonOffice2024Renderer
    {
        internal List<(string Keys, Rectangle Bounds)> Tips = [];
        public override void DrawKeyTip(RibbonPaintContext c, string keys) { Tips.Add((keys, c.Bounds)); base.DrawKeyTip(c, keys); }
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Joined_glass_keeps_one_exact_caption_crease_and_extends_the_last_ramp(int dpi)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        int caption = S(32), header = S(48), total = caption + header;
        var source = RibbonBackstage2007Renderer.HeaderFill(RibbonThemes.Office2007Blue);
        var joined = RibbonBackstageHeaderJoin.Extend(source, caption, header);
        Assert.Equal(source.Start, joined.Start); Assert.Equal(source.End, joined.End);
        var stops = joined.IntermediateStops!; Assert.Equal(2, stops.Count);
        Assert.Equal(stops[0].Offset, stops[1].Offset);
        Assert.Equal(.45 * (caption - 1d), stops[0].Offset * (total - 1d), 10);
        Assert.Equal(source.IntermediateStops![0].Color, stops[0].Color); Assert.Equal(source.IntermediateStops![1].Color, stops[1].Color);
        var above = RibbonFillColors.At(joined, (caption - 1d) / (total - 1d));
        var below = RibbonFillColors.At(joined, caption / (total - 1d));
        Assert.InRange(Math.Abs(above.R - below.R), 0, 2); Assert.InRange(Math.Abs(above.G - below.G), 0, 2); Assert.InRange(Math.Abs(above.B - below.B), 0, 2);
    }

    [Fact]
    public void Native_colored_standard_caption_and_header_share_a_canvas_with_independent_opt_out_and_overrides() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 700), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.Backstage };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        using var page = new Panel(); ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        Assert.False(ribbon.TryGetBackstageHeaderJoin(false, out _, out _));
        var reference = RibbonThemes.Office2007Blue;
        foreach (var accent in new[] { reference.Accent, Color.SeaGreen, Color.Firebrick })
        {
            var colored = reference with { Accent = accent, TitleBarBackgroundFill = reference.IdentityAccent!.CreateCaptionFill(accent), TitleBarForeground = Color.White };
            ribbon.Theme = colored;
            Assert.True(ribbon.TryGetBackstageHeaderJoin(false, out var fill, out var canvas));
            Assert.Equal(host.CaptionHeight + ribbon.BackstageSurface!.HeaderBounds.Height, canvas.Height);
            using var image = new Bitmap(host.ClientSize.Width, canvas.Height); using var g = Graphics.FromImage(image);
            ribbon.PaintCaptionBackground(g, new(0, 0, image.Width, host.CaptionHeight), false);
            int x = image.Width / 2, y = host.CaptionHeight - 1;
            Assert.Equal(RibbonFillColors.At(fill, y / (double)(canvas.Height - 1)).ToArgb(), image.GetPixel(x, y).ToArgb());
            using var body = new Bitmap(ribbon.BackstageSurface.Width, ribbon.BackstageSurface.Height);
            ribbon.BackstageSurface.DrawToBitmap(body, new(Point.Empty, body.Size));
            Assert.Equal(RibbonFillColors.At(fill, host.CaptionHeight / (double)(canvas.Height - 1)).ToArgb(), body.GetPixel(x, 0).ToArgb());
            Assert.Equal(RibbonFillColors.At(fill, 1).ToArgb(), body.GetPixel(x, ribbon.BackstageSurface.HeaderBounds.Height - 1).ToArgb());
            host.TitleBarTintOpacity = .6f;
            Assert.True(ribbon.TryGetBackstageHeaderJoin(true, out var alpha, out var same));
            Assert.Equal(canvas, same); Assert.Equal((int)Math.Round(255 * host.TitleBarTintOpacity), alpha.Start.A);
            Assert.Equal(fill.IntermediateStops!.Select(s => s.Offset), alpha.IntermediateStops!.Select(s => s.Offset));
            ribbon.Theme = colored with { BackstageAppearance = colored.BackstageAppearance! with { Workspace = colored.BackstageAppearance.Workspace! with { JoinColoredCaption = false } } };
            Assert.False(ribbon.TryGetBackstageHeaderJoin(false, out _, out _));
            ribbon.Theme = colored with { BackstageAppearance = colored.BackstageAppearance! with { Workspace = colored.BackstageAppearance.Workspace! with { HeaderBackground = new(Color.Coral) } } };
            Assert.False(ribbon.TryGetBackstageHeaderJoin(false, out _, out _));
            ribbon.Theme = colored; host.TitleBarColor = Color.DarkSlateGray; Assert.False(ribbon.TryGetBackstageHeaderJoin(false, out _, out _)); host.TitleBarColor = Color.Empty;
            ribbon.Theme = colored with { AeroFrameAppearance = new() }; host.FrameAppearance = RibbonWindowFrameAppearance.Office2010Aero;
            Assert.NotNull(host.AeroFrameSettings); Assert.False(ribbon.TryGetBackstageHeaderJoin(false, out _, out _)); host.FrameAppearance = RibbonWindowFrameAppearance.Standard;
        }
        ribbon.IsBackstageOpen = false; Assert.False(ribbon.TryGetBackstageHeaderJoin(false, out _, out _));
    });
}
