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
using Rectangle = System.Windows.Shapes.Rectangle;

namespace RibbonKit.Portability.Tests;

internal static class SimplifiedSpacingChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SlateGray, null, new RectangleGeometry(new Rect(3, 2, 10, 12))));
        var font = new RibbonComboBox { InputWidth = 150, IsEditable = true, Text = "Calibri" };
        var size = new RibbonComboBox { InputWidth = 48 }; size.Items.Add("11"); size.SelectedIndex = 0;
        var bold = new RibbonToggleButton { Header = "Bold", Icon = icon, IsChecked = true };
        var italic = new RibbonToggleButton { Header = "Italic", Icon = icon, IsChecked = true };
        var underline = new RibbonToggleButton { Header = "Underline", Icon = icon, IsChecked = true };
        var copy = new RibbonButton { Header = "Copy", Icon = icon };
        var paste = new RibbonSplitButton { Header = "Paste", Icon = icon };
        var select = new RibbonDropDownButton { Header = "Select", Icon = icon };
        var gallery = new InRibbonGallery { Header = "Styles", Icon = icon, Width = 180 };
        gallery.Items.Add(new RibbonGalleryItem { Content = "Normal" });
        var clipboard = new RibbonGroup { Header = "Clipboard", CanResize = false }; clipboard.Items.Add(paste); clipboard.Items.Add(copy);
        var formatting = new RibbonGroup { Header = "Font", CanResize = false };
        foreach (var item in new FrameworkElement[] { font, size, bold, italic, underline }) formatting.Items.Add(item);
        var styles = new RibbonGroup { Header = "Styles", CanResize = false }; styles.Items.Add(gallery); styles.Items.Add(select);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(clipboard); tab.Groups.Add(formatting); tab.Groups.Add(styles);
        var sources = new FrameworkElement[] { paste, copy, font, size, bold, italic, underline, gallery, select };
        foreach (var source in sources) Ribbon.SetSimplifiedPlacement(source, RibbonSimplifiedPlacement.Inline);
        Ribbon.SetSimplifiedSize(underline, RibbonControlSize.Small);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow }; ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(copy); ribbon.AddToQuickAccess(gallery);
        var window = new Window { Content = ribbon, Width = 1200, Height = 330,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024); window.Show(); Layout();
            var classic = new Dictionary<RibbonDensity, Dictionary<FrameworkElement, (Thickness Margin, Size Size)>>();
            var qat = new Dictionary<RibbonDensity, Dictionary<FrameworkElement, (Thickness Margin, Size Size)>>();
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density; Layout();
                classic[density] = sources.ToDictionary(item => item, item => (item.Margin, item.RenderSize));
                qat[density] = ribbon.QuickAccessItems.OfType<FrameworkElement>().ToDictionary(item => item, item => (item.Margin, item.RenderSize));
            }
            ribbon.Density = RibbonDensity.Compact;
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout();
            foreach (var pair in qat[RibbonDensity.Compact]) Assert.Equal(pair.Value, (pair.Key.Margin, pair.Key.RenderSize));

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection direction in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1d, 1.5 })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = direction;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); Layout();
                var row = Descendants<RibbonQuickAccessToolBar>((DependencyObject)tab.Content).Single();
                FrameworkElement Inline(FrameworkElement source) => row.Items.OfType<FrameworkElement>().Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), source));
                var before = Paint(Inline(underline), row);
                var after = Paint(Inline(gallery), row);
                var divider = row.Items.OfType<RibbonGroupSeparator>().Single(item =>
                {
                    var line = Bounds(Descendants<Rectangle>(item).Single(), row);
                    return line.Left >= Math.Min(before.Right, after.Right) && line.Right <= Math.Max(before.Left, after.Left);
                });
                var stroke = Bounds(Descendants<Rectangle>(divider).Single(), row);
                double expectedGap = density == RibbonDensity.Compact ? 3 : 4;
                foreach (var (left, right) in new (FrameworkElement, FrameworkElement)[] { (paste, copy), (font, size),
                             (size, bold), (bold, italic), (italic, underline), (gallery, select) })
                {
                    Near(expectedGap, Gap(Paint(Inline(left), row), Paint(Inline(right), row)), scale);
                }
                Near(expectedGap, Gap(before, stroke), scale); Near(expectedGap, Gap(stroke, after), scale);
                Near(5, divider.ActualWidth, scale);
                Near(density == RibbonDensity.Compact ? 28 : 36, stroke.Height, scale);
                foreach (var editor in new[] { (RibbonComboBox)Inline(font), (RibbonComboBox)Inline(size) })
                    Near(density == RibbonDensity.Compact ? 28 : 44, Part<Grid>(editor, "InputBox").ActualHeight, scale);
                var small = (Control)Inline(underline); var smallChrome = Part<Border>(small, "Chrome");
                Near(small.ActualHeight, small.ActualWidth, scale); Near(smallChrome.ActualHeight, smallChrome.ActualWidth, scale);
                Assert.Equal(density == RibbonDensity.Compact ? new Thickness() : new Thickness(2, 1, 2, 1), Inline(gallery).Margin);
                Near(density == RibbonDensity.Compact ? 36 : 46, row.ActualHeight, scale);
                if (direction == FlowDirection.LeftToRight && scale == 1)
                {
                    Console.WriteLine($"{theme}/{dark}/{density}: buttons={Gap(Paint(Inline(bold), row), Paint(Inline(italic), row)):F3}; divider={divider.ActualWidth:F3}; button/line={Gap(before, stroke):F3}; line/gallery={Gap(stroke, after):F3}");
                    Save(row, $"{theme}-{(dark ? "dark" : "light")}-{density}");
                }
                var kept = row.Items.OfType<FrameworkElement>().Where(item => LayoutInformation.GetLayoutSlot(item).Width > 0).ToArray();
                double span = kept.Max(item => LayoutInformation.GetLayoutSlot(item).Right) - kept.Min(item => LayoutInformation.GetLayoutSlot(item).Left);
                var more = Part<ToggleButton>(row, "PART_OverflowButton");
                row.Width = span + more.ActualWidth + 4 / scale; Layout();
                Assert.True(LayoutInformation.GetLayoutSlot(Inline(select)).Width > 0);
                row.Width = span + more.ActualWidth - 4 / scale; Layout();
                Assert.True(LayoutInformation.GetLayoutSlot(Inline(select)).Width == 0);
                row.ClearValue(FrameworkElement.WidthProperty); Layout();
                italic.Visibility = Visibility.Collapsed; Layout();
                Near(expectedGap, Gap(Paint(Inline(bold), row), Paint(Inline(underline), row)), scale);
                italic.Visibility = Visibility.Visible; Layout();
            }

            ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
            ribbon.Density = RibbonDensity.Compact; window.FlowDirection = FlowDirection.LeftToRight;
            VisualTreeHelper.SetRootDpi(window, new DpiScale(1, 1)); Layout();
            var finalRow = Descendants<RibbonQuickAccessToolBar>((DependencyObject)tab.Content).Single();
            var finalBold = finalRow.Items.OfType<FrameworkElement>().Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), bold));
            var finalItalic = finalRow.Items.OfType<FrameworkElement>().Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), italic));
            finalRow.Resources["RibbonKit.Metrics.SimplifiedItemSpacing"] = 2d; Layout();
            Near(4, Gap(Paint(finalBold, finalRow), Paint(finalItalic, finalRow)), 1);
            ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout();
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density; Layout();
                foreach (var pair in classic[density].Concat(qat[density]))
                {
                    Assert.Equal(pair.Value.Margin, pair.Key.Margin);
                    Near(pair.Value.Size.Width, pair.Key.ActualWidth, 1); Near(pair.Value.Size.Height, pair.Key.ActualHeight, 1);
                }
            }
            void Layout()
            {
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static Rect Paint(FrameworkElement item, Visual relative)
    {
        if (item is RibbonComboBox combo) return Bounds(Part<Border>(combo, "Chrome"), relative);
        var control = Assert.IsAssignableFrom<Control>(item);
        if (item is RibbonDropDownButton)
        {
            var toggle = Part<ToggleButton>(control, "PART_Toggle");
            Rect bounds = Bounds(Part<Border>(toggle, "Chrome"), relative);
            if (item is RibbonSplitButton)
                bounds.Union(Bounds(Part<Border>(Part<Button>(control, "PART_Primary"), "Chrome"), relative));
            return bounds;
        }
        return Bounds(Part<Border>(control, "Chrome"), relative);
    }
    private static double Gap(Rect a, Rect b) => Math.Max(a.Left, b.Left) - Math.Min(a.Right, b.Right);
    private static Rect Bounds(FrameworkElement element, Visual relative) => element.TransformToAncestor(relative).TransformBounds(new Rect(element.RenderSize));
    private static void Near(double expected, double actual, double scale) => Assert.InRange(Math.Abs(expected - actual), 0, 1 / scale + 0.001);
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_SIMPLIFIED_SPACING_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        // Include the toolbar's outer three-DIP top/bottom clearance so tall
        // Touch fields are not clipped by the nested visual's layout offset.
        element = (FrameworkElement)VisualTreeHelper.GetParent(element);
        var bounds = new Rect(element.RenderSize);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle((Brush)element.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), null, bounds);
            drawing.DrawRectangle(new VisualBrush(element) { ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = bounds, Stretch = Stretch.Fill }, null, bounds);
        }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
