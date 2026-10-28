using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GroupIconTests
{
    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Borrowed_group_icon_is_clipped_by_neither_caption_nor_arrow_and_appears_in_both_qat_surfaces(int dpi) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var group = new RibbonGroup { Text = "Clipboard and document commands" }; var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var icon = new RecordingIcon(); group.Icon = icon; var reference = new RibbonQuickAccessGroupReference { Source = group }; ribbon.QuickAccessItems.Add(reference);
        using var renderer = new RecordingRenderer(); using var image = new Bitmap(500, 500); using var graphics = Graphics.FromImage(image);
        int scale(int value) => (int)Math.Ceiling(value * dpi / 96d);
        var bounds = new Rectangle(0, 0, scale(96), scale(110));
        var context = new RibbonPaintContext(graphics, bounds, ribbon.Theme, ribbon.Font, dpi);
        renderer.DrawGroup(context, group, true); Assert.Single(icon.Bounds); Assert.True(bounds.Contains(icon.Bounds[0])); Assert.All(renderer.Captions, caption => Assert.True(caption.Top >= icon.Bounds[0].Bottom));
        renderer.DrawQuickAccessItem(context with { Bounds = new(0, 0, scale(44), scale(30)) }, reference, false);
        renderer.DrawQuickAccessItem(context with { Bounds = new(0, 0, scale(240), scale(32)) }, reference, true); Assert.Equal(3, icon.Bounds.Count);
        group.Enabled = false; renderer.DrawGroup(context, group, true); Assert.False(icon.Enabled); ribbon.Dispose(); Assert.False(icon.Disposed);
    });
    private sealed class RecordingIcon : IRibbonIcon, IDisposable
    {
        internal readonly List<Rectangle> Bounds = []; internal bool Enabled, Disposed;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { Bounds.Add(bounds); Enabled = context.Enabled; }
        public void Dispose() => Disposed = true;
    }
    private sealed class RecordingRenderer : RibbonOffice2024Renderer
    {
        internal readonly List<Rectangle> Captions = [];
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Captions.Add(context.Bounds); base.DrawText(context, text, color, flags); }
    }
}
