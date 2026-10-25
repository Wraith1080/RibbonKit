using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// A public-API consumer: no Showcase reference, resources, helpers or internals.
internal static class GroupQuickAccessPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            VerifyMatrix(application);
            VerifyOverflowAndKeyboard(application);
            VerifyKeyTips(application);
            VerifyHoverBindings(application);
        }
        finally { RibbonAnimation.GlobalLevel = motion; ThemeManager.SetDarkMode(application, false); }
    }

    private static void VerifyMatrix(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var command = new RibbonButton { Header = "Action", Width = 85 };
        var combo = new RibbonComboBox { Header = "Choice", Width = 110, ItemsSource = new[] { "First", "Second" } };
        var gallery = new InRibbonGallery { Header = "Styles", Width = 135, Height = 58 };
        gallery.Items.Add(new RibbonGalleryItem { Content = "Plain" });
        gallery.Items.Add(new RibbonGalleryItem { Content = "Heading" });
        var menu = new RibbonDropDownButton { Header = "More", Icon = Icon() };
        menu.Items.Add(new RibbonMenuItem { Header = "Nested action" });
        var source = new RibbonGroup { Header = "Formatting", Icon = Icon(), ShowDialogLauncher = true };
        source.Resources["ConsumerBrush"] = Brushes.DarkCyan;
        command.SetResourceReference(Control.ForegroundProperty, "ConsumerBrush");
        var model = new Model(); source.DataContext = model;
        combo.SetBinding(Selector.SelectedIndexProperty, new Binding(nameof(Model.Index)) { Mode = BindingMode.TwoWay });
        source.Items.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { command, combo, gallery, menu } });
        var home = new RibbonTab { Header = "Home" }; home.Groups.Add(source);
        var other = new RibbonTab { Header = "Other" };
        var ribbon = new Ribbon { Backstage = new Backstage() }; ribbon.Tabs.Add(home); ribbon.Tabs.Add(other); ribbon.SelectedTab = other;
        Ribbon.SetCommandId(home, "tab.home"); Ribbon.SetCommandId(other, "tab.other"); Ribbon.SetCommandId(source, "group.formatting");
        var qatPage = new RibbonQuickAccessPage { Ribbon = ribbon, Height = 150 };
        var customize = new RibbonCustomizePage { Ribbon = ribbon, Height = 150 };
        var window = Window(new StackPanel { Children = { ribbon, qatPage, customize } });
        try
        {
            window.Show(); Layout(window);
            var available = Part<ListBox>(qatPage, "PART_AvailableList");
            var entry = available.Items.Cast<RibbonCommandEntry>().Single(item => ReferenceEquals(item.Control, source));
            Assert.Same(source.Icon, entry.Icon);
            Assert.DoesNotContain(Part<ListBox>(customize, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), item => item.Control is RibbonGroup);
            Assert.Contains(Part<ListBox>(customize, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), item => ReferenceEquals(item.Control, command));
            available.SelectedItem = entry;
            Part<ButtonBase>(qatPage, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout(window);
            var proxy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            int cases = 0;
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
                        VisualTreeHelper.SetRootDpi(window, new DpiScale(scale, scale)); Layout(window);
                        proxy.IsDropDownOpen = true; Layout(window);
                        var popup = Part<Popup>(source, "PART_Popup");
                        var card = Part<Border>(source, "PART_PopupHost");
                        Assert.True(popup.IsOpen); Assert.Same(proxy, popup.PlacementTarget);
                        Assert.False(Part<Popup>(proxy, "PART_Popup").IsOpen);
                        Assert.Same(other, ribbon.SelectedTab); Assert.Equal(RibbonGroupSizeState.Collapsed, source.SizeState);
                        Assert.True(card.IsAncestorOf(command)); Assert.True(card.IsAncestorOf(combo)); Assert.True(card.IsAncestorOf(gallery));
                        InputManager.Current.ProcessInput(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                            { RoutedEvent = Mouse.PreviewMouseDownEvent, Source = combo });
                        Layout(window); Assert.True(popup.IsOpen); // The source logical tree may route back through an active ribbon.
                        Assert.True(command.ActualHeight > 0); Assert.True(combo.ActualHeight > 0); Assert.True(gallery.ActualWidth > 0);
                        Assert.Same(Brushes.DarkCyan, command.Foreground);
                        Assert.True(flow == card.FlowDirection,
                            $"flow={flow}, ribbon={ribbon.FlowDirection}, tab={home.FlowDirection}, group={source.FlowDirection}, proxy={proxy.FlowDirection}, popup={popup.FlowDirection}, card={card.FlowDirection}, parent={source.Parent}");
                        Assert.Equal(density, Ribbon.GetDensity(combo));
                        foreach (var control in new FrameworkElement[] { command, combo, gallery, menu })
                        {
                            Point position = control.TranslatePoint(new Point(), card);
                            Assert.True(position.X >= -1 && position.Y >= -1 && position.X + control.ActualWidth <= card.ActualWidth + 1);
                        }
                        combo.SetCurrentValue(Selector.SelectedIndexProperty, 1); Assert.Equal(1, model.Index);
                        Assert.NotNull(BindingOperations.GetBindingExpression(combo, Selector.SelectedIndexProperty));
                        gallery.SelectedIndex = 1; Assert.Equal(1, gallery.SelectedIndex);
                        var peer = UIElementAutomationPeer.CreatePeerForElement(proxy)!;
                        Assert.Contains(peer.GetChildren()!, child => child.GetName() == "Action");
                        if (!dark && density == RibbonDensity.Compact && flow == FlowDirection.LeftToRight
                            && scale == 1.25 && placement == RibbonQuickAccessPosition.TabRow
                            && theme is RibbonTheme.Office2007 or RibbonTheme.Office2024 or RibbonTheme.CrystalLight)
                            Save(card, "group-qat-" + theme);
                        proxy.IsDropDownOpen = false; Layout(window);
                        Assert.Equal(RibbonGroupSizeState.Collapsed, source.SizeState);
                        Assert.NotNull(Part<Decorator>(source, "PART_NormalHost").Child); Assert.Null(card.Child);
                        cases++;
                    }
                }
            }
            Assert.Equal(336, cases);
            string json = RibbonCustomizationSerializer.Serialize(ribbon);
            ribbon.QuickAccessItems.Clear(); Layout(window);
            RibbonCustomizationSerializer.Apply(ribbon, json); Layout(window);
            Assert.Same(source, Ribbon.GetQuickAccessSource(Assert.IsAssignableFrom<DependencyObject>(Assert.Single(ribbon.QuickAccessItems))));
        }
        finally { window.Close(); Layout(window); }
    }

    private static void VerifyOverflowAndKeyboard(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        foreach (bool rtl in new[] { false, true })
        {
            var first = new RibbonButton { Header = "First", Icon = Icon() };
            var last = new RibbonButton { Header = "Last", Icon = Icon() };
            var nested = new RibbonDropDownButton { Header = "Menu", Icon = Icon() };
            nested.Items.Add(new RibbonMenuItem { Header = "Nested" });
            var group = new RibbonGroup { Header = "Commands", Icon = Icon() };
            group.Items.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { first, nested, last } });
            var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
            var ribbon = new Ribbon { FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, QuickAccessMaxWidth = 65 };
            ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
            for (int i = 0; i < 3; i++)
                ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Icon = Icon(), Size = RibbonControlSize.Small });
            ribbon.AddToQuickAccess(group);
            var window = Window(ribbon);
            window.ShowActivated = true;
            try
            {
                window.Show(); window.Activate(); Layout(window);
                var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
                var toolbar = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
                Assert.True(toolbar.HasOverflow);
                Part<ToggleButton>(toolbar, "PART_OverflowButton").IsChecked = true; Layout(window);
                var overflow = Part<ItemsControl>(toolbar, "PART_OverflowHost");
                var proxy = overflow.Items.OfType<RibbonDropDownButton>().Single();
                var opener = Part<ToggleButton>(proxy, "PART_Toggle"); Assert.True(opener.Focus());
                Press(rtl ? Key.Left : Key.Right); Layout(window);
                Assert.True(Part<Popup>(group, "PART_Popup").IsOpen);
                Assert.False(Part<Popup>(proxy, "PART_Popup").IsOpen);
                Assert.Same(first, Keyboard.FocusedElement);
                InputManager.Current.ProcessInput(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                    { RoutedEvent = Mouse.PreviewMouseDownEvent, Source = first });
                Layout(window); Assert.True(proxy.IsDropDownOpen); Assert.True(Part<Popup>(toolbar, "PART_OverflowPopup").IsOpen);
                Press(Key.End); Assert.Same(last, Keyboard.FocusedElement);
                Press(Key.Home); Assert.Same(first, Keyboard.FocusedElement);
                nested.IsDropDownOpen = true; Layout(window); Press(Key.Escape); Layout(window);
                Assert.False(nested.IsDropDownOpen); Assert.True(proxy.IsDropDownOpen);
                Press(Key.Escape); Layout(window); Assert.False(proxy.IsDropDownOpen); Assert.Same(opener, Keyboard.FocusedElement);
                opener.Focus(); Press(rtl ? Key.Left : Key.Right); Layout(window);
                Press(Key.End); Layout(window); Assert.Same(last, Keyboard.FocusedElement);
                int invoked = 0; last.Click += (_, _) => invoked++;
                Press(Key.Enter); Layout(window); Assert.Equal(1, invoked); Assert.False(proxy.IsDropDownOpen);
                proxy.IsDropDownOpen = true; Layout(window);
                ribbon.QuickAccessItems.Remove(ribbon.QuickAccessItems.OfType<RibbonDropDownButton>().Single()); Layout(window);
                Assert.False(Part<Popup>(group, "PART_Popup").IsOpen);
                Assert.Equal(RibbonGroupSizeState.Large, group.SizeState);
            }
            finally { window.Close(); Layout(window); }
        }
    }

    private static void VerifyKeyTips(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var command = new RibbonButton { Header = "Action", Icon = Icon() }; KeyTip.SetKeys(command, "A");
        var group = new RibbonGroup { Header = "Commands", Icon = Icon() }; group.Items.Add(command);
        var sourceTab = new RibbonTab { Header = "Home" }; sourceTab.Groups.Add(group);
        var other = new RibbonTab { Header = "Other" };
        var ribbon = new Ribbon(); ribbon.Tabs.Add(sourceTab); ribbon.Tabs.Add(other); ribbon.SelectedTab = other;
        for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Icon = Icon(), Size = RibbonControlSize.Small });
        ribbon.AddToQuickAccess(group);
        var strip = (RibbonDropDownButton)ribbon.QuickAccessItems[^1]; KeyTip.SetKeys(strip, "G");
        var window = Window(ribbon); window.ShowActivated = true;
        try
        {
            window.Show(); window.Activate(); Layout(window);
            int invoked = 0; command.Click += (_, _) => invoked++;
            foreach (bool overflow in new[] { false, true })
            {
                ribbon.QuickAccessMaxWidth = overflow ? 65 : 600; Layout(window);
                var toolbar = Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost");
                KeyTip.SetKeys(Part<ToggleButton>(toolbar, "PART_OverflowButton"), "Q");
                KeyTipPress(Key.F10);
                if (overflow) KeyTipPress(Key.Q);
                var proxy = overflow ? Part<ItemsControl>(toolbar, "PART_OverflowHost").Items.OfType<RibbonDropDownButton>().Single() : strip;
                KeyTipPress(Key.G);
                Assert.True(proxy.IsDropDownOpen, $"KeyTip group open failed: overflow={overflow}, focused={Keyboard.FocusedElement}");
                Assert.Same(other, ribbon.SelectedTab);
                Assert.Contains(AdornerLayer.GetAdornerLayer(command)?.GetAdorners(command) ?? Array.Empty<Adorner>(), a => a.GetType().Name == "KeyTipAdorner");
                KeyTipPress(Key.A); Layout(window);
                Assert.Equal(overflow ? 2 : 1, invoked);
                Assert.False(proxy.IsDropDownOpen);
                Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
            }
        }
        finally { window.Close(); Layout(window); }

        void KeyTipPress(Key key)
        {
            window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, Environment.TickCount, key)
                { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            Layout(window);
        }
    }

    private static void VerifyHoverBindings(Application application)
    {
        foreach (bool utility in new[] { true, false })
        foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
        foreach (RibbonQuickAccessPosition placement in Enum.GetValues<RibbonQuickAccessPosition>())
        {
            ThemeManager.Apply(application, utility ? RibbonTheme.CrystalLight : RibbonTheme.Office2007);
            ThemeManager.SetDarkMode(application, false);
            var group = new RibbonGroup { Header = "Commands", Icon = Icon() }; group.Items.Add(new RibbonButton { Header = "Action" });
            var home = new RibbonTab { Header = "Home" }; home.Groups.Add(group);
            var ribbon = new Ribbon { Backstage = new Backstage(), QuickAccessPosition = placement };
            ribbon.Tabs.Add(home); Assert.True(ribbon.AddToQuickAccess(group));
            var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
            var window = Window(ribbon); window.FlowDirection = flow;
            try
            {
                window.Show(); Layout(window);
                var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
                ButtonBase button; FrameworkElement paint; BindingBase binding;
                if (utility)
                {
                    var modal = new RibbonTab { Header = "Preview", IsModal = true }; ribbon.Tabs.Add(modal);
                    Assert.True(ribbon.EnterModal(modal)); Layout(window);
                    button = Part<Button>(tabs, "PART_ModalClose");
                    paint = Part<Border>(button, "CrystalUtilityRim");
                    binding = paint.Style.Triggers.OfType<MultiDataTrigger>().Single().Conditions[1].Binding;
                }
                else
                {
                    button = Part<ToggleButton>(tabs, "PART_ApplicationButton");
                    var presenter = Part<ContentPresenter>(button, "Orb");
                    paint = Descendants<System.Windows.Shapes.Ellipse>(presenter).Single(ellipse => ellipse.Name == "OrbFill");
                    binding = presenter.ContentTemplate.Triggers.OfType<MultiDataTrigger>().Single().Conditions[1].Binding;
                }
                var expression = (BindingExpression)BindingOperations.SetBinding(paint, FrameworkElement.TagProperty, binding);
                Layout(window); Assert.Equal(BindingStatus.Active, expression.Status); Assert.Equal(false, paint.Tag);
                qat.IsDropDownOpen = true; Layout(window); Assert.Equal(true, paint.Tag); Assert.False(expression.HasError);
                qat.IsDropDownOpen = false; Layout(window); Assert.Equal(false, paint.Tag);
                if (utility)
                {
                    ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();
                    Layout(window); Assert.False(ribbon.IsModal);
                }
            }
            finally { window.Close(); Layout(window); }
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private sealed class Model { public int Index { get; set; } }
    private static RibbonWindow Window(FrameworkElement content) => new()
        { Content = content, Width = 900, Height = 650, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static void Layout(Window window)
        { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); }
    private static void Press(Key key)
    {
        var target = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement);
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target)!, Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target)!, Environment.TickCount, key)
            { RoutedEvent = Keyboard.PreviewKeyUpEvent });
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
    private static void Save(FrameworkElement element, string name)
    {
        string? directory = Environment.GetEnvironmentVariable("RIBBONKIT_GROUP_QAT_DIAGNOSTICS");
        if (string.IsNullOrEmpty(directory)) return;
        Directory.CreateDirectory(directory);
        var dpi = VisualTreeHelper.GetDpi(element);
        var image = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        image.Render(element); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
    }
}
