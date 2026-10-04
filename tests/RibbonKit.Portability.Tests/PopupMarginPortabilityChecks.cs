using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class PopupMarginPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var dropdown = new RibbonDropDownButton { Header = "Layout" };
        var split = new RibbonSplitButton { Header = "Paste", Layout = RibbonSplitButtonLayout.Vertical };
        foreach (var button in new[] { dropdown, split }) button.Items.Add(new RibbonMenuItem { Header = "Command" });
        var window = new Window
        {
            Content = new StackPanel { Children = { dropdown, split } },
            Width = 500, Height = 300, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false, UseLayoutRounding = true,
        };
        var borderStyle = new Style(typeof(Border));
        borderStyle.Setters.Add(new Setter(FrameworkElement.TagProperty, "consumer-border"));
        window.Resources[typeof(Border)] = borderStyle;
        window.Resources["RibbonKit.Brushes.ScreenTip.Border"] = Brushes.Orange;
        try
        {
            window.Show();
            foreach (RibbonTheme theme in new[] { RibbonTheme.Office2013, RibbonTheme.Office2019,
                RibbonTheme.Office2024, RibbonTheme.CrystalLight })
            foreach (bool dark in new[] { false, true })
            foreach (FlowDirection flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                window.FlowDirection = flow;
                Drain();
                var primary = Assert.IsType<Button>(split.Template.FindName("PART_Primary", split));
                var arrow = Assert.IsType<ToggleButton>(split.Template.FindName("PART_Toggle", split));
                var primaryChrome = Assert.IsType<Border>(primary.Template.FindName("Chrome", primary));
                var arrowChrome = Assert.IsType<Border>(arrow.Template.FindName("Chrome", arrow));
                Assert.Equal(arrowChrome.ActualWidth, primaryChrome.ActualWidth);
                Assert.Equal(arrowChrome.BorderThickness.Left, primaryChrome.BorderThickness.Left);
                Assert.Equal(arrowChrome.BorderThickness.Right, primaryChrome.BorderThickness.Right);
                window.Resources["RibbonKit.Metrics.SplitVerticalPrimaryBorderThickness"] = new Thickness(3);
                Drain();
                Assert.Equal(new Thickness(3), primaryChrome.BorderThickness);
                window.Resources.Remove("RibbonKit.Metrics.SplitVerticalPrimaryBorderThickness");
                Drain();
                foreach (var button in new[] { dropdown, split })
                {
                    button.IsDropDownOpen = true;
                    Drain();
                    var popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));
                    var surface = Assert.IsAssignableFrom<Border>(popup.Child);
                    Assert.Equal("consumer-border", surface.Tag);
                    Assert.Same(Brushes.Orange, surface.BorderBrush);
                    DpiScale dpi = VisualTreeHelper.GetDpi(surface);
                    Assert.Equal(Math.Round(2 * dpi.DpiScaleY) / dpi.DpiScaleY, surface.Margin.Top);
                    Assert.Null(VisualTreeHelper.GetClip(surface));
                    surface.UseLayoutRounding = false;
                    Assert.Equal(new Thickness(4, 2, 8, 8), surface.Margin);
                    surface.ClearValue(FrameworkElement.UseLayoutRoundingProperty);
                    button.IsDropDownOpen = false;
                    Drain();
                }
            }
        }
        finally
        {
            dropdown.IsDropDownOpen = split.IsDropDownOpen = false;
            window.Close();
            Drain();
            RibbonAnimation.GlobalLevel = motion;
            ThemeManager.SetDarkMode(application, false);
        }
    }

    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
}
