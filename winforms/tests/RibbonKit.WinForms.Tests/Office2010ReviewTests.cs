using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2010ReviewTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Backstage_hover_is_flat_with_only_lighter_horizontal_edges(int dpi) => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2010Blue; using var renderer = new RibbonOffice2024Renderer(); using var item = new RibbonButtonItem();
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var image = new Bitmap(S(240), S(54)); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, dpi, Hot: true);
        renderer.DrawBackstageEntry(c, item, false);
        Assert.Equal(image.GetPixel(S(150), S(10)), image.GetPixel(S(150), S(40)));
        Assert.True(image.GetPixel(S(150), 0).R > image.GetPixel(S(150), S(10)).R);
        var hidden = theme with { BackstageAppearance = theme.BackstageAppearance! with { NavigationHoverEdgeWidth = 0 } };
        renderer.DrawBackstageEntry(c with { Theme = hidden }, item, false);
        Assert.Equal(image.GetPixel(S(150), 0), image.GetPixel(S(150), S(10)));
    });

    [Fact]
    public void Live_native_selected_row_rebases_after_accent_change_without_replacing_page() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 900, 650), ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
        using var notes = new RichTextBox { Text = "Retained notes" }; var page = new RibbonBackstagePageItem { Text = "Info", Content = notes };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.Backstage.Items.Add(page); ribbon.FileMenu.Visible = true; form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Show(); ribbon.IsBackstageOpen = true; Application.DoEvents(); var handle = notes.Handle; var parent = notes.Parent;
        var surface = ribbon.BackstageSurface!; var row = Descendants(surface).Single(c => c.AccessibleName == "Info" && c is Button);
        var before = Pixel();
        foreach (var accent in new[] { Color.Green, Color.Purple, Color.Red })
        {
            ribbon.Theme = ribbon.Theme with { Accent = accent }; Application.DoEvents(); var current = Pixel();
            Assert.NotEqual(before, current); before = current;
            Assert.Equal(handle, notes.Handle); Assert.Same(parent, notes.Parent); Assert.Equal("Retained notes", notes.Text);
        }
        Color Pixel() { using var image = new Bitmap(row.Width, row.Height); row.DrawToBitmap(image, new(Point.Empty, image.Size)); return image.GetPixel(row.Width / 2, row.Height - 4); }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Header_and_gallery_utilities_use_command_strength_and_gold_state_outlines(bool pressed) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue }; using var painter = new Probe();
        using var image = new Bitmap(180, 70); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(5, 5, 140, 50), ribbon.HeaderPaintTheme(false, true), Control.DefaultFont, 96, Hot: true, Pressed: pressed);
        painter.DrawMinimizeButton(c, false); Assert.Equal(pressed ? ribbon.Theme.Pressed : ribbon.Theme.Hover, painter.Fill);
        painter.DrawModalClose(c, "Close"); Assert.Equal(pressed ? ribbon.Theme.Pressed : ribbon.Theme.Hover, painter.Fill);
        foreach (var button in Enum.GetValues<RibbonGalleryStripButton>())
        {
            g.Clear(Color.White); painter.DrawGalleryStripButton(c with { Theme = ribbon.Theme }, button, true);
            var ink = pressed ? ribbon.Theme.GalleryAppearance!.UtilityStateChrome!.PressedBorderColor : ribbon.Theme.GalleryAppearance!.UtilityStateChrome!.HoverBorderColor;
            Assert.Contains(Pixels(image), p => p.ToArgb() == ink.ToArgb());
        }
    });

    [Theory] [InlineData(8)] [InlineData(9)] [InlineData(20)]
    public void Caption_states_have_reflection_depth_and_independent_rim_reset(int hit) => FoundationTests.Sta(() =>
    {
        using var painter = new RoundedPainter(); using var image = new Bitmap(80, 44); using var g = Graphics.FromImage(image);
        var theme = RibbonThemes.Office2010Blue with { CaptionButtonAppearance = RibbonThemes.Office2010Blue.CaptionButtonAppearance! with { GlyphSize = 0 } };
        var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, 96, Hot: true);
        RibbonCaptionButtonRenderer.Draw(painter, c, hit, false, false, _ => { });
        Assert.NotEqual(image.GetPixel(40, 9), image.GetPixel(40, 25));
        if (Environment.GetEnvironmentVariable("RIBBONKIT_OFFICE2010_REVIEW_CAPTURE") is { Length: > 0 } output)
        {
            Directory.CreateDirectory(output);
            foreach (bool press in new[] { false, true })
            {
                g.Clear(Color.FromArgb(216, 229, 243));
                RibbonCaptionButtonRenderer.Draw(painter, c with { Theme = RibbonThemes.Office2010Blue, Pressed = press }, hit, false, false, _ => { });
                image.Save(Path.Combine(output, $"caption-{hit}-{press}.png"));
            }
        }
        Assert.True(theme.CaptionButtonAppearance!.StateChrome!.InnerBorderWidth > 0);
        var reset = theme with { CaptionButtonAppearance = new() { GlyphSize = 0, HoverBackground = new(Color.Lime), CloseHoverBackground = new(Color.Lime) } };
        g.Clear(Color.White); RibbonCaptionButtonRenderer.Draw(painter, c with { Theme = reset }, hit, false, false, _ => { });
        Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(40, 9).ToArgb()); Assert.Equal(image.GetPixel(40, 9), image.GetPixel(40, 25));
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void All_text_respects_a_body_viewport_clip_in_solid_and_composited_paint(bool composited) => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var font = new Font("Segoe UI", 18);
        using var image = new Bitmap(320, 60); using var g = Graphics.FromImage(image); g.Clear(Color.White);
        var clip = new Rectangle(40, 0, 70, 60); g.SetClip(clip);
        renderer.DrawText(new(g, new(0, 0, 300, 60), RibbonThemes.Office2024Light, font, 96) { CompositedText = composited }, "MMMMMMMMMMMMMMMM", Color.Magenta, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        int painted = 0;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
        { if (clip.Contains(x, y)) { if (image.GetPixel(x, y).G < 200) painted++; } else Assert.Equal(Color.White.ToArgb(), image.GetPixel(x, y).ToArgb()); }
        Assert.True(painted > 0);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Presenter_frame_uses_theme_ink_and_resets_without_painting_content(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (var theme in new[] { RibbonThemes.Office2010Blue, RibbonThemes.Office2013Light, RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
        {
            using var renderer = new RibbonRenderer(); using var image = new Bitmap(120, 70); using var g = Graphics.FromImage(image); var bounds = new Rectangle(4, 4, 100, 55);
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
            g.Clear(Color.Magenta); renderer.DrawPopupContentFrame(c);
            Assert.Equal(theme.EditorBorderColor.ToArgb(), image.GetPixel(50, 4).ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(50, 20).ToArgb());
            g.Clear(Color.Magenta); renderer.DrawPopupContentFrame(c with { Theme = theme with { PopupAppearance = new() { ContentBorderWidth = 0 } } });
            Assert.All(Pixels(image), p => Assert.Equal(Color.Magenta.ToArgb(), p.ToArgb()));
            renderer.DrawPopupContentFrame(c with { Theme = theme with { PopupAppearance = new() { ContentBorderColor = Color.Lime, ContentBorderWidth = 2 } } }); Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(50, 4).ToArgb());
        }
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Narrow_2024_body_keeps_real_command_and_group_text_inside_its_viewport(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var form = new Form { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 285, 600), ShowInTaskbar = false };
        using var renderer = new BodyTextProbe(); using var ribbon = new RibbonControl { Renderer = renderer, LayoutMode = mode, AnimationLevel = RibbonAnimationLevel.None };
        var tab = new RibbonTab { Text = "Home" };
        foreach (int index in Enumerable.Range(0, 4))
        {
            var group = new RibbonGroup { Text = "Formatting commands " + index, CanResize = false };
            group.Items.Add(new RibbonButtonItem { Text = "A long command caption " + index, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.Add(group);
        }
        ribbon.Tabs.Add(tab); form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
        var clip = ribbon.Snapshot.BodyViewport; int inside = 0;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
        {
            var pixel = image.GetPixel(x, y); if (pixel.R > 200 && pixel.B > 150 && pixel.G < 80)
            { Assert.True(clip.Contains(x, y), $"Body text leaked at {x},{y}, viewport {clip}"); inside++; }
        }
        Assert.True(inside > 0);
        if (Environment.GetEnvironmentVariable("RIBBONKIT_OFFICE2010_REVIEW_CAPTURE") is { Length: > 0 } output)
        { Directory.CreateDirectory(output); image.Save(Path.Combine(output, "narrow-2024-" + mode + ".png")); }
    });

    [Fact]
    public void New_decoration_ranges_reject_atomically() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var theme = ribbon.Theme;
        foreach (var invalid in new[] { theme with { PopupAppearance = new() { ContentBorderWidth = 33 } }, theme with { BackstageAppearance = new() { NavigationHoverEdgeWidth = -1 } },
            theme with { HeaderAppearance = new() { UtilityStateChrome = new() { InnerBorderWidth = 33 } } }, theme with { GalleryAppearance = new() { UtilityStateChrome = new() { InnerBorderInset = -1 } } },
            theme with { CaptionButtonAppearance = new() { StateChrome = new() { InnerBorderWidth = -1 } } } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = invalid); Assert.Same(theme, ribbon.Theme); }
    });

    private static IEnumerable<Control> Descendants(Control root) { foreach (Control child in root.Controls) { yield return child; foreach (var c in Descendants(child)) yield return c; } }
    private static IEnumerable<Color> Pixels(Bitmap image) { for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) yield return image.GetPixel(x, y); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal RibbonFill Fill;
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border) { Fill = fill; base.DrawChrome(c, fill, border); }
    }
    private sealed class BodyTextProbe : RibbonOffice2024Renderer
    {
        private bool _body;
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup group, bool collapsed) { _body = true; try { base.DrawGroup(c, group, collapsed); } finally { _body = false; } }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { _body = true; try { base.DrawItem(c, item, size, menu); } finally { _body = false; } }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => base.DrawText(c, text, _body ? Color.Magenta : color, flags);
    }
}
