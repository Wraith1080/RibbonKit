using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Public controls and library resources only; no Showcase integration.
internal static class NativeGalleryPopupPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var choices = Enumerable.Range(0, 9).Select(i => new Choice($"Style {i}", $"Section {i / 3 + 1}",
            i == 4 ? Brushes.Red : Brushes.SeaGreen)).ToArray();
        var view = new ListCollectionView(choices);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Section)));
        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetBinding(TextBlock.TextProperty, new Binding(nameof(Choice.Name)));
        var tile = new FrameworkElementFactory(typeof(Border));
        tile.SetValue(FrameworkElement.WidthProperty, 100d);
        tile.SetValue(FrameworkElement.HeightProperty, 40d);
        tile.SetBinding(Border.BackgroundProperty, new Binding(nameof(Choice.Color)));
        tile.AppendChild(label);
        var gallery = new InRibbonGallery { Width = 258, PopupWidth = 440, DropDownHeader = "Choose a style",
            ItemsSource = view, ItemTemplate = new DataTemplate { VisualTree = tile }, SelectedIndex = 4,
            IsSynchronizedWithCurrentItem = false };
        var ribbon = new Ribbon();
        var home = new RibbonTab { Header = "Gallery" };
        var group = new RibbonGroup { Header = "Styles", CanResize = false };
        group.Items.Add(gallery); home.Groups.Add(group); ribbon.Tabs.Add(home);
        var mode = new RibbonTab { Header = "Direction" };
        mode.Groups.Add(new RibbonGroup { Header = "Direction" }); ribbon.Tabs.Add(mode);
        ribbon.SelectedTab = home;
        var window = new Window { Content = ribbon, Width = 600, Height = 400, Left = 50, Top = 50,
            WindowStyle = WindowStyle.None, ShowActivated = false, ShowInTaskbar = false };
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        try
        {
            window.Show(); Layout(window);
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1.25, 2d })
            {
                ribbon.SelectedTab = mode; Layout(window);
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = flow;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                ribbon.SelectedTab = home; Layout(window);
                var strip = Part<ScrollViewer>(gallery, "PART_ScrollViewer");
                var presenter = Assert.IsType<ItemsPresenter>(strip.Content);
                var popup = Part<Popup>(gallery, "PART_Popup");
                var viewport = Part<ScrollViewer>(gallery, "PART_PopupScrollViewer");
                var context = $"{theme}/{dark}/{density}/{flow}/{scale}";
                BitmapSource? before = null;
                bool capture = !dark && scale == 1.25 && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight;
                if (capture) before = RenderStrip(window, gallery);
                // Check the first Opened notification, not just a later repaired layout.
                EventHandler opened = (_, _) => Assert.True(viewport.ActualHeight > 200, $"{context}: {viewport.RenderSize}");
                popup.Opened += opened;
                gallery.IsDropDownOpen = true;
                Assert.IsType<DrawingImage>(Assert.IsType<Image>(strip.Content).Source);
                if (capture)
                {
                    var during = RenderStrip(window, gallery);
                    Assert.True(RedPixels(during) >= RedPixels(before!) * .9, context);
                }
                Layout(window); popup.Opened -= opened;
                Assert.True(popup.IsOpen, context); Assert.Same(presenter, viewport.Content);
                Assert.Equal(4, gallery.SelectedIndex);
                Assert.Equal(3, Descendants<GroupItem>(presenter).Count());
                var popupHost = Part<Border>(gallery, "PART_PopupHost");
                var popupRoot = Assert.IsAssignableFrom<FrameworkElement>(PresentationSource.FromVisual(viewport)!.RootVisual);
                Rect rootCardBounds = popupHost.TransformToVisual(popupRoot).TransformBounds(new Rect(popupHost.RenderSize));
                Rect viewportBounds = viewport.TransformToVisual(popupHost).TransformBounds(new Rect(viewport.RenderSize));
                Assert.True(viewportBounds.Bottom <= popupHost.ActualHeight + 1,
                    $"{context}: viewport bounds {viewportBounds}, card {popupHost.RenderSize}, desired {popupHost.DesiredSize}, root {popupRoot.RenderSize}, viewport {viewport.RenderSize}, desired {viewport.DesiredSize}, presenter {presenter.RenderSize}, desired {presenter.DesiredSize}, visual parent={VisualTreeHelper.GetParent(presenter)}, scroll child={Part<ScrollContentPresenter>(viewport, "PART_ScrollContentPresenter").Content}, open={gallery.IsDropDownOpen}");
                Assert.True(popupHost.ActualHeight > 200,
                    $"{context}: card {popupHost.RenderSize}, viewport {viewport.RenderSize}, presenter {presenter.RenderSize}, desired {presenter.DesiredSize}, visual parent={VisualTreeHelper.GetParent(presenter)}, scroll child={Part<ScrollContentPresenter>(viewport, "PART_ScrollContentPresenter").Content}, open={gallery.IsDropDownOpen}");
                Assert.True(rootCardBounds.Bottom <= popupRoot.ActualHeight + 1,
                    $"{context}: card {popupHost.RenderSize}, root {popupRoot.RenderSize}, bounds {rootCardBounds}, viewport {viewport.RenderSize}");
                Rect source = ScreenBounds(gallery), card = ScreenBounds(popupHost);
                double leadingError = flow == FlowDirection.LeftToRight ? card.Left - source.Left : card.Right - source.Right;
                Assert.InRange(leadingError, -1, 1); Assert.InRange(card.Top - source.Top, -1, 1);
                if (capture)
                {
                    string name = $"{theme}-{density}-{flow}";
                    Save(before!, name + "-strip-before"); Save(RenderStrip(window, gallery), name + "-strip-open");
                    var root = Assert.IsAssignableFrom<FrameworkElement>(PresentationSource.FromVisual(viewport)!.RootVisual);
                    var dpi = VisualTreeHelper.GetDpi(root);
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(root.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                    bitmap.Render(root); Save(bitmap, name + "-popup");
                }
                gallery.IsDropDownOpen = false; Layout(window);
                Assert.Same(presenter, strip.Content);
                var selected = Assert.IsType<RibbonGalleryItem>(gallery.ItemContainerGenerator.ContainerFromIndex(4));
                var chrome = Part<Border>(selected, "Chrome");
                Rect bounds = chrome.TransformToVisual(strip).TransformBounds(new Rect(chrome.RenderSize));
                Assert.True(bounds.Top >= -1 && bounds.Bottom <= strip.ActualHeight + 1, $"{context}: {bounds}, strip {strip.RenderSize}");
            }
        }
        finally { gallery.IsDropDownOpen = false; window.Close(); RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private sealed record Choice(string Name, string Section, Brush Color);
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static void Layout(Window window) { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background); window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background); }
    private static Rect ScreenBounds(FrameworkElement element) => new(element.PointToScreen(default), element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)));
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static BitmapSource RenderStrip(Window window, FrameworkElement gallery)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window);
        Rect bounds = gallery.TransformToVisual(window).TransformBounds(new Rect(gallery.RenderSize));
        int left = (int)Math.Floor(bounds.Left * dpi.DpiScaleX), top = (int)Math.Floor(bounds.Top * dpi.DpiScaleY);
        return new CroppedBitmap(bitmap, new Int32Rect(left, top, (int)Math.Ceiling(bounds.Right * dpi.DpiScaleX) - left, (int)Math.Ceiling(bounds.Bottom * dpi.DpiScaleY) - top));
    }
    private static int RedPixels(BitmapSource bitmap)
    {
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        int count = 0;
        for (int i = 0; i < pixels.Length; i += 4) if (pixels[i + 2] > 180 && pixels[i + 1] < 80 && pixels[i] < 80 && pixels[i + 3] > 180) count++;
        return count;
    }
    private static void Save(BitmapSource bitmap, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_NATIVE_GALLERY_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
