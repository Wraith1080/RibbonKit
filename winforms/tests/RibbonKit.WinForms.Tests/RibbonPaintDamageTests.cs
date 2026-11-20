using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class RibbonPaintDamageTests
{
    [Theory]
    [InlineData(RibbonBuiltInTheme.Office2024Light)]
    [InlineData(RibbonBuiltInTheme.Office2010Blue)]
    public void Caption_repaint_does_not_draw_command_body_or_replace_the_callers_clip(RibbonBuiltInTheme theme) => FoundationTests.Sta(() =>
    {
        using var host = new RibbonForm { Bounds = new(-16000, -16000, 1000, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var renderer = new BodyProbe();
        var ribbon = new PaintProbe { BuiltInTheme = theme, Renderer = renderer };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" };
        group.Items.Add(new RibbonButtonItem { Text = "Print" }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        using var image = new Bitmap(ribbon.Width, ribbon.Height); using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Magenta);
        var damage = new Rectangle(0, 0, ribbon.Width, ribbon.WindowCaptionHeight);
        graphics.SetClip(damage); renderer.BodyCalls = 0;
        ribbon.PaintDamage(graphics, damage);
        Assert.Equal(0, renderer.BodyCalls);
        Assert.Equal(damage, Rectangle.Round(graphics.ClipBounds));
        var body = ribbon.Snapshot.BodyViewport;
        Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(body.Left + 5, body.Top + 5).ToArgb());
    });

    private sealed class PaintProbe : RibbonControl
    { internal void PaintDamage(Graphics graphics, Rectangle damage) => OnPaint(new(graphics, damage)); }
    private sealed class BodyProbe : RibbonOffice2024Renderer
    {
        internal int BodyCalls;
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        { BodyCalls++; base.DrawGroup(context, group, collapsed); }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
        { BodyCalls++; base.DrawItem(context, item, size, menu); }
    }
}
