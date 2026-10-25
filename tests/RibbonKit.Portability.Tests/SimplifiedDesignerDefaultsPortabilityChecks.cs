using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Loads the persisted XAML settings used by the designer in a separate consumer.
// This verifies runtime presentation, not Visual Studio's isolated Model API.
internal static class SimplifiedDesignerDefaultsPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var level = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var types = new[] { "RibbonButton", "RibbonToggleButton", "RibbonSplitButton", "RibbonDropDownButton",
            "RibbonComboBox", "RibbonGallery", "InRibbonGallery" };
        try
        {
            VerifyStarterStructures();
            foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var size in new[] { RibbonControlSize.Medium, RibbonControlSize.Small })
            {
                ThemeManager.Apply(application, theme);
                string commands = string.Join("", types.Select(type =>
                    $"<rk:{type} Header=\"{type}\" Icon=\"{{StaticResource Glyph}}\" rk:Ribbon.CommandId=\"{type}\" "
                    + (type == "RibbonComboBox" ? "InputWidth=\"130\" " : "")
                    + $"rk:Ribbon.SimplifiedPlacement=\"Inline\" rk:Ribbon.SimplifiedSize=\"{size}\">"
                    + StarterChild(type) + $"</rk:{type}>"));
                string xaml = "<rk:Ribbon xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" "
                    + "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:rk=\"urn:ribbonkit\" LayoutMode=\"Simplified\" SelectedIndex=\"0\">"
                    + "<rk:Ribbon.Resources><DrawingImage x:Key=\"Glyph\"><DrawingImage.Drawing>"
                    + "<GeometryDrawing Brush=\"SteelBlue\" Geometry=\"M0,0 L16,0 16,16 0,16 Z\" />"
                    + "</DrawingImage.Drawing></DrawingImage></rk:Ribbon.Resources>"
                    + "<rk:Ribbon.Tabs><rk:RibbonTab Header=\"Home\"><rk:RibbonTab.Groups>"
                    + "<rk:RibbonGroup Header=\"Commands\">" + commands
                    + "<rk:RibbonButton Header=\"Runtime defaults\" /><rk:RibbonTextBox Header=\"Native editor\" />"
                    + "</rk:RibbonGroup></rk:RibbonTab.Groups></rk:RibbonTab></rk:Ribbon.Tabs></rk:Ribbon>";
                var ribbon = Assert.IsType<Ribbon>(XamlReader.Parse(xaml));
                var window = new RibbonWindow { Content = ribbon, Width = 1500, Height = 350,
                    FlowDirection = direction, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
                try
                {
                    window.Show(); Layout(window);
                    var group = ribbon.Tabs[0].Groups[0];
                    var toolbar = Descendants((DependencyObject)ribbon.Tabs[0].Content)
                        .OfType<RibbonQuickAccessToolBar>().Single();
                    foreach (var source in group.Items.OfType<FrameworkElement>().Take(types.Length))
                    {
                        Assert.Equal(RibbonSimplifiedPlacement.Inline, source.ReadLocalValue(Ribbon.SimplifiedPlacementProperty));
                        Assert.Equal(size, source.ReadLocalValue(Ribbon.SimplifiedSizeProperty));
                        if (source is RibbonComboBox combo)
                        {
                            Assert.Equal(130d, combo.InputWidth);
                            Assert.Equal(DependencyProperty.UnsetValue, combo.ReadLocalValue(FrameworkElement.WidthProperty));
                        }
                        var copy = Assert.IsAssignableFrom<Control>(toolbar.Items.OfType<FrameworkElement>()
                            .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), source)));
                        if (source is RibbonComboBox)
                        {
                            // Original-slot combos retain their native field at either command size.
                            var editor = Assert.IsAssignableFrom<RibbonComboBox>(copy);
                            Assert.Equal(130d, editor.InputWidth);
                            Assert.True(Assert.IsType<Border>(editor.Template.FindName("Chrome", editor)).IsVisible);
                        }
                        else
                        {
                            var medium = Assert.IsAssignableFrom<FrameworkElement>(copy.Template.FindName("MediumLayout", copy));
                            var small = Assert.IsAssignableFrom<FrameworkElement>(copy.Template.FindName("SmallLayout", copy));
                            Assert.Equal(size == RibbonControlSize.Medium ? Visibility.Visible : Visibility.Collapsed, medium.Visibility);
                            Assert.Equal(size == RibbonControlSize.Small ? Visibility.Visible : Visibility.Collapsed, small.Visibility);
                        }
                        Assert.Equal(direction, copy.FlowDirection);
                        Assert.True(copy.ActualWidth > 0);
                        Assert.True(copy.IsVisible && copy.ActualHeight > 0);
                        if (source is ItemsControl container && StarterChild(source.GetType().Name).Length > 0)
                        {
                            Assert.Single(container.Items);
                            if (container.Items[0] is RibbonMenuItem menuItem) Assert.Equal("Menu Item", menuItem.Header);
                            else Assert.Equal("Item", Assert.IsAssignableFrom<ContentControl>(container.Items[0]).Content);
                        }
                    }
                    var unconfigured = Assert.IsType<RibbonButton>(group.Items[types.Length]);
                    Assert.Equal(RibbonSimplifiedPlacement.Overflow, Ribbon.GetSimplifiedPlacement(unconfigured));
                    Assert.Equal(RibbonControlSize.Medium, Ribbon.GetSimplifiedSize(unconfigured));
                    Assert.DoesNotContain(toolbar.Items.OfType<FrameworkElement>(), item =>
                        ReferenceEquals(Ribbon.GetQuickAccessSource(item), unconfigured));
                    Assert.DoesNotContain(toolbar.Items.OfType<FrameworkElement>(), item =>
                        ReferenceEquals(Ribbon.GetQuickAccessSource(item), group.Items[types.Length + 1]));
                }
                finally { window.Close(); }
            }
        }
        finally { RibbonAnimation.GlobalLevel = level; }
    }

    private static string StarterChild(string type) => type switch
    {
        "RibbonComboBox" => "<ComboBoxItem Content=\"Item\" />",
        "RibbonGallery" or "InRibbonGallery" => "<rk:RibbonGalleryItem Content=\"Item\" />",
        "RibbonSplitButton" or "RibbonDropDownButton" => "<rk:RibbonMenuItem Header=\"Menu Item\" />",
        _ => "",
    };

    private static void VerifyStarterStructures()
    {
        var ribbon = Assert.IsType<Ribbon>(XamlReader.Parse("""
            <rk:Ribbon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:rk="urn:ribbonkit">
              <rk:Ribbon.MessageBar><rk:RibbonMessageBar><rk:RibbonMessage Title="Notification" Message="Message text" /></rk:RibbonMessageBar></rk:Ribbon.MessageBar>
              <rk:Ribbon.QuickAccessItems><rk:RibbonButton Header="Button" Size="Small" /></rk:Ribbon.QuickAccessItems>
              <rk:Ribbon.Backstage><rk:Backstage><rk:BackstageTabItem Header="Info" /></rk:Backstage></rk:Ribbon.Backstage>
              <rk:Ribbon.ApplicationMenu><rk:RibbonApplicationMenu>
                <rk:RibbonApplicationMenuItem Header="New Command" />
              </rk:RibbonApplicationMenu></rk:Ribbon.ApplicationMenu>
              <rk:Ribbon.Tabs><rk:RibbonTab Header="Home"><rk:RibbonTab.Groups><rk:RibbonGroup Header="Group">
                <StackPanel><rk:RibbonScrollBar /><rk:RibbonGroupSeparator /></StackPanel>
              </rk:RibbonGroup></rk:RibbonTab.Groups></rk:RibbonTab></rk:Ribbon.Tabs>
            </rk:Ribbon>
            """));
        var tab = Assert.Single(ribbon.Tabs);
        Assert.Equal("Home", tab.Header);
        var group = Assert.Single(tab.Groups);
        Assert.Equal("Group", group.Header);
        Assert.Equal(DependencyProperty.UnsetValue, group.ReadLocalValue(FrameworkElement.WidthProperty));
        Assert.Equal("Message text", Assert.IsType<RibbonMessage>(Assert.Single(ribbon.MessageBar!.Items.Cast<object>())).Message);
        Assert.Equal("Button", Assert.IsType<RibbonButton>(Assert.Single(ribbon.QuickAccessItems)).Header);
        var backstage = Assert.IsType<Backstage>(ribbon.Backstage);
        Assert.Equal("Info", Assert.IsType<BackstageTabItem>(Assert.Single(backstage.Items.Cast<object>())).Header);
        var menu = Assert.IsType<RibbonApplicationMenu>(ribbon.ApplicationMenu);
        var command = Assert.IsType<RibbonApplicationMenuItem>(Assert.Single(menu.Items.Cast<object>()));
        Assert.Equal("New Command", command.Header);
        Assert.Null(command.Content);
        var panel = Assert.IsType<StackPanel>(Assert.Single(group.Items.Cast<object>()));
        Assert.IsType<RibbonScrollBar>(panel.Children[0]);
        Assert.IsType<RibbonGroupSeparator>(panel.Children[1]);
        Assert.Empty(new Ribbon().Tabs);
        Assert.Empty(new RibbonComboBox().Items);
        Assert.Empty(new RibbonApplicationMenu().Items);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    private static void Layout(Window window)
    {
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
    }
}
