using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SimplifiedContextMenuTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonControl Ribbon = new() { BindingContext = new BindingContext(), Width = 1200 };
        internal readonly RibbonTab Home = new() { Id = "home", Text = "Home" };
        internal readonly RibbonGroup Group = new() { Id = "group", Text = "Commands" };
        internal readonly RibbonButtonItem Copy = new() { Id = "copy", Text = "Copy", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        internal readonly RibbonQuickAccessReference Qat;
        internal Fixture() { Group.Items.Add(Copy); Home.Groups.Add(Group); Ribbon.Tabs.Add(Home); Qat = new() { Source = Copy }; Ribbon.QuickAccessItems.Add(Qat); Ribbon.CommandCatalog.CaptureDefaults(); }
        internal void Show(Form form) { form.Size = new(1300, 650); form.Controls.Add(Ribbon); form.Show(); Application.DoEvents(); }
        public void Dispose() => Ribbon.Dispose();
    }
    private static RibbonButtonItem? Toggle(RibbonDropDownItem menu) => menu.Items.OfType<RibbonButtonItem>().SingleOrDefault(i => i.Id == "ToggleSimplifiedRibbon");
    [Theory] [InlineData(RibbonLayoutMode.Classic)] [InlineData(RibbonLayoutMode.Simplified)]
    public void Regular_ribbon_menu_reflects_mode_and_toggles_without_changing_qat_or_density(RibbonLayoutMode start) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.LayoutMode = start; f.Ribbon.Density = RibbonDensity.Touch; int changed = 0; f.Ribbon.LayoutModeChanged += (_, _) => changed++;
        using var menu = f.Ribbon.CreateCustomizationMenu(f.Copy); var toggle = Assert.IsType<RibbonButtonItem>(Toggle(menu));
        Assert.Equal("Simplified Ribbon", toggle.Text); Assert.Equal(start == RibbonLayoutMode.Simplified, toggle.Icon is RibbonMenuCheckmark);
        toggle.PerformClick(); Assert.Equal(start == RibbonLayoutMode.Classic ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic, f.Ribbon.LayoutMode);
        Assert.Equal(1, changed); Assert.Equal(RibbonDensity.Touch, f.Ribbon.Density); Assert.Same(f.Qat, Assert.Single(f.Ribbon.QuickAccessItems));
        using var next = f.Ribbon.CreateCustomizationMenu(null); Assert.Equal(f.Ribbon.LayoutMode == RibbonLayoutMode.Simplified, Toggle(next)!.Icon is RibbonMenuCheckmark);
        using var qat = f.Ribbon.CreateCustomizationMenu(f.Qat); Assert.Null(Toggle(qat));
    });
    [Theory]
    [InlineData(RibbonQuickAccessPosition.TabRow, RibbonDensity.Compact)] [InlineData(RibbonQuickAccessPosition.TabRow, RibbonDensity.Touch)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonDensity.Compact)] [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonDensity.Touch)]
    public void Qat_panel_and_item_contexts_omit_mode_for_pointer_and_keyboard(RibbonQuickAccessPosition position, RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form(); f.Ribbon.QuickAccessPosition = position; f.Ribbon.Density = density; f.Show(form);
        var panel = f.Ribbon.Snapshot.QuickAccessBounds;
        Assert.True(f.Ribbon.ShowCustomizationMenu(f.Ribbon, f.Ribbon.PointToScreen(panel.Location), null)); Assert.Null(Toggle(f.Ribbon.CustomizationMenu!)); f.Ribbon.CloseCustomizationMenu();
        Assert.True(f.Ribbon.ShowCustomizationMenu(f.Ribbon, f.Ribbon.PointToScreen(new(400, 80)), f.Qat)); Assert.Null(Toggle(f.Ribbon.CustomizationMenu!)); f.Ribbon.CloseCustomizationMenu();
        f.Ribbon.FocusElement(f.Qat); Assert.True(f.Ribbon.ProcessCustomizationKey(Keys.Apps)); Assert.Null(Toggle(f.Ribbon.CustomizationMenu!)); f.Ribbon.CloseCustomizationMenu();
    });
    [Fact]
    public void Qat_group_and_overflow_origins_remain_excluded_while_simplified_more_groups_are_regular_contexts() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form(); f.Show(form);
        var reference = new RibbonQuickAccessGroupReference { Source = f.Group }; f.Ribbon.QuickAccessItems.Add(reference);
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == reference)); var group = f.Ribbon.Popups.Top!;
        Assert.True(f.Ribbon.ShowCustomizationMenu(group, group.PointToScreen(Point.Empty), f.Copy)); Assert.Null(Toggle(f.Ribbon.CustomizationMenu!)); f.Ribbon.Popups.CloseAll(false);
        f.Ribbon.QuickAccessMaxWidth = RibbonQuickAccess.ButtonWidth; f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Ribbon.QuickAccessOverflow)); var overflow = f.Ribbon.Popups.Top!;
        Assert.True(f.Ribbon.ShowCustomizationMenu(overflow, overflow.PointToScreen(Point.Empty), f.Qat)); Assert.Null(Toggle(f.Ribbon.CustomizationMenu!)); f.Ribbon.Popups.CloseAll(false);
        f.Ribbon.QuickAccessItems.Remove(reference); f.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Element == f.Ribbon.Simplified.More)); var more = f.Ribbon.Popups.Top!;
        f.Ribbon.Activate(more.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference), more); var full = f.Ribbon.Popups.Top!;
        Assert.True(f.Ribbon.ShowCustomizationMenu(full, full.PointToScreen(Point.Empty), f.Copy)); Assert.IsType<RibbonMenuCheckmark>(Toggle(f.Ribbon.CustomizationMenu!)!.Icon);
    });
    [Fact]
    public void Invalid_focused_input_blocks_the_context_mode_change() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var form = new Form(); var field = new RibbonTextBoxItem { Id = "name", Text = "Name", Value = "Accepted" }; f.Group.Items.Add(field); f.Show(form);
        field.ValueValidating += (_, e) => e.Cancel = field.Value == "bad"; field.PerformClick(); var native = f.Ribbon.Editors.EditorFor(field)!; native.Text = "bad";
        using var menu = f.Ribbon.CreateCustomizationMenu(f.Copy); Toggle(menu)!.PerformClick(); Assert.Equal(RibbonLayoutMode.Classic, f.Ribbon.LayoutMode); Assert.Null(Toggle(menu)!.Icon);
        native.Text = "Good"; Toggle(menu)!.PerformClick(); Assert.Equal(RibbonLayoutMode.Simplified, f.Ribbon.LayoutMode); Assert.Equal("Good", field.Value);
    });
}
