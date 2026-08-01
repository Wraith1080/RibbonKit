using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using RibbonKit.Controls;
using RibbonKit.Layout;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>
/// The quick access toolbar's overflow behaviour: which items the strip decides it cannot show
/// (<see cref="RibbonQuickAccessPanel"/>), and what the flyout puts in front of the user instead
/// (<see cref="Ribbon.CreateCommandProxy"/>).
/// </summary>
/// <remarks>
/// The measure rules are the interesting half. A horizontal <see cref="StackPanel"/> measures its
/// children with INFINITE width and so can never notice it has run out of room — that is why this
/// panel exists at all, and why "honours the width it is given" is worth pinning down in tests.
/// </remarks>
public class QuickAccessOverflowTests
{
    [Fact]
    public void An_unconstrained_strip_never_overflows() => Sta.Run(() =>
    {
        // Below the ribbon the strip owns a full-width row and is measured with infinity. Office
        // never shows a » there, and neither do we.
        RibbonQuickAccessPanel panel = Strip(100, 100, 100);

        panel.Measure(new Size(double.PositiveInfinity, 22));

        Assert.Empty(panel.OverflowedChildren);
        Assert.Equal(300d, panel.DesiredSize.Width);
    });

    [Fact]
    public void Everything_that_fits_exactly_stays_in_the_strip() => Sta.Run(() =>
    {
        RibbonQuickAccessPanel panel = Strip(100, 100, 100);

        panel.Measure(new Size(300, 22));

        Assert.Empty(panel.OverflowedChildren);
    });

    [Fact]
    public void The_first_item_that_does_not_fit_moves_to_the_overflow() => Sta.Run(() =>
    {
        RibbonQuickAccessPanel panel = Strip(100, 100, 100);

        panel.Measure(new Size(250, 22));

        UIElement overflowed = Assert.Single(panel.OverflowedChildren);
        Assert.Same(panel.Children[2], overflowed);

        // The strip reports only what it actually shows, so the template's DockPanel hands the
        // leftover width to the » button instead of leaving a gap.
        Assert.Equal(200d, panel.DesiredSize.Width);
    });

    [Fact]
    public void Order_wins_over_packing_once_the_strip_is_full() => Sta.Run(() =>
    {
        // A narrow item AFTER the first one that didn't fit must not jump the queue back into the
        // strip: the quick access toolbar is an ordered list the user arranged, and resequencing it
        // to save a few pixels would be a worse surprise than the » button.
        RibbonQuickAccessPanel panel = Strip(100, 100, 10);

        panel.Measure(new Size(150, 22));

        Assert.Equal(
            new[] { panel.Children[1], panel.Children[2] },
            panel.OverflowedChildren);
    });

    [Fact]
    public void Overflowed_items_are_arranged_away_not_collapsed() => Sta.Run(() =>
    {
        // Visibility belongs to the application — a quick access item may legitimately be hidden —
        // so an item that doesn't fit is given a zero LAYOUT SLOT and left alone otherwise. (Its
        // RenderSize stays natural: WPF refuses to arrange an element smaller than its unclipped
        // desired size and clips instead. The slot is the honest record of what layout asked for.)
        RibbonQuickAccessPanel panel = Strip(100, 100, 100);

        panel.Measure(new Size(150, 22));
        panel.Arrange(new Rect(0, 0, 150, 22));

        Assert.Equal(new Rect(0, 0, 100, 22), Slot(panel, 0));
        Assert.Equal(new Rect(0, 0, 0, 0), Slot(panel, 2));
        Assert.Equal(Visibility.Visible, panel.Children[2].Visibility);
    });

    [Fact]
    public void Items_that_stay_are_laid_out_left_to_right_without_gaps() => Sta.Run(() =>
    {
        RibbonQuickAccessPanel panel = Strip(60, 40, 100);

        panel.Measure(new Size(150, 22));
        panel.Arrange(new Rect(0, 0, 150, 22));

        Assert.Equal(new Rect(0, 0, 60, 22), Slot(panel, 0));
        Assert.Equal(new Rect(60, 0, 40, 22), Slot(panel, 1));
    });

    [Fact]
    public void A_widening_strip_takes_its_items_back() => Sta.Run(() =>
    {
        // Overflow is recomputed from scratch every measure, so a window the user drags wider
        // empties the flyout again — the toolbar closes it when that happens.
        RibbonQuickAccessPanel panel = Strip(100, 100, 100);

        panel.Measure(new Size(150, 22));
        Assert.Equal(2, panel.OverflowedChildren.Count);

        panel.Measure(new Size(400, 22));
        Assert.Empty(panel.OverflowedChildren);
    });

