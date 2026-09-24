using System;
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
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
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
        var split = new RibbonSplitButton { Header = "Paste" };
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
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show();
            Sta.Drain();
            Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(
                window.FindResource("RibbonKit.Brushes.Control.SplitActiveHover")).Color);
            var drawer = Assert.IsType<Border>(ribbon.Template.FindName("QatBelowHost", ribbon));
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

            Assert.Contains(window.Resources.MergedDictionaries, dictionary =>
                dictionary.Source?.OriginalString.EndsWith("Crystal.Light.xaml", StringComparison.Ordinal) == true);
            Assert.Same(window.FindResource(typeof(RibbonComboBox)), combo.Style);
            Assert.Same(window.FindResource(typeof(ScrollBar)),
                presentation.Palette![typeof(ScrollBar)]);
            Assert.True(context.CrystalEnabled);
            Assert.IsType<DrawingBrush>(context.ContextualSelectionBrush);
            Assert.Same(split.FindResource("RibbonKit.Brushes.Control.SplitActiveHover"),
                split.Resources["RibbonKit.Brushes.Control.HoverBackground"]);
            Assert.Same(backstage.FindResource("Crystal.Backstage.Sidebar"), backstage.Style);
            backstage.Design = RibbonBackstageDesign.Classic;
            presentation.UpdateBackstageStyle();
            Assert.Same(originalBackstageStyle, backstage.Style);
            backstage.Design = RibbonBackstageDesign.Modern;
            presentation.UpdateBackstageStyle();
            Assert.Same(backstage.FindResource("Crystal.Backstage.Sidebar"), backstage.Style);
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
            Assert.Same(ribbon.FindResource("Crystal.Effects.QuickAccessShadow"), drawer.Effect);
            Assert.Equal(new CornerRadius(10), messageRoot.CornerRadius);

            var blueHover = Assert.IsType<SolidColorBrush>(window.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            presentation.Apply(true, Colors.Purple);
            var purpleHover = Assert.IsType<SolidColorBrush>(window.FindResource(
                "RibbonKit.Brushes.Control.HoverBackground")).Color;
            Assert.NotEqual(blueHover, purpleHover);
            Assert.Same(window.FindResource(typeof(RibbonComboBox)), combo.Style);
            Assert.Same(split.FindResource("RibbonKit.Brushes.Control.SplitActiveHover"),
                split.Resources["RibbonKit.Brushes.Control.HoverBackground"]);

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
            Assert.Same(originalComboStyle, combo.Style);
            Assert.Same(originalBackstageStyle, backstage.Style);
            Assert.False(context.CrystalEnabled);
            Assert.Null(context.ContextualSelectionBrush);
            Assert.False(split.Resources.Contains("RibbonKit.Brushes.Control.HoverBackground"));
            Assert.Null(window.TryFindResource("Crystal.Brushes.FrostedFrame"));

            foreach (var office in new[] { RibbonTheme.Office2019, RibbonTheme.Office2013,
                RibbonTheme.Office2010, RibbonTheme.Office2007 })
            {
                ThemeManager.Apply(application, office);
                presentation.Apply(false);
                Assert.DoesNotContain(window.Resources.MergedDictionaries, dictionary =>
                    dictionary.Source?.OriginalString.EndsWith("Crystal.Light.xaml", StringComparison.Ordinal) == true);
                Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(
                    window.FindResource("RibbonKit.Brushes.Control.SplitActiveHover")).Color);
                Assert.Null(window.TryFindResource("Crystal.Brushes.FrostedFrame"));
            }
        }
        finally
        {
            window.Close();
            application.Shutdown();
        }
    });
}
