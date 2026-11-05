using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TabHeaderClippingTests
{
    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    public void Scrolled_tab_text_stays_out_of_File_QAT_and_overflow_slots(bool office, int qat) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new Office() : new Neutral();
        using var ribbon = new RibbonControl { Width = 780, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.FileMenu.Visible = true; ribbon.QuickAccessVisible = qat != 0; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = qat == 2 ? 80 : 240;
        for (int i = 0; i < 7; i++) ribbon.Tabs.Add(new RibbonTab { Text = "Gallery geometry " + i });
        for (int i = 0; i < 4; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save " + i, Icon = RibbonStockIcons.Create("save") });
        var sources = ribbon.QuickAccessItems.ToArray();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            ribbon.Density = density; ribbon.LayoutMode = mode; ribbon.Scroll(true, int.MinValue);
            var original = ribbon.Snapshot;
            Assert.Contains(original.Targets, t => t.Kind == TargetKind.TabBack);
            if (qat == 2) Assert.Contains(original.Targets, t => t.Kind == TargetKind.QuickAccessOverflow);
            foreach (int offset in new[] { 54, 53, 74 })
            {
                ribbon.Scroll(true, offset); var snapshot = ribbon.Snapshot; var viewport = snapshot.TabViewport;
                Assert.Equal(original.TabViewport, viewport); Assert.Equal(original.QuickAccessBounds, snapshot.QuickAccessBounds);
                Assert.Equal(original.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds, snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds);
                Assert.All(snapshot.Targets.Where(t => t.Kind == TargetKind.Tab && !t.VisibleBounds.IsEmpty), t => Assert.True(viewport.Contains(t.VisibleBounds)));
                using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                string? output = Environment.GetEnvironmentVariable("RIBBONKIT_TAB_CLIP_CAPTURE");
                if (!string.IsNullOrEmpty(output)) { Directory.CreateDirectory(output); image.Save(Path.Combine(output, $"{office}-{qat}-{density}-{mode}-{offset}.png")); }
                int escaped = 0, visible = 0;
                for (int y = viewport.Top; y < viewport.Bottom; y++) for (int x = 0; x < image.Width; x++)
                    if (IsTabInk(image.GetPixel(x, y))) { if (viewport.Contains(x, y)) visible++; else escaped++; }
                Assert.True(visible > 20, "The check must capture actual tab glyphs.");
                Assert.True(escaped == 0, $"{office}/{qat}/{density}/{mode}: {escaped} tab pixels escaped the header viewport.");
            }
            Assert.Equal(sources, ribbon.QuickAccessItems);
        }
    });

    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void Tab_glyphs_honor_the_paint_clip_in_native_and_composited_paths(bool office, bool composited) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var font = new Font("Segoe UI", 12); var tab = new RibbonTab { Text = "A long scrolled tab header" };
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            var bounds = new Rectangle(20, 0, 330, 70); var clip = new Rectangle(160, 0, 120, 70);
            using var image = new Bitmap(400, 80); using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
            void Draw(Graphics g) { if (composited) g.Clear(Color.White); g.SetClip(clip, CombineMode.Intersect); painter.DrawTab(new(g, bounds, RibbonThemes.Office2024Light, font, dpi) { CompositedText = composited }, tab, true); }
            if (composited) { using var buffer = new RibbonBackdropBuffer(); buffer.Paint(graphics, image.Size, image.Height, Color.White, Draw); } else Draw(graphics);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_TAB_CLIP_CAPTURE") is { Length: > 0 } output) { Directory.CreateDirectory(output); image.Save(Path.Combine(output, $"direct-{office}-{composited}-{dpi}.png")); }
            int ink = 0;
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                if (!clip.Contains(x, y)) Assert.True(image.GetPixel(x, y).ToArgb() == Color.White.ToArgb(), $"{office}/{composited}/{dpi}: outside clip at {x},{y}: {image.GetPixel(x,y)}"); else if (image.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) ink++;
            Assert.True(ink > 20);
        }
    });
    private static bool IsTabInk(Color c) => c.R > 160 && c.B > 160 && c.G < 120;
    private sealed class Neutral : RibbonRenderer
    {
        private bool _tab;
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { _tab = true; try { base.DrawTab(c, tab, selected); } finally { _tab = false; } }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => base.DrawText(c, text, _tab ? Color.Magenta : color, flags);
    }
    private sealed class Office : RibbonOffice2024Renderer
    {
        private bool _tab;
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { _tab = true; try { base.DrawTab(c, tab, selected); } finally { _tab = false; } }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => base.DrawText(c, text, _tab ? Color.Magenta : color, flags);
    }
}
