using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// A separate consumer using only public RibbonKit controls and shared resources.
internal static class GalleryQuickAccessPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            string? scope = Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE");
            if (scope is not "GalleryQuickAccessKeyboard" and not "GalleryQuickAccessVisible") VerifyPlacements(application);
            if (scope != "GalleryQuickAccessKeyboard") VerifyVisibleStrips(application);
            VerifyInputAndOverflow(application);
        }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void VerifyPlacements(Application application)
    {
        var choices = new ObservableCollection<Choice>
        {
            new("Crystal", "Modern"), new("Office 2024", "Office Modern"), new("Office 2019", "Office Modern"),
            new("Office 2013", "Office Modern"), new("Office 2010", "Office Legacy"), new("Office 2007", "Office Legacy"),
        };
        var view = new ListCollectionView(choices); view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Choice.Family)));
        var text = new FrameworkElementFactory(typeof(TextBlock)); text.SetBinding(TextBlock.TextProperty, new Binding(nameof(Choice.Name)));
        text.SetValue(FrameworkElement.WidthProperty, 112d); text.SetValue(FrameworkElement.HeightProperty, 40d);
        var source = new InRibbonGallery { Header = "Theme", Icon = Icon(), Width = 158, PopupWidth = 440,
            ItemsSource = view, ItemTemplate = new DataTemplate { VisualTree = text }, IsSynchronizedWithCurrentItem = false, SelectedIndex = 0 };
        var ribbon = RibbonFor(source);
        var qatPage = new RibbonQuickAccessPage { Ribbon = ribbon }; var customizePage = new RibbonCustomizePage { Ribbon = ribbon };
        var root = new StackPanel { Children = { ribbon, qatPage, customizePage } };
        var window = new RibbonWindow { Content = root, Width = 900, Height = 620, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        try
        {
            window.Show(); Layout(window); Assert.False(source.IsLoaded);
            foreach (var page in new Control[] { qatPage, customizePage })
                Assert.Contains(Part<ListBox>(page, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), e => ReferenceEquals(e.Control, source) && ReferenceEquals(e.Icon, source.Icon));
            var available = Part<ListBox>(qatPage, "PART_AvailableList");
            available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(e => ReferenceEquals(e.Control, source));
            Part<ButtonBase>(qatPage, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1.25, 2d })
            foreach (RibbonQuickAccessPosition placement in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; ribbon.FlowDirection = flow; ribbon.QuickAccessPosition = placement;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); Layout(window);
                proxy.IsDropDownOpen = true; Layout(window);
                Assert.True(Part<Popup>(proxy, "PART_Popup").IsOpen); Assert.Same(source.Icon, proxy.Icon);
                var viewport = Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(proxy.Items));
                var presenter = Assert.IsType<ItemsPresenter>(viewport.Content);
                Assert.Equal(428, viewport.ActualWidth, 2); Assert.True(viewport.ViewportHeight > 200, $"{theme}/{density}/{flow}/{placement}: viewport={viewport.ViewportWidth}x{viewport.ViewportHeight}, extent={viewport.ExtentHeight}");
                var groups = Descendants<GroupItem>(presenter).ToArray(); Assert.Equal(3, groups.Length);
                double previous = 0;
                foreach (var group in groups)
                {
                    var heading = Part<Border>(group, "GroupHeaderHost"); Assert.Equal(Visibility.Visible, heading.Visibility);
                    Assert.Same(heading.FindResource("RibbonKit.Brushes.ApplicationMenu.HeaderBackground"), heading.Background);
                    Rect headingBounds = Bounds(heading, viewport); Assert.True(headingBounds.Top >= previous - 1);
                    var tiles = Descendants<RibbonGalleryItem>(group).ToArray();
                    var bounds = tiles.Select(t => Bounds(Part<Border>(t, "Chrome"), viewport)).OrderBy(r => r.Left).ToArray();
                    Assert.All(bounds, b => { Assert.Equal(bounds[0].Top, b.Top, 2); Assert.True(b.Top >= headingBounds.Bottom - 1); Assert.True(b.Left >= -1 && b.Right <= viewport.ViewportWidth + 1); });
                    for (int i = 1; i < bounds.Length; i++) Assert.True(bounds[i - 1].Right <= bounds[i].Left + 1);
                    Assert.All(tiles, tile => { Assert.Equal(flow, tile.FlowDirection); Assert.Equal(density, Ribbon.GetDensity(tile)); });
                    previous = bounds.Max(b => b.Bottom);
                }
                var last = Assert.IsAssignableFrom<RibbonGalleryItem>(source.ItemContainerGenerator.ContainerFromItem(choices[^1]));
                Assert.Contains(Descendants<TextBlock>(last), t => t.Text == "Office 2007");
                var viewportPeer = UIElementAutomationPeer.CreatePeerForElement(viewport);
                Assert.IsAssignableFrom<ISelectionProvider>(viewportPeer.GetPattern(PatternInterface.Selection));
                var choicePeer = AutomationChildren(viewportPeer).Last(peer => peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider);
                ((ISelectionItemProvider)choicePeer.GetPattern(PatternInterface.SelectionItem)).Select();
                Assert.Same(choices[^1], source.SelectedItem);
                if (!dark && flow == FlowDirection.LeftToRight && scale == 1.25 && placement == RibbonQuickAccessPosition.TitleBar
                    && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight)
                    Save(Part<FrameworkElement>(proxy, "PART_MenuHost"), $"{theme}-{density}-gallery-qat");
                proxy.IsDropDownOpen = false; Layout(window); Assert.Null(viewport.Content);
                Assert.Same(presenter, Part<ScrollViewer>(source, "PART_ScrollViewer").Content);
                Assert.Same(choices[^1], source.SelectedItem);
            }
        }
        finally { window.Close(); Layout(window); }
    }

    private static void VerifyVisibleStrips(Application application)
    {
        var panel = new FrameworkElementFactory(typeof(StackPanel)); panel.SetValue(FrameworkElement.WidthProperty, 62d);
        var swatch = new FrameworkElementFactory(typeof(Border)); swatch.SetValue(FrameworkElement.WidthProperty, 58d);
        swatch.SetValue(FrameworkElement.HeightProperty, 22d); swatch.SetBinding(Border.BackgroundProperty, new Binding(nameof(Swatch.Color))); panel.AppendChild(swatch);
        var text = new FrameworkElementFactory(typeof(TextBlock)); text.SetBinding(TextBlock.TextProperty, new Binding(nameof(Swatch.Name)));
        text.SetValue(TextBlock.FontSizeProperty, 11d); panel.AppendChild(text);
        var choices = Enumerable.Range(0, 9).Select(i => new Swatch($"Style {i}", i == 5 ? Brushes.Red : Brushes.SeaGreen)).ToArray();
        var source = new InRibbonGallery { Header = "Styles", Icon = Icon(), Width = 240, PopupWidth = 440,
            ItemsSource = choices, ItemTemplate = new DataTemplate { VisualTree = panel }, IsSynchronizedWithCurrentItem = false, SelectedIndex = 5 };
        var ribbon = RibbonFor(source); ribbon.SelectedTab = ribbon.Tabs[1]; ribbon.AddToQuickAccess(source);
        var window = new RibbonWindow { Content = ribbon, Width = 900, Height = 620, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(window);
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            foreach (RibbonTheme theme in new[] { RibbonTheme.Office2024, RibbonTheme.CrystalLight })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1.25, 2d })
            foreach (RibbonQuickAccessPosition placement in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, false);
                ribbon.Density = density; ribbon.FlowDirection = flow; ribbon.QuickAccessPosition = placement;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); source.SelectedIndex = 5; Layout(window);
                var strip = Part<ScrollViewer>(source, "PART_ScrollViewer"); var live = strip.Content;
                var before = RenderStrip(strip, window, scale);
                Assert.True(RedBounds(before).Width > 50, $"{theme}/{density}/{flow}/{scale}/{placement}: visible={source.IsVisible}, selected={source.SelectedIndex}, viewport={strip.RenderSize}, offset={strip.VerticalOffset}, red={RedBounds(before)}");
                proxy.IsDropDownOpen = true; Layout(window); Assert.True(proxy.IsDropDownOpen);
                var during = RenderStrip(strip, window, scale);
                Assert.Equal(RedBounds(before), RedBounds(during)); Assert.Same(choices[5], source.SelectedItem);
                if (flow == FlowDirection.RightToLeft && scale == 2 && placement == RibbonQuickAccessPosition.TitleBar)
                {
                    Save(before, $"{theme}-{density}-visible-strip-before"); Save(during, $"{theme}-{density}-visible-strip-qat-open");
                }
                proxy.IsDropDownOpen = false; Layout(window); Assert.Same(live, strip.Content);
            }

            ribbon.QuickAccessItems.Clear();
            for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Icon = Icon(), Size = RibbonControlSize.Small });
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 55; ribbon.AddToQuickAccess(source); Layout(window);
            var sourceStrip = Part<ScrollViewer>(source, "PART_ScrollViewer"); var sourcePresenter = sourceStrip.Content;
            var original = RenderStrip(sourceStrip, window, 2);
            var toolbar = Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost");
            var overflow = Part<ToggleButton>(toolbar, "PART_OverflowButton"); overflow.IsChecked = true; Layout(window);
            var entry = Assert.Single(Part<ItemsControl>(toolbar, "PART_OverflowHost").Items.OfType<RibbonDropDownButton>());
            entry.IsDropDownOpen = true; Layout(window);
            Assert.Equal(RedBounds(original), RedBounds(RenderStrip(sourceStrip, window, 2)));
            source.SelectedItem = choices[^1]; overflow.IsChecked = false; Layout(window);
            Assert.Same(sourcePresenter, sourceStrip.Content); Assert.Same(choices[^1], source.SelectedItem);
            var last = Assert.IsType<RibbonGalleryItem>(source.ItemContainerGenerator.ContainerFromItem(choices[^1]));
            var bounds = Bounds(Part<Border>(last, "Chrome"), sourceStrip);
            Assert.True(bounds.Top >= -1 && bounds.Bottom <= sourceStrip.ActualHeight + 1);
        }
        finally { window.Close(); Layout(window); }
    }

    private static BitmapSource RenderStrip(FrameworkElement element, Window window, double scale)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * scale), (int)Math.Ceiling(window.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(window);
        Rect bounds = Bounds(element, window);
        int left = (int)Math.Floor(bounds.Left * scale), top = (int)Math.Floor(bounds.Top * scale);
        return new CroppedBitmap(bitmap, new Int32Rect(left, top, (int)Math.Ceiling(bounds.Right * scale) - left, (int)Math.Ceiling(bounds.Bottom * scale) - top));
    }

    private static Rect RedBounds(BitmapSource bitmap)
    {
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        Rect bounds = Rect.Empty;
        for (int y = 0; y < bitmap.PixelHeight; y++) for (int x = 0; x < bitmap.PixelWidth; x++)
        {
            int i = (y * bitmap.PixelWidth + x) * 4;
            if (pixels[i + 2] > 180 && pixels[i + 1] < 80 && pixels[i] < 80 && pixels[i + 3] > 180) bounds.Union(new Rect(x, y, 1, 1));
        }
        return bounds;
    }

    private static void VerifyInputAndOverflow(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
        var source = new InRibbonGallery { Header = "Styles", Icon = Icon(), Width = 120, PopupWidth = 280 };
        for (int i = 0; i < 15; i++) source.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 64, Height = 36, Background = i == 0 ? Brushes.Red : Brushes.SteelBlue }, IsEnabled = i != 0 });
        source.SelectedIndex = 1;
        var ribbon = RibbonFor(source); ribbon.QuickAccessMaxWidth = 55;
        for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Icon = Icon(), Size = RibbonControlSize.Small });
        ribbon.AddToQuickAccess(source);
        var window = new Window { Content = ribbon, Width = 900, Height = 620, Left = -10000, Top = -10000, ShowActivated = true, ShowInTaskbar = false };
        try
        {
            window.Show(); window.Activate(); Layout(window);
            var toolbar = Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost");
            var overflowButton = Part<ToggleButton>(toolbar, "PART_OverflowButton"); overflowButton.IsChecked = true; Layout(window);
            var entry = Assert.Single(Part<ItemsControl>(toolbar, "PART_OverflowHost").Items.OfType<RibbonDropDownButton>());
            var opener = Part<ToggleButton>(entry, "PART_Toggle"); Assert.True(opener.Focus());
            Press(Key.Down); Layout(window); Assert.True(entry.IsDropDownOpen);
            Assert.Same(source.Items[1], Keyboard.FocusedElement);
            Press(Key.Right); Layout(window); Assert.Equal(2, source.SelectedIndex);
            Assert.Same(source.Items[2], Keyboard.FocusedElement); Assert.True(entry.IsDropDownOpen);
            Press(Key.Enter); Layout(window); Assert.False(entry.IsDropDownOpen); Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Down); Layout(window); Press(Key.Escape); Layout(window); Assert.False(entry.IsDropDownOpen); Assert.True(overflowButton.IsChecked);
            Press(Key.Up); Layout(window); Assert.Same(source.Items[^1], Keyboard.FocusedElement);
            Press(Key.Space); Layout(window); Assert.Same(source.Items[^1], source.SelectedItem); Assert.False(entry.IsDropDownOpen);
            source.SelectedIndex = 0;
            Press(Key.Down); Layout(window); Assert.Same(source.Items[1], Keyboard.FocusedElement);
            Press(Key.Enter); Layout(window); Assert.Equal(1, source.SelectedIndex);
            Press(Key.Down); Layout(window); Assert.True(entry.IsDropDownOpen);
            overflowButton.IsChecked = false; Layout(window); Assert.False(entry.IsDropDownOpen);
            Assert.Equal(15, source.Items.Count);
            Assert.NotNull(Part<ScrollViewer>(source, "PART_ScrollViewer").Content);
            ribbon.QuickAccessMaxWidth = 600; Layout(window);
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[^1]); proxy.IsDropDownOpen = true; Layout(window);
            var tile = Assert.IsAssignableFrom<RibbonGalleryItem>(source.Items[5]);
            int previews = 0, cancellations = 0, changes = 0;
            source.ItemPreview += (_, e) => { Assert.Same(tile, e.PreviewedItem); previews++; };
            source.ItemPreviewCancelled += (_, _) => cancellations++;
            source.SelectionChanged += (_, _) => changes++;
            tile.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
            tile.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent });
            tile.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.MouseUpEvent });
            Layout(window); Assert.Same(tile, source.SelectedItem); Assert.Equal(1, changes); Assert.Equal(1, previews);
            Assert.False(proxy.IsDropDownOpen); Assert.Equal(1, cancellations);
            proxy.IsDropDownOpen = true; Layout(window); ribbon.QuickAccessItems.Remove(proxy); Layout(window);
            Assert.False(proxy.IsDropDownOpen); Assert.NotNull(Part<ScrollViewer>(source, "PART_ScrollViewer").Content);
        }
        finally { window.Close(); Layout(window); }
    }

    private sealed record Choice(string Name, string Family);
    private sealed record Swatch(string Name, Brush Color);
    private static Ribbon RibbonFor(RibbonGallery source)
    {
        var ribbon = new Ribbon(); var home = new RibbonTab { Header = "Home" }; home.Groups.Add(new RibbonGroup { Header = "Tools" });
        var other = new RibbonTab { Header = "Other" }; var group = new RibbonGroup { Header = "Gallery" }; group.Items.Add(source); other.Groups.Add(group);
        ribbon.Tabs.Add(home); ribbon.Tabs.Add(other); ribbon.SelectedTab = home; return ribbon;
    }
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static void Layout(Window window) { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
    private static void Press(Key key)
    {
        var target = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement);
        var source = PresentationSource.FromVisual(target)!;
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyUpEvent });
    }
    private static Rect Bounds(FrameworkElement element, Visual relative) => element.TransformToVisual(relative).TransformBounds(new Rect(element.RenderSize));
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T match) yield return match; foreach (var item in Descendants<T>(child)) yield return item; }
    }
    private static IEnumerable<AutomationPeer> AutomationChildren(AutomationPeer parent)
    {
        foreach (var child in parent.GetChildren() ?? new List<AutomationPeer>()) { yield return child; foreach (var item in AutomationChildren(child)) yield return item; }
    }
    private static void Save(FrameworkElement element, string name)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32); bitmap.Render(element);
        Save(bitmap, name);
    }
    private static void Save(BitmapSource bitmap, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_QAT_DIAGNOSTICS"); if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
