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

// Creates the copies through the real customization page and uses only RibbonKit.
internal static class StackedCustomGroupPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var ribbon = new Ribbon { Density = RibbonDensity.Touch };
        var font = new RibbonComboBox { Header = "Font", Icon = Icon() }; font.Items.Add("Calibri");
        var size = new RibbonComboBox { Header = "Font Size", Icon = Icon() }; size.Items.Add("11");
        var mode = new RibbonDropDownButton { Header = "Touch/Mouse Mode", Icon = Icon() }; mode.Items.Add(new RibbonMenuItem { Header = "Touch" });
        var styles = new InRibbonGallery { Header = "Styles", Icon = Icon() }; styles.Items.Add(new RibbonGalleryItem { Content = "Normal" });
        var commands = new FrameworkElement[] { font, size, mode, styles,
            new RibbonButton { Header = "Undo", Icon = Icon() }, new RibbonButton { Header = "Redo", Icon = Icon() }, new RibbonButton { Header = "Save", Icon = Icon() } };
        var sources = new RibbonGroup { Header = "Source commands" }; var sourceTab = new RibbonTab { Header = "Source" };
        foreach (var command in commands) sources.Items.Add(command);
        sourceTab.Groups.Add(sources); ribbon.Tabs.Add(sourceTab);
        var group = new RibbonGroup { Header = "Custom Group", Layout = RibbonGroupLayout.Stacked, CanResize = false };
        var customTab = new RibbonTab { Header = "Custom Tab" }; customTab.Groups.Add(group);
        Ribbon.SetIsCustom(group, true); Ribbon.SetIsCustom(customTab, true);
        ribbon.Tabs.Add(customTab); ribbon.SelectedTab = customTab;
        var page = new RibbonCustomizePage { Ribbon = ribbon };
        var root = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); root.Children.Add(ribbon); root.Children.Add(page);
        var window = new Window { Content = root, Width = 1100, Height = 700, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            Assert.IsType<RibbonCustomizeNode>(Part<TreeView>(page, "PART_Tree").Items[1]).Children.Single().IsSelected = true;
            var available = Part<ListBox>(page, "PART_AvailableList");
            foreach (var command in commands)
            {
                available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(entry => ReferenceEquals(entry.Control, command));
                Layout(); Part<ButtonBase>(page, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
            }
            var copies = group.Items.Cast<FrameworkElement>().ToArray(); Assert.Equal(7, copies.Length);
            Assert.All(copies, copy => Assert.Contains(Ribbon.GetQuickAccessSource(copy), commands));
            foreach (var copy in copies.Skip(3)) group.Items.Remove(copy); Layout();

            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (var flow in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1.25, 2d })
            {
                ThemeManager.Apply(application, theme); ribbon.FlowDirection = flow;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                foreach (var density in Enum.GetValues<RibbonDensity>())
                {
                    ribbon.Density = density; Layout();
                    Assert.False(font.IsLoaded); Assert.False(styles.IsLoaded);
                    double height = ribbon.ActualHeight;
                    foreach (var copy in copies.Skip(3))
                    {
                        group.Items.Add(copy); Layout();
                        Assert.InRange(ribbon.ActualHeight, height - 0.5, height + 0.5);
                    }
                    for (int i = 0; i < copies.Length; i++)
                    {
                        var slot = LayoutInformation.GetLayoutSlot(copies[i]);
                        Assert.Equal(i % 3 == 0, Math.Abs(slot.Y) < 0.5);
                        var column = LayoutInformation.GetLayoutSlot(copies[i / 3 * 3]);
                        Assert.InRange(slot.X, column.X - 0.5, column.X + 0.5);
                        if (density == RibbonDensity.Touch) Assert.True(copies[i].ActualHeight >= 44);
                        if (i > 0 && i % 3 == 0)
                        {
                            double current = copies[i].TranslatePoint(new Point(), window).X;
                            double previous = copies[i - 3].TranslatePoint(new Point(), window).X;
                            Assert.True(flow == FlowDirection.LeftToRight ? current > previous : current < previous);
                        }
                    }
                    if (scale == 1.25 && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight)
                        Save(window, ribbon, $"{theme}-{density}-{flow}");
                    foreach (var copy in copies.Skip(3)) group.Items.Remove(copy); Layout();
                    Assert.InRange(ribbon.ActualHeight, height - 0.5, height + 0.5);
                }
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = animation; }
        void Layout() { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
    }

    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.SteelBlue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
    private static void Save(Window window, FrameworkElement surface, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_STACKED_GROUP_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(window);
        var render = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        render.Render(window);
        var bounds = surface.TransformToAncestor(window).TransformBounds(new Rect(surface.RenderSize));
        var crop = new CroppedBitmap(render, new Int32Rect((int)Math.Floor(bounds.X * dpi.DpiScaleX), (int)Math.Floor(bounds.Y * dpi.DpiScaleY), (int)Math.Ceiling(bounds.Width * dpi.DpiScaleX), (int)Math.Ceiling(bounds.Height * dpi.DpiScaleY)));
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(crop));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
