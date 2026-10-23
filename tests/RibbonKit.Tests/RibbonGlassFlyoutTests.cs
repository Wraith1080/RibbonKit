using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class RibbonGlassFlyoutTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2013, false)]
    [InlineData(RibbonTheme.Office2013, true)]
    [InlineData(RibbonTheme.Office2019, false)]
    [InlineData(RibbonTheme.Office2019, true)]
    [InlineData(RibbonTheme.Office2024, false)]
    [InlineData(RibbonTheme.Office2024, true)]
    [InlineData(RibbonTheme.Office2024Flat, false)]
    [InlineData(RibbonTheme.Office2024Flat, true)]
    public void Glass_follows_group_and_both_overflows_but_not_command_menus(RibbonTheme theme, bool dark) => Run(theme, dark, (window, ribbon, group) =>
    {
        var overlay = ThemeManager.CreateGlassOverlay(window, dark);
        window.Resources.MergedDictionaries.Add(overlay);
        using var capture = new CapturedBackdrop(ribbon, window);
        capture.Apply(true); Layout(window);
        var groupPopup = Part<Popup>(group, "PART_Popup");
        groupPopup.IsOpen = true; Layout(window);
        var groupHost = Part<Border>(group, "PART_PopupHost");
        CheckGlass(groupHost);
        groupPopup.IsOpen = false; Layout(window);

        var qat = Assert.IsType<RibbonQuickAccessToolBar>(ribbon.ActiveQuickAccessToolBar);
        Assert.True(qat.HasOverflow);
        Part<ToggleButton>(qat, "PART_OverflowButton").IsChecked = true; Layout(window);
        var qatHost = Part<Border>(qat, "PART_OverflowPopupHost"); CheckGlass(qatHost);
        qat.CloseOverflow(); Layout(window);

        ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(window);
        var simplified = Assert.IsType<RibbonGroupsHost>(ribbon.SelectedTab!.Content).SimplifiedToolBar!;
        Assert.True(simplified.HasOverflow);
        Part<ToggleButton>(simplified, "PART_OverflowButton").IsChecked = true; Layout(window);
        var simplifiedHost = Part<Border>(simplified, "PART_OverflowPopupHost"); CheckGlass(simplifiedHost);
        simplified.CloseOverflow(); Layout(window);
        ribbon.LayoutMode = RibbonLayoutMode.Classic; Layout(window);

        // An ordinary command menu retains the theme surface under Glass.
        var menu = (RibbonDropDownButton)group.Items[^1];
        menu.IsDropDownOpen = true; Layout(window);
        var menuHost = Part<Border>(menu, "PART_MenuHost");
        Assert.Same(menu.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), menuHost.Background);
        menu.IsDropDownOpen = false; Layout(window);

        groupPopup.IsOpen = true; Layout(window); CheckGlass(groupHost);
        groupHost.Background = Brushes.Orange; capture.Refresh(); Layout(window);
        Assert.Same(Brushes.Orange, groupHost.Background);
        groupPopup.IsOpen = false; Layout(window); groupHost.ClearValue(Border.BackgroundProperty);
        groupPopup.IsOpen = true; Layout(window); capture.Refresh(); Layout(window); CheckGlass(groupHost);
        // Removing the overlay also retires live capture registrations.
        window.Resources.MergedDictionaries.Remove(overlay); Layout(window);
        Assert.Same(group.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), groupHost.Background);
        Assert.Same(qat.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), qatHost.Background);
        window.Resources.MergedDictionaries.Add(overlay); capture.Refresh(); Layout(window); CheckGlass(groupHost);
        capture.Refresh(); capture.Dispose(); Layout(window);
        Assert.Same(group.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), groupHost.Background);
        groupPopup.IsOpen = false;
    });

    [Theory]
    [InlineData(RibbonTheme.Office2007, false)]
    [InlineData(RibbonTheme.Office2007, true)]
    [InlineData(RibbonTheme.Office2010, false)]
    [InlineData(RibbonTheme.Office2010, true)]
    [InlineData(RibbonTheme.CrystalLight, false)]
    [InlineData(RibbonTheme.CrystalLight, true)]
    public void Legacy_and_Crystal_keep_their_existing_flyout_treatment(RibbonTheme theme, bool dark) => Run(theme, dark, (window, ribbon, group) =>
    {
        var overlay = ThemeManager.CreateGlassOverlay(window, dark);
        Assert.False(overlay.Contains("RibbonKit.Metrics.Ribbon.FlyoutGlassOpacity"));
        window.Resources.MergedDictionaries.Add(overlay);
        using var capture = new CapturedBackdrop(ribbon, window); capture.Apply(true);
        var popup = Part<Popup>(group, "PART_Popup"); popup.IsOpen = true; Layout(window);
        Assert.Same(group.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), Part<Border>(group, "PART_PopupHost").Background);
        popup.IsOpen = false;
    });

    private static void CheckGlass(Border host)
    {
        var material = Assert.IsType<DrawingGroup>(Assert.IsType<DrawingBrush>(host.Background).Drawing);
        Assert.IsAssignableFrom<System.Windows.Media.Imaging.BitmapSource>(Assert.IsType<ImageDrawing>(material.Children[1]).ImageSource);
        var tint = Assert.IsType<GeometryDrawing>(material.Children[2]).Brush;
        Assert.Equal(((Brush)host.FindResource("RibbonKit.Brushes.ApplicationMenu.FrameBand")).Opacity * .84, tint.Opacity, 5);
    }

    private static T Part<T>(Control control, string name) where T : class => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static void Layout(Window window) { window.UpdateLayout(); Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    private static void Run(RibbonTheme theme, bool dark, Action<Window, Ribbon, RibbonGroup> verify) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), theme); ThemeManager.SetDarkMode(Application.Current, dark);
        var level = RibbonKit.Animation.RibbonAnimation.GlobalLevel;
        RibbonKit.Animation.RibbonAnimation.GlobalLevel = RibbonKit.Animation.RibbonAnimationLevel.None;
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow, QuickAccessMaxWidth = 60 };
        var tab = new RibbonTab { Header = "Home" }; var group = new RibbonGroup { Header = "Commands" };
        for (int i = 0; i < 6; i++)
        {
            var button = new RibbonButton { Header = "Command " + i, Size = RibbonControlSize.Large,
                Icon = new DrawingImage(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) };
            group.Items.Add(button); ribbon.AddToQuickAccess(button);
        }
        var menu = new RibbonDropDownButton { Header = "Menu" }; menu.Items.Add(new RibbonMenuItem { Header = "Choice" }); group.Items.Add(menu);
        tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ((FrameworkElement)tab.Content).Width = 50;
        var dock = new DockPanel { Background = Brushes.CornflowerBlue }; DockPanel.SetDock(ribbon, Dock.Top); dock.Children.Add(ribbon);
        dock.Children.Add(new Border { Background = Brushes.Orange });
        var window = new RibbonWindow { Content = dock, Width = 720, Height = 450, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try { window.Show(); Layout(window); verify(window, ribbon, group); }
        finally { window.Close(); Sta.Drain(); RibbonKit.Animation.RibbonAnimation.GlobalLevel = level; Sta.ResetApplication(); }
    });
}
