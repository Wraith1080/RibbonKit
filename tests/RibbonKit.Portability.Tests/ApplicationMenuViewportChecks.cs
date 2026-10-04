using System.Windows;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class ApplicationMenuViewportChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var menu = new RibbonApplicationMenu { DefaultContent = Page() };
        var split = new RibbonApplicationMenuItem { Header = "Save As", IsSplit = true,
            PaneHeader = "Save a copy", Content = Page() };
        menu.Items.Add(split);
        for (int index = 0; index < 11; index++)
            menu.Items.Add(new RibbonApplicationMenuItem { Header = $"Command {index}" });
        var footer = new RibbonApplicationMenuButton { Content = "Options" };
        menu.FooterContent = footer;
        var ribbon = new Ribbon { ApplicationMenu = menu };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var root = new DockPanel();
        DockPanel.SetDock(ribbon, Dock.Top);
        root.Children.Add(ribbon);
        root.Children.Add(new Border());
        var window = new RibbonWindow { Content = root, Width = 720, Height = 320,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            foreach (var theme in new[] { RibbonTheme.CrystalLight, RibbonTheme.Office2007, RibbonTheme.Office2024 })
            foreach (bool dark in new[] { false, true })
            foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                root.FlowDirection = flow;
                double shortScrollRange = double.PositiveInfinity;
                foreach (double height in new[] { 320d, 950d, 320d })
                {
                    window.Height = height;
                    ribbon.IsBackstageOpen = true;
                    Layout();
                    var frame = Part<Border>(menu, "PART_Frame");
                    var footerBounds = footer.TransformToAncestor(root).TransformBounds(new Rect(footer.RenderSize));
                    Assert.True(footerBounds.Top >= 0 && footerBounds.Bottom <= root.ActualHeight + 1,
                        $"{theme}, dark={dark}, {flow}, height={height}: footer {footerBounds}, viewport {root.RenderSize}");
                    var navigation = Part<ScrollViewer>(menu, "NavigationScroll");
                    var recent = Part<ScrollViewer>(menu, "DefaultPaneScroll");
                    if (height == 320)
                    {
                        Assert.True(navigation.ScrollableHeight > 0);
                        shortScrollRange = navigation.ScrollableHeight;
                        Assert.True(recent.ScrollableHeight > 0);
                        CheckScrollButtons(navigation);
                        CheckScrollButtons(recent);
                        navigation.ScrollToBottom();
                        recent.ScrollToBottom();
                        Layout();
                        Assert.True(navigation.VerticalOffset > 0 && recent.VerticalOffset > 0);
                        ((Button)split.Template.FindName("PART_Arrow", split))
                            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Layout();
                        var active = Part<ScrollViewer>(menu, "ActivePaneScroll");
                        Assert.True(active.ScrollableHeight > 0);
                        CheckScrollButtons(active);
                        Assert.Same(split, menu.ActiveItem);
                        active.ScrollToBottom();
                        Layout();
                        Assert.True(active.VerticalOffset > 0);
                        footerBounds = footer.TransformToAncestor(root).TransformBounds(new Rect(footer.RenderSize));
                        Assert.True(footerBounds.Bottom <= root.ActualHeight + 1);
                        // Reflow the anchor without resizing the native window (which closes File).
                        double originalMaximum = frame.MaxHeight;
                        ribbon.Margin = new Thickness(0, 40, 0, 0);
                        Layout();
                        Assert.True(ribbon.IsApplicationMenuOpen);
                        Assert.Equal(originalMaximum - 40, frame.MaxHeight, 3);
                        ribbon.Margin = new Thickness(0);
                        Layout();
                        Assert.Equal(originalMaximum, frame.MaxHeight, 3);
                        footerBounds = footer.TransformToAncestor(root).TransformBounds(new Rect(footer.RenderSize));
                        Assert.True(footerBounds.Bottom <= root.ActualHeight + 1);
                    }
                    else
                    {
                        // Windows can clamp the requested height to the monitor at high DPI.
                        // The reopened menu must reclaim actual space, even if it still needs scrolling.
                        Assert.True(navigation.ScrollableHeight < shortScrollRange,
                            $"{theme}, dark={dark}, {flow}: height={window.ActualHeight}, scroll={navigation.ScrollableHeight}, short={shortScrollRange}");
                        frame.MaxHeight = 180;
                        Layout();
                        Assert.Equal(180d, frame.MaxHeight);
                        frame.ClearValue(FrameworkElement.MaxHeightProperty);
                        Layout();
                        Assert.True(frame.MaxHeight > 180);
                    }
                    ribbon.IsBackstageOpen = false;
                    Layout();
                }
            }
            window.Content = null;
            Layout();
            Assert.Equal(double.PositiveInfinity, Part<Border>(menu, "PART_Frame").MaxHeight);
            window.Content = root;
            ribbon.IsBackstageOpen = true;
            Layout();
            var restoredFooter = footer.TransformToAncestor(root).TransformBounds(new Rect(footer.RenderSize));
            Assert.True(restoredFooter.Bottom <= root.ActualHeight + 1);
        }
        finally
        {
            window.Close();
            RibbonAnimation.GlobalLevel = animation;
        }
        void Layout()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
        void CheckScrollButtons(ScrollViewer viewer)
        {
            viewer.ScrollToTop();
            Layout();
            var bar = Descendants(viewer).OfType<ScrollBar>()
                .Single(b => b.Orientation == Orientation.Vertical);
            var buttons = Descendants(bar).OfType<RepeatButton>().ToArray();
            foreach (var command in new[] { ScrollBar.LineDownCommand, ScrollBar.PageDownCommand })
            {
                var button = buttons.Single(b => b.Command == command);
                Click(button);
                Layout();
                Assert.True(ribbon.IsApplicationMenuOpen, $"{viewer.Name}: {command.Name} closed File");
                Assert.True(viewer.VerticalOffset > 0);
                viewer.ScrollToTop();
                Layout();
            }
            viewer.ScrollToBottom();
            Layout();
            double previousOffset = viewer.VerticalOffset;
            var up = buttons.Single(b => b.Command == ScrollBar.LineUpCommand);
            Click(up);
            Layout();
            Assert.True(ribbon.IsApplicationMenuOpen, $"{viewer.Name}: LineUp closed File");
            Assert.True(viewer.VerticalOffset < previousOffset);
        }
    }

    // Exercise the realized WPF button's Click event and routed command together. Offscreen
    // automation invocation depends on native activation/command requery, outside this STA gate.
    private static void Click(RepeatButton button) =>
        typeof(ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, null);

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static T Part<T>(Control owner, string name) where T : FrameworkElement =>
        Assert.IsType<T>(owner.Template.FindName(name, owner));

    private static StackPanel Page()
    {
        var page = new StackPanel();
        for (int index = 0; index < 20; index++)
            page.Children.Add(new TextBlock { Text = $"Document {index}", Height = 24 });
        return page;
    }
}
