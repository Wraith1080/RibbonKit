using System.Windows;
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

internal static class CapturedBackdropPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try { VerifyPopups(application); VerifyMenu(application); VerifyGlass(application); }
        finally
        {
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = motion;
        }
    }

    private static void VerifyPopups(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        var dropdown = new RibbonDropDownButton { Header = "Layout" };
        var split = new RibbonSplitButton { Header = "Paste" };
        foreach (var button in new[] { dropdown, split })
            button.Items.Add(new RibbonMenuItem { Header = "Command" });
        var panel = new StackPanel { Children = { dropdown, split } };
        var window = TestWindow(panel);
        using var first = new CapturedBackdrop(dropdown, window);
        using var second = new CapturedBackdrop(split, window);
        try
        {
            window.Show();
            Drain();
            dropdown.IsDropDownOpen = true;
            Drain();
            var plain = Surface(dropdown);
            Brush ordinary = plain.Background;
            Assert.Same(dropdown.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), ordinary);
            dropdown.IsDropDownOpen = false;
            first.Apply(true);
            second.Apply(true);

            foreach (bool dark in new[] { false, true })
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.SetDarkMode(application, dark);
                window.FlowDirection = flow;
                foreach (var button in new[] { dropdown, split })
                {
                    button.IsDropDownOpen = true;
                    Drain();
                    var surface = Surface(button);
                    var material = Assert.IsType<DrawingBrush>(surface.Background);
                    Assert.Equal(flow, surface.FlowDirection);
                    Assert.NotNull(surface.Child);
                    Assert.IsType<CroppedBitmap>(Assert.IsType<ImageDrawing>(
                        Assert.IsType<DrawingGroup>(material.Drawing).Children[1]).ImageSource);
                    Assert.Same(button.FindResource("RibbonKit.Brushes.ApplicationMenu.FrameBand"),
                        Assert.IsType<GeometryDrawing>(Assert.IsType<DrawingGroup>(material.Drawing).Children[2]).Brush);
                    var paint = Brushes.Orange;
                    surface.Background = paint;
                    first.Refresh(); second.Refresh(); Drain();
                    Assert.Same(paint, surface.Background);
                    button.IsDropDownOpen = false;
                    Drain();
                    Assert.Same(paint, surface.Background); // Closing cannot erase a host override.
                    surface.ClearValue(Border.BackgroundProperty);
                }
            }

            dropdown.IsDropDownOpen = true;
            Drain();
            var host = Surface(dropdown);
            var original = host.Background;
            var palette = ThemeManager.CreatePalette(RibbonTheme.CrystalLight, Colors.Purple, dark: true);
            window.Resources.MergedDictionaries.Add(palette);
            first.Refresh();
            Drain();
            Assert.NotSame(original, host.Background);
            Assert.Same(palette["RibbonKit.Brushes.ApplicationMenu.FrameBand"],
                Assert.IsType<GeometryDrawing>(Assert.IsType<DrawingGroup>(
                    Assert.IsType<DrawingBrush>(host.Background).Drawing).Children[2]).Brush);
            first.Apply(false);
            Assert.Same(palette["RibbonKit.Brushes.Ribbon.ContentBackground"], host.Background);
            Assert.False(DependencyPropertyHelper.GetValueSource(host, Border.BackgroundProperty).IsCurrent);
            window.Resources.MergedDictionaries.Remove(palette);
            Drain();
            Assert.Same(window.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), host.Background);

            first.Apply(true);
            Drain();
            host.SetCurrentValue(Border.BackgroundProperty, Brushes.Lime);
            first.Refresh();
            Drain();
            Assert.Same(Brushes.Lime, host.Background);
            first.Apply(false);
            Assert.Same(Brushes.Lime, host.Background);
            host.ClearValue(Border.BackgroundProperty);

            // Bindings remain intact across enable, close and disposal.
            var binding = new Binding(nameof(Border.Background)) { Source = new Border { Background = Brushes.Gold } };
            host.SetBinding(Border.BackgroundProperty, binding);
            first.Apply(true);
            Drain();
            Assert.Same(Brushes.Gold, host.Background);
            Assert.NotNull(BindingOperations.GetBindingExpression(host, Border.BackgroundProperty));
            first.Dispose();
            Assert.Same(Brushes.Gold, host.Background);
            Assert.NotNull(BindingOperations.GetBindingExpression(host, Border.BackgroundProperty));
            Assert.Throws<ObjectDisposedException>(() => first.Refresh());
            dropdown.IsDropDownOpen = false;

            // Reparenting unloads the registration's engines and load reattaches them.
            split.IsDropDownOpen = false;
            panel.Children.Remove(split);
            Drain();
            panel.Children.Add(split);
            Drain();
            split.IsDropDownOpen = true;
            Drain();
            Assert.IsType<DrawingBrush>(Surface(split).Background);
            var queuedHost = Surface(split);
            second.Refresh();
            second.Dispose();
            Drain();
            Assert.Same(split.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), queuedHost.Background);
        }
        finally { dropdown.IsDropDownOpen = split.IsDropDownOpen = false; window.Close(); Drain(); }
    }

    private static void VerifyMenu(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        var menu = new RibbonApplicationMenu();
        menu.Items.Add(new RibbonApplicationMenuItem { Header = "Open" });
        var group = new RibbonGroup { Header = "Commands" };
        var command = new RibbonButton { Header = "Command", Width = 180 };
        group.Items.Add(command);
        var tab = new RibbonTab { Header = "Home" };
        tab.Groups.Add(group);
        var ribbon = new Ribbon { ApplicationMenu = menu };
        ribbon.Tabs.Add(tab);
        var scroll = new ScrollViewer { Content = new Border { Height = 1500, Background = Brushes.Blue } };
        var root = new DockPanel();
        DockPanel.SetDock(ribbon, Dock.Top);
        root.Children.Add(ribbon); root.Children.Add(scroll);
        var window = TestWindow(root);
        using var capture = new CapturedBackdrop(menu, window);
        using var groupCapture = new CapturedBackdrop(group, window);
        capture.Apply(true); groupCapture.Apply(true);
        try
        {
            window.Show();
            foreach (bool dark in new[] { false, true })
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.SetDarkMode(application, dark);
                window.FlowDirection = flow;
                ribbon.IsBackstageOpen = true;
                Drain();
                var frame = Assert.IsType<Border>(menu.Template.FindName("Frame", menu));
                var wrapper = Assert.IsType<Grid>(menu.Template.FindName("MenuSurface", menu));
                var layer = Assert.IsType<Grid>(wrapper.Children[1]);
                var canvas = Assert.IsType<Canvas>(layer.Children[0]);
                var image = Assert.IsType<Image>(canvas.Children[0]);
                var snapshot = Assert.IsAssignableFrom<BitmapSource>(image.Source);
                Assert.Equal(new Size(), canvas.DesiredSize);
                Assert.False(layer.IsHitTestVisible);
                Assert.False(image.Focusable);
                Assert.Equal(6, Assert.IsType<System.Windows.Media.Effects.BlurEffect>(image.Effect).Radius);
                Assert.Equal(new Rect(frame.RenderSize), Assert.IsType<RectangleGeometry>(layer.Clip).Rect);
                Assert.Equal(1, wrapper.Opacity);
                var focus = Keyboard.FocusedElement;
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset + 20);
                Drain();
                Assert.NotSame(snapshot, image.Source);
                Assert.Same(focus, Keyboard.FocusedElement);
                double width = frame.ActualWidth;
                window.Width -= 40;
                Drain();
                Assert.True(frame.ActualWidth <= width);
                Assert.Equal(new Rect(frame.RenderSize), Assert.IsType<RectangleGeometry>(layer.Clip).Rect);
                capture.Apply(false);
                Assert.Null(image.Source);
                Assert.False(wrapper.Children.Contains(layer));
                capture.Apply(true);
                ribbon.IsBackstageOpen = false;
                Drain();
                ribbon.IsBackstageOpen = true;
                Drain();
                Assert.Equal(3, wrapper.Children.Count);
                ribbon.IsBackstageOpen = false;
                Drain();
            }

            // A collapsed group uses the same optional popup material and keeps its borrowed content.
            window.Width = 160;
            Drain();
            Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
            var toggle = Assert.IsType<ToggleButton>(group.Template.FindName("PART_CollapsedButton", group));
            toggle.IsChecked = true;
            Drain();
            var host = Assert.IsType<Border>(group.Template.FindName("PART_PopupHost", group));
            Assert.IsType<DrawingBrush>(host.Background);
            Assert.NotNull(host.Child);
            groupCapture.Dispose();
            Assert.Same(group.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), host.Background);
            toggle.IsChecked = false;
        }
        finally { ribbon.IsBackstageOpen = false; window.Close(); Drain(); }
    }

    private static void VerifyGlass(Application application)
    {
        var first = new Border(); var second = new Border();
        int changes = 0;
        EventHandler changed = (_, _) => changes++;
        ThemeManager.Changed += changed;
        try
        {
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                var palette = ThemeManager.CreatePalette(theme, Colors.Purple, dark);
                first.Resources.MergedDictionaries.Add(palette);
                var original = (Brush)first.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground");
                var other = second.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground");
                int before = changes;
                var overlay = ThemeManager.CreateGlassOverlay(first, dark);
                Assert.Equal(before, changes);
                Assert.Same(original, first.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"));
                first.Resources.MergedDictionaries.Add(overlay);
                Assert.Equal(original.Opacity * 0.44, ((Brush)first.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity, 5);
                Assert.Equal(1, original.Opacity);
                Assert.Same(other, second.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"));
                Assert.Equal(dark ? (byte)0x50 : (byte)0x98,
                    Assert.IsType<SolidColorBrush>(first.FindResource("RibbonKit.Brushes.Control.HoverBackground")).Color.A);
                Assert.False(overlay.Contains("RibbonKit.Brushes.Ribbon.ContentBackground"));
                first.Resources["RibbonKit.Brushes.Tab.HoverBackground"] = Brushes.Orange;
                Assert.Same(Brushes.Orange, first.FindResource("RibbonKit.Brushes.Tab.HoverBackground"));
                first.Resources.Remove("RibbonKit.Brushes.Tab.HoverBackground");
                first.Resources.MergedDictionaries.Remove(overlay);
                var replacement = ThemeManager.CreateGlassOverlay(first, dark);
                Assert.Equal(((Brush)overlay["RibbonKit.Brushes.Ribbon.BodyBackground"]).Opacity,
                    ((Brush)replacement["RibbonKit.Brushes.Ribbon.BodyBackground"]).Opacity);
                first.Resources.MergedDictionaries.Remove(palette);
                Assert.Same(other, first.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"));
            }
        }
        finally { ThemeManager.Changed -= changed; }
    }

    private static Border Surface(Control control) => Assert.IsAssignableFrom<Border>(
        control.Template.FindName("PART_MenuHost", control));

    private static Window TestWindow(UIElement content) => new()
    {
        Content = content, Width = 700, Height = 500, Left = -10000, Top = -10000,
        ShowActivated = false, ShowInTaskbar = false,
    };

    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
}
