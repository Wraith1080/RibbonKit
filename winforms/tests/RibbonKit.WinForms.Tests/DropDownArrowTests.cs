using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DropDownArrowTests
{
    [Fact]
    public void Dropdown_and_split_indicators_have_consistent_font_independent_device_size() => FoundationTests.Sta(() =>
    {
        const int dpi = 120;
        int S(double value) => (int)Math.Round(value * dpi / 96d);
        using var renderer = new RibbonRenderer();
        foreach (int points in new[] { 9, 18 })
        foreach (bool split in new[] { false, true })
        {
            using var font = new Font("Segoe UI", points);
            using var bitmap = new Bitmap(S(180), S(48));
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            var context = new RibbonPaintContext(graphics, new(S(4), S(4), S(160), S(40)), RibbonThemes.Office2024Light, font, dpi);
            var arrow = new Rectangle(context.Bounds.Right - S(20), context.Bounds.Top, S(20), context.Bounds.Height);
            if (split)
            {
                using var item = new RibbonSplitButtonItem();
                renderer.DrawSplitItem(context with { Bounds = context.Bounds with { Width = context.Bounds.Width - arrow.Width } }, context with { Bounds = arrow }, item, RibbonItemSize.Medium, false);
            }
            else
            {
                using var item = new RibbonDropDownItem();
                renderer.DrawItem(context, item, RibbonItemSize.Medium, false);
            }
            var ink = Rectangle.Empty;
            for (int y = arrow.Top; y < arrow.Bottom; y++)
            for (int x = arrow.Left; x < arrow.Right; x++)
                if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb())
                    ink = ink.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(ink, new(x, y, 1, 1));
            Assert.InRange(ink.Width, S(8) - 1, S(8) + 1);
            Assert.InRange(ink.Height, S(6) - 1, S(6) + 1);
            Assert.InRange(Math.Abs((ink.Top + ink.Bottom) / 2d - (arrow.Top + arrow.Bottom) / 2d), 0, 1);
        }
    });
}
