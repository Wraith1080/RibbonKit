using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Animation;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class GalleryQuickAccessTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2007)]
    [InlineData(RibbonTheme.Office2010)]
    public void Legacy_dark_gallery_qat_heading_uses_the_header_foreground(RibbonTheme theme) => Sta.Run(() =>
    {
        var source = new InRibbonGallery { Header = "Styles", DropDownHeader = "Choose a style", Width = 200 };
        source.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 60, Height = 30 } });
        var ribbon = RibbonFor(source);
        Assert.True(ribbon.AddToQuickAccess(source));
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        using var host = new Host(proxy);
        try
        {
            ThemeManager.Apply(Application.Current, theme);
            ThemeManager.SetDarkMode(Application.Current, true);
            proxy.IsDropDownOpen = true; host.Layout();
            var title = Assert.IsType<TextBlock>(proxy.Template.FindName("DropDownHeaderText", proxy));
            Assert.Equal(source.DropDownHeader, title.Text);
            Assert.Same(proxy.FindResource("RibbonKit.Brushes.ApplicationMenu.Foreground"), title.Foreground);
            Assert.True(Assert.IsType<SolidColorBrush>(title.Foreground).Color.R >= 200);
        }
        finally { proxy.IsDropDownOpen = false; ThemeManager.SetDarkMode(Application.Current, false); host.Layout(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unrealized_authored_gallery_keeps_visuals_items_selection_and_preview_handlers(bool inRibbon) => Sta.Run(() =>
    {
        RibbonGallery source = inRibbon ? new InRibbonGallery { PopupWidth = 440 } : new RibbonGallery();
        source.Header = "Styles"; source.Width = 200;
        var first = new RibbonGalleryItem { Content = new Border { Width = 80, Height = 40, Background = Brushes.Red } };
        var last = new RibbonGalleryItem { Content = new TextBlock { Text = "Heading" } };
        source.Items.Add(first); source.Items.Add(last); source.SelectedIndex = 0;
        var ribbon = RibbonFor(source); Assert.True(ribbon.AddToQuickAccess(source)); Assert.False(ribbon.AddToQuickAccess(source));
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        using var host = new Host(proxy);
        int changes = 0, previews = 0, cancelled = 0;
        source.SelectionChanged += (_, e) => { Assert.Same(source, e.Source); changes++; };
        source.ItemPreview += (_, e) => { Assert.Same(source, e.Source); Assert.Same(last, e.PreviewedItem); previews++; };
        source.ItemPreviewCancelled += (_, _) => cancelled++;
        Assert.False(source.IsLoaded);
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.True(proxy.IsDropDownOpen); Assert.True(source.IsQuickAccessOpen);
        var viewport = Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(proxy.Items));
        var presenter = Assert.IsType<ItemsPresenter>(viewport.Content);
        Assert.Same(source, first.Parent); Assert.Same(source, last.Parent);
        Assert.Same(last, source.ItemContainerGenerator.ContainerFromIndex(1));
        Assert.True(first.ActualHeight > 0); Assert.True(last.ActualHeight > 0);
        last.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
        Assert.Equal(1, previews);
        last.IsSelected = true; Assert.Same(last, source.SelectedItem); Assert.Equal(1, changes);
        proxy.IsDropDownOpen = false; host.Layout();
        Assert.False(source.IsQuickAccessOpen); Assert.Null(viewport.Content);
        Assert.IsType<ScrollViewer>(presenter.Parent); Assert.Equal(2, source.Items.Count);
        Assert.Same(last, source.SelectedItem); Assert.Equal(1, changes); Assert.Equal(1, cancelled);
        source.IsEnabled = false; Assert.False(proxy.IsEnabled);
    });

    [Fact]
    public void Grouped_bound_items_keep_groups_templates_live_updates_and_selection_binding() => Sta.Run(() =>
    {
        var model = new Model();
        var view = new CollectionViewSource { Source = model.Choices };
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Family)));
        var source = new InRibbonGallery { Header = "Theme", PopupWidth = 440, ItemsSource = view.View, IsSynchronizedWithCurrentItem = false };
        source.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(Model.Selected)) { Source = model, Mode = BindingMode.TwoWay });
        var ribbon = RibbonFor(source); ribbon.AddToQuickAccess(source);
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[0]);
        using var host = new Host(proxy);
        proxy.IsDropDownOpen = true; host.Layout();
        Assert.True(proxy.IsDropDownOpen);
        var viewport = Assert.IsAssignableFrom<ScrollViewer>(proxy.Items[0]);
        Assert.Equal(428, viewport.ActualWidth, 2);
        source.PopupWidth = 460; host.Layout(); Assert.Equal(448, viewport.ActualWidth, 2);
        source.SelectedItem = model.Choices[1]; Assert.Same(model.Choices[1], model.Selected);
        model.Choices.Add(new Choice("2013", "Modern")); host.Layout(); Assert.Equal(3, source.Items.Count);
        Assert.True(BindingOperations.IsDataBound(source, Selector.SelectedItemProperty));
        Assert.Equal(2, Assert.IsAssignableFrom<ICollectionView>(source.ItemsSource).Groups!.Count);
        var second = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.CreateCommandProxy(source, RibbonControlSize.Medium));
        host.Window.Content = null; host.Layout();
        host.Window.Content = new StackPanel { Children = { proxy, second } }; host.Layout();
        second.IsDropDownOpen = true; host.Layout();
        Assert.False(proxy.IsDropDownOpen); Assert.True(second.IsDropDownOpen);
        second.IsDropDownOpen = false; host.Layout(); Assert.Same(model.Selected, source.SelectedItem);
    });

    [Fact]
    public void Declared_qat_catalog_custom_group_and_persistence_restore_gallery_sources() => Sta.Run(() =>
    {
        var source = new InRibbonGallery { Header = "Theme" }; source.Items.Add("Crystal");
        Ribbon.SetCommandId(source, "theme"); var ribbon = RibbonFor(source);
        Assert.Same(source, Assert.Single(RibbonCommandCatalog.CollectAvailable(ribbon)).Control);
        ribbon.AddToQuickAccess(source);
        var custom = new RibbonGroup { Header = "Custom" }; Ribbon.SetIsCustom(custom, true); Ribbon.SetCommandId(custom, "custom:group");
        custom.Items.Add(ribbon.CreateCommandProxy(source, RibbonControlSize.Medium)); ribbon.Tabs[0].Groups.Add(custom);
        string json = RibbonCustomizationSerializer.Serialize(ribbon);
        var fresh = new InRibbonGallery { Header = "Theme" }; fresh.Items.Add("2024"); Ribbon.SetCommandId(fresh, "theme");
        var restored = RibbonFor(fresh); RibbonCustomizationSerializer.Apply(restored, json);
        Assert.Same(fresh, Ribbon.GetQuickAccessSource(Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(restored.QuickAccessItems))));
        Assert.Same(fresh, Ribbon.GetQuickAccessSource(Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(restored.Tabs[0].Groups[1].Items))));
        var only = new RibbonGallery { Header = "Only", ItemsSource = new[] { "A" } }; Ribbon.SetCommandId(only, "only");
        restored.QuickAccessItems.Add(only); Pump();
        Assert.IsAssignableFrom<RibbonDropDownButton>(restored.QuickAccessItems[^1]);
        restored.QuickAccessItems.Clear(); Assert.Contains(RibbonCommandCatalog.CollectAvailable(restored), e => ReferenceEquals(e.Control, only));
        Assert.True(restored.AddToQuickAccess(only)); Assert.IsAssignableFrom<RibbonDropDownButton>(restored.QuickAccessItems[0]);
    });

    [Fact]
    public void Removing_an_open_proxy_and_opening_the_native_gallery_returns_the_presenter() => Sta.Run(() =>
    {
        var source = new InRibbonGallery { Header = "Styles", Width = 240, ItemsSource = new[] { "A", "B" } };
        var ribbon = RibbonFor(source); ribbon.AddToQuickAccess(source);
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[0]);
        using var host = new Host(ribbon);
        proxy.IsDropDownOpen = true; host.Layout(); Assert.True(source.IsQuickAccessOpen);
        source.IsDropDownOpen = true; host.Layout(); Assert.False(proxy.IsDropDownOpen); Assert.False(source.IsQuickAccessOpen);
        source.IsDropDownOpen = false; proxy.IsDropDownOpen = true; host.Layout();
        ribbon.QuickAccessItems.Clear(); host.Layout(); Assert.False(proxy.IsDropDownOpen); Assert.False(source.IsQuickAccessOpen);
        var presenter = Assert.IsType<ItemsPresenter>(source.Template.FindName("PART_ItemsPresenter", source));
        Assert.Same(presenter, Assert.IsType<ScrollViewer>(source.Template.FindName("PART_ScrollViewer", source)).Content);
    });

    [Fact]
    public void Selected_tile_is_revealed_when_the_source_tab_becomes_visible_after_a_qat_pick() => Sta.Run(() =>
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            var source = new InRibbonGallery { Header = "Styles", Width = 180, PopupWidth = 320 };
            for (int i = 0; i < 12; i++) source.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 70, Height = 40 } });
            var ribbon = RibbonFor(source);
            var home = new RibbonTab { Header = "Other" }; home.Groups.Add(new RibbonGroup()); ribbon.Tabs.Add(home); ribbon.SelectedTab = home;
            ribbon.AddToQuickAccess(source); using var host = new Host(ribbon);
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[0]);
            proxy.IsDropDownOpen = true; host.Layout();
            var last = Assert.IsType<RibbonGalleryItem>(source.Items[^1]); last.IsSelected = true;
            proxy.IsDropDownOpen = false; host.Layout();
            ribbon.SelectedTab = ribbon.Tabs[0]; host.Layout();
            var viewport = Assert.IsType<ScrollViewer>(source.Template.FindName("PART_ScrollViewer", source));
            var chrome = Assert.IsType<Border>(last.Template.FindName("Chrome", last));
            var bounds = chrome.TransformToVisual(viewport).TransformBounds(new Rect(chrome.RenderSize));
            Assert.True(bounds.Top >= -1 && bounds.Bottom <= viewport.ActualHeight + 1, $"bounds={bounds}, viewport={viewport.ActualHeight}");
            Assert.Same(last, source.SelectedItem);
        }
        finally { RibbonAnimation.GlobalLevel = motion; }
    });

    [Fact]
    public void Gallery_discovery_preserves_existing_button_and_combo_automatic_ids() => Sta.Run(() =>
    {
        var ribbon = RibbonFor(new RibbonGallery { Header = "Styles" });
        var save = new RibbonButton { Header = "Save" }; var combo = new RibbonComboBox { Header = "Size" };
        ribbon.Tabs[0].Groups[0].Items.Add(combo); ribbon.Tabs[0].Groups[0].Items.Add(save);
        RibbonCustomizationSerializer.Apply(ribbon, "{\"Tabs\":[],\"QuickAccess\":[{\"Ref\":\"auto:tab.home/group.gallery/Save#0\"},{\"Ref\":\"auto:combo:tab.home/group.gallery/Size#0\"}]}");
        Assert.Equal(2, ribbon.QuickAccessItems.Count);
        Assert.Same(save, Ribbon.GetQuickAccessSource((DependencyObject)ribbon.QuickAccessItems[0]));
        Assert.Same(combo, Ribbon.GetQuickAccessSource((DependencyObject)ribbon.QuickAccessItems[1]));
    });

    [Theory]
    [InlineData(RibbonTheme.Office2024, RibbonDensity.Compact, 1.25, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2024, RibbonDensity.Touch, 2, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, RibbonDensity.Compact, 2, FlowDirection.RightToLeft)]
    [InlineData(RibbonTheme.CrystalLight, RibbonDensity.Touch, 1.25, FlowDirection.LeftToRight)]
    public void Visible_source_strip_keeps_its_tiles_while_qat_or_overflow_gallery_is_open(RibbonTheme theme, RibbonDensity density, double scale, FlowDirection flow) => Sta.Run(() =>
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            var source = new InRibbonGallery { Header = "Styles", Width = 240, PopupWidth = 440, SelectedIndex = 5 };
            for (int i = 0; i < 9; i++)
            {
                var content = new StackPanel { Width = 62 };
                content.Children.Add(new Border { Width = 58, Height = 22, Background = i == 5 || i % 3 == 0 ? Brushes.Red : Brushes.SeaGreen });
                content.Children.Add(new TextBlock { Text = $"Style {i}", FontSize = 11 });
                source.Items.Add(new RibbonGalleryItem { Content = content });
            }
            var ribbon = RibbonFor(source); ribbon.Density = density; ribbon.FlowDirection = flow; ribbon.SelectedTab = ribbon.Tabs[0]; ribbon.AddToQuickAccess(source);
            using var host = new Host(ribbon); ThemeManager.Apply(Application.Current, theme);
            VisualTreeHelper.SetRootDpi(host.Window, new DpiScale(scale, scale)); host.Layout();
            var strip = Assert.IsType<ScrollViewer>(source.Template.FindName("PART_ScrollViewer", source));
            var live = strip.Content;
            BitmapSource before = Render(strip, scale);
            Save(before, $"{theme}-{density}-strip-before");
            Assert.True(RedPixels(before) > 200, $"source visible={source.IsVisible}, size={source.RenderSize}, selected={source.SelectedIndex}, strip={strip.RenderSize}, viewport={strip.ViewportHeight}, offset={strip.VerticalOffset}, red={RedPixels(before)}");
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[0]);
            proxy.IsDropDownOpen = true; host.Layout();
            BitmapSource during = Render(strip, scale);
            Save(before, $"{theme}-{density}-strip-before"); Save(during, $"{theme}-{density}-strip-qat-open");
            Assert.True(RedPixels(during) >= RedPixels(before) * .9, $"Strip lost visible swatches: before={RedPixels(before)}, open={RedPixels(during)}");
            Rect beforeBounds = RedBounds(before), duringBounds = RedBounds(during);
            Assert.Equal(beforeBounds.Left, duringBounds.Left, 0); Assert.Equal(beforeBounds.Top, duringBounds.Top, 0);
            Assert.Equal(beforeBounds.Width, duringBounds.Width, 0); Assert.Equal(beforeBounds.Height, duringBounds.Height, 0);
            Assert.Equal(before.PixelWidth, during.PixelWidth); Assert.Equal(before.PixelHeight, during.PixelHeight);
            Assert.Same(source.Items[5], source.SelectedItem);
            proxy.IsDropDownOpen = false; host.Layout(); Assert.Same(live, strip.Content);

            ribbon.QuickAccessItems.Clear();
            for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small, Icon = new DrawingImage() });
            ribbon.QuickAccessMaxWidth = 55; ribbon.AddToQuickAccess(source); host.Layout();
            var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            var toolbar = Assert.IsType<RibbonQuickAccessToolBar>(tabs.Template.FindName("QatTabRowHost", tabs));
            var overflow = Assert.IsType<ToggleButton>(toolbar.Template.FindName("PART_OverflowButton", toolbar));
            overflow.IsChecked = true; host.Layout();
            var entries = Assert.IsType<ItemsControl>(toolbar.Template.FindName("PART_OverflowHost", toolbar));
            var entry = Assert.Single(entries.Items.OfType<RibbonDropDownButton>());
            entry.IsDropDownOpen = true; host.Layout();
            Assert.True(RedPixels(Render(strip, scale)) >= RedPixels(before) * .9);
            var last = Assert.IsType<RibbonGalleryItem>(source.Items[^1]); last.IsSelected = true;
            overflow.IsChecked = false; host.Layout(); Assert.Same(live, strip.Content); Assert.Same(last, source.SelectedItem);
            var chrome = Assert.IsType<Border>(last.Template.FindName("Chrome", last));
            var bounds = chrome.TransformToVisual(strip).TransformBounds(new Rect(chrome.RenderSize));
            Assert.True(bounds.Top >= -1 && bounds.Bottom <= strip.ActualHeight + 1);
        }
        finally { RibbonAnimation.GlobalLevel = motion; }
    });

    private static BitmapSource Render(FrameworkElement element, double scale)
    {
        // Render through the neutral window root: rendering an RTL viewport by
        // itself also applies its parent-facing mirror, unlike the visible UI.
        var window = Window.GetWindow(element)!;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * scale), (int)Math.Ceiling(window.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(window);
        Rect bounds = element.TransformToAncestor(window).TransformBounds(new Rect(element.RenderSize));
        int left = (int)Math.Floor(bounds.Left * scale), top = (int)Math.Floor(bounds.Top * scale);
        return new CroppedBitmap(bitmap, new Int32Rect(left, top, (int)Math.Ceiling(bounds.Right * scale) - left, (int)Math.Ceiling(bounds.Bottom * scale) - top));
    }
    private static int RedPixels(BitmapSource bitmap)
    {
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        int count = 0; for (int i = 0; i < pixels.Length; i += 4) if (pixels[i + 2] > 180 && pixels[i + 1] < 80 && pixels[i] < 80 && pixels[i + 3] > 180) count++;
        return count;
    }
    private static void Save(BitmapSource bitmap, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_QAT_DIAGNOSTICS"); if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }

    private static Rect RedBounds(BitmapSource bitmap)
    {
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        Rect bounds = Rect.Empty;
        for (int y = 0; y < bitmap.PixelHeight; y++) for (int x = 0; x < bitmap.PixelWidth; x++)
        {
            int i = (y * bitmap.PixelWidth + x) * 4;
            if (pixels[i + 2] > 180 && pixels[i + 1] < 80 && pixels[i] < 80 && pixels[i + 3] > 180)
                bounds.Union(new Rect(x, y, 1, 1));
        }
        return bounds;
    }

    private sealed record Choice(string Name, string Family);
    private sealed class Model
    {
        public ObservableCollection<Choice> Choices { get; } = new() { new("Crystal", "Crystal"), new("2024", "Modern") };
        public Choice? Selected { get; set; }
    }
    private static Ribbon RibbonFor(RibbonGallery source)
    {
        var group = new RibbonGroup { Header = "Gallery" }; group.Items.Add(source);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        Ribbon.SetCommandId(tab, "tab.home"); Ribbon.SetCommandId(group, "group.gallery");
        var ribbon = new Ribbon(); ribbon.Tabs.Add(tab); return ribbon;
    }
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private sealed class Host : IDisposable
    {
        internal Window Window { get; }
        internal Host(object content)
        {
            Sta.UseApplication();
            Window = new Window { Content = content, Width = 800, Height = 600, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal void Layout() { Pump(); Window.UpdateLayout(); Pump(); }
        public void Dispose() { Window.Close(); Pump(); Sta.ResetApplication(); }
    }
}
