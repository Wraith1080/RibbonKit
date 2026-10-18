using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// A RibbonKit-only consumer, including fractional-DPI layout rounding.
internal static class RadioIndicatorPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var selected = new RibbonRadioButton { Header = "Comfortable", IsChecked = true };
        var other = new RibbonRadioButton { Header = "Compact" };
        var disabled = new RibbonRadioButton { Header = "Custom", IsEnabled = false };
        var options = new StackPanel { Margin = new Thickness(10), Children = { selected, other, disabled } };
        var root = new Grid { UseLayoutRounding = true, Children = { options } };
        var window = new Window { Content = root, Width = 180, Height = 220,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (double scale in new[] { 1d, 1.25, 1.5, 2d })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                root.SetResourceReference(Panel.BackgroundProperty, "RibbonKit.Brushes.Window.Background");
                root.FlowDirection = flow;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch, RibbonDensity.Compact })
                {
                    Ribbon.SetDensity(root, density);
                    Invalidate(root);
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                    window.UpdateLayout();
                    if (theme == RibbonTheme.Office2024 && !dark && flow == FlowDirection.LeftToRight)
                        Capture(root, $"{density}-{scale * 100:0}", scale);
                    foreach (var radio in new[] { selected, other, disabled })
                    {
                        var indicator = Part<Ellipse>(radio, "Indicator");
                        var dot = Part<Ellipse>(radio, "SelectionMark");
                        var outer = Center(indicator, radio);
                        var focus = Center(Part<Ellipse>(radio, "FocusRing"), radio);
                        string context = $"{theme}, dark={dark}, {flow}, {scale * 100}%, {density}";
                        Assert.True((outer - focus).Length * scale < 0.001, $"Focus ring center: {context}");
                        Assert.Equal(radio == selected ? Visibility.Visible : Visibility.Collapsed, dot.Visibility);
                        if (radio == selected)
                        {
                            var inner = Center(dot, radio);
                            Assert.True((outer - inner).Length * scale < 0.001,
                                $"Selected dot center: {context}; circle={outer}, dot={inner}");
                        }
                        Assert.Equal(density == RibbonDensity.Touch ? 44 : 22, radio.MinHeight);
                    }
                }
            }
            Assert.True(selected.IsChecked);
            Assert.False(other.IsChecked);
        }
        finally { window.Close(); }
    }

    private static Point Center(Ellipse ellipse, Visual ancestor)
    {
        Rect bounds = ellipse.RenderedGeometry.Bounds;
        return ellipse.TransformToAncestor(ancestor).Transform(new Point(
            bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
    }

    private static T Part<T>(Control control, string name) where T : FrameworkElement =>
        Assert.IsType<T>(control.Template.FindName(name, control));

    private static void Invalidate(DependencyObject element)
    {
        if (element is UIElement ui) ui.InvalidateMeasure();
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            Invalidate(VisualTreeHelper.GetChild(element, i));
    }

    private static void Capture(FrameworkElement element, string name, double scale)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_TOUCH_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * scale),
            (int)Math.Ceiling(element.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(System.IO.Path.Combine(directory, $"radio-{name}.png"));
        encoder.Save(file);
    }
}
