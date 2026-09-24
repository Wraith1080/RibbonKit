using System;
using System.Windows;
using System.Windows.Media;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CrystalThemeManagerTests
{
    [Fact]
    public void Crystal_light_switches_with_office_without_flattening_glass_tokens() => Sta.Run(() =>
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        try
        {
            var manuallyMergedCrystal = new ResourceDictionary
            { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Light.xaml", UriKind.Relative) };
            application.Resources.MergedDictionaries.Add(manuallyMergedCrystal);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.SetTitleBarBackdrop(application, false);
            ThemeManager.ClearAccent(application);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Assert.DoesNotContain(manuallyMergedCrystal, application.Resources.MergedDictionaries);
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            Assert.Equal(RibbonTheme.CrystalLight, ThemeManager.CurrentTheme);
            Assert.False(ThemeManager.SupportsDarkMode(RibbonTheme.CrystalLight));
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"]);
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.Control.CheckedBackground"]);
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.ApplicationButton.MenuOpenBackground"]);

            ThemeManager.SetAccent(application, Colors.Purple);
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"]);
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.Control.CheckedBackground"]);
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.ApplicationButton.MenuOpenBackground"]);
            Assert.IsType<LinearGradientBrush>(application.Resources["RibbonKit.Brushes.TitleBar.Background"]);
            Assert.Equal(Colors.Purple,
                ((SolidColorBrush)application.Resources["RibbonKit.Brushes.Tab.SelectedForeground"]).Color);

            ThemeManager.SetDarkMode(application, true);
            Assert.IsType<LinearGradientBrush>(application.Resources["RibbonKit.Brushes.TitleBar.Background"]);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Assert.IsType<SolidColorBrush>(application.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"]);
            Assert.Null(application.Resources["Crystal.Brushes.FrostedFrame"]);
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"]);
        }
        finally
        {
            ThemeManager.ClearAccent(application);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.SetTitleBarBackdrop(application, false);
            application.Shutdown();
        }
    });
}
