using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Theming;
using Xunit;
using Uia = System.Windows.Automation.Automation;

namespace RibbonKit.Tests;

public sealed class BackstageAutomationTests
{
    [Fact]
    public void Structure_notifications_do_not_reenter_peer_discovery() => Sta.Run(() =>
    {
        var app = Sta.UseApplication(); ThemeManager.Apply(app, RibbonTheme.Office2024);
        var window = new Window { Content = new Grid(), Width = 800, Height = 450,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        using var ready = new ManualResetEventSlim(); using var stop = new ManualResetEventSlim();
        var failures = new List<string>(); int uiThread = Environment.CurrentManagedThreadId, structureEvents = 0;
        EventHandler<FirstChanceExceptionEventArgs> firstChance = (_, e) =>
        {
            if (Environment.CurrentManagedThreadId == uiThread && e.Exception is InvalidOperationException
                && e.Exception.Message.Contains("Recursive call", StringComparison.OrdinalIgnoreCase))
                failures.Add(e.Exception.ToString());
        };
        Task? client = null;
        AppDomain.CurrentDomain.FirstChanceException += firstChance;
        try
        {
            window.Show(); Sta.Drain();
            var handle = new WindowInteropHelper(window).Handle;
            client = Task.Run(() =>
            {
                var element = AutomationElement.FromHandle(handle);
                Assert.Equal(Environment.ProcessId, element.Current.ProcessId);
                StructureChangedEventHandler handler = (_, _) => Interlocked.Increment(ref structureEvents);
                Uia.AddStructureChangedEventHandler(element, TreeScope.Subtree, handler);
                ready.Set();
                try
                {
                    var watch = Stopwatch.StartNew();
                    while (!stop.IsSet && watch.Elapsed < TimeSpan.FromSeconds(15))
                    {
                        try { ReadMetadata(element); }
                        catch (ElementNotAvailableException) { } // A page may be replaced during traversal.
                        Thread.Sleep(10);
                    }
                    Assert.True(stop.IsSet);
                }
                finally { Uia.RemoveStructureChangedEventHandler(element, handler); }
            });
            PumpUntil(() => ready.IsSet || client.IsCompleted);
            Assert.True(ready.IsSet, client.Exception?.ToString());
            var stage = new Backstage();
            stage.Items.Add(new BackstageTabItem { Header = "Home", Content = new Button { Content = "Open" } });
            stage.Items.Add(new BackstageTabItem { Header = "New", Content = new Button { Content = "Create" } });
            stage.SelectedIndex = 0; window.Content = stage;
            stage.ApplyTemplate(); stage.Measure(new Size(800, 450)); stage.Arrange(new Rect(0, 0, 800, 450));
            // Query the newly realized item peer before the queued automation
            // layout update has established its children/parent connections.
            UIElementAutomationPeer.CreatePeerForElement(stage)!.GetChildren();
            window.UpdateLayout();
            var root = UIElementAutomationPeer.CreatePeerForElement(window)!;
            Tree(root).ToArray();
            Assert.Same(stage.Items[0], stage.SelectedItem);
            var selection = Assert.IsAssignableFrom<ISelectionProvider>(
                UIElementAutomationPeer.CreatePeerForElement(stage)!.GetPattern(PatternInterface.Selection));
            Assert.Equal("Home", Assert.Single(selection.GetSelection()).GetPropertyValue(AutomationElementIdentifiers.NameProperty.Id));
            for (int i = 0; i < 3; i++)
            {
                var added = new BackstageTabItem { Header = "Added", Content = new Button { Content = "Added action" } };
                stage.Items.Add(added); window.UpdateLayout(); Tree(root).ToArray();
                stage.SelectedItem = added; Sta.Drain(); window.UpdateLayout(); Tree(root).ToArray();
                Assert.Equal("Added", Assert.Single(selection.GetSelection()).GetPropertyValue(AutomationElementIdentifiers.NameProperty.Id));
                stage.SelectedIndex = 0; stage.Items.Remove(added); Sta.Drain(); window.UpdateLayout(); Tree(root).ToArray();
                Assert.Same(stage.Items[0], stage.SelectedItem);
                Assert.Equal("Home", Assert.Single(selection.GetSelection()).GetPropertyValue(AutomationElementIdentifiers.NameProperty.Id));
            }
            PumpUntil(() => Volatile.Read(ref structureEvents) > 0);
            Assert.Empty(failures);
        }
        finally
        {
            stop.Set();
            try
            {
                if (client != null) { PumpUntil(() => client.IsCompleted); client.GetAwaiter().GetResult(); }
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= firstChance;
                window.Close(); Sta.Drain(); Sta.ResetApplication();
            }
        }

        static void PumpUntil(Func<bool> condition)
        {
            var watch = Stopwatch.StartNew(); var frame = new DispatcherFrame();
            var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(20) };
            timer.Tick += (_, _) => { if (condition() || watch.Elapsed > TimeSpan.FromSeconds(10)) frame.Continue = false; };
            timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
            Assert.True(condition(), "Owned UIA client did not complete in time.");
        }

        static void ReadMetadata(AutomationElement element)
        {
            _ = element.Current.Name; _ = element.Current.ControlType; _ = element.Current.IsEnabled;
            element.TryGetCurrentPattern(VirtualizedItemPattern.Pattern, out _);
            for (var child = TreeWalker.ControlViewWalker.GetFirstChild(element); child != null;
                child = TreeWalker.ControlViewWalker.GetNextSibling(child)) ReadMetadata(child);
        }
    });

