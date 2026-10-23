using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Public controls, scoped material and host-supplied capture only.
internal static class RibbonGlassFlyoutPortabilityChecks
{
    internal static void Verify(Application app)
    {
        var level = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        bool visible = Environment.GetEnvironmentVariable("RIBBONKIT_GLASS_FLYOUT_VISIBLE") == "1";
        try
        {
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            {
                if (visible && theme is not (RibbonTheme.Office2024 or RibbonTheme.Office2024Flat)) continue;
                ThemeManager.Apply(app, theme); ThemeManager.SetDarkMode(app, dark);
                bool supported = theme is RibbonTheme.Office2013 or RibbonTheme.Office2019 or RibbonTheme.Office2024 or RibbonTheme.Office2024Flat;
                var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, QuickAccessMaxWidth = 60,
                    Density = dark ? RibbonDensity.Touch : RibbonDensity.Compact,
                    FlowDirection = theme == RibbonTheme.Office2024Flat ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup { Header = "Commands" };
                for (int i = 0; i < 6; i++)
                {
                    var command = new RibbonButton { Header = "Command " + i, Size = RibbonControlSize.Large,
                        Icon = new DrawingImage(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) };
                    Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline);
                    group.Items.Add(command); ribbon.AddToQuickAccess(command);
                }
                tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
                ((FrameworkElement)tab.Content).Width = 50;
                var scene = new DockPanel { Background = Brushes.CornflowerBlue }; DockPanel.SetDock(ribbon, Dock.Top); scene.Children.Add(ribbon);
                scene.Children.Add(new Border { Background = Brushes.Orange });
                var window = new RibbonWindow { Content = scene, Width = 740, Height = 450,
                    Left = visible ? 40 : -10000, Top = visible ? 40 : -10000, ShowActivated = false, ShowInTaskbar = false };
                var overlay = ThemeManager.CreateGlassOverlay(window, dark); window.Resources.MergedDictionaries.Add(overlay);
                using var capture = new CapturedBackdrop(ribbon, window); capture.Apply(true);
                try
                {
                    window.Show(); Layout();
                    var popup = Part<Popup>(group, "PART_Popup"); popup.IsOpen = true; Layout();
                    Check(Part<Border>(group, "PART_PopupHost"), "group"); popup.IsOpen = false; Layout();
                    // TitleBar lives outside the ribbon's visual tree.
                    var qat = Descendants(window).OfType<RibbonQuickAccessToolBar>().Single(t => t.IsVisible && t.HasOverflow);
                    Part<ToggleButton>(qat, "PART_OverflowButton").IsChecked = true; Layout();
                    Check(Part<Border>(qat, "PART_OverflowPopupHost"), "title-qat");
                    Part<ToggleButton>(qat, "PART_OverflowButton").IsChecked = false; Layout();
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout();
                    var host = (Control)tab.Content;
                    var simplified = Part<RibbonQuickAccessToolBar>(host, "PART_SimplifiedToolBar");
                    Assert.True(simplified.HasOverflow);
                    Part<ToggleButton>(simplified, "PART_OverflowButton").IsChecked = true; Layout();
                    var simplifiedFrame = Part<Border>(simplified, "PART_OverflowPopupHost"); Check(simplifiedFrame, "simplified");
                    window.Resources.MergedDictionaries.Remove(overlay); Layout();
                    Assert.Same(simplified.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), simplifiedFrame.Background);
                    Part<ToggleButton>(simplified, "PART_OverflowButton").IsChecked = false;
                    Console.WriteLine($"Glass flyouts {theme}/{dark}: supported={supported}, three surfaces and removal passed.");
                }
                finally { window.Close(); Drain(); }

                void Layout() { window.UpdateLayout(); Drain(); window.UpdateLayout(); Drain(); }
                void Check(Border frame, string name)
                {
                    if (!supported)
                    {
                        Assert.Same(frame.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), frame.Background); return;
                    }
                    var drawing = Assert.IsType<DrawingGroup>(Assert.IsType<DrawingBrush>(frame.Background).Drawing);
                    Assert.Equal(((Brush)frame.FindResource("RibbonKit.Brushes.ApplicationMenu.FrameBand")).Opacity * .84,
                        Assert.IsType<GeometryDrawing>(drawing.Children[2]).Brush.Opacity, 5);
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(frame.ActualWidth), (int)Math.Ceiling(frame.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(frame);
                    byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
                    Assert.True(Enumerable.Range(0, pixels.Length / 4).Any(i => pixels[i * 4] >= 253 && pixels[i * 4 + 1] <= 2 && pixels[i * 4 + 2] <= 2),
                        $"{theme}/{dark}/{name}: command ink lost contrast.");
                    string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_GLASS_FLYOUT_DIAGNOSTICS");
                    if (string.IsNullOrEmpty(folder)) return;
                    Directory.CreateDirectory(folder); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(Path.Combine(folder, $"{theme}-{dark}-{name}.png")); encoder.Save(stream);
                }
            }
        }
        finally { RibbonAnimation.GlobalLevel = level; }
    }

    private static T Part<T>(Control control, string name) where T : class => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
