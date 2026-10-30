using System.Drawing.Imaging;
using Xunit;

namespace RibbonKit.WinForms.Tests;

// Native activation messages and offscreen paint only. Never show/activate a window.
public sealed class HiddenCaptionColorTests
{
    [Theory] [InlineData(false)] [InlineData(true)]
    public void Active_caption_keeps_its_foreground_without_keyboard_focus_and_dims_on_real_deactivation(bool composited) => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", 12);
        using var form = new ActivationProbe { Size = new(1000, 500), Text = "Active window caption", Font = font };
        _ = form.Handle;
        Assert.False(form.ContainsFocus); Assert.False(form.Visible);
        form.Activation(true); using var active = Caption(form, composited);
        form.Activation(false); using var inactive = Caption(form, composited);
        long activeInk = Darkness(active), inactiveInk = Darkness(inactive);
        Assert.True(activeInk > inactiveInk * 1.15, $"An active owner must retain stronger title ink while keyboard focus is elsewhere: active={activeInk}; inactive={inactiveInk}.");
        Assert.False(form.ContainsFocus); Assert.False(form.Visible);
    });
    private static Bitmap Caption(RibbonForm form, bool composited)
    {
        var image = new Bitmap(form.ClientSize.Width, form.CaptionHeight, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
        if (composited)
        {
            using var buffer = new RibbonBackdropBuffer();
            buffer.Paint(graphics, image.Size, 0, Color.White, canvas => form.PaintCaption(canvas, null, true), opaque: true);
        }
        else form.PaintCaption(graphics, null);
        return image;
    }
    private static long Darkness(Bitmap image)
    {
        long count = 0;
        for (int y = 1; y < image.Height - 1; y++) for (int x = 60; x < 500; x++)
        { var pixel = image.GetPixel(x, y); count += 765 - pixel.R - pixel.G - pixel.B; }
        return count;
    }
    private sealed class ActivationProbe : RibbonForm
    {
        internal void Activation(bool active)
        { var message = Message.Create(Handle, 0x86, active ? 1 : 0, 0); WndProc(ref message); }
    }
}
