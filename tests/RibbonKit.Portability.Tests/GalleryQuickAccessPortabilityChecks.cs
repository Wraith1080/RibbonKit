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
            VerifyScreenTips(application);
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
                Rect beforeStripBounds = Bounds(strip, window);
                Assert.True(RedBounds(before).Width > 50, $"{theme}/{density}/{flow}/{scale}/{placement}: visible={source.IsVisible}, selected={source.SelectedIndex}, viewport={strip.RenderSize}, offset={strip.VerticalOffset}, red={RedBounds(before)}");
                proxy.IsDropDownOpen = true; Layout(window); Assert.True(proxy.IsDropDownOpen);
                var during = RenderStrip(strip, window, scale);
                AssertStripPreserved(before, during, beforeStripBounds, Bounds(strip, window),
                    $"{theme}-{density}-{flow}-{scale}-{placement}");
                Assert.Same(choices[5], source.SelectedItem);
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
            Rect originalStripBounds = Bounds(sourceStrip, window);
            var toolbar = Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost");
            var overflow = Part<ToggleButton>(toolbar, "PART_OverflowButton"); overflow.IsChecked = true; Layout(window);
            var entry = Assert.Single(Part<ItemsControl>(toolbar, "PART_OverflowHost").Items.OfType<RibbonDropDownButton>());
            entry.IsDropDownOpen = true; Layout(window);
            AssertStripPreserved(original, RenderStrip(sourceStrip, window, 2), originalStripBounds,
                Bounds(sourceStrip, window), "overflow");
            source.SelectedItem = choices[^1]; overflow.IsChecked = false; Layout(window);
            Assert.Same(sourcePresenter, sourceStrip.Content); Assert.Same(choices[^1], source.SelectedItem);
            var last = Assert.IsType<RibbonGalleryItem>(source.ItemContainerGenerator.ContainerFromItem(choices[^1]));
            var bounds = Bounds(Part<Border>(last, "Chrome"), sourceStrip);
            Assert.True(bounds.Top >= -1 && bounds.Bottom <= sourceStrip.ActualHeight + 1);
        }
        finally { window.Close(); Layout(window); }
    }

    private static void AssertStripPreserved(BitmapSource before, BitmapSource during,
        Rect beforeViewport, Rect duringViewport, string scenario)
    {
        Rect beforeRed = RedBounds(before), duringRed = RedBounds(during);
        if (beforeRed != duringRed)
        {
            Save(before, scenario + "-before"); Save(during, scenario + "-open");
            Console.WriteLine($"Gallery strip {scenario}: red before={beforeRed}, open={duringRed}; " +
                $"viewport before={beforeViewport}, open={duringViewport}");
        }
        Assert.True(beforeViewport == duringViewport,
            $"{scenario}: viewport moved from {beforeViewport} to {duringViewport}");
        Assert.Equal(before.PixelWidth, during.PixelWidth); Assert.Equal(before.PixelHeight, during.PixelHeight);
        Assert.False(beforeRed.IsEmpty, $"{scenario}: source swatch is missing");
        Assert.False(duringRed.IsEmpty, $"{scenario}: frozen swatch is missing");
        Assert.Equal(beforeRed.Size, duringRed.Size);
        // Live visuals and the frozen DrawingImage can snap their identical
        // logical geometry to adjacent pixels at fractional DPI. Keep size and
        // layout exact; allow at most one output-bitmap pixel of translation.
        Assert.True(Math.Abs(beforeRed.X - duringRed.X) <= 1 && Math.Abs(beforeRed.Y - duringRed.Y) <= 1,
            $"{scenario}: swatch moved from {beforeRed} to {duringRed}");
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
            // Overflow reserves Up/Down for rows; Forward enters the gallery submenu.
            Press(Key.Right); Layout(window); Assert.True(entry.IsDropDownOpen);
            Assert.Same(source.Items[1], Keyboard.FocusedElement);
            Press(Key.Right); Layout(window); Assert.Equal(2, source.SelectedIndex);
            Assert.Same(source.Items[2], Keyboard.FocusedElement); Assert.True(entry.IsDropDownOpen);
            Press(Key.Enter); Layout(window); Assert.False(entry.IsDropDownOpen); Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Right); Layout(window); Press(Key.Escape); Layout(window); Assert.False(entry.IsDropDownOpen); Assert.True(overflowButton.IsChecked);
            Press(Key.Right); Layout(window); Press(Key.End); Layout(window); Assert.Same(source.Items[^1], Keyboard.FocusedElement);
            Press(Key.Space); Layout(window); Assert.Same(source.Items[^1], source.SelectedItem); Assert.False(entry.IsDropDownOpen);
            source.SelectedIndex = 0;
            Press(Key.Right); Layout(window); Assert.Same(source.Items[1], Keyboard.FocusedElement);
            Press(Key.Enter); Layout(window); Assert.Equal(1, source.SelectedIndex);
            Press(Key.Right); Layout(window); Assert.True(entry.IsDropDownOpen);
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

    private static void VerifyScreenTips(Application application)
    {
        foreach (bool inRibbon in new[] { false, true })
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
            RibbonGallery source = inRibbon ? new InRibbonGallery { Width = 240 } : new RibbonGallery { Width = 240 };
            source.Header = "Styles"; source.ScreenTipTitle = "Text styles";
            source.ScreenTipText = "Choose a style for the selected text.";
            var tile = new RibbonGalleryItem { Content = "Heading", ToolTip = "Heading 1" }; source.Items.Add(tile);
            var ribbon = RibbonFor(source);
            var customTab = new RibbonTab { Header = "Custom" }; Ribbon.SetIsCustom(customTab, true);
            var customGroup = new RibbonGroup { Header = "Custom styles" }; Ribbon.SetIsCustom(customGroup, true);
            customTab.Groups.Add(customGroup); ribbon.Tabs.Add(customTab); ribbon.SelectedTab = customTab;
            var customize = new RibbonCustomizePage { Ribbon = ribbon, Height = 200 };
            var window = new RibbonWindow { Content = new StackPanel { Children = { ribbon, customize } },
                Width = 900, Height = 620, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            try
            {
                window.Show(); Layout(window);
                var available = Part<ListBox>(customize, "PART_AvailableList");
                available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(entry => ReferenceEquals(entry.Control, source));
                Part<TreeView>(customize, "PART_Tree").Items.Cast<RibbonCustomizeNode>().Single(node => node.IsCustom)
                    .Children.Single().IsSelected = true;
                Layout(window); Part<ButtonBase>(customize, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
                var custom = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(customGroup.Items));
                Assert.True(ribbon.AddToQuickAccess(custom));
                var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
                foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
                foreach (bool dark in new[] { false, true })
                foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
                {
                    ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                    window.FlowDirection = flow; ribbon.QuickAccessMaxWidth = 600;
                    ribbon.SelectedTab = ribbon.Tabs[1]; Layout(window);
                    VerifyRenderedTip(source, source.ScreenTipTitle, source.ScreenTipText, window);
                    ribbon.SelectedTab = customTab; Layout(window);
                    VerifyRenderedTip(custom, source.ScreenTipTitle, source.ScreenTipText, window);
                    VerifyRenderedTip(qat, source.ScreenTipTitle, source.ScreenTipText, window,
                        inRibbon && !dark && flow == FlowDirection.LeftToRight && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight
                            ? $"{theme}-gallery-screentip" : null);
                    ribbon.QuickAccessMaxWidth = 1; Layout(window);
                    var toolbar = Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost");
                    Part<ToggleButton>(toolbar, "PART_OverflowButton").IsChecked = true; Layout(window);
                    var overflow = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(Part<ItemsControl>(toolbar, "PART_OverflowHost").Items));
                    VerifyRenderedTip(overflow, source.ScreenTipTitle, source.ScreenTipText, window);
                    overflow.IsDropDownOpen = true; Layout(window);
                    Assert.Equal("Heading 1", tile.ToolTip);
                    Assert.True(tile.ActualWidth > 0);
                    overflow.IsDropDownOpen = false;
                    Part<ToggleButton>(toolbar, "PART_OverflowButton").IsChecked = false; Layout(window);
                }
            }
            finally { window.Close(); Layout(window); }
        }
    }

    private static void VerifyRenderedTip(FrameworkElement owner, string? title, string? description, Window window, string? captureName = null)
    {
        var tip = Assert.IsType<RibbonScreenTip>(owner.ToolTip);
        tip.PlacementTarget = owner; tip.IsOpen = true;
        try
        {
            Layout(window); tip.UpdateLayout();
            Assert.NotNull(PresentationSource.FromVisual(tip));
            Assert.Equal(title, Part<TextBlock>(tip, "TitleText").Text);
            Assert.Equal(description, Part<TextBlock>(tip, "DescriptionText").Text);
            Assert.True(tip.ActualWidth > 0 && tip.ActualHeight > 0);
            Assert.Same(tip.FindResource("RibbonKit.Brushes.ScreenTip.Surface"), tip.Background);
            Assert.Equal(owner.FlowDirection, tip.FlowDirection);
            if (captureName is not null) Save(tip, captureName);
        }
        finally { tip.IsOpen = false; Layout(window); }
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
