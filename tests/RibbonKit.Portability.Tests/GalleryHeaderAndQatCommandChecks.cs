using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// A consumer of shared RibbonKit resources only, with no Showcase helpers.
internal static class GalleryHeaderAndQatCommandChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            VerifyGallery(application);
            VerifyCustomizationPages(application);
        }
        finally
        {
            RibbonAnimation.GlobalLevel = motion;
            ThemeManager.SetDarkMode(application, false);
        }
    }

    private static void VerifyGallery(Application application)
    {
        var gallery = new InRibbonGallery { Width = 180 };
        for (int i = 0; i < 24; i++)
            gallery.Items.Add(new RibbonGalleryItem { Content = new TextBlock { Text = $"Style {i}", Width = 72, Height = 36 } });
        gallery.SelectedIndex = 16;
        var group = new RibbonGroup { Header = "Styles", CanResize = false }; group.Items.Add(gallery);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon(); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        var window = Window(ribbon);
        try
        {
            window.Show();
            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (double scale in new[] { 1.25, 2 })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; window.FlowDirection = flow;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); Layout(window);
                gallery.DropDownHeader = null; gallery.IsDropDownOpen = true; Layout(window);
                var header = Part<Border>(gallery, "DropDownHeaderHost");
                var title = Part<TextBlock>(gallery, "DropDownHeaderText");
                var popup = Part<Border>(gallery, "PART_PopupHost");
                var scroll = Part<ScrollViewer>(gallery, "PART_PopupScrollViewer");
                Assert.Equal(Visibility.Collapsed, header.Visibility);
                double stripHeight = gallery.ActualHeight;
                double emptyHeight = popup.ActualHeight;
                gallery.DropDownHeader = ""; Layout(window);
                Assert.Equal(emptyHeight, popup.ActualHeight);
                gallery.DropDownHeader = "Choose a style"; Layout(window);
                Assert.Equal(Visibility.Visible, header.Visibility);
                Assert.Equal(gallery.DropDownHeader, title.Text);
                Assert.False(header.IsHitTestVisible); Assert.False(title.Focusable);
                Assert.Equal(flow, title.FlowDirection);
                Assert.Equal(density, Ribbon.GetDensity(header));
                Assert.Equal((CornerRadius)gallery.FindResource("RibbonKit.Metrics.DropDownHeaderCornerRadius"), header.CornerRadius);
                Assert.Same(gallery.FindResource("RibbonKit.Brushes.ApplicationMenu.HeaderBackground"), header.Background);
                Assert.Same(gallery.FindResource("RibbonKit.Brushes.ApplicationMenu.Foreground"), title.Foreground);
                if (dark && theme is RibbonTheme.Office2007 or RibbonTheme.Office2010)
                    Assert.True(Assert.IsType<SolidColorBrush>(title.Foreground).Color.R >= 200);
                var sectionPadding = (Thickness)gallery.FindResource("RibbonKit.Metrics.GallerySectionPadding");
                Assert.Equal(sectionPadding, scroll.Margin);
                Assert.Equal(sectionPadding.Top, scroll.TranslatePoint(new Point(), popup).Y
                    - header.TranslatePoint(new Point(0, header.ActualHeight), popup).Y, 1);
                gallery.Resources["RibbonKit.Brushes.ApplicationMenu.Foreground"] = Brushes.Yellow; Layout(window);
                Assert.Same(Brushes.Yellow, title.Foreground);
                gallery.Resources.Remove("RibbonKit.Brushes.ApplicationMenu.Foreground"); Layout(window);
                gallery.Resources["RibbonKit.Brushes.ApplicationMenu.HeaderBackground"] = Brushes.Coral; Layout(window);
                Assert.Same(Brushes.Coral, header.Background);
                gallery.Resources.Remove("RibbonKit.Brushes.ApplicationMenu.HeaderBackground"); Layout(window);
                Assert.Equal(stripHeight, gallery.ActualHeight);
                Assert.True(header.TranslatePoint(new Point(0, header.ActualHeight), popup).Y
                    <= scroll.TranslatePoint(new Point(), popup).Y + 0.01);
                double headerY = header.TranslatePoint(new Point(), popup).Y;
                scroll.MaxHeight = 80; scroll.ScrollToEnd(); Layout(window);
                Assert.True(scroll.ScrollableHeight > 0);
                Assert.Equal(headerY, header.TranslatePoint(new Point(), popup).Y);
                scroll.ClearValue(FrameworkElement.MaxHeightProperty); Layout(window);
                gallery.DropDownHeader = "Updated style heading"; Layout(window);
                Assert.Equal(gallery.DropDownHeader, title.Text);
                if (density == RibbonDensity.Compact && flow == FlowDirection.LeftToRight && scale == 1.25)
                    SavePreview(popup, $"{theme}-{(dark ? "dark" : "light")}");
                gallery.IsDropDownOpen = false; Layout(window);
                Assert.Equal(16, gallery.SelectedIndex);
                Assert.Equal(stripHeight, gallery.ActualHeight);
                Assert.Same(Part<ItemsPresenter>(gallery, "PART_ItemsPresenter"), Part<ScrollViewer>(gallery, "PART_ScrollViewer").Content);
            }
        }
        finally { gallery.IsDropDownOpen = false; window.Close(); Layout(window); }
    }

    private static void VerifyCustomizationPages(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
        var ribbon = new Ribbon { DataContext = new { UndoCaption = "Undo" } };
        var undo = new RibbonButton { Header = "Undo", Size = RibbonControlSize.Small };
        undo.SetBinding(RibbonButton.HeaderProperty, new Binding("UndoCaption"));
        Ribbon.SetCommandId(undo, "cmd.undo"); ribbon.QuickAccessItems.Add(undo);
        ribbon.QuickAccessItems.Clear(); // Equivalent to restoring saved customization without Undo.
        var tab = new RibbonTab { Header = "Home" }; Ribbon.SetCommandId(tab, "tab.home");
        var group = new RibbonGroup { Header = "My commands", Layout = RibbonGroupLayout.Stacked };
        Ribbon.SetIsCustom(group, true); Ribbon.SetCommandId(group, "custom:commands");
        tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        var qatPage = new RibbonQuickAccessPage { Ribbon = ribbon };
        var ribbonPage = new RibbonCustomizePage { Ribbon = ribbon };
        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition()); content.RowDefinitions.Add(new RowDefinition());
        content.Children.Add(ribbon); content.Children.Add(qatPage); content.Children.Add(ribbonPage);
        Grid.SetRow(qatPage, 1); Grid.SetRow(ribbonPage, 2);
        var window = Window(content); window.Height = 900;
        try
        {
            window.Show(); Layout(window);
            Assert.Equal("Undo", undo.Header);
            var qatAvailable = Part<ListBox>(qatPage, "PART_AvailableList");
            var ribbonAvailable = Part<ListBox>(ribbonPage, "PART_AvailableList");
            var qatEntry = Assert.IsType<RibbonCommandEntry>(Assert.Single(qatAvailable.Items));
            var ribbonEntry = Assert.IsType<RibbonCommandEntry>(Assert.Single(ribbonAvailable.Items));
            Assert.Same(undo, qatEntry.Control); Assert.Same(undo, ribbonEntry.Control);
            qatAvailable.SelectedItem = qatEntry;
            Part<ButtonBase>(qatPage, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            Assert.Same(undo, Assert.Single(ribbon.QuickAccessItems));
            var current = Part<ListBox>(qatPage, "PART_CurrentList"); current.SelectedIndex = 0;
            Part<ButtonBase>(qatPage, "PART_RemoveButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            Assert.Empty(ribbon.QuickAccessItems);
            Assert.Same(undo, Assert.IsType<RibbonCommandEntry>(Assert.Single(qatAvailable.Items)).Control);

            var tree = Part<TreeView>(ribbonPage, "PART_Tree");
            var groupNode = Assert.Single(Assert.IsType<RibbonCustomizeNode>(Assert.Single(tree.Items)).Children);
            groupNode.IsSelected = true; ribbonAvailable.SelectedItem = ribbonEntry; Layout(window);
            var add = Part<ButtonBase>(ribbonPage, "PART_AddButton"); Assert.True(add.IsEnabled);
            add.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            var proxy = Assert.IsType<RibbonButton>(Assert.Single(group.Items));
            Assert.Same(undo, Ribbon.GetQuickAccessSource(proxy));
            int clicks = 0; undo.Click += (_, _) => clicks++;
            proxy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window); Assert.Equal(1, clicks);
        }
        finally { window.Close(); Layout(window); }
    }

    private static Window Window(FrameworkElement content) => new RibbonWindow
    {
        Content = content, Width = 950, Height = 500, Left = -10000, Top = -10000,
        ShowActivated = false, ShowInTaskbar = false,
    };

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void SavePreview(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_HEADER_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        DpiScale dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
