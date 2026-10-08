using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using RibbonKit.Showcase;
using Xunit;

namespace RibbonKit.Tests;

public sealed class InRibbonGalleryPopupTests
{
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
