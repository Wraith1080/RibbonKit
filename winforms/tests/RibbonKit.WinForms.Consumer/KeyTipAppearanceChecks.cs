using System.Drawing;
using System.IO;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Public rendering API only. No windows, Showcase resources, reflection or input.
internal static class KeyTipAppearanceChecks
{
    internal static int Run(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            foreach (bool office in new[] { false, true })
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
                using var font = new Font("Segoe UI", 14 * dpi / 96f, FontStyle.Bold, GraphicsUnit.Pixel);
                var theme = new RibbonTheme { Surface = Color.AliceBlue, Foreground = Color.DarkGreen, Border = Color.Purple };
                string name = $"{(office ? "Office" : "Neutral")}-{dpi}";
                Color background = office ? Color.FromArgb(255, 255, 225) : theme.Surface;
                Color foreground = office ? Color.Black : theme.Foreground;
                using var baseline = Draw(theme, "HH");
                Require(baseline.GetPixel(8, baseline.Height / 2).ToArgb() == background.ToArgb(), name + " default fill");
                Require(Count(baseline, foreground) > 4, name + " default text");
                Require(Count(baseline, theme.Border) > 4, name + " default border");

                var custom = theme with { KeyTipBackground = new RibbonFill(Color.MidnightBlue), KeyTipForeground = Color.White, KeyTipBorder = Color.Lime };
                using var actual = Draw(custom, "HH");
                Require(actual.GetPixel(8, actual.Height / 2).ToArgb() == Color.MidnightBlue.ToArgb(), name + " configured fill");
                Require(Count(actual, Color.White) > 4, name + " configured text");
                Require(Count(actual, Color.Lime) > 4, name + " configured border");
                var reset = custom with { KeyTipBackground = null, KeyTipForeground = Color.Empty, KeyTipBorder = Color.Empty };
                using var restored = Draw(reset, "HH");
                Require(Equal(baseline, restored), name + " reset restores the renderer default exactly");

                using var gradient = Draw(theme with { KeyTipBackground = new RibbonFill(Color.Red, Color.Blue), KeyTipBorder = Color.Transparent }, "");
                Require(gradient.GetPixel(8, 6).R > gradient.GetPixel(8, gradient.Height - 7).R, name + " gradient fill");
                Require(Count(gradient, theme.Border) == 0, name + " transparent outline");
                Require(theme.KeyTipBackground is null && theme.KeyTipForeground.IsEmpty && theme.KeyTipBorder.IsEmpty, name + " authored theme retained");
                baseline.Save(Path.Combine(directory, name + "-default.png")); actual.Save(Path.Combine(directory, name + "-custom.png"));
                log.Add("PASS: " + name + "; default palette, three overrides, reset, gradient and transparent border");

                Bitmap Draw(RibbonTheme palette, string keys)
                {
                    var image = new Bitmap(112 * dpi / 96, 40 * dpi / 96);
                    using var graphics = Graphics.FromImage(image); graphics.Clear(Color.Magenta);
                    renderer.DrawKeyTip(new(graphics, new(Point.Empty, image.Size), palette, font, dpi), keys);
                    return image;
                }
            }
            File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 0;
        }
        catch (Exception error) { log.Add("FAIL: " + error); File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 1; }
    }
    private static int Count(Bitmap image, Color color)
    {
        int count = 0;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) count++;
        return count;
    }
    private static bool Equal(Bitmap first, Bitmap second)
    {
        for (int y = 0; y < first.Height; y++) for (int x = 0; x < first.Width; x++)
            if (first.GetPixel(x, y) != second.GetPixel(x, y)) return false;
        return true;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
