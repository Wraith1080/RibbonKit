using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Independent consumer: shared menu framing and tab paint without Showcase.
internal static class ThemePolishPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var level = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var menu = new RibbonApplicationMenu { DefaultContent = new TextBlock { Text = "Recent documents" } };
        var item = new RibbonApplicationMenuItem { Header = "Save As", PaneHeader = "Save a copy",
            Content = new RibbonApplicationMenuPaneItem { Content = "Document" } };
        menu.Items.Add(item);
        var ribbon = new Ribbon { ApplicationMenu = menu };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var window = new RibbonWindow { Content = ribbon, Width = 850, Height = 600,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        string context = "startup";
        var events = new Queue<string>();
        void Trace(string reason)
        {
            if (events.Count == 12) events.Dequeue();
            events.Enqueue($"{context}: {reason}, open={ribbon.IsBackstageOpen}, visible={menu.IsVisible}, windowActive={window.IsActive}");
        }
        window.Deactivated += (_, _) => Trace("window deactivated");
        window.LocationChanged += (_, _) => Trace("window moved");
        window.SizeChanged += (_, e) => Trace($"window resized {e.PreviousSize}->{e.NewSize}");
        menu.CloseRequested += (_, _) => Trace("menu close requested");
        try
        {
            window.Show();
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                context = $"{theme}/{dark}/{flow}";
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                ribbon.FlowDirection = flow;
                menu.FlowDirection = flow;
                ribbon.IsBackstageOpen = true;
                Layout();
                var active = Part<Border>(menu, "ActivePage");
                var navigation = Part<Border>(menu, "Navigation");
                var pane = Part<Border>(menu, "PART_Pane");
                var inner = Part<Grid>(menu, "InnerContent");
                Assert.Equal(new Thickness(0), active.BorderThickness);
                Assert.Equal(new Thickness(0), active.Margin);
                Assert.Equal(new CornerRadius(0), navigation.CornerRadius);
                Assert.Equal(new CornerRadius(0), pane.CornerRadius);
                if (theme is RibbonTheme.Office2024 or RibbonTheme.Office2024Flat)
                {
                    Assert.Equal(new CornerRadius(6), Part<Border>(menu, "ContentRim").CornerRadius);
                    Assert.Equal(new CornerRadius(5), Part<Border>(menu, "ContentOutline").CornerRadius);
                    Assert.NotNull(inner.Clip);
                    Assert.False(inner.Clip.FillContains(new Point(0, 0)));
                    Assert.True(inner.Clip.FillContains(new Point(inner.ActualWidth / 2, 0.5)));
                }
                Part<Button>(item, "PART_Arrow").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Layout();
                Assert.Same(item, menu.ActiveItem);
                Assert.True(active.IsVisible, $"{context}: active page must remain visible; currentPart={ReferenceEquals(active, menu.Template.FindName("ActivePage", menu))}, currentVisible={(menu.Template.FindName("ActivePage", menu) as FrameworkElement)?.IsVisible}, open={ribbon.IsBackstageOpen}, menuVisible={menu.IsVisible}. Events: {string.Join(" | ", events)}");
                Assert.Equal(pane.ActualWidth, active.ActualWidth, 3);
                var heading = Part<Border>(menu, "HeaderBand");
                var join = Part<Border>(menu, "HeaderBandJoin");
                var separator = inner.Children.OfType<StackPanel>().Single();
                var divider = (FrameworkElement)separator.Children[0];
                Rect dividerBounds = divider.TransformToVisual(inner).TransformBounds(new Rect(divider.RenderSize));
                Rect headingBounds = heading.TransformToVisual(inner).TransformBounds(new Rect(heading.RenderSize));
                Rect joinBounds = join.TransformToVisual(inner).TransformBounds(new Rect(join.RenderSize));
                // The heading fill meets the one-pixel divider, covering its
                // highlight strip. Text retains the original inset from the pane.
                var title = Assert.IsType<TextBlock>(heading.Child);
                Rect titleBounds = title.TransformToVisual(pane).TransformBounds(new Rect(title.RenderSize));
                double rounding = 0.5 / VisualTreeHelper.GetDpi(pane).DpiScaleX + 0.001;
                // InnerContent's own coordinates are logical; its RTL reflection
                // applies to the whole joined surface when rendered in the host.
                Assert.Equal(flow, inner.FlowDirection);
                Assert.Equal(dividerBounds.Right, joinBounds.Left, 3);
                Assert.Equal(headingBounds.Left, joinBounds.Right, 3);
                Assert.InRange(titleBounds.Left, 10 - rounding, 10 + rounding);
                if (theme == RibbonTheme.Office2013)
                {
                    var hover = (SolidColorBrush)ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBackground");
                    var border = (SolidColorBrush)ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBorder");
                    Assert.Equal(hover.Color, border.Color);
                    var chrome = Part<Border>(ribbon.Tabs[0], "HeaderChrome");
                    Assert.Equal(new Thickness(1, 1, 1, 0), chrome.BorderThickness);
                }
                if (theme == RibbonTheme.Office2024 && !dark && flow == FlowDirection.LeftToRight)
                {
                    ribbon.IsBackstageOpen = false; Layout();
                    item.PaneHeader = null;
                    ribbon.IsBackstageOpen = true; Layout();
                    Part<Button>(item, "PART_Arrow").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Layout();
                    Assert.Equal(Visibility.Collapsed, Part<Border>(menu, "HeaderBand").Visibility);
                    Assert.Equal(Visibility.Collapsed, Part<Border>(menu, "HeaderBandJoin").Visibility);
                    Assert.Equal(0, Assert.IsType<Grid>(Part<Border>(menu, "ActivePage").Child).RowDefinitions[0].ActualHeight);
                    item.PaneHeader = "Save a copy";
                }
                ribbon.IsBackstageOpen = false; Layout();
            }
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = level;
        }

        void Layout() { window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); }
    }

    private static T Part<T>(Control owner, string name) where T : class =>
        Assert.IsType<T>(owner.Template.FindName(name, owner));
}
