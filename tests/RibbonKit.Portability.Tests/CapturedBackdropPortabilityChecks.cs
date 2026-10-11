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

    internal static void VerifyGlass(Application application)
    {
        var first = new Border { Background = new SolidColorBrush(Color.FromRgb(0x95, 0x95, 0x95)) };
        var second = new Border();
        var ribbon = new Ribbon();
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(new RibbonGroup { Header = "Commands" });
        ribbon.Tabs.Add(home);
        first.Child = ribbon;
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
                var originalBand = (Brush)first.FindResource("RibbonKit.Brushes.Ribbon.Background");
                var originalTitle = (Brush)first.FindResource("RibbonKit.Brushes.TitleBar.Background");
                var originalFileHover = (Brush)first.FindResource("RibbonKit.Brushes.ApplicationButton.HoverBackground");
                var other = second.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground");
                int before = changes;
                var overlay = ThemeManager.CreateGlassOverlay(first, dark);
                Assert.Equal(before, changes);
                Assert.Same(original, first.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"));
                first.Resources.MergedDictionaries.Add(overlay);
                first.Measure(new Size(700, 170));
                first.Arrange(new Rect(0, 0, 700, 170));
                first.UpdateLayout();
                Drain();
                var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
                var tabBand = Assert.IsType<Grid>(tabs.Template.FindName("PART_TabHeaderHost", tabs));
                var body = Assert.IsType<Border>(tabs.Template.FindName("ContentHost", tabs));
                Assert.Same(first.FindResource("RibbonKit.Brushes.Ribbon.TabStripBackground"), tabBand.Background);
                double bandOpacity = theme is RibbonTheme.Office2013 or RibbonTheme.Office2019 ? 0.48 : 0.88;
                Assert.Equal(originalBand.Opacity * bandOpacity, tabBand.Background.Opacity, 5);
                Assert.Equal(1, originalBand.Opacity);
                if (theme is RibbonTheme.Office2013 or RibbonTheme.Office2019)
                {
                    var title = (Brush)first.FindResource("RibbonKit.Brushes.TitleBar.Background");
                    Assert.Equal(title.Opacity / originalTitle.Opacity,
                        tabBand.Background.Opacity / originalBand.Opacity, 5);
                }
                Assert.Equal(0, ribbon.Background.Opacity);
                Assert.Same(first.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"), body.Background);
                double bodyOpacity = theme == RibbonTheme.Office2019 && dark ? 0.64 : 0.44;
                Assert.Equal(original.Opacity * bodyOpacity, body.Background.Opacity, 5);
                if (theme == RibbonTheme.Office2019 && dark)
                    VerifyDarkGlassSeparation(first, tabBand, body);
                Assert.Equal(1, original.Opacity);
                Assert.Same(other, second.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"));
                Assert.Equal(dark ? (byte)0x50 : (byte)0x98,
                    Assert.IsType<SolidColorBrush>(first.FindResource("RibbonKit.Brushes.Control.HoverBackground")).Color.A);
                Assert.False(overlay.Contains("RibbonKit.Brushes.Ribbon.ContentBackground"));
                if (theme == RibbonTheme.Office2013 && !dark)
                {
                    var fileHover = Assert.IsType<SolidColorBrush>(first.FindResource("RibbonKit.Brushes.ApplicationButton.HoverBackground"));
                    Assert.NotSame(originalFileHover, fileHover);
                    Assert.Equal(Assert.IsType<SolidColorBrush>(originalFileHover).Color, fileHover.Color);
                    Assert.Equal(originalFileHover.Opacity, fileHover.Opacity);
                }
                first.Resources["RibbonKit.Brushes.Tab.HoverBackground"] = Brushes.Orange;
                Assert.Same(Brushes.Orange, first.FindResource("RibbonKit.Brushes.Tab.HoverBackground"));
                first.Resources.Remove("RibbonKit.Brushes.Tab.HoverBackground");
                first.Resources.MergedDictionaries.Remove(overlay);
                Drain();
                Assert.Same(first.FindResource("RibbonKit.Brushes.Ribbon.Background"), ribbon.Background);
                Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(tabBand.Background).Color);
                var replacement = ThemeManager.CreateGlassOverlay(first, dark);
                Assert.Equal(((Brush)overlay["RibbonKit.Brushes.Ribbon.BodyBackground"]).Opacity,
                    ((Brush)replacement["RibbonKit.Brushes.Ribbon.BodyBackground"]).Opacity);
                first.Resources.MergedDictionaries.Remove(palette);
                Assert.Same(other, first.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"));
            }

            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            ThemeManager.SetDarkMode(application, false);
            foreach (var (scopedTheme, scopedDark) in new[]
                { (RibbonTheme.Office2013, false), (RibbonTheme.Office2019, true) })
            {
                var scoped = ThemeManager.CreatePalette(scopedTheme, Colors.Purple, scopedDark);
                first.Resources.MergedDictionaries.Add(scoped);
                var scopedOverlay = ThemeManager.CreateGlassOverlay(first, scopedDark);
                if (scopedTheme == RibbonTheme.Office2013)
                {
                    Assert.Equal(Assert.IsType<SolidColorBrush>(first.FindResource("RibbonKit.Brushes.ApplicationButton.HoverBackground")).Color,
                        Assert.IsType<SolidColorBrush>(scopedOverlay["RibbonKit.Brushes.ApplicationButton.HoverBackground"]).Color);
                }
                first.Resources.MergedDictionaries.Add(scopedOverlay);
                Drain();
                var scopedTabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
                var scopedBand = Assert.IsType<Grid>(scopedTabs.Template.FindName("PART_TabHeaderHost", scopedTabs));
                var scopedBody = Assert.IsType<Border>(scopedTabs.Template.FindName("ContentHost", scopedTabs));
                Assert.Equal(0.48, scopedBand.Background.Opacity, 5);
                Assert.Equal(scopedDark ? 0.64 : 0.44, scopedBody.Background.Opacity, 5);
                first.Resources.MergedDictionaries.Remove(scopedOverlay);
                first.Resources.MergedDictionaries.Remove(scoped);
            }
        }
        finally { ThemeManager.Changed -= changed; }
    }

    private static void VerifyDarkGlassSeparation(Border root, FrameworkElement tabBand, FrameworkElement body)
    {
        var bitmap = new RenderTargetBitmap((int)root.ActualWidth, (int)root.ActualHeight,
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        byte Sample(FrameworkElement surface)
        {
            var point = surface.TransformToAncestor(root).Transform(
                new Point(surface.ActualWidth * 0.75, surface.ActualHeight * 0.5));
            var pixel = new byte[4];
            bitmap.CopyPixels(new Int32Rect((int)point.X, (int)point.Y, 1, 1), pixel, 4, 0);
            return pixel[0];
        }
        byte headerShade = Sample(tabBand);
        byte bodyShade = Sample(body);
        Assert.True(headerShade - bodyShade >= 16,
            $"Dark Office 2019 glass needs distinct surfaces over gray paint; header={headerShade}, body={bodyShade}.");
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
