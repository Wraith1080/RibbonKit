using System;
using System.Linq;
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
    public void Bilingual_application_menu_label_stays_within_primary_hit_area() => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.CrystalLight);
        var demo = new LocalizationRtlDemo
        {
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
        };
        try
        {
            demo.DemoRibbon.ApplicationMenu = demo.DemoApplicationMenu;
            demo.Show();
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.SetDarkMode(application, dark);
                demo.ApplyCrystal(true);
                foreach (bool rtl in new[] { false, true })
                {
                    demo.RightToLeftToggle.IsChecked = rtl;
                    demo.DemoRibbon.IsBackstageOpen = true;
                    Sta.Drain();
                    demo.UpdateLayout();
                    var item = demo.DemoApplicationMenu.Items.OfType<RibbonApplicationMenuItem>()
                        .Single(entry => entry.HasPane);
                    var primary = Assert.IsType<Button>(item.Template.FindName("PART_Primary", item));
                    var text = FindMenuLabel(primary, Assert.IsType<string>(item.Header));
                    Assert.NotNull(text);
                    Assert.True(primary.ActualWidth > 0 && primary.ActualHeight > 0);
                    int positions = 0;
                    for (var position = text!.ContentStart; position is not null
                         && position.CompareTo(text.ContentEnd) < 0;
                         position = position.GetNextInsertionPosition(System.Windows.Documents.LogicalDirection.Forward))
                    {
                        var character = position.GetCharacterRect(System.Windows.Documents.LogicalDirection.Forward);
                        if (character.IsEmpty) continue;
                        var bounds = text.TransformToAncestor(primary).TransformBounds(character);
                        Assert.True(bounds.Left >= -1 && bounds.Right <= primary.ActualWidth + 1
                            && bounds.Top >= -1 && bounds.Bottom <= primary.ActualHeight + 1,
                            $"dark={dark}, rtl={rtl}: character {bounds} exceeds primary {primary.RenderSize}");
                        positions++;
                    }
                    Assert.True(positions >= 10);
                    demo.DemoRibbon.IsBackstageOpen = false;
                }
            }
        }
        finally
        {
            demo.Close();
            Sta.ResetApplication();
        }
    });

    private static TextBlock? FindMenuLabel(DependencyObject parent, string label)
    {
        if (parent is TextBlock text && text.Text == label) return text;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var found = FindMenuLabel(VisualTreeHelper.GetChild(parent, index), label);
            if (found is not null) return found;
        }
        return null;
    }

    [Theory]
    [InlineData(1800, 1.25)]
    [InlineData(720, 1.25)]
    [InlineData(1800, 2)]
    [InlineData(720, 2)]
    public void Detached_rtl_lab_applies_crystal_and_restores_office_options(double width, double scale) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Icons.xaml", UriKind.Relative) });
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var demo = new LocalizationRtlDemo
        {
            Left = -10000, Top = -10000, Width = width, ShowActivated = false, ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
        };
        try
        {
            demo.Show();
            VisualTreeHelper.SetRootDpi(demo, new DpiScale(scale, scale));
            Sta.Drain();
            Assert.Null(demo.TryFindResource("Crystal.Brushes.FrostedFrame"));
            demo.RightToLeftToggle.IsChecked = true;
            Sta.Drain();
            demo.UpdateLayout();
            AssertOptionIndicatorSide(demo.RtlCheckBox, rtl: true, scale);
            AssertOptionIndicatorSide(demo.RtlRadioButton, rtl: true, scale);
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
            AssertOptionIndicatorSide(demo.RtlCheckBox, rtl: true, scale);
            AssertOptionIndicatorSide(demo.RtlRadioButton, rtl: true, scale);
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
            AssertOptionIndicatorSide(demo.RtlCheckBox, rtl: false, scale);
            AssertOptionIndicatorSide(demo.RtlRadioButton, rtl: false, scale);
            demo.RightToLeftToggle.IsChecked = true;

            Assert.NotNull(demo.TryFindResource("Crystal.Brushes.FrostedFrame"));
            Assert.False(demo.DemoApplicationMenu.Resources.Contains(
                "RibbonKit.Brushes.ApplicationMenu.FrameBorder"));
            Assert.IsAssignableFrom<Brush>(demo.FindResource("RibbonKit.Brushes.ApplicationMenu.FrameBorder"));
            Assert.Equal(new CornerRadius(10), messageRoot.CornerRadius);
            var purpleHover = Assert.IsType<SolidColorBrush>(demo.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            var crystalDialog = demo.CreateOptionsDialog(showQuickAccessPage: true);
            Assert.Equal(FlowDirection.RightToLeft, crystalDialog.FlowDirection);
            Assert.IsType<RibbonQuickAccessPage>(crystalDialog.SelectedPage!.Content);
            Assert.Equal(1d, crystalDialog.FindResource("RibbonKit.Metrics.OptionsDialog.BottomMarkerOpacity"));
            AssertCustomizeTreeDirection(crystalDialog);

            demo.ApplyCrystal(true, Colors.SeaGreen);
            Sta.Drain();
            var greenHover = Assert.IsType<SolidColorBrush>(demo.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            Assert.NotEqual(purpleHover, greenHover);

            ThemeManager.SetDarkMode(application, true);
            demo.ApplyCrystal(true, Colors.SeaGreen);
            Sta.Drain();
            demo.UpdateLayout();
            AssertOptionIndicatorSide(demo.RtlCheckBox, rtl: true, scale);
            AssertOptionIndicatorSide(demo.RtlRadioButton, rtl: true, scale);
            Color darkGlyph = Assert.IsType<SolidColorBrush>(demo.RtlCheckBox.FindResource(
                "RibbonKit.Brushes.Option.Glyph")).Color;
            Assert.True(darkGlyph.R > 0xD0 && darkGlyph.G > 0xD0 && darkGlyph.B > 0xD0);
            Assert.IsType<DrawingBrush>(demo.RtlCheckBox.FindResource("RibbonKit.Brushes.Option.SelectedSurface"));
            ThemeManager.SetDarkMode(application, false);

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
            Assert.Equal(0d, officeDialog.FindResource("RibbonKit.Metrics.OptionsDialog.BottomMarkerOpacity"));
        }
        finally
        {
            demo.Close();
            Sta.ResetApplication();
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
        Assert.Null(stage.Template.FindName("CrystalBrand", stage));
        var content = Assert.IsType<Grid>(stage.Template.FindName("ContentArea", stage));
        var (backLeft, backRight) = ScreenX(button);
        var (contentLeft, contentRight) = ScreenX(content);
        Assert.True(button.ActualWidth > 0);
        if (rtl)
            Assert.True(Math.Abs(backRight - contentRight) <= 1, $"back={backRight}, content={contentRight}");
        else
            Assert.True(Math.Abs(backLeft - contentLeft) <= 1, $"back={backLeft}, content={contentLeft}");
    }

    private static void AssertOptionIndicatorSide(Control option, bool rtl, double scale)
    {
        DependencyObject? ancestor = option;
        while (ancestor is not null && ancestor is not RibbonGroup)
            ancestor = LogicalTreeHelper.GetParent(ancestor) ?? VisualTreeHelper.GetParent(ancestor);
        var group = Assert.IsType<RibbonGroup>(ancestor);
        bool open = group.SizeState == RibbonGroupSizeState.Collapsed;
        if (open)
        {
            // A native high-DPI window can be narrower than its requested DIP width.
            // Measure the visible flyout instead of the hidden, zero-sized group content.
            group.CollapsedButton!.IsChecked = true;
            Sta.Drain();
            group.UpdateLayout();
        }
        try
        {
            // A group flyout has a separate visual root. Apply the simulated scale there too;
            // the popup HWND itself still uses the monitor's native DPI.
            Visual root = option;
            while (VisualTreeHelper.GetParent(root) is Visual parent) root = parent;
            VisualTreeHelper.SetRootDpi(root, new DpiScale(scale, scale));
            Sta.Drain();
            option.UpdateLayout();
            Assert.Equal(scale, VisualTreeHelper.GetDpi(option).DpiScaleX, 5);
            option.ApplyTemplate();
            var indicator = Assert.IsAssignableFrom<FrameworkElement>(option.Template.FindName("Indicator", option));
            var header = Assert.IsType<ContentPresenter>(option.Template.FindName("HeaderPresenter", option));
            Assert.True(option.ActualWidth > 0 && indicator.ActualWidth > 0 && header.ActualWidth > 0);
            double indicatorLeft = ScreenX(indicator).left;
            double headerLeft = ScreenX(header).left;
            Assert.True(rtl ? indicatorLeft > headerLeft : indicatorLeft < headerLeft,
                $"{option.Name}: indicator={indicatorLeft}, header={headerLeft}, option width={option.ActualWidth}, flow={option.FlowDirection}");
        }
        finally
        {
            if (open) group.CollapsedButton!.IsChecked = false;
        }
    }

    private static void AssertCustomizeTreeDirection(RibbonOptionsDialog dialog)
    {
        var page = Assert.IsType<RibbonCustomizePage>(dialog.Pages.Single(entry => entry.Content is RibbonCustomizePage).Content);
        page.ApplyTemplate();
        var tree = Assert.IsType<TreeView>(page.Template.FindName("PART_Tree", page));
        var style = Assert.IsType<Style>(tree.ItemContainerStyle);
        var template = Assert.IsType<ControlTemplate>(Assert.Single(style.Setters.OfType<Setter>(),
            setter => setter.Property == Control.TemplateProperty).Value);
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
