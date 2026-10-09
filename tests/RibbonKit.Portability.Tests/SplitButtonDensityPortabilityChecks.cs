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

// Exercises native split buttons and source-backed QAT copies without Showcase resources.
internal static class SplitButtonDensityPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        bool darkMode = ThemeManager.IsDarkMode;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null,
            Geometry.Parse("M3,1 L13,1 L13,15 L3,15 Z M6,0 L10,0 L10,3 L6,3 Z")));
        var horizontal = Enum.GetValues<RibbonControlSize>().Select(size => new RibbonSplitButton
            { Header = "Paste", Size = size, Icon = icon, LargeIcon = icon }).ToArray();
        var vertical = new RibbonSplitButton { Header = "Paste", Layout = RibbonSplitButtonLayout.Vertical,
            Icon = icon, LargeIcon = icon };
        var reducedVertical = new RibbonSplitButton { Header = "Paste", Layout = RibbonSplitButtonLayout.Vertical,
            Size = RibbonControlSize.Medium, Icon = icon, LargeIcon = icon };
        var controls = horizontal.Concat(new[] { vertical, reducedVertical }).ToArray();
        foreach (var split in controls) split.Items.Add(new RibbonMenuItem { Header = "Paste Special" });
        var ordinary = new RibbonButton { Header = "Paste", Icon = icon, LargeIcon = icon };
        var group = new RibbonGroup { Header = "Split commands", CanResize = false };
        foreach (var split in controls) group.Items.Add(split);
        group.Items.Add(ordinary);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessMaxWidth = 650 }; ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(horizontal.Single(split => split.Size == RibbonControlSize.Large));
        ribbon.AddToQuickAccess(vertical);
        var copies = ribbon.QuickAccessItems.Cast<RibbonSplitButton>().ToArray();
        var window = new RibbonWindow { Content = ribbon, Width = 1200, Height = 450,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
            UseLayoutRounding = true };
        try
        {
            window.Show();
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (var flow in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1.25, 2d })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                window.FlowDirection = flow; VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
                {
                    ribbon.QuickAccessPosition = placement; ribbon.Density = RibbonDensity.Compact; Layout(window);
                    var compactMinimums = controls.Concat(copies).Select(split =>
                        (Part<Button>(split, "PART_Primary").MinWidth, Part<ToggleButton>(split, "PART_Toggle").MinWidth)).ToArray();
                    ribbon.Density = RibbonDensity.Touch; Layout(window);
                    foreach (var split in controls.Concat(copies)) VerifyGeometry(split, flow, true);
                    foreach (var split in horizontal.Where(split => split.Size == RibbonControlSize.Large))
                        Assert.True(Part<Button>(split, "PART_Primary").ActualWidth >= ordinary.ActualWidth - 1);
                    Assert.All(copies, copy => Assert.Equal(RibbonDensity.Touch, Ribbon.GetDensity(copy)));
                    if (theme == RibbonTheme.Office2024 && !dark && scale == 1.25
                        && placement == RibbonQuickAccessPosition.BelowRibbon)
                        Save(window, $"{theme}-Touch-{flow}");
                    ribbon.Density = RibbonDensity.Compact; Layout(window);
                    int index = 0;
                    foreach (var split in controls.Concat(copies))
                    {
                        VerifyGeometry(split, flow, false);
                        Assert.Equal(compactMinimums[index++],
                            (Part<Button>(split, "PART_Primary").MinWidth, Part<ToggleButton>(split, "PART_Toggle").MinWidth));
                    }
                }
            }

            // Changing geometry must retain primary routing and native menu borrowing.
            ribbon.Density = RibbonDensity.Touch; Layout(window);
            var small = horizontal.Single(split => split.Size == RibbonControlSize.Small);
            window.Resources["RibbonKit.Metrics.Touch.SplitPrimaryMinWidth"] = 80d; Layout(window);
            Assert.InRange(Part<Button>(small, "PART_Primary").ActualWidth, 80, 81);
            Assert.All(copies, copy => Assert.InRange(Part<Button>(copy, "PART_Primary").ActualWidth, 80, 81));
            window.Resources.Remove("RibbonKit.Metrics.Touch.SplitPrimaryMinWidth"); Layout(window);
            Assert.Equal(56, Part<Button>(small, "PART_Primary").MinWidth);
            foreach (var copy in copies)
            {
                var source = Assert.IsType<RibbonSplitButton>(Ribbon.GetQuickAccessSource(copy));
                int invocations = 0; RoutedEventHandler clicked = (_, _) => invocations++;
                source.Click += clicked;
                Part<Button>(copy, "PART_Primary").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Assert.Equal(1, invocations); source.Click -= clicked;
                copy.IsDropDownOpen = true; Layout(window);
                Assert.True(Part<Popup>(copy, "PART_Popup").IsOpen);
                Assert.Single(copy.Items); Assert.Empty(source.Items);
                copy.IsDropDownOpen = false; Layout(window);
                Assert.Single(source.Items);
            }
        }
        finally
        {
            window.Close(); ThemeManager.SetDarkMode(application, darkMode);
            RibbonAnimation.GlobalLevel = animation;
        }
    }

    private static void VerifyGeometry(RibbonSplitButton split, FlowDirection flow, bool touch)
    {
        Assert.True(split.IsVisible && split.ActualWidth > 0);
        var primary = Part<Button>(split, "PART_Primary");
        var arrow = Part<ToggleButton>(split, "PART_Toggle");
        if (touch)
        {
            Assert.True(primary.ActualWidth >= 44 && primary.ActualHeight >= 44);
            Assert.True(arrow.ActualWidth >= 44 && arrow.ActualHeight >= 44);
        }
        var mainBounds = primary.TransformToAncestor(split).TransformBounds(new Rect(primary.RenderSize));
        var arrowBounds = arrow.TransformToAncestor(split).TransformBounds(new Rect(arrow.RenderSize));
        var overlap = Rect.Intersect(mainBounds, arrowBounds);
        Assert.True(overlap.IsEmpty || overlap.Width < 0.01 || overlap.Height < 0.01);
        if (split.IsVerticalLayout)
        {
            Assert.InRange(primary.ActualWidth, arrow.ActualWidth - 0.5, arrow.ActualWidth + 0.5);
            Assert.True(mainBounds.Bottom <= arrowBounds.Top + 0.5);
        }
        else
        {
            Assert.True(primary.ActualWidth > arrow.ActualWidth + 4,
                $"{split.Size}/{flow}/touch={touch}: primary {primary.ActualWidth}, arrow {arrow.ActualWidth}");
            Assert.InRange(primary.ActualHeight, arrow.ActualHeight - 0.5, arrow.ActualHeight + 0.5);
            // RTL can mirror above the split's own coordinate system (notably title-bar QAT).
            double mainX = primary.PointToScreen(new Point(primary.ActualWidth / 2, 0)).X;
            double arrowX = arrow.PointToScreen(new Point(arrow.ActualWidth / 2, 0)).X;
            Assert.True(flow == FlowDirection.LeftToRight ? mainX < arrowX : arrowX < mainX,
                $"{split.Size}/{flow}: screen centers primary={mainX}, arrow={arrowX}, control flow={split.FlowDirection}");
        }
    }

    private static T Part<T>(Control control, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }

    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_SPLIT_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