    [Fact]
    public void A_panel_outside_a_toolbar_measures_without_an_owner() => Sta.Run(() =>
    {
        // The panel reports overflow to its ItemsControl owner. Standing alone (a design-time
        // surface, a test) there isn't one, and that must not throw.
        RibbonQuickAccessPanel panel = Strip(100);

        panel.Measure(new Size(50, 22));

        Assert.Single(panel.OverflowedChildren);
    });

    [Fact]
    public void The_strip_is_capped_at_the_ribbons_declared_width() => Sta.Run(() =>
    {
        // Overflow only ever happens because something CONSTRAINS the width — an Auto grid column
        // measures with infinity and would never trigger it. That constraint is this property.
        var ribbon = new Ribbon();

        Assert.Equal(240d, ribbon.QuickAccessMaxWidth);
    });

    [Fact]
    public void A_dropdown_proxy_mirrors_the_original_and_borrows_its_menu() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var original = new RibbonDropDownButton { Header = "Paste" };
        original.Items.Add(new RibbonMenuItem { Header = "Keep Formatting" });

        var proxy = (RibbonDropDownButton)ribbon.CreateCommandProxy(original, RibbonControlSize.Medium);

        Assert.Equal("Paste", proxy.Header);
        Assert.Same(original, Ribbon.GetQuickAccessSource(proxy));

        proxy.IsDropDownOpen = true;
        Assert.Single(proxy.Items);
        Assert.Empty(original.Items);
    });

    [Fact]
    public void A_split_source_produces_a_real_split_proxy() => Sta.Run(() =>
    {
        // Not a plain button with the menu dropped: the QAT entry keeps its chevron, matching
        // Office, which is also what makes the borrow protocol apply to it.
        var ribbon = new Ribbon();
        var original = new RibbonSplitButton { Header = "Undo" };

        FrameworkElement proxy = ribbon.CreateCommandProxy(original, RibbonControlSize.Small);

        Assert.IsType<RibbonSplitButton>(proxy);
    });

    [Fact]
    public void Adding_a_command_to_the_quick_access_toolbar_adds_a_proxy_of_it_once() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var original = new RibbonButton { Header = "Save" };

        Assert.True(ribbon.AddToQuickAccess(original));
        Assert.False(ribbon.AddToQuickAccess(original));

        object item = Assert.Single(ribbon.QuickAccessItems);
        Assert.NotSame(original, item);
        Assert.Same(original, Ribbon.GetQuickAccessSource((DependencyObject)item));
        Assert.True(ribbon.IsInQuickAccess(original));
    });

    [Fact]
    public void An_overflow_entry_points_back_at_the_toolbar_item_it_stands_for() => Sta.Run(() =>
    {
        // Entries in the flyout are proxies, not members of QuickAccessItems, so a right-click on
        // one used to find nothing to remove. This attached property is the way back.
        var toolbarItem = new RibbonButton { Header = "Save" };
        var entry = new RibbonButton { Header = "Save" };

        Ribbon.SetQuickAccessOverflowItemInternal(entry, toolbarItem);

        Assert.Same(toolbarItem, Ribbon.GetQuickAccessOverflowItem(entry));
        Assert.Null(Ribbon.GetQuickAccessOverflowItem(toolbarItem));
    });

    [Fact]
    public void Overflow_membership_is_available_to_KeyTip_filtering() => Sta.Run(() =>
    {
        var toolbar = new RibbonQuickAccessToolBar();
        RibbonQuickAccessPanel panel = Strip(100, 100, 100);

        panel.Measure(new Size(150, 22));
        toolbar.OnOverflowChanged(panel, hasOverflow: true);

        Assert.False(toolbar.IsOverflowed(panel.Children[0]));
        Assert.True(toolbar.IsOverflowed(panel.Children[1]));
        Assert.True(toolbar.IsOverflowed(panel.Children[2]));
    });

    private static RibbonQuickAccessPanel Strip(params double[] widths)
    {
        var panel = new RibbonQuickAccessPanel();
        foreach (double width in widths)
        {
            panel.Children.Add(new Border { Width = width, Height = 22 });
        }

        return panel;
    }

    private static Rect Slot(Panel panel, int index) =>
        LayoutInformation.GetLayoutSlot((FrameworkElement)panel.Children[index]);
}
