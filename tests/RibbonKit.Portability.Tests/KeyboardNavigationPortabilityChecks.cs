using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

internal static class KeyboardNavigationPortabilityChecks
{
    private static FlowDirection _direction;
    private static string _lastInput = "";

    internal static void Verify(Application application)
    {
        var failures = new List<string>();
        VerifyNestedDropdownEscape();
        foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
            {
                ThemeManager.Apply(application, theme);
                _direction = direction;
                foreach (Action check in new Action[] { VerifyQatOrder, VerifyQatOverflow, VerifyDropdowns,
                             VerifyApplicationMenu, VerifyBackstage, VerifyBackstageEntry, VerifyCollapsedGroup })
                {
                    _lastInput = "";
                    try { check(); }
                    catch (Exception error)
                    {
                        failures.Add($"{theme}/{direction}/{check.Method.Name}: {error.Message} "
                            + error.StackTrace?.Split('\n').FirstOrDefault(line => line.Contains("Portability.Tests"))
                            + $"; last input: {_lastInput}");
                    }
                }
            }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static void VerifyCollapsedGroup()
    {
        var first = new RibbonButton { Header = "First" };
        var last = new RibbonButton { Header = "Last" };
        var editor = new TextBox { Text = "Editable" };
        var commands = new StackPanel { Width = 270 };
        commands.Children.Add(new RibbonButton { Header = "Disabled", IsEnabled = false });
        commands.Children.Add(first);
        commands.Children.Add(editor);
        commands.Children.Add(last);
        var group = new RibbonGroup { Header = "Commands", ShowDialogLauncher = true };
        group.Items.Add(commands);
        var tab = new RibbonTab { Header = "Home" };
        tab.Groups.Add(group);
        var ribbon = new Ribbon { Width = 200 };
        ribbon.Tabs.Add(tab);
        var document = new Button { Content = "Document" };
        InWindow(new StackPanel { Children = { ribbon, document } }, window =>
        {
            Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
            var opener = Assert.IsType<ToggleButton>(group.Template.FindName("PART_CollapsedButton", group));
            var popup = Assert.IsType<Popup>(group.Template.FindName("PART_Popup", group));
            var launcher = Assert.IsType<Button>(group.Template.FindName("PART_DialogLauncher", group));
            Assert.True(opener.Focus());
            Press(Key.Enter);
            Assert.True(popup.IsOpen);
            Assert.Same(first, Keyboard.FocusedElement);
            KeyboardFocusPortabilityChecks.VerifyFocusVisual(first, owner: window);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                Press(Key.Tab); Assert.Same(editor, Keyboard.FocusedElement);
                Press(Key.Tab); Assert.Same(last, Keyboard.FocusedElement);
                Press(Key.Tab); Assert.Same(launcher, Keyboard.FocusedElement);
                Press(Key.Tab); Assert.Same(first, Keyboard.FocusedElement);
            }
            Previous(first, launcher);
            Previous(launcher, last);
            Press(Key.Escape);
            Assert.False(popup.IsOpen);
            Assert.Same(opener, Keyboard.FocusedElement);
            Press(Key.Space);
            Assert.Same(first, Keyboard.FocusedElement);
            Press(Key.End);
            Assert.Same(launcher, Keyboard.FocusedElement);
            Press(Key.Home);
            Assert.Same(first, Keyboard.FocusedElement);
            Press(Key.Escape);
            Assert.Same(opener, Keyboard.FocusedElement);
            // Closing after focus leaves the flyout must preserve the document's focus.
            Press(Key.Enter);
            Assert.True(document.Focus());
            opener.IsChecked = false;
            Drain();
            Assert.Same(document, Keyboard.FocusedElement);
        });
    }

