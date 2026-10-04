using System;
using System.Windows;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CrystalThemeManagerTests
{
    [Fact]
    public void Crystal_dark_host_palette_recolors_scoped_inputs_and_options() => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        try
        {
            var blue = CrystalPalette.Create(CrystalPalette.Blue, dark: true);
            var green = CrystalPalette.Create(Colors.SeaGreen, dark: true);
            Assert.EndsWith("Crystal.Dark.xaml", blue.Source!.OriginalString);
            Assert.Equal(Color.FromRgb(0xEA, 0xF4, 0xFC),
                Assert.IsType<SolidColorBrush>(blue["RibbonKit.Brushes.Text.Primary"]).Color);
            Assert.IsType<DrawingBrush>(blue["RibbonKit.Brushes.Input.SurfaceBackground"]);
            Assert.IsType<DrawingBrush>(blue["RibbonKit.Brushes.Option.SelectedSurface"]);
            Assert.NotEqual(blue["RibbonKit.Brushes.Input.SurfaceBackground"],
                green["RibbonKit.Brushes.Input.SurfaceBackground"]);
            Assert.NotEqual(((SolidColorBrush)blue["RibbonKit.Brushes.Control.HoverBackground"]).Color,
                ((SolidColorBrush)green["RibbonKit.Brushes.Control.HoverBackground"]).Color);
            Assert.IsType<DrawingBrush>(blue["RibbonKit.Brushes.Control.CheckedBackground"]);
            Assert.IsType<DrawingBrush>(blue["RibbonKit.Brushes.Tab.SelectedUnderline"]);
        }
        finally { Sta.ResetApplication(); }
    });

    [Fact]
    public void Crystal_light_switches_with_office_without_flattening_glass_tokens() => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
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
            Assert.True(ThemeManager.SupportsDarkMode(RibbonTheme.CrystalLight));
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
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.Control.CheckedBackground"]);
            Assert.Equal(Color.FromRgb(0xEA, 0xF4, 0xFC), ((SolidColorBrush)application.Resources[
                "RibbonKit.Brushes.Text.Primary"]).Color);
            Assert.Equal(Color.FromRgb(0x26, 0x3A, 0x4B), ((SolidColorBrush)application.Resources[
                "RibbonKit.Brushes.Tab.ConnectNotch"]).Color);
            Assert.Equal(Color.FromRgb(0x25, 0x39, 0x47),
                ((LinearGradientBrush)application.Resources["RibbonKit.Brushes.Backstage.ContentBackground"])
                    .GradientStops[0].Color);
            Assert.NotEqual(Colors.Purple,
                ((SolidColorBrush)application.Resources["RibbonKit.Brushes.Tab.SelectedForeground"]).Color);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Assert.IsType<SolidColorBrush>(application.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"]);
            Assert.Null(application.Resources["Crystal.Brushes.FrostedFrame"]);
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            Assert.IsType<DrawingBrush>(application.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"]);
            ThemeManager.SetDarkMode(application, false);
            Assert.Equal(Color.FromRgb(0x20, 0x36, 0x4A), ((SolidColorBrush)application.Resources[
                "RibbonKit.Brushes.Text.Primary"]).Color);
        }
        finally
        {
            ThemeManager.ClearAccent(application);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.SetTitleBarBackdrop(application, false);
            Sta.ResetApplication();
        }
    });
}
