using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackdropBufferOpacityTests
{
    [Theory] [InlineData((byte)1)] [InlineData((byte)128)] [InlineData((byte)255)]
    public void Offset_frame_tiles_match_full_scene_paint_without_writing_into_the_page(byte opacity) => FoundationTests.Sta(() =>
    {
        var size = new Size(67, 19); var bounds = new Rectangle(Point.Empty, size);
        using var full = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        using var tiled = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        using var fullGraphics = Graphics.FromImage(full); using var tileGraphics = Graphics.FromImage(tiled);
        using var scene = new RibbonBackdropBuffer(); using var rail = new RibbonBackdropBuffer(); using var top = new RibbonBackdropBuffer();
        using var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(bounds, Color.FromArgb(30, 90, 180), Color.FromArgb(220, 160, 70), 0f);
        void Paint(Graphics graphics) => graphics.FillRectangle(gradient, bounds);
        tileGraphics.Clear(Color.Magenta);
        scene.Paint(fullGraphics, size, size.Height, Color.Transparent, Paint, opacity: opacity);
        rail.Paint(tileGraphics, new(23, size.Height), size.Height, Color.Transparent, Paint, opacity: opacity);
        top.Paint(tileGraphics, new(size.Width - 23, 4), 4, Color.Transparent, Paint, opacity: opacity, origin: new(23, 0));
        for (int y = 0; y < size.Height; y++) for (int x = 0; x < size.Width; x++)
        {
            var actual = tiled.GetPixel(x, y);
            if (x >= 23 && y >= 4) { Assert.Equal(Color.Magenta.ToArgb(), actual.ToArgb()); continue; }
            // A bitmap Graphics HDC normalizes destination alpha; compare both
            // the actual blit and the source DIB's independent alpha coverage.
            var expected = full.GetPixel(x, y); Assert.Equal(expected.A, actual.A);
            Assert.Equal(scene.Image.GetPixel(x, y).A, (x < 23 ? rail.Image.GetPixel(x, y) : top.Image.GetPixel(x - 23, y)).A);
            Assert.InRange(Math.Abs(expected.R - actual.R), 0, 1); Assert.InRange(Math.Abs(expected.G - actual.G), 0, 1); Assert.InRange(Math.Abs(expected.B - actual.B), 0, 1);
        }
    });

    [Theory] [InlineData((byte)1)] [InlineData((byte)128)] [InlineData((byte)255)]
    public void Fade_preserves_premultiplied_color_and_repainting_does_not_accumulate_opacity(byte alpha) => FoundationTests.Sta(() =>
    {
        using var destination = new Bitmap(12, 12); using var graphics = Graphics.FromImage(destination);
        using var buffer = new RibbonBackdropBuffer(); var color = Color.FromArgb(240, 128, 64);
        using var brush = new SolidBrush(color);
        foreach (byte opacity in new[] { alpha, (byte)255, alpha })
        {
            buffer.Paint(graphics, destination.Size, destination.Height, Color.Transparent, g => g.FillRectangle(brush, 0, 0, 12, 12), opacity: opacity);
            var pixels = buffer.Image.LockBits(new(0, 0, 12, 12), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                int pixel = Marshal.ReadInt32(pixels.Scan0);
                Assert.Equal(opacity, (byte)(pixel >> 24));
                Assert.Equal((color.R * opacity + 127) / 255, (byte)(pixel >> 16));
                Assert.Equal((color.G * opacity + 127) / 255, (byte)(pixel >> 8));
                Assert.Equal((color.B * opacity + 127) / 255, (byte)pixel);
            }
            finally { buffer.Image.UnlockBits(pixels); }
        }
    });
}
