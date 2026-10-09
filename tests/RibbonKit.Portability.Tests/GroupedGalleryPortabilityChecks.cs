using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class GroupedGalleryPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try { VerifyLayouts(application); }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void VerifyLayouts(Application application)
    {
        foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
        foreach (bool dark in new[] { false, true })
        foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
        foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
        foreach (double scale in new[] { 1.25, 2d })
        {
            ThemeManager.Apply(application, theme);
            ThemeManager.SetDarkMode(application, dark);
            var choices = new ObservableCollection<Choice>
            {
                new("Crystal", "Modern"), new("Office 2024", "Office Modern"),
                new("Office 2019", "Office Modern"), new("Office 2013", "Office Modern"),
                new("Office 2010", "Office Legacy"), new("Office 2007", "Office Legacy"),
            };
            var view = new ListCollectionView(choices);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Family)));
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new Binding(nameof(Choice.Name)));
            text.SetValue(FrameworkElement.WidthProperty, 112d);
            text.SetValue(FrameworkElement.HeightProperty, 40d);
            var gallery = new InRibbonGallery
            {
                Width = 158, PopupWidth = 440, Height = density == RibbonDensity.Touch ? 78 : 54,
                ItemsSource = view, ItemTemplate = new DataTemplate { VisualTree = text }, FlowDirection = flow,
            };
            Ribbon.SetDensity(gallery, density);
            var window = new Window
            {
                Content = gallery, Width = 650, Height = 220,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
            };
            try
            {
                window.Show();
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                Layout();
                var presenter = Part<ItemsPresenter>(gallery, "PART_ItemsPresenter");
                double stripWidth = gallery.ActualWidth;
                Assert.True(stripWidth >= 158);
                Assert.Equal(stripWidth, presenter.MaxWidth, 3);
                Assert.True(Descendants<GroupItem>(presenter).Count() == 3,
                    $"groups={view.Groups?.Count}; grouping={gallery.IsGrouping}; styles={gallery.GroupStyle.Count}; descendants={string.Join(",", Descendants<FrameworkElement>(presenter).Select(element => element.GetType().Name))}");
                Assert.All(Descendants<GroupItem>(presenter), group =>
                    Assert.Equal(Visibility.Collapsed, Part<Border>(group, "GroupHeaderHost").Visibility));

                // Arrow steps follow native row positions, including the remainder
                // at the bottom, rather than accumulating rounded viewport heights.
                var rowStrip = Part<ScrollViewer>(gallery, "PART_ScrollViewer");
                var rows = Descendants<RibbonGalleryItem>(presenter)
                    .OrderBy(item => Bounds(item, presenter).Top).ToArray();
                int initialSelection = gallery.SelectedIndex;
                Assert.Equal(6, rows.Length);
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    rowStrip.ScrollToVerticalOffset(rowStrip.ScrollableHeight); Layout();
                    for (int row = rows.Length - 2; row >= 0; row--)
                    {
                        Part<ButtonBase>(gallery, "PART_LineUp").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                        Assert.InRange(Bounds(rows[row], rowStrip).Top, -0.1, 0.1);
                    }
                    for (int row = 1; row < rows.Length - 1; row++)
                    {
                        Part<ButtonBase>(gallery, "PART_LineDown").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                        Assert.InRange(Bounds(rows[row], rowStrip).Top, -0.1, 0.1);
                        if (cycle == 0 && row == 2 && !dark && scale == 1.25 && density == RibbonDensity.Compact
                            && (theme == RibbonTheme.Office2024 && flow == FlowDirection.LeftToRight
                                || theme == RibbonTheme.CrystalLight && flow == FlowDirection.RightToLeft))
                            SaveStrip(window, gallery, $"{theme}-{flow}-row-scroll");
                    }
                    Part<ButtonBase>(gallery, "PART_LineDown").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                    Assert.Equal(rowStrip.ScrollableHeight, rowStrip.VerticalOffset, 3);
                    Assert.Equal(initialSelection, gallery.SelectedIndex);
                }
                rowStrip.ScrollToVerticalOffset(0); Layout();

                gallery.IsDropDownOpen = true;
                Layout();
                var popup = Part<Border>(gallery, "PART_PopupHost");
                var scroller = Part<ScrollViewer>(gallery, "PART_PopupScrollViewer");
                Assert.Equal(440, popup.ActualWidth, 2);
                Assert.Equal(440, presenter.MaxWidth, 2);
                Assert.Same(presenter, scroller.Content);
                Assert.Equal(Visibility.Collapsed, Part<Border>(gallery, "DropDownHeaderHost").Visibility);
                var groups = Descendants<GroupItem>(presenter).ToArray();
                Assert.Equal(new[] { "Modern", "Office Modern", "Office Legacy" },
                    groups.Select(group => ((CollectionViewGroup)group.Content).Name));
                double previousBottom = 0;
                foreach (var group in groups)
                {
                    var header = Part<Border>(group, "GroupHeaderHost");
                    Assert.Equal(Visibility.Visible, header.Visibility);
                    Assert.False(header.IsHitTestVisible);
                    Assert.Same(gallery.FindResource("RibbonKit.Brushes.ApplicationMenu.HeaderBackground"), header.Background);
                    var headerBounds = Bounds(header, scroller);
                    Assert.True(headerBounds.Top >= previousBottom - 0.01);
                    var tiles = Descendants<RibbonGalleryItem>(group).ToArray();
                    Assert.Equal(((CollectionViewGroup)group.Content).ItemCount, tiles.Length);
                    var bounds = tiles.Select(tile => Bounds(tile, scroller)).OrderBy(rect => rect.Left).ToArray();
                    Assert.All(bounds, rect =>
                    {
                        Assert.Equal(bounds[0].Top, rect.Top, 2);
                        Assert.True(rect.Top >= headerBounds.Bottom - 0.01);
                        Assert.True(rect.Left >= -0.01 && rect.Right <= scroller.ViewportWidth + 0.01);
                    });
                    for (int i = 1; i < bounds.Length; i++) Assert.True(bounds[i - 1].Right <= bounds[i].Left + 0.01);
                    previousBottom = bounds.Max(rect => rect.Bottom);
                }
                Assert.True(previousBottom <= scroller.ViewportHeight + 0.01);
                if (!dark && scale == 1.25 && flow == FlowDirection.LeftToRight
                    && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight)
                    Save(popup, $"{theme}-{density}-grouped-gallery");

                // A popup width binding can change while open, then falls back to the
                // original wrapping when the selected item returns to the strip.
                var model = new WidthModel();
                gallery.SetBinding(InRibbonGallery.PopupWidthProperty, new Binding(nameof(WidthModel.Width)) { Source = model });
                model.Width = 460;
                Layout();
                Assert.Equal(460, popup.ActualWidth, 2);
                gallery.SelectedItem = choices[^1];
                Layout();
                gallery.IsDropDownOpen = false;
                Layout();
                Assert.Same(choices[^1], gallery.SelectedItem);
                var strip = Part<ScrollViewer>(gallery, "PART_ScrollViewer");
                Assert.Same(presenter, strip.Content);
                Assert.Equal(stripWidth, gallery.ActualWidth, 3);
                Assert.Equal(stripWidth, presenter.MaxWidth, 3);
                Assert.All(groups, group => Assert.Equal(Visibility.Collapsed, Part<Border>(group, "GroupHeaderHost").Visibility));
                var selected = Descendants<RibbonGalleryItem>(presenter).Single(tile => ReferenceEquals(tile.Content, choices[^1]));
                var selectedBounds = Bounds(Part<Border>(selected, "Chrome"), strip);
                Assert.True(selectedBounds.Top >= -0.01 && selectedBounds.Bottom <= strip.ActualHeight + 0.01,
                    $"{theme}/{density}/{flow}/{scale}: selected bounds {selectedBounds}, strip {strip.RenderSize}.");
            }
            finally { window.Close(); Layout(); }
            void Layout()
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
        }
        ThemeManager.SetDarkMode(application, false);
    }

    private sealed record Choice(string Name, string Family);
    private sealed class WidthModel : INotifyPropertyChanged
    {
        private double _width = 440;
        public double Width { get => _width; set { _width = value; PropertyChanged?.Invoke(this, new(nameof(Width))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private static T Part<T>(Control control, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static Rect Bounds(FrameworkElement element, Visual relativeTo) =>
        element.TransformToVisual(relativeTo).TransformBounds(new Rect(element.RenderSize));
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GROUPED_GALLERY_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }

    private static void SaveStrip(Window window, FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GROUPED_GALLERY_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(window);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var bounds = Bounds(element, window);
        int left = (int)Math.Floor(bounds.Left * dpi.DpiScaleX), top = (int)Math.Floor(bounds.Top * dpi.DpiScaleY);
        var crop = new CroppedBitmap(bitmap, new Int32Rect(left, top,
            (int)Math.Ceiling(bounds.Right * dpi.DpiScaleX) - left, (int)Math.Ceiling(bounds.Bottom * dpi.DpiScaleY) - top));
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(crop));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
