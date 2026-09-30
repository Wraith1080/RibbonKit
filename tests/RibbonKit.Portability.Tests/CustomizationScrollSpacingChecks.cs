using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class CustomizationScrollSpacingChecks
{
    internal static void Verify(Application application)
    {
        foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2010,
            RibbonTheme.Office2013, RibbonTheme.Office2019, RibbonTheme.Office2024, RibbonTheme.CrystalLight })
        foreach (bool dark in new[] { false, true })
        foreach (double scale in new[] { 1d, 1.25, 1.5, 2d })
        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            ThemeManager.Apply(application, theme);
            ThemeManager.SetDarkMode(application, dark);
            var ribbon = new Ribbon();
            var tab = new RibbonTab { Header = "Home" };
            var group = new RibbonGroup { Header = "Commands" };
            for (int i = 0; i < 30; i++)
            {
                group.Items.Add(new RibbonButton { Header = "Command " + i });
                ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Quick command " + i });
            }
            tab.Groups.Add(group);
            ribbon.Tabs.Add(tab);
            var root = new Grid { Width = 659.2, Height = 480, FlowDirection = direction };
            root.SetResourceReference(Panel.BackgroundProperty, "RibbonKit.Brushes.Window.Background");
            var customize = new RibbonCustomizePage { Ribbon = ribbon, Margin = new Thickness(24, 20.8, 24, 13.6) };
            root.Children.Add(customize);
            Layout(root, scale);
            Check(customize, "PART_AvailableList", "PART_Tree");
            Capture(root, theme, dark, scale, direction);

            root.Children.Clear();
            var qat = new RibbonQuickAccessPage { Ribbon = ribbon, Margin = customize.Margin };
            root.Children.Add(qat);
            Layout(root, scale);
            Check(qat, "PART_AvailableList", "PART_CurrentList");

            void Check(Control page, string firstPart, string secondPart)
            {
                Assert.True(page.UseLayoutRounding);
                int expectedGap = (int)(Math.Round(scale) + Math.Round((theme == RibbonTheme.CrystalLight ? 2 : 1) * scale));
                var gaps = new List<int>();
                foreach (string part in new[] { firstPart, secondPart })
                {
                    var control = Assert.IsAssignableFrom<Control>(page.Template.FindName(part, page));
                    var frame = Assert.IsType<Border>(control.Template.FindName("Frame", control));
                    var viewer = Assert.IsType<ScrollViewer>(control.Template.FindName("PART_ScrollViewer", control));
                    var bar = Assert.IsType<ScrollBar>(viewer.Template.FindName("PART_VerticalScrollBar", viewer));
                    Assert.Equal(Visibility.Visible, bar.Visibility);
                    Assert.Equal(scale, VisualTreeHelper.GetDpi(bar).DpiScaleX);
                    var paint = Assert.IsType<Border>(bar.Template.FindName("Root", bar));
                    var frameBounds = frame.TransformToAncestor(root).TransformBounds(new Rect(frame.RenderSize));
                    var barBounds = paint.TransformToAncestor(root).TransformBounds(new Rect(paint.RenderSize));
                    int Pixel(double value) => (int)Math.Round(value * scale, MidpointRounding.AwayFromZero);
                    int top = Pixel(barBounds.Top) - Pixel(frameBounds.Top);
                    int bottom = Pixel(frameBounds.Bottom) - Pixel(barBounds.Bottom);
                    int side = Math.Min(Pixel(barBounds.Left) - Pixel(frameBounds.Left), Pixel(frameBounds.Right) - Pixel(barBounds.Right));
                    Assert.Equal(expectedGap, top);
                    Assert.Equal(top, bottom);
                    Assert.Equal(top, side);
                    gaps.Add(side);
                    viewer.ScrollToBottom();
                    root.UpdateLayout();
                    Assert.True(viewer.VerticalOffset > 0);
                    viewer.ScrollToTop();
                }
                Assert.Equal(gaps[0], gaps[1]);
            }
        }
        ThemeManager.SetDarkMode(application, false);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
    }

    private static void Layout(FrameworkElement root, double scale)
    {
        Arrange();
        // Deferred native templates materialize after the first layout. Refresh
        // their DPI and measurement, then verify the actual bar DPI in Check.
        for (int i = 0; i < 2; i++)
        {
            VisualTreeHelper.SetRootDpi(root, new DpiScale(scale == 1 ? 2 : 1, scale == 1 ? 2 : 1));
            VisualTreeHelper.SetRootDpi(root, new DpiScale(scale, scale));
            Invalidate(root);
            Arrange();
        }
        void Arrange()
        {
            root.Measure(new Size(root.Width, root.Height));
            root.Arrange(new Rect(0, 0, root.Width, root.Height));
            root.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            root.UpdateLayout();
        }
        static void Invalidate(DependencyObject element)
        {
            if (element is UIElement ui) { ui.InvalidateMeasure(); ui.InvalidateArrange(); }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                Invalidate(VisualTreeHelper.GetChild(element, i));
        }
    }

    private static void Capture(FrameworkElement root, RibbonTheme theme, bool dark, double scale, FlowDirection direction)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_CAPTURE_CUSTOMIZE_SPACING");
        if (string.IsNullOrWhiteSpace(directory) || scale == 1 || scale == 1.5
            || (theme != RibbonTheme.CrystalLight && (theme != RibbonTheme.Office2024 || dark))) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.Width * scale), (int)Math.Ceiling(root.Height * scale),
            96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, $"{theme}-{dark}-{scale}-{direction}.png"));
        encoder.Save(stream);
    }
}
