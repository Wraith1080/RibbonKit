using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Layout;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class TouchAdaptiveLayoutTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Showcase_view_collapses_backstage_on_initial_touch_layout(bool setBeforeShow) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        MainWindow? window = null;
        try
        {
            window = new MainWindow { Width = 1080, Height = 680,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            var type = typeof(MainWindow);
            window.Loaded -= (RoutedEventHandler)type.GetMethod("OnWindowLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), window);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            var ribbon = window.MainRibbon;
            var view = ribbon.Tabs.Single(t => Equals(t.Header, "View"));
            var savedLayout = RibbonCustomizationSerializer.Serialize(ribbon);
            if (setBeforeShow)
            {
                RibbonCustomizationSerializer.Apply(ribbon, savedLayout);
                window.TouchModeToggle.IsChecked = true;
                ribbon.SelectedTab = view;
            }
            window.Show(); Layout(window);
            if (!setBeforeShow)
            {
                RibbonCustomizationSerializer.Apply(ribbon, savedLayout);
                window.TouchModeToggle.IsChecked = true;
                Layout(window);
            }
            ribbon.SelectedTab = view; Layout(window);
            var backstageGroup = view.Groups.Single(g => Equals(g.Header, "Backstage"));
            Assert.Equal(RibbonGroupSizeState.Collapsed, backstageGroup.SizeState);
            var scroll = Part<RibbonScrollContentHost>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "PART_ContentScroll");
            Assert.Equal(scroll.ExtentWidth > scroll.ViewportWidth + 0.5, scroll.CanScrollRight);
            SavePreview(window, $"adaptive-startup-{setBeforeShow}");
        }
        finally
        {
            window?.Close();
            RibbonAnimation.GlobalLevel = animation;
            Sta.ResetApplication();
        }
    });

    [Theory]
    [InlineData(RibbonTheme.Office2007, false)]
    [InlineData(RibbonTheme.Office2010, false)]
    [InlineData(RibbonTheme.Office2013, false)]
    [InlineData(RibbonTheme.Office2019, false)]
    [InlineData(RibbonTheme.Office2024, false)]
    [InlineData(RibbonTheme.CrystalLight, false)]
    [InlineData(RibbonTheme.Office2007, true)]
    [InlineData(RibbonTheme.Office2010, true)]
    [InlineData(RibbonTheme.Office2013, true)]
    [InlineData(RibbonTheme.Office2019, true)]
    [InlineData(RibbonTheme.Office2024, true)]
    [InlineData(RibbonTheme.CrystalLight, true)]
    public void Density_changes_in_a_collapsed_flyout_keep_reduction_and_overflow_current(RibbonTheme theme, bool startTouch) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        ThemeManager.Apply(application, theme);
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var tab = new RibbonTab { Header = "View" };
        var fixedGroup = new RibbonGroup { Header = "Fixed", CanResize = false, Width = 340 };
        fixedGroup.Items.Add(new RibbonButton { Header = "Fixed", Size = RibbonControlSize.Large });
        tab.Groups.Add(fixedGroup);
        var adaptive = new RibbonGroup { Header = "Backstage" };
        for (int i = 0; i < 7; i++)
            adaptive.Items.Add(new RibbonToggleButton { Header = $"Command {i}", Size = RibbonControlSize.Large });
        tab.Groups.Add(adaptive);
        var ribbon = new Ribbon { Density = startTouch ? RibbonDensity.Touch : RibbonDensity.Compact };
        ribbon.Tabs.Add(tab);
        ribbon.SelectedTab = tab;
        var window = new Window { Content = ribbon, Width = 560, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(window);
            AssertCollapsed();
            for (int iteration = 0; iteration < 4; iteration++)
            {
                Part<ToggleButton>(adaptive, "PART_CollapsedButton").IsChecked = true;
                Layout(window);
                Assert.NotNull(Part<Border>(adaptive, "PART_PopupHost").Child);
                ribbon.Density = ribbon.Density == RibbonDensity.Compact ? RibbonDensity.Touch : RibbonDensity.Compact;
                Layout(window);
                AssertCollapsed();
            }

            // Content which cannot reduce must expose scrolling immediately at the
            // same viewport width, without a resize or a tab switch to refresh it.
            adaptive.CanResize = false;
            Layout(window);
            var scroll = Part<RibbonScrollContentHost>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "PART_ContentScroll");
            Assert.True(scroll.ExtentWidth > scroll.ViewportWidth);
            Assert.True(scroll.CanScrollRight);
            Assert.Equal(RibbonGroupSizeState.Large, adaptive.SizeState);
        }
        finally
        {
            window.Close();
            RibbonAnimation.GlobalLevel = animation;
            Sta.ResetApplication();
        }

        void AssertCollapsed() => Assert.True(adaptive.SizeState == RibbonGroupSizeState.Collapsed,
            $"{theme}/{ribbon.Density}: expected collapsed, got {adaptive.SizeState}; group={adaptive.DesiredSize.Width}, ribbon={ribbon.ActualWidth}");
    });

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Layout(Window window)
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        window.UpdateLayout();
    }

    private static void SavePreview(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_TOUCH_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),
            (int)Math.Ceiling(element.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(file);
    }
}
