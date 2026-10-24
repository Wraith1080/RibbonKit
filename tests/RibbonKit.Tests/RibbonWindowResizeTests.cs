using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public class RibbonWindowResizeTests
{
    [Theory]
    [InlineData(RibbonTheme.CrystalLight, RibbonQuickAccessPosition.TitleBar)]
    [InlineData(RibbonTheme.CrystalLight, RibbonQuickAccessPosition.TabRow)]
    [InlineData(RibbonTheme.CrystalLight, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(RibbonTheme.Office2024Flat, RibbonQuickAccessPosition.TitleBar)]
    [InlineData(RibbonTheme.Office2024Flat, RibbonQuickAccessPosition.TabRow)]
    [InlineData(RibbonTheme.Office2024Flat, RibbonQuickAccessPosition.BelowRibbon)]
    public void Native_resize_reuses_ribbon_paint_and_releases_it_on_exit_cancel_theme_and_close(
        RibbonTheme theme, RibbonQuickAccessPosition placement) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(); ThemeManager.Apply(application, theme);
        var ribbon = new Ribbon { QuickAccessPosition = placement };
        if (placement == RibbonQuickAccessPosition.TitleBar) ribbon.Density = RibbonDensity.Touch;
        var tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup { Header = "Commands" };
        var button = new RibbonButton { Header = "Live command" }; group.Items.Add(button); tab.Groups.Add(group);
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        var editor = new TextBox { Text = "Live document" };
        var panel = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); panel.Children.Add(ribbon); panel.Children.Add(editor);
        var window = new RibbonWindow { Content = panel, Width = 840, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(DispatcherPriority.ApplicationIdle); window.UpdateLayout();
            var handle = new WindowInteropHelper(window).Handle;
            var qat = ribbon.ActiveQuickAccessToolBar;
            if (placement != RibbonQuickAccessPosition.BelowRibbon) Assert.NotNull(qat);
            bool detached = qat is not null && !ribbon.IsAncestorOf(qat);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                SendMessage(handle, 0x0231, IntPtr.Zero, IntPtr.Zero);
                var cache = Assert.IsType<BitmapCache>(ribbon.CacheMode);
                Assert.True(cache.EnableClearType); Assert.True(cache.SnapsToDevicePixels);
                if (detached) Assert.IsType<BitmapCache>(qat!.CacheMode);
                Assert.Null(editor.CacheMode);
                int clicked = 0; RoutedEventHandler handler = (_, _) => clicked++;
                button.Click += handler;
                button.RaiseEvent(new RoutedEventArgs(RibbonButton.ClickEvent, button));
                button.Click -= handler;
                Assert.Equal(1, clicked);
                button.Header = "Still live " + cycle;
                window.Height += 3; window.UpdateLayout();
                Assert.Same(cache, ribbon.CacheMode);
                if (cycle == 2) ThemeManager.SetDarkMode(application, true);
                else SendMessage(handle, cycle == 0 ? 0x0232u : 0x001fu, IntPtr.Zero, IntPtr.Zero);
                Assert.Null(ribbon.CacheMode);
                Assert.Equal(DependencyProperty.UnsetValue, ribbon.ReadLocalValue(UIElement.CacheModeProperty));
                if (detached) Assert.Null(qat!.CacheMode);
            }
            SendMessage(handle, 0x0231, IntPtr.Zero, IntPtr.Zero);
            Assert.IsType<BitmapCache>(ribbon.CacheMode);
            window.Close(); Assert.Null(ribbon.CacheMode);
        }
        finally { window.Close(); ThemeManager.SetDarkMode(application, false); }
    });

    [Theory]
    [InlineData("local")]
    [InlineData("binding")]
    [InlineData("style")]
    [InlineData("explicit-null")]
    public void Resize_cache_respects_authored_cache_modes(string source) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        var ribbon = new Ribbon(); var authored = new BitmapCache { RenderAtScale = 2 };
        Binding? binding = null;
        if (source == "binding")
        {
            binding = new Binding { Source = authored };
            ribbon.SetBinding(UIElement.CacheModeProperty, binding);
        }
        else if (source == "style")
        {
            var style = new Style(typeof(Ribbon)); style.Setters.Add(new Setter(UIElement.CacheModeProperty, authored)); ribbon.Style = style;
        }
        else ribbon.CacheMode = source == "explicit-null" ? null : authored;
        var window = new RibbonWindow { Content = ribbon, Width = 800, Height = 300,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain();
            var handle = new WindowInteropHelper(window).Handle;
            SendMessage(handle, 0x0231, IntPtr.Zero, IntPtr.Zero);
            Assert.Same(source == "explicit-null" ? null : authored, ribbon.CacheMode);
            if (source == "explicit-null") Assert.Null(ribbon.ReadLocalValue(UIElement.CacheModeProperty));
            SendMessage(handle, 0x0232, IntPtr.Zero, IntPtr.Zero);
            Assert.Same(source == "explicit-null" ? null : authored, ribbon.CacheMode);
            if (source == "explicit-null") Assert.Null(ribbon.ReadLocalValue(UIElement.CacheModeProperty));
            if (binding != null) Assert.Same(binding, BindingOperations.GetBinding(ribbon, UIElement.CacheModeProperty));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Author_cache_replacement_during_resize_survives_cleanup() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        var ribbon = new Ribbon();
        var window = new RibbonWindow { Content = ribbon, Width = 800, Height = 300,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); var handle = new WindowInteropHelper(window).Handle;
            SendMessage(handle, 0x0231, IntPtr.Zero, IntPtr.Zero);
            Assert.IsType<BitmapCache>(ribbon.CacheMode);
            var authored = new BitmapCache { RenderAtScale = 2 }; ribbon.CacheMode = authored;
            SendMessage(handle, 0x0232, IntPtr.Zero, IntPtr.Zero);
            Assert.Same(authored, ribbon.CacheMode);
        }
        finally { window.Close(); }
    });

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);

    [Theory]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight, false)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft, false)]
    [InlineData(RibbonTheme.Office2024Flat, FlowDirection.LeftToRight, false)]
    [InlineData(RibbonTheme.Office2024Flat, FlowDirection.RightToLeft, false)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight, true)]
    [InlineData(RibbonTheme.Office2024Flat, FlowDirection.LeftToRight, true)]
    [InlineData(RibbonTheme.Office2007, FlowDirection.LeftToRight, false)]
    [InlineData(RibbonTheme.Office2010, FlowDirection.RightToLeft, false)]
    public void Height_resize_keeps_caption_and_content_origin_stable(RibbonTheme theme, FlowDirection direction, bool touch) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), theme);
        var titleContent = new Border { Width = 80, Height = touch ? 46 : 20 };
        Ribbon.SetDensity(titleContent, touch ? RibbonDensity.Touch : RibbonDensity.Compact);
        var content = new Border { Background = Brushes.White };
        var window = new RibbonWindow
        {
            Content = content, TitleBarContent = titleContent, FlowDirection = direction,
            UseLayoutRounding = true, Width = 840, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false,
        };
        try
        {
            window.Show(); Sta.Drain(DispatcherPriority.ApplicationIdle); window.UpdateLayout();
            var caption = Assert.IsType<Grid>(window.Template.FindName("TitleBarBand", window));
            double expectedTop = content.TranslatePoint(new Point(), window).Y;
            double expectedCaption = caption.ActualHeight;
            double scale = VisualTreeHelper.GetDpi(window).DpiScaleY;
            for (int step = 1; step <= 128; step++)
            {
                int pixel = step <= 64 ? step : 128 - step;
                window.Height = 400 + pixel / scale;
                window.UpdateLayout(); Sta.Drain(DispatcherPriority.Loaded);
                Assert.True(Math.Abs(content.TranslatePoint(new Point(), window).Y - expectedTop) < .01,
                    $"{theme}/{direction}/Touch={touch}, DPI={scale}, pixel={pixel}: content top {content.TranslatePoint(new Point(), window).Y} vs {expectedTop}; caption {caption.ActualHeight} vs {expectedCaption}");
                Assert.Equal(expectedCaption, caption.ActualHeight, 4);
            }
        }
        finally { window.Close(); }
    });
}
