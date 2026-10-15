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

internal static class FeatureAvailabilityPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var previous = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ThemeManager.Apply(application, theme);
                const string xaml = "<rk:Ribbon xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:rk=\"urn:ribbonkit\" "
                    + "IsSimplifiedRibbonEnabled=\"False\" LayoutMode=\"Simplified\" SelectedIndex=\"0\" "
                    + "CanUserCustomizeRibbon=\"False\" CanUserCustomizeQuickAccess=\"False\" CanUserRepositionQuickAccess=\"False\" "
                    + "CanUserToggleQuickAccessVisibility=\"False\" CanUserMinimizeRibbon=\"False\" CanUserChangeLayoutMode=\"False\" CanUserChangeDensity=\"False\">"
                    + "<rk:RibbonTab Header=\"Home\" rk:Ribbon.CommandId=\"home\"><rk:RibbonGroup Header=\"Commands\" rk:Ribbon.CommandId=\"commands\">"
                    + "<rk:RibbonButton Header=\"Save\" rk:Ribbon.CommandId=\"save\" rk:Ribbon.SimplifiedPlacement=\"Inline\" />"
                    + "</rk:RibbonGroup></rk:RibbonTab></rk:Ribbon>";
                var ribbon = Assert.IsType<Ribbon>(XamlReader.Parse(xaml)); ribbon.Density = density;
                var other = new Ribbon { LayoutMode = RibbonLayoutMode.Simplified, Density = density };
                var otherTab = new RibbonTab { Header = "Other" }; var otherGroup = new RibbonGroup { Header = "Other" };
                var otherCommand = new RibbonButton { Header = "Other command" }; Ribbon.SetSimplifiedPlacement(otherCommand, RibbonSimplifiedPlacement.Inline);
                otherGroup.Items.Add(otherCommand); otherTab.Groups.Add(otherGroup); other.Tabs.Add(otherTab); other.SelectedTab = otherTab;
                var panel = new StackPanel(); panel.Children.Add(ribbon); panel.Children.Add(other);
                var window = new RibbonWindow { Content = panel, FlowDirection = direction, Width = 1000, Height = 600,
                    Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
                try
                {
                    window.Show(); Layout(window);
                    var source = (RibbonButton)ribbon.Tabs[0].Groups[0].Items[0];
                    var host = (DependencyObject)ribbon.Tabs[0].Content;
                    Ribbon.SetLayoutMode(host, RibbonLayoutMode.Simplified); Layout(window);
                    Assert.Equal(RibbonLayoutMode.Classic, ribbon.LayoutMode); Assert.Equal(RibbonLayoutMode.Classic, Ribbon.GetLayoutMode(host));
                    Assert.True(source.IsVisible); Assert.Equal(density, ribbon.Density);
                    var tabs = Descendants(ribbon).OfType<RibbonTabControl>().Single();
                    var minimize = (System.Windows.Controls.Primitives.ToggleButton)tabs.Template.FindName("MinimizeToggle", tabs);
                    Assert.Equal(Visibility.Collapsed, minimize.Visibility); Assert.False(minimize.IsEnabled);
                    Assert.False(new RibbonCustomizePage { Ribbon = ribbon }.IsEnabled);
                    Assert.False(new RibbonQuickAccessPage { Ribbon = ribbon }.IsEnabled);
                    Assert.Equal(RibbonLayoutMode.Simplified, other.LayoutMode);
                    var toolbar = Descendants(host).OfType<RibbonQuickAccessToolBar>().Single(); Assert.Empty(toolbar.Items);
                    ribbon.IsSimplifiedRibbonEnabled = true; Layout(window);
                    Assert.Equal(RibbonLayoutMode.Simplified, ribbon.LayoutMode);
                    Assert.Contains(toolbar.Items.OfType<FrameworkElement>(), item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), source));
                    Assert.False(new RibbonSimplifiedCustomizePage { Ribbon = ribbon }.IsEnabled);
                    string profile = RibbonCustomizationSerializer.Serialize(ribbon);
                    ribbon.IsSimplifiedRibbonEnabled = false; Layout(window);
                    Assert.Empty(toolbar.Items); RibbonCustomizationSerializer.Apply(ribbon, profile); Layout(window);
                    Assert.Equal(RibbonLayoutMode.Classic, ribbon.LayoutMode); Assert.False(ribbon.CanUserCustomizeQuickAccess);
                    Assert.Equal(RibbonSimplifiedPlacement.Inline, Ribbon.GetSimplifiedPlacement(source));
                    ribbon.CanUserCustomizeRibbon = true;
                    Assert.True(new RibbonCustomizePage { Ribbon = ribbon }.IsEnabled);
                    Assert.Equal(RibbonLayoutMode.Simplified, other.LayoutMode);
                }
                finally { window.Close(); Layout(window); }
            }
        }
        finally { RibbonAnimation.GlobalLevel = previous; }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Layout(Window window)
    {
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
    }
}
