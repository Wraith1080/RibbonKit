using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CrystalPrintPreviewIntegrationTests
{
    [Fact]
    public void Preview_page_uses_crystal_host_paint_and_restores_every_office_palette() => Sta.Run(() =>
    {
        var application = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.InitializeComponent();
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow();
        try
        {
            // Keep the test's theme and accent selections out of user preferences.
            typeof(MainWindow).GetField("_restoringAppearance",
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            var page = Assert.IsType<Border>(window.PrintPreviewSurface.Child);
            var text = Assert.IsType<StackPanel>(page.Child);
            var title = Assert.IsType<TextBlock>(text.Children[0]);
            var body = Assert.IsType<TextBlock>(text.Children[1]);

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>().Where(t => t != RibbonTheme.CrystalLight))
            {
                SelectTheme(theme);
                foreach (bool dark in new[] { false, true })
                {
                    if (dark && !ThemeManager.SupportsDarkMode(theme)) continue;
                    ThemeManager.SetDarkMode(application, dark);
                    AssertPaint(Color.FromRgb(0x6E, 0x6E, 0x6E),
                        Color.FromRgb(0x3F, 0x3F, 0x3F),
                        Color.FromRgb(0x3F, 0x3F, 0x3F),
                        Color.FromRgb(0x6B, 0x6B, 0x6B));
                }
            }

            ThemeManager.SetDarkMode(application, false);
            SelectTheme(RibbonTheme.CrystalLight);
            Assert.True(window.MainRibbon.EnterModal(window.PrintPreviewTab));
            Assert.Equal(Visibility.Visible, window.PrintPreviewSurface.Visibility);
            AssertPaint(Color.FromRgb(0xA7, 0xBE, 0xCE),
                Color.FromRgb(0x82, 0x9F, 0xB3),
                Color.FromRgb(0x20, 0x36, 0x4A),
                Color.FromRgb(0x4F, 0x64, 0x76));
            window.AccentGallery.SelectedItem = window.AccentGallery.Items[4]; // Purple
            Color tintedCanvas = ColorOf(window.PrintPreviewSurface.Background);
            Assert.NotEqual(Color.FromRgb(0xA7, 0xBE, 0xCE), tintedCanvas);
            Assert.NotEqual(Color.FromRgb(0x82, 0x9F, 0xB3), ColorOf(page.BorderBrush));
            Assert.Equal(Color.FromRgb(0x20, 0x36, 0x4A), ColorOf(title.Foreground));
            Assert.Equal(Color.FromRgb(0x4F, 0x64, 0x76), ColorOf(body.Foreground));

            window.DarkModeToggle.IsChecked = true;
            Assert.True(ThemeManager.IsDarkMode);
            Assert.True(ThemeManager.SupportsDarkMode(RibbonTheme.CrystalLight));
            Assert.Contains(window.Resources.MergedDictionaries, dictionary =>
                dictionary.Source?.OriginalString.EndsWith("Crystal.Dark.xaml", StringComparison.Ordinal) == true);
            Color darkCanvas = ColorOf(window.PrintPreviewSurface.Background);
            Assert.NotEqual(tintedCanvas, darkCanvas);
            Assert.True(darkCanvas.R < 0x70 && darkCanvas.G < 0x70 && darkCanvas.B < 0x70);
            Assert.Equal(Color.FromRgb(0x20, 0x36, 0x4A), ColorOf(title.Foreground));
            Assert.Equal(Colors.White, ColorOf(page.Background));

            window.DarkModeToggle.IsChecked = false;
            Assert.Equal(tintedCanvas, ColorOf(window.PrintPreviewSurface.Background));
            Assert.DoesNotContain(window.Resources.MergedDictionaries, dictionary =>
                dictionary.Source?.OriginalString.EndsWith("Crystal.Dark.xaml", StringComparison.Ordinal) == true);

            Assert.True(window.MainRibbon.ExitModal());
            Assert.Equal(Visibility.Collapsed, window.PrintPreviewSurface.Visibility);
            SelectTheme(RibbonTheme.Office2024);
            AssertPaint(Color.FromRgb(0x6E, 0x6E, 0x6E),
                Color.FromRgb(0x3F, 0x3F, 0x3F),
                Color.FromRgb(0x3F, 0x3F, 0x3F),
                Color.FromRgb(0x6B, 0x6B, 0x6B));

            void SelectTheme(RibbonTheme theme) => window.ThemeGallery.SelectedItem =
                window.ThemeGallery.Items.Cast<FrameworkElement>()
                    .Single(item => (string)item.Tag == theme.ToString());

            void AssertPaint(Color canvas, Color border, Color heading, Color description)
            {
                Assert.Equal(canvas, ColorOf(window.PrintPreviewSurface.Background));
                Assert.Equal(border, ColorOf(page.BorderBrush));
                Assert.Equal(heading, ColorOf(title.Foreground));
                Assert.Equal(description, ColorOf(body.Foreground));
                Assert.Equal(Colors.White, ColorOf(page.Background));
            }
        }
        finally
        {
            window.Close();
            application.Shutdown();
        }
    });

    private static Color ColorOf(Brush brush) => Assert.IsType<SolidColorBrush>(brush).Color;
}
