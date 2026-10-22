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

internal static class SimplifiedButtonShapeChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SlateGray, null,
            Geometry.Parse("M3,1 L9,1 C14,1 14,7 10,8 C15,9 15,15 9,15 L3,15 Z M6,4 L6,6 L9,6 L9,4 Z M6,10 L6,12 L9,12 L9,10 Z")));
        var action = new RibbonButton { Header = "Bold action", Icon = icon };
        var toggle = new RibbonToggleButton { Header = "Bold", Icon = icon, IsChecked = true };
        var labeled = new RibbonButton { Header = "Document style", Icon = icon };
        var group = new RibbonGroup { Header = "Formatting" };
        foreach (var command in new FrameworkElement[] { action, toggle, labeled })
        {
            Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline);
            group.Items.Add(command);
        }
        Ribbon.SetSimplifiedSize(action, RibbonControlSize.Small);
        Ribbon.SetSimplifiedSize(toggle, RibbonControlSize.Small);
        Ribbon.SetSimplifiedPriority(action, 100);
        Ribbon.SetSimplifiedPriority(toggle, 90);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(action);
        var window = new Window { Content = ribbon, Width = 900, Height = 330,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show(); VisualTreeHelper.SetRootDpi(window, new DpiScale(1, 1)); Layout();
            var qatCopy = Assert.IsType<RibbonButton>(Assert.Single(ribbon.QuickAccessItems));
            Size qatSize = qatCopy.RenderSize;
            Size classicSize = action.RenderSize;
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout();
            SameSize(qatSize, qatCopy.RenderSize);

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection direction in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1d, 1.25, 1.5, 2 })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                ribbon.Density = density; window.FlowDirection = direction; Layout();
                var toolbar = Descendants((RibbonGroupsHost)tab.Content).OfType<RibbonQuickAccessToolBar>().Single();
                var button = toolbar.Items.OfType<RibbonButton>().Single(item => item.Header == action.Header);
                var checkedButton = toolbar.Items.OfType<RibbonToggleButton>().Single();
                foreach (Control item in new Control[] { button, checkedButton })
                {
                    Assert.InRange(Math.Abs(item.ActualWidth - item.ActualHeight), 0, 1 / scale);
                    var chrome = Assert.IsType<Border>(item.Template.FindName("Chrome", item));
                    Assert.InRange(Math.Abs(chrome.ActualWidth - chrome.ActualHeight), 0, 1 / scale);
                }
                var medium = toolbar.Items.OfType<RibbonButton>().Single(item => item.Header == labeled.Header);
                Assert.Equal(RibbonControlSize.Medium, medium.Size);
                Assert.True(medium.ActualWidth > medium.ActualHeight);
                if (scale == 1.5 && !dark && direction == FlowDirection.LeftToRight)
                    Save(toolbar, $"{theme}-{density}");

                var more = (ToggleButton)toolbar.Template.FindName("PART_OverflowButton", toolbar);
                double slots = SlotWidth(button) + SlotWidth(checkedButton);
                // Leave a few physical pixels below the boundary so independent
                // margin/row rounding cannot turn this into an exact-fit case.
                toolbar.Width = slots + more.ActualWidth - 4 / scale; Layout();
                Assert.True(LayoutInformation.GetLayoutSlot(button).Width > 0);
                Assert.True(LayoutInformation.GetLayoutSlot(checkedButton).Width == 0,
                    $"{theme}/{dark}/{density}/{direction}/{scale}: toggle did not overflow at width {toolbar.ActualWidth}.");
                if (scale == 1 && theme == RibbonTheme.Office2024 && !dark)
                {
                    more.IsChecked = true; Layout();
                    var entries = (ItemsControl)toolbar.Template.FindName("PART_OverflowHost", toolbar);
                    var overflow = Assert.Single(entries.Items.OfType<RibbonToggleButton>());
                    Assert.Equal(RibbonControlSize.Medium, overflow.Size);
                    Assert.True(overflow.ActualWidth > overflow.ActualHeight);
                }
                more.IsChecked = false; toolbar.ClearValue(FrameworkElement.WidthProperty); Layout();
            }

            // Normal QAT and classic geometry, and explicitly authored proxy widths,
            // keep their existing sizing policy.
            ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
            VisualTreeHelper.SetRootDpi(window, new DpiScale(1, 1));
            ribbon.Density = RibbonDensity.Compact; window.FlowDirection = FlowDirection.LeftToRight; Layout();
            var row = Descendants((RibbonGroupsHost)tab.Content).OfType<RibbonQuickAccessToolBar>().Single();
            var customWidth = row.Items.OfType<RibbonButton>().Single(item => item.Header == action.Header);
            customWidth.Width = 74; Layout(); Assert.Equal(74, customWidth.ActualWidth);
            customWidth.ClearValue(FrameworkElement.WidthProperty); Layout();
            Assert.Equal(customWidth.ActualHeight, customWidth.ActualWidth, 6);
            ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout();
            SameSize(classicSize, action.RenderSize); SameSize(qatSize, qatCopy.RenderSize);

            void Layout()
            {
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void SameSize(Size expected, Size actual)
    {
        // The baseline uses simulated 100%; cached native text metrics can round
        // by one pixel while changing the physical window's DPI/mode.
        Assert.InRange(Math.Abs(expected.Width - actual.Width), 0, 1);
        Assert.InRange(Math.Abs(expected.Height - actual.Height), 0, 1);
    }
    private static double SlotWidth(FrameworkElement item) => item.ActualWidth + item.Margin.Left + item.Margin.Right;
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_SIMPLIFIED_BUTTON_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        // Include the toolbar's outer vertical margin in the capture. Its own
        // visual retains that offset, which otherwise clips the bottom edge.
        element = (FrameworkElement)VisualTreeHelper.GetParent(element);
        var dpi = VisualTreeHelper.GetDpi(element);
        double width = Math.Min(320, element.ActualWidth);
        var bounds = new Rect(0, 0, width, element.ActualHeight);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle((Brush)element.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), null, bounds);
            drawing.DrawRectangle(new VisualBrush(element) { ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = bounds, Stretch = Stretch.Fill }, null, bounds);
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
