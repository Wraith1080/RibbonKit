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
            foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var size in new[] { RibbonControlSize.Medium, RibbonControlSize.Small })
            {
                ThemeManager.Apply(application, theme);
                string commands = string.Join("", types.Select(type =>
                    $"<rk:{type} Header=\"{type}\" Icon=\"{{StaticResource Glyph}}\" rk:Ribbon.CommandId=\"{type}\" "
                    + $"rk:Ribbon.SimplifiedPlacement=\"Inline\" rk:Ribbon.SimplifiedSize=\"{size}\" />"));
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
                        var copy = Assert.IsAssignableFrom<Control>(toolbar.Items.OfType<FrameworkElement>()
                            .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), source)));
                        var medium = Assert.IsAssignableFrom<FrameworkElement>(copy.Template.FindName("MediumLayout", copy));
                        var small = Assert.IsAssignableFrom<FrameworkElement>(copy.Template.FindName("SmallLayout", copy));
                        Assert.Equal(size == RibbonControlSize.Medium ? Visibility.Visible : Visibility.Collapsed, medium.Visibility);
                        Assert.Equal(size == RibbonControlSize.Small ? Visibility.Visible : Visibility.Collapsed, small.Visibility);
                        Assert.Equal(direction, copy.FlowDirection);
                        Assert.True(copy.ActualWidth > 0);
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
