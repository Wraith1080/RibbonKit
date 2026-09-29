using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CrystalMainWindowPresentationTests
{
    [Fact]
    public void Crystal_host_details_restore_office_presentation_when_theme_changes() => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        var message = new RibbonMessage { Title = "Notice", Message = "Sample", IsOpen = true };
        var bar = new RibbonMessageBar();
        bar.Items.Add(message);
        var menu = new RibbonApplicationMenu();
        var backstage = new Backstage { Design = RibbonBackstageDesign.Modern };
        var ribbon = new Ribbon
        {
            QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
            MessageBar = bar,
            ApplicationMenu = menu,
            Backstage = backstage,
        };
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        var combo = new RibbonComboBox();
        combo.Items.Add("One");
        group.Items.Add(combo);
        var split = new RibbonSplitButton { Header = "Paste", Size = RibbonControlSize.Large,
            Layout = RibbonSplitButtonLayout.Vertical };
        group.Items.Add(split);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        var context = new CrystalContextualTab { Header = "Picture", IsContextual = true,
            ContextualColor = Brushes.Purple, CrystalEnabled = false };
        context.Groups.Add(new RibbonGroup { Header = "Picture tools" });
        ribbon.Tabs.Add(context);
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        var window = new RibbonWindow { Content = ribbon, Width = 700, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            AssertOfficeBodyTokenParity();
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show();
            Sta.Drain();
            AssertSamePaint((Brush)window.FindResource("RibbonKit.Brushes.Control.HoverBackground"),
                (Brush)window.FindResource("RibbonKit.Brushes.Control.SplitActiveHover"));
            var drawer = Assert.IsType<Border>(ribbon.Template.FindName("QatBelowHost", ribbon));
            Assert.Equal(0, drawer.MinHeight);
            var originalDrawerRadius = drawer.CornerRadius;
            var originalDrawerEffect = drawer.Effect;
            var messageRoot = Assert.IsType<Border>(message.Template.FindName("PART_Root", message));
            var originalMessageRadius = messageRoot.CornerRadius;
            var originalComboStyle = combo.Style;
            var originalBackstageStyle = backstage.Style;
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            var presentation = new CrystalMainWindowPresentation(window, ribbon, bar, menu, backstage);
            presentation.Apply(true);
            Sta.Drain();
            window.UpdateLayout();
            var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            var notch = Assert.IsType<Border>(tabs.Template.FindName("PART_ConnectNotch", tabs));
            var foot = Assert.IsType<Border>(tab.Template.FindName("ConnectFoot", tab));
            Assert.Equal(1d, Assert.IsType<TranslateTransform>(foot.RenderTransform).Y);
            Assert.True(notch.Width > 0d);
            Assert.Equal(Color.FromRgb(0xC7, 0xDE, 0xEF),
                Assert.IsType<SolidColorBrush>(tab.FindResource("RibbonKit.Brushes.Tab.ConnectNotch")).Color);
            Assert.Same(tab.FindResource("RibbonKit.Brushes.Tab.ConnectFootSelected"), foot.Background);
            Assert.Equal(1d, foot.Opacity);
            Assert.Equal(Assert.IsType<SolidColorBrush>(foot.Background).Color,
                Assert.IsType<SolidColorBrush>(notch.Background).Color);

            Assert.Contains(window.Resources.MergedDictionaries, dictionary =>
                dictionary.Source?.OriginalString.EndsWith("Crystal.Light.xaml", StringComparison.Ordinal) == true);
            Assert.Null(combo.Style);
            Assert.Same(window.FindResource(typeof(ScrollBar)),
                presentation.Palette![typeof(ScrollBar)]);
            Assert.True(context.CrystalEnabled);
            Assert.IsType<DrawingBrush>(context.ContextualSelectionBrush);
            Assert.NotSame(split.FindResource("RibbonKit.Brushes.Control.HoverBackground"),
                split.FindResource("RibbonKit.Brushes.Control.SplitActiveHover"));
            Assert.False(split.Resources.Contains("RibbonKit.Brushes.Control.HoverBackground"));
            Assert.Same(originalBackstageStyle, backstage.Style);
            backstage.Design = RibbonBackstageDesign.CrystalSidebar;
            Assert.Same(originalBackstageStyle, backstage.Style);
            backstage.Design = RibbonBackstageDesign.CrystalFloating;
            Assert.Same(originalBackstageStyle, backstage.Style);
            backstage.Design = RibbonBackstageDesign.Modern;
            Assert.Same(originalBackstageStyle, backstage.Style);
            var source = new RibbonMergeSource();
            var merged = new CrystalContextualTab { Header = "Chart", IsContextual = true,
                ContextualColor = Brushes.SeaGreen, CrystalEnabled = false };
            merged.Groups.Add(new RibbonGroup { Header = "Chart tools" });
            source.Tabs.Add(merged);
            Assert.True(ribbon.Merge(source));
            Sta.Drain();
            Assert.True(merged.CrystalEnabled);
            Assert.IsType<DrawingBrush>(merged.ContextualSelectionBrush);
            Assert.True(ribbon.Unmerge(source));
            Assert.False(merged.CrystalEnabled);
            Assert.Equal(new CornerRadius(0, 0, 10, 10), drawer.CornerRadius);
            Assert.Equal(32, drawer.MinHeight);
            Assert.Same(ribbon.FindResource("RibbonKit.Effects.QatExtenderShadow"), drawer.Effect);
            AssertSamePaint((Brush)window.FindResource("RibbonKit.Brushes.Tab.HoverBackground"),
                drawer.Background);
            Assert.Equal(new CornerRadius(10), messageRoot.CornerRadius);
            Assert.Equal(new Thickness(0, 2, 0, 2), message.Margin);
            Assert.True(split.IsVerticalLayout);
            var primary = Assert.IsType<Button>(split.Template.FindName("PART_Primary", split));
            var primaryChrome = Assert.IsType<Border>(primary.Template.FindName("Chrome", primary));
            Assert.Equal(new Thickness(1, 1, 1, 0), primaryChrome.BorderThickness);

            var blueHover = Assert.IsType<SolidColorBrush>(window.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            presentation.Apply(true, Colors.Purple);
            var purpleHover = Assert.IsType<SolidColorBrush>(window.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            var opaqueNotch = Assert.IsType<SolidColorBrush>(notch.Background).Color;
            Assert.NotEqual(blueHover, purpleHover);
            AssertSamePaint((Brush)window.FindResource("RibbonKit.Brushes.Tab.HoverBackground"),
                drawer.Background);
            Assert.Null(combo.Style);
            Assert.Same(window.FindResource("RibbonKit.Brushes.Input.SurfaceBackground"),
                Assert.IsType<Border>(combo.Template.FindName("Chrome", combo)).Background);
            Assert.False(split.Resources.Contains("RibbonKit.Brushes.Control.HoverBackground"));

            var opaqueBody = (Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground");
            var glass = new AcrylicGlassPresentation(window);
            glass.Apply(true, darkMode: false);
            Sta.Drain();
            var acrylicHover = Assert.IsType<SolidColorBrush>(split.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground"));
            Assert.Equal((byte)0x98, acrylicHover.Color.A);
            Assert.True(acrylicHover.Color.R > acrylicHover.Color.G);
            Assert.True(acrylicHover.Color.B > acrylicHover.Color.G);
            Assert.NotEqual(purpleHover, acrylicHover.Color);
            var purpleQatGlass = Assert.IsType<SolidColorBrush>(drawer.Background).Color;
            Assert.Equal((byte)0x50, purpleQatGlass.A);
            Assert.NotEqual(Color.FromArgb(0x50, 0xF3, 0xFA, 0xFF), purpleQatGlass);
            Assert.Equal(purpleQatGlass, Assert.IsType<SolidColorBrush>(tab.FindResource(
                "RibbonKit.Brushes.Tab.HoverBackground")).Color);
            Assert.Equal(0d, tab.FindResource("RibbonKit.Metrics.TabHoverConnectFootOpacity"));
            Assert.Equal(acrylicHover.Color, Assert.IsType<SolidColorBrush>(split.FindResource(
                "RibbonKit.Brushes.Control.CompanionBackground")).Color);
            Assert.Equal(Color.FromArgb(0xB8, acrylicHover.Color.R,
                    acrylicHover.Color.G, acrylicHover.Color.B),
                Assert.IsType<SolidColorBrush>(split.FindResource(
                    "RibbonKit.Brushes.Control.SplitActiveHover")).Color);
            Assert.Equal(0.44, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity, 2);
            Assert.Equal(0.62, ((Brush)window.FindResource("RibbonKit.Brushes.Tab.SelectedBackground")).Opacity, 2);
            Assert.True(((Brush)window.FindResource("RibbonKit.Brushes.Tab.SelectedUnderline")).Opacity < 0.9);
            Assert.Equal(0.48, ((Brush)window.FindResource("RibbonKit.Brushes.TitleBar.Background")).Opacity, 2);
            Assert.Equal(0.48, ((Brush)window.FindResource("RibbonKit.Brushes.Window.Background")).Opacity, 2);
            Assert.Equal(1, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground")).Opacity);
            var body = Assert.IsType<Border>(tabs.Template.FindName("ContentHost", tabs));
            Assert.Same(window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"), body.Background);
            Assert.Equal(Assert.IsType<SolidColorBrush>(foot.Background).Color,
                Assert.IsType<SolidColorBrush>(notch.Background).Color);
            var file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
            Assert.Same(tab.FindResource("RibbonKit.Brushes.Tab.HoverBackground"),
                file.FindResource("RibbonKit.Brushes.ApplicationButton.HoverBackground"));
            glass.Apply(false, darkMode: false);
            Sta.Drain();
            AssertSamePaint(opaqueBody, (Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground"));
            Assert.Equal(1, ((Brush)window.FindResource("RibbonKit.Brushes.Ribbon.BodyBackground")).Opacity);
            Assert.Equal(purpleHover, Assert.IsType<SolidColorBrush>(split.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color);
            Assert.NotEqual(Color.FromArgb(0x50, 0xF3, 0xFA, 0xFF),
                Assert.IsType<SolidColorBrush>(tab.FindResource("RibbonKit.Brushes.Tab.HoverBackground")).Color);
            AssertSamePaint((Brush)window.FindResource("RibbonKit.Brushes.Tab.HoverBackground"),
                drawer.Background);
            Assert.Equal(0d, tab.FindResource("RibbonKit.Metrics.TabHoverConnectFootOpacity"));
            Assert.NotSame(tab.FindResource("RibbonKit.Brushes.Tab.HoverBackground"),
                file.FindResource("RibbonKit.Brushes.ApplicationButton.HoverBackground"));
            Assert.Equal(opaqueNotch, Assert.IsType<SolidColorBrush>(notch.Background).Color);
            Assert.Equal(Assert.IsType<SolidColorBrush>(foot.Background).Color,
                Assert.IsType<SolidColorBrush>(notch.Background).Color);

            presentation.Apply(true, Colors.SeaGreen);
            glass.Apply(true, darkMode: false);
            var greenGlassHover = Assert.IsType<SolidColorBrush>(split.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            Assert.NotEqual(acrylicHover.Color, greenGlassHover);
            Assert.True(greenGlassHover.G > greenGlassHover.R);
            var greenQatGlass = Assert.IsType<SolidColorBrush>(drawer.Background).Color;
            Assert.NotEqual(purpleQatGlass, greenQatGlass);
            Assert.True(greenQatGlass.G > greenQatGlass.R);
            Assert.Equal(greenQatGlass, Assert.IsType<SolidColorBrush>(tab.FindResource(
                "RibbonKit.Brushes.Tab.HoverBackground")).Color);
            Assert.Equal(Assert.IsType<SolidColorBrush>(foot.Background).Color,
                Assert.IsType<SolidColorBrush>(notch.Background).Color);
            glass.Apply(false, darkMode: false);

            presentation.Apply(true, Colors.Red);
            glass.Apply(true, darkMode: false);
            var redQatGlass = Assert.IsType<SolidColorBrush>(drawer.Background).Color;
            Assert.True(redQatGlass.R > redQatGlass.G);
            Assert.True(redQatGlass.R > redQatGlass.B);
            Assert.Equal(redQatGlass, Assert.IsType<SolidColorBrush>(tab.FindResource(
                "RibbonKit.Brushes.Tab.HoverBackground")).Color);
            glass.Apply(false, darkMode: false);

            ThemeManager.SetDarkMode(application, true);
            presentation.Apply(true, Colors.Red);
            Sta.Drain();
            Assert.EndsWith("Crystal.Dark.xaml", presentation.Palette!.Source!.OriginalString);
            Assert.Equal(Color.FromRgb(0xEA, 0xF4, 0xFC), Assert.IsType<SolidColorBrush>(
                window.FindResource("RibbonKit.Brushes.Text.Primary")).Color);
            var darkInput = Assert.IsType<DrawingBrush>(combo.FindResource(
                "RibbonKit.Brushes.Input.SurfaceBackground"));
            var inputDrawing = Assert.IsType<DrawingGroup>(darkInput.Drawing);
            var inputFace = Assert.IsType<RadialGradientBrush>(Assert.IsType<GeometryDrawing>(
                inputDrawing.Children[0]).Brush);
            Color darkInputFace = inputFace.GradientStops[0].Color;
            Assert.True(darkInputFace.R < 0x70 && darkInputFace.G < 0x70 && darkInputFace.B < 0x70);
            Assert.True(darkInputFace.R > darkInputFace.G);
            Assert.IsType<DrawingBrush>(context.ContextualSelectionBrush);
            var darkContext = Assert.IsType<RadialGradientBrush>(context.Resources[
                "RibbonKit.Brushes.Tab.SelectedBackground"]);
            Assert.True(darkContext.GradientStops[0].Color.R < 0x80);
            Assert.Equal(Assert.IsType<SolidColorBrush>(foot.Background).Color,
                Assert.IsType<SolidColorBrush>(notch.Background).Color);
            ThemeManager.SetDarkMode(application, false);
            presentation.Apply(true, Colors.Red);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            presentation.Apply(false);
            Sta.Drain();
            window.UpdateLayout();
            Assert.DoesNotContain(window.Resources.MergedDictionaries, dictionary =>
                dictionary.Source?.OriginalString.EndsWith("Crystal.Light.xaml", StringComparison.Ordinal) == true);
            Assert.Equal(originalDrawerRadius, drawer.CornerRadius);
            var originalShadow = Assert.IsType<DropShadowEffect>(originalDrawerEffect);
            var restoredShadow = Assert.IsType<DropShadowEffect>(drawer.Effect);
            Assert.Equal(originalShadow.BlurRadius, restoredShadow.BlurRadius);
            Assert.Equal(originalShadow.ShadowDepth, restoredShadow.ShadowDepth);
            Assert.Equal(originalShadow.Opacity, restoredShadow.Opacity);
            Assert.Equal(originalMessageRadius, messageRoot.CornerRadius);
            Assert.Equal(new Thickness(), message.Margin);
            Assert.Equal(0, drawer.MinHeight);
            Assert.Same(originalComboStyle, combo.Style);
            Assert.Same(originalBackstageStyle, backstage.Style);
            Assert.False(context.CrystalEnabled);
            Assert.Null(context.ContextualSelectionBrush);
            Assert.False(split.Resources.Contains("RibbonKit.Brushes.Control.HoverBackground"));
            Assert.Null(window.TryFindResource("Crystal.Brushes.FrostedFrame"));
            Assert.Equal(0d, notch.Width);
            var officePrimary = Assert.IsType<Button>(split.Template.FindName("PART_Primary", split));
            var officeChrome = Assert.IsType<Border>(officePrimary.Template.FindName("Chrome", officePrimary));
            Assert.Equal(new Thickness(1), officeChrome.BorderThickness);

            foreach (var office in new[] { RibbonTheme.Office2019, RibbonTheme.Office2013,
                RibbonTheme.Office2010, RibbonTheme.Office2007 })
            {
                ThemeManager.Apply(application, office);
                presentation.Apply(false);
                Assert.DoesNotContain(window.Resources.MergedDictionaries, dictionary =>
                    dictionary.Source?.OriginalString.EndsWith("Crystal.Light.xaml", StringComparison.Ordinal) == true);
                AssertSamePaint((Brush)window.FindResource("RibbonKit.Brushes.Control.HoverBackground"),
                    (Brush)window.FindResource("RibbonKit.Brushes.Control.SplitActiveHover"));
                glass.Apply(true, darkMode: false);
                var officeHover = Assert.IsType<SolidColorBrush>(split.FindResource(
                    "RibbonKit.Brushes.Control.HoverBackground")).Color;
                Assert.Equal(Color.FromArgb(0xB8, officeHover.R, officeHover.G, officeHover.B),
                    Assert.IsType<SolidColorBrush>(split.FindResource(
                        "RibbonKit.Brushes.Control.SplitActiveHover")).Color);
                glass.Apply(false, darkMode: false);
                Assert.Null(window.TryFindResource("Crystal.Brushes.FrostedFrame"));
            }
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, true);
            glass.Apply(true, darkMode: true);
            var darkGlassHover = Assert.IsType<SolidColorBrush>(split.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            Assert.Equal((byte)0x50, darkGlassHover.A);
            Assert.True(darkGlassHover.B > darkGlassHover.R);
            var darkTabHover = Assert.IsType<SolidColorBrush>(tab.FindResource(
                "RibbonKit.Brushes.Tab.HoverBackground")).Color;
            Assert.Equal((byte)0x30, darkTabHover.A);
            Assert.Equal(darkTabHover, Assert.IsType<SolidColorBrush>(drawer.Background).Color);
            glass.Apply(false, darkMode: true);
            ThemeManager.SetDarkMode(application, false);
        }
        finally
        {
            window.Close();
            Sta.ResetApplication();
        }
    });

    private static void AssertOfficeBodyTokenParity()
    {
        foreach (var generation in new[] { "2007", "2010", "2013", "2019", "2024" })
        {
            var resources = new ResourceDictionary();
            resources.MergedDictionaries.Add(new ResourceDictionary
            { Source = new Uri($"/RibbonKit;component/Themes/Tokens.Office{generation}.xaml", UriKind.Relative) });
            foreach (var dark in new[] { false, true })
            {
                if (dark)
                    resources.MergedDictionaries.Add(new ResourceDictionary
                    { Source = new Uri($"/RibbonKit;component/Themes/Tokens.Office{generation}.Dark.xaml", UriKind.Relative) });
                AssertSamePaint((Brush)resources["RibbonKit.Brushes.Ribbon.ContentBackground"],
                    (Brush)resources["RibbonKit.Brushes.Ribbon.BodyBackground"]);
                AssertSamePaint((Brush)resources["RibbonKit.Brushes.Ribbon.ContentBackground"],
                    (Brush)resources["RibbonKit.Brushes.QatExtender.Background"]);
                Assert.Equal(new Thickness(1),
                    resources["RibbonKit.Metrics.SplitVerticalPrimaryBorderThickness"]);
            }
        }
    }

    private static void AssertSamePaint(Brush expected, Brush actual)
    {
        if (expected is SolidColorBrush solid)
        {
            Assert.Equal(solid.Color, Assert.IsType<SolidColorBrush>(actual).Color);
            return;
        }

        var gradient = Assert.IsType<LinearGradientBrush>(expected);
        var splitGradient = Assert.IsType<LinearGradientBrush>(actual);
        Assert.Equal(gradient.StartPoint, splitGradient.StartPoint);
        Assert.Equal(gradient.EndPoint, splitGradient.EndPoint);
        Assert.Equal(gradient.GradientStops.Select(stop => (stop.Color, stop.Offset)),
            splitGradient.GradientStops.Select(stop => (stop.Color, stop.Offset)));
    }
}
