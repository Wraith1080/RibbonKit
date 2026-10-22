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

namespace RibbonKit.Portability.Tests;

// Authored underlays prove WPF paint continuity without claiming native DWM activation.
internal static class Office2024BackdropChecks
{
    internal static void VerifyCaptionMotion(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        bool respect = RibbonAnimation.RespectSystemReduceMotion;
        var ribbon = new Ribbon(); ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var window = new RibbonWindow { Content = ribbon, Width = 800, Height = 260,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
            RibbonAnimation.RespectSystemReduceMotion = false;
            window.Show(); window.UpdateLayout();
            foreach (var level in new[] { RibbonAnimationLevel.Subtle, RibbonAnimationLevel.Expressive })
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            {
                RibbonAnimation.GlobalLevel = level;
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
                // Isolate caption changes from the preceding real palette animation.
                tabs.BeginAnimation(UIElement.OpacityProperty, null);
                foreach (bool colored in new[] { true, false, true, false })
                {
                    ThemeManager.SetAccentedTitleBar(application, colored);
                    Assert.False(tabs.HasAnimatedProperties, $"{theme}/{dark}/{level}: caption coloring started a ribbon animation.");
                    Assert.Equal(1, tabs.Opacity);
                    Assert.Equal(colored, ThemeManager.IsAccentedTitleBar);
                }
                ThemeManager.SetAccent(application, Colors.Purple);
                Assert.True(tabs.HasAnimatedProperties); // Palette motion remains available.
                tabs.BeginAnimation(UIElement.OpacityProperty, null);
            }
        }
        finally
        {
            window.Close(); ThemeManager.ClearAccent(application);
            ThemeManager.SetAccentedTitleBar(application, false); ThemeManager.SetDarkMode(application, false);
            RibbonAnimation.GlobalLevel = motion; RibbonAnimation.RespectSystemReduceMotion = respect;
        }
    }

    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var ribbon = new Ribbon { VerticalAlignment = VerticalAlignment.Top };
        var home = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Clipboard" };
        group.Items.Add(new RibbonButton { Header = "Paste" }); home.Groups.Add(group);
        ribbon.Tabs.Add(home); ribbon.Tabs.Add(new RibbonTab { Header = "View" }); ribbon.SelectedTab = home;
        var scene = new Grid(); scene.Children.Add(ribbon);
        var window = new RibbonWindow { Content = scene, Width = 800, Height = 280,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        ResourceDictionary? overlay = null;
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show(); Layout();
            foreach (var theme in new[] { RibbonTheme.Office2024, RibbonTheme.Office2024Flat })
            foreach (bool dark in new[] { false, true })
            {
                RemoveOverlay();
                ThemeManager.SetTitleBarBackdrop(application, false);
                ThemeManager.SetAccentedTitleBar(application, false);
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                var resting = Assert.IsType<SolidColorBrush>(ribbon.FindResource("RibbonKit.Brushes.Ribbon.Background")).Color;
                ThemeManager.SetTitleBarBackdrop(application, true);
                foreach (bool glass in new[] { false, true })
                foreach (Color underlay in new[] { Color.FromRgb(0x40, 0x6E, 0x9E), Color.FromRgb(0x9E, 0x66, 0x40) })
                {
                    scene.Background = new SolidColorBrush(underlay);
                    Apply(false, glass, dark); Layout();
                    var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
                    var header = Part<Grid>(tabs, "PART_TabHeaderHost");
                    var body = Part<Border>(tabs, "ContentHost");
                    Point headerPoint = header.TranslatePoint(new Point(header.ActualWidth * 0.65, header.ActualHeight / 2), scene);
                    Point bodyPoint = body.TranslatePoint(new Point(body.ActualWidth * 0.65, body.ActualHeight / 2), scene);
                    var before = Render(scene);
                    Assert.Equal(underlay, Pixel(before, headerPoint));
                    foreach (bool colored in new[] { true, false })
                    {
                        Apply(colored, glass, dark); Layout();
                        Assert.Equal(0, Alpha(ribbon.Background));
                        Assert.Equal(0, Alpha(header.Background));
                        var after = Render(scene);
                        Assert.Equal(Pixel(before, headerPoint), Pixel(after, headerPoint));
                        Assert.Equal(Pixel(before, bodyPoint), Pixel(after, bodyPoint));
                        if (colored)
                        {
                            var title = Assert.IsType<SolidColorBrush>(window.FindResource("RibbonKit.Brushes.TitleBar.Background"));
                            Assert.Equal(Assert.IsType<SolidColorBrush>(window.FindResource("RibbonKit.Brushes.Accent")).Color, title.Color);
                        }
                        if (dark && underlay.B == 0x9E) Save(after, $"{theme}-glass-{glass}-colored-{colored}");
                    }
                }
                RemoveOverlay();
                ThemeManager.SetTitleBarBackdrop(application, false); Layout();
                Assert.Equal(resting, Assert.IsType<SolidColorBrush>(ribbon.Background).Color);
            }
            // Application-level backdrop policy cannot erase an explicit consumer resource.
            window.Resources["RibbonKit.Brushes.Ribbon.Background"] = Brushes.Plum;
            ThemeManager.SetTitleBarBackdrop(application, true);
            ThemeManager.SetAccentedTitleBar(application, true); Layout();
            Assert.Same(Brushes.Plum, ribbon.Background);
        }
        finally
        {
            RemoveOverlay(); window.Close();
            ThemeManager.SetTitleBarBackdrop(application, false);
            ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.SetDarkMode(application, false); RibbonAnimation.GlobalLevel = motion;
        }
        void Apply(bool colored, bool glass, bool dark)
        {
            RemoveOverlay(); ThemeManager.SetAccentedTitleBar(application, colored);
            if (glass) { overlay = ThemeManager.CreateGlassOverlay(window, dark); window.Resources.MergedDictionaries.Add(overlay); }
        }
        void RemoveOverlay()
        {
            if (overlay is not null) window.Resources.MergedDictionaries.Remove(overlay);
            overlay = null;
        }
        void Layout()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }

    private static double Alpha(Brush brush) => Assert.IsType<SolidColorBrush>(brush).Color.A * brush.Opacity;
    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static RenderTargetBitmap Render(FrameworkElement element)
    {
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element); return bitmap;
    }
    private static Color Pixel(BitmapSource bitmap, Point point)
    {
        var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect((int)(point.X * bitmap.DpiX / 96), (int)(point.Y * bitmap.DpiY / 96), 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }
    private static void Save(BitmapSource bitmap, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_2024_BACKDROP_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
