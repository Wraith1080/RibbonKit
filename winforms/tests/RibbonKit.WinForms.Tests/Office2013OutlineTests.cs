using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2013OutlineTests
{
    [Fact]
    public void Built_in_body_has_the_reference_outline() => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var image = new Bitmap(260, 100); using var g = Graphics.FromImage(image);
        g.Clear(Color.White); var bounds = new Rectangle(5, 5, 250, 90);
        renderer.DrawBody(new(g, bounds, RibbonThemes.Office2013Light, Control.DefaultFont, 96));
        Assert.Equal(Color.FromArgb(145, 145, 145).ToArgb(), image.GetPixel(bounds.Left + 30, bounds.Top).ToArgb());
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Selected_tab_and_body_share_one_open_seam_in_both_public_renderers(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        foreach (bool office in new[] { false, true })
        foreach (bool clipped in new[] { false, true })
        foreach (int? width in new int?[] { null, 3 })
        {
            var theme = RibbonThemes.Office2013Light with { BodyAppearance = new() { BorderWidth = width } };
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(340), S(170)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            var body = new Rectangle(S(5), S(52), S(330), S(110)); var tabBounds = new Rectangle(S(50), S(12), S(120), S(40));
            var clip = clipped ? new Rectangle(S(80), tabBounds.Top, S(70), tabBounds.Height) : new Rectangle(0, tabBounds.Top, image.Width, tabBounds.Height);
            var gap = RibbonHeaderBodyConnectionMetrics.Resolve(theme, dpi, body, tabBounds, clip);
            var c = new RibbonPaintContext(g, body, theme, Control.DefaultFont, dpi) { HeaderBodyConnectionBounds = gap };
            renderer.DrawBody(c); using var tab = new RibbonTab();
            var saved = g.Save(); g.SetClip(clip); renderer.DrawTab(c with { Bounds = tabBounds }, tab, true); g.Restore(saved);
            int edge = width is { } logical ? S(logical) : 1;
            Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(body.Left + S(10), body.Top).ToArgb());
            Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(body.Left, body.Top + S(20)).ToArgb());
            Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(body.Right - 1, body.Top + S(20)).ToArgb());
            Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(body.Left + S(10), body.Bottom - 1).ToArgb());
            int middle = gap.Left + gap.Width / 2;
            for (int y = tabBounds.Bottom - edge; y < body.Top + edge; y++) Assert.Equal(Color.White.ToArgb(), image.GetPixel(middle, y).ToArgb());
            Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(middle, tabBounds.Top).ToArgb());
            if (!clipped)
            {
                Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(tabBounds.Left, tabBounds.Top + S(10)).ToArgb());
                Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(tabBounds.Right - 1, tabBounds.Top + S(10)).ToArgb());
            }
            else
            {
                Assert.Equal(Color.White.ToArgb(), image.GetPixel(tabBounds.Left, tabBounds.Top + S(10)).ToArgb());
                Assert.Equal(Color.White.ToArgb(), image.GetPixel(clip.Left, body.Top).ToArgb());
            }
            // Without an adjacent body, the tab closes its own bottom edge.
            g.Clear(Color.White); renderer.DrawTab(c with { Bounds = tabBounds, HeaderBodyConnectionBounds = Rectangle.Empty }, tab, true);
            Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(tabBounds.Left + tabBounds.Width / 2, tabBounds.Bottom - 1).ToArgb());
        }
    });

    [Fact]
    public void Independent_border_overrides_zero_alpha_and_seam_policy_keep_fill_composition() => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var image = new Bitmap(340, 170); using var g = Graphics.FromImage(image); using var tab = new RibbonTab();
        var body = new Rectangle(5, 52, 330, 110); var header = new Rectangle(50, 12, 120, 40);
        var basis = RibbonThemes.Office2013Light;
        var authored = basis with { BodyBorder = Color.Navy, BodyAppearance = new() { BorderWidth = 3 }, HeaderAppearance = basis.HeaderAppearance! with { FilledBorderColor = Color.DarkRed, FilledBorderWidth = 2 } };
        Draw(authored); Assert.Equal(Color.DarkRed.ToArgb(), image.GetPixel(100, header.Top).ToArgb()); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(20, body.Top).ToArgb());
        Draw(authored with { HeaderAppearance = authored.HeaderAppearance! with { ConnectSelectedTabToBody = false } });
        Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(100, body.Top).ToArgb()); Assert.Equal(Color.DarkRed.ToArgb(), image.GetPixel(100, header.Bottom - 1).ToArgb());
        Draw(authored with { HeaderAppearance = authored.HeaderAppearance! with { FilledBorderColor = Color.Transparent } });
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(100, header.Top).ToArgb()); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(20, body.Top).ToArgb());
        Draw(authored with { BodyAppearance = new() { BorderWidth = 0 } });
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(20, body.Top).ToArgb()); Assert.Equal(Color.DarkRed.ToArgb(), image.GetPixel(100, header.Top).ToArgb());
        Draw(authored with { HeaderAppearance = authored.HeaderAppearance! with { FilledBorderWidth = 0 } });
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(100, header.Top).ToArgb());
        Draw(basis with { BodyBackground = new(Color.Red, Color.Blue), HeaderSelectedBackground = new(Color.Red, Color.Blue) });
        Assert.Equal(Color.Red.ToArgb(), image.GetPixel(100, body.Top).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), image.GetPixel(100, header.Bottom - 1).ToArgb());
        var alpha = basis with { BodyBackground = new(Color.FromArgb(80, Color.Red)), HeaderSelectedBackground = new(Color.FromArgb(80, Color.Red)), BodyBorder = Color.FromArgb(128, Color.Blue) };
        Draw(alpha, Color.Magenta);
        Assert.Equal(image.GetPixel(100, body.Top + 10), image.GetPixel(100, body.Top));
        Assert.Equal(image.GetPixel(100, header.Top + 10), image.GetPixel(100, header.Bottom - 1));
        var reset = authored with { HeaderAppearance = authored.HeaderAppearance! with { FilledBorderColor = Color.Empty, FilledBorderWidth = null } };
        Draw(reset); Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(100, header.Top).ToArgb());
        Assert.Equal(3, reset.BodyAppearance!.BorderWidth);

        void Draw(RibbonTheme theme, Color? backing = null)
        {
            g.Clear(backing ?? Color.White); var gap = RibbonHeaderBodyConnectionMetrics.Resolve(theme, 96, body, header, header);
            var c = new RibbonPaintContext(g, body, theme, Control.DefaultFont, 96) { HeaderBodyConnectionBounds = gap };
            renderer.DrawBody(c); renderer.DrawTab(c with { Bounds = header }, tab, true);
        }
    });

    [Theory] [InlineData(96)] [InlineData(192)]
    public void Very_thick_outlines_on_short_frames_keep_the_bottom_border(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var theme = RibbonThemes.Office2013Light with { BodyAppearance = new() { BorderWidth = 32 }, HeaderAppearance = new() { ConnectSelectedTabToBody = true, FilledBorderWidth = 1 } };
        var body = new Rectangle(S(5), S(52), S(250), S(10)); var tab = new Rectangle(S(50), S(12), S(120), S(40));
        using var renderer = new RibbonOffice2024Renderer(); using var image = new Bitmap(S(270), S(70)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
        var gap = RibbonHeaderBodyConnectionMetrics.Resolve(theme, dpi, body, tab, tab);
        renderer.DrawBody(new(g, body, theme, Control.DefaultFont, dpi) { HeaderBodyConnectionBounds = gap });
        int center = gap.Left + gap.Width / 2;
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(center, body.Top).ToArgb());
        Assert.Equal(theme.BodyBorder.ToArgb(), image.GetPixel(center, body.Bottom - 1).ToArgb());
    });

    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Live_selection_scroll_visibility_collection_and_minimization_use_current_measured_bounds(RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 780, BuiltInTheme = RibbonBuiltInTheme.Office2013Light, LayoutMode = mode, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true;
        for (int i = 0; i < 7; i++) ribbon.Tabs.Add(new RibbonTab { Id = "t" + i, Text = "Document layout " + i, IsContextual = i == 3, ContextualColor = Color.Purple });
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save", Icon = RibbonStockIcons.Create("save") });
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
        {
            ribbon.Density = density; ribbon.QuickAccessPosition = position;
            ribbon.SelectedTab = ribbon.Tabs[0]; ribbon.Scroll(true, int.MinValue);
            Check(); ribbon.Scroll(true, 53); Check();
            ribbon.SelectedTab = ribbon.Tabs[3]; Check(); ribbon.Tabs[3].Visible = false; Check(); ribbon.Tabs[3].Visible = true;
            var moved = ribbon.Tabs[0]; ribbon.Tabs.RemoveAt(0); ribbon.Tabs.Insert(2, moved); Check(); ribbon.Tabs.RemoveAt(2); ribbon.Tabs.Insert(0, moved);
            var before = Geometry(ribbon.Snapshot); var theme = ribbon.Theme;
            ribbon.Theme = theme with { HeaderAppearance = theme.HeaderAppearance! with { ConnectSelectedTabToBody = false } };
            Assert.Equal(before, Geometry(ribbon.Snapshot)); ribbon.Theme = theme; Check();
            ribbon.IsMinimized = true; Check(); ribbon.IsMinimized = false; Check();
        }
        void Check()
        {
            var s = ribbon.Snapshot; var selected = s.Targets.SingleOrDefault(t => t.Kind == TargetKind.Tab && ReferenceEquals(t.Element, ribbon.DisplaySelectedTab));
            using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
            if (s.BodyFrameBounds.IsEmpty || selected is null) return;
            var gap = RibbonHeaderBodyConnectionMetrics.Resolve(ribbon.Theme, ribbon.DeviceDpi, s.BodyFrameBounds, selected.Bounds, selected.Clip);
            int y = s.BodyFrameBounds.Top;
            for (int x = s.BodyFrameBounds.Left + 1; x < s.BodyFrameBounds.Right - 1; x++)
            {
                bool open = gap.Height > 0 && x >= gap.Left && x < gap.Right;
                Assert.Equal((open ? Color.White : ribbon.Theme.BodyBorder).ToArgb(), image.GetPixel(x, y).ToArgb());
            }
        }
        static string[] Geometry(RibbonLayoutSnapshot s) => s.Targets.Select(t => $"{t.Identity}:{t.Bounds}:{t.Clip}").Append($"{s.BodyBounds}/{s.BodyFrameBounds}/{s.BodyViewport}/{s.TabViewport}").ToArray();
    });

    [Theory]
    [InlineData(RibbonTabContentAnimation.Fade, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonTabContentAnimation.Fade, RibbonAnimationLevel.Expressive)]
    [InlineData(RibbonTabContentAnimation.SlideDown, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonTabContentAnimation.SlideDown, RibbonAnimationLevel.Expressive)]
    public void Selected_header_and_connected_border_commit_on_the_first_content_animation_frame(RibbonTabContentAnimation style, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Bounds = new(-16000, -16000, 1000, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light, TabContentAnimation = style };
        ribbon.FileMenu.Visible = true;
        foreach (string name in new[] { "Home", "View" })
        { var tab = new RibbonTab { Text = name }; var group = new RibbonGroup { Text = name }; group.Items.Add(new RibbonButtonItem { Text = name }); tab.Groups.Add(group); ribbon.Tabs.Add(tab); }
        form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        using var from = new RibbonMotionScene(ribbon); ribbon.SelectedTab = ribbon.Tabs[1]; using var to = new RibbonMotionScene(ribbon);
        ribbon.AnimationLevel = level;
        var target = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, ribbon.SelectedTab));
        int[]? departureContent = null;
        foreach (double p in new[] { 0d, .05, .25, .65, .95, 1d })
        {
            using var frame = new Bitmap(ribbon.Width, ribbon.Height, PixelFormat.Format32bppPArgb); using var g = Graphics.FromImage(frame); using var alpha = new ImageAttributes();
            RibbonMotionScene.Render(g, to.Bounds, ribbon, from, to, p, alpha);
            foreach (var header in to.Headers.Values)
                for (int x = header.Left; x < header.Right; x++) for (int y = header.Top; y < header.Bottom; y++)
                    Assert.Equal(to.Image.GetPixel(x, y).ToArgb(), frame.GetPixel(x, y).ToArgb());
            Assert.Equal(Color.White.ToArgb(), frame.GetPixel(target.Bounds.Left + target.Bounds.Width / 2, to.Frame.Top).ToArgb());
            if (p is 0 or 1)
            {
                var crop = Rectangle.Inflate(to.Body, -4, -4);
                var pixels = Enumerable.Range(crop.Top, crop.Height).SelectMany(y => Enumerable.Range(crop.Left, crop.Width).Select(x => frame.GetPixel(x, y).ToArgb())).ToArray();
                if (p == 0) departureContent = pixels; else Assert.NotEqual(departureContent, pixels);
            }
            if (Environment.GetEnvironmentVariable("RIBBONKIT_OFFICE2013_OUTLINE_MOTION_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); frame.Save(Path.Combine(output, $"{style}-{level}-{p.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}.png")); }
        }
    });
}
