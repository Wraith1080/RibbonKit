using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonShadowRenderingTests
{
    [Theory]
    [InlineData(1d, false, FlowDirection.LeftToRight)]
    [InlineData(1d, true, FlowDirection.RightToLeft)]
    [InlineData(1.5d, true, FlowDirection.LeftToRight)]
    [InlineData(2d, false, FlowDirection.RightToLeft)]
    public void Decorative_shadows_do_not_resample_foreground_pixels(double scale, bool dark, FlowDirection direction) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        ThemeManager.SetDarkMode(application, dark);
        var level = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
        ribbon.Resources["RibbonKit.Brushes.Ribbon.BodyBackground"] = Brushes.White;
        ribbon.Resources["RibbonKit.Brushes.QatExtender.Background"] = Brushes.White;
        var command = new RibbonButton { Header = "Sharp text", Size = RibbonControlSize.Large,
            Icon = new DrawingImage(new GeometryDrawing(Brushes.Black, null, new RectangleGeometry(new Rect(1, 1, 14, 14)))) };
        var group = new RibbonGroup { Header = "Commands" }; group.Items.Add(command);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.AddToQuickAccess(command);
        var root = new Grid { Width = 760, Height = 270, Background = Brushes.White,
            UseLayoutRounding = true, SnapsToDevicePixels = true, FlowDirection = direction };
        TextOptions.SetTextFormattingMode(root, TextFormattingMode.Display);
        TextOptions.SetTextHintingMode(root, TextHintingMode.Fixed);
        TextOptions.SetTextRenderingMode(root, TextRenderingMode.Grayscale);
        root.Children.Add(ribbon); ribbon.VerticalAlignment = VerticalAlignment.Top;
        var window = new Window { Width = 1, Height = 1, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            window.UpdateLayout(); Drain(); window.UpdateLayout();
            // Match the static snapshot fixture: capture a detached, arranged root
            // so native window content offsets do not shift the sampled rectangles.
            VisualTreeHelper.SetRootDpi(root, new DpiScale(scale, scale));
            root.Measure(new Size(760, 270)); root.Arrange(new Rect(0, 0, 760, 270)); root.UpdateLayout();
            var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
            var body = (Border)tabs.Template.FindName("ContentHost", tabs);
            var drawer = (Border)ribbon.Template.FindName("QatBelowHost", ribbon);
            Assert.Null(body.Child); Assert.Null(drawer.Child);
            var effects = new[] { body.Effect, drawer.Effect };
            Assert.All(effects, effect => Assert.NotNull(effect));
            BitmapSource withShadows = Capture(root, scale);
            Save(withShadows, $"foreground-{scale * 100:0}-{dark}-{direction}");
            foreach (var (chrome, source) in new[] { (body, command), (drawer, (RibbonButton)ribbon.QuickAccessItems[0]) })
            {
                var effect = chrome.Effect;
                chrome.SetCurrentValue(UIElement.EffectProperty, null);
                BitmapSource withoutShadow = Capture(root, scale);
                chrome.SetCurrentValue(UIElement.EffectProperty, effect);
                Save(withoutShadow, $"without-{chrome.Name}-{scale * 100:0}-{dark}-{direction}");
                var foreground = Descendants(source).OfType<TextBlock>().Where(text => text.Text == "Sharp text" && text.ActualWidth > 0 && text.Visibility == Visibility.Visible)
                    .Cast<FrameworkElement>().Concat(Descendants(source).OfType<Image>().Where(image => image.Source is not null && image.ActualWidth > 0 && image.Visibility == Visibility.Visible)).ToArray();
                Assert.NotEmpty(foreground);
                foreach (var element in foreground)
                {
                    Rect bounds = element.TransformToVisual(root).TransformBounds(new Rect(element.RenderSize));
                    var rectangle = new Int32Rect((int)Math.Ceiling(bounds.Left * scale), (int)Math.Ceiling(bounds.Top * scale),
                        Math.Max(1, (int)Math.Floor(bounds.Width * scale) - 1), Math.Max(1, (int)Math.Floor(bounds.Height * scale) - 1));
                    byte[] actual = new byte[rectangle.Width * rectangle.Height * 4], expected = new byte[actual.Length];
                    withShadows.CopyPixels(rectangle, actual, rectangle.Width * 4, 0);
                    withoutShadow.CopyPixels(rectangle, expected, rectangle.Width * 4, 0);
                    Assert.True(expected.SequenceEqual(actual), $"Shadow changed {element.GetType().Name} foreground at {bounds}.");
                }
                Assert.NotEqual(Pixels(withShadows), Pixels(withoutShadow)); // The decorative halo still exists.
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = level; Sta.ResetApplication(); }
    });

    private static BitmapSource Capture(FrameworkElement root, double scale)
    {
        var bitmap = new RenderTargetBitmap((int)(root.ActualWidth * scale), (int)(root.ActualHeight * scale),
            96 * scale, 96 * scale, PixelFormats.Pbgra32); bitmap.Render(root); return bitmap;
    }
    private static byte[] Pixels(BitmapSource bitmap)
    { byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0); return pixels; }
    private static void Save(BitmapSource bitmap, string name)
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestResults", "shadow-fix"); Directory.CreateDirectory(folder);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(file);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        { var child = VisualTreeHelper.GetChild(parent, i); yield return child; foreach (var descendant in Descendants(child)) yield return descendant; }
    }
    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
}
