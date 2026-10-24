using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class OverflowMenuPresentationTests
{
    private static void PressOverflowKey(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target)!;
        var down = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        target.RaiseEvent(down);
        if (!down.Handled) { down.RoutedEvent = Keyboard.KeyDownEvent; target.RaiseEvent(down); }
        var up = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyUpEvent };
        target.RaiseEvent(up);
        if (!up.Handled) { up.RoutedEvent = Keyboard.KeyUpEvent; target.RaiseEvent(up); }
        Sta.Drain();
    }

    [Theory]
    [InlineData(false, RibbonDensity.Compact, FlowDirection.LeftToRight)]
    [InlineData(true, RibbonDensity.Compact, FlowDirection.LeftToRight)]
    [InlineData(false, RibbonDensity.Touch, FlowDirection.LeftToRight)]
    [InlineData(true, RibbonDensity.Touch, FlowDirection.LeftToRight)]
    [InlineData(false, RibbonDensity.Compact, FlowDirection.RightToLeft)]
    [InlineData(true, RibbonDensity.Compact, FlowDirection.RightToLeft)]
    [InlineData(false, RibbonDensity.Touch, FlowDirection.RightToLeft)]
    [InlineData(true, RibbonDensity.Touch, FlowDirection.RightToLeft)]
    public void Vertical_arrows_traverse_overflow_rows_without_opening_submenus(
        bool simplified, RibbonDensity density, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(simplified, density, flow, windowFlow: true);
        AddScrollCommands(host);
        var gallery = new InRibbonGallery { Header = "Extra styles", Width = 120 };
        gallery.Items.Add(new RibbonGalleryItem { Content = "Style A" });
        Ribbon.SetSimplifiedPlacement(gallery, RibbonSimplifiedPlacement.Inline);
        host.Group.Items.Add(gallery); host.Ribbon.AddToQuickAccess(gallery);
        host.Window.Activate(); host.Layout();
        var opener = (ToggleButton)host.Toolbar.Template.FindName("PART_OverflowButton", host.Toolbar);
        Assert.True(opener.Focus()); PressOverflowKey(opener, Key.Down); host.Layout();
        var entries = host.Toolbar.OverflowEntries.ToArray();
        entries.Single(entry => entry is RibbonButton button && button.Header == "Command 0").IsEnabled = false;
        entries.Single(entry => entry is RibbonButton button && button.Header == "Command 1").Visibility = Visibility.Collapsed;
        var viewer = (ScrollViewer)host.Toolbar.Template.FindName("PART_OverflowScrollViewer", host.Toolbar);
        viewer.MaxHeight = 220; host.Layout();
        var viewport = (ScrollContentPresenter)viewer.Template.FindName("PART_ScrollContentPresenter", viewer);
        var candidates = entries.Where(entry => entry.IsEnabled && entry.IsVisible).ToArray();
        Assert.True(host.Popup.IsOpen, $"Opening overflow failed; window active={host.Window.IsActive}, focus={Keyboard.FocusedElement}");
        Assert.NotEmpty(candidates);
        Check(candidates[0]);
        foreach (var next in candidates.Skip(1)) { PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Down); host.Layout(); Check(next); }
        Assert.True(viewer.VerticalOffset > 0);
        foreach (var previous in candidates.AsEnumerable().Reverse().Skip(1)) { PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Up); host.Layout(); Check(previous); }
        PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Up); host.Layout(); Check(candidates[^1]);
        PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Down); host.Layout(); Check(candidates[0]);
        PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Escape); host.Layout();
        Assert.False(host.Popup.IsOpen); Assert.Same(opener, Keyboard.FocusedElement);

        void Check(FrameworkElement expected)
        {
            Assert.True(host.Popup.IsOpen);
            Assert.All(entries.OfType<RibbonDropDownButton>(), entry => Assert.False(entry.IsDropDownOpen));
            Assert.True(expected.IsKeyboardFocusWithin, $"Expected {expected}, actual {Keyboard.FocusedElement}");
            double top = expected.TranslatePoint(new Point(), viewport).Y;
            Assert.InRange(top, -.5, viewport.ActualHeight - expected.ActualHeight + .5);
        }
    });

    [Theory]
    [InlineData(false, FlowDirection.LeftToRight)]
    [InlineData(true, FlowDirection.LeftToRight)]
    [InlineData(false, FlowDirection.RightToLeft)]
    [InlineData(true, FlowDirection.RightToLeft)]
    public void Explicit_submenu_entry_keeps_vertical_keys_inside_the_child(bool simplified, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(simplified, RibbonDensity.Touch, flow, windowFlow: true);
        host.Dropdown.Items.Add(new RibbonMenuItem { Header = "Second action" });
        host.Window.Activate(); host.Layout();
        host.Toolbar.OpenOverflow(); host.Layout();
        var entry = (RibbonDropDownButton)host.Entry(host.Dropdown);
        Assert.Equal(flow, entry.FlowDirection);
        var opener = (ToggleButton)entry.Template.FindName("PART_Toggle", entry);
        Assert.True(opener.Focus());
        PressOverflowKey(opener, flow == FlowDirection.LeftToRight ? Key.Right : Key.Left); host.Layout();
        Assert.True(entry.IsDropDownOpen);
        Assert.True(((UIElement)entry.Items[0]).IsKeyboardFocusWithin);
        PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Down); host.Layout();
        Assert.True(((UIElement)entry.Items[1]).IsKeyboardFocusWithin);
        Assert.True(entry.IsDropDownOpen); Assert.True(host.Popup.IsOpen);
        PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Up); host.Layout();
        Assert.True(((UIElement)entry.Items[0]).IsKeyboardFocusWithin);
        PressOverflowKey((UIElement)Keyboard.FocusedElement, flow == FlowDirection.LeftToRight ? Key.Left : Key.Right); host.Layout();
        Assert.False(entry.IsDropDownOpen, $"Return failed: entry flow={entry.FlowDirection}, opener flow={opener.FlowDirection}, focus={Keyboard.FocusedElement}, outer={host.Popup.IsOpen}");
        Assert.True(opener.IsKeyboardFocused);
        PressOverflowKey(opener, Key.Enter); host.Layout();
        Assert.True(entry.IsDropDownOpen);
        PressOverflowKey((UIElement)Keyboard.FocusedElement, Key.Escape); host.Layout();
        Assert.False(entry.IsDropDownOpen); Assert.True(host.Popup.IsOpen);
    });
}
