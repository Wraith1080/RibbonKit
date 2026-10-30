using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackdropBufferOpacityTests
{
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
