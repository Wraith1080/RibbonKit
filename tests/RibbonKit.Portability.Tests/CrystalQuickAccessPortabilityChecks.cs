using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Called on the consumer fixture's single STA/Application. No Showcase resources or helpers.
internal static class CrystalQuickAccessPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animationLevel = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var messages = new RibbonMessageBar();
        var message = new RibbonMessage { Message = "Document notice", IsOpen = false };
        messages.Items.Add(message);
        var ribbon = new Ribbon { MessageBar = messages, Backstage = new Backstage(),
            QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, QuickAccessMaxWidth = 45 };
        var home = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        var save = new RibbonButton { Header = "Save" };
        var dropdown = new RibbonDropDownButton { Header = "Select" };
        dropdown.Items.Add(new RibbonMenuItem { Header = "All" });
        var split = new RibbonSplitButton { Header = "Paste" };
        split.Items.Add(new RibbonMenuItem { Header = "Text" });
        group.Items.Add(save);
        group.Items.Add(dropdown);
        group.Items.Add(split);
        home.Groups.Add(group);
        ribbon.Tabs.Add(home);
        ribbon.SelectedTab = home;
        var contextual = new RibbonTab { Header = "Picture", IsContextual = true,
            ContextualColor = Brushes.Teal };
        contextual.Groups.Add(new RibbonGroup { Header = "Picture commands" });
        ribbon.Tabs.Add(contextual);
        Assert.True(ribbon.AddToQuickAccess(save));
        var window = new RibbonWindow { Content = ribbon, Width = 800, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            ThemeManager.SetDarkMode(application, false);
            window.Show();
            Layout();
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var body = Part<Border>(tabs, "ContentHost");
            var drawer = Part<Border>(ribbon, "QatBelowHost");
            Assert.Equal(new CornerRadius(14), body.CornerRadius);
            AssertDrawer(false, false);
            double initialHeight = drawer.ActualHeight;
            Assert.True(ribbon.AddToQuickAccess(dropdown));
            Layout();
            Assert.Equal(initialHeight, drawer.ActualHeight, 1);
            Assert.True(ribbon.AddToQuickAccess(split));
            Layout();
            Assert.Equal(initialHeight, drawer.ActualHeight, 1);
            Assert.IsType<RibbonButton>(ribbon.QuickAccessItems[0]);
            Assert.IsType<RibbonDropDownButton>(ribbon.QuickAccessItems[1]);
            Assert.IsType<RibbonSplitButton>(ribbon.QuickAccessItems[2]);
            var proxies = ribbon.QuickAccessItems.Cast<FrameworkElement>().ToArray();
            Assert.Same(save, Ribbon.GetQuickAccessSource(proxies[0]));
            Assert.Same(dropdown, Ribbon.GetQuickAccessSource(proxies[1]));
            Assert.Same(split, Ribbon.GetQuickAccessSource(proxies[2]));

            var lightPaint = drawer.Background;
            var lightShadow = drawer.Effect;
            foreach (bool dark in new[] { false, true, false })
            {
                ThemeManager.SetDarkMode(application, dark);
                foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
                foreach (bool minimized in new[] { false, true })
                foreach (bool notice in new[] { false, true })
                {
                    ribbon.FlowDirection = direction;
                    ribbon.QuickAccessPosition = position;
                    ribbon.IsMinimized = minimized;
                    message.IsOpen = notice;
                    Layout();
                    Assert.Equal(notice, ribbon.HasOpenMessages);
                    Assert.Equal(minimized ? Visibility.Collapsed : Visibility.Visible, body.Visibility);
                    Assert.Equal(position == RibbonQuickAccessPosition.BelowRibbon
                        ? Visibility.Visible : Visibility.Collapsed, drawer.Visibility);
                    if (position == RibbonQuickAccessPosition.BelowRibbon)
                    {
                        AssertDrawer(minimized, notice);
                        Assert.Equal(new CornerRadius(14), body.CornerRadius);
                        Assert.Equal(3, Part<ItemsControl>(ribbon, "QatBelowItems").Items.Count);
                        // BelowRibbon retains its full row; the title/tab width cap does not apply.
                        Assert.True(drawer.ActualWidth > ribbon.QuickAccessMaxWidth);
                        // Compare in the LTR window's physical coordinates, not the mirrored host.
                        double first = Bounds(proxies[0], window).Left;
                        double last = Bounds(proxies[2], window).Left;
                        Assert.True(direction == FlowDirection.LeftToRight ? first < last : first > last,
                            $"{direction}: first={first}, last={last}");
                        if (!minimized)
                        {
                            Assert.Equal(body.ActualWidth - 32, drawer.ActualWidth, 1);
                            var bodyBounds = Bounds(body, ribbon);
                            var drawerBounds = Bounds(drawer, ribbon);
                            Assert.Equal(bodyBounds.Left + 16, drawerBounds.Left, 1);
                            Assert.Equal(bodyBounds.Bottom, drawerBounds.Top, 1);
                        }
                    }
                    else
                    {
                        Assert.Equal(0, Panel.GetZIndex(tabs));
                        var toolbar = position == RibbonQuickAccessPosition.TitleBar
                            ? Assert.IsType<RibbonQuickAccessToolBar>(window.TitleBarContent)
                            : Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
                        Assert.True(toolbar.HasOverflow);
                        Assert.Equal(3, toolbar.Items.Count);
                        if (!minimized && !notice) Assert.Equal(new CornerRadius(14), body.CornerRadius);
                    }
                    Assert.Equal(proxies, ribbon.QuickAccessItems.Cast<FrameworkElement>().ToArray());
                }
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                ribbon.IsMinimized = false;
                message.IsOpen = false;
                Layout();
                var shadow = Assert.IsType<DropShadowEffect>(drawer.Effect);
                Assert.Equal(dark ? 0.45 : 0.3, shadow.Opacity);
                Assert.Equal(dark ? Colors.Black : Color.FromRgb(0x34, 0x48, 0x5A), shadow.Color);
                Assert.Equal(dark ? Color.FromArgb(0x38, 0x79, 0xA8, 0xC7)
                    : Color.FromArgb(0x18, 0x7B, 0xA5, 0xC7),
                    Assert.IsType<SolidColorBrush>(drawer.Background).Color);
                if (dark)
                {
                    Assert.NotSame(lightPaint, drawer.Background);
                    Assert.NotSame(lightShadow, drawer.Effect);
                }
            }

            // Contextual tab changes must not recolor the ordinary QAT drawer.
            var paint = drawer.Background;
            var rim = drawer.BorderBrush;
            ribbon.SelectedTab = contextual;
            contextual.ContextualColor = Brushes.Coral;
            Layout();
            Assert.Same(paint, drawer.Background);
            Assert.Same(rim, drawer.BorderBrush);
            ribbon.SelectedTab = home;

            // Live overflow keeps source-linked menus intact through theme switches and closure.
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            Layout();
            var tabToolbar = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
            var opener = Part<ToggleButton>(tabToolbar, "PART_OverflowButton");
            var popup = Part<Popup>(tabToolbar, "PART_OverflowPopup");
            opener.IsChecked = true;
            Layout();
            Assert.True(popup.IsOpen);
            var overflow = Part<ItemsControl>(tabToolbar, "PART_OverflowHost");
            var dropdownEntry = Assert.Single(overflow.Items.OfType<RibbonDropDownButton>(),
                item => item is not RibbonSplitButton);
            var splitEntry = Assert.Single(overflow.Items.OfType<RibbonSplitButton>());
            dropdownEntry.IsDropDownOpen = true;
            Layout();
            Assert.Single(dropdownEntry.Items);
            Assert.Empty(dropdown.Items);
            ThemeManager.SetDarkMode(application, true);
            Layout();
            Assert.True(popup.IsOpen);
            dropdownEntry.IsDropDownOpen = false;
            splitEntry.IsDropDownOpen = true;
            Layout();
            Assert.Single(splitEntry.Items);
            Assert.Empty(split.Items);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Layout();
            splitEntry.IsDropDownOpen = false;
            opener.IsChecked = false;
            Layout();
            Assert.False(popup.IsOpen);
            Assert.Single(dropdown.Items);
            Assert.Single(split.Items);

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>().Where(t => t != RibbonTheme.CrystalLight))
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                foreach (bool minimized in new[] { false, true })
                foreach (bool notice in new[] { false, true })
                {
                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                    ribbon.IsMinimized = minimized;
                    message.IsOpen = notice;
                    Layout();
                    Assert.Equal(0, Panel.GetZIndex(tabs));
                    Assert.Equal(0, drawer.MinHeight);
                    Assert.Equal((Thickness)ribbon.FindResource("RibbonKit.Metrics.QatExtenderMargin" +
                        (notice ? "MessageBar" : minimized ? "Minimized" : "")), drawer.Margin);
                    Assert.Equal((CornerRadius)ribbon.FindResource("RibbonKit.Metrics.QatExtenderCornerRadius" +
                        (notice ? "MessageBar" : minimized ? "Minimized" : "")), drawer.CornerRadius);
                    Assert.Equal((CornerRadius)ribbon.FindResource("RibbonKit.Metrics.ContentCornerRadiusTop"), body.CornerRadius);
                    Assert.Equal(Assert.IsType<SolidColorBrush>(ribbon.FindResource("RibbonKit.Brushes.Ribbon.Border")).Color,
                        Assert.IsType<SolidColorBrush>(drawer.BorderBrush).Color);
                    var bodyShadow = Assert.IsType<DropShadowEffect>(body.Effect);
                    var qatShadow = Assert.IsType<DropShadowEffect>(drawer.Effect);
                    Assert.Equal(bodyShadow.Color, qatShadow.Color);
                    Assert.Equal(bodyShadow.Opacity, qatShadow.Opacity);
                    Assert.Equal(bodyShadow.BlurRadius, qatShadow.BlurRadius);
                    Assert.Equal(bodyShadow.ShadowDepth, qatShadow.ShadowDepth);
                    Assert.Equal(bodyShadow.Direction, qatShadow.Direction);
                }
                ThemeManager.Apply(application, RibbonTheme.CrystalLight);
                Layout();
                AssertDrawer(true, true);
            }

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false);
            // Manually merged palettes must update already realized parts, too.
            var manual = new ResourceDictionary
            { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Light.xaml", UriKind.Relative) };
            window.Resources.MergedDictionaries.Add(manual);
            ribbon.IsMinimized = false;
            message.IsOpen = false;
            Layout();
            AssertDrawer(false, false);
            var manualDark = new ResourceDictionary
            { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Dark.xaml", UriKind.Relative) };
            window.Resources.MergedDictionaries.Add(manualDark);
            Layout();
            Assert.Equal(0.45, Assert.IsType<DropShadowEffect>(drawer.Effect).Opacity);

            // Nearer resource replacements stay live; explicit part values outrank theme triggers.
            var hostPaint = Brushes.Plum;
            var hostRim = Brushes.Green;
            var hostShadow = new DropShadowEffect { Opacity = 0.7 };
            ribbon.Resources["RibbonKit.Brushes.QatExtender.Background"] = hostPaint;
            ribbon.Resources["RibbonKit.Brushes.QatExtender.Border"] = hostRim;
            ribbon.Resources["RibbonKit.Effects.QatExtenderShadow"] = hostShadow;
            ribbon.Resources["RibbonKit.Metrics.QatExtenderMargin"] = new Thickness(31, 2, 35, 6);
            Layout();
            Assert.Same(hostPaint, drawer.Background);
            Assert.Same(hostRim, drawer.BorderBrush);
            Assert.Same(hostShadow, drawer.Effect);
            Assert.Equal(new Thickness(31, 2, 35, 6), drawer.Margin);
            ribbon.Resources["RibbonKit.Brushes.QatExtender.Border"] = Brushes.Orange;
            Layout();
            Assert.Same(Brushes.Orange, drawer.BorderBrush);
            drawer.Margin = new Thickness(9);
            drawer.CornerRadius = new CornerRadius(3);
            drawer.Background = Brushes.Red;
            drawer.BorderBrush = Brushes.Blue;
            drawer.Effect = null;
            body.CornerRadius = new CornerRadius(5);
            Panel.SetZIndex(tabs, 7);
            window.Resources.MergedDictionaries.Clear();
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            ThemeManager.SetDarkMode(application, true);
            ribbon.IsMinimized = true;
            message.IsOpen = true;
            Layout();
            Assert.Equal(new Thickness(9), drawer.Margin);
            Assert.Equal(new CornerRadius(3), drawer.CornerRadius);
            Assert.Same(Brushes.Red, drawer.Background);
            Assert.Same(Brushes.Blue, drawer.BorderBrush);
            Assert.Null(drawer.Effect);
            Assert.Equal(new CornerRadius(5), body.CornerRadius);
            Assert.Equal(7, Panel.GetZIndex(tabs));
            foreach (var property in new[] { FrameworkElement.MarginProperty, Border.CornerRadiusProperty,
                Border.BackgroundProperty, Border.BorderBrushProperty, UIElement.EffectProperty })
                drawer.ClearValue(property);
            body.ClearValue(Border.CornerRadiusProperty);
            tabs.ClearValue(Panel.ZIndexProperty);
            ribbon.Resources.Clear();
            Layout();
            AssertDrawer(true, true);

            void AssertDrawer(bool minimized, bool notice)
            {
                Assert.Equal(new Thickness(23, minimized && !notice ? 3 : 0, 23, 4), drawer.Margin);
                Assert.Equal(new CornerRadius(minimized && !notice ? 10 : 0, minimized && !notice ? 10 : 0, 10, 10), drawer.CornerRadius);
                Assert.Equal(new Thickness(1, minimized ? 1 : 0, 1, 1), drawer.BorderThickness);
                Assert.Equal(new Thickness(8, 2, 8, 2), drawer.Padding);
                Assert.Equal(32, drawer.MinHeight);
                Assert.Equal(1, Panel.GetZIndex(tabs));
                Assert.Same(ribbon.FindResource("RibbonKit.Brushes.QatExtender.Background"), drawer.Background);
                Assert.Same(ribbon.FindResource("RibbonKit.Brushes.QatExtender.Border"), drawer.BorderBrush);
                Assert.Equal(new Point(0.5, 1), drawer.BorderBrush.RelativeTransform.Transform(new Point(0.5, 0)));
                Assert.Same(ribbon.FindResource("RibbonKit.Effects.QatExtenderShadow"), drawer.Effect);
                var shadow = Assert.IsType<DropShadowEffect>(drawer.Effect);
                Assert.Equal(10, shadow.BlurRadius);
                Assert.Equal(0, shadow.ShadowDepth);
                Assert.Same(ribbon.FindResource("RibbonKit.Effects.ContentShadow"), body.Effect);
            }
        }
        finally
        {
            window.Close();
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false);
            RibbonAnimation.GlobalLevel = animationLevel;
        }
        void Layout()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
    }

    private static T Part<T>(Control control, string name) where T : FrameworkElement =>
        Assert.IsType<T>(control.Template.FindName(name, control));

    private static Rect Bounds(FrameworkElement element, Visual relativeTo) =>
        element.TransformToVisual(relativeTo).TransformBounds(new Rect(element.RenderSize));
}
