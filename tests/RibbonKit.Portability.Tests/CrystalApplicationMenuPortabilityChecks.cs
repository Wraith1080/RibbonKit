using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Runs inside the existing consumer's single STA/Application, without Showcase.
internal static class CrystalApplicationMenuPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        ThemeManager.SetDarkMode(application, false);
        var menu = new RibbonApplicationMenu { DefaultContent = new TextBlock { Text = "Recent documents" } };
        var command = new MenuCommand();
        var plain = new RibbonApplicationMenuItem { Header = "Open", Command = command, CommandParameter = "open" };
        var split = new RibbonApplicationMenuItem { Header = "Save As", IsSplit = true,
            Command = command, CommandParameter = "save", PaneHeader = "Save a copy", Content = new TextBlock { Text = "Copy formats" } };
        var paneCommand = new RibbonApplicationMenuPaneItem { Content = "Print", Command = command, CommandParameter = "print" };
        var paneRows = new StackPanel();
        paneRows.Children.Add(paneCommand);
        paneRows.Children.Add(new RibbonApplicationMenuPaneItem { Content = "Unavailable", IsEnabled = false });
        var dropdown = new RibbonApplicationMenuItem { Header = "Print", IsSplit = false, PaneHeader = "Print options", Content = paneRows };
        var separator = new RibbonApplicationMenuSeparator();
        menu.Items.Add(plain);
        menu.Items.Add(split);
        menu.Items.Add(separator);
        menu.Items.Add(dropdown);
        var footerButton = new RibbonApplicationMenuButton { Content = "Options", Command = command, CommandParameter = "options" };
        menu.FooterContent = footerButton;
        var message = new RibbonMessage { Title = "Notice", Message = "Menu must cover this row." };
        var bar = new RibbonMessageBar();
        bar.Items.Add(message);
        var ribbon = new Ribbon { MessageBar = bar };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save" });
        var window = new RibbonWindow { Content = ribbon, Width = 720, Height = 480,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        // A resource-declared menu may be templated before Ribbon ever owns it.
        window.Resources["ConsumerMenu"] = menu;
        menu.ApplyTemplate();
        ribbon.ApplicationMenu = menu;
        try
        {
            window.Show();
            ribbon.IsBackstageOpen = true;
            Layout();
            var frame = Part<Border>(menu, "Frame");
            Assert.Equal(new CornerRadius(14), frame.CornerRadius);
            var pane = Part<Border>(menu, "PART_Pane");
            var inner = Part<Grid>(menu, "InnerContent");
            var shadow = Part<Grid>(menu, "OutsideShadow");
            Assert.Null(frame.Effect);
            Assert.False(shadow.IsHitTestVisible);
            AssertShadowPixels(shadow);
            Brush lightPaint = frame.Background;

            foreach (bool dark in new[] { false, true, false })
            {
                ThemeManager.SetDarkMode(application, dark);
                foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
                foreach (bool minimized in new[] { false, true })
                foreach (bool messages in new[] { false, true })
                {
                    ribbon.FlowDirection = flow;
                    ribbon.QuickAccessPosition = placement;
                    ribbon.IsMinimized = minimized;
                    message.IsOpen = messages;
                    ribbon.IsBackstageOpen = true;
                    Layout();
                    Assert.True(ribbon.IsApplicationMenuOpen);
                    Assert.Equal(flow, menu.FlowDirection);
                    Assert.Equal(new CornerRadius(14), frame.CornerRadius);
                    Assert.Same(menu.FindResource("RibbonKit.Brushes.ApplicationMenu.FrameBand"), frame.Background);
                    Assert.Same(menu.FindResource("RibbonKit.Brushes.ApplicationMenu.FrameBorder"), frame.BorderBrush);
                    Assert.Equal(Visibility.Visible, shadow.Visibility);
                    Assert.Equal(9d, Assert.IsType<RectangleGeometry>(inner.Clip).RadiusX);
                    Assert.Equal(new Rect(inner.RenderSize), Assert.IsType<RectangleGeometry>(inner.Clip).Rect);
                    Assert.Equal(new Thickness(4, 2, 4, 2), split.Margin);
                    Assert.Equal(new Thickness(12, 4, 12, 4), separator.Margin);
                    Assert.Equal(8d, Assert.IsType<RectangleGeometry>(Part<Grid>(split, "Root").Clip).RadiusX);
                    Assert.Equal(0, Part<Border>(menu, "ActivePage").BorderThickness.Left);
                    Assert.Equal(300d, pane.Width);
                    var paneBounds = pane.TransformToAncestor(window).TransformBounds(new Rect(pane.RenderSize));
                    var nav = Part<Border>(menu, "Navigation");
                    var navBounds = nav.TransformToAncestor(window).TransformBounds(new Rect(nav.RenderSize));
                    Assert.True(flow == FlowDirection.LeftToRight ? navBounds.Right < paneBounds.Left : paneBounds.Right < navBounds.Left);
                }
                if (dark) Assert.NotSame(lightPaint, frame.Background);
                window.Width = 420;
                Layout();
                Assert.Equal(Math.Clamp(ribbon.ActualWidth - 192, 180, 300), pane.Width, 3);
                Assert.True(menu.ActualWidth < window.ActualWidth);
                menu.Resources["RibbonKit.Metrics.ApplicationMenuPaneViewportInset"] = 48d;
                Layout();
                Assert.Equal(Math.Clamp(ribbon.ActualWidth - 208, 180, 300), pane.Width, 3);
                menu.Resources.Remove("RibbonKit.Metrics.ApplicationMenuPaneViewportInset");
                window.Width = 720;
                ribbon.IsBackstageOpen = true;
                Layout();
                Assert.True(ribbon.IsApplicationMenuOpen);

                // Real template click handlers select split/dropdown panes and route commands.
                Click(Part<Button>(split, "PART_Arrow"));
                Layout();
                Assert.True(split.IsSplitPresentation);
                Assert.Same(split, menu.ActiveItem);
                Assert.Equal(1d, Part<Border>(split, "ArrowFill").Opacity);
                Assert.Equal(menu.FindResource("RibbonKit.Metrics.ApplicationMenuDimOpacity"), Part<Border>(split, "MainFill").Opacity);
                Click(Part<Button>(dropdown, "PART_Primary"));
                Layout();
                Assert.False(dropdown.IsSplitPresentation);
                Assert.Same(dropdown, menu.ActiveItem);
                Assert.True(ribbon.IsApplicationMenuOpen);
                Invoke(paneCommand);
                Layout();
                Assert.Equal("print", command.LastParameter);
                Assert.False(ribbon.IsApplicationMenuOpen);
                ribbon.IsBackstageOpen = true;
                Layout();
                Assert.Null(menu.ActiveItem);
                Click(Part<Button>(split, "PART_Primary"));
                Layout();
                Assert.Equal("save", command.LastParameter);
                Assert.False(ribbon.IsApplicationMenuOpen);
                ribbon.IsBackstageOpen = true;
                Layout();
                Invoke(footerButton);
                Layout();
                Assert.Equal("options", command.LastParameter);
                Assert.False(ribbon.IsApplicationMenuOpen);
            }

            ribbon.IsBackstageOpen = true;
            Layout();
            menu.Resources["RibbonKit.Metrics.ApplicationMenuCornerRadius"] = new CornerRadius(6);
            menu.Resources["RibbonKit.Metrics.ApplicationMenuItemClipCornerRadius"] = new CornerRadius(5);
            menu.Resources["RibbonKit.Metrics.ApplicationMenuContentOutlineCornerRadius"] = new CornerRadius(7);
            menu.Resources["RibbonKit.Metrics.ApplicationMenuPaneWidth"] = 280d;
            menu.Resources["RibbonKit.Effects.ApplicationMenuShadow"] = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 0 };
            Layout();
            Assert.Equal(new CornerRadius(6), frame.CornerRadius);
            Assert.Equal(5d, Assert.IsType<RectangleGeometry>(Part<Grid>(split, "Root").Clip).RadiusX);
            Assert.Equal(6d, Assert.IsType<RectangleGeometry>(inner.Clip).RadiusX);
            Assert.Equal(280d, pane.Width);
            var halo = Assert.IsType<CombinedGeometry>(shadow.Clip);
            Assert.Equal(-12d, Assert.IsType<RectangleGeometry>(halo.Geometry1).Rect.Left);
            menu.Resources["RibbonKit.Effects.ApplicationMenuShadow"] = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 3 };
            Layout();
            Assert.Equal(-15d, Assert.IsType<RectangleGeometry>(Assert.IsType<CombinedGeometry>(shadow.Clip).Geometry1).Rect.Left);
            frame.CornerRadius = new CornerRadius(1, 2, 3, 4);
            frame.Background = Brushes.Magenta;
            var hostEffect = new DropShadowEffect { BlurRadius = 2 };
            frame.Effect = hostEffect;
            pane.Width = 125;
            split.Margin = new Thickness(9);
            ThemeManager.SetDarkMode(application, true);
            Layout();
            Assert.Equal(new CornerRadius(1, 2, 3, 4), frame.CornerRadius);
            Assert.IsType<StreamGeometry>(Assert.IsType<CombinedGeometry>(shadow.Clip).Geometry2);
            Assert.Same(Brushes.Magenta, frame.Background);
            Assert.Same(hostEffect, frame.Effect);
            Assert.Equal(125, pane.Width);
            double dpiScale = VisualTreeHelper.GetDpi(pane).DpiScaleX;
            Assert.Equal(Math.Round(125 * dpiScale) / dpiScale, pane.ActualWidth, 6);
            pane.Width = 360;
            Layout();
            Assert.Equal(360, pane.ActualWidth);
            Assert.Equal(new Thickness(9), split.Margin);
            frame.ClearValue(Border.CornerRadiusProperty);
            frame.ClearValue(Border.BackgroundProperty);
            frame.ClearValue(UIElement.EffectProperty);
            pane.ClearValue(FrameworkElement.WidthProperty);
            split.ClearValue(FrameworkElement.MarginProperty);
            foreach (string key in new[] { "CornerRadius", "ItemClipCornerRadius", "ContentOutlineCornerRadius", "PaneWidth" })
                menu.Resources.Remove("RibbonKit.Metrics.ApplicationMenu" + key);
            menu.Resources.Remove("RibbonKit.Effects.ApplicationMenuShadow");
            Layout();
            Assert.Equal(new CornerRadius(14), frame.CornerRadius);
            Assert.Null(frame.Effect);
            Assert.Equal(300, pane.Width);
            Assert.Equal(new Thickness(4, 2, 4, 2), split.Margin);

            // Every Office palette restores its original geometry and whole-frame shadow.
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>().Where(t => t != RibbonTheme.CrystalLight))
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                Layout();
                Assert.Equal(menu.FindResource("RibbonKit.Metrics.ApplicationMenuCornerRadius"), frame.CornerRadius);
                Assert.Equal(Visibility.Collapsed, shadow.Visibility);
                Assert.Same(menu.FindResource("RibbonKit.Effects.ApplicationMenuShadow"), frame.Effect);
                Assert.Null(inner.Clip);
                Assert.Null(Part<Grid>(split, "Root").Clip);
                Assert.Equal(new Thickness(), split.Margin);
                Assert.Equal(new Thickness(), separator.Margin);
                Assert.Equal(menu.FindResource("RibbonKit.Metrics.ApplicationMenuPaneWidth"), pane.Width);
                Assert.Equal(new Thickness(1), Part<Border>(menu, "ActivePage").BorderThickness);
            }

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false);
            foreach (bool dark in new[] { false, true })
            {
                var scope = new ResourceDictionary();
                scope.MergedDictionaries.Add(Tokens("Crystal.Light"));
                if (dark) scope.MergedDictionaries.Add(Tokens("Crystal.Dark"));
                menu.Resources.MergedDictionaries.Add(scope);
                Layout();
                Assert.Equal(new CornerRadius(14), frame.CornerRadius);
                Assert.Equal(Visibility.Visible, shadow.Visibility);
                Assert.Null(frame.Effect);
                Assert.Equal(9d, Assert.IsType<RectangleGeometry>(inner.Clip).RadiusX);
                menu.Resources.MergedDictionaries.Remove(scope);
                Layout();
                Assert.Equal(new CornerRadius(8), frame.CornerRadius);
                Assert.Null(inner.Clip);
                Assert.Equal(340d, pane.Width);
            }

            // An explicit host paint/size choice survives even a full Office/Crystal switch.
            frame.Background = Brushes.Lime;
            plain.Margin = new Thickness(11);
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            Layout();
            Assert.Same(Brushes.Lime, frame.Background);
            Assert.Equal(new Thickness(11), plain.Margin);
            ribbon.IsBackstageOpen = false;
            ribbon.ApplicationMenu = null;
            Layout();
            var otherRibbon = new Ribbon { Width = 420, ApplicationMenu = menu };
            otherRibbon.Tabs.Add(new RibbonTab { Header = "Other host" });
            window.Content = otherRibbon;
            otherRibbon.IsBackstageOpen = true;
            Layout();
            Assert.Equal(228d, pane.Width, 3);
            Assert.True(menu.ActualWidth < otherRibbon.ActualWidth);
            Assert.Same(Brushes.Lime, frame.Background);
        }
        finally
        {
            window.Close();
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false);
            RibbonAnimation.GlobalLevel = animation;
        }
        void Layout()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
    }

    private static T Part<T>(Control owner, string name) where T : FrameworkElement =>
        Assert.IsType<T>(owner.Template.FindName(name, owner));
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Invoke(Button button) =>
        ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)!).Invoke();
    private static ResourceDictionary Tokens(string name) => new()
    { Source = new Uri($"/RibbonKit;component/Themes/Tokens.{name}.xaml", UriKind.Relative) };

    private static void AssertShadowPixels(Grid shadow)
    {
        const int padding = 24;
        int width = (int)Math.Ceiling(shadow.ActualWidth), height = (int)Math.Ceiling(shadow.ActualHeight);
        var image = new RenderTargetBitmap(width + padding * 2, height + padding * 2, 96, 96, PixelFormats.Pbgra32);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
            context.DrawRectangle(new VisualBrush(shadow) { ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(-padding, -padding, image.PixelWidth, image.PixelHeight), Stretch = Stretch.Fill },
                null, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
        image.Render(drawing);
        Assert.True(Alpha(padding + width / 2, padding - 2) > 0);
        Assert.True(Alpha(padding + width / 2, padding + height + 2) > 0);
        Assert.True(Alpha(padding - 2, padding + height / 2) > 0);
        Assert.True(Alpha(padding + width + 2, padding + height / 2) > 0);
        Assert.Equal(0, Alpha(padding + width / 2, padding + height / 2));
        int Alpha(int x, int y)
        {
            var pixel = new byte[4];
            image.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
            return pixel[3];
        }
    }

    private sealed class MenuCommand : ICommand
    {
        internal object? LastParameter { get; private set; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => LastParameter = parameter;
    }
}
