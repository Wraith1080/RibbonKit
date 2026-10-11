using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class QuickAccessBackdropPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null, Geometry.Parse("M4,2 L12,2 L12,15 L4,15 Z")));
        var split = new RibbonSplitButton { Icon = icon, Size = RibbonControlSize.Small };
        var dropdown = new RibbonDropDownButton { Icon = icon, Size = RibbonControlSize.Small };
        var button = new RibbonButton { Icon = icon, Size = RibbonControlSize.Small };
        var toggle = new RibbonToggleButton { Icon = icon, Size = RibbonControlSize.Small };
        var ribbon = new Ribbon(); ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        foreach (var command in new Control[] { split, dropdown, button, toggle }) ribbon.QuickAccessItems.Add(command);
        var window = new RibbonWindow { Content = ribbon, Width = 800, Height = 330, Left = -10000,
            Top = -10000, ShowActivated = false, ShowInTaskbar = false, UseLayoutRounding = true };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetAccentedTitleBar(application, false); ThemeManager.SetTitleBarBackdrop(application, false);
            window.Show(); Layout(window);
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.SetDarkMode(application, dark);
                window.FlowDirection = dark ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(dark ? 2 : 1.25, dark ? 2 : 1.25));
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; Layout(window);
                var primary = Part<Button>(split, "PART_Primary");
                var arrow = Part<ToggleButton>(split, "PART_Toggle");
                SetHover(arrow, true); Layout(window);
                var band = Assert.IsType<SolidColorBrush>(ribbon.FindResource("RibbonKit.Brushes.TitleBar.Background"));
                var companion = Assert.IsType<SolidColorBrush>(Part<Border>(primary, "Chrome").Background);
                int companionContrast = Math.Abs(companion.Color.R - band.Color.R);
                var activeNeutral = Assert.IsType<SolidColorBrush>(split.FindResource("RibbonKit.Brushes.Control.SplitActiveHover"));
                Assert.True(companionContrast >= 6);
                Assert.True(companionContrast < Math.Abs(activeNeutral.Color.R - band.Color.R));
                Save(window, band, $"office2024-neutral-{dark}");
                SetHover(arrow, false);
                ThemeManager.SetTitleBarBackdrop(application, true); Layout(window);
                foreach (var position in new[] { RibbonQuickAccessPosition.TitleBar, RibbonQuickAccessPosition.TabRow })
                {
                    ribbon.QuickAccessPosition = position; Layout(window);
                    primary = Part<Button>(split, "PART_Primary"); arrow = Part<ToggleButton>(split, "PART_Toggle");
                    SetHover(arrow, true); Layout(window);
                    var active = Part<Border>(arrow, "Chrome").Background;
                    var quiet = Part<Border>(primary, "Chrome").Background;
                    AssertWash(active, dark); AssertWash(quiet, dark);
                    Assert.InRange(Alpha(quiet), Alpha(active) * 0.45, Alpha(active) * 0.55);
                    Assert.False(Ribbon.GetQatOnColoredSurface(split));
                    Assert.Equal(Visibility.Visible, Part<Image>(split, "SmallImage").Visibility);
                    // Two underlying material colors must still show through the real chrome.
                    var warm = Color.FromRgb(0xDE, 0xA9, 0x91);
                    var cool = Color.FromRgb(0x91, 0xAD, 0xDE);
                    var warmPixel = Pixel(Part<Border>(primary, "Chrome"), warm);
                    var coolPixel = Pixel(Part<Border>(primary, "Chrome"), cool);
                    Assert.True(warmPixel.R > coolPixel.R + 50 && coolPixel.B > warmPixel.B + 50);
                    if (position == RibbonQuickAccessPosition.TitleBar)
                        Save(window, new LinearGradientBrush(warm, cool, 0), $"office2024-backdrop-{dark}");
                    SetHover(arrow, false);
                    var dropdownToggle = Part<ToggleButton>(dropdown, "PART_Toggle");
                    SetHover(dropdownToggle, true); Layout(window);
                    Assert.Same(active, Part<Border>(dropdownToggle, "Chrome").Background);
                    SetHover(dropdownToggle, false);
                    // The existing plain/toggle fade layers resolve the same surface wash;
                    // native pointer input remains a live check, separate from brush routing.
                    Assert.Same(active, Part<Border>(button, "HoverWash").Background);
                    Assert.Same(active, Part<Border>(toggle, "HoverWash").Background);
                    toggle.IsChecked = true; Layout(window);
                    AssertWash(Part<Border>(toggle, "CheckWash").Background, dark);
                    Assert.Equal(1d, Part<Border>(toggle, "CheckWash").Opacity);
                    toggle.IsChecked = false;
                    SetHover(arrow, false); SetHover(button, false); SetHover(toggle, false); SetHover(dropdownToggle, false);
                    // Local paint wins over the owned merged palette, and unrelated dictionaries survive.
                    var authored = new ResourceDictionary { ["Consumer.Marker"] = "retained" };
                    dropdown.Resources.MergedDictionaries.Add(authored);
                    dropdown.Resources["RibbonKit.Brushes.Control.HoverBackground"] = Brushes.Magenta;
                    SetHover(dropdownToggle, true); Layout(window);
                    Assert.Same(Brushes.Magenta, Part<Border>(dropdownToggle, "Chrome").Background);
                    SetHover(dropdownToggle, false); dropdown.Resources.Remove("RibbonKit.Brushes.Control.HoverBackground");
                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; Layout(window);
                    Assert.Single(dropdown.Resources.MergedDictionaries);
                    Assert.Same(authored, dropdown.Resources.MergedDictionaries[0]);
                    Assert.Equal(255d, Alpha(split.FindResource("RibbonKit.Brushes.Control.SplitActiveHover") as Brush));
                    dropdown.Resources.MergedDictionaries.Remove(authored);
                }
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
                ThemeManager.SetAccentedTitleBar(application, true); Layout(window);
                Assert.Empty(split.Resources.MergedDictionaries);
                Assert.True(Ribbon.GetQatOnColoredSurface(split));
                ThemeManager.SetAccentedTitleBar(application, false); Layout(window);
                AssertWash(split.FindResource("RibbonKit.Brushes.Control.SplitActiveHover") as Brush, dark);
                ThemeManager.SetTitleBarBackdrop(application, false); Layout(window);
                Assert.Empty(split.Resources.MergedDictionaries);
                Assert.Equal(255d, Alpha(split.FindResource("RibbonKit.Brushes.Control.SplitActiveHover") as Brush));
            }
            ThemeManager.SetTitleBarBackdrop(application, true);
            foreach (var theme in new[] { RibbonTheme.Office2019, RibbonTheme.Office2010, RibbonTheme.CrystalLight })
            {
                ThemeManager.Apply(application, theme); Layout(window);
                Assert.Empty(split.Resources.MergedDictionaries);
            }
            // A nearer non-2024 palette must also suppress the global 2024 backdrop policy.
            var palette = ThemeManager.CreatePalette(RibbonTheme.CrystalLight);
            window.Resources.MergedDictionaries.Add(palette);
            ThemeManager.Apply(application, RibbonTheme.Office2024); Layout(window);
            Assert.Empty(split.Resources.MergedDictionaries);
            window.Resources.MergedDictionaries.Remove(palette); Layout(window);
            Assert.Single(split.Resources.MergedDictionaries);
            ribbon.QuickAccessItems.Remove(split); Layout(window);
            Assert.Empty(split.Resources.MergedDictionaries);
        }
        finally
        {
            window.Close(); ThemeManager.SetTitleBarBackdrop(application, false);
            ThemeManager.SetAccentedTitleBar(application, false); ThemeManager.SetDarkMode(application, false);
            RibbonAnimation.GlobalLevel = animation;
        }
    }

    private static void AssertWash(Brush? brush, bool dark)
    {
        var solid = Assert.IsType<SolidColorBrush>(brush);
        Assert.InRange(Alpha(solid), 5d, 80d);
        Assert.Equal(dark ? (byte)255 : (byte)0, solid.Color.R);
        Assert.Equal(solid.Color.R, solid.Color.G); Assert.Equal(solid.Color.G, solid.Color.B);
    }
    private static double Alpha(Brush? brush) => Assert.IsType<SolidColorBrush>(brush).Color.A * brush!.Opacity;
    private static void Save(RibbonWindow window, Brush underlay, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_QAT_BACKDROP_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        var host = Assert.IsAssignableFrom<FrameworkElement>(window.TitleBarContent);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(underlay, null, new Rect(0, 0, host.ActualWidth + 16, 48));
            drawing.DrawRectangle(new VisualBrush(host), null, new Rect(8, 8, host.ActualWidth, host.ActualHeight));
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(host.ActualWidth + 16), 48, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(directory);
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
    private static Color Pixel(Border chrome, Color underlay)
    {
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(new SolidColorBrush(underlay), null, new Rect(chrome.RenderSize));
            drawing.DrawRectangle(new VisualBrush(chrome), null, new Rect(chrome.RenderSize));
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(chrome.ActualWidth), (int)Math.Ceiling(chrome.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); var pixel = new byte[4]; bitmap.CopyPixels(new Int32Rect(4, 5, 1, 1), pixel, 4, 0);
        return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
    }
    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static void SetHover(UIElement target, bool value) => target.SetValue(
        (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!, value);
    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
