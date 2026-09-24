using System;
using System.Windows;
using System.Windows.Controls;
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
        var ribbon = new Ribbon
        {
            QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
            MessageBar = bar,
            ApplicationMenu = menu,
        };
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        var combo = new RibbonComboBox();
        combo.Items.Add("One");
        group.Items.Add(combo);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        var window = new RibbonWindow { Content = ribbon, Width = 700, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show();
            Sta.Drain();
            var drawer = Assert.IsType<Border>(ribbon.Template.FindName("QatBelowHost", ribbon));
            var originalDrawerRadius = drawer.CornerRadius;
            var originalDrawerEffect = drawer.Effect;
            var messageRoot = Assert.IsType<Border>(message.Template.FindName("PART_Root", message));
            var originalMessageRadius = messageRoot.CornerRadius;
            var originalComboStyle = combo.Style;
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            var presentation = new CrystalMainWindowPresentation(window, ribbon, bar, menu);
            presentation.Apply(true);
            Sta.Drain();
            window.UpdateLayout();

            Assert.Contains(window.Resources.MergedDictionaries, dictionary =>
                dictionary.Source?.OriginalString.EndsWith("Crystal.ControlStyles.xaml", StringComparison.Ordinal) == true);
            Assert.Same(window.FindResource(typeof(RibbonComboBox)), combo.Style);
            Assert.Equal(new CornerRadius(0, 0, 10, 10), drawer.CornerRadius);
            Assert.Same(ribbon.FindResource("Crystal.Effects.QuickAccessShadow"), drawer.Effect);
            Assert.Equal(new CornerRadius(10), messageRoot.CornerRadius);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            presentation.Apply(false);
            Sta.Drain();
            window.UpdateLayout();
            Assert.DoesNotContain(window.Resources.MergedDictionaries, dictionary =>
                dictionary.Source?.OriginalString.EndsWith("Crystal.ControlStyles.xaml", StringComparison.Ordinal) == true);
            Assert.Equal(originalDrawerRadius, drawer.CornerRadius);
            var originalShadow = Assert.IsType<DropShadowEffect>(originalDrawerEffect);
            var restoredShadow = Assert.IsType<DropShadowEffect>(drawer.Effect);
            Assert.Equal(originalShadow.BlurRadius, restoredShadow.BlurRadius);
            Assert.Equal(originalShadow.ShadowDepth, restoredShadow.ShadowDepth);
            Assert.Equal(originalShadow.Opacity, restoredShadow.Opacity);
            Assert.Equal(originalMessageRadius, messageRoot.CornerRadius);
            Assert.Same(originalComboStyle, combo.Style);
            Assert.Null(window.TryFindResource("Crystal.Brushes.FrostedFrame"));

            foreach (var office in new[] { RibbonTheme.Office2019, RibbonTheme.Office2013,
                RibbonTheme.Office2010, RibbonTheme.Office2007 })
            {
                ThemeManager.Apply(application, office);
                presentation.Apply(false);
                Assert.DoesNotContain(window.Resources.MergedDictionaries, dictionary =>
                    dictionary.Source?.OriginalString.EndsWith("Crystal.ControlStyles.xaml", StringComparison.Ordinal) == true);
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
