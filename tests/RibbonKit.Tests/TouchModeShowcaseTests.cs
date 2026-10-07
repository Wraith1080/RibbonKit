using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class TouchModeShowcaseTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2007)]
    [InlineData(RibbonTheme.Office2010)]
    [InlineData(RibbonTheme.Office2013)]
    [InlineData(RibbonTheme.Office2019)]
    [InlineData(RibbonTheme.Office2024)]
    [InlineData(RibbonTheme.CrystalLight)]
    public void Touch_commands_are_consistent_and_the_body_fits_its_content(RibbonTheme theme) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        MainWindow? window = null;
        try
        {
            ThemeManager.Apply(application, theme);
            window = new MainWindow { Width = 1540, Height = 900, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false };
            // Realize the factory UI without reading/writing the user's saved layouts.
            var type = typeof(MainWindow);
            window.Loaded -= (RoutedEventHandler)type.GetMethod("OnWindowLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), window);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            window.Show();
            window.ThemeGallery.SelectedItem = window.ThemeGallery.Items.OfType<RibbonGalleryItem>()
                .Single(item => Equals(item.Tag, theme.ToString()));
            window.MainRibbon.SelectedTab = window.MainRibbon.Tabs.Single(tab => Equals(tab.Header, "View"));
            window.MainRibbon.Density = RibbonDensity.Compact; Layout(window);
            var tabs = Part<RibbonTabControl>(window.MainRibbon, "TabControlHost");
            var body = Part<Border>(tabs, "ContentHost");
            double compactIcon = Part<Image>(window.TouchModeToggle, "LargeImage").ActualWidth;
            window.TouchModeToggle.IsChecked = true; Layout(window);
            Assert.Equal(RibbonDensity.Touch, window.MainRibbon.Density);
            Assert.InRange(body.ActualHeight, window.TouchModeToggle.ActualHeight,
                window.TouchModeToggle.ActualHeight + 48);
            Assert.True(Part<Image>(window.TouchModeToggle, "LargeImage").ActualWidth > compactIcon);
            foreach (bool colored in new[] { false, true })
            {
                window.AccentedTitleBarToggle.IsChecked = colored; Layout(window);
                foreach (var command in new[] { window.DarkModeToggle, window.AccentedTitleBarToggle,
                    window.TouchModeToggle, window.GlassTreatmentToggle, window.MicaToggle, window.AcrylicToggle })
                    Assert.InRange(command.ActualHeight, window.TouchModeToggle.ActualHeight - 1,
                        window.TouchModeToggle.ActualHeight + 1);
                if (window.MainRibbon.ApplicationButtonShape == RibbonApplicationButtonShape.Tab)
                {
                    var file = Part<ToggleButton>(tabs, "PART_ApplicationButton");
                    var caption = Part<ContentPresenter>(file, "ApplicationCaption");
                    var header = window.MainRibbon.SelectedTab!;
                    var headerChrome = Part<Border>(header, "HeaderChrome");
                    var headerText = Part<ContentPresenter>(header, "HeaderText");
                    Assert.InRange(file.ActualHeight, headerChrome.ActualHeight - 1, headerChrome.ActualHeight + 1);
                    double labelCenter = caption.TranslatePoint(new Point(0, caption.ActualHeight / 2), tabs).Y;
                    double tabCenter = headerText.TranslatePoint(new Point(0, headerText.ActualHeight / 2), tabs).Y;
                    Assert.InRange(labelCenter, tabCenter - 1, tabCenter + 1);
                }
            }
            var viewport = Part<ScrollViewer>(window.ThemeGallery, "PART_ScrollViewer");
            var selectedTile = Assert.IsType<RibbonGalleryItem>(window.ThemeGallery.SelectedItem);
            Assert.True(selectedTile.ActualWidth <= viewport.ActualWidth + 1,
                $"{theme}: theme tile {selectedTile.ActualWidth}, viewport {viewport.ActualWidth}");
            Assert.InRange(selectedTile.ActualWidth, viewport.ViewportWidth - 1, viewport.ViewportWidth + 1);
            Assert.InRange(selectedTile.ActualHeight, viewport.ViewportHeight - 1, viewport.ViewportHeight + 1);
            var selectedBounds = selectedTile.TransformToAncestor(viewport).TransformBounds(new Rect(selectedTile.RenderSize));
            Assert.InRange(selectedBounds.Top, -1, 1);
            Assert.InRange(selectedBounds.Bottom, viewport.ViewportHeight - 1, viewport.ViewportHeight + 1);
            Assert.False(Part<RepeatButton>(window.ThemeGallery, "PART_LineUp").IsVisible);
            Assert.False(Part<RepeatButton>(window.ThemeGallery, "PART_LineDown").IsVisible);
            var opener = Part<ToggleButton>(window.ThemeGallery, "PART_ExpandToggle");
            Assert.True(opener.ActualWidth >= 44 && opener.ActualHeight >= 44);
            Save(window.MainRibbon, $"{theme}-showcase-view");
            window.MainRibbon.SelectedTab = window.MainRibbon.Tabs.Single(tab => Equals(tab.Header, "Home"));
            Layout(window);
            Assert.InRange(window.FontCompactSeparator.ActualHeight,
                window.SuperscriptButton.ActualHeight - 1, window.SuperscriptButton.ActualHeight + 1);
            var smallStack = Assert.IsType<StackPanel>(window.HomeTab.Groups[0].Items[1]);
            var paste = window.DemoSplitTarget;
            // Three individually rounded rows can differ by one physical pixel
            // each from a single rounded large button at fractional desktop DPI.
            Assert.InRange(paste.ActualHeight + paste.Margin.Top + paste.Margin.Bottom,
                smallStack.ActualHeight - 2, smallStack.ActualHeight + 2);
            Save(window.MainRibbon, $"{theme}-showcase-home");
            window.MainRibbon.EnterModal(window.PrintPreviewTab); Layout(window);
            var close = Part<Button>(tabs, "PART_ModalClose");
            var closeLabel = Part<ContentPresenter>(close, "Label");
            Assert.True(close.IsVisible && close.ActualWidth > 44);
            var labelBounds = closeLabel.TransformToAncestor(close).TransformBounds(new Rect(closeLabel.RenderSize));
            Assert.InRange(labelBounds.Left, 0, close.ActualWidth);
            Assert.InRange(labelBounds.Right, 0, close.ActualWidth);
            Assert.InRange(close.TranslatePoint(new Point(close.ActualWidth, 0), window).X, 0, window.ActualWidth);
            Save(window.MainRibbon, $"{theme}-showcase-modal");
            window.MainRibbon.ExitModal(); Layout(window);
        }
        finally
        {
            window?.Close();
            RibbonAnimation.GlobalLevel = animation;
            Sta.ResetApplication();
        }
    });

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void Save(FrameworkElement element, string name)
    {
        string? path = Environment.GetEnvironmentVariable("RIBBONKIT_TOUCH_DIAGNOSTICS");
        if (string.IsNullOrEmpty(path)) return;
        Directory.CreateDirectory(path);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var backing = new DrawingVisual();
        using (var context = backing.RenderOpen())
            context.DrawRectangle((Brush)element.FindResource("RibbonKit.Brushes.Window.Background"), null,
                new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        bitmap.Render(backing); bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(path, name + ".png")); encoder.Save(file);
    }
}
