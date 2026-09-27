using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CrystalLocalizationIntegrationTests
{
    [Fact]
    public void Detached_rtl_lab_applies_crystal_and_restores_office_options() => Sta.Run(() =>
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Icons.xaml", UriKind.Relative) });
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var demo = new LocalizationRtlDemo
        {
            Left = -10000, Top = -10000, Width = 1800, ShowActivated = false, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
        };
        try
        {
            demo.Show();
            Sta.Drain();
            Assert.Null(demo.TryFindResource("Crystal.Brushes.FrostedFrame"));
            demo.RightToLeftToggle.IsChecked = true;
            Sta.Drain();
            demo.UpdateLayout();
            AssertOptionIndicatorSide(demo.RtlCheckBox, rtl: true);
            AssertOptionIndicatorSide(demo.RtlRadioButton, rtl: true);
            demo.RightToLeftToggle.IsChecked = false;
            Sta.Drain();
            demo.DemoProtectedViewMessage.IsOpen = true;
            Sta.Drain();
            var messageRoot = Assert.IsType<Border>(demo.DemoProtectedViewMessage.Template.FindName(
                "PART_Root", demo.DemoProtectedViewMessage));
            var officeMessageRadius = messageRoot.CornerRadius;

            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            demo.ApplyCrystal(true, Colors.Purple);
            demo.RightToLeftToggle.IsChecked = true;
            demo.PseudoLocalizationToggle.IsChecked = true;
            Sta.Drain();
            demo.UpdateLayout();
            AssertOptionIndicatorSide(demo.RtlCheckBox, rtl: true);
            AssertOptionIndicatorSide(demo.RtlRadioButton, rtl: true);
            demo.DemoBackstage.Design = RibbonBackstageDesign.CrystalSidebar;
            demo.DemoRibbon.IsBackstageOpen = true;
            Sta.Drain();
            demo.UpdateLayout();
            Assert.Equal(FlowDirection.RightToLeft, demo.DemoBackstage.FlowDirection);
            AssertBackButton(demo.DemoBackstage, "→", "⟦Back⟧");
            var sidebarHeading = Assert.IsType<TextBlock>(demo.DemoBackstage.Template.FindName(
                "CrystalFileHeading", demo.DemoBackstage));
            Assert.Equal("⟦File⟧", sidebarHeading.Text);
            AssertNavSide(demo.DemoBackstage, "NavColumn", rtl: true);

            demo.DemoBackstage.Design = RibbonBackstageDesign.CrystalFloating;
            Sta.Drain();
            demo.UpdateLayout();
            AssertBackButton(demo.DemoBackstage, "→", "⟦Back⟧");
            AssertNavSide(demo.DemoBackstage, "NavSurface", rtl: true);
            AssertFloatingHeaderSides(demo.DemoBackstage, rtl: true);

            demo.PseudoLocalizationToggle.IsChecked = false;
            Sta.Drain();
            AssertBackButton(demo.DemoBackstage, "→", "Back");
            demo.RightToLeftToggle.IsChecked = false;
            Sta.Drain();
            demo.UpdateLayout();
            AssertBackButton(demo.DemoBackstage, "←", "Back");
            AssertNavSide(demo.DemoBackstage, "NavSurface", rtl: false);
            AssertFloatingHeaderSides(demo.DemoBackstage, rtl: false);
            demo.DemoBackstage.Design = RibbonBackstageDesign.CrystalSidebar;
            Sta.Drain();
            demo.UpdateLayout();
            AssertBackButton(demo.DemoBackstage, "←", "Back");
            AssertNavSide(demo.DemoBackstage, "NavColumn", rtl: false);
            demo.RightToLeftToggle.IsChecked = true;
            demo.DemoRibbon.IsBackstageOpen = false;
            Sta.Drain();
            demo.RightToLeftToggle.IsChecked = false;
            Sta.Drain();
            demo.UpdateLayout();
            AssertOptionIndicatorSide(demo.RtlCheckBox, rtl: false);
            AssertOptionIndicatorSide(demo.RtlRadioButton, rtl: false);
            demo.RightToLeftToggle.IsChecked = true;

            Assert.NotNull(demo.TryFindResource("Crystal.Brushes.FrostedFrame"));
            Assert.True(demo.DemoApplicationMenu.Resources.Contains(
                "RibbonKit.Brushes.ApplicationMenu.FrameBorder"));
            Assert.Equal(new CornerRadius(10), messageRoot.CornerRadius);
            var purpleHover = Assert.IsType<SolidColorBrush>(demo.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            var crystalDialog = demo.CreateOptionsDialog(showQuickAccessPage: true);
            Assert.Equal(FlowDirection.RightToLeft, crystalDialog.FlowDirection);
            Assert.IsType<RibbonQuickAccessPage>(crystalDialog.SelectedPage!.Content);
            Assert.NotNull(crystalDialog.TryFindResource("Crystal.Customize.Navigation"));
            AssertCustomizeTreeDirection(crystalDialog);

            demo.ApplyCrystal(true, Colors.SeaGreen);
            Sta.Drain();
            var greenHover = Assert.IsType<SolidColorBrush>(demo.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            Assert.NotEqual(purpleHover, greenHover);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            demo.ApplyCrystal(false);
            Sta.Drain();
            Assert.Null(demo.TryFindResource("Crystal.Brushes.FrostedFrame"));
            Assert.False(demo.DemoApplicationMenu.Resources.Contains(
                "RibbonKit.Brushes.ApplicationMenu.FrameBorder"));
            Assert.Equal(officeMessageRadius, messageRoot.CornerRadius);
            var officeDialog = demo.CreateOptionsDialog(showQuickAccessPage: false);
            Assert.Equal(FlowDirection.RightToLeft, officeDialog.FlowDirection);
            Assert.IsType<RibbonCustomizePage>(officeDialog.SelectedPage!.Content);
            Assert.Null(officeDialog.TryFindResource("Crystal.Customize.Navigation"));
        }
        finally
        {
            demo.Close();
            application.Shutdown();
        }
    });

    private static void AssertBackButton(Backstage stage, string arrow, string label)
    {
        var button = Assert.IsType<Button>(stage.Template.FindName("PART_BackButton", stage));
        var content = Assert.IsType<StackPanel>(button.Content);
        var arrowBlock = Assert.IsType<TextBlock>(content.Children[0]);
        var labelBlock = Assert.IsType<TextBlock>(content.Children[1]);
        Assert.Equal(arrow, arrowBlock.Text);
        Assert.Equal(label, labelBlock.Text);
        double arrowLeft = ScreenX(arrowBlock).left;
        double labelLeft = ScreenX(labelBlock).left;
        if (stage.FlowDirection == FlowDirection.RightToLeft)
            Assert.True(arrowLeft > labelLeft);
        else
            Assert.True(arrowLeft < labelLeft);
        Assert.Equal(label, AutomationProperties.GetName(button));
    }

    private static void AssertNavSide(Backstage stage, string navName, bool rtl)
    {
        var nav = Assert.IsType<Border>(stage.Template.FindName(navName, stage));
        var content = Assert.IsAssignableFrom<FrameworkElement>(
            stage.Template.FindName("ContentArea", stage));
        var (navLeft, navRight) = ScreenX(nav);
        var (contentLeft, contentRight) = ScreenX(content);
        if (navName == "NavSurface")
        {
            if (rtl)
                Assert.True(navRight >= contentRight - 1, $"nav={navLeft}, content={contentLeft}");
            else
                Assert.True(navLeft <= contentLeft + 1, $"nav={navLeft}, content={contentLeft}");
        }
        else if (rtl)
            Assert.True(navLeft > contentLeft, $"nav={navLeft}, content={contentLeft}");
        else
            Assert.True(navLeft < contentLeft, $"nav={navLeft}, content={contentLeft}");
    }

    private static void AssertFloatingHeaderSides(Backstage stage, bool rtl)
    {
        var button = Assert.IsType<Button>(stage.Template.FindName("PART_BackButton", stage));
        var brand = Assert.IsType<TextBlock>(stage.Template.FindName("CrystalBrand", stage));
        double backLeft = ScreenX(button).left;
        double brandLeft = ScreenX(brand).left;
        if (rtl)
            Assert.True(backLeft > brandLeft);
        else
            Assert.True(backLeft < brandLeft);
    }

    private static void AssertOptionIndicatorSide(Control option, bool rtl)
    {
        var indicator = Assert.IsAssignableFrom<FrameworkElement>(option.Template.FindName("Indicator", option));
        var header = Assert.IsType<ContentPresenter>(option.Template.FindName("HeaderPresenter", option));
        double indicatorLeft = ScreenX(indicator).left;
        double headerLeft = ScreenX(header).left;
        if (rtl)
            Assert.True(indicatorLeft > headerLeft);
        else
            Assert.True(indicatorLeft < headerLeft);
    }

    private static void AssertCustomizeTreeDirection(RibbonOptionsDialog dialog)
    {
        var template = Assert.IsType<ControlTemplate>(dialog.FindResource("Crystal.Customize.TreeItem"));
        var treeItem = new TreeViewItem
        {
            FlowDirection = FlowDirection.RightToLeft, Header = "Item", Template = template,
        };
        treeItem.Items.Add(new TreeViewItem { Header = "Child" });
        var physicalHost = new Grid { Width = 400, Height = 200, FlowDirection = FlowDirection.LeftToRight };
        physicalHost.Children.Add(treeItem);
        physicalHost.Measure(new Size(400, 200));
        physicalHost.Arrange(new Rect(0, 0, 400, 200));
        var expander = Assert.IsType<ToggleButton>(template.FindName("Expander", treeItem));
        var row = Assert.IsType<Border>(template.FindName("Row", treeItem));
        double expanderLeft = expander.TransformToVisual(physicalHost).TransformBounds(
            new Rect(0, 0, expander.ActualWidth, expander.ActualHeight)).Left;
        double rowLeft = row.TransformToVisual(physicalHost).TransformBounds(
            new Rect(0, 0, row.ActualWidth, row.ActualHeight)).Left;
        Assert.True(expanderLeft > rowLeft);
        expander.ApplyTemplate();
        var arrow = Assert.IsType<Path>(expander.Template.FindName("Arrow", expander));
        var geometry = arrow.Data.GetFlattenedPathGeometry();
        geometry.GetPointAtFractionLength(0, out Point tail, out _);
        geometry.GetPointAtFractionLength(0.5, out Point tip, out _);
        var toHost = arrow.TransformToVisual(physicalHost);
        Assert.True(toHost.Transform(tip).X < toHost.Transform(tail).X);
        expander.IsChecked = true;
        toHost = arrow.TransformToVisual(physicalHost);
        Assert.True(toHost.Transform(tip).Y > toHost.Transform(tail).Y);
    }

    private static (double left, double right) ScreenX(FrameworkElement element)
    {
        double x0 = element.PointToScreen(new Point()).X;
        double x1 = element.PointToScreen(new Point(element.ActualWidth, 0)).X;
        return (Math.Min(x0, x1), Math.Max(x0, x1));
    }
}
