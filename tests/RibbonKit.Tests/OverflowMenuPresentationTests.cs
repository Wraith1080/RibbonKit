using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class OverflowMenuPresentationTests
{
    [Theory]
    [InlineData(FlowDirection.LeftToRight, 300, 200, PlacementMode.Right)]
    [InlineData(FlowDirection.RightToLeft, 300, 200, PlacementMode.Left)]
    [InlineData(FlowDirection.LeftToRight, 850, 200, PlacementMode.Left)]
    [InlineData(FlowDirection.RightToLeft, 10, 200, PlacementMode.Right)]
    [InlineData(FlowDirection.LeftToRight, 300, 800, PlacementMode.Bottom)]
    [InlineData(FlowDirection.RightToLeft, 300, 800, PlacementMode.Bottom)]
    public void Screen_space_selects_the_preferred_side_other_side_or_overlay(
        FlowDirection direction, double x, double width, PlacementMode expected)
    {
        // Physical pixel coordinates also cover negative-origin monitors and any
        // target/popup DPI; placement must not assume the primary screen starts at 0.
        var work = new Rect(-1000, -200, 1000, 800);
        var target = new Rect(x - 1000, 100, 100, 32);
        Assert.Equal(expected, RibbonOverflowMenu.SelectSide(target, width, work, direction));
        Assert.Equal(PlacementMode.Bottom, RibbonOverflowMenu.SelectSide(target, width, Rect.Empty, direction));
    }

    [Theory]
    [InlineData(false, RibbonDensity.Compact, FlowDirection.LeftToRight)]
    [InlineData(false, RibbonDensity.Touch, FlowDirection.RightToLeft)]
    [InlineData(true, RibbonDensity.Compact, FlowDirection.LeftToRight)]
    [InlineData(true, RibbonDensity.Touch, FlowDirection.RightToLeft)]
    public void Both_overflows_use_menu_rows_side_popups_and_restore_the_originals(
        bool simplified, RibbonDensity density, FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(simplified, density, direction);
        host.Toolbar.OpenOverflow(); host.Layout();
        Assert.All(host.Toolbar.OverflowEntries, entry =>
        {
            Assert.True(RibbonOverflowMenu.IsEntry(entry));
            Assert.True(entry.ActualWidth >= 200);
            Assert.True(entry.ActualHeight >= (density == RibbonDensity.Touch ? 44 : 32));
        });
        foreach (RibbonDropDownButton source in new RibbonDropDownButton[] { host.Dropdown, host.Split })
        {
            var entry = (RibbonDropDownButton)host.Entry(source);
            entry.IsDropDownOpen = true; host.Layout();
            var popup = (Popup)entry.Template.FindName("PART_Popup", entry);
            Assert.Equal(PlacementMode.Custom, popup.Placement);
            Assert.Same(entry, popup.PlacementTarget);
            Assert.True(popup.IsOpen, $"{source.Header}: entry open={entry.IsDropDownOpen}, outer open={host.Popup.IsOpen}"); Assert.Single(entry.Items); Assert.Empty(source.Items);
            CheckJoin(host.Popup, popup);
            var toggle = (ToggleButton)entry.Template.FindName("PART_Toggle", entry);
            var arrow = (System.Windows.Shapes.Path)(entry is RibbonSplitButton
                ? toggle.Template.FindName("ToggleChevron", toggle)
                : entry.Template.FindName("OverflowChevron", entry));
            Assert.True(arrow.Data.Bounds.Height > arrow.Data.Bounds.Width);
            Assert.True(arrow.IsVisible);
            if (!simplified && density == RibbonDensity.Compact && direction == FlowDirection.LeftToRight)
                Capture((FrameworkElement)host.Popup.Child, (FrameworkElement)popup.Child, "qat-overflow-side");
            entry.IsDropDownOpen = false; host.Layout();
            Assert.Single(source.Items);
            Assert.True(host.Popup.IsOpen, $"Outer closed after returning {source.Header}");
        }
        var groupEntry = (RibbonGroupQuickAccessProxy)host.Entry(host.Group);
        var native = (Popup)host.Group.Template.FindName("PART_Popup", host.Group);
        PlacementMode originalPlacement = native.Placement;
        groupEntry.IsDropDownOpen = true; host.Layout();
        Assert.Equal(PlacementMode.Custom, native.Placement); Assert.Same(groupEntry, native.PlacementTarget);
        Assert.True(native.IsOpen);
        CheckJoin(host.Popup, native);
        Assert.True(((FrameworkElement)native.Child).ActualWidth > 50, $"Native child width={((FrameworkElement)native.Child).ActualWidth}");
        if (simplified && density == RibbonDensity.Compact && direction == FlowDirection.LeftToRight)
            Capture((FrameworkElement)host.Popup.Child, (FrameworkElement)native.Child, "simplified-overflow-side");
        groupEntry.IsDropDownOpen = false; host.Layout();
        Assert.Equal(originalPlacement, native.Placement); Assert.Null(native.CustomPopupPlacementCallback);
        Assert.True(host.Popup.IsOpen);
        host.Toolbar.CloseOverflow(); host.Layout();
    });

    [Fact]
    public void Submenu_keys_return_focus_and_split_primary_invokes_once() => Sta.Run(() =>
    {
        using var host = new Host(false, RibbonDensity.Compact, FlowDirection.LeftToRight);
        host.Window.Activate(); host.Layout();
        host.Toolbar.OpenOverflow(); host.Layout();
        var entry = (RibbonDropDownButton)host.Entry(host.Dropdown);
        var opener = (ToggleButton)entry.Template.FindName("PART_Toggle", entry);
        Assert.True(opener.Focus(), $"Visible={opener.IsVisible}, enabled={opener.IsEnabled}, focusable={opener.Focusable}, outer open={host.Popup.IsOpen}"); Send(opener, Key.Right); host.Layout();
        Assert.True(entry.IsDropDownOpen);
        var item = (RibbonMenuItem)entry.Items[0]; Assert.True(item.Focus());
        Send(item, Key.Left); host.Layout();
        Assert.False(entry.IsDropDownOpen); Assert.True(host.Popup.IsOpen); Assert.True(opener.IsKeyboardFocused);
        int clicks = 0; host.Split.Click += (_, _) => clicks++;
        var split = (RibbonSplitButton)host.Entry(host.Split);
        ((Button)split.Template.FindName("PART_Primary", split)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); host.Layout();
        Assert.Equal(1, clicks); Assert.False(host.Popup.IsOpen);
        host.Toolbar.OpenOverflow(); host.Layout();
        Assert.Same(split, host.Entry(host.Split));
    });

    [Theory]
    [InlineData(RibbonDensity.Compact)]
    [InlineData(RibbonDensity.Touch)]
    public void Native_and_projected_combo_choices_share_readable_menu_minimums(RibbonDensity density) => Sta.Run(() =>
    {
        using var host = new Host(false, density, FlowDirection.LeftToRight);
        var combo = new RibbonComboBox { ItemsSource = new[] { "11", "12" }, SelectedIndex = 0 };
        Ribbon.SetDensity(combo, density);
        host.Panel.Children.Add(combo); host.Layout();
        combo.IsDropDownOpen = true; host.Layout();
        Check((FrameworkElement)combo.ItemContainerGenerator.ContainerFromIndex(0), density);
        combo.IsDropDownOpen = false;
        host.Toolbar.OpenOverflow(); host.Layout();
        var copy = (RibbonDropDownButton)host.Entry(host.Combo);
        copy.IsDropDownOpen = true; host.Layout();
        Check((FrameworkElement)copy.Items[0], density);
        copy.IsDropDownOpen = false; host.Layout();
        Assert.Equal(0, host.Combo.SelectedIndex);
    });

    private static void Check(FrameworkElement row, RibbonDensity density)
    {
        Assert.True(row.ActualWidth >= 200, $"Width={row.ActualWidth}, MinWidth={row.MinWidth}");
        Assert.True(row.ActualHeight >= (density == RibbonDensity.Touch ? 44 : 32), $"Height={row.ActualHeight}, MinHeight={row.MinHeight}, density={Ribbon.GetDensity(row)}");
    }
    private static void Send(UIElement target, Key key) => target.RaiseEvent(new KeyEventArgs(
        Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });

    [Fact]
    public void A_submenu_too_wide_for_either_side_uses_the_overlay_fallback() => Sta.Run(() =>
    {
        using var host = new Host(true, RibbonDensity.Compact, FlowDirection.LeftToRight);
        ((RibbonMenuItem)host.Dropdown.Items[0]).Width = SystemParameters.VirtualScreenWidth + 10;
        host.Toolbar.OpenOverflow(); host.Layout();
        var entry = (RibbonDropDownButton)host.Entry(host.Dropdown);
        entry.IsDropDownOpen = true; host.Layout();
        var popup = (Popup)entry.Template.FindName("PART_Popup", entry);
        Assert.True(popup.IsOpen);
        Rect frame = PopupBounds(popup), target = Bounds(entry);
        Assert.True(frame.Left < target.Right && frame.Right > target.Left);
        Assert.True(frame.Top >= target.Bottom - 1);
    });

    private static Rect Bounds(FrameworkElement element) => new(element.PointToScreen(new Point()),
        element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)));
    private static Rect PopupBounds(Popup popup) => Bounds((FrameworkElement)PresentationSource.FromVisual(popup.Child)!.RootVisual);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Actual_popups_open_on_the_available_side_at_both_screen_edges(bool simplified, bool rightEdge) => Sta.Run(() =>
    {
        using var host = new Host(simplified, RibbonDensity.Compact, FlowDirection.LeftToRight, rightEdge);
        host.Toolbar.OpenOverflow(); host.Layout();
        foreach (var source in new FrameworkElement[] { host.Dropdown, host.Split, host.Combo, host.Group })
        {
            var entry = (RibbonDropDownButton)host.Entry(source);
            entry.IsDropDownOpen = true; host.Layout();
            var popup = (Popup)(source == host.Group ? host.Group.Template.FindName("PART_Popup", host.Group) : entry.Template.FindName("PART_Popup", entry));
            Rect chrome = ParentChromeBounds(popup), parent = ParentChromeBounds(host.Popup);
            Assert.InRange(rightEdge ? parent.Left - chrome.Right : chrome.Left - parent.Right, -1, 1);
            CheckJoin(host.Popup, popup);
            Capture((FrameworkElement)host.Popup.Child, (FrameworkElement)popup.Child,
                $"{(simplified ? "simplified" : "qat")}-{(rightEdge ? "left" : "right")}-{entry.Header!.ToLowerInvariant()}");
            entry.IsDropDownOpen = false; host.Layout();
        }
    });

    private static Rect ParentChromeBounds(Popup popup) => Bounds(popup.Child is AdornerDecorator decorator
        ? (FrameworkElement)decorator.Child : (FrameworkElement)popup.Child);
    private static void CheckJoin(Popup parent, Popup child)
    {
        Rect parentChrome = ParentChromeBounds(parent), childChrome = ParentChromeBounds(child);
        Assert.True(Math.Abs(childChrome.Left - parentChrome.Right) <= 1 || Math.Abs(parentChrome.Left - childChrome.Right) <= 1,
            $"Detached popup chrome: parent={parentChrome}, child={childChrome}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Popup_alignment_uses_the_final_row_slot_while_parent_content_is_sliding(bool group) => Sta.Run(() =>
    {
        using var host = new Host(false, RibbonDensity.Compact, FlowDirection.LeftToRight, rightEdge: true);
        host.Toolbar.OpenOverflow(); host.Layout();
        var parentChrome = (Border)((AdornerDecorator)host.Popup.Child).Child;
        var content = (FrameworkElement)parentChrome.Child;
        content.RenderTransform = new TranslateTransform(0, -12);
        var entry = (RibbonDropDownButton)host.Entry(group ? host.Group : host.Combo);
        entry.IsDropDownOpen = true; host.Layout();
        var popup = (Popup)(group ? host.Group.Template.FindName("PART_Popup", host.Group) : entry.Template.FindName("PART_Popup", entry));
        Rect before = ParentChromeBounds(popup);
        content.RenderTransform = Transform.Identity; host.Layout();
        Rect after = ParentChromeBounds(popup);
        Assert.Equal(before, after);
        Assert.InRange(after.Top - Bounds(entry).Top, -1, 1);
        CheckJoin(host.Popup, popup);
    });

    private static void Capture(FrameworkElement parent, FrameworkElement child, string name)
    {
        var rectangles = new[] { parent, child }.Select(element =>
            new Rect(element.PointToScreen(new Point()), element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)))).ToArray();
        Rect bounds = rectangles[0]; bounds.Union(rectangles[1]);
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
            for (int i = 0; i < 2; i++)
            {
                Rect rect = rectangles[i]; rect.Offset(-bounds.X, -bounds.Y);
                var element = i == 0 ? parent : child;
                // Use the arranged visual extent rather than stretching an
                // effect-expanded brush bound, which can invent a visible gap.
                drawing.DrawRectangle(new VisualBrush(element) { Stretch = Stretch.Fill,
                    ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, element.ActualWidth, element.ActualHeight) }, null, rect);
            }
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory("TestResults/overflow-menus");
        using var stream = File.Create("TestResults/overflow-menus/" + name + ".png"); encoder.Save(stream);
    }

    private sealed class Host : IDisposable
    {
        internal readonly Ribbon Ribbon = new() { QuickAccessPosition = RibbonQuickAccessPosition.TabRow, QuickAccessMaxWidth = 36 };
        internal readonly RibbonGroup Group = new() { Header = "Editing" };
        internal readonly RibbonDropDownButton Dropdown = new() { Header = "Select", Size = RibbonControlSize.Medium };
        internal readonly RibbonSplitButton Split = new() { Header = "Paste", Size = RibbonControlSize.Medium };
        internal readonly RibbonComboBox Combo = new() { Header = "Size", ItemsSource = new[] { "11", "12" }, SelectedIndex = 0 };
        internal readonly StackPanel Panel = new();
        internal readonly RibbonWindow Window;
        internal RibbonQuickAccessToolBar Toolbar { get; }
        internal Popup Popup => (Popup)Toolbar.Template.FindName("PART_OverflowPopup", Toolbar);
        internal FrameworkElement Entry(FrameworkElement source) => Toolbar.OverflowEntries.Single(entry => Ribbon.GetQuickAccessSource(entry) == source);
        internal Host(bool simplified, RibbonDensity density, FlowDirection direction, bool rightEdge = false)
        {
            ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
            Ribbon.Density = density; Ribbon.FlowDirection = direction;
            Dropdown.Items.Add(new RibbonMenuItem { Header = "Select all" }); Split.Items.Add(new RibbonMenuItem { Header = "Paste special" });
            foreach (var item in new FrameworkElement[] { Dropdown, Split, Combo })
            { Group.Items.Add(item); Ribbon.SetSimplifiedPlacement(item, RibbonSimplifiedPlacement.Inline); }
            var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(Group); Ribbon.Tabs.Add(tab); Ribbon.SelectedTab = tab;
            foreach (var item in new FrameworkElement[] { Dropdown, Split, Combo, Group }) Ribbon.AddToQuickAccess(item);
            Panel.Children.Add(Ribbon);
            Window = new RibbonWindow { Content = Panel, Width = 850, Height = 330, Left = rightEdge ? 10000 : -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            Window.Show(); Layout();
            if (simplified)
            { Ribbon.LayoutMode = RibbonLayoutMode.Simplified; Layout(); Toolbar = ((RibbonGroupsHost)tab.Content).SimplifiedToolBar!; Toolbar.Width = 36; }
            else Toolbar = Descendants(Ribbon).OfType<RibbonQuickAccessToolBar>().Single(toolbar => toolbar.IsVisible);
            Layout();
        }
        internal void Layout() { Sta.Drain(); Window.UpdateLayout(); Sta.Drain(); Window.UpdateLayout(); }
        public void Dispose() { Window.Close(); Sta.Drain(); RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; Sta.ResetApplication(); }
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        { var child = VisualTreeHelper.GetChild(parent, i); yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
}
