using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Exercises the shared template in a consumer with no Showcase resources or helpers.
internal static class CollapsedGroupPressChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
                Verify(application, theme, density);
        }
        finally { RibbonAnimation.GlobalLevel = motion; }
    }

    private static void Verify(Application application, RibbonTheme theme, RibbonDensity density)
    {
        ThemeManager.Apply(application, theme);
        var command = new RibbonButton { Header = "Action", Width = 700 };
        var group = new RibbonGroup { Header = "Formatting" };
        group.Items.Add(command);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon { Width = 420, Density = density };
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        var outside = new Button { Content = "Outside" };
        var window = new Window { Content = new StackPanel { Children = { ribbon, outside } },
            Width = 800, Height = 400, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
            var opener = Part<ToggleButton>(group, "PART_CollapsedButton");
            var popup = Part<Popup>(group, "PART_Popup");
            var normal = Part<Decorator>(group, "PART_NormalHost");
            var flyout = Part<Border>(group, "PART_PopupHost");
            var chrome = Part<Border>(opener, "Chrome");
            var paint = new List<(bool Checked, Brush Background, Brush Border)>();
            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = (size, target, offset) =>
            {
                // Record before native creation completes; assert outside WPF's binding callback.
                if (popup.IsOpen) paint.Add((opener.IsChecked == true, chrome.Background, chrome.BorderBrush));
                return new[] { new CustomPopupPlacement(new Point(0, target.Height), PopupPrimaryAxis.None) };
            };

            Click(true);
            Assert.NotEmpty(paint);
            Assert.All(paint, item =>
            {
                Assert.True(item.Checked);
                Assert.Same(group.FindResource("RibbonKit.Brushes.Control.PressedBackground"), item.Background);
                Assert.Same(group.FindResource("RibbonKit.Brushes.Control.PressedBorder"), item.Border);
            });
            Assert.Null(normal.Child); Assert.True(flyout.IsAncestorOf(command));
            Click(false); // A second press closes; release must not reopen it.
            Assert.NotNull(normal.Child); Assert.Null(flyout.Child);
            Click(true); // Immediate reopen retains the popup binding and content ownership.
            InputManager.Current.ProcessInput(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                { RoutedEvent = Mouse.PreviewMouseDownEvent, Source = outside });
            Layout(); Check(false);
            Click(true);
            command.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Layout(); Check(false);
            Assert.NotNull(normal.Child); Assert.Null(flyout.Child);
            Assert.True(BindingOperations.IsDataBound(popup, Popup.IsOpenProperty));

            void Click(bool open)
            {
                // As in the dropdown regression, supply press state for the offscreen
                // fixture; this exercises WPF handlers, not physical OS mouse input.
                opener.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                    { RoutedEvent = Mouse.PreviewMouseDownEvent, Source = opener });
                var pressedKey = (DependencyPropertyKey)typeof(ButtonBase)
                    .GetField("IsPressedPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                opener.SetValue(pressedKey, true);
                try { MouseEvent("OnMouseLeftButtonDown", Mouse.MouseDownEvent); Check(open); }
                finally { opener.SetValue(pressedKey, false); }
                MouseEvent("OnMouseLeftButtonUp", Mouse.MouseUpEvent);
                Layout(); Check(open);
            }
            void Check(bool open)
            {
                Assert.Equal(open, popup.IsOpen);
                Assert.Equal(open, opener.IsChecked);
            }
            void MouseEvent(string method, RoutedEvent routedEvent) => typeof(ButtonBase)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(opener, new object[] { new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                    { RoutedEvent = routedEvent, Source = opener } });
            void Layout()
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }
        }
        finally { window.Close(); }
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
}
