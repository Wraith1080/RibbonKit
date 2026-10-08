using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class GalleryStripStateTests
{
    [Theory]
    [InlineData(false, false, FlowDirection.LeftToRight)]
    [InlineData(false, false, FlowDirection.RightToLeft)]
    [InlineData(false, true, FlowDirection.LeftToRight)]
    [InlineData(false, true, FlowDirection.RightToLeft)]
    [InlineData(true, false, FlowDirection.LeftToRight)]
    [InlineData(true, false, FlowDirection.RightToLeft)]
    [InlineData(true, true, FlowDirection.LeftToRight)]
    [InlineData(true, true, FlowDirection.RightToLeft)]
    public void Focused_gallery_keeps_the_viewed_row_through_native_and_qat_popups(bool qat, bool browse, FlowDirection flow) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.CrystalLight);
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
        var gallery = new InRibbonGallery { Header = "Styles", Width = 258, SelectedIndex = 7, DropDownHeader = "Choose a style" };
        for (int i = 0; i < 8; i++)
        {
            var content = new StackPanel { Width = 64 };
            content.Children.Add(new TextBlock { Text = "AaBbCc", FontSize = 14, HorizontalAlignment = HorizontalAlignment.Center });
            content.Children.Add(new TextBlock { Text = $"Style {i}", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center });
            if (i == (browse ? 4 : 7)) content.Background = Brushes.Red;
            gallery.Items.Add(new RibbonGalleryItem { Content = content });
        }
        var ribbon = new Ribbon();
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Styles", CanResize = false };
        group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(gallery);
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        var window = new Window { Content = ribbon, FlowDirection = flow, Width = 1000, Height = 400,
            Left = -10000, Top = -10000, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            var strip = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_ScrollViewer", gallery));
            var selected = Assert.IsType<RibbonGalleryItem>(gallery.ItemContainerGenerator.ContainerFromIndex(7));
            Assert.Same(selected, Keyboard.Focus(selected)); Layout();
            if (browse)
            {
                var browsed = Assert.IsType<RibbonGalleryItem>(gallery.ItemContainerGenerator.ContainerFromIndex(4));
                strip.ScrollToVerticalOffset(strip.VerticalOffset + browsed.TransformToVisual(strip).Transform(default).Y); Layout();
            }
            double offset = strip.VerticalOffset;
            int expected = RedPixels(window); Assert.True(expected > 200);
            var frames = new List<(bool Open, int Pixels, double Offset)>();
            bool sampling = false;
            EventHandler frame = (_, _) =>
            {
                if (sampling) frames.Add((qat ? proxy.IsDropDownOpen : gallery.IsDropDownOpen, RedPixels(window), strip.VerticalOffset));
            };
            CompositionTarget.Rendering += frame;
            try
            {
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    sampling = true;
                    if (qat) proxy.IsDropDownOpen = true; else gallery.IsDropDownOpen = true;
                    Pump(220);
                    Assert.True(RedPixels(window) >= expected * .9, $"open: expected {expected}, actual {RedPixels(window)}");
                    if (qat) proxy.IsDropDownOpen = false; else gallery.IsDropDownOpen = false;
                    Pump(300);
                    Assert.True(frames.All(f => f.Pixels >= expected * .9), $"cycle {cycle}, expected {expected}, frames: {string.Join(", ", frames)}");
                    Assert.InRange(strip.VerticalOffset - offset, -2, 2);
                    Assert.Equal(7, gallery.SelectedIndex);
                    Assert.Same(qat ? proxy.Template.FindName("PART_Toggle", proxy) : selected, Keyboard.FocusedElement);
                    var focused = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement);
                    var source = PresentationSource.FromVisual(focused)!;
                    focused.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                    focused.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape) { RoutedEvent = Keyboard.KeyDownEvent });
                    Pump(50);
                    Assert.Equal(7, gallery.SelectedIndex);
                    Assert.InRange(strip.VerticalOffset - offset, -2, 2);
                }
                Assert.NotEmpty(frames);
                if (qat)
                {
                    // A native popup taking over an open QAT borrow must retain the
                    // original viewed offset, rather than the empty scroller's zero.
                    proxy.IsDropDownOpen = true; Pump(220);
                    gallery.IsDropDownOpen = true; Pump(220);
                    Assert.False(proxy.IsDropDownOpen);
                    Assert.True(RedPixels(window) >= expected * .9);
                    gallery.IsDropDownOpen = false; Pump(250);
                    Assert.True(RedPixels(window) >= expected * .9);
                    Assert.InRange(strip.VerticalOffset - offset, -2, 2);
                    Assert.Equal(7, gallery.SelectedIndex);
                }
                sampling = false;
                // Returning focus after a different choice must follow that choice,
                // rather than reselecting the formerly focused strip tile.
                if (qat) proxy.IsDropDownOpen = true; else gallery.IsDropDownOpen = true;
                Pump(220);
                var pick = Assert.IsType<RibbonGalleryItem>(gallery.ItemContainerGenerator.ContainerFromIndex(2));
                Assert.Same(pick, Keyboard.Focus(pick)); pick.IsSelected = true;
                if (qat) proxy.IsDropDownOpen = false; else gallery.IsDropDownOpen = false;
                Pump(300);
                Assert.Equal(2, gallery.SelectedIndex);
                Assert.Same(qat ? proxy.Template.FindName("PART_Toggle", proxy) : pick, Keyboard.FocusedElement);
                var chrome = Assert.IsType<Border>(pick.Template.FindName("Chrome", pick));
                Rect bounds = chrome.TransformToVisual(strip).TransformBounds(new Rect(chrome.RenderSize));
                Assert.True(bounds.Top >= -1 && bounds.Bottom <= strip.ActualHeight + 1, $"{bounds}, strip {strip.RenderSize}");
            }
            finally { CompositionTarget.Rendering -= frame; }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    private static int RedPixels(Window window)
    {
        var dpi = VisualTreeHelper.GetDpi(window);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        int count = 0;
        for (int i = 0; i < pixels.Length; i += 4)
            if (pixels[i + 2] > 180 && pixels[i + 1] < 80 && pixels[i] < 80 && pixels[i + 3] > 180) count++;
        return count;
    }

    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
}
