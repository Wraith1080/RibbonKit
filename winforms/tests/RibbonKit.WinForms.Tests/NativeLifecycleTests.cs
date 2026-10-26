using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class NativeLifecycleTests
{
    [Fact]
    public void Cached_accessibility_group_keeps_its_parent_after_tab_selection_changes() => FoundationTests.Sta(() =>
    {
        using var form = new Form(); var ribbon = new RibbonControl(); var home = new RibbonTab(); var insert = new RibbonTab(); var group = new RibbonGroup();
        home.Groups.Add(group); ribbon.Tabs.AddRange(home, insert); form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        var provider = ribbon.AutomationRoot.Node(group);
        ribbon.SelectedTab = insert;
        Assert.Same(home, provider.Parent!.Element); Assert.Equal(Rectangle.Empty, provider.ScreenBounds);
        ribbon.SelectedTab = null;
        Assert.Same(home, provider.Parent!.Element);
    });
    [Fact]
    public void Native_caller_honours_explicit_priority_ties_before_unspecified_width() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 4000 };
        var tab = new RibbonTab();
        ribbon.Tabs.Add(tab);
        for (int i = 0; i < 4; i++)
        {
            var group = new RibbonGroup { Text = "Group " + i, ReductionPriority = i == 3 ? null : 10, CanResize = i != 0 };
            for (int j = 0; j < (i == 3 ? 6 : 3); j++)
                group.Items.Add(new RibbonButtonItem { Text = "Command " + j });
            tab.Groups.Add(group);
        }
        int full = ribbon.Snapshot.BodyExtent;
        ribbon.Width = full + RibbonLayoutEngine.Scale(8, ribbon.DeviceDpi) - 1;
        Assert.Same(tab.Groups[2], Assert.Single(ribbon.Snapshot.Groups, g => g.Collapsed).Group);
        Assert.False(ribbon.Snapshot.Groups[0].Collapsed);
        Assert.False(ribbon.Snapshot.Groups[3].Collapsed);
    });

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Activation_handlers_may_detach_dispose_item_or_dispose_ribbon(int action) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 700 };
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var item = new RibbonButtonItem();
        group.Items.Add(item);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        int clicks = 0;
        item.Click += (_, _) => { clicks++; if (action == 0) group.Items.Remove(item); else if (action == 1) item.Dispose(); else ribbon.Dispose(); };
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, item)));
        Assert.Equal(1, clicks);
        Assert.Empty(group.Items);
        if (action == 0)
            item.Dispose();
    });

    [Fact]
    public void Popups_cancel_reopen_and_close_on_model_removal_or_disposal() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 };
        var ribbon = new RibbonControl();
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var menu = new RibbonDropDownItem { Text = "Menu" };
        menu.Items.Add(new RibbonButtonItem { Text = "Choice" });
        group.Items.Add(menu);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon);
        form.Show();
        Application.DoEvents();
        int closed = 0;
        menu.DropDownClosed += (_, _) => closed++;
        System.ComponentModel.CancelEventHandler cancel = (_, e) => e.Cancel = true;
        menu.DropDownOpening += cancel;
        menu.ShowDropDown();
        Assert.False(ribbon.Popups.HasOpen);
        menu.DropDownOpening -= cancel;
        for (int i = 0; i < 5; i++)
        {
            menu.ShowDropDown();
            Assert.True(ribbon.Popups.HasOpen);
            menu.CloseDropDown();
            Assert.False(ribbon.Popups.HasOpen);
        }
        Assert.Equal(5, closed);
        menu.ShowDropDown();
        ribbon.Visible = false;
        Assert.False(ribbon.Popups.HasOpen);
        ribbon.Visible = true;
        menu.ShowDropDown();
        group.Items.Remove(menu);
        Assert.False(ribbon.Popups.HasOpen);
        Assert.False(menu.IsDisposed);
        group.Items.Add(menu);
        menu.ShowDropDown();
        form.Dispose();
        Assert.True(menu.IsDisposed);
        Assert.False(ribbon.Popups.HasOpen);
    });

    [Fact]
    public void Overflow_focus_reveals_last_tab_and_fixed_group_command() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 140 };
        for (int i = 0; i < 8; i++)
            ribbon.Tabs.Add(new RibbonTab { Text = "Tab number " + i });
        var last = ribbon.Tabs[^1];
        ribbon.SelectedTab = last;
        ribbon.FocusElement(last);
        Assert.True(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, last)).VisibleBounds.Width > 0);
        var group = new RibbonGroup { CanResize = false };
        for (int i = 0; i < 10; i++)
            group.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        last.Groups.Add(group);
        var command = group.Items[^1];
        ribbon.FocusElement(command);
        Assert.False(Assert.Single(ribbon.Snapshot.Groups).Collapsed);
        Assert.True(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, command)).VisibleBounds.Width > 0);
        var before = ribbon.Snapshot.Targets.Select(t => t.Bounds).ToArray();
        ribbon.Refresh();
        Assert.Equal(before, ribbon.Snapshot.Targets.Select(t => t.Bounds).ToArray());
    });

    [Fact]
    public void Attached_model_rejects_cross_thread_changes_before_mutation() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var toggle = new RibbonToggleItem();
        group.Items.Add(toggle);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                Assert.Throws<InvalidOperationException>(() => toggle.Checked = true);
                Assert.Throws<InvalidOperationException>(() => group.ReductionPriority = 9);
                Assert.Throws<InvalidOperationException>(() => ribbon.Theme = new RibbonTheme { TabHeight = 42 });
            }
            catch (Exception e) { failure = e; }
        });
        worker.Start();
        Assert.True(worker.Join(5000));
        if (failure is not null)
            throw failure;
        Assert.False(toggle.Checked);
        Assert.Null(group.ReductionPriority);
        Assert.Equal(32, ribbon.Theme.TabHeight);
    });

    [Fact]
    public void Runtime_font_and_text_changes_remeasure_neighbours() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1500 };
        var tab = new RibbonTab();
        var group = new RibbonGroup { CanResize = false };
        var item = new RibbonButtonItem { Text = "A", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        group.Items.Add(item);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        int width = ribbon.Snapshot.Targets.Single(t => t.Element == item).Bounds.Width, height = ribbon.Height;
        item.Text = "A substantially longer label";
        Assert.True(ribbon.Snapshot.Targets.Single(t => t.Element == item).Bounds.Width > width);
        using var largerFont = new Font(ribbon.Font.FontFamily, 20);
        ribbon.Font = largerFont;
        Assert.True(ribbon.Height > height);
        int rowHeight = ribbon.Snapshot.Targets.Single(t => t.Element == item).Bounds.Height;
        Assert.True(rowHeight >= largerFont.Height);
        Assert.DoesNotContain(ribbon.Snapshot.Targets, t => t.Element == item && t.Bounds.Bottom > ribbon.Snapshot.BodyViewport.Bottom);
    });
}
