using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackdropGroupCaptionTests
{
    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Material_group_caption_keeps_the_opaque_vertical_position(int dpi) => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        int S(int value) => (int)Math.Round(value * dpi / 96d);
        using var renderer = new RibbonOffice2024Renderer();
        using var group = new RibbonGroup { Text = "Document tools" };
        using var font = new Font("Segoe UI", 9 * dpi / 96f);
        var theme = RibbonThemes.Office2024Light with { SecondaryText = Color.Magenta };
        var bounds = new Rectangle(S(12), S(8), S(220), S(104));
        var size = new Size(S(250), S(130));
        Rectangle Ink(bool material)
        {
            using var image = new Bitmap(size.Width, size.Height);
            using var graphics = Graphics.FromImage(image);
            using var buffer = new RibbonBackdropBuffer();
            void Paint(Graphics canvas) => renderer.DrawGroup(new(canvas, bounds, theme, font, dpi) { CompositedText = material }, group, false);
            if (material) buffer.Paint(graphics, size, 0, Color.White, Paint);
            else { graphics.Clear(Color.White); Paint(graphics); }
            int left = size.Width, top = size.Height, right = 0, bottom = 0;
            for (int y = 0; y < size.Height; y++) for (int x = 0; x < size.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                if (pixel.R > 120 && pixel.B > 120 && pixel.G < 100)
                { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x + 1); bottom = Math.Max(bottom, y + 1); }
            }
            Assert.True(right > left && bottom > top, "The caption must have visible glyphs.");
            return Rectangle.FromLTRB(left, top, right, bottom);
        }
        var opaque = Ink(false); var material = Ink(true);
        Assert.True(Math.Abs(opaque.Top - material.Top) <= 1 && Math.Abs(opaque.Bottom - material.Bottom) <= 1,
            $"At {dpi} DPI: opaque caption {opaque}; material caption {material}.");
    });
}
