using System.IO;
using System.Reflection;
using System.Collections;
using System.Xml.Linq;
using System.Windows.Markup;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;
using KeyTipService = RibbonKit.Controls.KeyTipService;

namespace RibbonKit.Tests;

public sealed class SimplifiedRibbonTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mode_toggle_inside_group_closes_flyout_after_click_but_ordinary_toggles_keep_it_open(bool simplified) => Sta.Run(() =>
    {
        using var host = new Host(initialSimplified: simplified);
        var mode = new RibbonToggleButton { Header = "Simplified", IsChecked = simplified };
        host.Group.Items.Add(mode);
        mode.Checked += (_, _) => host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        mode.Unchecked += (_, _) => host.Ribbon.LayoutMode = RibbonLayoutMode.Classic;
        bool clickFinishedInFlyout = false;
        mode.Click += (_, _) => clickFinishedInFlyout = host.GroupPopup.IsOpen
            && ((Border)host.Group.Template.FindName("PART_PopupHost", host.Group)).IsAncestorOf(mode);
        host.GroupsHost.Width = 70; host.Layout();
        if (simplified)
        {
            host.ToolBar.OpenOverflow(); host.Layout();
            host.ToolBar.OverflowEntries.OfType<RibbonGroupQuickAccessProxy>().Single().IsDropDownOpen = true;
        }
        else host.Group.CollapsedButton!.IsChecked = true;
        host.Layout();
        Assert.True(host.GroupPopup.IsOpen);
        host.Toggle.InvokeFromKeyTip(); host.Layout();
        Assert.True(host.GroupPopup.IsOpen); // The ordinary toggle exemption remains.
        mode.InvokeFromKeyTip();
        Assert.True(clickFinishedInFlyout);
        host.Layout();
        Assert.Equal(simplified ? RibbonLayoutMode.Classic : RibbonLayoutMode.Simplified, host.Ribbon.LayoutMode);
        Assert.False(host.GroupPopup.IsOpen);
        Assert.False(host.Group.CollapsedButton!.IsChecked);
        Assert.True(((Decorator)host.Group.Template.FindName("PART_NormalHost", host.Group)).IsAncestorOf(mode));
        Assert.True(host.Toggle.IsChecked);
    });

    [Fact]
    public void Mode_change_closes_title_bar_QAT_nested_menu_and_returns_borrowed_items() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        host.Ribbon.AddToQuickAccess(host.Group); host.Layout();
        var entry = host.Ribbon.QuickAccessItems.OfType<RibbonGroupQuickAccessProxy>().Single();
        entry.IsDropDownOpen = true; host.Layout();
        host.Split.IsDropDownOpen = true; host.Layout();
        Assert.True(host.GroupPopup.IsOpen);
        Assert.True(host.Split.IsDropDownOpen);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.False(host.GroupPopup.IsOpen);
        Assert.False(entry.IsDropDownOpen);
        Assert.False(host.Split.IsDropDownOpen);
        Assert.Single(host.Split.Items);
        Assert.True(((Decorator)host.Group.Template.FindName("PART_NormalHost", host.Group)).IsAncestorOf(host.Editor));
    });

    [Fact]
    public void Mode_change_closes_native_combo_and_ribbon_context_menu_but_preserves_unrelated_popup() => Sta.Run(() =>
    {
        using var host = new Host();
        var combo = new RibbonComboBox { Header = "Font" }; combo.Items.Add("Calibri");
        host.Group.Items.Add(combo); host.Layout();
        var unrelated = new RibbonDropDownButton { Header = "Document menu" };
        unrelated.Items.Add(new RibbonMenuItem { Header = "Document command" });
        host.Panel.Children.Add(unrelated); host.Layout();
        unrelated.IsDropDownOpen = true; host.Layout();
        combo.IsDropDownOpen = true; host.Layout();
        Assert.True(combo.IsDropDownOpen);
        var menu = host.Ribbon.CreateRibbonContextMenu(host.Action);
        menu.IsOpen = true; host.Layout();
        Assert.True(menu.IsOpen);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.False(combo.IsDropDownOpen);
        Assert.False(menu.IsOpen);
        Assert.True(unrelated.IsDropDownOpen);
    });

    [Theory]
    [InlineData(RibbonDensity.Compact)]
    [InlineData(RibbonDensity.Touch)]
    public void Switching_preserves_classic_structure_native_controls_and_QAT_membership(RibbonDensity density) => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.Density = density; host.Layout();
        host.Ribbon.AddToQuickAccess(host.Group);
        host.Layout();
        double classic = host.GroupsHost.ActualHeight;
        string before = RibbonCustomizationSerializer.Serialize(host.Ribbon);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.True(host.GroupsHost.ActualHeight < classic / 2);
        Assert.Same(host.Action, Ribbon.GetQuickAccessSource(host.Copy(host.Action)));
        Assert.DoesNotContain(host.ToolBar.Items.OfType<FrameworkElement>(), item => Ribbon.GetQuickAccessSource(item) == host.Editor);
        host.ToolBar.OpenOverflow(); host.Layout();
        var groupOpener = host.ToolBar.OverflowEntries.OfType<RibbonGroupQuickAccessProxy>().Single();
        groupOpener.IsDropDownOpen = true; host.Layout();
        Assert.True(host.GroupPopup.IsOpen);
        Assert.True(((Border)host.Group.Template.FindName("PART_PopupHost", host.Group)).IsAncestorOf(host.Editor));
        host.Editor.Text = "Preserved";
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.False(host.GroupPopup.IsOpen);
        Assert.Equal("Preserved", host.Editor.Text);
        Assert.Equal(classic, host.GroupsHost.ActualHeight);
        Assert.Equal(before, RibbonCustomizationSerializer.Serialize(host.Ribbon));
        Assert.True(host.Group.IsQuickAccessCollapsed);
        host.Ribbon.QuickAccessItems.Clear(); host.Layout();
        Assert.False(host.Group.IsQuickAccessCollapsed);
    });

    [Fact]
    public void Inline_actions_and_toggles_share_state_and_split_menus_return_on_switch() => Sta.Run(() =>
    {
        using var host = new Host();
        int actions = 0; host.Action.Click += (_, _) => actions++;
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        ((RibbonButton)host.Copy(host.Action)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        host.Layout();
        Assert.Equal(1, actions);
        host.Action.Header = "Save document"; host.Layout();
        Assert.Equal("Save document", ((RibbonButton)host.Copy(host.Action)).Header);
        ((RibbonToggleButton)host.Copy(host.Toggle)).IsChecked = true;
        Assert.True(host.Toggle.IsChecked);
        host.Group.IsEnabled = false; host.Layout();
        Assert.False(host.Copy(host.Action).IsEnabled);
        host.Group.IsEnabled = true;
        var split = (RibbonSplitButton)host.Copy(host.Split);
        split.IsDropDownOpen = true; host.Layout();
        Assert.Single(split.Items);
        Assert.Empty(host.Split.Items);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.Single(host.Split.Items);
        Assert.False(split.IsDropDownOpen);
    });

    [Fact]
    public void Resize_retains_higher_priorities_and_overflow_uses_original_command_sources() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        var action = host.Copy(host.Action); var toggle = host.Copy(host.Toggle); var split = host.Copy(host.Split);
        action.Width = 130; toggle.Width = 130; split.Width = 130;
        host.ToolBar.Width = 305; host.Layout();
        Assert.False(host.ToolBar.IsOverflowed(action));
        Assert.True(host.ToolBar.IsOverflowed(toggle));
        Assert.False(host.ToolBar.IsOverflowed(split));
        Assert.False(toggle.Focusable);
        host.ToolBar.OpenOverflow(); host.Layout();
        Assert.Contains(host.ToolBar.OverflowEntries, entry => Ribbon.GetQuickAccessSource(entry) == host.Toggle);
        Assert.DoesNotContain(host.ToolBar.OverflowEntries, entry => entry is RibbonGroupSeparator);
        host.ToolBar.CloseOverflow(); host.ToolBar.Width = 800; host.Layout();
        Assert.False(host.ToolBar.IsOverflowed(toggle));
        Assert.True(toggle.Focusable);
        Assert.True(host.ToolBar.HasOverflow); // Native full-group access remains available.
    });

    [Fact]
    public void Live_selection_visibility_items_and_tab_switches_release_stale_sources() => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Ribbon.SetSimplifiedPlacement(host.Toggle, RibbonSimplifiedPlacement.Overflow); host.Layout();
        Assert.DoesNotContain(host.ToolBar.Items.OfType<FrameworkElement>(), item => Ribbon.GetQuickAccessSource(item) == host.Toggle);
        host.Action.Visibility = Visibility.Collapsed; host.Layout();
        Assert.Equal(Visibility.Collapsed, host.Copy(host.Action).Visibility);
        host.ToolBar.OpenOverflow(); host.Layout();
        Assert.DoesNotContain(host.ToolBar.OverflowEntries, item => Ribbon.GetQuickAccessSource(item) == host.Action);
        host.Action.Visibility = Visibility.Visible;
        var added = new RibbonButton { Header = "Added" };
        Ribbon.SetSimplifiedPlacement(added, RibbonSimplifiedPlacement.Inline);
        host.Group.Items.Add(added); host.Layout();
        Assert.NotNull(host.Copy(added));
        var other = new RibbonTab { Header = "Other" }; host.Ribbon.Tabs.Add(other);
        host.Ribbon.SelectedTab = other; host.Layout();
        Assert.False(host.Group.IsQuickAccessCollapsed);
        host.Ribbon.SelectedTab = host.Tab; host.Layout();
        Assert.True(host.Group.IsQuickAccessCollapsed);
        host.Tab.Groups.Clear(); host.Layout();
        Assert.False(host.Group.IsQuickAccessCollapsed);
        Assert.Empty(host.ToolBar.Items);
    });

    [Fact]
    public void Classic_customization_is_disabled_only_in_simplified_mode_and_preserves_app_disable() => Sta.Run(() =>
    {
        using var host = new Host();
        var page = new RibbonCustomizePage { Ribbon = host.Ribbon };
        host.Panel.Children.Add(page); host.Layout();
        Assert.True(page.IsEnabled);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.False(page.IsEnabled);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.True(page.IsEnabled);
        page.IsEnabled = false;
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.False(page.IsEnabled);
        var unloadedPage = new RibbonCustomizePage { Ribbon = host.Ribbon };
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.False(unloadedPage.IsEnabled);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.True(unloadedPage.IsEnabled);
    });

    [Fact]
    public void Initial_simplified_layout_realizes_sources_and_separators_follow_visible_groups() => Sta.Run(() =>
    {
        using var host = new Host(initialSimplified: true);
        Assert.True(host.Copy(host.Action).ActualWidth > 0);
        var second = new RibbonGroup { Header = "Second" };
        var command = new RibbonButton { Header = "Second action" };
        Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline);
        second.Items.Add(command); host.Tab.Groups.Add(second); host.Layout();
        var separator = host.ToolBar.Items.OfType<RibbonGroupSeparator>().Last();
        Assert.False(host.ToolBar.IsOverflowed(separator));
        second.Visibility = Visibility.Collapsed; host.Layout();
        Assert.True(host.ToolBar.IsOverflowed(separator));
        Assert.Equal(Visibility.Collapsed, host.Copy(command).Visibility);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.False(host.Group.IsQuickAccessCollapsed);
        Assert.False(second.IsQuickAccessCollapsed);
    });

    [Fact]
    public void Combo_choices_and_KeyTips_use_only_visible_simplified_targets() => Sta.Run(() =>
    {
        using var host = new Host();
        var combo = new RibbonComboBox { Header = "Font", SelectedIndex = 0, IsEditable = true };
        combo.Items.Add(new ComboBoxItem { Content = "Calibri" }); combo.Items.Add(new ComboBoxItem { Content = "Arial" });
        Ribbon.SetSimplifiedPlacement(combo, RibbonSimplifiedPlacement.Inline); host.Group.Items.Add(combo);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        var copy = (RibbonDropDownButton)host.Copy(combo);
        copy.IsDropDownOpen = true; host.Layout();
        KeyTipService.InvokeControl((UIElement)copy.Items[1]); host.Layout();
        Assert.Equal(1, combo.SelectedIndex);
        Assert.Equal("Arial", combo.Text);
        Assert.Equal(2, combo.Items.Count);
        var service = new KeyTipService(host.Ribbon);
        object level = typeof(KeyTipService).GetMethod("BuildTabGroupsLevel", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, new object[] { host.Tab })!;
        var targets = ((IEnumerable)level.GetType().GetProperty("Items")!.GetValue(level)!).Cast<object>()
            .Select(item => (UIElement)item.GetType().GetProperty("Target")!.GetValue(item)!).ToArray();
        Assert.Contains(host.ToolBar.OverflowButton!, targets);
        Assert.Contains(host.Copy(host.Action), targets);
        Assert.DoesNotContain(host.Action, targets);
        host.ToolBar.Width = 65; host.Layout();
        level = typeof(KeyTipService).GetMethod("BuildTabGroupsLevel", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, new object[] { host.Tab })!;
        targets = ((IEnumerable)level.GetType().GetProperty("Items")!.GetValue(level)!).Cast<object>()
            .Select(item => (UIElement)item.GetType().GetProperty("Target")!.GetValue(item)!).ToArray();
        Assert.DoesNotContain(host.Copy(host.Action), targets);
        Assert.Contains(host.ToolBar.OverflowButton!, targets);
        var more = host.ToolBar.OverflowButton!;
        Assert.True(more.Focus());
        more.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(more), Environment.TickCount, Key.Down)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        host.Layout();
        Assert.True(((Popup)host.ToolBar.Template.FindName("PART_OverflowPopup", host.ToolBar)).IsOpen);
        Assert.Contains(host.ToolBar.OverflowEntries, entry => entry.IsKeyboardFocusWithin);
    });

    [Fact]
    public void Rendered_layout_and_popup_evidence_has_visible_commands_and_group_access() => Sta.Run(() =>
    {
        using var host = new Host();
        Save(host.Ribbon, "classic");
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        Assert.True(host.Copy(host.Action).ActualWidth > 20);
        Save(host.Ribbon, "simplified");
        host.ToolBar.OpenOverflow(); host.Layout();
        var popup = (Popup)host.ToolBar.Template.FindName("PART_OverflowPopup", host.ToolBar);
        Assert.True(popup.IsOpen);
        Save((FrameworkElement)popup.Child, "more-commands");
    });

    [Theory]
    [InlineData(RibbonDensity.Compact)]
    [InlineData(RibbonDensity.Touch)]
    public void Small_inline_commands_hide_labels_keep_names_and_use_labeled_overflow(RibbonDensity density) => Sta.Run(() =>
    {
        using var host = new Host();
        host.Ribbon.Density = density;
        foreach (var source in new FrameworkElement[] { host.Action, host.Toggle, host.Split })
            Ribbon.SetSimplifiedSize(source, RibbonControlSize.Small);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        var action = (RibbonButton)host.Copy(host.Action);
        var toggle = (RibbonToggleButton)host.Copy(host.Toggle);
        var split = (RibbonSplitButton)host.Copy(host.Split);
        Assert.Equal(RibbonControlSize.Small, action.Size);
        Assert.Equal(RibbonControlSize.Small, toggle.Size);
        Assert.Equal(RibbonControlSize.Small, split.Size);
        Assert.Equal(Visibility.Visible, ((FrameworkElement)action.Template.FindName("SmallLayout", action)).Visibility);
        Assert.Equal(Visibility.Collapsed, ((FrameworkElement)action.Template.FindName("MediumLayout", action)).Visibility);
        Assert.Equal("Save", System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(action)!.GetName());
        Assert.Equal("Save", action.ScreenTipTitle);
        Ribbon.SetSimplifiedSize(host.Action, RibbonControlSize.Medium); host.Layout();
        Assert.Same(action, host.Copy(host.Action));
        Assert.Equal(RibbonControlSize.Medium, action.Size);
        Assert.Equal(Visibility.Visible, ((FrameworkElement)action.Template.FindName("MediumLayout", action)).Visibility);
        host.ToolBar.Width = 40; host.Layout(); host.ToolBar.OpenOverflow(); host.Layout();
        var overflow = host.ToolBar.OverflowEntries.OfType<RibbonToggleButton>().Single();
        Assert.Equal(RibbonControlSize.Medium, overflow.Size);
        Assert.Equal("Bold", overflow.Header);
        Assert.Equal(Visibility.Visible, ((FrameworkElement)overflow.Template.FindName("MediumLayout", overflow)).Visibility);
        Assert.Throws<ArgumentException>(() => Ribbon.SetSimplifiedSize(host.Action, RibbonControlSize.Large));
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.Equal(RibbonControlSize.Large, host.Action.Size);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Galleries_use_native_QAT_dropdowns_in_small_row_and_labeled_overflow(bool strip) => Sta.Run(() =>
    {
        using var host = new Host();
        RibbonGallery source = strip ? new InRibbonGallery { Width = 180 } : new RibbonGallery { Width = 180 };
        source.Header = "Styles"; source.Icon = new DrawingImage();
        var first = new RibbonGalleryItem { Content = new Border { Width = 60, Height = 30, Background = Brushes.Red } };
        var last = new RibbonGalleryItem { Content = new Border { Width = 60, Height = 30, Background = Brushes.Blue } };
        source.Items.Add(first); source.Items.Add(last); source.SelectedIndex = 0;
        Ribbon.SetSimplifiedPlacement(source, RibbonSimplifiedPlacement.Inline);
        Ribbon.SetSimplifiedSize(source, RibbonControlSize.Small);
        host.Group.Items.Add(source); host.Layout();
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        var copy = Assert.IsType<RibbonGalleryQuickAccessProxy>(host.Copy(source));
        Assert.Equal(RibbonControlSize.Small, copy.Size);
        copy.IsDropDownOpen = true; host.Layout();
        var viewport = Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(copy.Items));
        var presenter = Assert.IsType<ItemsPresenter>(viewport.Content);
        Assert.Same(source, first.Parent);
        last.IsSelected = true; host.Layout(); Assert.Same(last, source.SelectedItem);
        copy.IsDropDownOpen = false; host.Layout(); Assert.NotSame(viewport, VisualTreeHelper.GetParent(presenter));
        host.ToolBar.Width = 40; host.Layout(); host.ToolBar.OpenOverflow(); host.Layout();
        var overflow = host.ToolBar.OverflowEntries.OfType<RibbonGalleryQuickAccessProxy>().Single();
        Assert.Equal(RibbonControlSize.Medium, overflow.Size);
        overflow.IsDropDownOpen = true; host.Layout();
        Assert.Same(presenter, Assert.IsAssignableFrom<ScrollViewer>(Assert.Single(overflow.Items)).Content);
        first.IsSelected = true; host.Layout(); Assert.Same(first, source.SelectedItem);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Classic; host.Layout();
        Assert.False(overflow.IsDropDownOpen);
        Assert.Same(source, first.Parent);
        Assert.Equal(2, source.Items.Count);
    });

    [Theory]
    [InlineData(RibbonTheme.Office2007)]
    [InlineData(RibbonTheme.Office2010)]
    public void Legacy_overflow_open_state_uses_the_same_gradient_and_border_as_commands(RibbonTheme theme) => Sta.Run(() =>
    {
        using var host = new Host();
        ThemeManager.Apply(Application.Current, theme);
        host.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; host.Layout();
        var more = host.ToolBar.OverflowButton!;
        var chrome = (Border)more.Template.FindName("Chrome", more);
        Assert.Equal(new Thickness(0), chrome.BorderThickness);
        host.ToolBar.OpenOverflow(); host.Layout();
        Assert.Equal(host.Ribbon.FindResource("RibbonKit.Metrics.ControlHighlightBorderThickness"), chrome.BorderThickness);
        Assert.Same(host.Ribbon.FindResource("RibbonKit.Brushes.Control.CheckedBorder"), chrome.BorderBrush);
        Assert.IsAssignableFrom<GradientBrush>(chrome.Background);
        Save(more, theme.ToString().ToLowerInvariant() + "-more-open");
        host.ToolBar.CloseOverflow(); host.Layout();
        Assert.Equal(new Thickness(0), chrome.BorderThickness);
    });

    private static void Save(FrameworkElement element, string name)
    {
        string folder = Path.Combine("TestResults", "simplified"); Directory.CreateDirectory(folder);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        // A nested button retains its layout offset inside the ribbon; use its
        // visual brush to capture the control at the origin of this small image.
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
            drawing.DrawRectangle(new VisualBrush(element) { Stretch = Stretch.Fill }, null,
                new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, name + ".png")); encoder.Save(stream);
    }

    [Fact]
    public void Showcase_home_declaration_renders_selected_commands_with_shared_templates() => Sta.Run(() =>
    {
        var app = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(app, RibbonTheme.Office2024);
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln"))) directory = directory.Parent;
        var document = XDocument.Load(Path.Combine(directory!.FullName, "samples", "RibbonKit.Showcase", "MainWindow.xaml"));
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var home = new XElement(document.Descendants().Single(element => element.Name.LocalName == "RibbonTab" && (string?)element.Attribute(xaml + "Name") == "HomeTab"));
        // Render the consumer's declaration without constructing its window or touching
        // user preferences. This fixture has no application event handlers.
        foreach (var attribute in home.DescendantsAndSelf().Attributes().Where(attribute =>
                     attribute.Name.LocalName is "Click" or "Checked" or "Unchecked" or "DialogLauncherClick"
                         or "SelectionChanged" or "PreviewChanged" or "SelectionCommitted" or "PreviewStarted" or "PreviewEnded"
                         or "ItemPreview" or "ItemPreviewCancelled").ToArray()) attribute.Remove();
        var declaration = new XElement(home.Name.Namespace + "Ribbon",
            document.Root!.Attributes().Where(attribute => attribute.IsNamespaceDeclaration).Select(attribute => new XAttribute(attribute)), home);
        var ribbon = (Ribbon)XamlReader.Parse(declaration.ToString(), new ParserContext
            { BaseUri = new Uri("pack://application:,,,/RibbonKit.Showcase;component/MainWindow.xaml") });
        var window = new RibbonWindow { Content = ribbon, Width = 1200, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); window.UpdateLayout(); Sta.Drain();
            Save(ribbon, "showcase-classic");
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; Sta.Drain(); window.UpdateLayout(); Sta.Drain();
            var toolbar = ((RibbonGroupsHost)ribbon.Tabs[0].Content).SimplifiedToolBar!;
            Assert.Contains(toolbar.Items.OfType<RibbonSplitButton>(), split => split.Header == "Paste");
            Assert.Equal(2, toolbar.Items.OfType<RibbonComboBoxQuickAccessProxy>().Count());
            Save(ribbon, "showcase-simplified");
            var bold = toolbar.Items.OfType<RibbonToggleButton>().Single(button => button.Header == "Bold");
            bold.IsChecked = true;
            var fontSize = toolbar.Items.OfType<RibbonComboBoxQuickAccessProxy>().Single(button => button.Header == "Font Size");
            fontSize.IsDropDownOpen = true; Sta.Drain(); window.UpdateLayout(); Sta.Drain();
            Save(ribbon, "showcase-simplified-spacing");
            fontSize.IsDropDownOpen = false;
        }
        finally { window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; Sta.ResetApplication(); }
    });

    private sealed class Host : IDisposable
    {
        internal readonly Ribbon Ribbon = new();
        internal readonly RibbonTab Tab = new() { Header = "Home" };
        internal readonly RibbonGroup Group = new() { Header = "Editing" };
        internal readonly RibbonButton Action = new() { Header = "Save", Icon = new DrawingImage() };
        internal readonly RibbonToggleButton Toggle = new() { Header = "Bold", Icon = new DrawingImage() };
        internal readonly RibbonSplitButton Split = new() { Header = "Paste", Icon = new DrawingImage() };
        internal readonly RibbonTextBox Editor = new() { Header = "Find", Text = "Original" };
        internal readonly StackPanel Panel = new();
        internal readonly RibbonWindow Window;
        internal RibbonGroupsHost GroupsHost => (RibbonGroupsHost)Tab.Content;
        internal RibbonQuickAccessToolBar ToolBar => GroupsHost.SimplifiedToolBar!;
        internal Popup GroupPopup => (Popup)Group.Template.FindName("PART_Popup", Group);
        internal FrameworkElement Copy(FrameworkElement source) => ToolBar.Items.OfType<FrameworkElement>().Single(item => Ribbon.GetQuickAccessSource(item) == source);
        internal Host(bool initialSimplified = false)
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            foreach (var item in new FrameworkElement[] { Action, Toggle, Split }) Ribbon.SetSimplifiedPlacement(item, RibbonSimplifiedPlacement.Inline);
            Ribbon.SetSimplifiedPriority(Action, 100); Ribbon.SetSimplifiedPriority(Split, 90);
            Split.Items.Add(new RibbonMenuItem { Header = "Paste special" });
            Group.Items.Add(Action); Group.Items.Add(Toggle); Group.Items.Add(Split); Group.Items.Add(Editor);
            Ribbon.SetCommandId(Tab, "home"); Ribbon.SetCommandId(Group, "editing"); Ribbon.SetCommandId(Action, "save");
            Tab.Groups.Add(Group); Ribbon.Tabs.Add(Tab); Ribbon.SelectedTab = Tab;
            if (initialSimplified) Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            Panel.Children.Add(Ribbon);
            Window = new RibbonWindow { Content = Panel, Width = 900, Height = 500, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
        }
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); Window.UpdateLayout(); }
        public void Dispose() { Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; Sta.ResetApplication(); }
    }
}
