using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class OverflowFocusAndUtilityTests
{
    public static IEnumerable<object[]> OverflowOpeners()
    {
        foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2010,
            RibbonTheme.Office2019, RibbonTheme.Office2024, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (bool simplified in new[] { false, true })
                yield return new object[] { theme, simplified };
    }

    [Theory]
    [MemberData(nameof(OverflowOpeners))]
    public void Overflow_is_checked_and_painted_before_native_popup_creation(RibbonTheme theme, bool simplified) => Sta.Run(() =>
    {
        using var host = new Host(FlowDirection.LeftToRight);
        ThemeManager.Apply(host.Application, theme);
        host.Ribbon.LayoutMode = simplified ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
        host.Layout();
        var toolbar = simplified ? ((RibbonGroupsHost)host.Tab.Content).SimplifiedToolBar!
            : Descendants(host.Window).OfType<RibbonQuickAccessToolBar>().First(bar => !bar.IsSimplified && bar.IsVisible);
        toolbar.Width = 1; host.Layout();
        var opener = toolbar.OverflowButton!;
        var chrome = (Border)opener.Template.FindName("Chrome", opener);
        var popup = (Popup)toolbar.Template.FindName("PART_OverflowPopup", toolbar);
        var paint = new List<(bool? Checked, Brush Background, Brush Border)>();
        popup.Placement = PlacementMode.Custom;
        popup.CustomPopupPlacementCallback = (_, target, _) =>
        {
            // Observe native creation, before Opened can hide an in-progress
            // toggle transfer. Assert afterwards: bindings can swallow exceptions.
            if (popup.IsOpen) paint.Add((opener.IsChecked, chrome.Background, chrome.BorderBrush));
            return new[] { new CustomPopupPlacement(new Point(0, target.Height), PopupPrimaryAxis.None) };
        };
        foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            host.Window.FlowDirection = flow; host.Layout();
            // Invoke without IsPressed, matching the release-click transition
            // that exposed the dropdown owner's reentrant checked-state gap.
            Click(); host.Layout();
            Assert.True(popup.IsOpen); Assert.True(opener.IsChecked); Assert.NotEmpty(paint);
            bool colored = Ribbon.GetQatOnColoredSurface(opener);
            var background = opener.FindResource(simplified ? "RibbonKit.Brushes.Control.CheckedBackground"
                : colored ? Ribbon.QatColoredPressedBackgroundKey : "RibbonKit.Brushes.TabStrip.ControlHoverBackground");
            foreach (var sample in paint)
            {
                Assert.True(sample.Checked);
                Assert.Same(background, sample.Background);
                if (simplified)
                    Assert.Same(opener.FindResource("RibbonKit.Brushes.Control.CheckedBorder"), sample.Border);
            }
            paint.Clear();
            Click(); host.Layout();
            Assert.False(popup.IsOpen); Assert.False(opener.IsChecked);
        }

        void Click() => typeof(ToggleButton).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(opener, null);
    });

    [Theory]
    [InlineData(false, FlowDirection.LeftToRight)]
    [InlineData(false, FlowDirection.RightToLeft)]
    [InlineData(true, FlowDirection.LeftToRight)]
    [InlineData(true, FlowDirection.RightToLeft)]
    public void Opening_overflow_by_click_enter_space_or_arrows_focuses_an_enabled_entry(bool simplified, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(flow);
        host.Window.Activate(); host.Layout();
        host.Ribbon.LayoutMode = simplified ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic; host.Layout();
        var toolbar = simplified ? ((RibbonGroupsHost)host.Tab.Content).SimplifiedToolBar!
            : Descendants(host.Window).OfType<RibbonQuickAccessToolBar>().First(bar => !bar.IsSimplified && bar.IsVisible);
        toolbar.Width = 1; host.Layout();
        var items = toolbar.Items.OfType<RibbonButton>().ToArray();
        items[0].IsEnabled = false; host.Layout();
        var opener = toolbar.OverflowButton!;
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            host.Ribbon.Density = density; host.Layout();
            foreach (var key in new[] { Key.None, Key.Enter, Key.Space, Key.Down, Key.Up })
            {
                Assert.True(opener.Focus());
                if (key == Key.None) typeof(ToggleButton).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(opener, null);
                else Send(opener, key);
                host.Layout(); Assert.True(opener.IsChecked);
                var candidates = toolbar.OverflowEntries.Where(entry => entry.IsEnabled && entry.IsVisible).ToArray();
                var expected = key == Key.Up ? candidates.Last() : candidates.First();
                Assert.True(expected.IsKeyboardFocusWithin, $"{density}/{key}: expected focus within {expected}, actual={Keyboard.FocusedElement}, open={opener.IsChecked}");
                Assert.True(((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(key == Key.Up ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next)));
                Assert.NotSame(opener, Keyboard.FocusedElement);
                Send((UIElement)Keyboard.FocusedElement, Key.Escape); host.Layout();
                Assert.False(opener.IsChecked); Assert.Same(opener, Keyboard.FocusedElement);
            }
        }
        // Opening then dismissing before realization must not steal focus afterward.
        var outside = new Button { Content = "After the ribbon" }; host.Panel.Children.Add(outside); host.Layout();
        toolbar.OpenOverflow(); toolbar.CloseOverflow();
        Assert.True(outside.Focus()); host.Layout(); Assert.Same(outside, Keyboard.FocusedElement);
    });

    [Theory]
    [InlineData(RibbonQuickAccessPosition.TitleBar, FlowDirection.LeftToRight)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, FlowDirection.RightToLeft)]
    [InlineData(RibbonQuickAccessPosition.TabRow, FlowDirection.LeftToRight)]
    [InlineData(RibbonQuickAccessPosition.TabRow, FlowDirection.RightToLeft)]
    public void Qat_overflow_is_a_focus_stop_after_visible_commands_and_opens_with_arrows(RibbonQuickAccessPosition position, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(flow);
        host.Ribbon.QuickAccessPosition = position;
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            host.Ribbon.Density = density; host.Layout();
            var toolbar = Descendants(host.Window).OfType<RibbonQuickAccessToolBar>().First(bar => !bar.IsSimplified && bar.IsVisible);
            toolbar.Width = 115; host.Layout();
            var visible = toolbar.Items.OfType<RibbonButton>().Where(item => !toolbar.IsOverflowed(item)).ToArray();
            Assert.NotEmpty(visible); Assert.True(toolbar.HasOverflow);
            var opener = toolbar.OverflowButton!;
            Assert.True(visible[0].Focus());
            foreach (var next in visible.Skip(1)) Move(FocusNavigationDirection.Next, next);
            Move(FocusNavigationDirection.Next, opener);
            Move(FocusNavigationDirection.Previous, visible.Last());
            Assert.True(opener.Focus()); Send(opener, Key.Down); host.Layout();
            Assert.True(opener.IsChecked);
            Assert.Same(toolbar.OverflowEntries.First(), Keyboard.FocusedElement);
            Send((UIElement)Keyboard.FocusedElement, Key.Escape); host.Layout();
            Assert.False(opener.IsChecked); Assert.Same(opener, Keyboard.FocusedElement);
        }
    });

    [Theory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Simplified_tab_order_enters_the_first_command_and_reaches_more_last(FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(flow);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        var toolbar = ((RibbonGroupsHost)host.Tab.Content).SimplifiedToolBar!;
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        {
            host.Ribbon.Density = density; host.Layout();
            var commands = toolbar.Items.OfType<RibbonButton>().Where(item => !toolbar.IsOverflowed(item)).ToArray();
            Assert.True(toolbar.MoveFocus(new TraversalRequest(FocusNavigationDirection.First)));
            Assert.Same(commands.First(), Keyboard.FocusedElement);
            foreach (var next in commands.Skip(1)) Move(FocusNavigationDirection.Next, next);
            Move(FocusNavigationDirection.Next, toolbar.OverflowButton!);
            foreach (var previous in commands.AsEnumerable().Reverse()) Move(FocusNavigationDirection.Previous, previous);
        }
    });

    [Theory]
    [InlineData(false, FlowDirection.LeftToRight)]
    [InlineData(false, FlowDirection.RightToLeft)]
    [InlineData(true, FlowDirection.LeftToRight)]
    [InlineData(true, FlowDirection.RightToLeft)]
    public void Flat_pressed_and_checked_utility_rims_are_visible_and_restore_other_themes(bool dark, FlowDirection flow) => Sta.Run(() =>
    {
        using var host = new Host(flow);
        ThemeManager.Apply(host.Application, RibbonTheme.Office2024Flat); ThemeManager.SetDarkMode(host.Application, dark); host.Layout();
        var toolbar = Descendants(host.Window).OfType<RibbonQuickAccessToolBar>().First(bar => !bar.IsSimplified && bar.IsVisible);
        toolbar.Width = 115; host.Layout();
        var tabs = (RibbonTabControl)host.Ribbon.Template.FindName("TabControlHost", host.Ribbon);
        var collapse = (ToggleButton)tabs.Template.FindName("MinimizeToggle", tabs);
        foreach (var toggle in new[] { toolbar.OverflowButton!, collapse })
        {
            var rim = (Border)toggle.Template.FindName("CrystalUtilityRim", toggle);
            Assert.Equal(0, rim.Opacity);
            toggle.IsChecked = true; host.Layout();
            Assert.Equal(1, rim.Opacity); Assert.Equal(new Thickness(1), rim.BorderThickness);
            Assert.Same(toggle.FindResource("RibbonKit.Brushes.Utility.CheckedBorder"), rim.BorderBrush);
            SetPressed(toggle, true); host.Layout();
            Assert.Same(toggle.FindResource("RibbonKit.Brushes.Control.PressedBorder"), rim.BorderBrush);
            SetPressed(toggle, false); toggle.IsChecked = false; host.Layout();
        }
        var dictionary = new ResourceDictionary { Source = new Uri("/RibbonKit;component/Themes/Controls.Shared.xaml", UriKind.Relative) };
        var utility = new RepeatButton { Style = (Style)dictionary["RibbonKit.ScrollLeftButton"] };
        host.Panel.Children.Add(utility); host.Layout();
        SetPressed(utility, true); host.Layout();
        var utilityRim = (Border)utility.Template.FindName("CrystalUtilityRim", utility);
        Assert.Equal(1, utilityRim.Opacity);
        Assert.Same(utility.FindResource("RibbonKit.Brushes.Control.PressedBorder"), utilityRim.BorderBrush);
        toolbar.OpenOverflow(); host.Layout();
        Save(host.Window, "flat-utilities-" + dark + "-" + flow);
        toolbar.CloseOverflow(); SetPressed(utility, false);
        ThemeManager.Apply(host.Application, RibbonTheme.Office2024); host.Layout();
        SetPressed(utility, true); Assert.Equal(0, utilityRim.Opacity); SetPressed(utility, false);
        ThemeManager.Apply(host.Application, RibbonTheme.CrystalLight); host.Layout();
        collapse.IsChecked = true; host.Layout();
        var crystalRim = (Border)collapse.Template.FindName("CrystalUtilityRim", collapse);
        Assert.Equal(1, crystalRim.Opacity); Assert.IsType<DrawingBrush>(crystalRim.BorderBrush);
        collapse.IsChecked = false;
    });

    private static void SetPressed(ButtonBase button, bool value) => button.SetValue(
        (DependencyPropertyKey)typeof(ButtonBase).GetField("IsPressedPropertyKey", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!, value);

    private static void Move(FocusNavigationDirection direction, IInputElement expected)
    {
        Assert.True(((UIElement)Keyboard.FocusedElement).MoveFocus(new TraversalRequest(direction))); Sta.Drain();
        Assert.Same(expected, Keyboard.FocusedElement);
    }

    private static void Send(UIElement target, Key key)
    {
        var source = PresentationSource.FromVisual(target)!;
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyUpEvent });
        Sta.Drain();
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i); yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Save(FrameworkElement element, string name)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "simplified-customization"); Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }

    private sealed class Host : IDisposable
    {
        internal readonly Ribbon Ribbon = new() { QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        internal readonly Application Application;
        internal readonly RibbonTab Tab = new() { Header = "Home" };
        internal readonly StackPanel Panel = new();
        internal readonly RibbonWindow Window;
        internal Host(FlowDirection flow)
        {
            Application = Sta.UseApplication();
            ThemeManager.Apply(Application, RibbonTheme.Office2024); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            var group = new RibbonGroup { Header = "Commands" };
            for (int index = 0; index < 4; index++)
            {
                var button = new RibbonButton { Header = "Command " + index };
                Ribbon.SetSimplifiedPlacement(button, RibbonSimplifiedPlacement.Inline); group.Items.Add(button);
                Ribbon.QuickAccessItems.Add(new RibbonButton { Header = "QAT " + index, Width = 45, Size = RibbonControlSize.Small });
            }
            Tab.Groups.Add(group); Ribbon.Tabs.Add(Tab); Panel.Children.Add(Ribbon);
            Window = new RibbonWindow { Content = Panel, Width = 850, Height = 450, Left = -10000, Top = -10000, FlowDirection = flow, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
            foreach (FrameworkElement item in Ribbon.QuickAccessItems) item.Width = 45;
            Layout();
        }
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); Window.UpdateLayout(); }
        public void Dispose() { Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; Sta.ResetApplication(); }
    }
}