    private static void VerifyNestedDropdownEscape()
    {
        var source = new RibbonDropDownButton { Header = "Nested menu" };
        var action = new RibbonMenuItem { Header = "Nested action" };
        source.Items.Add(action);
        var ribbon = new Ribbon { QuickAccessMaxWidth = 45 };
        var tab = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        group.Items.Add(source);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Wide QAT command", Width = 80 });
        Assert.True(ribbon.AddToQuickAccess(source));
        InWindow(ribbon, _ =>
        {
            var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            var qat = Assert.IsType<RibbonQuickAccessToolBar>(tabs.Template.FindName("QatTabRowHost", tabs));
            Assert.True(qat.HasOverflow);
            var opener = Assert.IsType<ToggleButton>(qat.Template.FindName("PART_OverflowButton", qat));
            var popup = Assert.IsType<Popup>(qat.Template.FindName("PART_OverflowPopup", qat));
            Assert.True(((ToggleButton)source.Template.FindName("PART_Toggle", source)).Focus());
            // The outer QAT chevron uses the existing pointer/KeyTip entry path.
            // This regression drives keyboard focus and Escape in its nested dropdown.
            opener.IsChecked = true;
            Drain();
            Assert.True(popup.IsOpen);
            var host = Assert.IsType<ItemsControl>(qat.Template.FindName("PART_OverflowHost", qat));
            var nested = Assert.Single(host.Items.OfType<RibbonDropDownButton>());
            var nestedOpener = Assert.IsType<ToggleButton>(nested.Template.FindName("PART_Toggle", nested));
            Assert.True(nestedOpener.Focus(), $"Nested opener: visible={nestedOpener.IsVisible}, enabled={nestedOpener.IsEnabled}, focusable={nestedOpener.Focusable}, popup={popup.IsOpen}");
            Press(Key.Enter);
            Assert.Same(action, Keyboard.FocusedElement);
            Assert.Empty(source.Items);
            Press(Key.Escape);
            Assert.False(nested.IsDropDownOpen);
            Assert.True(popup.IsOpen);
            Assert.Same(nestedOpener, Keyboard.FocusedElement);
            Assert.Same(action, Assert.Single(source.Items.Cast<object>()));
            Press(Key.Escape);
            Assert.False(popup.IsOpen);
        });
    }

