using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Only public RibbonKit APIs/resources; no Showcase or internal access.
internal static class SimplifiedRibbonPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var command = new RibbonButton { Header = "Save", Icon = new DrawingImage() };
        var toggle = new RibbonToggleButton { Header = "Bold", Icon = new DrawingImage() };
        var editor = new RibbonTextBox { Header = "Find", Text = "Source editor" };
        var gallery = new RibbonGallery { Header = "Shapes", Icon = new DrawingImage(), Width = 160 };
        var strip = new InRibbonGallery { Header = "Styles", Icon = new DrawingImage(), Width = 160 };
        foreach (RibbonGallery source in new RibbonGallery[] { gallery, strip })
        {
            source.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 50, Height = 25, Background = Brushes.Red } });
            source.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 50, Height = 25, Background = Brushes.Blue } });
            source.SelectedIndex = 0;
            Ribbon.SetSimplifiedPlacement(source, RibbonSimplifiedPlacement.Inline);
        }
        var group = new RibbonGroup { Header = "Editing" };
        group.Items.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { command, toggle, editor, gallery, strip } });
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon(); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline);
        Ribbon.SetSimplifiedPlacement(toggle, RibbonSimplifiedPlacement.Inline);
        Ribbon.SetSimplifiedSize(command, RibbonControlSize.Small);
        Ribbon.SetSimplifiedSize(toggle, RibbonControlSize.Small);
        Ribbon.SetSimplifiedPriority(command, 100);
        var window = new RibbonWindow { Content = ribbon, Width = 900, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show(); Layout(window);
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection direction in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1, 1.25, 1.5, 2 })
            {
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = direction;
                ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(window);
                double classicHeight = ((FrameworkElement)tab.Content).ActualHeight;
                var scroller = Descendants(ribbon).OfType<FrameworkElement>().Single(element => element.Name == "PART_ContentScroll");
                Assert.Equal(ribbon.FindResource("RibbonKit.Metrics.GroupsRowMargin"), scroller.Margin);
                ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(window);
                var host = (RibbonGroupsHost)tab.Content;
                double expectedHeight = density == RibbonDensity.Compact ? 42 : 52;
                double pixel = 1 / VisualTreeHelper.GetDpi(host).DpiScaleY;
                Assert.InRange(host.ActualHeight, expectedHeight - pixel, expectedHeight + pixel);
                Assert.True(host.ActualHeight < classicHeight / 2);
                Assert.Equal(ribbon.FindResource("RibbonKit.Metrics.SimplifiedGroupsRowMargin"), scroller.Margin);
                var toolbar = Descendants(host).OfType<RibbonQuickAccessToolBar>().Single();
                double commandsHeight = host.ActualHeight - toolbar.Margin.Top - toolbar.Margin.Bottom;
                Assert.InRange(toolbar.ActualHeight, commandsHeight - pixel, commandsHeight + pixel);
                foreach (var inline in toolbar.Items.OfType<FrameworkElement>().Where(item =>
                             item is RibbonButton or RibbonToggleButton or RibbonDropDownButton { Header: "Shapes" or "Styles" }))
                    Assert.InRange(inline.ActualHeight, toolbar.ActualHeight - inline.Margin.Top - inline.Margin.Bottom - pixel, toolbar.ActualHeight + pixel);
                var actionCopy = toolbar.Items.OfType<RibbonButton>().Single();
                Assert.True(actionCopy.ActualWidth > 20);
                Assert.Equal(RibbonControlSize.Small, actionCopy.Size);
                Assert.Equal(Visibility.Visible, ((FrameworkElement)actionCopy.Template.FindName("SmallLayout", actionCopy)).Visibility);
                Assert.Equal(Visibility.Collapsed, ((FrameworkElement)actionCopy.Template.FindName("MediumLayout", actionCopy)).Visibility);
                Assert.Equal(direction, actionCopy.FlowDirection);
                int invoked = 0; RoutedEventHandler clicked = (_, _) => invoked++;
                command.Click += clicked;
                var peer = UIElementAutomationPeer.CreatePeerForElement(actionCopy)!;
                ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke(); Layout(window);
                command.Click -= clicked;
                Assert.Equal(1, invoked);
                var toggleCopy = toolbar.Items.OfType<RibbonToggleButton>().Single();
                toggleCopy.IsChecked = true; Assert.True(toggle.IsChecked); toggle.IsChecked = false;
                foreach (RibbonGallery source in new RibbonGallery[] { gallery, strip })
                {
                    var copy = toolbar.Items.OfType<RibbonDropDownButton>().Single(item => item.Header == source.Header);
                    copy.IsDropDownOpen = true; Layout(window);
                    Assert.True(copy.IsDropDownOpen, $"Gallery {source.Header}: {theme}, dark={dark}, {density}, {direction}, DPI={scale}");
                    Assert.IsType<ItemsPresenter>(Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(copy.Items)).Content);
                    var tile = (RibbonGalleryItem)source.Items[1]; tile.IsSelected = true; Layout(window);
                    Assert.Same(tile, source.SelectedItem);
                    copy.IsDropDownOpen = false; Layout(window);
                    Assert.Equal(2, source.Items.Count);
                    Assert.Same(source, tile.Parent);
                }
                var more = (ToggleButton)toolbar.Template.FindName("PART_OverflowButton", toolbar);
                Assert.True(more.IsVisible); Assert.True(more.Focusable);
                Assert.InRange(more.ActualHeight, toolbar.ActualHeight - pixel, toolbar.ActualHeight + pixel);
                more.IsChecked = true; Layout(window);
                var chrome = (Border)more.Template.FindName("Chrome", more);
                Assert.Equal(ribbon.FindResource("RibbonKit.Metrics.ControlHighlightBorderThickness"), chrome.BorderThickness);
                Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Control.CheckedBorder"), chrome.BorderBrush);
                var popup = (Popup)toolbar.Template.FindName("PART_OverflowPopup", toolbar);
                Assert.True(popup.IsOpen);
                var entries = (ItemsControl)toolbar.Template.FindName("PART_OverflowHost", toolbar);
                var opener = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(entries.Items));
                opener.IsDropDownOpen = true; Layout(window);
                var native = (Popup)group.Template.FindName("PART_Popup", group);
                Assert.True(native.IsOpen);
                Assert.True(((Border)group.Template.FindName("PART_PopupHost", group)).IsAncestorOf(editor));
                ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(window);
                Assert.False(native.IsOpen); Assert.False(popup.IsOpen);
                Assert.Equal("Source editor", editor.Text);
                Assert.Empty(ribbon.QuickAccessItems);
            }
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(window);
            var overflowToolbar = Descendants((RibbonGroupsHost)tab.Content).OfType<RibbonQuickAccessToolBar>().Single();
            foreach (RibbonGallery source in new RibbonGallery[] { gallery, strip })
            {
                var rowCopy = overflowToolbar.Items.OfType<RibbonDropDownButton>().Single(item => item.Header == source.Header);
                rowCopy.IsDropDownOpen = true; Layout(window);
                var presenter = Assert.IsType<ItemsPresenter>(Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(rowCopy.Items)).Content);
                rowCopy.IsDropDownOpen = false; Layout(window);
                overflowToolbar.Width = 40; Layout(window);
                var more = (ToggleButton)overflowToolbar.Template.FindName("PART_OverflowButton", overflowToolbar);
                more.IsChecked = true; Layout(window);
                var entries = (ItemsControl)overflowToolbar.Template.FindName("PART_OverflowHost", overflowToolbar);
                var menuCopy = entries.Items.OfType<RibbonDropDownButton>().Single(item => item.Header == source.Header);
                Assert.Equal(RibbonControlSize.Medium, menuCopy.Size);
                menuCopy.IsDropDownOpen = true; Layout(window);
                Assert.True(menuCopy.IsDropDownOpen);
                Assert.Same(presenter, Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(menuCopy.Items)).Content);
                menuCopy.IsDropDownOpen = false; more.IsChecked = false;
                overflowToolbar.Width = double.NaN; Layout(window);
            }
        }
        finally
        {
            window.Close(); Layout(window);
            RibbonAnimation.GlobalLevel = motion;
            ThemeManager.SetDarkMode(application, false);
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Layout(Window window)
    {
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }
}
