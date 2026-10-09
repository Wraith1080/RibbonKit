using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Layout;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ComboBoxKeyTipTests
{
    [Theory]
    [InlineData("QAT", FlowDirection.LeftToRight)]
    [InlineData("QAT", FlowDirection.RightToLeft)]
    [InlineData("Overflow", FlowDirection.LeftToRight)]
    [InlineData("Overflow", FlowDirection.RightToLeft)]
    [InlineData("Custom", FlowDirection.LeftToRight)]
    [InlineData("Custom", FlowDirection.RightToLeft)]
    [InlineData("Collapsed", FlowDirection.LeftToRight)]
    [InlineData("Collapsed", FlowDirection.RightToLeft)]
    public void Copy_KeyTip_opens_choices_without_badges_and_hands_off_keyboard(string placement, FlowDirection flow) => Sta.Run(() =>
    {
        var application = Sta.UseApplication();
        var animation = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var ribbon = new Ribbon { FlowDirection = flow, QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        var combo = new RibbonComboBox { Header = "Font", SelectedIndex = 1 };
        combo.Items.Add("Calibri"); combo.Items.Add("Times New Roman"); combo.Items.Add("Georgia");
        var sourceTab = new RibbonTab { Header = "Source" };
        var sourceGroup = new RibbonGroup { Header = "Font" }; sourceGroup.Items.Add(combo);
        sourceTab.Groups.Add(sourceGroup); ribbon.Tabs.Add(sourceTab);
        var customTab = new RibbonTab { Header = "Custom" };
        var customGroup = new RibbonGroup { Header = "Commands", CanResize = false };
        customTab.Groups.Add(customGroup); ribbon.Tabs.Add(customTab); ribbon.SelectedTab = customTab;
        KeyTip.SetKeys(customTab, "C"); KeyTip.SetKeys(customGroup, "G");
        if (placement == "Collapsed")
        {
            customGroup.CanResize = true; customGroup.ReductionMode = RibbonGroupReductionMode.Collapse;
            customGroup.Items.Add(new Border { Width = 300, Height = 30 });
            customTab.Groups.Add(new RibbonGroup { Header = "Fixed commands", CanResize = false, Width = 900 });
        }
        RibbonDropDownButton copy;
        if (placement is "Custom" or "Collapsed")
        {
            copy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.CreateCommandProxy(combo, RibbonControlSize.Medium));
            customGroup.Items.Add(copy);
        }
        else
        {
            if (placement == "Overflow")
            {
                ribbon.QuickAccessMaxWidth = 55;
                for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small, Icon = Icon() });
            }
            Assert.True(ribbon.AddToQuickAccess(combo));
            copy = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[^1]);
        }
        KeyTip.SetKeys(copy, "F");
        var window = new Window { Content = ribbon, Width = 900, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); window.Activate(); Layout();
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var toolbar = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
            if (placement == "Collapsed") Assert.Equal(RibbonGroupSizeState.Collapsed, customGroup.SizeState);
            if (placement == "Overflow") KeyTip.SetKeys(Part<ToggleButton>(toolbar, "PART_OverflowButton"), "O");
            KeyTipPress(Key.F10);
            Assert.True(IsKeyTipActive(ribbon));
            if (placement == "Overflow")
            {
                Assert.True(toolbar.HasOverflow);
                KeyTipPress(Key.O);
                var overflow = Part<ItemsControl>(toolbar, "PART_OverflowHost");
                copy = Assert.Single(overflow.Items.OfType<RibbonDropDownButton>());
            }
            else if (placement is "Custom" or "Collapsed")
            {
                KeyTipPress(Key.C);
                if (placement == "Collapsed") KeyTipPress(Key.G);
            }
            Assert.Contains(AdornerLayer.GetAdornerLayer(copy)?.GetAdorners(copy) ?? Array.Empty<Adorner>(), a => a is KeyTipAdorner);
            KeyTipPress(Key.F);
            Assert.True(copy.IsDropDownOpen);
            Assert.False(IsKeyTipActive(ribbon));
            var rows = copy.Items.OfType<Button>().ToArray();
            Assert.Equal(3, rows.Length);
            Assert.Same(rows[1], Keyboard.FocusedElement);
            Assert.Equal(1, combo.SelectedIndex);
            foreach (var row in rows)
                Assert.DoesNotContain(AdornerLayer.GetAdornerLayer(row)?.GetAdorners(row) ?? Array.Empty<Adorner>(), a => a is KeyTipAdorner);
            if (placement == "Overflow") Assert.True(Part<Popup>(toolbar, "PART_OverflowPopup").IsOpen);
            if (placement == "Collapsed") Assert.True(Part<Popup>(customGroup, "PART_Popup").IsOpen);
            Press(Key.Down); Layout(); Assert.Same(rows[2], Keyboard.FocusedElement);
            Press(Key.Escape); Layout(); Assert.False(copy.IsDropDownOpen); Assert.Equal(1, combo.SelectedIndex);
            Assert.Same(Part<ToggleButton>(copy, "PART_Toggle"), Keyboard.FocusedElement);
            if (placement == "Collapsed")
            {
                // The group owns arrow navigation on its opener buttons.
                KeyTipPress(Key.F10); KeyTipPress(Key.C); KeyTipPress(Key.G); KeyTipPress(Key.F);
            }
            else { Press(Key.Down); Layout(); }
            Assert.True(copy.IsDropDownOpen); Assert.False(IsKeyTipActive(ribbon));
            Press(Key.Down); Layout(); Assert.Same(copy.Items[2], Keyboard.FocusedElement);
            Press(Key.Enter); Layout(); Assert.Equal(2, combo.SelectedIndex); Assert.False(copy.IsDropDownOpen);
        }
        finally { window.Close(); Sta.Drain(); Sta.ResetApplication(); RibbonAnimation.GlobalLevel = animation; }

        void Layout() { Sta.Drain(DispatcherPriority.ApplicationIdle); window.UpdateLayout(); Sta.Drain(DispatcherPriority.ApplicationIdle); }
        void KeyTipPress(Key key)
        {
            window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, Environment.TickCount, key)
                { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Layout();
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ordinary_dropdown_and_split_menu_choices_keep_their_KeyTips(bool split) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(); ThemeManager.Apply(application, RibbonTheme.Office2024);
        var animation = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var ribbon = new Ribbon(); var tab = new RibbonTab { Header = "Home" }; KeyTip.SetKeys(tab, "H");
        var group = new RibbonGroup { Header = "Commands", CanResize = false }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.SelectedTab = tab;
        RibbonDropDownButton menu = split ? new RibbonSplitButton { Header = "Paste" } : new RibbonDropDownButton { Header = "Paste" };
        KeyTip.SetKeys(menu, split ? "X" : "P");
        var choice = new RibbonMenuItem { Header = "Special" }; KeyTip.SetKeys(choice, "S"); menu.Items.Add(choice); group.Items.Add(menu);
        int clicks = 0; choice.Click += (_, _) => clicks++;
        var window = new Window { Content = ribbon, Width = 900, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); window.Activate(); Sta.Drain(); window.UpdateLayout(); Sta.Drain();
            KeyTipPress(Key.F10); KeyTipPress(Key.H); KeyTipPress(Key.P);
            Assert.True(menu.IsDropDownOpen); Assert.True(IsKeyTipActive(ribbon));
            Assert.Contains(AdornerLayer.GetAdornerLayer(choice)?.GetAdorners(choice) ?? Array.Empty<Adorner>(), a => a is KeyTipAdorner);
            KeyTipPress(Key.S); Assert.Equal(1, clicks); Assert.False(menu.IsDropDownOpen); Assert.False(IsKeyTipActive(ribbon));
        }
        finally { window.Close(); Sta.Drain(); Sta.ResetApplication(); RibbonAnimation.GlobalLevel = animation; }
        void KeyTipPress(Key key)
        {
            window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, Environment.TickCount, key)
                { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Sta.Drain(); window.UpdateLayout(); Sta.Drain();
        }
    });

    private static bool IsKeyTipActive(Ribbon ribbon)
    {
        var service = typeof(Ribbon).GetField("_keyTipService", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ribbon)!;
        return (bool)service.GetType().GetField("_active", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;
    }
    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
    private static void Press(Key key)
    {
        var target = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement);
        var source = PresentationSource.FromVisual(target)!;
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyUpEvent });
        Sta.Drain();
    }
}
