using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class HeaderButtonTests
{
    [Fact]
    public void Header_utilities_keep_equal_painted_margins_at_each_dpi_without_changing_their_hit_slots() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl { Width = 1100 }; var home = new RibbonTab { Text = "Home" }; var preview = new RibbonTab { Text = "Preview", IsModal = true };
        host.Tabs.AddRange(home, preview); var minimize = host.Snapshot.Targets.Single(t => t.Kind == TargetKind.Minimize).Bounds;
        host.EnterModal(preview); var close = host.Snapshot.Targets.Single(t => t.Kind == TargetKind.ModalClose).Bounds;
        host.Theme = host.Theme with { HeaderButtonInset = 7 }; Assert.Equal(close, host.Snapshot.Targets.Single(t => t.Kind == TargetKind.ModalClose).Bounds);
        host.ExitModal(); Assert.Equal(minimize, host.Snapshot.Targets.Single(t => t.Kind == TargetKind.Minimize).Bounds);
        Assert.Throws<ArgumentOutOfRangeException>(() => host.Theme = host.Theme with { HeaderButtonInset = -1 });
        using var neutral = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        foreach (var painter in new RibbonRenderer[] { neutral, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int odd in new[] { 0, 1 })
        foreach (bool modal in new[] { false, true })
        foreach (int inset in new[] { 0, 4, 7, int.MaxValue })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            var slot = new Rectangle(0, 0, S(modal ? 180 : 28) + odd, S(32) + odd);
            var theme = RibbonThemes.Office2024Light with { HeaderButtonInset = inset };
            var visual = RibbonHeaderButtonLayout.PaintBounds(slot, theme, dpi);
            Assert.Equal(visual.Top, slot.Bottom - visual.Bottom); Assert.Equal(visual.Top, slot.Right - visual.Right);
            Assert.Equal(slot.Top + slot.Bottom, visual.Top + visual.Bottom);
            foreach (bool pressed in new[] { false, true })
            {
                using var bitmap = new Bitmap(slot.Width, slot.Height); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(theme.Background);
                using var font = new Font("Segoe UI", 9 * dpi / 96f);
                var context = new RibbonPaintContext(graphics, slot, theme, font, dpi, true, pressed, modal);
                if (modal) painter.DrawModalClose(context, "Close Preview"); else painter.DrawMinimizeButton(context, pressed);
                for (int y = 0; y < slot.Height; y++) for (int x = 0; x < slot.Width; x++)
                    if (!visual.Contains(x, y)) Assert.Equal(theme.Background.ToArgb(), bitmap.GetPixel(x, y).ToArgb());
                if (inset == 4 && Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_CAPTURE_DIR") is { Length: > 0 } capture)
                {
                    Directory.CreateDirectory(capture); bitmap.Save(Path.Combine(capture, $"header-{(modal ? "close" : "minimize")}-{painter.GetType().Name}-{dpi}-{odd}-{(pressed ? "pressed" : "hover")}.png"));
                }
            }
        }
    });
}
