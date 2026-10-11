using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class CustomizeDialogLayoutTests
{
    [Fact]
    public void Showcase_customization_actions_fit_the_content_clip() => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var window = new MainWindow { Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain();
            var dialog = window.CreateCustomizationDialog(false);
            try
            {
                dialog.WindowStartupLocation = WindowStartupLocation.Manual;
                dialog.Left = dialog.Top = -10000; dialog.ShowActivated = false;
                dialog.Show(); Sta.Drain();
                // Reproduce the fractional-DPI clip regardless of the test machine's monitor.
                VisualTreeHelper.SetRootDpi(dialog, new DpiScale(1.25, 1.25));
                Sta.Drain(); dialog.UpdateLayout();
                var page = Assert.IsType<RibbonCustomizePage>(dialog.SelectedPage!.Content);
                var scroll = Assert.IsType<ScrollViewer>(dialog.Template.FindName("PART_ContentScroll", dialog));
                var viewport = Assert.IsType<ScrollContentPresenter>(scroll.Template.FindName("PART_ScrollContentPresenter", scroll));
                for (DependencyObject? clipHost = viewport; clipHost is Visual; clipHost = VisualTreeHelper.GetParent(clipHost))
                {
                    if (VisualTreeHelper.GetClip((Visual)clipHost) is not { } clip) continue;
                    foreach (string name in new[] { "PART_UpButton", "PART_DownButton", "PART_EditButton" })
                    {
                        var button = Assert.IsType<Button>(page.Template.FindName(name, page));
                        Rect bounds = button.TransformToVisual((Visual)clipHost).TransformBounds(new Rect(button.RenderSize));
                        Rect tolerance = clip.Bounds; tolerance.Inflate(0.01, 0.01);
                        Assert.True(tolerance.Contains(bounds), $"{name}: bounds={bounds}; clip={clip.Bounds}; host={clipHost.GetType().Name}");
                    }
                }
            }
            finally { dialog.Close(); Sta.Drain(); }
        }
        finally { window.Close(); Sta.Drain(); }
    });
}
