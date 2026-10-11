using System.Runtime.InteropServices;
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
                        ((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Part<ToggleButton>(menu, "PART_Toggle"))!
                            .GetPattern(PatternInterface.Toggle)).Toggle();
                        Layout(); Assert.True(menu.IsDropDownOpen);
                        menu.IsDropDownOpen = false; Layout();
                    }
                }
            }
        }
        finally { window.Close(); Layout(); }

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

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Invoke(FrameworkElement target) =>
        ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(target)!
            .GetPattern(PatternInterface.Invoke)).Invoke();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
}
