using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;
using Xunit.Abstractions;

namespace RibbonKit.Tests;

public sealed class ThemeGalleryLayoutTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(1.25)]
    [InlineData(2)]
    public void Theme_tiles_fit_the_strip_at_both_dpi_scales(double scale) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var oldMotion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var main = new MainWindow();
        typeof(MainWindow).GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(main, true);
        var theme = main.ThemeGallery;
        var styles = main.StylesGallery;
        main.MainRibbon.Tabs.SelectMany(tab => tab.Groups).Single(group => group.Items.Contains(theme)).Items.Remove(theme);
        main.HomeTab.Groups.Single(group => group.Items.Contains(styles)).Items.Remove(styles);
        var surface = new Grid { Width = 460, Height = 90, Background = Brushes.White };
        surface.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        surface.ColumnDefinitions.Add(new ColumnDefinition());
        theme.HorizontalAlignment = styles.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetColumn(styles, 1);
        surface.Children.Add(theme);
        surface.Children.Add(styles);
        var window = new Window { Content = surface, Width = 500, Height = 140,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
            Layout();
            Assert.Equal(scale, VisualTreeHelper.GetDpi(theme).DpiScaleY, 5);
            foreach (RibbonGalleryItem tile in theme.Items)
            {
                // Picking in the expanded gallery reveals the selected row when it closes.
                theme.IsDropDownOpen = true;
                Layout();
                theme.SelectedItem = tile;
                Layout();
                Assert.Same(tile, theme.SelectedItem);
                Assert.True(Assert.IsType<ScrollViewer>(theme.Template.FindName("PART_PopupScrollViewer", theme)).ViewportHeight >= tile.ActualHeight);
                theme.IsDropDownOpen = false;
                Layout();
                Assert.Same(tile, theme.SelectedItem);
                Assert.Equal(scale, VisualTreeHelper.GetDpi(tile).DpiScaleY, 5);
                if (Equals(tile.Tag, "Office2024")) SavePreview(surface, scale);
                var viewer = Assert.IsType<ScrollViewer>(theme.Template.FindName("PART_ScrollViewer", theme));
                var styleViewer = Assert.IsType<ScrollViewer>(styles.Template.FindName("PART_ScrollViewer", styles));
                var styleTile = Assert.IsType<RibbonGalleryItem>(styles.Items[0]);
                output.WriteLine($"scale={scale}, theme={tile.Tag}, theme tile={tile.ActualHeight}, viewport={viewer.ViewportHeight}, style tile={styleTile.ActualHeight}, viewport={styleViewer.ViewportHeight}");
                Assert.True(tile.ActualHeight <= viewer.ViewportHeight + 0.01,
                    $"{tile.Tag}: tile height {tile.ActualHeight} exceeds viewport {viewer.ViewportHeight} at {scale * 100}%.");
                var chrome = Assert.IsType<Border>(tile.Template.FindName("Chrome", tile));
                var bounds = chrome.TransformToVisual(viewer).TransformBounds(new Rect(chrome.RenderSize));
                Assert.True(bounds.Top >= -0.01 && bounds.Bottom <= viewer.ActualHeight + 0.01,
                    $"{tile.Tag}: selected border {bounds} does not fit viewport {viewer.RenderSize}.");
                Assert.True(styleTile.ActualHeight <= styleViewer.ViewportHeight + 0.01);
                var content = Assert.IsType<StackPanel>(tile.Content);
                var icon = Assert.IsType<Image>(content.Children[0]);
                var text = Assert.IsType<TextBlock>(content.Children[1]);
                var iconBounds = icon.TransformToVisual(content).TransformBounds(new Rect(icon.RenderSize));
                var textBounds = text.TransformToVisual(content).TransformBounds(new Rect(text.RenderSize));
                Assert.True(iconBounds.Right <= textBounds.Left);
                Assert.True(textBounds.Right <= content.ActualWidth + 0.01);
            }
        }
        finally
        {
            window.Close();
            main.Close();
            RibbonAnimation.GlobalLevel = oldMotion;
            Sta.ResetApplication();
        }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    private void SavePreview(FrameworkElement surface, double scale)
    {
        var image = new RenderTargetBitmap((int)Math.Ceiling(surface.ActualWidth * scale),
            (int)Math.Ceiling(surface.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        image.Render(surface);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "RibbonKit.sln"))) root = root.Parent;
        var directory = Path.Combine(root!.FullName, "tests", "RibbonKit.Tests", "TestResults", "theme-gallery-diagnostics");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"theme-and-styles-{scale * 100}-{DateTime.UtcNow:HHmmssfff}.png");
        using var stream = File.Create(path);
        encoder.Save(stream);
        output.WriteLine(path);
    }
}
