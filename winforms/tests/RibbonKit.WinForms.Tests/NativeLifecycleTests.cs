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
        Assert.Equal(RibbonLayoutEngine.Scale(7, ribbon.DeviceDpi), ribbon.Snapshot.BodyBounds.Left);
        Assert.Equal(RibbonLayoutEngine.Scale(7, ribbon.DeviceDpi), ribbon.Width - ribbon.Snapshot.BodyBounds.Right);
        int reserved = ribbon.Width - ribbon.Snapshot.BodyViewport.Width;
        ribbon.Width = full + reserved - 1;
        Assert.Same(tab.Groups[2], Assert.Single(ribbon.Snapshot.Groups, g => g.Collapsed).Group);
        var collapsed = Assert.Single(ribbon.Snapshot.Groups, g => g.Collapsed);
        Assert.Equal(ribbon.Snapshot.BodyBounds.Top + RibbonLayoutEngine.Scale(6, ribbon.DeviceDpi), collapsed.Bounds.Top);
        Assert.Equal(ribbon.Snapshot.BodyBounds.Bottom - RibbonLayoutEngine.Scale(8, ribbon.DeviceDpi), collapsed.Bounds.Bottom);
        Assert.Equal(collapsed.Bounds, ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, collapsed.Group)).Bounds);
        Assert.False(ribbon.Snapshot.Groups[0].Collapsed);
        Assert.False(ribbon.Snapshot.Groups[3].Collapsed);
    });

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Activation_handlers_may_detach_item_or_dispose_ribbon(int action) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 700 };
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var item = new RibbonButtonItem();
        group.Items.Add(item);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        int clicks = 0;
        item.Click += (_, _) => { clicks++; if (action == 0) group.Items.Remove(item); else ribbon.Dispose(); };
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
        var unavailable = new List<Exception>();
        int uiThread = Environment.CurrentManagedThreadId;
        void FirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (Environment.CurrentManagedThreadId == uiThread && e.Exception is System.Runtime.InteropServices.COMException { HResult: unchecked((int)0x80040201) })
                unavailable.Add(e.Exception);
        }
        AppDomain.CurrentDomain.FirstChanceException += FirstChance;
        RibbonKit.WinForms.Accessibility.RibbonAutomationRoot? closedProvider = null;
        try
        {
            for (int i = 0; i < 5; i++)
            {
                menu.ShowDropDown();
                Assert.True(ribbon.Popups.HasOpen);
                var provider = ribbon.Popups.Top!.AutomationRoot;
                closedProvider = provider;
                menu.CloseDropDown();
                Assert.True(provider.Disconnected);
                Assert.False(ribbon.Popups.HasOpen);
            }
        }
        finally { AppDomain.CurrentDomain.FirstChanceException -= FirstChance; }
        Assert.Empty(unavailable);
        Assert.Throws<System.Runtime.InteropServices.COMException>(() => { _ = closedProvider!.HostRawElementProvider; });
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
        foreach (var scroll in ribbon.Snapshot.Targets.Where(t => t.Kind is TargetKind.BodyBack or TargetKind.BodyNext))
        {
            Assert.Equal(ribbon.Snapshot.BodyBounds.Top + RibbonLayoutEngine.Scale(2, ribbon.DeviceDpi), scroll.Bounds.Top);
            Assert.Equal(ribbon.Snapshot.BodyBounds.Bottom - RibbonLayoutEngine.Scale(2, ribbon.DeviceDpi), scroll.Bounds.Bottom);
            Assert.Equal(RibbonLayoutEngine.Scale(22, ribbon.DeviceDpi), scroll.Bounds.Width);
            int sideClearance = scroll.Kind == TargetKind.BodyBack ? scroll.Bounds.Left - ribbon.Snapshot.BodyBounds.Left
                : ribbon.Snapshot.BodyBounds.Right - scroll.Bounds.Right;
            Assert.Equal(sideClearance, scroll.Bounds.Top - ribbon.Snapshot.BodyBounds.Top);
        }
        foreach (var scroll in ribbon.Snapshot.Targets.Where(t => t.Kind is TargetKind.TabBack or TargetKind.TabNext))
        {
            Assert.Equal(0, scroll.Bounds.Top);
            Assert.Equal(ribbon.Snapshot.TabViewport.Height, scroll.Bounds.Height);
        }
        var before = ribbon.Snapshot.Targets.Select(t => t.Bounds).ToArray();
        ribbon.Refresh();
        Assert.Equal(before, ribbon.Snapshot.Targets.Select(t => t.Bounds).ToArray());
        var originalTheme = ribbon.Theme;
        var originalBody = ribbon.Snapshot.BodyBounds;
        var originalViewport = ribbon.Snapshot.BodyViewport;
        Assert.Equal(2, originalTheme.BodyScrollButtonVerticalInset);
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = originalTheme with { BodyScrollButtonVerticalInset = -1 });
        Assert.Same(originalTheme, ribbon.Theme);
        foreach (int inset in new[] { 0, 6, int.MaxValue })
        {
            ribbon.Theme = originalTheme with { BodyScrollButtonVerticalInset = inset };
            int expected = Math.Min(RibbonLayoutEngine.Scale(Math.Min(inset, ribbon.LayoutTheme.BodyHeight), ribbon.DeviceDpi), originalBody.Height / 2);
            foreach (var scroll in ribbon.Snapshot.Targets.Where(t => t.Kind is TargetKind.BodyBack or TargetKind.BodyNext))
            {
                Assert.Equal(originalBody.Top + expected, scroll.Bounds.Top);
                Assert.Equal(originalBody.Bottom - expected, scroll.Bounds.Bottom);
            }
            Assert.Equal(originalBody, ribbon.Snapshot.BodyBounds);
            Assert.Equal(originalViewport, ribbon.Snapshot.BodyViewport);
        }
        ribbon.Theme = originalTheme;
    });

    [Fact]
    public void Attached_model_rejects_cross_thread_changes_before_mutation() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        var originalTheme = ribbon.Theme;
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
        Assert.Same(originalTheme, ribbon.Theme);
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
