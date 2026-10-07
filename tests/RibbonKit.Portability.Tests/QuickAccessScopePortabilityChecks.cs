using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class QuickAccessScopePortabilityChecks
{
    internal static void VerifyThemeRestore(Application application)
    {
        // App.xaml's palette and saved QAT placement are realized before restoring the theme.
        var initialPalette = new ResourceDictionary
        {
            Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative),
        };
        application.Resources.MergedDictionaries.Add(initialPalette);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var button = new RibbonButton { Header = "Save" };
        ribbon.QuickAccessItems.Add(button);
        var window = Window(ribbon);
        try
        {
            window.Loaded += (_, _) => ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            window.Show();
            Layout();
            Assert.DoesNotContain(initialPalette, application.Resources.MergedDictionaries);
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (bool dark in new[] { false, true })
            foreach (bool colored in new[] { false, true })
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            {
                ribbon.Density = density;
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                ThemeManager.SetAccentedTitleBar(application, colored);
                Layout();
                Assert.Equal(colored && theme == RibbonTheme.Office2019,
                    Ribbon.GetQatOnColoredSurface(button));
            }
        }
        finally
        {
            window.Close();
            ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
        }
        void Layout()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
    }

    internal static void Verify(Application application)
    {
        var ribbon = new Ribbon { Backstage = new Backstage() };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var button = new RibbonButton { Header = "Save" };
        ribbon.QuickAccessItems.Add(button);
        var globalRibbon = new Ribbon { Backstage = new Backstage() };
        globalRibbon.Tabs.Add(new RibbonTab { Header = "Home" });
        var globalButton = new RibbonButton { Header = "Save" };
        globalRibbon.QuickAccessItems.Add(globalButton);
        var window = Window(ribbon);
        var globalWindow = Window(globalRibbon);
        try
        {
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.SetAccentedTitleBar(application, true);
            ThemeManager.Apply(application, RibbonTheme.Office2019);
            window.Show(); globalWindow.Show();
            foreach (bool dark in new[] { false, true })
            {
                var palette = ThemeManager.CreatePalette(RibbonTheme.CrystalLight, dark: dark);
                window.Resources.MergedDictionaries.Add(palette);
                foreach (var globalTheme in new[] { RibbonTheme.Office2019, RibbonTheme.Office2024, RibbonTheme.CrystalLight })
                {
                    ThemeManager.Apply(application, globalTheme);
                    foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
                    {
                        ribbon.QuickAccessPosition = globalRibbon.QuickAccessPosition = position;
                        Layout();
                        Assert.False(Ribbon.GetQatOnColoredSurface(button));
                        Assert.Equal(position == RibbonQuickAccessPosition.TitleBar
                            || position == RibbonQuickAccessPosition.TabRow && globalTheme == RibbonTheme.Office2019,
                            Ribbon.GetQatOnColoredSurface(globalButton));
                        Assert.False(button.Resources.Contains(Ribbon.QatColoredHoverBackgroundKey));
                    }
                }
                // Existing realized QAT items follow nearer policy overrides and their removal.
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                window.Resources["RibbonKit.Metrics.QatTabRowColored"] = true;
                Layout();
                Assert.True(Ribbon.GetQatOnColoredSurface(button));
                Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBackground"),
                    button.Resources[Ribbon.QatColoredHoverBackgroundKey]);
                window.Resources.Remove("RibbonKit.Metrics.QatTabRowColored");
                Layout();
                Assert.False(Ribbon.GetQatOnColoredSurface(button));
                window.Resources.MergedDictionaries.Remove(palette);
            }
            ThemeManager.Apply(application, RibbonTheme.Office2019);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            Layout();
            Assert.True(Ribbon.GetQatOnColoredSurface(button));
            ThemeManager.SetAccentedTitleBar(application, false);
            Layout();
            Assert.False(Ribbon.GetQatOnColoredSurface(button));
        }
        finally
        {
            window.Close(); globalWindow.Close();
            ThemeManager.SetAccentedTitleBar(application, false);
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
        }
        void Layout()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout(); globalWindow.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }

    private static RibbonWindow Window(Ribbon ribbon) => new()
    {
        Content = ribbon, Width = 720, Height = 330, Left = -10000, Top = -10000,
        ShowActivated = false, ShowInTaskbar = false,
    };
}
