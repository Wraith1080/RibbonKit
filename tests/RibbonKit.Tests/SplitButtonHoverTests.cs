using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class SplitButtonHoverTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2013, false)]
    [InlineData(RibbonTheme.Office2013, true)]
    [InlineData(RibbonTheme.Office2019, false)]
    [InlineData(RibbonTheme.Office2019, true)]
    [InlineData(RibbonTheme.Office2024, false)]
    [InlineData(RibbonTheme.Office2024, true)]
    public void Vertical_halves_paint_hover_across_the_same_width(RibbonTheme theme, bool dark) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, theme);
        ThemeManager.SetDarkMode(application, dark);
        var split = new RibbonSplitButton { Header = "Paste", Layout = RibbonSplitButtonLayout.Vertical,
            Width = 80, Height = 90 };
        var window = new Window { Content = split, Width = 200, Height = 200,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
            UseLayoutRounding = true };
        // A scoped solid wash isolates the painted extent from each theme's color.
        window.Resources["RibbonKit.Brushes.Control.SplitActiveHover"] = Brushes.Magenta;
        window.Resources["RibbonKit.Brushes.Control.HoverBorder"] = Brushes.Transparent;
        try
        {
            window.Show();
            VisualTreeHelper.SetRootDpi(window, new DpiScale(1.25, 1.25));
            Sta.Drain();
            var primary = Assert.IsType<Button>(split.Template.FindName("PART_Primary", split));
            var toggle = Assert.IsType<ToggleButton>(split.Template.FindName("PART_Toggle", split));
            var primaryChrome = Assert.IsType<Border>(primary.Template.FindName("Chrome", primary));
            var toggleChrome = Assert.IsType<Border>(toggle.Template.FindName("Chrome", toggle));
            Assert.Equal(primaryChrome.ActualWidth, toggleChrome.ActualWidth);
            SetHover(primary, true);
            SetHover(toggle, false);
            Sta.Drain();
            int primarySpan = PaintSpan(primaryChrome, $"split-hover-{theme}-{dark}-primary.png");
            SetHover(primary, false);
            SetHover(toggle, true);
            Sta.Drain();
            int toggleSpan = PaintSpan(toggleChrome, $"split-hover-{theme}-{dark}-arrow.png");
            Assert.True(primarySpan > 50);
            Assert.Equal(toggleSpan, primarySpan);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    private static void SetHover(UIElement target, bool value) => target.SetValue(
        (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey",
            BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!, value);

    private static int PaintSpan(Border chrome, string name)
    {
        const double scale = 1.25;
        int width = (int)Math.Ceiling((chrome.ActualWidth + 4) * scale);
        int height = (int)Math.Ceiling((chrome.ActualHeight + 4) * scale);
        var bitmap = new RenderTargetBitmap(width, height, 120, 120, PixelFormats.Pbgra32);
        bitmap.Render(chrome);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var output = File.Create(Path.Combine(AppContext.BaseDirectory, name))) encoder.Save(output);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);
        int row = (int)(chrome.ActualHeight * scale / 2);
        var painted = Enumerable.Range(0, width).Where(x =>
        {
            int index = (row * width + x) * 4;
            return pixels[index] > 200 && pixels[index + 1] < 80
                && pixels[index + 2] > 200 && pixels[index + 3] > 200;
        }).ToArray();
        Assert.NotEmpty(painted);
        return painted[^1] - painted[0] + 1;
    }
}
