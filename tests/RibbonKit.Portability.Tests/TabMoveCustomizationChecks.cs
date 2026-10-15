using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// This consumer invokes the public editor/menu and inspects normal WPF template
// parts; it neither constructs the internal modal nor references Showcase resources.
internal static class TabMoveCustomizationChecks
{
    internal static void Verify(Application application)
    {
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        int cases = 0;
        try
        {
            foreach (var theme in new[] { RibbonTheme.Office2010, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (var direction in Enum.GetValues<FlowDirection>())
            {
                ThemeManager.Apply(application, theme);
                var ribbon = new Ribbon { LayoutMode = RibbonLayoutMode.Simplified, Density = density };
                var home = new RibbonTab { Header = "Home" }; Ribbon.SetCommandId(home, "home");
                var group = new RibbonGroup { Header = "Commands" }; Ribbon.SetCommandId(group, "commands");
                var copy = new RibbonButton { Header = "Copy" }; Ribbon.SetCommandId(copy, "copy");
                Ribbon.SetSimplifiedPlacement(copy, RibbonSimplifiedPlacement.Inline); group.Items.Add(copy); home.Groups.Add(group); ribbon.Tabs.Add(home);
                var excluded = new RibbonTab { Header = "Context", IsContextual = true }; ribbon.Tabs.Add(excluded);
                var page = new RibbonSimplifiedCustomizePage { Ribbon = ribbon };
                var owner = new RibbonOptionsDialog { Width = 900, Height = 600, ShowActivated = false, ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000, FlowDirection = direction };
                Ribbon.SetDensity(owner, density); owner.Pages.Add(new RibbonOptionsPage { Header = "Customize Ribbon", Content = page });
                try
                {
                    owner.Show(); Layout(owner);
                    OpenMenu(page, "PART_NewButton", RibbonString.NewTab).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                    Part<Button>(page, "PART_NewButton").ContextMenu.IsOpen = false; Layout(owner);
                    var originalOrder = DestinationHeaders(page);
                    Assert.Equal(new[] { home, excluded }, ribbon.Tabs);
                    ChooseMove(page, owner, density, direction, theme, before: true, cancel: true);
                    Assert.Equal(originalOrder, DestinationHeaders(page));
                    ChooseMove(page, owner, density, direction, theme, before: true, cancel: false);
                    Assert.Equal(originalOrder.AsEnumerable().Reverse(), DestinationHeaders(page));
                    Assert.Equal(new[] { home, excluded }, ribbon.Tabs);
                    ApplyWithoutAlignmentErrors(page, owner);
                    var custom = ribbon.Tabs.Single(Ribbon.GetIsCustom); string customId = Ribbon.GetCommandId(custom)!;
                    Assert.Same(custom, ribbon.Tabs[0]); Assert.Same(home, ribbon.Tabs[1]); Assert.Contains(excluded, ribbon.Tabs);
                    ChooseMove(page, owner, density, direction, theme, before: false, cancel: false);
                    page.CancelChanges(); Layout(owner); Assert.Same(custom, ribbon.Tabs[0]);
                    ChooseMove(page, owner, density, direction, theme, before: false, cancel: false);
                    ApplyWithoutAlignmentErrors(page, owner); Assert.True(ribbon.Tabs.IndexOf(custom) > ribbon.Tabs.IndexOf(home));
                    string layout = RibbonCustomizationSerializer.Serialize(ribbon);
                    ribbon.Tabs.Move(ribbon.Tabs.IndexOf(custom), 0); RibbonCustomizationSerializer.Apply(ribbon, layout);
                    Assert.Equal(new[] { "home", customId }, ribbon.Tabs.Where(tab => !tab.IsContextual).Select(Ribbon.GetCommandId));
                    cases++;
                }
                finally { owner.Close(); Layout(owner); }
            }
        }
        finally { RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; }
        Console.WriteLine($"Tab move independent consumer: {cases} legacy/flat/Crystal, density and direction cases passed; Touch dialog geometry also uses simulated 150% root DPI.");
    }

    private static string[] DestinationHeaders(RibbonSimplifiedCustomizePage page)
    {
        // Destination combo descriptors are normal WPF items; the control's selected
        // text identifies the draft without relying on its internal descriptor type.
        var selector = Part<Selector>(page, "PART_TabSelector"); Assert.Equal(2, selector.Items.Count);
        return selector.Items.Cast<object>().Select(item =>
        {
            var text = new TextBlock(); text.SetBinding(TextBlock.TextProperty, new Binding(selector.DisplayMemberPath) { Source = item });
            Assert.False(string.IsNullOrWhiteSpace(text.Text)); return text.Text;
        }).ToArray();
    }

    private static MenuItem OpenMenu(Control page, string part, RibbonString action)
    {
        var button = Part<Button>(page, part); button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        return button.ContextMenu.Items.OfType<MenuItem>().Single(item => Equals(item.Header, RibbonLocalization.GetString(action)));
    }

    private static void ApplyWithoutAlignmentErrors(RibbonSimplifiedCustomizePage page, Window owner)
    {
        var selectors = new[] { Part<RibbonComboBox>(page, "PART_SourceTabSelector"), Part<RibbonComboBox>(page, "PART_TabSelector") };
        foreach (var selector in selectors)
        { selector.IsDropDownOpen = true; Layout(owner); selector.IsDropDownOpen = false; Layout(owner); }
        var oldItems = selectors.SelectMany(selector => selector.Items.Cast<object>()
            .Select(item => selector.ItemContainerGenerator.ContainerFromItem(item))).OfType<ComboBoxItem>().ToArray();
        Assert.NotEmpty(oldItems);
        using var errors = new StringWriter(); using var listener = new TextWriterTraceListener(errors);
        var trace = PresentationTraceSources.DataBindingSource; var previous = trace.Switch.Level;
        trace.Listeners.Add(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = SourceLevels.Warning;
        try
        {
            page.ApplyChanges(); Layout(owner);
            // Inspect discarded properties as the debugger's binding diagnostics does.
            foreach (var old in oldItems)
            {
                _ = old.HorizontalContentAlignment; _ = old.VerticalContentAlignment;
                BindingOperations.GetBindingExpression(old, Control.HorizontalContentAlignmentProperty)?.UpdateTarget();
                BindingOperations.GetBindingExpression(old, Control.VerticalContentAlignmentProperty)?.UpdateTarget();
            }
            Layout(owner); listener.Flush();
            Assert.DoesNotContain("HorizontalContentAlignment", errors.ToString()); Assert.DoesNotContain("VerticalContentAlignment", errors.ToString());
        }
        finally { trace.Listeners.Remove(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = previous; }
    }

    private static void ChooseMove(RibbonSimplifiedCustomizePage page, Window owner, RibbonDensity density, FlowDirection direction, RibbonTheme theme, bool before, bool cancel)
    {
        var action = OpenMenu(page, "PART_EditButton", RibbonString.MoveTab); Assert.True(action.IsEnabled);
        bool seen = false;
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            var dialog = Application.Current.Windows.OfType<RibbonCustomizeEditDialog>().Single();
            try
            {
                seen = true; Layout(dialog);
                var position = Part<RibbonComboBox>(dialog, "PART_PositionSelector"); var target = Part<RibbonComboBox>(dialog, "PART_TargetTabSelector");
                Assert.Single(target.Items); Assert.Equal(direction, dialog.FlowDirection); Assert.Equal(density, Ribbon.GetDensity(dialog));
                Assert.True(position.IsKeyboardFocusWithin);
                position.SelectedIndex = before ? 0 : 1;
                if (density == RibbonDensity.Touch) { VisualTreeHelper.SetRootDpi(dialog, new DpiScale(1.5, 1.5)); Layout(dialog); }
                foreach (var name in new[] { "PART_PositionSelector", "PART_TargetTabSelector", "PART_OkButton", "PART_CancelButton" })
                {
                    var part = Part<FrameworkElement>(dialog, name); var bounds = part.TransformToVisual(dialog).TransformBounds(new Rect(part.RenderSize));
                    Assert.True(bounds.Left >= -0.75 && bounds.Right <= dialog.ActualWidth + 0.75 && bounds.Top >= -0.75 && bounds.Bottom <= dialog.ActualHeight + 0.75,
                        $"{theme}/{density}/{direction}/{name}: {bounds}, dialog={dialog.RenderSize}");
                    if (density == RibbonDensity.Touch) Assert.True(part.ActualHeight >= 43.5);
                    Assert.False(string.IsNullOrWhiteSpace(part is Button button ? new ButtonAutomationPeer(button).GetName() : AutomationProperties.GetName(part)));
                }
                if (before && !cancel && density == RibbonDensity.Touch) Save(dialog, "move-tab-" + theme + "-" + direction);
                Part<Button>(dialog, cancel ? "PART_CancelButton" : "PART_OkButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            finally { if (dialog.IsVisible) dialog.Close(); }
        }));
        action.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); Part<Button>(page, "PART_EditButton").ContextMenu.IsOpen = false;
        Layout(owner); Assert.True(seen);
    }

    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static void Layout(Window window) { Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); if (window.IsVisible) window.UpdateLayout(); }
    private static void Save(Window window, string name)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestResults", "simplified-customization"); Directory.CreateDirectory(path);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(path, name + ".png")); encoder.Save(stream);
    }
}
