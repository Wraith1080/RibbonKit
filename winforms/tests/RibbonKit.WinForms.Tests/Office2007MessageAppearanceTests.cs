using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public class Office2007MessageAppearanceTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Each_row_restarts_the_reference_gold_gradient_inside_one_connected_amber_frame(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var theme = RibbonThemes.Office2007Blue;
        Assert.Equal(Color.FromArgb(0xFF, 0xFF, 0xE6), theme.MessageBackground.Start);
        Assert.Equal(Color.FromArgb(0xFF, 0xF2, 0xC0), theme.MessageBackground.End);
        Assert.Equal(new[] { .45, .46 }, theme.MessageBackground.IntermediateStops!.Select(s => s.Offset));
        foreach (bool office in new[] { false, true }) foreach (bool connected in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            var frame = new Rectangle(S(8), S(8), S(430), S(68)); int height = S(34);
            using var image = new Bitmap(frame.Right + S(8), frame.Bottom + S(8)); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
            var c = new RibbonPaintContext(g, frame, theme, Control.DefaultFont, dpi) { IsTopEdgeConnected = connected };
            painter.DrawMessageBar(c);
            using var message = new RibbonMessage();
            for (int i = 0; i < 2; i++)
                painter.DrawMessage(c with { Bounds = new(frame.Left, frame.Top + height * i, frame.Width, height), BackgroundBounds = frame }, message, Rectangle.Empty, Rectangle.Empty, Rectangle.Empty, i == 0);
            for (int y = S(4); y < height - S(4); y++)
            {
                var actual = image.GetPixel(frame.Left + frame.Width / 2, frame.Top + y);
                Assert.Equal(actual, image.GetPixel(frame.Left + frame.Width / 2, frame.Top + height + y));
                Assert.Equal(RibbonFillColors.At(theme.MessageBackground, y / (height - 1d)).ToArgb(), actual.ToArgb());
            }
            Assert.Equal(theme.MessageBorder.ToArgb(), image.GetPixel(frame.Left, frame.Top + height).ToArgb());
            Assert.Equal(theme.MessageBorder.ToArgb(), image.GetPixel(frame.Left + frame.Width / 2, frame.Top + height).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(frame.Right, frame.Bottom - 1).ToArgb());
            Assert.Equal(image.GetPixel(frame.Left, frame.Bottom - 1), image.GetPixel(frame.Right - 1, frame.Bottom - 1));
            Assert.Equal(image.GetPixel(frame.Left + 1, frame.Bottom - 2), image.GetPixel(frame.Right - 2, frame.Bottom - 2));
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Message_actions_and_close_states_have_thin_independent_rims_and_keep_the_callers_clip(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var theme = RibbonThemes.Office2007Blue; using var painter = new RibbonOffice2024Renderer();
        using var action = new RibbonButtonItem(); var bounds = new Rectangle(S(10), S(10), S(100), S(28));
        using var image = new Bitmap(S(130), S(55)); using var g = Graphics.FromImage(image);
        foreach (int state in new[] { 0, 1, 2, 3 })
        {
            action.Enabled = state != 3; g.Clear(Color.Magenta);
            var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi, Hot: state == 1, Pressed: state == 2);
            painter.DrawMessageAction(c, action);
            var border = state == 2 ? Color.FromArgb(0xB6, 0xA8, 0x92) : state == 1 ? Color.FromArgb(0xC1, 0xA8, 0x77) : Color.FromArgb(0x8D, 0xB2, 0xE3);
            Assert.Equal(border.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top).ToArgb());
            if (state == 0) Assert.Equal(Color.White.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top + 1).ToArgb());
            g.Clear(Color.Magenta); painter.DrawMessageClose(c, action.Enabled);
            if (state is 1 or 2) Assert.Equal(Color.FromArgb(0xA9, 0x7F, 0x2A).ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top).ToArgb());
            else Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top).ToArgb());
        }
        var clip = new Rectangle(bounds.Left + 3, bounds.Top + 3, bounds.Width / 2, bounds.Height / 2); g.Clear(Color.Magenta); g.SetClip(clip);
        painter.DrawMessageAction(new(g, bounds, theme, Control.DefaultFont, dpi), action);
        Assert.Equal(clip, Rectangle.Round(g.ClipBounds)); Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(clip.Right, clip.Top).ToArgb());
    });

    [Fact]
    public void Row_alpha_is_composited_once_and_zero_or_transparent_rims_keep_their_authority()
    {
        using var painter = new RibbonRenderer(); using var message = new RibbonMessage();
        var theme = RibbonThemes.Office2007Blue with { MessageBackground = new(Color.FromArgb(128, Color.Red)), MessageBorder = Color.Transparent,
            BodyAppearance = new() { MessageBorderWidth = 0 }, MessageAppearance = new() { BackgroundPerRow = true, CornerRadius = 0 } };
        using var pixels = new Bitmap(200, 80); using var g = Graphics.FromImage(pixels); g.Clear(Color.Navy);
        var c = new RibbonPaintContext(g, new(0, 0, 200, 80), theme, Control.DefaultFont, 96);
        painter.DrawMessageBar(c);
        for (int i = 0; i < 2; i++) painter.DrawMessage(c with { Bounds = new(0, i * 40, 200, 40), BackgroundBounds = c.Bounds }, message, Rectangle.Empty, Rectangle.Empty, Rectangle.Empty, i == 0);
        using var expected = new Bitmap(1, 1); using var eg = Graphics.FromImage(expected); eg.Clear(Color.Navy); using var brush = new SolidBrush(Color.FromArgb(128, Color.Red)); eg.FillRectangle(brush, 0, 0, 1, 1);
        Assert.Equal(expected.GetPixel(0, 0), pixels.GetPixel(100, 20)); Assert.Equal(pixels.GetPixel(100, 20), pixels.GetPixel(100, 60));
    }

    [Fact]
    public void Message_overrides_validate_atomically_reset_independently_and_preserve_native_commands() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2007Blue, AnimationLevel = RibbonAnimationLevel.None };
        var original = ribbon.Theme;
        foreach (var invalid in new[] { new RibbonMessageAppearanceSettings { CornerRadius = 65 }, new() { ActionBorderWidth = -1 }, new() { CloseCornerRadius = 65 }, new() { ActionStateChrome = new() { InnerBorderWidth = 33 } } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = original with { MessageAppearance = invalid }); Assert.Same(original, ribbon.Theme); }
        ribbon.Tabs.Add(new RibbonTab());
        using var first = new RibbonMessage { Title = "PROTECTED VIEW", Text = "Review this document." }; first.Action.Text = "Enable Editing";
        using var second = new RibbonMessage { Title = "SECURITY NOTICE", Text = "Macros are disabled." }; ribbon.MessageBar.Items.AddRange(first, second);
        var before = RibbonMessageLayout.Measure(ribbon, 900, ribbon.DeviceDpi);
        int invoked = 0, dismissed = 0; first.Action.Click += (_, _) => invoked++; first.Dismissed += (_, _) => dismissed++;
        ribbon.Theme = original with { MessageAppearance = original.MessageAppearance! with { ActionBorderWidth = 0, CloseBorderWidth = 0, CornerRadius = 0, ActionBackground = new(Color.Transparent) } };
        var after = RibbonMessageLayout.Measure(ribbon, 900, ribbon.DeviceDpi);
        Assert.Equal(before.Rows.Select(r => (r.Bounds, r.Action, r.Close, r.Title, r.Text)), after.Rows.Select(r => (r.Bounds, r.Action, r.Close, r.Title, r.Text)));
        first.Action.PerformClick(); first.Dismiss(); Assert.Equal(1, invoked); Assert.Equal(1, dismissed); Assert.True(second.IsOpen);
        ribbon.Theme = original with { MessageAppearance = null }; Assert.Null(ribbon.Theme.MessageAppearance);
        Assert.Equal(original.MessageBackground, ribbon.Theme.MessageBackground); Assert.Same(original.OptionsAppearance, ribbon.Theme.OptionsAppearance);
    });
}
