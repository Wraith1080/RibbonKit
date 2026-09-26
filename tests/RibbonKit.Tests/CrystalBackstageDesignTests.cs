using System;
using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CrystalBackstageDesignTests
{
    [Fact]
    public void Detached_backstage_uses_crystal_layout_when_opened() => Sta.Run(() =>
    {
        var stage = new Backstage { Design = RibbonBackstageDesign.CrystalSidebar };
        stage.Items.Add(new BackstageTabItem { Header = "Home", Content = new TextBlock { Text = "Page" } });
        var ribbon = new Ribbon { Backstage = stage };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var window = new RibbonWindow
        {
            Content = new Grid { Children = { ribbon } },
            Width = 680,
            Height = 440,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        var crystalTokens = new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Light.xaml", UriKind.Relative) };
        window.Resources.MergedDictionaries.Add(crystalTokens);

        try
        {
            window.Show();
            Sta.Drain();
            ribbon.IsBackstageOpen = true;
            Sta.Drain();
            window.UpdateLayout();

            Assert.Equal(RibbonBackstageDesign.CrystalSidebar, stage.Design);
            var nav = Assert.IsType<Border>(stage.Template.FindName("NavColumn", stage));
            Assert.Equal(new CornerRadius(18), nav.CornerRadius);

            stage.Design = RibbonBackstageDesign.CrystalFloating;
            Sta.Drain();
            window.UpdateLayout();
            Assert.Null(stage.Template.FindName("NavColumn", stage));
            Assert.NotNull(stage.Template.FindName("ContentArea", stage));

            window.Resources.MergedDictionaries.Remove(crystalTokens);
            window.Resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
            stage.Design = RibbonBackstageDesign.Modern;
            Sta.Drain();
            window.UpdateLayout();
            Assert.NotNull(stage.Template.FindName("NavColumn", stage));
            Assert.NotEqual(new CornerRadius(18),
                Assert.IsType<Border>(stage.Template.FindName("NavColumn", stage)).CornerRadius);
        }
        finally
        {
            window.Close();
        }
    });

    [Fact]
    public void Backstage_design_selects_each_crystal_layout_without_a_showcase_style_override() => Sta.Run(() =>
    {
        Assert.Equal(5, (int)RibbonBackstageDesign.CrystalSidebar);
        Assert.Equal(6, (int)RibbonBackstageDesign.CrystalFloating);
        var host = new Grid();
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Office2024.xaml", UriKind.Relative) });
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Light.xaml", UriKind.Relative) });

        var stage = new Backstage { Design = RibbonBackstageDesign.Modern };
        var page = new BackstageTabItem { Header = "Overview", Content = new TextBlock { Text = "Document" } };
        stage.Items.Add(page);
        stage.SelectedItem = page;
        host.Children.Add(stage);

        void Layout(double width = 680)
        {
            host.Measure(new Size(width, 440));
            host.Arrange(new Rect(0, 0, width, 440));
            host.UpdateLayout();
            Sta.Drain();
        }

        Layout();
        var officeTemplate = stage.Template;
        Assert.NotNull(officeTemplate);
        Assert.Equal(DependencyProperty.UnsetValue, stage.ReadLocalValue(FrameworkElement.StyleProperty));

        stage.Design = RibbonBackstageDesign.CrystalSidebar;
        Layout();
        Assert.Same(stage.FindResource("Crystal.Backstage.SidebarTemplate"), stage.Template);
        Assert.NotNull(stage.Template.FindName("NavColumn", stage));
        Assert.Same(page, stage.SelectedItem);

        stage.Design = RibbonBackstageDesign.CrystalFloating;
        Layout();
        Assert.Same(stage.FindResource("Crystal.Backstage.FloatingTemplate"), stage.Template);
        Assert.Null(stage.Template.FindName("NavColumn", stage));
        Assert.Same(page, stage.SelectedItem);
        var contentArea = Assert.IsType<Grid>(stage.Template.FindName("ContentArea", stage));
        Assert.Equal(stage.ActualWidth - 56, contentArea.ActualWidth, 1);
        Layout(1600);
        Assert.Equal(stage.ActualWidth - 56, contentArea.ActualWidth, 1);

        stage.Design = RibbonBackstageDesign.Modern;
        Layout();
        Assert.Same(officeTemplate, stage.Template);
        Assert.Equal(DependencyProperty.UnsetValue, stage.ReadLocalValue(FrameworkElement.StyleProperty));
    });
}
