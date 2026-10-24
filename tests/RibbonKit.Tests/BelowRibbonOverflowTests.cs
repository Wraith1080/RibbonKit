using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class BelowRibbonOverflowTests
{
    [Theory]
    [InlineData(RibbonDensity.Compact, FlowDirection.LeftToRight)]
    [InlineData(RibbonDensity.Touch, FlowDirection.LeftToRight)]
    [InlineData(RibbonDensity.Compact, FlowDirection.RightToLeft)]
    [InlineData(RibbonDensity.Touch, FlowDirection.RightToLeft)]
    public void Below_ribbon_uses_the_full_row_and_overflows_at_its_available_width(
        RibbonDensity density, FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(density, direction);
        var toolbar = Assert.IsType<RibbonQuickAccessToolBar>(host.Ribbon.Template.FindName("QatBelowItems", host.Ribbon));
        Assert.Same(toolbar, host.Ribbon.ActiveQuickAccessToolBar);
        Assert.Same(host.Ribbon, toolbar.Owner);
        Assert.Equal(density, Ribbon.GetDensity(toolbar));
        Assert.True(toolbar.ActualWidth > host.Ribbon.QuickAccessMaxWidth + 100);
        Assert.True(double.IsPositiveInfinity(toolbar.MaxWidth));
        Assert.True(toolbar.HasOverflow);
        var button = (ToggleButton)toolbar.Template.FindName("PART_OverflowButton", toolbar);
        Assert.True(button.IsVisible);
        Assert.InRange(button.TranslatePoint(new Point(), toolbar).X, -.5, toolbar.ActualWidth - button.ActualWidth + .5);
        toolbar.OpenOverflow(); host.Layout();
        Assert.Contains(toolbar.OverflowEntries, entry => Ribbon.GetQuickAccessSource(entry) == host.Last);
        var last = Assert.IsType<RibbonButton>(toolbar.OverflowEntries.Single(entry => Ribbon.GetQuickAccessSource(entry) == host.Last));
        last.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout();
        Assert.Equal(1, host.Invocations);
        Assert.False(host.Popup(toolbar).IsOpen);

        // Keep the resize case within the native monitor's maximum tracking width.
        var extras = host.Ribbon.QuickAccessItems.Skip(6).ToArray();
        foreach (var item in extras) host.Ribbon.QuickAccessItems.Remove(item);
        foreach (FrameworkElement item in host.Ribbon.QuickAccessItems) item.Width = 64;
        host.Layout();
        Assert.False(toolbar.HasOverflow); Assert.False(button.IsVisible);
        host.Window.Width = 320; host.Layout(); Assert.True(toolbar.HasOverflow);
        host.Window.Width = 640; host.Layout(); Assert.False(toolbar.HasOverflow);
        foreach (var item in extras) host.Ribbon.QuickAccessItems.Add(item);
        host.Layout(); Assert.True(toolbar.HasOverflow);

        host.Ribbon.IsMinimized = true; host.Layout();
        Assert.True(toolbar.IsVisible); Assert.True(button.IsVisible);
        toolbar.OpenOverflow(); host.Layout(); Assert.True(host.Popup(toolbar).IsOpen);
        host.Ribbon.IsQuickAccessVisible = false; host.Layout(); Assert.False(host.Popup(toolbar).IsOpen);
        host.Ribbon.IsQuickAccessVisible = true; host.Ribbon.IsMinimized = false; host.Layout();
        RibbonQuickAccessToolBar previous = toolbar;
        foreach (var position in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.TitleBar, RibbonQuickAccessPosition.BelowRibbon })
        {
            host.Ribbon.QuickAccessPosition = position; host.Layout();
            Assert.False(host.Popup(previous).IsOpen);
            var active = Assert.IsType<RibbonQuickAccessToolBar>(host.Ribbon.ActiveQuickAccessToolBar);
            Assert.Equal(host.Ribbon.QuickAccessItems.Count, active.Items.Count);
            Assert.True(active.HasOverflow);
            active.OpenOverflow(); host.Layout(); Assert.True(host.Popup(active).IsOpen);
            previous = active;
        }
    });

    private sealed class Host : IDisposable
    {
        internal readonly Ribbon Ribbon;
        internal readonly RibbonWindow Window;
        internal RibbonButton Last => (RibbonButton)Ribbon.GetQuickAccessSource((FrameworkElement)Ribbon.QuickAccessItems[^1])!;
        internal int Invocations;
        private readonly RibbonAnimationLevel _level = RibbonAnimation.GlobalLevel;
        internal Host(RibbonDensity density, FlowDirection direction)
        {
            var app = Sta.UseApplication();
            ThemeManager.Apply(app, RibbonTheme.CrystalLight); ThemeManager.SetDarkMode(app, true);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
                QuickAccessMaxWidth = 80, Density = density, FlowDirection = direction };
            var tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup { Header = "Commands" };
            tab.Groups.Add(group); Ribbon.Tabs.Add(tab); Ribbon.SelectedTab = tab;
            for (int i = 0; i < 30; i++)
            {
                var command = new RibbonButton { Header = $"Command {i}", Icon = new DrawingImage() };
                command.Click += (_, _) => Invocations++;
                group.Items.Add(command); Ribbon.AddToQuickAccess(command);
            }
            Window = new RibbonWindow { Content = Ribbon, Width = 640, Height = 360,
                FlowDirection = direction, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal Popup Popup(RibbonQuickAccessToolBar toolbar) => (Popup)toolbar.Template.FindName("PART_OverflowPopup", toolbar);
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); Window.UpdateLayout(); }
        public void Dispose() { Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = _level; Sta.ResetApplication(); }
    }
}
