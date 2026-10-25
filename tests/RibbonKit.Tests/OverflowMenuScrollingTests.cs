using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class OverflowMenuPresentationTests
{
    [Theory]
    [InlineData(false, RibbonDensity.Compact, FlowDirection.LeftToRight, RibbonTheme.Office2024, false)]
    [InlineData(true, RibbonDensity.Compact, FlowDirection.LeftToRight, RibbonTheme.Office2024, false)]
    [InlineData(false, RibbonDensity.Touch, FlowDirection.RightToLeft, RibbonTheme.Office2024Flat, true)]
    [InlineData(true, RibbonDensity.Touch, FlowDirection.RightToLeft, RibbonTheme.Office2024Flat, true)]
    [InlineData(false, RibbonDensity.Compact, FlowDirection.RightToLeft, RibbonTheme.CrystalLight, false)]
    [InlineData(true, RibbonDensity.Compact, FlowDirection.RightToLeft, RibbonTheme.CrystalLight, false)]
    [InlineData(false, RibbonDensity.Touch, FlowDirection.LeftToRight, RibbonTheme.Office2007, true)]
    [InlineData(true, RibbonDensity.Touch, FlowDirection.LeftToRight, RibbonTheme.Office2007, true)]
    public void Long_overflow_uses_separate_scroll_bands_and_keeps_commands_reachable(
        bool simplified, RibbonDensity density, FlowDirection flow, RibbonTheme theme, bool dark) => Sta.Run(() =>
    {
        using var host = new Host(simplified, density, flow);
        ThemeManager.Apply(Sta.UseApplication(), theme);
        ThemeManager.SetDarkMode(Application.Current, dark);
        AddScrollCommands(host);
        host.Layout(); host.Toolbar.OpenOverflow(); host.Layout();
        var viewer = Assert.IsType<ScrollViewer>(host.Toolbar.Template.FindName("PART_OverflowScrollViewer", host.Toolbar));
        Assert.True(double.IsFinite(viewer.MaxHeight));
        viewer.MaxHeight = 220; host.Layout();
        var up = Assert.IsType<RepeatButton>(viewer.Template.FindName("PART_ScrollUp", viewer));
        var down = Assert.IsType<RepeatButton>(viewer.Template.FindName("PART_ScrollDown", viewer));
        var viewport = Assert.IsType<ScrollContentPresenter>(viewer.Template.FindName("PART_ScrollContentPresenter", viewer));
        Assert.InRange(viewer.ActualHeight, 1, 220.5);
        Assert.True(viewer.ScrollableHeight > 0);
        Assert.True(up.IsVisible); Assert.True(down.IsVisible);
        Assert.False(up.IsEnabled); Assert.True(down.IsEnabled);
        viewer.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.MouseWheelEvent });
        host.Layout(); Assert.True(viewer.VerticalOffset > 0);
        viewer.ScrollToTop(); host.Layout();
        if (density == RibbonDensity.Touch) Assert.True(down.ActualHeight >= 44);
        Assert.True(up.TranslatePoint(new Point(0, up.ActualHeight), viewer).Y <= viewport.TranslatePoint(new Point(), viewer).Y + .5);
        Assert.True(viewport.TranslatePoint(new Point(0, viewport.ActualHeight), viewer).Y <= down.TranslatePoint(new Point(), viewer).Y + .5);
        Assert.DoesNotContain(Descendants(viewer).OfType<ScrollBar>(), bar => bar.IsVisible);
        down.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout();
        Assert.True(viewer.VerticalOffset > 0); Assert.True(host.Popup.IsOpen);
        viewer.ScrollToBottom(); host.Layout();
        Assert.False(down.IsEnabled); Assert.True(up.IsEnabled);
        viewer.ScrollToTop(); host.Layout();
        var last = host.Toolbar.OverflowEntries.Last();
        last.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, null, last)
            { RoutedEvent = Keyboard.GotKeyboardFocusEvent });
        host.Layout();
        Assert.True(viewer.VerticalOffset > 0);
        host.Toolbar.CloseOverflow(); host.Layout();
        host.Toolbar.OpenOverflow(); host.Layout();
        Assert.Equal(0, viewer.VerticalOffset);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Overflow_hover_stops_on_leave_close_and_nested_popup(bool simplified) => Sta.Run(() =>
    {
        using var host = new Host(simplified, RibbonDensity.Compact, FlowDirection.LeftToRight);
        AddScrollCommands(host); host.Layout();
        host.Toolbar.OpenOverflow(); host.Layout();
        var viewer = (ScrollViewer)host.Toolbar.Template.FindName("PART_OverflowScrollViewer", host.Toolbar);
        viewer.MaxHeight = 220; host.Layout();
        var down = (RepeatButton)viewer.Template.FindName("PART_ScrollDown", viewer);
        Enter(); Wait(500); host.Layout(); Assert.True(viewer.VerticalOffset > 0);
        down.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseLeaveEvent });
        double stopped = viewer.VerticalOffset;
        Wait(220); host.Layout(); Assert.Equal(stopped, viewer.VerticalOffset);
        viewer.ScrollToTop(); host.Layout();
        var entry = (RibbonDropDownButton)host.Entry(host.Dropdown);
        entry.IsDropDownOpen = true; host.Layout();
        Enter(); Wait(500); host.Layout();
        Assert.Equal(0, viewer.VerticalOffset); Assert.True(entry.IsDropDownOpen); Assert.True(host.Popup.IsOpen);
        entry.IsDropDownOpen = false; host.Layout();
        Enter(); host.Toolbar.CloseOverflow(); host.Layout(); Wait(450);
        host.Toolbar.OpenOverflow(); host.Layout();
        Assert.Equal(0, viewer.VerticalOffset);

        void Enter() => down.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
    });

    private static void AddScrollCommands(Host host)
    {
        for (int i = 0; i < 30; i++)
        {
            var command = new RibbonButton { Header = $"Command {i}" };
            Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline);
            host.Group.Items.Add(command);
            host.Ribbon.AddToQuickAccess(command);
        }
    }

    private static void Wait(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Short_overflow_does_not_reserve_scroll_bands(bool simplified) => Sta.Run(() =>
    {
        using var host = new Host(simplified, RibbonDensity.Compact, FlowDirection.LeftToRight);
        host.Toolbar.OpenOverflow(); host.Layout();
        var viewer = Assert.IsType<ScrollViewer>(host.Toolbar.Template.FindName("PART_OverflowScrollViewer", host.Toolbar));
        Assert.Equal(0, viewer.ScrollableHeight);
        Assert.False(((RepeatButton)viewer.Template.FindName("PART_ScrollUp", viewer)).IsVisible);
        Assert.False(((RepeatButton)viewer.Template.FindName("PART_ScrollDown", viewer)).IsVisible);
    });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Escape_from_a_native_combo_closes_only_the_innermost_popup(bool borrowed, bool editable) => Sta.Run(() =>
    {
        using var host = new Host(false, RibbonDensity.Compact, FlowDirection.LeftToRight);
        host.Combo.IsEditable = editable;
        RibbonGroupQuickAccessProxy? proxy = null;
        if (borrowed)
        {
            host.Toolbar.OpenOverflow(); host.Layout();
            proxy = (RibbonGroupQuickAccessProxy)host.Entry(host.Group);
            proxy.IsDropDownOpen = true;
        }
        else
        {
            host.Group.SetSizeState(RibbonGroupSizeState.Collapsed);
            ((ToggleButton)host.Group.Template.FindName("PART_CollapsedButton", host.Group)).IsChecked = true;
        }
        host.Layout();
        var groupPopup = (Popup)host.Group.Template.FindName("PART_Popup", host.Group);
        Assert.True(groupPopup.IsOpen);
        host.Combo.IsDropDownOpen = true; host.Layout();
        var preview = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(host.Combo)!, 0, Key.Escape)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        host.Combo.RaiseEvent(preview);
        Assert.True(groupPopup.IsOpen);
        if (!preview.Handled)
            host.Combo.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(host.Combo)!, 0, Key.Escape)
                { RoutedEvent = Keyboard.KeyDownEvent });
        host.Layout();
        Assert.False(host.Combo.IsDropDownOpen);
        Assert.True(groupPopup.IsOpen);
        Assert.Equal(0, host.Combo.SelectedIndex);
        Send(host.Combo, Key.Escape); host.Layout();
        Assert.False(groupPopup.IsOpen);
        if (borrowed) Assert.True(host.Popup.IsOpen);
    });
}
