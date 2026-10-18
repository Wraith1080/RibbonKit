using System.Windows;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class MergeSelectionTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024)]
    [InlineData(RibbonTheme.Office2024Flat)]
    [InlineData(RibbonTheme.CrystalLight)]
    public void Showcase_chart_tools_marker_settles_under_its_animated_header(RibbonTheme theme) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), theme);
        var level = RibbonAnimation.GlobalLevel; bool reduce = RibbonAnimation.RespectSystemReduceMotion;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; RibbonAnimation.RespectSystemReduceMotion = false;
        var window = new RibbonKit.Showcase.MainWindow { Width = 1080, Height = 640,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        window.Loaded -= (RoutedEventHandler)typeof(RibbonKit.Showcase.MainWindow).GetMethod("OnWindowLoaded", flags)!
            .CreateDelegate(typeof(RoutedEventHandler), window);
        typeof(RibbonKit.Showcase.MainWindow).GetField("_restoringAppearance", flags)!.SetValue(window, true);
        try
        {
            window.Show(); Sta.Drain();
            window.MergeToggle.IsChecked = true; Sta.Drain();
            var chart = window.ChartToolsSource.Tabs[0];
            Assert.Same(chart, window.MainRibbon.SelectedTab);
            Pump(TimeSpan.FromMilliseconds(300));
            var tabs = (RibbonTabControl)window.MainRibbon.Template.FindName("TabControlHost", window.MainRibbon);
            var marker = (Rectangle)tabs.Template.FindName("PART_TabMarker", tabs);
            var host = (Visual)marker.Parent;
            Point tabOrigin = chart.TransformToVisual(host).Transform(new Point());
            Point markerOrigin = marker.TransformToVisual(host).Transform(new Point());
            Assert.InRange(Math.Abs(markerOrigin.Y - tabOrigin.Y - chart.ActualHeight + marker.ActualHeight), 0, 0.75);
            Assert.InRange(Math.Abs(markerOrigin.X - tabOrigin.X - 10), 0, 0.75);
            string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_GEOMETRY_DIAGNOSTICS");
            if (!string.IsNullOrEmpty(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
                using var output = System.IO.File.Create(System.IO.Path.Combine(folder, $"{theme}-chart-tools.png")); png.Save(output);
            }
        }
        finally
        { window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = level; RibbonAnimation.RespectSystemReduceMotion = reduce; Sta.ResetApplication(); }
    });

    private static void Pump(TimeSpan duration)
    {
        var watch = Stopwatch.StartNew(); var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (watch.Elapsed >= duration) frame.Continue = false; };
        timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
    }

    [Theory]
    [InlineData("Home")]
    [InlineData("View")]
    [InlineData("Ribbon Lab")]
    public void Showcase_deactivation_keeps_the_selected_host_tab(string header) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), RibbonTheme.Office2013);
        var window = new RibbonKit.Showcase.MainWindow();
        try
        {
            typeof(RibbonKit.Showcase.MainWindow).GetField("_restoringAppearance",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(window, true);
            window.Left = -10000; window.Top = -10000;
            window.ShowActivated = false; window.ShowInTaskbar = false;
            window.Show();
            Sta.Drain();
            window.MergeToggle.IsChecked = true;
            Sta.Drain();
            Assert.Same(window.ChartToolsSource.Tabs[0], window.MainRibbon.SelectedTab);
            Assert.True(window.MainRibbon.IsMerged(window.ChartToolsSource));
            RibbonTab view = window.MainRibbon.Tabs.Single(tab => tab.Header?.ToString() == header);
            window.MainRibbon.SelectedTab = view;
            Sta.Drain();
            window.MergeToggle.IsChecked = false;
            Sta.Drain();
            Assert.Same(view, window.MainRibbon.SelectedTab);
            Assert.False(window.MainRibbon.IsMerged(window.ChartToolsSource));

            // Only a removed tool tab needs the ordinary visible-tab fallback.
            window.MergeToggle.IsChecked = true;
            Sta.Drain();
            Assert.Same(window.ChartToolsSource.Tabs[0], window.MainRibbon.SelectedTab);
            window.MergeToggle.IsChecked = false;
            Sta.Drain();
            Assert.Same(window.MainRibbon.Tabs[0], window.MainRibbon.SelectedTab);

            // A cancelled activation must not run the queued tool-tab jump later.
            window.MainRibbon.SelectedTab = view;
            window.MergeToggle.IsChecked = true;
            window.MergeToggle.IsChecked = false;
            Sta.Drain();
            Assert.Same(view, window.MainRibbon.SelectedTab);
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unmerging_an_unselected_source_preserves_the_realized_host_tab(bool contributeGroup) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2013);
        var ribbon = new Ribbon();
        var home = new RibbonTab { Header = "Home" };
        Ribbon.SetCommandId(home, "HOME");
        var view = new RibbonTab { Header = "View" };
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(view);
        var source = new RibbonMergeSource();
        source.Tabs.Add(new RibbonTab { Header = "Chart Design" });
        source.Tabs.Add(new RibbonTab { Header = "Chart Format" });
        if (contributeGroup)
        {
            source.Groups.Add(new RibbonGroupContribution
            {
                TargetTabId = "HOME",
                Group = new RibbonGroup { Header = "Chart Data" },
            });
        }

        var window = new RibbonWindow
        {
            Content = ribbon, Width = 800, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            Sta.Drain();
            ribbon.Merge(source);
            ribbon.SelectedTab = view;
            Sta.Drain();
            ribbon.Unmerge(source);
            Sta.Drain();
            Assert.Same(view, ribbon.SelectedTab);
            Assert.Equal(1, ribbon.SelectedIndex);
        }
        finally { window.Close(); }
    });
}
