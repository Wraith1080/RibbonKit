using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Uses only RibbonKit controls/tokens. Heading and row chrome do not depend on Showcase.
internal static class DropDownHeaderPortabilityChecks
{
    internal static void Verify(Application application)
    {
        RibbonAnimationLevel motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var dropdown = new RibbonDropDownButton { Header = "Mode", DropDownHeader = "Optimize spacing between commands" };
        var split = new RibbonSplitButton { Header = "Mode", DropDownHeader = dropdown.DropDownHeader };
        foreach (var button in new[] { dropdown, split })
        {
            button.Items.Add(new RibbonMenuItem { Header = "Mouse", Description = "Standard ribbon and commands.\nOptimized for use with mouse.", LargeIcon = Icon() });
            button.Items.Add(new RibbonMenuItem { Header = "Touch", Description = "More space between commands.\nOptimized for use with touch.", LargeIcon = Icon() });
            button.Items.Add(new RibbonMenuItem { Header = "Ordinary command" });
        }
        var group = new RibbonGroup { Header = "Commands" };
        group.Items.Add(dropdown); group.Items.Add(split);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon(); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        var window = new RibbonWindow { Content = ribbon, Width = 840, Height = 500,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        window.Resources["RibbonKit.Brushes.ApplicationMenu.HeaderBackground"] = Brushes.Transparent;
        try
        {
            window.Show();
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = flow; Layout(window);
                foreach (var button in new[] { dropdown, split })
                {
                    button.IsDropDownOpen = true; Layout(window);
                    var header = Part<Border>(button, "DropDownHeaderHost");
                    var title = Part<TextBlock>(button, "DropDownHeaderText");
                    var scroll = Part<ScrollViewer>(button, "PART_PopupScrollViewer");
                    var surface = Part<Border>(button, "PART_MenuHost");
                    Assert.Equal(Visibility.Visible, header.Visibility);
                    Assert.Equal(button.DropDownHeader, title.Text);
                    Assert.Same(button.FindResource("RibbonKit.Brushes.ApplicationMenu.Foreground"), title.Foreground);
                    if (dark && theme is RibbonTheme.Office2007 or RibbonTheme.Office2010)
                        Assert.True(Assert.IsType<SolidColorBrush>(title.Foreground).Color.R >= 200);
                    Assert.False(header.IsHitTestVisible); Assert.False(title.Focusable);
                    Assert.Same(Brushes.Transparent, header.Background);
                    Assert.Equal(surface.CornerRadius, header.CornerRadius);
                    Assert.Equal(flow, title.FlowDirection);
                    Assert.Equal(density, Ribbon.GetDensity(surface));
                    var rich = Assert.IsType<RibbonMenuItem>(button.Items[0]);
                    Assert.Equal(rich.Description, UIElementAutomationPeer.CreatePeerForElement(rich).GetHelpText());
                    Assert.Equal(FontWeights.SemiBold, Part<TextBlock>(rich, "HeaderText").FontWeight);
                    Assert.Equal(Visibility.Visible, Part<TextBlock>(rich, "DescriptionText").Visibility);
                    Assert.Equal(density == RibbonDensity.Touch
                        ? (double)button.FindResource("RibbonKit.Metrics.Touch.LargeIconSize") : 32,
                        Part<Image>(rich, "LargeImage").Width);
                    Assert.True(rich.ActualHeight >= 44);
                    var ordinary = Assert.IsType<RibbonMenuItem>(button.Items[2]);
                    Assert.Equal(Visibility.Collapsed, Part<TextBlock>(ordinary, "DescriptionText").Visibility);
                    Assert.Equal(Visibility.Collapsed, Part<Image>(ordinary, "LargeImage").Visibility);
                    Assert.Equal(16, Part<Image>(ordinary, "SmallImage").Width);
                    Assert.Equal(ordinary.FontWeight, Part<TextBlock>(ordinary, "HeaderText").FontWeight);
                    double ordinaryHeight = ordinary.ActualHeight;
                    ordinary.Description = ""; Layout(window);
                    Assert.Equal(ordinaryHeight, ordinary.ActualHeight);
                    ordinary.ClearValue(RibbonMenuItem.DescriptionProperty);

                    double headerY = header.TranslatePoint(new Point(), surface).Y;
                    scroll.MaxHeight = 64; scroll.ScrollToEnd(); Layout(window);
                    Assert.True(scroll.ScrollableHeight > 0);
                    Assert.Equal(headerY, header.TranslatePoint(new Point(), surface).Y);
                    scroll.ClearValue(FrameworkElement.MaxHeightProperty); scroll.ScrollToTop(); Layout(window);
                    if (flow == FlowDirection.LeftToRight && ReferenceEquals(button, dropdown))
                    {
                        // Capture the native heading paint after verifying the scoped brush above.
                        window.Resources.Remove("RibbonKit.Brushes.ApplicationMenu.HeaderBackground"); Layout(window);
                        SavePreview(surface, $"{theme}-{(dark ? "dark" : "light")}-{density}");
                        window.Resources["RibbonKit.Brushes.ApplicationMenu.HeaderBackground"] = Brushes.Transparent;
                        Layout(window);
                    }

                    string heading = button.DropDownHeader!;
                    button.DropDownHeader = ""; Layout(window);
                    Assert.Equal(Visibility.Collapsed, header.Visibility);
                    button.DropDownHeader = null; Layout(window);
                    Assert.Equal(Visibility.Collapsed, header.Visibility);
                    button.DropDownHeader = heading;
                    button.IsDropDownOpen = false; Layout(window);
                }
            }

            ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
            foreach (RibbonQuickAccessPosition placement in Enum.GetValues<RibbonQuickAccessPosition>())
            foreach (var source in new[] { dropdown, split })
                VerifyProxy(window, ribbon, source, placement);
        }
        finally
        {
            dropdown.IsDropDownOpen = split.IsDropDownOpen = false;
            window.Close(); Layout(window);
            RibbonAnimation.GlobalLevel = motion;
            ThemeManager.SetDarkMode(application, false);
        }
    }

