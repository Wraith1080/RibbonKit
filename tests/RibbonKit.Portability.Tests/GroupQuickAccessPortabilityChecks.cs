using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Public controls/resources only; no Showcase assembly or internal RibbonKit APIs.
internal static class GroupQuickAccessPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            if (Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") != "GroupQuickAccessKeyboard") VerifyMatrix(application);
            VerifyKeyboardAndOverflow(application);
        }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void VerifyMatrix(Application application)
    {
        var button = new RibbonButton { Header = "Run", ScreenTipTitle = "Run now" };
        var combo = new RibbonComboBox { Header = "Choice", Width = 120, ItemsSource = new[] { "A", "B" }, SelectedIndex = 0 };
        var gallery = new InRibbonGallery { Header = "Styles", Width = 180, PopupWidth = 340 };
        gallery.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 64, Height = 40, Background = Brushes.Red } });
        gallery.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 64, Height = 40, Background = Brushes.Blue } });
        gallery.SelectedIndex = 0;
        var group = new RibbonGroup { Header = "Tools", Icon = Icon(), ShowDialogLauncher = true };
        group.Items.Add(button); group.Items.Add(combo); group.Items.Add(gallery); Ribbon.SetCommandId(group, "tools");
        var ribbon = RibbonFor(group); ribbon.Resources["ConsumerPaint"] = Brushes.SeaGreen;
        button.SetResourceReference(Control.ForegroundProperty, "ConsumerPaint");
        var command = new RoutedCommand(); button.Command = command;
        int commands = 0; ribbon.CommandBindings.Add(new CommandBinding(command, (_, _) => commands++, (_, e) => e.CanExecute = true));
        var qatPage = new RibbonQuickAccessPage { Ribbon = ribbon }; var customizePage = new RibbonCustomizePage { Ribbon = ribbon };
        var root = new StackPanel { Children = { ribbon, qatPage, customizePage } };
        var window = new RibbonWindow { Content = root, Width = 1100, Height = 720, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        try
        {
            window.Show(); Layout(window); Assert.False(group.IsLoaded);
            var available = Part<ListBox>(qatPage, "PART_AvailableList");
            available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(e => ReferenceEquals(e.Control, group));
            Assert.DoesNotContain(Part<ListBox>(customizePage, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), e => e.Control is RibbonGroup);
            Part<ButtonBase>(qatPage, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            var copy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            Assert.False(ribbon.AddToQuickAccess(group));
            int cases = 0;
            bool previewOnly = Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") == "GroupQuickAccessPreview";
            foreach (var theme in Enum.GetValues<RibbonTheme>())
            foreach (bool dark in new[] { false, true })
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (var flow in Enum.GetValues<FlowDirection>())
            foreach (double scale in new[] { 1.25, 2d })
            foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                if (previewOnly && (dark || flow != FlowDirection.LeftToRight || scale != 1.25
                    || placement != RibbonQuickAccessPosition.TabRow || theme is not (RibbonTheme.Office2024 or RibbonTheme.CrystalLight))) continue;
                ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, dark);
                ribbon.Density = density; ribbon.FlowDirection = flow; ribbon.QuickAccessPosition = placement;
                VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); Layout(window);
                copy.IsDropDownOpen = true; Layout(window);
                Assert.True(copy.IsDropDownOpen, $"{theme}/{density}/{flow}/{placement}");
                Assert.True(Part<Popup>(copy, "PART_Popup").IsOpen);
                var host = Assert.IsAssignableFrom<Border>(Assert.Single(copy.Items)); var content = host.Child; Assert.NotNull(content);
                Assert.Same(group, button.Parent); Assert.Same(Brushes.SeaGreen, button.Foreground);
                Assert.True(button.IsEnabled, $"The routed command must resolve the owning ribbon's CommandBinding. group={command.CanExecute(null, group)}, button={command.CanExecute(null, button)}, parent={group.Parent}, tabParent={ribbon.Tabs[1].Parent}, visible={button.IsVisible}");
                Assert.Same(group.Icon, copy.Icon); Assert.Equal("Tools", copy.ScreenTipTitle);
                Assert.All(new FrameworkElement[] { button, combo, gallery }, item =>
                {
                    Assert.True(item.ActualWidth > 0); Assert.Equal(density, Ribbon.GetDensity(item)); Assert.Equal(flow, item.FlowDirection);
                    var bounds = item.TransformToVisual(host).TransformBounds(new Rect(item.RenderSize));
                    Assert.True(bounds.Left >= -1 && bounds.Right <= host.ActualWidth + 1 && bounds.Top >= -1 && bounds.Bottom <= host.ActualHeight + 1, $"{theme}/{density}/{flow}: {bounds} in {host.RenderSize}");
                });
                var provider = Assert.IsAssignableFrom<IExpandCollapseProvider>(UIElementAutomationPeer.CreatePeerForElement(copy).GetPattern(PatternInterface.ExpandCollapse));
                Assert.Equal(System.Windows.Automation.ExpandCollapseState.Expanded, provider.ExpandCollapseState);
                var peer = UIElementAutomationPeer.CreatePeerForElement(host); Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                Assert.Contains(AutomationChildren(peer), child => child.GetName() == "Run");
                int previews = 0; RibbonGalleryPreviewEventHandler preview = (_, _) => previews++; gallery.ItemPreview += preview;
                ((RibbonGalleryItem)gallery.Items[1]).RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
                Assert.Equal(1, previews); gallery.ItemPreview -= preview;
                combo.SelectedIndex = 1; gallery.SelectedIndex = 1;
                if (!dark && scale == 1.25 && flow == FlowDirection.LeftToRight && placement == RibbonQuickAccessPosition.TabRow
                    && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight) Save(Part<Popup>(copy, "PART_Popup").Child as FrameworkElement, $"{theme}-{density}");
                copy.IsDropDownOpen = false; Layout(window);
                Assert.Null(host.Child); Assert.Same(content, Part<Decorator>(group, "PART_NormalHost").Child);
                Assert.Equal(1, combo.SelectedIndex); Assert.Equal(1, gallery.SelectedIndex); cases++;
            }
            Assert.Equal(previewOnly ? 4 : 288, cases);
            // Public persistence resolves a new group's source, retaining default command copies.
            string saved = RibbonCustomizationSerializer.Serialize(ribbon); ribbon.QuickAccessItems.Clear();
            RibbonCustomizationSerializer.Apply(ribbon, saved); Assert.True(ribbon.IsInQuickAccess(group));
            Assert.Equal(0, commands);
        }
        finally { window.Close(); Layout(window); }
    }

    private static void VerifyKeyboardAndOverflow(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
        var disabled = new RibbonButton { Header = "Disabled", IsEnabled = false };
        var run = new RibbonButton { Header = "Run" }; var menu = new RibbonDropDownButton { Header = "Nested" }; menu.Items.Add(new RibbonMenuItem { Header = "Action" });
        var group = new RibbonGroup { Header = "Tools", Icon = Icon() }; group.Items.Add(disabled); group.Items.Add(run); group.Items.Add(menu);
        var ribbon = RibbonFor(group); ribbon.QuickAccessMaxWidth = 50;
        for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Icon = Icon(), Size = RibbonControlSize.Small });
        ribbon.AddToQuickAccess(group);
        int invoked = 0; run.Click += (_, _) => invoked++;
        var window = new Window { Content = ribbon, Width = 900, Height = 600, Left = -10000, Top = -10000, ShowActivated = true, ShowInTaskbar = false };
        try
        {
            window.Show(); window.Activate(); Layout(window);
            var toolbar = Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost");
            var overflow = Part<ToggleButton>(toolbar, "PART_OverflowButton"); overflow.IsChecked = true; Layout(window);
            var entry = Assert.Single(Part<ItemsControl>(toolbar, "PART_OverflowHost").Items.OfType<RibbonDropDownButton>());
            var opener = Part<ToggleButton>(entry, "PART_Toggle"); Assert.True(opener.Focus());
            Press(Key.Down); Layout(window); Assert.True(entry.IsDropDownOpen); Assert.Same(run, Keyboard.FocusedElement);
            Press(Key.End); Layout(window); Assert.Same(Part<ToggleButton>(menu, "PART_Toggle"), Keyboard.FocusedElement);
            Press(Key.Down); Layout(window); Assert.True(menu.IsDropDownOpen); Assert.True(entry.IsDropDownOpen);
            Press(Key.Escape); Layout(window); Assert.False(menu.IsDropDownOpen); Assert.True(entry.IsDropDownOpen);
            Press(Key.Escape); Layout(window); Assert.False(entry.IsDropDownOpen); Assert.True(overflow.IsChecked); Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Down); Layout(window); Press(Key.Enter); Layout(window); Assert.Equal(1, invoked); Assert.False(entry.IsDropDownOpen);
            Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Up); Layout(window); Assert.True(entry.IsDropDownOpen); Assert.Same(Part<ToggleButton>(menu, "PART_Toggle"), Keyboard.FocusedElement);
            overflow.IsChecked = false; Layout(window); Assert.False(entry.IsDropDownOpen);
            overflow.IsChecked = true; Layout(window); entry.IsDropDownOpen = true; Layout(window);
            ribbon.QuickAccessItems.RemoveAt(ribbon.QuickAccessItems.Count - 1); Layout(window); Assert.False(entry.IsDropDownOpen);
            Assert.NotNull(Part<Decorator>(group, "PART_NormalHost").Child);
        }
        finally { window.Close(); Layout(window); }
    }

    private static Ribbon RibbonFor(RibbonGroup group)
    {
        var ribbon = new Ribbon(); var home = new RibbonTab { Header = "Home" }; home.Groups.Add(new RibbonGroup());
        var other = new RibbonTab { Header = "Other" }; other.Groups.Add(group); Ribbon.SetCommandId(other, "other");
        ribbon.Tabs.Add(home); ribbon.Tabs.Add(other); ribbon.SelectedTab = home; return ribbon;
    }
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.SteelBlue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static void Layout(Window window) { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
    private static void Press(Key key)
    {
        var target = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement); var source = PresentationSource.FromVisual(target)!;
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyUpEvent });
    }
    private static IEnumerable<AutomationPeer> AutomationChildren(AutomationPeer parent)
    {
        foreach (var child in parent.GetChildren() ?? new()) { yield return child; foreach (var descendant in AutomationChildren(child)) yield return descendant; }
    }
    private static void Save(FrameworkElement? surface, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GROUP_QAT_DIAGNOSTICS"); if (string.IsNullOrEmpty(directory) || surface is null) return;
        Directory.CreateDirectory(directory);
        var root = Assert.IsAssignableFrom<UIElement>(PresentationSource.FromVisual(surface)!.RootVisual);
        var dpi = VisualTreeHelper.GetDpi(root);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.RenderSize.Width * dpi.DpiScaleX), (int)Math.Ceiling(root.RenderSize.Height * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32); bitmap.Render(root);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
