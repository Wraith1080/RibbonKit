using Xunit;

namespace RibbonKit.WinForms.Tests;

// Hidden native state messages and alpha-buffer checks; no desktop input.
public sealed class CaptionButtonBackdropTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, true, false)]
    [InlineData(true, false, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(false, true, true)]
    [InlineData(true, false, true)] [InlineData(true, true, true)]
    public void Only_material_state_fills_are_translucent_and_restore_ink_keeps_its_state_background(bool material, bool integrated, bool restore) => FoundationTests.Sta(() =>
    {
        using var form = new StateForm { Size = new(1000, 600), Text = "Caption states", WindowState = restore ? FormWindowState.Maximized : FormWindowState.Normal };
        using var ribbon = new RibbonControl();
        if (integrated) { form.Controls.Add(ribbon); form.Ribbon = ribbon; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); }
        _ = form.Handle;
        foreach (int hit in new[] { 8, 9, 20 })
        {
            var bounds = form.Caption.Targets.Single(target => target.Hit == hit).Bounds;
            int hoverAlpha = 0;
            foreach (int state in new[] { 0, 1, 2 })
            {
                form.State(state == 0 ? 0 : hit, state == 2);
                using var buffer = new RibbonBackdropBuffer();
                using var output = new Bitmap(form.ClientSize.Width, form.CaptionHeight);
                using var graphics = Graphics.FromImage(output);
                buffer.Paint(graphics, output.Size, output.Height, Color.Transparent, canvas => form.PaintCaption(canvas, integrated ? ribbon : null, material));
                var image = buffer.Image; int alpha = image.GetPixel(bounds.Left + 6, 6).A;
                if (!material) Assert.Equal(255, alpha);
                else if (state == 0) Assert.Equal(0, alpha);
                else
                {
                    Assert.InRange(alpha, 1, 254);
                    if (state == 1) hoverAlpha = alpha; else Assert.True(alpha > hoverAlpha);
                }
                int size = (int)Math.Round(10 * form.DeviceDpi / 96d);
                int x = bounds.Left + (bounds.Width - size) / 2, y = bounds.Top + (bounds.Height - size) / 2;
                int ink = 0;
                for (int py = y - 1; py <= y + size + 1; py++)
                for (int px = x - 1; px <= x + size + 1; px++)
                {
                    var p = image.GetPixel(px, py);
                    if (p.A == 255 && (hit == 20 && state > 0 ? p.R > 245 && p.G > 245 && p.B > 245 : p.R < 60 && p.G < 60 && p.B < 60)) ink++;
                }
                Assert.True(ink >= 8, "Caption symbols must retain opaque ink.");
                if (material && restore && hit == 9)
                    Assert.Equal(alpha, image.GetPixel(x + 2, y + 6).A);
            }
        }
        Assert.False(form.Visible);
    });
    private sealed class StateForm : RibbonForm
    {
        internal void State(int hit, bool pressed)
        {
            Capture = false;
            var cancel = Message.Create(Handle, 0x1F, 0, 0); WndProc(ref cancel);
            var hover = Message.Create(Handle, 0xA0, hit, 0); WndProc(ref hover);
            if (pressed) { var down = Message.Create(Handle, 0xA1, hit, 0); WndProc(ref down); }
        }
    }
}
