using RibbonKit.WinForms.Design;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerRepaintTests
{
    [Fact]
    public void Requests_combine_into_one_clipped_repaint_and_native_feedback_can_be_ignored() => FoundationTests.Sta(() =>
    {
        var posted = new List<Action>(); int paints = 0; RibbonDesignerRepaint? repaint = null;
        repaint = new(posted.Add, region =>
        {
            paints++; Assert.True(repaint!.IsFlushing);
            Assert.True(region.IsVisible(12, 42)); Assert.True(region.IsVisible(350, 85));
            Assert.False(region.IsVisible(350, 400)); Assert.False(region.IsVisible(12, 5));
            // An adorner refresh invalidates controls beneath it synchronously.
            if (!repaint.IsFlushing) repaint.Request(new Rectangle(0, 0, 900, 500));
        });
        using (repaint)
        {
            repaint.Request(new Rectangle(10, 40, 100, 30)); repaint.Request(new Rectangle(300, 80, 300, 25));
            Assert.Single(posted); Assert.Equal(0, paints); posted[0](); Assert.Equal(1, paints); Assert.Single(posted); Assert.False(repaint.IsFlushing);
        }
    });

    [Fact]
    public void Disposing_before_a_queued_callback_prevents_late_designer_access() => FoundationTests.Sta(() =>
    {
        var posted = new List<Action>(); int paints = 0;
        var repaint = new RibbonDesignerRepaint(posted.Add, _ => paints++);
        repaint.Request(new Rectangle(0, 0, 900, 500)); repaint.Dispose(); posted.Single()();
        repaint.Request(new Rectangle(0, 0, 900, 500)); Assert.Equal(0, paints); Assert.Single(posted);
    });

    [Fact]
    public void A_synchronous_nested_refresh_retains_the_outer_feedback_guard() => FoundationTests.Sta(() =>
    {
        int paints = 0; RibbonDesignerRepaint? repaint = null;
        repaint = new(action => action(), _ =>
        {
            Assert.True(repaint!.IsFlushing);
            if (++paints == 1) { repaint.Request(new Rectangle(1, 1, 2, 2)); Assert.True(repaint.IsFlushing); }
        });
        using (repaint) { repaint.Request(new Rectangle(0, 0, 10, 10)); Assert.Equal(2, paints); Assert.False(repaint.IsFlushing); }
    });
}
