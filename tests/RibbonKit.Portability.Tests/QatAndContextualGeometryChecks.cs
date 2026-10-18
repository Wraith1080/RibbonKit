using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Public-only consumer: no Showcase resources or presentation helpers.
internal static class QatAndContextualGeometryChecks
{
    internal static void VerifyQat(Application application) => Cases(application, animate: false, (window, ribbon) =>
    {
        var group = ribbon.Tabs[0].Groups[0];
        var plain = (RibbonButton)group.Items[0];
        var dropdown = new RibbonDropDownButton { Header = "Dropdown" };
        dropdown.Items.Add(new RibbonMenuItem { Header = "Action" });
        var split = new RibbonSplitButton { Header = "Split" };
        split.Items.Add(new RibbonMenuItem { Header = "Action" });
        var gallery = new InRibbonGallery { Header = "Gallery", Width = 160 };
        gallery.Items.Add(new RibbonGalleryItem { Content = "Choice" });
        foreach (var item in new FrameworkElement[] { dropdown, split, gallery }) group.Items.Add(item);
        var qatGroup = new RibbonGroup { Header = "Group", CanResize = false };
        qatGroup.Items.Add(new RibbonButton { Header = "Action" });
        ribbon.Tabs[0].Groups.Add(qatGroup);
        Assert.True(ribbon.AddToQuickAccess(plain)); Layout(window);
        var drawer = Part<Border>(ribbon, "QatBelowHost");
        double baseline = drawer.ActualHeight;
        var heights = new List<(string Kind, double Height)>();
        foreach (var item in new FrameworkElement[] { dropdown, split, gallery, qatGroup })
        {
            Assert.True(ribbon.AddToQuickAccess(item)); Layout(window);
            heights.Add((item.GetType().Name, drawer.ActualHeight));
            Console.WriteLine($"QAT {item.GetType().Name}: before={baseline}, after={drawer.ActualHeight}, item={((FrameworkElement)ribbon.QuickAccessItems[^1]).ActualHeight}");
            ribbon.QuickAccessItems.RemoveAt(ribbon.QuickAccessItems.Count - 1); Layout(window);
        }
        Assert.All(heights, item => Assert.Equal(baseline, item.Height, 3));
    });

    internal static void VerifyMarker(Application application) => Cases(application, animate: true, (window, ribbon) =>
    {
        var home = ribbon.SelectedTab;
        var source = new RibbonMergeSource { Target = ribbon };
        var chart = new RibbonTab { Header = "Chart Design", IsContextual = true, ContextualColor = Brushes.SeaGreen };
        chart.Groups.Add(new RibbonGroup { Header = "Chart commands" });
        source.Tabs.Add(chart);
        source.Tabs.Add(new RibbonTab { Header = "Chart Format", IsContextual = true, ContextualColor = Brushes.SeaGreen });
        for (int cycle = 0; cycle < 2; cycle++)
        {
            source.IsActive = true;
            ribbon.SelectedTab = chart;
            Layout(window);
            var watch = Stopwatch.StartNew();
            int frames = 0;
            // Sample while the header entrance runs, and again after it rests.
            PumpUntil(() =>
            {
                if (watch.ElapsedMilliseconds < 35) return false;
                AssertMarker(); frames++;
                return watch.ElapsedMilliseconds >= 300;
            });
            Assert.True(frames > 1);
            AssertMarker();
            // Sampling geometry must not cancel an ordinary tab-selection glide.
            ribbon.SelectedTab = home;
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var translate = (TranslateTransform)Part<Rectangle>(tabs, "PART_TabMarker").RenderTransform;
            Assert.True(translate.HasAnimatedProperties);
            var glideWatch = Stopwatch.StartNew();
            PumpUntil(() => glideWatch.ElapsedMilliseconds >= 35);
            if (ribbon.Density == RibbonDensity.Compact) Assert.True(translate.HasAnimatedProperties);
            source.IsActive = false; Layout(window);
            Assert.DoesNotContain(chart, ribbon.Tabs);
            Assert.Same(home, ribbon.SelectedTab);
        }

        void AssertMarker()
        {
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var marker = Part<Rectangle>(tabs, "PART_TabMarker");
            var host = (Visual)marker.Parent;
            Point tabOrigin = chart.TransformToVisual(host).Transform(new Point());
            Point markerOrigin = marker.TransformToVisual(host).Transform(new Point());
            Assert.InRange(Math.Abs(markerOrigin.Y - (tabOrigin.Y + chart.ActualHeight - marker.ActualHeight)), 0, 0.75);
            Assert.InRange(Math.Abs(marker.ActualWidth - (chart.ActualWidth - 20)), 0, 1 / VisualTreeHelper.GetDpi(marker).DpiScaleX);
            Assert.Equal(1, marker.Opacity);
        }
    });

    private static void Cases(Application application, bool animate, Action<RibbonWindow, Ribbon> verify)
    {
        var oldLevel = RibbonAnimation.GlobalLevel; bool reduce = RibbonAnimation.RespectSystemReduceMotion;
        var context = RibbonAnimation.GetActionOverride(RibbonAnimationAction.ContextualTab);
        try
        {
            RibbonAnimation.GlobalLevel = animate ? RibbonAnimationLevel.Subtle : RibbonAnimationLevel.None;
            RibbonAnimation.RespectSystemReduceMotion = false;
            RibbonAnimation.ClearActionLevel(RibbonAnimationAction.ContextualTab);
            foreach (var theme in new[] { RibbonTheme.Office2024Flat, RibbonTheme.Office2024, RibbonTheme.CrystalLight })
            foreach (bool dark in new[] { false, true })
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, Density = density };
                var home = new RibbonTab { Header = "Home" };
                var group = new RibbonGroup { Header = "Commands", CanResize = false };
                group.Items.Add(new RibbonButton { Header = "Save", Icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) });
                home.Groups.Add(group); ribbon.Tabs.Add(home); ribbon.SelectedTab = home;
                var window = new RibbonWindow { Content = ribbon, FlowDirection = direction, Width = 900, Height = 450,
                    Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
                try
                {
                    window.Show(); Layout(window);
                    if (density == RibbonDensity.Touch)
                    { VisualTreeHelper.SetRootDpi(window, new DpiScale(1.5, 1.5)); Layout(window); }
                    Console.WriteLine($"Geometry: {theme}/dark={dark}/{direction}/{density}/animate={animate}");
                    verify(window, ribbon);
                }
                finally { window.Close(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
            }
        }
        finally
        {
            RibbonAnimation.GlobalLevel = oldLevel; RibbonAnimation.RespectSystemReduceMotion = reduce;
            if (context is { } value) RibbonAnimation.SetActionLevel(RibbonAnimationAction.ContextualTab, value);
            else RibbonAnimation.ClearActionLevel(RibbonAnimationAction.ContextualTab);
        }
    }

    private static T Part<T>(Control owner, string name) where T : FrameworkElement => Assert.IsType<T>(owner.Template.FindName(name, owner));
    private static void Layout(Window window)
    { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); }
    private static void PumpUntil(Func<bool> done)
    {
        var frame = new DispatcherFrame(); var watch = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) => { if (done() || watch.ElapsedMilliseconds > 2000) frame.Continue = false; };
        timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Assert.True(done(), "Geometry sampling did not complete.");
    }
}
