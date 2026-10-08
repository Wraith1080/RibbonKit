using System.Collections.ObjectModel;
using System.ComponentModel;
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

// Uses only public RibbonKit APIs/resources, including the real customization pages.
internal static class ComboBoxQuickAccessPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            if (Environment.GetEnvironmentVariable("RIBBONKIT_PORTABILITY_SCOPE") != "ComboQuickAccessKeyboard")
                VerifyPlacements(application);
            VerifyOverflowAndKeyboard(application);
        }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void VerifyPlacements(Application application)
    {
        var model = new Model();
        var combo = new RibbonComboBox { Header = "Font size", Icon = Icon(), IsEditable = true,
            DisplayMemberPath = nameof(Choice.Name), MaxDropDownHeight = 160, DataContext = model };
        combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(model.Choices)));
        combo.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(model.Selected)) { Mode = BindingMode.TwoWay });
        var ribbon = RibbonFor(combo);
        var customGroup = new RibbonGroup { Header = "My commands" };
        Ribbon.SetIsCustom(customGroup, true); ribbon.Tabs[0].Groups.Add(customGroup);
        var qatPage = new RibbonQuickAccessPage { Ribbon = ribbon };
        var customizePage = new RibbonCustomizePage { Ribbon = ribbon };
        var root = new StackPanel(); root.Children.Add(ribbon); root.Children.Add(qatPage); root.Children.Add(customizePage);
        var window = Window(root);
        try
        {
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            window.Show(); Layout(window);
            Assert.False(combo.IsLoaded); // Source tab is deliberately inactive.
            foreach (var page in new Control[] { qatPage, customizePage })
                Assert.Contains(Part<ListBox>(page, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), entry => ReferenceEquals(entry.Control, combo) && ReferenceEquals(entry.Icon, combo.Icon));
            var available = Part<ListBox>(qatPage, "PART_AvailableList");
            available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(entry => ReferenceEquals(entry.Control, combo));
            Part<ButtonBase>(qatPage, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Layout(window);
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            Assert.Same(combo, Ribbon.GetQuickAccessSource(proxy));

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            {
                ThemeManager.Apply(application, theme);
                foreach (bool dark in new[] { false, true })
                {
                    ThemeManager.SetDarkMode(application, dark);
                    foreach (RibbonDensity density in Enum.GetValues<RibbonDensity>())
                    foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
                    foreach (double scale in new[] { 1.25, 2d })
                    foreach (RibbonQuickAccessPosition placement in Enum.GetValues<RibbonQuickAccessPosition>())
                    {
                        ribbon.Density = density; ribbon.FlowDirection = flow; ribbon.QuickAccessPosition = placement;
                        VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale));
                        Layout(window);
                        proxy.IsDropDownOpen = true; Layout(window);
                        Assert.Equal("Font size", proxy.DropDownHeader); Assert.Same(combo.Icon, proxy.Icon);
                        var popup = Part<Popup>(proxy, "PART_Popup"); Assert.True(popup.IsOpen);
                        var popupCard = Part<FrameworkElement>(proxy, "PART_MenuHost");
                        var cardClip = VisualTreeHelper.GetClip(popupCard);
                        Assert.True(cardClip is null || cardClip.Bounds.Height >= popupCard.ActualHeight - 1,
                            $"{theme}/{placement}/{density}: card height {popupCard.ActualHeight}, clip {cardClip?.Bounds}; {DescribeLayout(popupCard)}.");
                        var viewer = Part<ScrollViewer>(proxy, "PART_PopupScrollViewer");
                        Assert.True(viewer.ViewportHeight > 0 && viewer.ViewportHeight <= 161);
                        var rows = proxy.Items.OfType<Button>().ToArray(); Assert.Equal(2, rows.Length);
                        Assert.True(flow == rows[0].FlowDirection,
                            $"{theme}/{placement}/{density}: ribbon={ribbon.FlowDirection}, proxy={proxy.FlowDirection}, row={rows[0].FlowDirection}.");
                        Assert.Equal(density, Ribbon.GetDensity(rows[0]));
                        var chrome = Part<Border>(rows[0], "Chrome");
                        Assert.Same(rows[0].FindResource("RibbonKit.Brushes.Control.CheckedBackground"), chrome.Background);
                        Assert.Contains(Descendants<TextBlock>(rows[1]), text => text.Text == "14 pt");
                        foreach (var row in rows)
                        {
                            Assert.True(row.ActualHeight > 0, $"{theme}/{placement}/{density}: choice {row.Content} has no height.");
                            Assert.True(row.TranslatePoint(new Point(0, row.ActualHeight), viewer).Y <= viewer.ActualHeight + 1);
                        }
                        if (density == RibbonDensity.Touch)
                            Assert.True(rows[0].ActualHeight >= (double)rows[0].FindResource("RibbonKit.Metrics.Touch.TargetSize"));
                        Invoke(rows[1]); Layout(window); Assert.Same(model.Choices[1], model.Selected);
                        Assert.False(proxy.IsDropDownOpen);
                        Assert.True(BindingOperations.IsDataBound(combo, Selector.SelectedItemProperty));
                        model.Selected = model.Choices[0]; Layout(window);
                        if (!dark && flow == FlowDirection.LeftToRight && placement == RibbonQuickAccessPosition.BelowRibbon
                            && scale == 1.25 && theme is RibbonTheme.Office2024 or RibbonTheme.CrystalLight)
                        {
                            proxy.IsDropDownOpen = true; Layout(window);
                            Save(Part<FrameworkElement>(proxy, "PART_MenuHost"), $"{theme}-{density}-combo-qat");
                            proxy.IsDropDownOpen = false; Layout(window);
                        }
                    }
                }
            }
            ribbon.QuickAccessItems.Clear(); Layout(window);
            Assert.Contains(Part<ListBox>(qatPage, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), entry => ReferenceEquals(entry.Control, combo));
            var ribbonAvailable = Part<ListBox>(customizePage, "PART_AvailableList");
            ribbonAvailable.SelectedItem = ribbonAvailable.Items.Cast<RibbonCommandEntry>().Single(entry => ReferenceEquals(entry.Control, combo));
            var homeNode = Assert.IsType<RibbonCustomizeNode>(Part<TreeView>(customizePage, "PART_Tree").Items[0]);
            homeNode.Children.Single(node => node.IsCustom).IsSelected = true;
            Layout(window);
            var add = Part<ButtonBase>(customizePage, "PART_AddButton"); Assert.True(add.IsEnabled);
            add.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            var copy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(customGroup.Items));
            Assert.Same(combo, Ribbon.GetQuickAccessSource(copy));
            copy.IsDropDownOpen = true; Layout(window); Invoke(Assert.IsAssignableFrom<Button>(copy.Items[1])); Layout(window);
            Assert.Same(model.Choices[1], model.Selected);
        }
        finally { window.Close(); Layout(window); }
    }

    private static void VerifyOverflowAndKeyboard(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024); ThemeManager.SetDarkMode(application, false);
        var combo = new RibbonComboBox { Header = "Font size", Icon = Icon(), MaxDropDownHeight = 100 };
        combo.Items.Add(new ComboBoxItem { Content = "Unavailable", IsEnabled = false });
        for (int i = 0; i < 24; i++) combo.Items.Add(new ComboBoxItem { Content = $"{i + 8} pt" });
        combo.SelectedIndex = 3;
        var ribbon = RibbonFor(combo); ribbon.QuickAccessMaxWidth = 55;
        for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small, Icon = Icon() });
        ribbon.AddToQuickAccess(combo);
        var window = new Window { Content = ribbon, Width = 900, Height = 620,
            Left = -10000, Top = -10000, ShowActivated = true, ShowInTaskbar = false };
        try
        {
            window.Show(); window.Activate(); Layout(window);
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var toolbar = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
            Assert.True(toolbar.HasOverflow);
            var overflowButton = Part<ToggleButton>(toolbar, "PART_OverflowButton");
            Assert.True(Assert.IsAssignableFrom<RibbonButton>(ribbon.QuickAccessItems[0]).Focus());
            overflowButton.IsChecked = true; Layout(window);
            var overflow = Part<ItemsControl>(toolbar, "PART_OverflowHost");
            var entry = Assert.Single(overflow.Items.OfType<RibbonDropDownButton>());
            Assert.Same(combo, Ribbon.GetQuickAccessSource(entry));
            Assert.Same(ribbon.QuickAccessItems[^1], Ribbon.GetQuickAccessOverflowItem(entry));
            var opener = Part<ToggleButton>(entry, "PART_Toggle");
            Assert.True(opener.Focus(), $"visible={opener.IsVisible}, enabled={opener.IsEnabled}, focusable={opener.Focusable}, render={opener.RenderSize}, parent={entry.Parent}, popup={Part<Popup>(toolbar, "PART_OverflowPopup").IsOpen}, windowActive={window.IsActive}");
            Press(Key.Down); Layout(window); Assert.True(entry.IsDropDownOpen);
            var rows = entry.Items.OfType<Button>().ToArray();
            Assert.Same(rows[3], Keyboard.FocusedElement); // Start at the current source choice.
            Hover(rows[4]); Layout(window);
            Assert.Same(rows[4], Keyboard.FocusedElement);
            Assert.False(rows[1].IsKeyboardFocused); Assert.False(rows[3].IsKeyboardFocused);
            Assert.Equal(3, combo.SelectedIndex); Assert.True(entry.IsDropDownOpen);
            Assert.Same(rows[4].FindResource("RibbonKit.Brushes.Control.HoverBackground"), Part<Border>(rows[4], "Chrome").Background);
            Assert.Equal(Colors.Transparent, Assert.IsAssignableFrom<SolidColorBrush>(Part<Border>(rows[1], "Chrome").Background).Color);
            Hover(rows[0]); Layout(window); Assert.Same(rows[4], Keyboard.FocusedElement);
            Press(Key.Up); Layout(window); Assert.Same(rows[3], Keyboard.FocusedElement);
            Press(Key.End); Layout(window); Assert.Same(rows[^1], Keyboard.FocusedElement);
            var viewer = Part<ScrollViewer>(entry, "PART_PopupScrollViewer"); Assert.True(viewer.VerticalOffset > 0);
            Press(Key.Enter); Layout(window); Assert.Equal(24, combo.SelectedIndex); Assert.False(entry.IsDropDownOpen);
            Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Down); Layout(window); Press(Key.Escape); Layout(window);
            Assert.False(entry.IsDropDownOpen); Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Up); Layout(window); Assert.Same(entry.Items[^1], Keyboard.FocusedElement);
            Press(Key.Escape); Layout(window);
            combo.SelectedIndex = 0; // A disabled selection cannot become the active choice.
            Press(Key.Down); Layout(window); Assert.Same(entry.Items[1], Keyboard.FocusedElement);
            Press(Key.Escape); Layout(window); combo.SelectedIndex = 1;
            Press(Key.Down); Layout(window); Press(Key.Down); Layout(window); Press(Key.Space); Layout(window);
            Assert.False(entry.IsDropDownOpen); Assert.Equal(2, combo.SelectedIndex);
            Press(Key.Down); Layout(window); Assert.True(entry.IsDropDownOpen);
            Part<ToggleButton>(toolbar, "PART_OverflowButton").IsChecked = false; Layout(window);
            Assert.False(entry.IsDropDownOpen);
            Assert.Equal(25, combo.Items.Count);
            ribbon.QuickAccessMaxWidth = 600; Layout(window);
            var strip = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[^1]);
            strip.IsDropDownOpen = true; Layout(window); Assert.Equal(25, strip.Items.Count);
            ribbon.QuickAccessItems.Remove(strip); Layout(window); Assert.False(strip.IsDropDownOpen);
            Assert.True(ribbon.AddToQuickAccess(combo)); Layout(window);
            strip = Assert.IsAssignableFrom<RibbonDropDownButton>(ribbon.QuickAccessItems[^1]);
            strip.IsDropDownOpen = true; Layout(window); Invoke(Assert.IsAssignableFrom<Button>(strip.Items[3])); Layout(window);
            Assert.Equal(3, combo.SelectedIndex); Assert.Equal(25, combo.Items.Count);
        }
        finally { window.Close(); Layout(window); }
    }

    private static Ribbon RibbonFor(RibbonComboBox combo)
    {
        var ribbon = new Ribbon(); var home = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Font" }; group.Items.Add(combo);
        var other = new RibbonTab { Header = "Other" }; other.Groups.Add(group);
        home.Groups.Add(new RibbonGroup { Header = "Tools" }); ribbon.Tabs.Add(home); ribbon.Tabs.Add(other); ribbon.SelectedTab = home;
        return ribbon;
    }
    private static RibbonWindow Window(FrameworkElement content) => new()
    { Content = content, Width = 900, Height = 620, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static void Layout(Window window)
    { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
    private static string DescribeLayout(DependencyObject element)
    {
        var result = new List<string>();
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
            if (current is FrameworkElement item)
                result.Add($"{item.GetType().Name} actual={item.RenderSize} desired={item.DesiredSize} slot={LayoutInformation.GetLayoutSlot(item)} height={item.Height} max={item.MaxHeight}");
        return string.Join("; ", result);
    }
    private static void Invoke(Button button)
    { var peer = UIElementAutomationPeer.CreatePeerForElement(button)!; ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
    private static void Hover(Button button) => button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
        { RoutedEvent = Mouse.MouseEnterEvent });
    private static void Press(Key key)
    {
        var target = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement);
        var source = PresentationSource.FromVisual(target)!;
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
        { RoutedEvent = Keyboard.PreviewKeyUpEvent });
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    { for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T item) yield return item; foreach (var item2 in Descendants<T>(child)) yield return item2; } }
    private sealed record Choice(string Name);
    private sealed class Model : INotifyPropertyChanged
    {
        public ObservableCollection<Choice> Choices { get; } = new() { new("11 pt"), new("14 pt") };
        private Choice? _selected;
        internal Model() => _selected = Choices[0];
        public Choice? Selected { get => _selected; set { _selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_COMBO_QAT_DIAGNOSTICS"); if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, name + ".txt"), Descendants<Button>(element)
            .Select(row => $"{row.Content}: {row.ActualWidth} x {row.ActualHeight} at {row.TranslatePoint(new Point(), element)}, visible={row.IsVisible}"));
        File.AppendAllLines(Path.Combine(directory, name + ".txt"), Descendants<FrameworkElement>(element).Prepend(element)
            .Select(child => $"{child.GetType().Name} {child.Name}: {child.ActualWidth} x {child.ActualHeight}, clip={VisualTreeHelper.GetClip(child)?.Bounds}, layoutclip={System.Windows.Controls.Primitives.LayoutInformation.GetLayoutClip(child)?.Bounds}"));
        var dpi = VisualTreeHelper.GetDpi(element);
        var image = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        image.Render(element); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
