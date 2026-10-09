using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class QuickAccessPopupInteractionTests
{
    [Theory]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonDensity.Compact)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonDensity.Touch)]
    [InlineData(RibbonQuickAccessPosition.TabRow, RibbonDensity.Compact)]
    [InlineData(RibbonQuickAccessPosition.TabRow, RibbonDensity.Touch)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, RibbonDensity.Compact)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, RibbonDensity.Touch)]
    public void Every_placement_pauses_ribbon_hover_and_keeps_its_own_host_live(RibbonQuickAccessPosition position, RibbonDensity density) => Sta.Run(() =>
    {
        var inside = new RibbonComboBox { Items = { "One", "Two" } };
        var group = new RibbonGroup { Header = "Tools", Items = { inside } };
        var outside = new RibbonButton { Header = "Outside" };
        var ribbon = new Ribbon { QuickAccessPosition = position, Density = density,
            Tabs = { new RibbonTab { Header = "Home", Groups = { group, new RibbonGroup { Items = { outside } } } } } };
        ribbon.AddToQuickAccess(group); using var host = new Host(ribbon, position == RibbonQuickAccessPosition.TitleBar);
        var copy = (RibbonDropDownButton)ribbon.QuickAccessItems[0];
        copy.IsDropDownOpen = true; host.Layout(); Assert.True(Part<Popup>(copy, "PART_Popup").IsOpen);
        Assert.Same(ribbon, copy.GetValue(RibbonPopupInteraction.QuickAccessOwnerProperty));
        Hover(outside, true); Hover(copy, true); Hover(inside, true);
        Assert.False(RibbonPopupInteraction.GetIsHovered(outside));
        Assert.True(RibbonPopupInteraction.GetIsHovered(copy)); Assert.True(RibbonPopupInteraction.GetIsHovered(inside));
        copy.IsDropDownOpen = false; host.Layout(); Assert.True(RibbonPopupInteraction.GetIsHovered(outside));
    });

    [Fact]
    public void Pointer_group_open_does_not_focus_the_first_editor_but_keyboard_open_does() => Sta.Run(() =>
    {
        var first = new RibbonComboBox { IsEditable = true, Width = 130, Items = { "One", "Two" }, SelectedIndex = 0 };
        var group = new RibbonGroup { Header = "Tools", Items = { first, new RibbonButton { Header = "Later" } } };
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
            Tabs = { new RibbonTab { Header = "Home", Groups = { group } } } };
        ribbon.AddToQuickAccess(group); using var host = new Host(ribbon);
        var copy = (RibbonDropDownButton)ribbon.QuickAccessItems[0];
        var opener = Part<ToggleButton>(copy, "PART_Toggle"); opener.Focus();
        int visits = 0; first.GotKeyboardFocus += (_, _) => visits++;
        copy.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            { RoutedEvent = Mouse.PreviewMouseUpEvent });
        copy.IsDropDownOpen = true; host.Layout();
        Assert.Equal(0, visits); Assert.Same(opener, Keyboard.FocusedElement);
        copy.IsDropDownOpen = false; host.Layout(); copy.IsDropDownOpen = true; host.Layout();
        Assert.True(first.IsKeyboardFocusWithin); Assert.True(visits > 0);
    });

    [Theory]
    [InlineData("group", false)]
    [InlineData("combo", false)]
    [InlineData("gallery", false)]
    [InlineData("dropdown", false)]
    [InlineData("group", true)]
    [InlineData("combo", true)]
    [InlineData("gallery", true)]
    [InlineData("dropdown", true)]
    public void Outside_press_keeps_the_ribbon_targets_focus_and_capture(string kind, bool overflow) => Sta.Run(() =>
    {
        var target = new RibbonButton { Header = "Run" };
        var group = new RibbonGroup { Header = "Tools", Items = { new RibbonButton { Header = "Inside" } } };
        FrameworkElement source = kind switch
        {
            "group" => group,
            "combo" => new RibbonComboBox { Items = { "One", "Two" } },
            "gallery" => new InRibbonGallery { Width = 180, Items = { new RibbonGalleryItem { Content = "Tile" } } },
            _ => new RibbonDropDownButton { Header = "Menu", Items = { new RibbonMenuItem { Header = "Action" } } },
        };
        if (!ReferenceEquals(source, group)) group.Items.Add(source);
        var tab = new RibbonTab { Header = "Home", Groups = { group, new RibbonGroup { Header = "Commands", Items = { target } } } };
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow, Tabs = { tab },
            ApplicationMenu = new RibbonApplicationMenu { Items = { new RibbonApplicationMenuItem { Header = "Save" } } } };
        if (overflow) ribbon.QuickAccessMaxWidth = 35;
        ribbon.AddToQuickAccess(source);
        using var host = new Host(ribbon);
        var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
        var toolbar = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
        RibbonDropDownButton copy = (RibbonDropDownButton)ribbon.QuickAccessItems[0];
        if (overflow)
        {
            toolbar.OpenOverflow(); host.Layout();
            copy = Assert.Single(toolbar.OverflowEntries.OfType<RibbonDropDownButton>());
        }
        foreach (var clicked in new ButtonBase[] { target, Part<ButtonBase>(tabs, "PART_ApplicationButton") })
        {
            if (overflow) { toolbar.OpenOverflow(); host.Layout(); }
            copy.IsDropDownOpen = true; host.Layout();
            Assert.True(RibbonPopupInteraction.GetSuppressHover(ribbon));
            Assert.True(RibbonPopupInteraction.GetSuppressHover(target));
            Assert.False(RibbonPopupInteraction.GetSuppressHover(Part<Popup>(copy, "PART_Popup").Child));
            Assert.True(copy.IsDropDownOpen);
            var press = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                { RoutedEvent = Mouse.PreviewMouseDownEvent };
            clicked.RaiseEvent(press);
            Assert.False(press.Handled); // The native button's down/up path remains available.
            Assert.False(copy.IsDropDownOpen);
            // Model the target's down phase before WPF delivers delayed Popup.Closed.
            Assert.True(clicked.Focus()); Assert.True(Mouse.Capture(clicked));
            host.Layout();
            Assert.Same(clicked, Keyboard.FocusedElement); Assert.Same(clicked, Mouse.Captured);
            Assert.False(RibbonPopupInteraction.GetSuppressHover(ribbon));
            Mouse.Capture(null);
        }
        copy.IsDropDownOpen = true; host.Layout();
        ribbon.QuickAccessItems.Clear(); host.Layout();
        Assert.False(RibbonPopupInteraction.GetSuppressHover(ribbon));
    });

    [Fact]
    public void Effective_hover_pauses_ribbon_chrome_but_keeps_qat_and_nested_controls_live() => Sta.Run(() =>
    {
        var button = new RibbonButton { Header = "Command" };
        var nested = new RibbonDropDownButton { Header = "Nested", Items = { new RibbonMenuItem { Header = "Pick" } } };
        var group = new RibbonGroup { Header = "Tools", Items = { nested } };
        var other = new RibbonGroup { Header = "Other", Items = { button } };
        var tab = new RibbonTab { Header = "Home", Groups = { group, other } };
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow, Tabs = { tab },
            ApplicationMenu = new RibbonApplicationMenu() };
        ribbon.AddToQuickAccess(group); using var host = new Host(ribbon);
        var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
        var header = Part<FrameworkElement>(tab, "HeaderChrome");
        var file = Part<ButtonBase>(tabs, "PART_ApplicationButton");
        var copy = (RibbonDropDownButton)ribbon.QuickAccessItems[0];
        foreach (var surface in new UIElement[] { button, header, file })
        {
            Hover(surface, true); Assert.True(RibbonPopupInteraction.GetIsHovered(surface));
        }
        copy.IsDropDownOpen = true; host.Layout();
        foreach (var surface in new UIElement[] { button, header, file })
        {
            Assert.True((bool)surface.GetValue(UIElement.IsMouseOverProperty)); Assert.False(RibbonPopupInteraction.GetIsHovered(surface));
        }
        Hover(copy, true); Hover(nested, true);
        Assert.True(RibbonPopupInteraction.GetIsHovered(copy)); Assert.True(RibbonPopupInteraction.GetIsHovered(nested));
        nested.IsDropDownOpen = true; host.Layout(); nested.IsDropDownOpen = false; host.Layout();
        Assert.True(RibbonPopupInteraction.GetSuppressHover(ribbon));
        copy.IsDropDownOpen = false; host.Layout();
        foreach (var surface in new UIElement[] { button, header, file }) Assert.True(RibbonPopupInteraction.GetIsHovered(surface));
        copy.IsDropDownOpen = true; host.Layout(); host.Window.Close(); host.Layout();
        Assert.False(RibbonPopupInteraction.GetSuppressHover(ribbon));
    });

    [Fact]
    public void Borrowed_tiles_remain_hoverable_while_gallery_and_collapsed_group_chrome_are_paused() => Sta.Run(() =>
    {
        var tile = new RibbonGalleryItem { Content = "Tile" };
        var gallery = new InRibbonGallery { Width = 180, Items = { tile } };
        var group = new RibbonGroup { Header = "Tools", Items = { gallery, new RibbonButton { Header = "Wide", Width = 400 } } };
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow,
            Tabs = { new RibbonTab { Header = "Home", Groups = { group } } } };
        ribbon.AddToQuickAccess(gallery); ribbon.AddToQuickAccess(group); using var host = new Host(ribbon);
        var copy = (RibbonDropDownButton)ribbon.QuickAccessItems[0];
        copy.IsDropDownOpen = true; host.Layout();
        Hover(gallery, true); Assert.False(RibbonPopupInteraction.GetIsHovered(gallery));
        Hover(tile, true); Assert.True(RibbonPopupInteraction.GetIsHovered(tile));
        copy.IsDropDownOpen = false; host.Layout();
        host.Window.Width = 300; host.Layout(); Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
        copy = (RibbonDropDownButton)ribbon.QuickAccessItems[1]; copy.IsDropDownOpen = true; host.Layout();
        var collapsed = Part<ButtonBase>(group, "PART_CollapsedButton");
        Hover(collapsed, true); Assert.False(RibbonPopupInteraction.GetIsHovered(collapsed));
        Hover(tile, true); Assert.True(RibbonPopupInteraction.GetIsHovered(tile));
    });

    [Fact]
    public void Group_caption_resolves_to_the_group_without_hijacking_commands_or_custom_content() => Sta.Run(() =>
    {
        var command = new RibbonButton { Header = "Run" };
        var editor = new TextBox();
        var group = new RibbonGroup { Header = "Tools", Items = { command, editor } };
        var ribbon = new Ribbon { Tabs = { new RibbonTab { Header = "Home", Groups = { group } } } };
        using var host = new Host(ribbon);
        var resolve = typeof(Ribbon).GetMethod("ResolveCommandControl", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.Same(group, resolve.Invoke(ribbon, new[] { Part<FrameworkElement>(group, "GroupCaption") }));
        Assert.Same(group, resolve.Invoke(ribbon, new[] { Part<FrameworkElement>(group, "PART_CaptionHost") }));
        Assert.Same(command, resolve.Invoke(ribbon, new[] { command }));
        Assert.Null(resolve.Invoke(ribbon, new[] { editor }));
        Assert.True(ribbon.AddToQuickAccess(group)); Assert.False(ribbon.AddToQuickAccess(group));
    });

    private static void Hover(UIElement element, bool value)
    {
        // Emulate native reverse-inherited hover without moving the user's cursor.
        var key = (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        element.SetValue(key, value);
        element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
            { RoutedEvent = value ? Mouse.MouseEnterEvent : Mouse.MouseLeaveEvent });
    }
    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private sealed class Host : IDisposable
    {
        internal Window Window { get; }
        private readonly RibbonAnimationLevel _motion = RibbonAnimation.GlobalLevel;
        internal Host(Ribbon ribbon, bool ribbonWindow = false)
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Window = ribbonWindow ? new RibbonWindow() : new Window();
            Window.Content = ribbon; Window.Width = 900; Window.Height = 650;
            Window.Left = -10000; Window.Top = -10000; Window.ShowInTaskbar = false;
            Window.Show(); Window.Activate(); Layout();
        }
        internal void Layout() { Sta.Drain(DispatcherPriority.ApplicationIdle); Window.UpdateLayout(); Sta.Drain(DispatcherPriority.ApplicationIdle); }
        public void Dispose() { Mouse.Capture(null); Window.Close(); Sta.Drain(DispatcherPriority.ApplicationIdle); RibbonAnimation.GlobalLevel = _motion; Sta.ResetApplication(); }
    }
}