    private static void VerifyProxy(Window window, Ribbon ribbon, RibbonDropDownButton source,
        RibbonQuickAccessPosition placement)
    {
        ribbon.QuickAccessItems.Clear(); ribbon.QuickAccessPosition = placement;
        ribbon.Density = RibbonDensity.Compact;
        ribbon.AddToQuickAccess(source); Layout(window);
        var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[0]);
        Assert.Equal(source.DropDownHeader, proxy.DropDownHeader);
        var first = Assert.IsType<RibbonMenuItem>(source.Items[0]);
        RoutedEventHandler chooseTouch = (_, _) => ribbon.Density = RibbonDensity.Touch;
        first.Click += chooseTouch;
        try
        {
            proxy.IsDropDownOpen = true; Layout(window);
            Assert.Empty(source.Items); Assert.Equal(3, proxy.Items.Count);
            source.DropDownHeader = "Choose a density"; Layout(window);
            Assert.Equal(source.DropDownHeader, Part<TextBlock>(proxy, "DropDownHeaderText").Text);
            Assert.Same(proxy.FindResource("RibbonKit.Brushes.ApplicationMenu.Foreground"),
                Part<TextBlock>(proxy, "DropDownHeaderText").Foreground);
            first.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            Assert.Equal(RibbonDensity.Touch, ribbon.Density);
            Assert.False(proxy.IsDropDownOpen);
            Assert.Equal(3, source.Items.Count); Assert.Empty(proxy.Items);
            Assert.Same(first, source.Items[0]);
            source.IsDropDownOpen = true; Layout(window);
            Assert.Equal(source.DropDownHeader, Part<TextBlock>(source, "DropDownHeaderText").Text);
            Assert.Equal(Visibility.Visible, Part<TextBlock>(first, "DescriptionText").Visibility);
            source.IsDropDownOpen = false; Layout(window);
        }
        finally { first.Click -= chooseTouch; proxy.IsDropDownOpen = false; Layout(window); }
    }

    private static ImageSource Icon()
    {
        var image = new DrawingImage(new GeometryDrawing(Brushes.DodgerBlue, null,
            new RectangleGeometry(new Rect(0, 0, 24, 24), 3, 3)));
        image.Freeze(); return image;
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void SavePreview(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_DENSITY_SELECTOR_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        if (PresentationSource.FromVisual(element)?.RootVisual is FrameworkElement root && root is not Window)
            element = root;
        Directory.CreateDirectory(directory);
        DpiScale dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
