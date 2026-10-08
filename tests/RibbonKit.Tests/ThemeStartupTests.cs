using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class ThemeStartupTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2007)]
    [InlineData(RibbonTheme.Office2010)]
    [InlineData(RibbonTheme.Office2013)]
    [InlineData(RibbonTheme.Office2019)]
    [InlineData(RibbonTheme.Office2024)]
    [InlineData(RibbonTheme.CrystalLight)]
    public void Showcase_restores_a_tab_row_qat_before_the_first_theme_apply(RibbonTheme theme) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        MainWindow? window = null;
        try
        {
            window = new MainWindow { Width = 1080, Height = 680, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false };
            // Exercise the startup ordering without reading or writing saved preferences.
            var type = typeof(MainWindow);
            window.Loaded -= (RoutedEventHandler)type.GetMethod("OnWindowLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), window);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            ExceptionDispatchInfo? startupFailure = null;
            window.Loaded += (_, _) =>
            {
                try
                {
                    window.MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                    ThemeManager.Apply(application, theme);
                }
                catch (Exception ex) { startupFailure = ExceptionDispatchInfo.Capture(ex); }
            };
            window.Show();
            Sta.Drain();
            startupFailure?.Throw();
            window.UpdateLayout();
            Assert.Equal(theme, ThemeManager.CurrentTheme);
            Assert.False(UtilityChrome.GetQatTabRowColored(window.MainRibbon));
        }
        finally
        {
            window?.Close();
            Sta.ResetApplication();
        }
    });
}
