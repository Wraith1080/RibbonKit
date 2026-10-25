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

// Library-only popup chrome and native scrolling, without Writer or Showcase resources.
internal static class DropDownScrollBarChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var buttons = new RibbonDropDownButton[] { new(), new RibbonSplitButton() };
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var button in buttons)
        {
            button.Header = "Color";
            for (int i = 0; i < 30; i++) button.Items.Add(new RibbonMenuItem { Header = $"Color {i + 1}" });
            panel.Children.Add(button);
        }
        var window = new RibbonWindow { Content = panel, Width = 500, Height = 200,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        int cases = 0;
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show(); Layout();
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (var direction in Enum.GetValues<FlowDirection>())
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                window.FlowDirection = direction;
                foreach (var button in buttons) Ribbon.SetDensity(button, density);
                Layout();
                foreach (var button in buttons)
                {
                    var viewport = Part<ScrollViewer>(button, "PART_PopupScrollViewer");
                    viewport.MaxHeight = 180;
                    button.IsDropDownOpen = true; Layout();
                    var popup = Part<Popup>(button, "PART_Popup");
                    Assert.True(popup.IsOpen);
                    var bar = Part<ScrollBar>(viewport, "PART_VerticalScrollBar");
                    Assert.Same(viewport.FindResource("RibbonKit.ScrollBarStyle"), bar.Style);
                    Assert.Equal(Visibility.Visible, bar.Visibility);
                    Assert.Same(button.FindResource("RibbonKit.Brushes.ScrollBar.Track"), bar.Background);
                    double thickness = (double)button.FindResource("RibbonKit.Metrics.ScrollBar.Thickness");
                    Assert.Equal(thickness, bar.Width);
                    // Native popup layout rounds opposite edges independently at fractional DPI.
                    Assert.InRange(Math.Abs(bar.ActualWidth - thickness), 0,
                        1 / VisualTreeHelper.GetDpi(bar).DpiScaleX + 0.001);
                    var track = Part<Track>(bar, "PART_Track");
                    Assert.Equal(button.FindResource("RibbonKit.Metrics.ScrollBar.ThumbCornerRadius"),
                        Part<Border>(track.Thumb, "Pill").CornerRadius);
                    double before = viewport.VerticalOffset;
                    ScrollBar.LineDownCommand.Execute(null, bar); Layout();
                    Assert.True(viewport.VerticalOffset > before);
                    before = viewport.VerticalOffset;
                    ScrollBar.PageDownCommand.Execute(null, bar); Layout();
                    Assert.True(viewport.VerticalOffset > before);
                    viewport.ScrollToTop(); Layout();
                    Save((FrameworkElement)popup.Child, theme, dark, density, direction, button is RibbonSplitButton);
                    button.IsDropDownOpen = false; Layout();
                    Assert.Equal(30, button.Items.Count);
                    cases++;
                }
            }
            Console.WriteLine($"Dropdown/split scrollbar consumer: {cases} theme/palette/density/direction/control cases passed.");
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = motion;
        }
        void Layout()
        {
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Save(FrameworkElement element, RibbonTheme theme, bool dark, RibbonDensity density,
        FlowDirection direction, bool split)
    {
        string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_DROPDOWN_SCROLLBAR_DIAGNOSTICS");
        if (string.IsNullOrWhiteSpace(folder) || direction != FlowDirection.LeftToRight
            || !(theme is RibbonTheme.Office2010 or RibbonTheme.CrystalLight && density == RibbonDensity.Compact
                || theme == RibbonTheme.Office2024Flat && density == RibbonDensity.Touch)) return;
        Directory.CreateDirectory(folder);
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, $"{theme}-{dark}-{density}-{(split ? "split" : "dropdown")}.png"));
        encoder.Save(stream);
    }
}
