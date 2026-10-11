using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class CustomizationLayoutChecks
{
    internal static void Verify(Application application)
    {
        foreach (var theme in new[] { RibbonTheme.Office2024, RibbonTheme.CrystalLight })
        foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            ThemeManager.Apply(application, theme);
            var ribbon = new Ribbon();
            var tab = new RibbonTab { Header = "Home" };
            var group = new RibbonGroup { Header = "Editing" };
            for (int i = 0; i < 20; i++) group.Items.Add(new RibbonButton { Header = $"Command {i}" });
            tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var page = new RibbonCustomizePage { Ribbon = ribbon };
            var entry = new RibbonOptionsPage { Header = "Customize Ribbon", Content = page };
            var dialog = new RibbonOptionsDialog { Left = -10000, Top = -10000,
                WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false, FlowDirection = flow };
            dialog.Pages.Add(entry); dialog.SelectedPage = entry;
            try
            {
                dialog.Show(); Layout(dialog);
                foreach (double scale in new[] { 1.25, 2d })
                {
                    VisualTreeHelper.SetRootDpi(dialog, new DpiScale(scale, scale)); Layout(dialog);
                    var scroll = Part<ScrollViewer>(dialog, "PART_ContentScroll");
                    var viewport = Part<ScrollContentPresenter>(scroll, "PART_ScrollContentPresenter");
                    Save(dialog, $"{theme}-{flow}-{scale}");
                    foreach (var name in new[] { "PART_AvailableList", "PART_Tree", "PART_UpButton", "PART_DownButton",
                        "PART_NewTabButton", "PART_NewGroupButton", "PART_EditButton", "PART_ResetButton", "PART_ImportButton", "PART_ExportButton" })
                    {
                        var action = Part<FrameworkElement>(page, name);
                        Rect bounds = action.TransformToVisual(viewport).TransformBounds(new Rect(action.RenderSize));
                        Assert.True(bounds.Left >= -0.5 && bounds.Right <= viewport.ActualWidth + 0.5,
                            $"{theme}/{flow}/{scale}/{name}: {bounds}; viewport={viewport.ActualWidth}; page={page.ActualWidth}; dialog={dialog.ActualWidth}");
                        if (VisualTreeHelper.GetClip(viewport) is { } clip)
                        {
                            Rect roundedClip = clip.Bounds; roundedClip.Inflate(0.01, 0.01);
                            Assert.True(roundedClip.Contains(bounds), $"{name}: bounds={bounds}; clip={clip.Bounds}");
                        }
                    }
                }
            }
            finally { dialog.Close(); Layout(dialog); }
        }
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_CUSTOMIZATION_LAYOUT_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
