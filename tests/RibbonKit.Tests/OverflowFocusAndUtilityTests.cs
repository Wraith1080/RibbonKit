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