    [Theory]
    [InlineData(RibbonTheme.Office2024, RibbonBackstageDesign.Modern, false)]
    [InlineData(RibbonTheme.Office2024, RibbonBackstageDesign.Modern, true)]
    [InlineData(RibbonTheme.Office2010, RibbonBackstageDesign.Classic2010, false)]
    [InlineData(RibbonTheme.Office2010, RibbonBackstageDesign.Classic2010, true)]
    [InlineData(RibbonTheme.Office2007, RibbonBackstageDesign.Classic2007, false)]
    [InlineData(RibbonTheme.Office2007, RibbonBackstageDesign.Classic2007, true)]
    [InlineData(RibbonTheme.CrystalLight, RibbonBackstageDesign.CrystalFloating, false)]
    [InlineData(RibbonTheme.CrystalLight, RibbonBackstageDesign.CrystalFloating, true)]
    [InlineData(RibbonTheme.CrystalLight, RibbonBackstageDesign.CrystalSidebar, false)]
    [InlineData(RibbonTheme.CrystalLight, RibbonBackstageDesign.CrystalSidebar, true)]
    public void Page_content_keeps_native_actions_selection_bindings_and_geometry(
        RibbonTheme theme, RibbonBackstageDesign design, bool rtl) => Sta.Run(() =>
    {
        var app = Sta.UseApplication(); ThemeManager.Apply(app, theme);
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var state = new State();
        var action = new Button(); action.SetBinding(ContentControl.ContentProperty, new Binding(nameof(State.Title)) { Source = state });
        action.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(State.Enabled)) { Source = state });
        AutomationProperties.SetAutomationId(action, "page-action");
        var body = new ScrollViewer { Content = new StackPanel { Children = { new TextBox { Text = "Page editor" }, action } } };
        var first = new BackstageTabItem { Header = "Recent", Content = body };
        var alignment = new Binding(nameof(State.Alignment)) { Source = state };
        first.SetBinding(Control.HorizontalContentAlignmentProperty, alignment);
        var other = new Button { Content = "Other action" }; AutomationProperties.SetAutomationId(other, "other-action");
        var second = new BackstageTabItem { Header = "Other", Content = other };
        var stage = new Backstage { Design = design }; stage.Items.Add(first); stage.Items.Add(second);
        state.Selected = first;
        var selection = new Binding(nameof(State.Selected)) { Source = state, Mode = BindingMode.TwoWay };
        stage.SetBinding(Selector.SelectedItemProperty, selection);
        var ribbon = new Ribbon { Backstage = stage, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        int count = 0; action.Command = new Callback(() => { count++; ribbon.IsBackstageOpen = false; });
        var window = new RibbonWindow { Content = ribbon, Width = 1000, Height = 650,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); ribbon.IsBackstageOpen = true; Layout();
            double width = body.ActualWidth; Assert.True(width > 100);
            var root = UIElementAutomationPeer.CreatePeerForElement(stage)!;
            var regions = root.GetChildren()!;
            Assert.Equal(new[] { AutomationControlType.Group, AutomationControlType.Pane },
                regions.Select(child => child.GetAutomationControlType()));
            var navigation = regions[0]; var page = regions[1];
            Assert.Equal("File navigation", navigation.GetName());
            Assert.Equal("Recent", page.GetName());
            Assert.All(navigation.GetChildren()!, child => Assert.Equal(AutomationControlType.TabItem, child.GetAutomationControlType()));
            Assert.DoesNotContain(Tree(navigation), child => child.GetAutomationId() == "page-action");
            Assert.Single(Tree(page), child => child.GetAutomationId() == "page-action");
            Assert.False(navigation.IsKeyboardFocusable()); Assert.False(page.IsKeyboardFocusable());
            var peer = Find("page-action");
            Assert.Equal("Open document", peer.GetName()); Assert.True(peer.IsEnabled());
            Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke)).Invoke(); Layout();
            Assert.Equal(1, count); Assert.False(ribbon.IsBackstageOpen);
            ribbon.IsBackstageOpen = true; Layout();
            state.Enabled = false; state.Title = "Renamed document"; Layout();
            Assert.False(Find("page-action").IsEnabled()); Assert.Equal("Renamed document", Find("page-action").GetName());
            state.Enabled = true; state.Alignment = HorizontalAlignment.Right; Layout();
            var item = Assert.Single(navigation.GetChildren()!, child => child.GetName() == "Other");
            var itemProvider = Assert.IsAssignableFrom<ISelectionItemProvider>(item.GetPattern(PatternInterface.SelectionItem));
            Assert.NotNull(itemProvider.SelectionContainer);
            itemProvider.Select(); Layout();
            Assert.Same(second, state.Selected); Find("other-action");
            Assert.Equal("Other", page.GetName());
            Assert.DoesNotContain(Tree(root), child => child.GetAutomationId() == "page-action");
            state.Selected = first; Layout(); Find("page-action");
            Assert.Equal("Recent", page.GetName());
            first.Header = "Renamed page"; Layout();
            Assert.Equal("Renamed page", page.GetName());
            Assert.Equal(width, body.ActualWidth, 2);
            Assert.Same(alignment, BindingOperations.GetBinding(first, Control.HorizontalContentAlignmentProperty));
            Assert.Same(selection, BindingOperations.GetBinding(stage, Selector.SelectedItemProperty));
        }
        finally { window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }

        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
        AutomationPeer Find(string id)
        {
            var peer = UIElementAutomationPeer.CreatePeerForElement(stage)!;
            peer.ResetChildrenCache(); // Direct queries have no native structure-change listener.
            return Assert.Single(Tree(peer), child => child.GetAutomationId() == id);
        }
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Authored_template_keeps_page_discovery_without_duplicate_leaves(bool standardPart) => Sta.Run(() =>
    {
        var app = Sta.UseApplication(); ThemeManager.Apply(app, RibbonTheme.Office2024);
        string partName = standardPart ? "x:Name='PART_SelectedContentHost'" : string.Empty;
        var stage = new Backstage
        {
            Template = (ControlTemplate)XamlReader.Parse($$"""
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:rk="clr-namespace:RibbonKit.Controls;assembly=RibbonKit" TargetType="{x:Type rk:Backstage}">
                    <DockPanel><ItemsPresenter DockPanel.Dock="Left" />
                        <Border x:Name="ContentArea"><ContentPresenter {{partName}} ContentSource="SelectedContent" /></Border>
                    </DockPanel>
                </ControlTemplate>
                """)
        };
        var button = new Button { Content = "Authored action" };
        AutomationProperties.SetAutomationId(button, "authored-action");
        stage.Items.Add(new BackstageTabItem { Header = "Authored", Content = button,
            HorizontalContentAlignment = HorizontalAlignment.Stretch });
        stage.SelectedIndex = 0;
        var window = new Window { Content = stage, Width = 800, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); window.UpdateLayout();
            var peer = UIElementAutomationPeer.CreatePeerForElement(stage)!;
            var action = Assert.Single(Tree(peer), child => child.GetAutomationId() == "authored-action");
            Assert.IsAssignableFrom<IInvokeProvider>(action.GetPattern(PatternInterface.Invoke));
            Assert.Equal(standardPart ? 0 : 1,
                peer.GetChildren()!.Count(child => child.GetAutomationControlType() == AutomationControlType.Pane));
        }
        finally { window.Close(); Sta.Drain(); Sta.ResetApplication(); }
    });

    [Fact]
    public void Navigation_group_uses_live_localization_without_changing_existing_string_values() => Sta.Run(() =>
    {
        var app = Sta.UseApplication(); ThemeManager.Apply(app, RibbonTheme.Office2024);
        var previous = RibbonLocalization.Provider;
        var stage = new Backstage(); stage.Items.Add(new BackstageTabItem { Header = "Page", Content = new Button() });
        stage.SelectedIndex = 0;
        var window = new Window { Content = stage, Width = 800, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); window.UpdateLayout();
            var root = UIElementAutomationPeer.CreatePeerForElement(stage)!;
            var navigation = Assert.Single(root.GetChildren()!, child => child.GetAutomationControlType() == AutomationControlType.Group);
            Assert.Equal("File navigation", navigation.GetName());
            RibbonLocalization.Provider = new NavigationStrings(); Sta.Drain();
            Assert.Equal("Localized file navigation", navigation.GetName());
            Assert.Equal(90, (int)RibbonString.RemoveTab);
        }
        finally { RibbonLocalization.Provider = previous; window.Close(); Sta.Drain(); Sta.ResetApplication(); }
    });

    private static IEnumerable<AutomationPeer> Tree(AutomationPeer peer)
    {
        yield return peer;
        foreach (var child in peer.GetChildren() ?? []) foreach (var nested in Tree(child)) yield return nested;
    }
    private sealed class Callback(Action action) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
    private sealed class NavigationStrings : IRibbonLocalizationProvider
    {
        public string? GetString(RibbonString key, System.Globalization.CultureInfo culture) =>
            key == RibbonString.BackstageNavigation ? "Localized file navigation" : null;
    }
    private sealed class State : INotifyPropertyChanged
    {
        private BackstageTabItem? _selected;
        private string _title = "Open document";
        private bool _enabled = true;
        private HorizontalAlignment _alignment = HorizontalAlignment.Center;
        public event PropertyChangedEventHandler? PropertyChanged;
        public BackstageTabItem? Selected { get => _selected; set { _selected = value; PropertyChanged?.Invoke(this, new(nameof(Selected))); } }
        public string Title { get => _title; set { _title = value; PropertyChanged?.Invoke(this, new(nameof(Title))); } }
        public bool Enabled { get => _enabled; set { _enabled = value; PropertyChanged?.Invoke(this, new(nameof(Enabled))); } }
        public HorizontalAlignment Alignment { get => _alignment; set { _alignment = value; PropertyChanged?.Invoke(this, new(nameof(Alignment))); } }
    }
}
