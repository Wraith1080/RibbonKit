using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Exercise the customization page and native caption hit testing without Showcase resources.
internal static class QuickAccessCommandInteractionChecks
{
    internal static void Verify(Application application)
    {
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null,
            Geometry.Parse("M4,2 L12,2 L12,15 L4,15 Z")));
        var command = new RoutedUICommand("Undo", "Undo", typeof(QuickAccessCommandInteractionChecks));
        var undo = new RibbonButton { Header = "Undo", Icon = icon, Command = command, Size = RibbonControlSize.Small };
        var toggle = new RibbonToggleButton { Header = "Track", Icon = icon, Size = RibbonControlSize.Small };
        var split = new RibbonSplitButton { Header = "Redo", Icon = icon, Size = RibbonControlSize.Small };
        var dropdown = new RibbonDropDownButton { Header = "Actions", Icon = icon, Size = RibbonControlSize.Small };
        split.Items.Add(new RibbonMenuItem { Header = "Redo once" });
        dropdown.Items.Add(new RibbonMenuItem { Header = "Choose action" });
        var declared = new FrameworkElement[] { undo, toggle, split, dropdown };
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, QuickAccessMaxWidth = 400 };
        foreach (var item in declared)
        {
            Ribbon.SetCommandId(item, "cmd." + item.GetType().Name);
            ribbon.QuickAccessItems.Add(item);
        }
        var bodyCommand = new RibbonButton { Header = "Copy", Icon = icon };
        Ribbon.SetCommandId(bodyCommand, "cmd.copy");
        var group = new RibbonGroup { Header = "Editing" }; group.Items.Add(bodyCommand);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group);
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab; ribbon.AddToQuickAccess(bodyCommand);
        var page = new RibbonQuickAccessPage { Ribbon = ribbon };
        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition());
        content.Children.Add(ribbon); content.Children.Add(page); Grid.SetRow(page, 1);
        var window = new RibbonWindow { Content = content, Width = 900, Height = 700,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        int executions = 0, splitClicks = 0;
        window.CommandBindings.Add(new CommandBinding(command, (_, _) => executions++, (_, e) => e.CanExecute = true));
        split.Click += (_, _) => splitClicks++;
        try
        {
            window.Show(); Layout();
            VerifyTargets();
            var current = Part<ListBox>(page, "PART_CurrentList");
            var available = Part<ListBox>(page, "PART_AvailableList");
            foreach (var flow in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                window.FlowDirection = flow; Layout();
                foreach (var position in new[] { RibbonQuickAccessPosition.TitleBar,
                    RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
                {
                    ribbon.QuickAccessPosition = position; Layout();
                    foreach (var item in declared)
                    {
                        current.SelectedItem = current.Items.Cast<RibbonCommandEntry>().Single(e => ReferenceEquals(e.Control, item));
                        Part<ButtonBase>(page, "PART_RemoveButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                        Assert.DoesNotContain(item, ribbon.QuickAccessItems);
                        available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(e => ReferenceEquals(e.Control, item));
                        Part<ButtonBase>(page, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                        Assert.Contains(item, ribbon.QuickAccessItems); // Preserve the authored object and its handlers.
                        VerifyTargets();
                    }
                    string saved = RibbonCustomizationSerializer.Serialize(ribbon);
                    ribbon.QuickAccessItems.Clear(); Layout();
                    RibbonCustomizationSerializer.Apply(ribbon, saved); Layout();
                    VerifyTargets();
                    int before = executions;
                    Invoke(undo); Layout(); Assert.Equal(before + 1, executions);
                    before = splitClicks;
                    Invoke(Part<Button>(split, "PART_Primary")); Layout(); Assert.Equal(before + 1, splitClicks);
                    bool wasChecked = toggle.IsChecked == true;
                    ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(toggle)!
                        .GetPattern(PatternInterface.Toggle)).Toggle();
                    Layout(); Assert.Equal(!wasChecked, toggle.IsChecked);
                    foreach (var menu in new[] { split, dropdown })
                    {
                        var opener = Part<ToggleButton>(menu, "PART_Toggle");
                        var chrome = Part<Border>(opener, "Chrome");
                        var primaryChrome = menu is RibbonSplitButton ? Part<Border>(Part<Button>(menu, "PART_Primary"), "Chrome") : null;
                        var popup = Part<Popup>(menu, "PART_Popup");
                        var paint = new List<(bool Open, Brush Background, Brush Border, Brush? Companion)>();
                        var originalPlacement = popup.Placement;
                        var originalCallback = popup.CustomPopupPlacementCallback;
                        popup.SetCurrentValue(Popup.PlacementProperty, PlacementMode.Custom);
                        popup.SetCurrentValue(Popup.CustomPopupPlacementCallbackProperty,
                            new CustomPopupPlacementCallback((size, target, offset) =>
                            {
                                // Native creation precedes Opened; inspect that transition,
                                // not just the stable menu after the dispatcher drains.
                                if (popup.IsOpen) paint.Add((menu.IsDropDownOpen, chrome.Background, chrome.BorderBrush, primaryChrome?.Background));
                                return new[] { new CustomPopupPlacement(new Point(0, target.Height), PopupPrimaryAxis.None) };
                            }));
                        try
                        {
                            Assert.Equal(ClickMode.Press, opener.ClickMode);
                            PressOpener(opener);
                            Layout(); Assert.True(menu.IsDropDownOpen);
                            Assert.NotEmpty(paint);
                            bool colored = Ribbon.GetQatOnColoredSurface(opener);
                            foreach (var sample in paint)
                            {
                                Assert.True(sample.Open);
                                Assert.Same(menu.FindResource(colored ? Ribbon.QatColoredPressedBackgroundKey
                                    : "RibbonKit.Brushes.Control.PressedBackground"), sample.Background);
                                Assert.Same(menu.FindResource(colored ? "RibbonKit.Brushes.Qat.ColoredStateBorder"
                                    : "RibbonKit.Brushes.Control.PressedBorder"), sample.Border);
                                if (primaryChrome is not null)
                                    Assert.Same(menu.FindResource(colored ? "RibbonKit.Brushes.Qat.ColoredCompanionBackground"
                                        : "RibbonKit.Brushes.Control.CompanionBackground"), sample.Companion);
                            }
                            menu.IsDropDownOpen = false; Layout();
                            // Accessibility activation has no preceding physical press.
                            // Keep its ordinary toggle/open/close contract as well.
                            ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(opener)!
                                .GetPattern(PatternInterface.Toggle)).Toggle();
                            Layout(); Assert.True(menu.IsDropDownOpen); Assert.True(popup.IsOpen);
                            Assert.True(opener.IsChecked);
                            Assert.Same(menu.FindResource(colored ? Ribbon.QatColoredPressedBackgroundKey
                                : "RibbonKit.Brushes.Control.PressedBackground"), chrome.Background);
                            menu.IsDropDownOpen = false; Layout();
                        }
                        finally
                        {
                            menu.IsDropDownOpen = false;
                            popup.SetCurrentValue(Popup.CustomPopupPlacementCallbackProperty, originalCallback);
                            popup.SetCurrentValue(Popup.PlacementProperty, originalPlacement);
                        }
                    }
                }
            }
        }
        finally { window.Close(); Layout(); }

        VerifyCustomCommandMenus(icon);

        void Layout()
        {
            CommandManager.InvalidateRequerySuggested();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }

        void VerifyTargets()
        {
            var bodyCopy = ribbon.QuickAccessItems.OfType<FrameworkElement>()
                .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), bodyCommand));
            var targets = new FrameworkElement[] { undo, toggle, Part<Button>(split, "PART_Primary"),
                Part<ToggleButton>(split, "PART_Toggle"), Part<ToggleButton>(dropdown, "PART_Toggle"), bodyCopy };
            foreach (var target in targets)
            {
                Assert.True(target.IsEnabled && target.ActualWidth > 0 && target.ActualHeight > 0,
                    $"{target.GetType().Name}: enabled={target.IsEnabled}, size={target.ActualWidth}x{target.ActualHeight}");
                var center = new Point(target.ActualWidth / 2, target.ActualHeight / 2);
                var hit = Assert.IsAssignableFrom<IInputElement>(window.InputHitTest(target.TranslatePoint(center, window)));
                if (ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar)
                {
                    Assert.True(WindowChrome.GetIsHitTestVisibleInChrome(hit), $"{target.GetType().Name} lost caption interaction");
                    var screen = target.PointToScreen(center);
                    var packed = new IntPtr(unchecked(((int)Math.Round(screen.Y) & 0xffff) << 16 | ((int)Math.Round(screen.X) & 0xffff)));
                    Assert.Equal(new IntPtr(1), SendMessage(new WindowInteropHelper(window).Handle, 0x0084, IntPtr.Zero, packed)); // HTCLIENT
                }
            }
        }
    }

    private static void PressOpener(ToggleButton opener)
    {
        // This offscreen consumer supplies press state without injecting OS input.
        var pressedKey = (DependencyPropertyKey)typeof(ButtonBase)
            .GetField("IsPressedPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        opener.SetValue(pressedKey, true);
        try { MouseEvent("OnMouseLeftButtonDown", Mouse.MouseDownEvent); }
        finally { opener.SetValue(pressedKey, false); }
        MouseEvent("OnMouseLeftButtonUp", Mouse.MouseUpEvent);

        void MouseEvent(string method, RoutedEvent routedEvent) => typeof(ButtonBase)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(opener, new object[] { new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                { RoutedEvent = routedEvent, Source = opener } });
    }

    private static void VerifyCustomCommandMenus(ImageSource icon)
    {
        int cases = 0;
        foreach (bool split in new[] { false, true })
        {
            RibbonDropDownButton source = split ? new RibbonSplitButton() : new RibbonDropDownButton();
            source.Header = "Touch/Mouse Mode"; source.Icon = icon;
            source.DropDownHeader = "Optimize spacing between commands";
            Ribbon.SetCommandId(source, "command.mode");
            var mouse = new RibbonMenuItem { Header = "Mouse" };
            var touch = new RibbonMenuItem { Header = "Touch" };
            int clicks = 0; touch.Click += (_, _) => clicks++;
            source.Items.Add(mouse); source.Items.Add(touch);
            var home = new RibbonTab { Header = "Home" };
            var originalGroup = new RibbonGroup { Header = "Mode" }; originalGroup.Items.Add(source); home.Groups.Add(originalGroup);
            var customTab = new RibbonTab { Header = "Custom Tab" }; Ribbon.SetIsCustom(customTab, true);
            var customGroup = new RibbonGroup { Header = "Custom Group" }; Ribbon.SetIsCustom(customGroup, true);
            customTab.Groups.Add(customGroup);
            var ribbon = new Ribbon(); ribbon.Tabs.Add(home); ribbon.Tabs.Add(customTab); ribbon.SelectedTab = customTab;
            var customize = new RibbonCustomizePage { Ribbon = ribbon, Height = 200 };
            var qatPage = new RibbonQuickAccessPage { Ribbon = ribbon, Height = 200 };
            var window = new RibbonWindow { Content = new StackPanel { Children = { ribbon, customize, qatPage } },
                Width = 900, Height = 650, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            try
            {
                window.Show(); Layout();
                var available = Part<ListBox>(customize, "PART_AvailableList");
                available.SelectedItem = available.Items.Cast<RibbonCommandEntry>().Single(entry => ReferenceEquals(entry.Control, source));
                var node = Part<TreeView>(customize, "PART_Tree").Items.Cast<RibbonCustomizeNode>()
                    .Single(item => item.IsCustom);
                node.Children.Single().IsSelected = true; Layout();
                Part<ButtonBase>(customize, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                var copy = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(customGroup.Items));
                var qatAvailable = Part<ListBox>(qatPage, "PART_AvailableList");
                qatAvailable.SelectedItem = qatAvailable.Items.Cast<RibbonCommandEntry>().Single(entry => ReferenceEquals(entry.Control, source));
                foreach (FlowDirection flow in Enum.GetValues<FlowDirection>())
                foreach (RibbonQuickAccessPosition placement in Enum.GetValues<RibbonQuickAccessPosition>())
                {
                    window.FlowDirection = flow; ribbon.QuickAccessPosition = placement;
                    ribbon.QuickAccessItems.Clear(); Layout();
                    Assert.True(Part<ButtonBase>(qatPage, "PART_AddButton").IsEnabled);
                    var menu = OpenCommandMenu(Part<ToggleButton>(copy, "PART_Toggle"));
                    var add = Assert.IsType<MenuItem>(menu.Items[0]); Assert.True(add.IsEnabled);
                    Invoke(add); Layout(); menu.IsOpen = false; Layout();
                    var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
                    Assert.Same(source, Ribbon.GetQuickAccessSource(qat));
                    Assert.True(ribbon.IsInQuickAccess(copy)); Assert.False(ribbon.AddToQuickAccess(copy));
                    Layout(); Assert.False(Part<ButtonBase>(qatPage, "PART_AddButton").IsEnabled);
                    Part<ButtonBase>(qatPage, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); Layout();
                    Assert.Single(ribbon.QuickAccessItems);
                    menu = OpenCommandMenu(Part<ToggleButton>(copy, "PART_Toggle"));
                    Assert.False(Assert.IsType<MenuItem>(menu.Items[0]).IsEnabled); menu.IsOpen = false; Layout();
                    qat.IsDropDownOpen = true; Layout();
                    Assert.True(Part<Popup>(qat, "PART_Popup").IsOpen);
                    Assert.Equal(new object[] { mouse, touch }, qat.Items.Cast<object>());
                    Assert.True(mouse.ActualHeight > 0 && touch.ActualHeight > 0);
                    int before = clicks; Invoke(touch); Layout();
                    Assert.Equal(before + 1, clicks); Assert.False(qat.IsDropDownOpen);
                    Assert.Equal(new object[] { mouse, touch }, source.Items.Cast<object>());
                    cases++;
                }
            }
            finally { window.Close(); Layout(); }

            void Layout()
            {
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            }

            ContextMenu OpenCommandMenu(UIElement target)
            {
                var constructor = typeof(ContextMenuEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single(ctor => ctor.GetParameters().Length == 2);
                var args = (ContextMenuEventArgs)constructor.Invoke(new object[] { target, true });
                target.RaiseEvent(args); Layout(); Assert.True(args.Handled);
                return PresentationSource.CurrentSources.Cast<PresentationSource>().OfType<HwndSource>()
                    .Select(host => host.RootVisual).SelectMany(Descendants).OfType<ContextMenu>().Single(menu => menu.IsOpen);
            }
        }
        Assert.Equal(12, cases);
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i))) yield return child;
    }

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Invoke(FrameworkElement target) =>
        ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(target)!
            .GetPattern(PatternInterface.Invoke)).Invoke();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
}
