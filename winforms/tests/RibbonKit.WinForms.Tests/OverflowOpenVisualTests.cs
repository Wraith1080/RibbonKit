using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OverflowOpenVisualTests
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Overflow_opener_keeps_pressed_ink_until_its_popup_chain_closes(bool simplified, bool office) => FoundationTests.Sta(() =>
    {
        var pressed = new Dictionary<Rectangle, bool>();
        using RibbonRenderer renderer = office ? new OfficeProbe(context => pressed[context.Bounds] = context.Pressed) : new NeutralProbe(context => pressed[context.Bounds] = context.Pressed);
        using var form = new Form { Width = 1000, Height = 650 };
        var ribbon = new RibbonControl { Renderer = renderer, AnimationLevel = RibbonAnimationLevel.None };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" };
        group.Items.Add(new RibbonButtonItem { Text = "Action" }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        if (simplified) ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        else
        {
            ribbon.QuickAccessMaxWidth = 28;
            ribbon.QuickAccessItems.AddRange(new RibbonButtonItem { Text = "Save" }, new RibbonButtonItem { Text = "Undo" });
        }
        form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        var opener = ribbon.Snapshot.Targets.Single(target => simplified ? target.Element is RibbonSimplifiedMore : target.Kind == TargetKind.QuickAccessOverflow);
        var idle = Paint(); Assert.False(pressed[opener.Bounds]);
        ribbon.Activate(opener); Assert.True(ribbon.Popups.IsExpanded(opener.Element!));
        var opened = Paint(); Assert.True(pressed[opener.Bounds]); Assert.False(idle.SequenceEqual(opened));
        if (simplified)
        {
            var parent = ribbon.Popups.Top!;
            ribbon.Activate(parent.Targets.Single(target => target.Element is RibbonQuickAccessGroupReference), parent);
            Assert.Equal(2, ribbon.Popups.Surfaces.Count()); Assert.Equal(opened, Paint()); Assert.True(pressed[opener.Bounds]);
            ribbon.Popups.CloseTop(true); Assert.Equal(opened, Paint()); Assert.True(pressed[opener.Bounds]);
        }
        ribbon.Popups.CloseAll(false); Assert.Equal(idle, Paint()); Assert.False(pressed[opener.Bounds]);
        ribbon.Activate(opener); Assert.Equal(opened, Paint());
        ribbon.Activate(opener); Assert.False(ribbon.Popups.HasOpen); Assert.Equal(idle, Paint());

        int[] Paint()
        {
            using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
            var bounds = opener.VisibleBounds; var pixels = new int[bounds.Width * bounds.Height];
            for (int y = 0; y < bounds.Height; y++) for (int x = 0; x < bounds.Width; x++) pixels[y * bounds.Width + x] = image.GetPixel(bounds.Left + x, bounds.Top + y).ToArgb();
            return pixels;
        }
    });
    private sealed class OfficeProbe(Action<RibbonPaintContext> observe) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessOverflow(RibbonPaintContext context) { observe(context); base.DrawQuickAccessOverflow(context); }
    }
    private sealed class NeutralProbe(Action<RibbonPaintContext> observe) : RibbonRenderer
    {
        public override void DrawQuickAccessOverflow(RibbonPaintContext context) { observe(context); base.DrawQuickAccessOverflow(context); }
    }
}