    private static void VerifyQatOrder()
    {
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "Document" };
        var command = new RibbonButton { Header = "Ribbon command" };
        var qat = new RibbonButton { Header = "QAT command", Size = RibbonControlSize.Small };
        var home = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" };
        group.Items.Add(command);
        home.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, Backstage = new Backstage() };
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Insert(0, new RibbonTab { Header = "Other" });
        ribbon.SelectedTab = home;
        ribbon.QuickAccessItems.Add(qat);
        var root = new StackPanel();
        root.Children.Add(before);
        root.Children.Add(ribbon);
        root.Children.Add(after);
        InWindow(root, window =>
        {
            // This plain host has no overlapping content; give siblings equal paint order.
            // Ribbon's default ZIndex=1 is for hosts whose document paints under its shadow.
            Panel.SetZIndex(ribbon, 0);
            foreach (var position in new[] { RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TabRow,
                         RibbonQuickAccessPosition.BelowRibbon })
            {
                ribbon.QuickAccessPosition = position;
                Drain();
                window.UpdateLayout();
                Assert.True(before.Focus());
                Assert.Same(before, Keyboard.FocusedElement);
                var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
                var file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
                var stops = new List<IInputElement?>();
                for (int i = 0; i < 12; i++)
                {
                    Press(Key.Tab);
                    stops.Add(Keyboard.FocusedElement);
                    if (Keyboard.FocusedElement == after) break;
                }
                Assert.True(stops.Contains(command) && stops.Contains(qat), string.Join(" -> ", stops.Select(Describe)));
                Assert.Same(file, stops[0]);
                Assert.Same(home, ribbon.SelectedTab);
                Assert.Equal(position == RibbonQuickAccessPosition.BelowRibbon, stops.IndexOf(command) < stops.IndexOf(qat));
                Previous(after, position == RibbonQuickAccessPosition.BelowRibbon ? qat : command);
                Assert.True(after.Focus());
                Press(Key.Tab);
                Assert.Same(before, Keyboard.FocusedElement);
                Press(Key.Tab);
                Assert.Same(file, Keyboard.FocusedElement);
            }
            ribbon.IsMinimized = true;
            Drain();
            Assert.True(before.Focus());
            Press(Key.Tab);
            var minimizedTabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
            Assert.Same(minimizedTabs.Template.FindName("PART_ApplicationButton", minimizedTabs), Keyboard.FocusedElement);
        });
    }

    private static void VerifyQatOverflow()
    {
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        var visible = new RibbonButton { Header = "Visible", Width = 20 };
        var hidden = new RibbonButton { Header = "Overflow", Width = 90 };
        var dropdown = new RibbonDropDownButton { Header = "Overflow menu", Width = 90 };
        var applicationOwned = new RibbonButton { Header = "App-owned focus", Width = 90 };
        applicationOwned.SetCurrentValue(UIElement.FocusableProperty, true);
        var setting = new CheckBox { IsChecked = true };
        var binding = BindingOperations.SetBinding(hidden, UIElement.FocusableProperty,
            new Binding(nameof(CheckBox.IsChecked)) { Source = setting });
        var qat = new RibbonQuickAccessToolBar { MaxWidth = 65 };
        qat.Items.Add(visible);
        qat.Items.Add(hidden);
        qat.Items.Add(dropdown);
        qat.Items.Add(applicationOwned);
        var root = new StackPanel { Children = { before, qat, after } };
        InWindow(root, window =>
        {
            Assert.True(qat.HasOverflow);
            Assert.False(hidden.Focusable);
            Assert.Equal(Visibility.Visible, hidden.Visibility);
            Assert.Equal(KeyboardNavigationMode.None, KeyboardNavigation.GetTabNavigation(dropdown));
            Assert.True(before.Focus());
            Press(Key.Tab);
            Assert.Same(visible, Keyboard.FocusedElement);
            Press(Key.Tab);
            Assert.Same(after, Keyboard.FocusedElement);
            Previous(after, visible);

            setting.IsChecked = false;
            setting.IsChecked = true;
            Drain();
            Assert.False(hidden.Focusable);
            Assert.Same(binding, BindingOperations.GetBindingExpression(hidden, UIElement.FocusableProperty));
            // Application changes made while overflowed must become effective when it returns.
            KeyboardNavigation.SetTabNavigation(dropdown, KeyboardNavigationMode.Cycle);
            Assert.Equal(KeyboardNavigationMode.None, KeyboardNavigation.GetTabNavigation(dropdown));
            applicationOwned.Focusable = false;
            qat.MaxWidth = 500;
            Drain();
            window.UpdateLayout();
            Assert.False(qat.HasOverflow);
            Assert.True(hidden.Focusable);
            Assert.False(applicationOwned.Focusable);
            Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(dropdown));
            Assert.Same(binding, BindingOperations.GetBindingExpression(hidden, UIElement.FocusableProperty));
            Assert.True(visible.Focus());
            Press(Key.Tab);
            Assert.Same(hidden, Keyboard.FocusedElement);
            Press(Key.Tab);
            Assert.Same(dropdown.Template.FindName("PART_Toggle", dropdown), Keyboard.FocusedElement);

            qat.MaxWidth = 65;
            Drain();
            Assert.False(hidden.Focusable);
            qat.Template = (ControlTemplate)XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid /></ControlTemplate>");
            Drain();
            Assert.True(hidden.Focusable);
            qat.ClearValue(Control.TemplateProperty);
            Drain();
            window.UpdateLayout();
            Assert.False(hidden.Focusable);
            qat.Items.Remove(hidden);
            Drain();
            Assert.True(hidden.Focusable);
        });
        Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(dropdown));
    }

    private static void VerifyBackstageEntry()
    {
        foreach (var design in Enum.GetValues<RibbonBackstageDesign>())
        {
            var first = new BackstageTabItem { Header = "Home", Content = new Button { Content = "Document action" } };
            var active = new BackstageTabItem { Header = "Info", Content = new Button { Content = "Active page action" } };
            var backstage = new Backstage { Design = design };
            backstage.Items.Add(first);
            backstage.Items.Add(active);
            backstage.SelectedItem = active;
            var ribbon = new Ribbon { Backstage = backstage };
            ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
            var document = new TextBox();
            var root = new DockPanel();
            DockPanel.SetDock(ribbon, Dock.Top);
            root.Children.Add(ribbon);
            root.Children.Add(document);
            InWindow(root, window =>
            {
                var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
                var file = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
                Assert.True(document.Focus());
                Press(Key.Tab);
                Assert.Same(file, Keyboard.FocusedElement);
                for (int opening = 0; opening < 3; opening++)
                {
                    if (opening == 1 && design is RibbonBackstageDesign.CrystalSidebar or RibbonBackstageDesign.CrystalFloating)
                    {
                        var resources = new ResourceDictionary
                        {
                            Source = new Uri("/RibbonKit;component/Themes/Controls.CrystalBackstage.xaml", UriKind.Relative),
                        };
                        backstage.Style = (Style)resources[design == RibbonBackstageDesign.CrystalSidebar
                            ? "Crystal.Backstage.Sidebar" : "Crystal.Backstage.Style"];
                    }
                    if (opening == 2) first.IsEnabled = false;
                    var expected = opening == 2 ? active : Assert.IsType<BackstageTabItem>(backstage.SelectedItem);
                    Press(Key.Enter);
                    Assert.True(ribbon.IsBackstageOpen);
                    var back = Assert.IsType<Button>(backstage.Template.FindName("PART_BackButton", backstage));
                    Assert.True(ReferenceEquals(expected, Keyboard.FocusedElement),
                        $"{design}, opening {opening}: focus={Describe(Keyboard.FocusedElement)}, "
                        + $"back visible={back.IsVisible}, focusable={back.Focusable}");
                    Assert.Same(expected, backstage.SelectedItem);
                    Assert.Null(backstage.FocusVisualStyle);
                    var focused = Assert.IsAssignableFrom<Control>(Keyboard.FocusedElement);
                    Assert.NotNull(focused.FocusVisualStyle);
                    VerifySingleBackstageFocus(focused, Assert.IsType<Button>(expected.Content), window,
                        $"backstage-{design}-{_direction}-entry");
                    if (first.IsEnabled)
                    {
                        Assert.True(first.Focus());
                        VerifySingleBackstageFocus(first, Assert.IsType<Button>(first.Content), window,
                            $"backstage-{design}-{_direction}-item");
                    }
                    if (back.IsVisible)
                        VerifySingleBackstageFocus(back, first.IsEnabled ? first : active, window,
                            $"backstage-{design}-{_direction}-back");
                    Press(Key.Escape);
                    Assert.False(ribbon.IsBackstageOpen);
                    Assert.True(file.Focus());
                }
            });
        }
    }

    private static void VerifySingleBackstageFocus(Control target, Control alternate, Window window, string reviewName)
    {
        // SelectedContent is transferred at DataBind priority after a header changes selection.
        Drain();
        window.UpdateLayout();
        Assert.True(alternate.Focus(), $"{reviewName}: alternate {Describe(alternate)}, "
            + $"visible={alternate.IsVisible}, enabled={alternate.IsEnabled}, focusable={alternate.Focusable}");
        window.UpdateLayout();
        var chrome = target.Template.FindName("Chrome", target) as Border;
        var ordinaryBorder = chrome?.BorderBrush;
        Assert.True(target.Focus());
        window.UpdateLayout();
        // Selection and ordinary glass paint remain, but keyboard focus adds only the adorner.
        if (chrome is not null) Assert.Same(ordinaryBorder, chrome.BorderBrush);
        KeyboardFocusPortabilityChecks.VerifyFocusVisual(target,
            ThemeManager.CurrentTheme == RibbonTheme.CrystalLight ? reviewName : null, window);
    }

    private static void VerifyDropdowns()
    {
        foreach (RibbonDropDownButton dropdown in new RibbonDropDownButton[]
                 { new() { Header = "Menu" }, new RibbonSplitButton { Header = "Split" } })
        {
            var first = new RibbonMenuItem { Header = "First" };
            var disabled = new RibbonMenuItem { Header = "Disabled", IsEnabled = false };
            var last = new RibbonMenuItem { Header = "Last" };
            dropdown.Items.Add(first);
            dropdown.Items.Add(disabled);
            dropdown.Items.Add(new Separator());
            dropdown.Items.Add(last);
            InWindow(new StackPanel { Children = { dropdown } }, window =>
            {
                int executions = 0;
                var command = new RoutedCommand();
                last.Command = command;
                last.CommandParameter = "menu-parameter";
                window.CommandBindings.Add(new CommandBinding(command, (_, e) =>
                {
                    Assert.Equal("menu-parameter", e.Parameter);
                    executions++;
                }, (_, e) => { e.CanExecute = true; e.Handled = true; }));
                var opener = Assert.IsType<ToggleButton>(dropdown.Template.FindName("PART_Toggle", dropdown));
                Assert.True(opener.Focus());
                Press(Key.Enter);
                Assert.True(dropdown.IsDropDownOpen);
                Assert.Same(first, Keyboard.FocusedElement);
                Press(Key.Down);
                Assert.Same(last, Keyboard.FocusedElement);
                Press(Key.Down);
                Assert.Same(first, Keyboard.FocusedElement);
                Press(Key.Tab);
                Assert.Same(last, Keyboard.FocusedElement);
                Press(Key.Tab);
                Assert.Same(first, Keyboard.FocusedElement);
                Previous(first, last);
                Press(Key.Home);
                Assert.Same(first, Keyboard.FocusedElement);
                Press(Key.End);
                Assert.Same(last, Keyboard.FocusedElement);
                Press(Key.Enter);
                Assert.Equal(1, executions);
                Assert.False(dropdown.IsDropDownOpen);
                Assert.Same(opener, Keyboard.FocusedElement);
                Press(Key.Up);
                Assert.Same(last, Keyboard.FocusedElement);
                Press(Key.Escape);
                Assert.False(dropdown.IsDropDownOpen);
                Assert.Same(opener, Keyboard.FocusedElement);
                Press(Key.Down);
                Assert.Same(first, Keyboard.FocusedElement);
                Press(Key.Escape);
                Assert.Same(opener, Keyboard.FocusedElement);
            });
        }
    }

    private static void VerifyApplicationMenu()
    {
        var first = new RibbonApplicationMenuItem { Header = "First" };
        var paneFirst = new RibbonApplicationMenuPaneItem { Content = "Pane first" };
        var paneLast = new RibbonApplicationMenuPaneItem { Content = "Pane last" };
        var pane = new StackPanel { Children = { paneFirst, paneLast } };
        var split = new RibbonApplicationMenuItem
        {
            Header = "Split",
            Content = pane,
        };
        var last = new RibbonApplicationMenuItem { Header = "Last" };
        var recent = new RibbonApplicationMenuPaneItem { Content = "Recent" };
        var footer = new RibbonApplicationMenuButton { Content = "Options" };
        var menu = new RibbonApplicationMenu { DefaultContent = recent, FooterContent = footer };
        menu.Items.Add(first);
        menu.Items.Add(new RibbonApplicationMenuItem { Header = "Disabled", IsEnabled = false });
        menu.Items.Add(new RibbonApplicationMenuSeparator());
        menu.Items.Add(split);
        menu.Items.Add(last);
        var ribbon = new Ribbon { ApplicationMenu = menu };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        InWindow(ribbon, _ =>
        {
            ribbon.IsBackstageOpen = true;
            Drain();
            var firstPrimary = Part(first, "PART_Primary");
            var splitPrimary = Part(split, "PART_Primary");
            Assert.True(firstPrimary.Focus());
            Press(Key.Down);
            Assert.Same(splitPrimary, Keyboard.FocusedElement);
            Press(Key.Down);
            Assert.Same(Part(last, "PART_Primary"), Keyboard.FocusedElement);
            Press(Key.Down);
            Assert.Same(firstPrimary, Keyboard.FocusedElement);
            Press(Key.End);
            Assert.Same(Part(last, "PART_Primary"), Keyboard.FocusedElement);
            Press(Key.Up);
            Assert.Same(splitPrimary, Keyboard.FocusedElement);
            Key enterPane = _direction == FlowDirection.RightToLeft ? Key.Left : Key.Right;
            Key leavePane = _direction == FlowDirection.RightToLeft ? Key.Right : Key.Left;
            Press(enterPane);
            Assert.Same(Part(split, "PART_Arrow"), Keyboard.FocusedElement);
            Press(enterPane);
            Assert.Same(paneFirst, Keyboard.FocusedElement);
            Press(Key.Down);
            Assert.Same(paneLast, Keyboard.FocusedElement);
            Press(Key.Down);
            Assert.Same(paneFirst, Keyboard.FocusedElement);
            Press(Key.Tab);
            Assert.Same(paneLast, Keyboard.FocusedElement);
            Previous(paneLast, paneFirst);
            Press(leavePane);
            Assert.Same(Part(split, "PART_Arrow"), Keyboard.FocusedElement);
            Press(leavePane);
            Assert.Same(splitPrimary, Keyboard.FocusedElement);
            Press(Key.Home);
            Assert.Same(firstPrimary, Keyboard.FocusedElement);
            Press(enterPane);
            Assert.Same(recent, Keyboard.FocusedElement);
            Press(leavePane);
            Assert.Same(firstPrimary, Keyboard.FocusedElement);
            Press(Key.Tab);
            Assert.Same(splitPrimary, Keyboard.FocusedElement);
            Press(Key.Tab);
            Assert.Same(Part(split, "PART_Arrow"), Keyboard.FocusedElement);
        });
    }

    private static void VerifyBackstage()
    {
        foreach (var design in Enum.GetValues<RibbonBackstageDesign>())
        {
            var home = new BackstageTabItem { Header = "Home", Content = new Button { Content = "Home action" } };
            var info = new BackstageTabItem { Header = "Info", Content = new Button { Content = "Info action" } };
            var account = new BackstageTabItem
            {
                Header = "Account",
                Placement = BackstageItemPlacement.Bottom,
                Content = new Button { Content = "Account action" },
            };
            var options = new BackstageTabItem { Header = "Options", IsButton = true, Placement = BackstageItemPlacement.Bottom };
            var exit = new BackstageTabItem { Header = "Exit", IsButton = true, Placement = BackstageItemPlacement.Bottom };
            var backstage = new Backstage { SelectedIndex = 0 };
            Backstage.SetDesign(backstage, design);
            var disabled = new BackstageTabItem { Header = "Disabled", IsEnabled = false };
            foreach (var item in new[] { home, disabled, info, account, options, exit }) backstage.Items.Add(item);
            InWindow(backstage, _ =>
            {
                int clicks = 0;
                exit.Click += (_, _) => clicks++;
                options.Click += (_, _) => clicks++;
                Assert.True(home.Focus());
                var back = Assert.IsType<Button>(backstage.Template.FindName("PART_BackButton", backstage));
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    foreach (var item in new[] { info, account, options, exit })
                    {
                        Press(Key.Tab);
                        Assert.Same(item, Keyboard.FocusedElement);
                        Assert.Equal(0, clicks);
                        if (item.IsButton)
                        {
                            Assert.False(item.IsSelected);
                            Assert.Same(account, backstage.SelectedItem);
                        }
                    }
                    Press(Key.Tab);
                    Assert.Same(account.Content, Keyboard.FocusedElement);
                    if (back.IsVisible) { Press(Key.Tab); Assert.Same(back, Keyboard.FocusedElement); }
                    Press(Key.Tab);
                    Assert.Same(home, Keyboard.FocusedElement);
                }
                Assert.True(exit.Focus());
                Assert.Same(home, backstage.SelectedItem);
                Previous(exit, options);
                Previous(options, account);
                Previous(account, info);
                Previous(info, home);
                Assert.True(exit.Focus());
                Press(Key.Enter);
                Assert.Equal(1, clicks);
                Assert.True(options.Focus());
                Press(Key.Space);
                Assert.Equal(2, clicks);
                Assert.Same(home, backstage.SelectedItem);
            });
        }
    }

    private static Button Part(RibbonApplicationMenuItem item, string name) =>
        Assert.IsType<Button>(item.Template.FindName(name, item));

    // ProcessInput includes WPF's post-processing navigation; RaiseEvent alone does not.
    private static void Press(Key key)
    {
        var target = Assert.IsAssignableFrom<UIElement>(Keyboard.FocusedElement);
        string before = InputState(target);
        var source = PresentationSource.FromVisual(target)!;
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
        { RoutedEvent = Keyboard.PreviewKeyUpEvent });
        Drain();
        _lastInput = $"{key}: before [{before}], after [{InputState(target)}]";
    }

    private static void InWindow(UIElement content, Action<Window> verify)
    {
        var window = new Window
        {
            FlowDirection = _direction,
            Content = content,
            Width = 800,
            Height = 650,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        try { window.Show(); Drain(); window.UpdateLayout(); verify(window); }
        finally { window.Close(); Drain(); }
    }

    private static void Previous(UIElement from, UIElement to)
    {
        Assert.Same(from, Keyboard.FocusedElement);
        string before = InputState(from);
        Assert.True(from.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous)));
        Drain();
        _lastInput = $"Previous: before [{before}], after [{InputState(from)}]";
        Assert.Same(to, Keyboard.FocusedElement);
    }

    private static string InputState(UIElement target) =>
        $"modifiers={Keyboard.Modifiers}, focused={Describe(Keyboard.FocusedElement)}, "
        + $"visible={target.IsVisible}, active={Window.GetWindow(target)?.IsActive}";

    private static string Describe(IInputElement? element) => element switch
    {
        RibbonButton button => button.Header ?? "RibbonButton",
        TabItem tab => tab.Header?.ToString() ?? "TabItem",
        ContentControl control => control.Content?.ToString() ?? control.GetType().Name,
        _ => element?.GetType().Name ?? "null",
    };

    private static void Drain() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
}
