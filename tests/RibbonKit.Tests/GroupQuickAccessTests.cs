using System.Reflection;
using System.Text.Json;
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

namespace RibbonKit.Tests;

public sealed class GroupQuickAccessTests
{
    [Theory]
    [InlineData(RibbonDensity.Compact)]
    [InlineData(RibbonDensity.Touch)]
    public void Live_preview_updates_values_and_checked_state_without_changing_slots_and_detaches_on_close(RibbonDensity density) => Sta.Run(() =>
    {
        var combo = new RibbonComboBox { InputWidth = 130, Items = { "AAAA", "BBBB" }, SelectedIndex = 0 };
        var toggle = new RibbonToggleButton { Header = "Toggle" };
        var editor = new TextBox { Width = 90, Text = "Old" };
        var group = new RibbonGroup { Header = "Tools", Items = { combo, toggle, editor } };
        var ribbon = RibbonFor(group); ribbon.Density = density; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        ribbon.AddToQuickAccess(group); using var host = new Host(ribbon);
        var normal = Part<Decorator>(group, "PART_NormalHost"); var original = normal.Child; var size = group.RenderSize;
        var copy = (RibbonDropDownButton)ribbon.QuickAccessItems[0]; copy.IsDropDownOpen = true; host.Layout();
        var preview = Assert.IsType<Image>(normal.Child); var before = Pixels(preview);
        combo.SelectedIndex = 1; host.Layout(); var selected = Pixels(preview);
        Assert.False(before.SequenceEqual(selected), "The source choice must update while the popup is still open.");
        toggle.IsChecked = true; host.Layout(); var checkedPixels = Pixels(preview);
        Assert.False(selected.SequenceEqual(checkedPixels), "The source checked paint must update while the popup is still open.");
        editor.Text = "Changed"; host.Layout();
        Assert.True(copy.IsDropDownOpen); Assert.Same(preview, normal.Child); Assert.Equal(size, group.RenderSize);
        Assert.False(checkedPixels.SequenceEqual(Pixels(preview)), "The source text must update while the popup is still open.");
        var stable = Pixels(preview, "stable");
        editor.Margin = new Thickness(20, 0, 0, 0); host.Layout();
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(50) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; }; timer.Start(); Dispatcher.PushFrame(frame);
        if (Environment.GetEnvironmentVariable("RIBBONKIT_GROUP_QAT_DIAGNOSTICS") is { Length: > 0 } directory)
        {
            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen()) dc.DrawRectangle(new VisualBrush(editor) { AutoLayoutContent = false, ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, 500, 150) }, null, new Rect(0, 0, 500, 150));
            var bitmap = new RenderTargetBitmap(500, 150, 96, 96, PixelFormats.Pbgra32); bitmap.Render(drawingVisual);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = System.IO.File.Create(System.IO.Path.Combine(directory, "brush-wide.png")); encoder.Save(stream);
        }
        Assert.True(stable.SequenceEqual(Pixels(preview, "moved")), "Moving a native popup control must preserve its full paint in the original preview slot.");
        var brushes = LiveBrushes(((DrawingImage)preview.Source).Drawing).ToArray();
        Assert.NotEmpty(brushes); Assert.All(brushes, b => Assert.NotNull(b.Visual));
        copy.IsDropDownOpen = false; host.Layout();
        Assert.Same(original, normal.Child); Assert.All(brushes, b => Assert.Null(b.Visual));
        Assert.Equal(1, combo.SelectedIndex); Assert.True(toggle.IsChecked); Assert.Equal("Changed", editor.Text);
    });

    private static byte[] Pixels(FrameworkElement element, string? name = null)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element); byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        if (name != null && Environment.GetEnvironmentVariable("RIBBONKIT_GROUP_QAT_DIAGNOSTICS") is { Length: > 0 } directory)
        {
            System.IO.Directory.CreateDirectory(directory);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = System.IO.File.Create(System.IO.Path.Combine(directory, name + ".png")); encoder.Save(stream);
        }
        return pixels;
    }
    private static IEnumerable<VisualBrush> LiveBrushes(Drawing drawing)
    {
        if (drawing is GeometryDrawing { Brush: VisualBrush brush }) yield return brush;
        if (drawing is DrawingGroup group) foreach (Drawing child in group.Children)
            foreach (var item in LiveBrushes(child)) yield return item;
    }

    [Fact]
    public void Only_qat_catalog_and_page_offer_groups_while_both_offer_individual_commands() => Sta.Run(() =>
    {
        var group = Group(); var ribbon = RibbonFor(group);
        var qat = new RibbonQuickAccessPage { Ribbon = ribbon };
        var customize = new RibbonCustomizePage { Ribbon = ribbon };
        using var host = new Host(new StackPanel { Children = { ribbon, qat, customize } });
        Assert.DoesNotContain(RibbonCommandCatalog.CollectAvailable(ribbon), e => e.Control is RibbonGroup);
        var available = Part<ListBox>(qat, "PART_AvailableList");
        var entry = Assert.Single(available.Items.Cast<RibbonCommandEntry>(), e => ReferenceEquals(e.Control, group));
        Assert.Equal("Home › Tools", entry.DisplayName); Assert.Same(group.Icon, entry.Icon);
        Assert.DoesNotContain(Part<ListBox>(customize, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), e => e.Control is RibbonGroup);
        Assert.Contains(Part<ListBox>(customize, "PART_AvailableList").Items.Cast<RibbonCommandEntry>(), e => ReferenceEquals(e.Control, group.Items[0]));
        available.SelectedItem = entry; Part<ButtonBase>(qat, "PART_AddButton").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        Assert.IsType<RibbonGroupQuickAccessProxy>(Assert.Single(ribbon.QuickAccessItems));
        Assert.False(ribbon.AddToQuickAccess(group));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Original_content_from_inactive_or_collapsed_group_keeps_bindings_resources_and_events(bool collapsed) => Sta.Run(() =>
    {
        var group = Group(); var ribbon = RibbonFor(group);
        if (collapsed) for (int i = 0; i < 8; i++) group.Items.Add(new RibbonButton { Header = "Wide command " + i });
        var other = new RibbonTab { Header = "Other" }; other.Groups.Add(new RibbonGroup()); ribbon.Tabs.Add(other);
        ribbon.SelectedTab = collapsed ? ribbon.Tabs[0] : other;
        var model = new Model(); group.DataContext = model; group.Resources["LocalPaint"] = Brushes.Red;
        var button = (RibbonButton)group.Items[0]; button.SetBinding(RibbonButton.HeaderProperty, new Binding(nameof(Model.Caption)));
        button.SetResourceReference(Control.ForegroundProperty, "LocalPaint");
        int clicks = 0, launches = 0; button.Click += (_, _) => clicks++; group.DialogLauncherClick += (_, _) => launches++;
        group.ShowDialogLauncher = true; ribbon.AddToQuickAccess(group);
        using var host = new Host(ribbon);
        if (collapsed) { host.Window.Width = 300; host.Layout(); Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState); }
        var copy = Assert.IsType<RibbonGroupQuickAccessProxy>(ribbon.QuickAccessItems[0]);
        copy.IsDropDownOpen = true; host.Layout();
        Assert.True(copy.IsDropDownOpen); Assert.NotNull(copy.GroupContent);
        Assert.Same(group, button.Parent); Assert.Same(model, button.DataContext); Assert.Equal("Live", button.Header);
        Assert.Same(Brushes.Red, button.Foreground); Assert.True(button.ActualWidth > 0);
        Assert.Same(copy.GroupContent, group.QuickAccessContent);
        var content = copy.GroupContent; var normal = Part<Decorator>(group, "PART_NormalHost");
        Assert.NotSame(content, normal.Child);
        var expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(UIElementAutomationPeer.CreatePeerForElement(copy).GetPattern(PatternInterface.ExpandCollapse));
        Assert.Equal(System.Windows.Automation.ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        group.DialogLauncher!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout(); Assert.Equal(1, launches);
        Assert.False(copy.IsDropDownOpen); Assert.Same(content, normal.Child);
        copy.IsDropDownOpen = true; host.Layout(); button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout();
        Assert.Equal(1, clicks); Assert.False(copy.IsDropDownOpen); Assert.Null(group.QuickAccessContent);
        Assert.True(BindingOperations.IsDataBound(button, RibbonButton.HeaderProperty));
        Assert.Equal(collapsed ? RibbonGroupSizeState.Collapsed : RibbonGroupSizeState.Large, group.SizeState);
    });

    [Theory]
    [InlineData(RibbonTheme.Office2024, RibbonDensity.Compact, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.CrystalLight, RibbonDensity.Touch, FlowDirection.RightToLeft)]
    public void Visible_group_keeps_its_footprint_and_restores_after_resize_and_template_changes(RibbonTheme theme, RibbonDensity density, FlowDirection flow) => Sta.Run(() =>
    {
        var group = Group(); var ribbon = RibbonFor(group); ribbon.Density = density; ribbon.FlowDirection = flow;
        using var host = new Host(ribbon); ThemeManager.Apply(Application.Current, theme); host.Layout();
        ribbon.AddToQuickAccess(group); host.Layout(); var copy = (RibbonGroupQuickAccessProxy)ribbon.QuickAccessItems[0];
        var normal = Part<Decorator>(group, "PART_NormalHost"); var live = normal.Child; var size = group.RenderSize;
        copy.IsDropDownOpen = true; host.Layout();
        Assert.True(copy.IsDropDownOpen); Assert.IsType<Image>(normal.Child); Assert.Equal(size, group.RenderSize);
        Assert.Equal(density, Ribbon.GetDensity((DependencyObject)copy.GroupContent!));
        group.SetSizeState(RibbonGroupSizeState.Collapsed); host.Layout();
        Assert.False(copy.IsDropDownOpen); Assert.Same(live, normal.Child);
        copy.IsDropDownOpen = true; host.Layout();
        group.OnApplyTemplate(); host.Layout(); Assert.False(copy.IsDropDownOpen); Assert.Same(live, normal.Child);
        copy.IsDropDownOpen = true; host.Layout();
        copy.OnApplyTemplate(); host.Layout(); Assert.False(copy.IsDropDownOpen); Assert.Same(live, normal.Child);
    });

    [Fact]
    public void Native_flyout_and_two_copies_transfer_one_content_tree_and_close_nested_controls() => Sta.Run(() =>
    {
        var group = Group(); var combo = new RibbonComboBox { ItemsSource = new[] { "A", "B" }, SelectedIndex = 0 };
        var gallery = new InRibbonGallery { Header = "Styles", Width = 160, ItemsSource = new[] { "A", "B" } };
        var menu = new RibbonDropDownButton { Header = "Menu" }; menu.Items.Add(new RibbonMenuItem { Header = "Run" });
        group.Items.Add(combo); group.Items.Add(gallery); group.Items.Add(menu);
        var ribbon = RibbonFor(group); ribbon.AddToQuickAccess(group);
        var first = (RibbonGroupQuickAccessProxy)ribbon.QuickAccessItems[0];
        var second = (RibbonGroupQuickAccessProxy)ribbon.CreateCommandProxy(group, RibbonControlSize.Medium);
        var galleryCopy = (RibbonDropDownButton)ribbon.CreateCommandProxy(gallery, RibbonControlSize.Medium);
        using var host = new Host(new StackPanel { Children = { ribbon, second, galleryCopy } });
        galleryCopy.IsDropDownOpen = true; host.Layout(); Assert.True(gallery.IsQuickAccessOpen);
        first.IsDropDownOpen = true; host.Layout(); var content = first.GroupContent;
        Assert.False(galleryCopy.IsDropDownOpen); Assert.False(gallery.IsQuickAccessOpen);
        second.IsDropDownOpen = true; host.Layout(); Assert.False(first.IsDropDownOpen); Assert.Same(content, second.GroupContent);
        menu.IsDropDownOpen = true; host.Layout(); Assert.True(second.IsDropDownOpen); Assert.True(menu.IsDropDownOpen);
        combo.IsDropDownOpen = true; host.Layout(); combo.SelectedIndex = 1;
        gallery.IsDropDownOpen = true; host.Layout();
        second.IsDropDownOpen = false; host.Layout();
        Assert.False(menu.IsDropDownOpen); Assert.False(combo.IsDropDownOpen); Assert.False(gallery.IsDropDownOpen);
        Assert.Equal(1, combo.SelectedIndex); Assert.Same(content, Part<Decorator>(group, "PART_NormalHost").Child);
        host.Window.Width = 300; host.Layout(); Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
        group.CollapsedButton!.IsChecked = true; host.Layout();
        Assert.Same(content, group.FlyoutContent);
        first.IsDropDownOpen = true; host.Layout(); Assert.False(group.CollapsedButton.IsChecked); Assert.Null(group.FlyoutContent);
        Assert.Same(content, first.GroupContent);
        group.CollapsedButton.IsChecked = true; host.Layout(); Assert.False(first.IsDropDownOpen); Assert.Same(content, group.FlyoutContent);
        group.CollapsedButton.IsChecked = false; host.Layout();
    });

    [Fact]
    public void Group_icon_caption_and_enabled_state_remain_live_with_a_shared_fallback_glyph() => Sta.Run(() =>
    {
        var group = Group(); group.Icon = null; var ribbon = RibbonFor(group); ribbon.AddToQuickAccess(group);
        var copy = (RibbonDropDownButton)Assert.Single(ribbon.QuickAccessItems); using var host = new Host(ribbon);
        Assert.NotNull(copy.Icon); var fallback = copy.Icon;
        group.Header = "Changed"; group.Icon = new DrawingImage(); host.Layout();
        Assert.Equal("Changed", copy.Header); Assert.Equal("Changed", copy.ScreenTipTitle); Assert.Same(group.Icon, copy.Icon);
        group.Icon = null; host.Layout(); Assert.Same(fallback, copy.Icon);
        group.IsEnabled = false; Assert.False(copy.IsEnabled); group.IsEnabled = true; Assert.True(copy.IsEnabled);
    });

    [Fact]
    public void Removal_clear_and_disabling_return_content_without_waiting_for_popup_closed() => Sta.Run(() =>
    {
        var group = Group(); var ribbon = RibbonFor(group); ribbon.AddToQuickAccess(group);
        var copy = (RibbonGroupQuickAccessProxy)ribbon.QuickAccessItems[0];
        using var host = new Host(ribbon); copy.IsDropDownOpen = true; host.Layout(); var content = copy.GroupContent;
        ribbon.QuickAccessItems.Remove(copy); Assert.Null(group.QuickAccessContent); Assert.False(copy.IsDropDownOpen);
        Assert.Same(content, Part<Decorator>(group, "PART_NormalHost").Child);
        ribbon.AddToQuickAccess(group); host.Layout(); copy = (RibbonGroupQuickAccessProxy)ribbon.QuickAccessItems[0];
        copy.IsDropDownOpen = true; host.Layout(); group.IsEnabled = false; host.Layout();
        Assert.False(copy.IsDropDownOpen); Assert.False(copy.IsEnabled); Assert.Null(group.QuickAccessContent);
        group.IsEnabled = true; copy.IsDropDownOpen = true; host.Layout(); ribbon.QuickAccessItems.Clear();
        Assert.Null(group.QuickAccessContent); Assert.False(copy.IsDropDownOpen);
    });

    [Fact]
    public void Overflow_dismissal_and_removal_return_group_and_native_gallery_presenter() => Sta.Run(() =>
    {
        var group = Group(); var gallery = new InRibbonGallery { Width = 180, ItemsSource = new[] { "A", "B" } }; group.Items.Add(gallery);
        var ribbon = RibbonFor(group); ribbon.QuickAccessMaxWidth = 50;
        for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        ribbon.AddToQuickAccess(group); using var host = new Host(ribbon);
        var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost"); var bar = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
        bar.OpenOverflow(); host.Layout(); var entry = Assert.Single(bar.OverflowEntries.OfType<RibbonGroupQuickAccessProxy>());
        entry.IsDropDownOpen = true; host.Layout(); var content = entry.GroupContent;
        gallery.IsDropDownOpen = true; host.Layout(); bar.CloseOverflow(); host.Layout();
        Assert.False(entry.IsDropDownOpen); Assert.False(gallery.IsDropDownOpen);
        Assert.Same(content, Part<Decorator>(group, "PART_NormalHost").Child);
        Assert.IsType<ItemsPresenter>(Part<ScrollViewer>(gallery, "PART_ScrollViewer").Content);
        bar.OpenOverflow(); host.Layout(); entry.IsDropDownOpen = true; host.Layout();
        ribbon.QuickAccessItems.RemoveAt(ribbon.QuickAccessItems.Count - 1);
        Assert.False(entry.IsDropDownOpen); Assert.Null(group.QuickAccessContent); host.Layout();
    });

    [Fact]
    public void Group_order_duplicates_custom_group_rebuild_and_reset_round_trip() => Sta.Run(() =>
    {
        var group = Group(); var ribbon = RibbonFor(group); var button = (RibbonButton)group.Items[0]; Ribbon.SetCommandId(button, "run");
        string baseline = RibbonCustomizationSerializer.Serialize(ribbon);
        var custom = new RibbonGroup { Header = "Custom", Layout = RibbonGroupLayout.Stacked };
        Ribbon.SetIsCustom(custom, true); Ribbon.SetCommandId(custom, "custom:group"); custom.Items.Add(ribbon.CreateCommandProxy(button, RibbonControlSize.Medium));
        ribbon.Tabs[0].Groups.Add(custom); ribbon.AddToQuickAccess(group); ribbon.AddToQuickAccess(custom); ribbon.AddToQuickAccess(button);
        ribbon.QuickAccessItems.Move(2, 0); string json = RibbonCustomizationSerializer.Serialize(ribbon);
        RibbonCustomizationSerializer.Apply(ribbon, json);
        Assert.Same(button, Ribbon.GetQuickAccessSource((DependencyObject)ribbon.QuickAccessItems[0]));
        Assert.Same(group, Ribbon.GetQuickAccessSource((DependencyObject)ribbon.QuickAccessItems[1]));
        var rebuilt = ribbon.Tabs[0].Groups[1]; Assert.NotSame(custom, rebuilt);
        Assert.Same(rebuilt, Ribbon.GetQuickAccessSource((DependencyObject)ribbon.QuickAccessItems[2]));
        RibbonCustomizationSerializer.Apply(ribbon, "{\"Tabs\":[],\"QuickAccess\":[{\"Ref\":\"tools\"},{\"Ref\":\"tools\"},{\"Ref\":\"missing\"}]}");
        Assert.Single(ribbon.QuickAccessItems);
        RibbonCustomizationSerializer.Apply(ribbon, baseline); Assert.Empty(ribbon.QuickAccessItems); Assert.Single(ribbon.Tabs[0].Groups);
        Ribbon.SetCommandId(group, null); ribbon.AddToQuickAccess(group); json = RibbonCustomizationSerializer.Serialize(ribbon);
        Assert.Contains("auto:group:home/Tools#0", json);
        RibbonCustomizationSerializer.Apply(ribbon, json); Assert.Same(group, Ribbon.GetQuickAccessSource((DependencyObject)Assert.Single(ribbon.QuickAccessItems)));
    });

    [Fact]
    public void Merged_group_is_parked_closed_and_excluded_from_persistence() => Sta.Run(() =>
    {
        var group = Group(); var ribbon = RibbonFor(new RibbonGroup());
        var source = new RibbonMergeSource(); source.Groups.Add(new RibbonGroupContribution { TargetTabId = "home", Group = group });
        ribbon.Merge(source); ribbon.AddToQuickAccess(group);
        var copy = (RibbonGroupQuickAccessProxy)Assert.Single(ribbon.QuickAccessItems);
        using var host = new Host(ribbon); copy.IsDropDownOpen = true; host.Layout();
        using (var json = JsonDocument.Parse(RibbonCustomizationSerializer.Serialize(ribbon))) Assert.Equal(0, json.RootElement.GetProperty("QuickAccess").GetArrayLength());
        ribbon.Unmerge(source); host.Layout(); Assert.False(copy.IsEnabled); Assert.False(copy.IsDropDownOpen); Assert.Null(group.QuickAccessContent);
        ribbon.Merge(source); host.Layout(); Assert.True(copy.IsEnabled); copy.IsDropDownOpen = true; host.Layout(); Assert.NotNull(copy.GroupContent);
    });

    [Theory]
    [InlineData(false, FlowDirection.LeftToRight)]
    [InlineData(true, FlowDirection.RightToLeft)]
    public void Group_KeyTips_badge_native_commands_and_nested_combo_keeps_the_group_open(bool overflow, FlowDirection flow) => Sta.Run(() =>
    {
        var group = Group(); var button = (RibbonButton)group.Items[0]; KeyTip.SetKeys(button, "R");
        var combo = new RibbonComboBox { Header = "Choice", ItemsSource = new[] { "A", "B" } }; KeyTip.SetKeys(combo, "C"); group.Items.Add(combo);
        var ribbon = RibbonFor(group); ribbon.FlowDirection = flow;
        if (overflow)
        {
            ribbon.QuickAccessMaxWidth = 50;
            for (int i = 0; i < 3; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save", Size = RibbonControlSize.Small });
        }
        ribbon.AddToQuickAccess(group); var copy = (RibbonDropDownButton)ribbon.QuickAccessItems[^1]; KeyTip.SetKeys(copy, "G");
        int clicks = 0; button.Click += (_, _) => clicks++;
        using var host = new Host(ribbon); host.Window.Activate(); host.Layout();
        var toolbar = Part<RibbonQuickAccessToolBar>(Part<RibbonTabControl>(ribbon, "TabControlHost"), "QatTabRowHost");
        if (overflow) KeyTip.SetKeys(toolbar.OverflowButton!, "O");
        OpenGroup();
        Assert.Contains(AdornerLayer.GetAdornerLayer(button)?.GetAdorners(button) ?? Array.Empty<Adorner>(), a => a is KeyTipAdorner);
        KeyTipPress(Key.C); Assert.True(combo.IsDropDownOpen); Assert.True(copy.IsDropDownOpen);
        combo.IsDropDownOpen = false; copy.IsDropDownOpen = false; if (overflow) toolbar.CloseOverflow(); host.Layout();
        OpenGroup(); KeyTipPress(Key.R); Assert.Equal(1, clicks); Assert.False(copy.IsDropDownOpen); Assert.Null(group.QuickAccessContent);
        void OpenGroup()
        {
            KeyTipPress(Key.F10);
            if (overflow) { KeyTipPress(Key.O); copy = Assert.Single(toolbar.OverflowEntries.OfType<RibbonDropDownButton>()); }
            KeyTipPress(overflow ? Key.T : Key.G); Assert.True(copy.IsDropDownOpen);
        }
        void KeyTipPress(Key key)
        {
            host.Window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(host.Window)!, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            host.Layout();
        }
    });

    private sealed class Model { public string Caption { get; } = "Live"; }
    private static RibbonGroup Group()
    {
        var group = new RibbonGroup { Header = "Tools", Icon = new DrawingImage() }; Ribbon.SetCommandId(group, "tools");
        group.Items.Add(new RibbonButton { Header = "Run" }); return group;
    }
    private static Ribbon RibbonFor(RibbonGroup group)
    {
        var tab = new RibbonTab { Header = "Home" }; Ribbon.SetCommandId(tab, "home"); tab.Groups.Add(group);
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow }; ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab; return ribbon;
    }
    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private sealed class Host : IDisposable
    {
        internal Window Window { get; }
        private readonly RibbonAnimationLevel _motion = RibbonAnimation.GlobalLevel;
        internal Host(object content)
        {
            var app = Sta.UseApplication(); ThemeManager.Apply(app, RibbonTheme.Office2024); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Window = new Window { Content = content, Width = 900, Height = 650, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal void Layout() { Sta.Drain(DispatcherPriority.ApplicationIdle); Window.UpdateLayout(); Sta.Drain(DispatcherPriority.ApplicationIdle); }
        public void Dispose() { Window.Close(); Sta.Drain(DispatcherPriority.ApplicationIdle); RibbonAnimation.GlobalLevel = _motion; Sta.ResetApplication(); }
    }
}
