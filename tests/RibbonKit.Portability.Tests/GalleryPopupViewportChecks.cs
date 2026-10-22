using System.Collections.ObjectModel;
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

// A library-only reproduction of the grouped Theme picker and overflowing Accent picker.
internal static class GalleryPopupViewportChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            foreach (RibbonTheme theme in new[] { RibbonTheme.Office2010 }.Concat(
                Enum.GetValues<RibbonTheme>().Where(theme => theme != RibbonTheme.Office2010)))
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
                Verify(application, theme, density);
        }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void Verify(Application application, RibbonTheme theme, RibbonDensity density)
    {
        ThemeManager.Apply(application, theme);
        var choices = new ObservableCollection<Choice>
        {
            new("Crystal", "Modern"), new("Office 2024", "Office Modern"), new("2024 Flat", "Office Modern"),
            new("Office 2019", "Office Modern"), new("Office 2013", "Office Modern"),
            new("Office 2010", "Office Legacy"), new("Office 2007", "Office Legacy"),
        };
        var view = new ListCollectionView(choices);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Family)));
        var gallery = new InRibbonGallery { Header = "Theme", Width = 158, PopupWidth = 440,
            ItemsSource = view, ItemTemplate = Tile(112, 40), IsSynchronizedWithCurrentItem = false };
        var sourceGroup = new RibbonGroup { Header = "Theme", CanResize = false }; sourceGroup.Items.Add(gallery);
        var customGroup = new RibbonGroup { Header = "Custom", CanResize = false }; Ribbon.SetIsCustom(customGroup, true);
        var home = new RibbonTab { Header = "Home" }; home.Groups.Add(sourceGroup); home.Groups.Add(customGroup);
        var ribbon = new Ribbon { Density = density, QuickAccessPosition = RibbonQuickAccessPosition.TabRow,
            FlowDirection = density == RibbonDensity.Touch ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        ribbon.Tabs.Add(home); ribbon.SelectedTab = home;
        var customize = new RibbonCustomizePage { Ribbon = ribbon, Height = 160 };
        var window = new RibbonWindow { Content = new StackPanel { Children = { ribbon, customize } },
            Width = 950, Height = 650, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(window);
            Assert.True(ribbon.AddToQuickAccess(gallery)); Layout(window);
            var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            var available = Part<ListBox>(customize, "PART_AvailableList");
            available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(entry => ReferenceEquals(entry.Control, gallery));
            Assert.IsType<RibbonCustomizeNode>(Part<TreeView>(customize, "PART_Tree").Items[0])
                .Children.Single(node => node.IsCustom).IsSelected = true;
            Layout(window);
            var add = Part<ButtonBase>(customize, "PART_AddButton"); Assert.True(add.IsEnabled);
            add.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            var custom = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(customGroup.Items));

            CheckCopies(false, "theme");
            // The same gallery still needs a usable, themed rail when its content really overflows.
            view.GroupDescriptions.Clear(); gallery.DropDownHeader = "Choose an accent color";
            gallery.PopupWidth = double.NaN; gallery.Width = 150; gallery.ItemTemplate = Tile(56, 28);
            choices.Clear();
            for (int i = 0; i < 18; i++) choices.Add(new Choice("Color " + i, ""));
            Layout(window);
            CheckCopies(true, "accent");

            void CheckCopies(bool overflow, string scene)
            {
                gallery.IsDropDownOpen = true; Layout(window);
                var native = Part<ScrollViewer>(gallery, "PART_PopupScrollViewer");
                var presenter = Assert.IsType<ItemsPresenter>(native.Content);
                var nativeBar = VerticalBar(native);
                var expectedVisibility = overflow ? Visibility.Visible : Visibility.Collapsed;
                Assert.Equal(expectedVisibility, native.ComputedVerticalScrollBarVisibility);
                var nativeHeight = native.ViewportHeight;
                Save(Part<FrameworkElement>(gallery, "PART_PopupHost"), theme, density, scene + "-native");
                gallery.IsDropDownOpen = false; Layout(window);
                foreach (var (copy, route) in new[] { (qat, "qat"), (custom, "custom") })
                {
                    copy.IsDropDownOpen = true; Layout(window);
                    var viewport = Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(copy.Items));
                    var outer = Part<ScrollViewer>(copy, "PART_PopupScrollViewer");
                    Save(Part<FrameworkElement>(copy, "PART_MenuHost"), theme, density, scene + "-" + route);
                    Assert.Same(presenter, viewport.Content);
                    Assert.True(viewport.ComputedVerticalScrollBarVisibility == expectedVisibility,
                        $"{theme}/{density}/{scene}/{route}: extent={viewport.ExtentHeight}, viewport={viewport.ViewportHeight}, native={nativeHeight}, max={viewport.MaxHeight}");
                    Assert.True(Math.Abs(nativeHeight - viewport.ViewportHeight) < 0.1,
                        $"{theme}/{density}/{scene}/{route}: native viewport={nativeHeight}, copy={viewport.ViewportHeight}");
                    Assert.Equal(Visibility.Collapsed, outer.ComputedVerticalScrollBarVisibility);
                    var bar = VerticalBar(viewport);
                    Assert.Same(nativeBar.Style, bar.Style);
                    Assert.Same(nativeBar.Template, bar.Template);
                    if (overflow)
                    {
                        viewport.ScrollToEnd(); Layout(window);
                        Assert.True(viewport.VerticalOffset > 0);
                        var last = Assert.IsAssignableFrom<FrameworkElement>(gallery.ItemContainerGenerator.ContainerFromItem(choices[^1]));
                        Rect bounds = last.TransformToAncestor(viewport).TransformBounds(new Rect(last.RenderSize));
                        Assert.True(bounds.Bottom <= viewport.ViewportHeight + 1);
                        ThemeManager.SetDarkMode(application, true); Layout(window);
                        Assert.Same(bar.FindResource("RibbonKit.Brushes.ScrollBar.Glyph"), bar.Foreground);
                        ThemeManager.SetDarkMode(application, false); Layout(window);
                    }
                    copy.IsDropDownOpen = false; Layout(window);
                    Assert.Null(viewport.Content);
                    Assert.Same(presenter, Part<ScrollViewer>(gallery, "PART_ScrollViewer").Content);
                }
            }
        }
        finally { window.Close(); Layout(window); }
    }

    private sealed record Choice(string Name, string Family);
    private static DataTemplate Tile(double width, double height)
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(FrameworkElement.WidthProperty, width); border.SetValue(FrameworkElement.HeightProperty, height);
        border.SetValue(Border.BackgroundProperty, Brushes.LightSteelBlue);
        var text = new FrameworkElementFactory(typeof(TextBlock)); text.SetBinding(TextBlock.TextProperty, new Binding(nameof(Choice.Name)));
        text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center); border.AppendChild(text);
        return new DataTemplate { VisualTree = border };
    }
    private static ScrollBar VerticalBar(ScrollViewer viewport) =>
        Assert.IsType<ScrollBar>(viewport.Template.FindName("PART_VerticalScrollBar", viewport));
    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
    private static void Save(FrameworkElement element, RibbonTheme theme, RibbonDensity density, string scene)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_VIEWPORT_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory) || density != RibbonDensity.Compact
            || theme is not (RibbonTheme.Office2010 or RibbonTheme.CrystalLight)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(element);
        var image = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        image.Render(element); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(directory, $"{theme}-{scene}.png")); encoder.Save(stream);
    }
}
