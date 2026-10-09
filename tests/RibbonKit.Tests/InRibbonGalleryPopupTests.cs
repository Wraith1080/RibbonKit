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
using RibbonKit.Showcase;
using Xunit;

namespace RibbonKit.Tests;

public sealed class InRibbonGalleryPopupTests
{
    [Theory]
    [InlineData(FlowDirection.LeftToRight, false, RibbonAnimationLevel.None, RibbonDensity.Compact)]
    [InlineData(FlowDirection.RightToLeft, false, RibbonAnimationLevel.None, RibbonDensity.Compact)]
    [InlineData(FlowDirection.LeftToRight, true, RibbonAnimationLevel.None, RibbonDensity.Compact)]
    [InlineData(FlowDirection.RightToLeft, true, RibbonAnimationLevel.None, RibbonDensity.Compact)]
    [InlineData(FlowDirection.LeftToRight, false, RibbonAnimationLevel.Subtle, RibbonDensity.Compact)]
    [InlineData(FlowDirection.RightToLeft, false, RibbonAnimationLevel.Subtle, RibbonDensity.Compact)]
    [InlineData(FlowDirection.LeftToRight, true, RibbonAnimationLevel.Subtle, RibbonDensity.Compact)]
    [InlineData(FlowDirection.RightToLeft, true, RibbonAnimationLevel.Subtle, RibbonDensity.Compact)]
    [InlineData(FlowDirection.LeftToRight, false, RibbonAnimationLevel.Subtle, RibbonDensity.Touch)]
    [InlineData(FlowDirection.RightToLeft, false, RibbonAnimationLevel.Subtle, RibbonDensity.Touch)]
    [InlineData(FlowDirection.LeftToRight, true, RibbonAnimationLevel.Subtle, RibbonDensity.Touch)]
    [InlineData(FlowDirection.RightToLeft, true, RibbonAnimationLevel.Subtle, RibbonDensity.Touch)]
    public void Dismiss_without_a_pick_preserves_the_first_uncovered_strip_frame(FlowDirection flow, bool browseAway, RibbonAnimationLevel level, RibbonDensity density) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(); ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = level;
        var gallery = new InRibbonGallery { Width = 258, SelectedIndex = 7, DropDownHeader = "Choose a style" };
        Ribbon.SetDensity(gallery, density);
        for (int i = 0; i < 9; i++) gallery.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 64, Height = 40, Background = i == (browseAway ? 4 : 7) ? Brushes.Red : Brushes.SeaGreen } });
        var window = new Window { Content = gallery, FlowDirection = flow, Width = 1000, Height = 400, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            var strip = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_ScrollViewer", gallery));
            if (browseAway)
            {
                var browsed = Assert.IsType<RibbonGalleryItem>(gallery.ItemContainerGenerator.ContainerFromIndex(4));
                strip.ScrollToVerticalOffset(strip.VerticalOffset + browsed.TransformToVisual(strip).Transform(default).Y); Layout();
            }
            double offset = strip.VerticalOffset;
            int before = RedPixels(Render(window)); Assert.True(before > 200);
            gallery.IsDropDownOpen = true; Layout();
            var frames = new List<(int Pixels, double Offset)>();
            var popup = Assert.IsType<Popup>(gallery.Template.FindName("PART_Popup", gallery));
            EventHandler frame = (_, _) => { if (!popup.IsOpen) frames.Add((RedPixels(Render(window)), strip.VerticalOffset)); };
            CompositionTarget.Rendering += frame;
            try
            {
                gallery.IsDropDownOpen = false;
                if (!popup.IsOpen) Assert.True(RedPixels(Render(window)) >= before * .9, $"immediate: {strip.VerticalOffset}, expected {offset}");
                Sta.Drain(DispatcherPriority.Render);
                if (!popup.IsOpen) Assert.True(RedPixels(Render(window)) >= before * .9, $"render: {strip.VerticalOffset}, expected {offset}");
                Layout();
                PumpFrames(300);
                Assert.False(popup.IsOpen);
                Assert.NotEmpty(frames);
                Assert.True(frames.All(f => f.Pixels >= before * .9), $"frames: {string.Join(", ", frames)}");
                Assert.True(RedPixels(Render(window)) >= before * .9, $"settled: {strip.VerticalOffset}, expected {offset}");
                Assert.InRange(strip.VerticalOffset - offset, -2, 2); Assert.Equal(7, gallery.SelectedIndex);
            }
            finally { CompositionTarget.Rendering -= frame; }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    private static void PumpFrames(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Reopening_and_qat_handoff_do_not_lose_the_pending_strip_return(FlowDirection flow) => Sta.Run(() =>
    {
        Sta.UseApplication();
        var gallery = new InRibbonGallery { Header = "Styles", Width = 258, SelectedIndex = 7 };
        for (int i = 0; i < 9; i++) gallery.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 64, Height = 40, Background = i == 7 ? Brushes.Red : Brushes.SeaGreen } });
        var ribbon = new Ribbon(); var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Styles", CanResize = false }; group.Items.Add(gallery);
        tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab; ribbon.AddToQuickAccess(gallery);
        var window = new Window { Content = ribbon, FlowDirection = flow, Width = 1000, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            var strip = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_ScrollViewer", gallery));
            var presenter = Assert.IsType<ItemsPresenter>(strip.Content);
            int before = RedPixels(Render(window)); Assert.True(before > 200);
            gallery.IsDropDownOpen = true; Layout();
            gallery.IsDropDownOpen = false; gallery.IsDropDownOpen = true; Layout();
            var viewport = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_PopupScrollViewer", gallery));
            Assert.Same(presenter, viewport.Content);
            gallery.IsDropDownOpen = false; Layout();
            Assert.Same(presenter, strip.Content); Assert.True(RedPixels(Render(window)) >= before * .9);

            gallery.IsDropDownOpen = true; Layout();
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            proxy.IsDropDownOpen = true; Layout();
            Assert.False(gallery.IsDropDownOpen); Assert.False(Assert.IsType<Popup>(gallery.Template.FindName("PART_Popup", gallery)).IsOpen);
            Assert.Same(presenter, Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(proxy.Items)).Content);
            Assert.True(RedPixels(Render(window)) >= before * .9);
            proxy.IsDropDownOpen = false; Layout();
            Assert.Same(presenter, strip.Content); Assert.Equal(7, gallery.SelectedIndex);
        }
        finally { window.Close(); Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void A_different_pick_still_reveals_its_row(FlowDirection flow) => Sta.Run(() =>
    {
        Sta.UseApplication();
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
        var gallery = new InRibbonGallery { Width = 258, SelectedIndex = 7 };
        for (int i = 0; i < 9; i++) gallery.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 64, Height = 40, Background = Brushes.SeaGreen } });
        var window = new Window { Content = gallery, FlowDirection = flow, Width = 900, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(); gallery.IsDropDownOpen = true; Layout();
            gallery.SelectedIndex = 4; gallery.IsDropDownOpen = false; Layout(); PumpFrames(300);
            Assert.Equal(4, gallery.SelectedIndex);
            var strip = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_ScrollViewer", gallery));
            var chosen = Assert.IsType<RibbonGalleryItem>(gallery.ItemContainerGenerator.ContainerFromIndex(4));
            var chrome = Assert.IsType<Border>(chosen.Template.FindName("Chrome", chosen));
            Rect bounds = chrome.TransformToVisual(strip).TransformBounds(new Rect(chrome.RenderSize));
            Assert.True(bounds.Top >= -1 && bounds.Bottom <= strip.ActualHeight + 1, $"{bounds}, strip {strip.RenderSize}");
            Assert.InRange(strip.VerticalOffset, 40, 60);
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Opening_keeps_the_visible_strip_row_until_the_popup_covers_it(FlowDirection flow) => Sta.Run(() =>
    {
        Sta.UseApplication();
        var gallery = new InRibbonGallery { Width = 258, Height = 54, PopupWidth = 440, SelectedIndex = 4 };
        for (int i = 0; i < 9; i++) gallery.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 100, Height = 40, Background = i == 4 ? Brushes.Red : Brushes.SeaGreen } });
        var window = new Window { Content = gallery, FlowDirection = flow, Width = 1000, Height = 400, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); window.UpdateLayout(); Sta.Drain();
            var before = Render(window);
            int redBefore = RedPixels(before); Assert.True(redBefore > 200);
            gallery.IsDropDownOpen = true;
            // Observe the main-window surface before and after deferred popup work.
            Assert.True(RedPixels(Render(window)) >= redBefore * .9);
            Sta.Drain(); window.UpdateLayout(); Sta.Drain();
            Assert.True(RedPixels(Render(window)) >= redBefore * .9);
            Assert.Equal(4, gallery.SelectedIndex);
            gallery.IsDropDownOpen = false; Sta.Drain();
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    private static BitmapSource Render(Window window)
    {
        var scale = VisualTreeHelper.GetDpi(window);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * scale.DpiScaleX), (int)Math.Ceiling(window.ActualHeight * scale.DpiScaleY), scale.PixelsPerInchX, scale.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window); return bitmap;
    }

    private static int RedPixels(BitmapSource bitmap)
    {
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        int count = 0; for (int i = 0; i < pixels.Length; i += 4) if (pixels[i + 2] > 180 && pixels[i + 1] < 80 && pixels[i] < 80 && pixels[i + 3] > 180) count++;
        return count;
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Wide_popup_card_anchors_to_the_gallery_upper_leading_corner(FlowDirection flow) => Sta.Run(() =>
    {
        Sta.UseApplication();
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var gallery = new InRibbonGallery { Width = 258, Height = 54, PopupWidth = 650, FlowDirection = flow, VerticalAlignment = VerticalAlignment.Top };
        for (int i = 0; i < 9; i++) gallery.Items.Add(new RibbonGalleryItem { Content = $"Style {i}", Width = 100, Height = 48 });
        var window = new Window { Content = gallery, Width = 1000, Height = 400, Left = 50, Top = 100, WindowStyle = WindowStyle.None, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); gallery.IsDropDownOpen = true; Sta.Drain(); window.UpdateLayout(); Sta.Drain();
            var card = Assert.IsType<Border>(gallery.Template.FindName("PART_PopupHost", gallery));
            Rect sourceBounds = ScreenBounds(gallery), popupBounds = ScreenBounds(card);
            double expected = flow == FlowDirection.LeftToRight ? sourceBounds.Left : sourceBounds.Right;
            double actual = flow == FlowDirection.LeftToRight ? popupBounds.Left : popupBounds.Right;
            Assert.InRange(actual - expected, -1, 1);
            Assert.InRange(popupBounds.Top - sourceBounds.Top, -1, 1);
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }
    });

    private static Rect ScreenBounds(FrameworkElement element)
    {
        Point start = element.PointToScreen(default), end = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));
        return new Rect(start, end);
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Direction_change_and_template_reload_return_live_tiles(FlowDirection flow) => Sta.Run(() =>
    {
        Sta.UseApplication();
        var gallery = new InRibbonGallery { Width = 258, PopupWidth = 440, SelectedIndex = 4 };
        for (int i = 0; i < 9; i++) gallery.Items.Add($"Style {i}");
        var window = new Window { Content = gallery, FlowDirection = flow, Width = 900, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            gallery.IsDropDownOpen = true; Layout();
            var oldPopup = Part<Popup>("PART_Popup");
            window.FlowDirection = flow == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            Layout();
            Assert.False(gallery.IsDropDownOpen); Assert.False(oldPopup.IsOpen);
            Assert.IsType<ItemsPresenter>(Part<ScrollViewer>("PART_ScrollViewer").Content);

            gallery.IsDropDownOpen = true; Layout();
            gallery.Template = null; gallery.ApplyTemplate();
            gallery.ClearValue(Control.TemplateProperty); gallery.ApplyTemplate(); Layout();
            Assert.False(oldPopup.IsOpen);
            Assert.True(gallery.IsDropDownOpen); Assert.True(Part<Popup>("PART_Popup").IsOpen);
            Assert.IsType<ItemsPresenter>(Part<ScrollViewer>("PART_PopupScrollViewer").Content);
            Assert.True(Part<ScrollViewer>("PART_PopupScrollViewer").ViewportHeight > 0);
            Assert.Equal(4, gallery.SelectedIndex);
            gallery.IsDropDownOpen = false; Layout();
            Assert.IsType<ItemsPresenter>(Part<ScrollViewer>("PART_ScrollViewer").Content);
            Assert.NotNull(gallery.ItemContainerGenerator.ContainerFromIndex(4));
        }
        finally { window.Close(); Sta.ResetApplication(); }
        T Part<T>(string name) where T : DependencyObject => Assert.IsType<T>(Assert.IsType<ControlTemplate>(gallery.Template).FindName(name, gallery));
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    [Fact]
    public void Rtl_lab_native_gallery_reopens_after_toggling_direction() => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true); ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        var window = new LocalizationRtlDemo { Left = 50, Top = 50, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        try
        {
            window.Show(); window.ApplyCrystal(true); Layout();
            var gallery = window.RtlStylesGallery;
            foreach (bool rtl in new[] { true, false, true, false })
            {
                window.DemoRibbon.SelectedTab = window.DemoRibbon.Tabs[0]; Layout();
                window.RightToLeftToggle.IsChecked = rtl; Layout();
                window.DemoRibbon.SelectedTab = window.RtlChoicesTab; Layout();
                var expand = Assert.IsType<ToggleButton>(gallery.Template.FindName("PART_ExpandToggle", gallery));
                var popup = Assert.IsType<Popup>(gallery.Template.FindName("PART_Popup", gallery));
                var viewport = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_PopupScrollViewer", gallery));
                EventHandler firstFrame = (_, _) => Assert.True(viewport.ActualHeight > 100, $"first frame rtl={rtl}: viewport={viewport.RenderSize}, content={viewport.Content}");
                popup.Opened += firstFrame;
                expand.IsChecked = true; Layout();
                popup.Opened -= firstFrame;
                Assert.True(viewport.ViewportHeight > 100, $"rtl={rtl}: popup={viewport.RenderSize}, content={viewport.Content}, strip={((ScrollViewer)gallery.Template.FindName("PART_ScrollViewer", gallery)).Content}");
                Assert.True(viewport.ActualHeight > 100);
                gallery.IsDropDownOpen = false; Layout();
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void First_popup_after_direction_and_dpi_change_measures_the_live_content(bool grouped) => Sta.Run(() =>
    {
        Sta.UseApplication();
        var choices = Enumerable.Range(0, 9).Select(i => new StyleChoice($"Style {i}", $"Section {i / 3}")).ToArray();
        var view = new ListCollectionView(choices);
        if (grouped) view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(StyleChoice.Section)));
        var label = new FrameworkElementFactory(typeof(TextBlock));
        label.SetBinding(TextBlock.TextProperty, new Binding(nameof(StyleChoice.Name)));
        label.SetValue(FrameworkElement.WidthProperty, 100d); label.SetValue(FrameworkElement.HeightProperty, 40d);
        var gallery = new InRibbonGallery { Width = 258, PopupWidth = 440, DropDownHeader = "Choose a style",
            ItemsSource = view, ItemTemplate = new DataTemplate { VisualTree = label }, SelectedIndex = 4 };
        var window = new Window { Content = gallery, Width = 600, Height = 400, Left = 50, Top = 50,
            WindowStyle = WindowStyle.None, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft, FlowDirection.LeftToRight })
            foreach (double scale in new[] { 1.25, 2d })
            {
                window.FlowDirection = flow; VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); Layout();
                var popup = Assert.IsType<Popup>(gallery.Template.FindName("PART_Popup", gallery));
                var viewport = Assert.IsType<ScrollViewer>(gallery.Template.FindName("PART_PopupScrollViewer", gallery));
                EventHandler firstFrame = (_, _) => Assert.True(viewport.ActualHeight > 100, $"{flow}/{scale}: actual {viewport.RenderSize}, reported viewport {viewport.ViewportHeight}");
                popup.Opened += firstFrame; gallery.IsDropDownOpen = true; Layout(); popup.Opened -= firstFrame;
                Assert.True(viewport.ActualHeight > 100);
                var host = Assert.IsType<Border>(gallery.Template.FindName("PART_PopupHost", gallery));
                Assert.True(host.ActualHeight > viewport.ActualHeight);
                Assert.Equal(4, gallery.SelectedIndex);
                gallery.IsDropDownOpen = false; Layout();
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    private sealed record StyleChoice(string Name, string Section);

    [Theory]
    [InlineData(RibbonTheme.Office2024, RibbonDensity.Compact)]
    [InlineData(RibbonTheme.Office2024, RibbonDensity.Touch)]
    [InlineData(RibbonTheme.CrystalLight, RibbonDensity.Compact)]
    [InlineData(RibbonTheme.CrystalLight, RibbonDensity.Touch)]
    public void Switching_direction_after_native_popup_use_keeps_one_live_presenter(RibbonTheme theme, RibbonDensity density) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(); ThemeManager.Apply(application, theme);
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var gallery = new InRibbonGallery { Width = 258, PopupWidth = 440, SelectedIndex = 4, DropDownHeader = "Choose a style" };
        for (int i = 0; i < 9; i++) gallery.Items.Add(new RibbonGalleryItem { Content = new TextBlock { Text = $"Style {i}", Width = 100, Height = 40 } });
        var ribbon = new Ribbon { Density = density }; var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Styles", CanResize = false }; group.Items.Add(gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        var other = new RibbonTab { Header = "Mode" }; other.Groups.Add(new RibbonGroup { Header = "Mode" }); ribbon.Tabs.Add(other);
        var window = new Window { Content = ribbon, Width = 900, Height = 400, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            var strip = Part<ScrollViewer>("PART_ScrollViewer"); var presenter = Assert.IsType<ItemsPresenter>(strip.Content);
            foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft, FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ribbon.SelectedTab = other; Layout(); window.FlowDirection = flow; Layout(); ribbon.SelectedTab = tab; Layout();
                Assert.Same(presenter, strip.Content);
                gallery.IsDropDownOpen = true; Layout();
                var popupViewport = Part<ScrollViewer>("PART_PopupScrollViewer");
                Assert.True(Part<Popup>("PART_Popup").IsOpen);
                Assert.Same(presenter, popupViewport.Content);
                Assert.True(popupViewport.ViewportHeight > 100, $"{theme}/{density}/{flow}: viewport={popupViewport.RenderSize}, presenter={presenter.RenderSize}, content={popupViewport.Content}, strip={strip.Content}");
                Assert.Equal(4, gallery.SelectedIndex);
                gallery.IsDropDownOpen = false; Layout(); Assert.Same(presenter, strip.Content);
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }
        T Part<T>(string name) where T : DependencyObject => Assert.IsType<T>(gallery.Template.FindName(name, gallery));
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });
}
