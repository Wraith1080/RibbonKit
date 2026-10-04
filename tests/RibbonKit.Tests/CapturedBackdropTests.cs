using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CapturedBackdropTests
{
    [Fact]
    public void Popup_registration_rebinds_templates_and_releases_queued_paint() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.CrystalLight);
        var button = new RibbonDropDownButton { Header = "Commands" };
        button.Items.Add(new RibbonMenuItem { Header = "Open" });
        var window = Window(button);
        using var capture = new CapturedBackdrop(button, window);
        try
        {
            window.Show();
            capture.Apply(true);
            button.IsDropDownOpen = true;
            Layout();
            Border host = Host();
            Assert.IsType<DrawingBrush>(host.Background);
            for (int index = 0; index < 2; index++)
            {
                button.IsDropDownOpen = false;
                button.Template = index == 0 ? new ControlTemplate(typeof(RibbonDropDownButton)) : null;
                Layout();
                capture.Refresh();
            }
            button.ClearValue(Control.TemplateProperty);
            button.IsDropDownOpen = true;
            Layout();
            Assert.False(host.Background is DrawingBrush); // The discarded template released captured paint.
            host = Host();
            Assert.IsType<DrawingBrush>(host.Background);

            foreach (double scale in new[] { 1.25, 2, 1.5, 1.25 })
            {
                Brush before = host.Background;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                Layout();
                capture.Refresh();
                Layout();
                Assert.NotSame(before, host.Background);
                var crop = Assert.IsType<CroppedBitmap>(Assert.IsType<ImageDrawing>(
                    Assert.IsType<DrawingGroup>(Assert.IsType<DrawingBrush>(host.Background).Drawing).Children[1]).ImageSource);
                Assert.Equal(VisualTreeHelper.GetDpi(window).PixelsPerInchX, crop.DpiX, 5);
                Assert.True(crop.PixelWidth > 0);
            }
            capture.Refresh();
            capture.Dispose();
            Layout();
            Assert.Same(button.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), host.Background);
            ThemeManager.SetDarkMode(Application.Current, true);
            Layout();
            Assert.Same(button.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), host.Background);
            Assert.Throws<ObjectDisposedException>(() => capture.Apply(true));
            Assert.Throws<ArgumentException>(() => new CapturedBackdrop(new Button(), window));
        }
        finally { button.IsDropDownOpen = false; window.Close(); Sta.ResetApplication(); }
        Border Host() => Assert.IsAssignableFrom<Border>(button.Template!.FindName("PART_MenuHost", button));
        void Layout() { window.UpdateLayout(); Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    [Fact]
    public void Menu_capture_retains_opacity_binding_and_updates_corner_and_dpi_overrides() => Sta.Run(() =>
    {
        var scene = new Canvas { Width = 240, Height = 180, Background = Brushes.White };
        scene.Children.Add(new Border { Width = 120, Height = 180, Background = Brushes.Black });
        var frame = new Border { Background = Brushes.Red, CornerRadius = new CornerRadius(14) };
        var wrapper = new Grid { Children = { new Grid(), frame } };
        var opacitySource = new Border { Opacity = 0.8 };
        wrapper.SetBinding(UIElement.OpacityProperty, new Binding(nameof(UIElement.Opacity)) { Source = opacitySource });
        var host = new Border { Width = 180, Height = 120, Child = wrapper };
        Canvas.SetLeft(host, 30); Canvas.SetTop(host, 30);
        scene.Children.Add(host);
        var window = Window(scene);
        CapturedMenuBackdrop? capture = null;
        try
        {
            window.Show(); window.UpdateLayout();
            capture = new CapturedMenuBackdrop(scene, host, frame, wrapper);
            Layout();
            Assert.Equal(0.8, wrapper.Opacity);
            Assert.NotNull(BindingOperations.GetBindingExpression(wrapper, UIElement.OpacityProperty));
            frame.CornerRadius = new CornerRadius(4, 10, 18, 2);
            Layout();
            Assert.IsType<StreamGeometry>(capture.Layer.Clip);
            Assert.False(capture.Layer.Clip.FillContains(new Point(179, 119)));
            opacitySource.Opacity = 0.6;
            foreach (double scale in new[] { 1.25, 2, 1.25 })
            {
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                capture.Refresh();
                Layout();
                var image = Assert.IsType<Image>(Assert.IsType<Canvas>(capture.Layer.Children[0]).Children[0]);
                Assert.Equal(VisualTreeHelper.GetDpi(scene).PixelsPerInchX,
                    Assert.IsAssignableFrom<BitmapSource>(image.Source).DpiX, 5);
                Assert.Equal(0.6, wrapper.Opacity);
            }
            capture.Refresh();
            capture.Remove();
            Layout();
            Assert.Equal(2, wrapper.Children.Count);
            Assert.NotNull(BindingOperations.GetBindingExpression(wrapper, UIElement.OpacityProperty));
        }
        finally { capture?.Remove(); window.Close(); }
        void Layout() { window.UpdateLayout(); Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    private static Window Window(UIElement content) => new()
    {
        Content = content, Width = 700, Height = 500, Left = -10000, Top = -10000,
        ShowActivated = false, ShowInTaskbar = false,
    };
}
