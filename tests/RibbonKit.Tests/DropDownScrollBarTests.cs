using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Markup;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class DropDownScrollBarTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Overflowing_dropdown_and_split_popup_use_shared_scrollbar_and_native_scrolling(bool split) => Sta.Run(() =>
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        RibbonDropDownButton button = split ? new RibbonSplitButton() : new RibbonDropDownButton();
        button.Header = "Color";
        for (int i = 0; i < 30; i++) button.Items.Add(new RibbonMenuItem { Header = $"Color {i + 1}" });
        var window = new Window
        {
            Width = 240, Height = 100, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None,
            Content = button,
        };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024Flat.xaml", UriKind.Relative),
        });
        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Loaded);
            var viewport = Assert.IsType<ScrollViewer>(button.Template.FindName("PART_PopupScrollViewer", button));
            viewport.MaxHeight = 180;
            button.IsDropDownOpen = true;
            Sta.Drain(DispatcherPriority.Render);
            var popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));
            var bar = Assert.Single(Descendants<ScrollBar>(viewport), item => item.Orientation == Orientation.Vertical);
            SavePreview((FrameworkElement)popup.Child, split ? "split" : "dropdown");
            var style = Assert.IsType<Style>(viewport.Resources[typeof(ScrollBar)]);
            Assert.Same(viewport.FindResource("RibbonKit.ScrollBarStyle"), style);
            Assert.Same(style, bar.Style);
            Assert.IsType<RibbonScrollBarTrack>(bar.Template.FindName("PART_Track", bar));
            Assert.Equal(Visibility.Visible, bar.Visibility);
            Assert.True(viewport.ScrollableHeight > 0);
            double before = viewport.VerticalOffset;
            ScrollBar.LineDownCommand.Execute(null, bar);
            Sta.Drain(DispatcherPriority.Render);
            Assert.True(viewport.VerticalOffset > before);
            Assert.True(popup.IsOpen);
        }
        finally { button.IsDropDownOpen = false; window.Close(); RibbonAnimation.GlobalLevel = motion; }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Custom_template_keeps_its_authored_native_scrollbar_style(bool split) => Sta.Run(() =>
    {
        RibbonDropDownButton button = split ? new RibbonSplitButton() : new RibbonDropDownButton();
        string target = split ? "RibbonSplitButton" : "RibbonDropDownButton";
        button.Template = (ControlTemplate)XamlReader.Parse($$"""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:rk="urn:ribbonkit" TargetType="{x:Type rk:{{target}}}">
              <ScrollViewer x:Name="PART_PopupScrollViewer" VerticalScrollBarVisibility="Auto">
                <ScrollViewer.Resources>
                  <Style TargetType="{x:Type ScrollBar}"><Setter Property="Tag" Value="host-scrollbar" /></Style>
                </ScrollViewer.Resources>
                <Border Height="1000" Width="80" />
              </ScrollViewer>
            </ControlTemplate>
            """);
        var window = new Window { Content = button, Width = 200, Height = 150,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(DispatcherPriority.Render);
            var viewport = Assert.IsType<ScrollViewer>(button.Template.FindName("PART_PopupScrollViewer", button));
            var bar = Assert.Single(Descendants<ScrollBar>(viewport), item => item.Orientation == Orientation.Vertical);
            Assert.Same(viewport.Resources[typeof(ScrollBar)], bar.Style);
            Assert.Equal("host-scrollbar", bar.Tag);
            Assert.Equal(Visibility.Visible, bar.Visibility);
        }
        finally { window.Close(); }
    });

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static void SavePreview(FrameworkElement element, string name)
    {
        string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_DROPDOWN_SCROLLBAR_DIAGNOSTICS");
        if (string.IsNullOrWhiteSpace(folder)) return;
        Directory.CreateDirectory(folder);
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, name + ".png"));
        encoder.Save(stream);
    }
}
