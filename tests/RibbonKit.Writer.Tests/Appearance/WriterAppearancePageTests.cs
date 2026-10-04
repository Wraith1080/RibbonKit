using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using RibbonKit;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Appearance;

[Collection("Writer UI")]
public sealed class WriterAppearancePageTests
{
    [Fact]
    public void TwoColumnAppearanceUsesSharedOptionsWithoutOverlapAndStacksInNarrowWindows()
    {
        StaTestHelper.Run(() =>
        {
            var page = new WriterAppearancePage { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16) };
            var window = new Window { Content = page, Width = 680, Height = 700,
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000,
                ShowInTaskbar = false, ShowActivated = false };
            using var scope = new WriterAppearanceScope(window);
            try
            {
                window.Show();
                foreach (bool dark in new[] { false, true })
                {
                    var preferences = new WriterAppearancePreferences { Theme = RibbonTheme.CrystalLight, DarkPalette = dark };
                    scope.Apply(preferences);
                    page.SetPreferences(preferences);
                    Assert.Same(window.FindResource("RibbonKit.Brushes.Text.Primary"), page.Foreground);
                    foreach (double width in new[] { 680d, 380d, 680d })
                    {
                        window.Width = width;
                        window.UpdateLayout();
                        window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                        var left = (StackPanel)page.FindName("AppearanceLeftColumn");
                        var right = (StackPanel)page.FindName("AppearanceRightColumn");
                        var leftPosition = left.TranslatePoint(new Point(), page);
                        var rightPosition = right.TranslatePoint(new Point(), page);
                        Assert.True(width == 680 ? rightPosition.X > leftPosition.X + left.ActualWidth
                            : rightPosition.Y >= leftPosition.Y + left.ActualHeight);
                        foreach (string name in new[] { "ThemeCombo", "PaletteCombo", "BackdropCombo", "FrameCombo", "BackstageCombo", "ApplicationButtonCombo", "AnimationCombo" })
                        {
                            var combo = Assert.IsType<RibbonComboBox>(page.FindName(name));
                            Assert.True(combo.ActualWidth > 100);
                            Assert.InRange(combo.InputWidth, combo.ActualWidth - 1, combo.ActualWidth + 1);
                        }
                        foreach (var column in new[] { left, right })
                        {
                            double precedingBottom = 0;
                            foreach (var check in column.Children.OfType<RibbonCheckBox>())
                            {
                                double top = check.TranslatePoint(new Point(), column).Y;
                                Assert.True(top >= precedingBottom);
                                Assert.True(check.ActualHeight >= 28);
                                precedingBottom = top + check.ActualHeight;
                            }
                        }
                        if (width == 680)
                        {
                            var bitmap = new RenderTargetBitmap((int)page.ActualWidth, (int)page.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                            var drawing = new DrawingVisual();
                            using (var context = drawing.RenderOpen())
                            {
                                context.DrawRectangle((Brush)window.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"), null, new Rect(page.RenderSize));
                                context.DrawRectangle(new VisualBrush(page), null, new Rect(page.RenderSize));
                            }
                            bitmap.Render(drawing);
                            string folder = Path.Combine(AppContext.BaseDirectory, "writer-theme-diagnostics");
                            Directory.CreateDirectory(folder);
                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap));
                            using var output = File.Create(Path.Combine(folder, $"WriterAppearance-two-columns-{(dark ? "dark" : "light")}.png"));
                            encoder.Save(output);
                            Assert.True(page.DesiredSize.Height - page.Margin.Top - page.Margin.Bottom < 460,
                                $"Appearance content height {page.DesiredSize.Height - page.Margin.Top - page.Margin.Bottom}, total {page.DesiredSize.Height}");
                        }
                    }
                }
            }
            finally { window.Close(); }
        });
    }

    [Fact]
    public void PagePublishesLivePreviewAndAppearanceOnlyDefaultsWithStableAutomationNames()
    {
        StaTestHelper.Run(() =>
        {
            var page = new WriterAppearancePage();
            var custom = new WriterAppearancePreferences
            {
                Theme = RibbonTheme.Office2007,
                DarkPalette = true,
                BackstageDesign = RibbonBackstageDesign.Glass2007,
                Backdrop = RibbonBackdrop.Acrylic,
                FrameAppearance = RibbonWindowFrameAppearance.Office2007Aero,
                ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
                ShowRuler = false,
            };
            page.SetPreferences(custom);

            Assert.Equal(custom, page.Preferences);
            Assert.Equal("Appearance settings", AutomationProperties.GetName(page));
            Assert.Equal("WriterAppearancePage", AutomationProperties.GetAutomationId(page));
            var content = Assert.IsType<StackPanel>(page.Content);
            var introduction = Assert.IsType<TextBlock>(content.Children[0]);
            Assert.StartsWith("Preview Writer's theme", introduction.Text, StringComparison.Ordinal);
            Assert.DoesNotContain(content.Children.OfType<TextBlock>(), text => text.Text == "Appearance");

            WriterAppearancePreferences? preview = null;
            page.PreferencesChanged += (_, preferences) => preview = preferences;
            var defaults = Assert.IsType<Button>(page.FindName("AppearanceDefaultsButton"));
            defaults.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(new WriterAppearancePreferences(), preview);
            Assert.Equal(new WriterAppearancePreferences(), page.Preferences);
            Assert.Equal("Restore appearance defaults", AutomationProperties.GetName(defaults));
            Assert.Same(page.TryFindResource("OptionsDialogActionButtonStyle"), defaults.Style);
            var accent = Assert.IsType<Button>(page.FindName("AccentButton"));
            Assert.Same(page.TryFindResource("OptionsDialogActionButtonStyle"), accent.Style);
            var apply = Assert.IsType<Button>(page.FindName("AppearanceApplyButton"));
            Assert.Same(page.TryFindResource("OptionsDialogPrimaryButtonStyle"), apply.Style);
            Assert.Equal(
                "Apply appearance settings",
                AutomationProperties.GetName(apply));
        });
    }
}
