using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessTests
{
    [Fact]
    public void References_share_source_state_and_inactive_commands_execute_once() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1000 };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup();
        var source = new RibbonToggleItem { Text = "Bold" }; group.Items.Add(source); tab.Groups.Add(group);
        ribbon.Tabs.AddRange(tab, new RibbonTab { Text = "Insert" });
        var first = new RibbonQuickAccessReference { Source = source }; var second = new RibbonQuickAccessReference { Source = source, Text = "Strong" };
        ribbon.QuickAccessItems.AddRange(first, second);
        int clicks = 0, changes = 0; source.Click += (_, _) => clicks++; source.CheckedChanged += (_, _) => changes++;
        ribbon.SelectedTab = ribbon.Tabs[1]; ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, first)));
        Assert.Equal(1, clicks); Assert.Equal(1, changes); Assert.True(source.Checked); Assert.Same(group, source.Parent);
        Assert.Equal("Bold", RibbonQuickAccess.Caption(first)); Assert.Equal("Strong", RibbonQuickAccess.Caption(second));
        source.Text = "Emphasis"; Assert.Equal("Emphasis", RibbonQuickAccess.Caption(first));
        source.Enabled = false; ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, second))); Assert.Equal(1, clicks);
        source.Enabled = true; group.Enabled = false; Assert.False(RibbonQuickAccess.Available(first));
        group.Enabled = true; source.Visible = false; Assert.False(RibbonQuickAccess.Available(first));
        source.Visible = true; tab.Visible = false; Assert.False(RibbonQuickAccess.Available(first));
        tab.Visible = true; ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, second)));
        Assert.Equal(2, clicks); Assert.Equal(2, changes); Assert.False(source.Checked);
    });

    [Fact]
    public void Overflow_toggles_retain_the_surface_and_source_state_while_commands_dismiss() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var home = new RibbonTab(); var other = new RibbonTab(); var group = new RibbonGroup();
        var bold = new RibbonToggleItem { Text = "Bold" };
        var track = new RibbonSplitButtonItem { Text = "Track changes", CheckOnClick = true };
        var paste = new RibbonSplitButtonItem { Text = "Paste" }; var copy = new RibbonButtonItem { Text = "Copy" };
        group.Items.AddRange(bold, track, paste, copy); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        var boldRef = new RibbonQuickAccessReference { Source = bold }; var duplicate = new RibbonQuickAccessReference { Source = bold };
        var trackRef = new RibbonQuickAccessDropDownReference { Source = track }; var pasteRef = new RibbonQuickAccessDropDownReference { Source = paste };
        var owned = new RibbonToggleItem { Text = "Toolbar toggle" }; var copyRef = new RibbonQuickAccessReference { Source = copy };
        ribbon.QuickAccessItems.AddRange(boldRef, duplicate, trackRef, owned, pasteRef, copyRef);
        var calls = new List<RibbonItem>(); var order = new List<string>();
        foreach (var item in new RibbonItem[] { bold, track, owned, paste, copy })
        {
            if (item is RibbonToggleItem toggle) toggle.CheckedChanged += (_, _) => order.Add("check");
            if (item is RibbonSplitButtonItem split) split.CheckedChanged += (_, _) => order.Add("check");
            if (item is RibbonButtonItem button) button.Click += (_, _) => { calls.Add(item); order.Add("click"); };
            if (item is RibbonSplitButtonItem splitButton) splitButton.Click += (_, _) => { calls.Add(item); order.Add("click"); };
        }
        ribbon.SelectedTab = other; form.Show(); Application.DoEvents();
        var surface = Open();
        foreach (var entry in new RibbonElement[] { boldRef, duplicate, trackRef, trackRef, owned, owned })
        {
            order.Clear(); int before = calls.Count;
            Activate(entry);
            Assert.Same(surface, ribbon.Popups.Top); Assert.False(surface.IsDisposed); Assert.True(surface.Visible);
            Assert.Same(entry, surface.FocusedElement); Assert.False(surface.FocusedDropDownPart);
            Assert.Equal(before + 1, calls.Count); Assert.Equal(new[] { "check", "click" }, order);
        }
        Assert.False(bold.Checked); Assert.False(track.Checked); Assert.False(owned.Checked); Assert.Same(other, ribbon.SelectedTab);
        Assert.Equal(new RibbonItem[] { bold, bold, track, track, owned, owned }, calls);
        Activate(pasteRef); Assert.True(surface.IsDisposed); Assert.False(ribbon.Popups.HasOpen); Assert.Same(paste, calls[^1]);
        surface = Open(); Activate(copyRef); Assert.True(surface.IsDisposed); Assert.False(ribbon.Popups.HasOpen); Assert.Same(copy, calls[^1]);

        RibbonPopupSurface Open() { ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow)); return ribbon.Popups.Top!; }
        void Activate(RibbonElement entry) => ribbon.Activate(surface.Targets.Single(t => ReferenceEquals(t.Element, entry) && !t.DropDownPart), ReferenceEquals(entry, duplicate) ? null : surface);
    });

    [Fact]
    public void Overflow_toggle_handlers_can_still_remove_their_presentation_or_hide_the_source() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var source = new RibbonToggleItem { Text = "Bold" }; var reference = new RibbonQuickAccessReference { Source = source };
        ribbon.QuickAccessItems.AddRange(source, reference); form.Show(); Application.DoEvents();
        bool remove = true; int clicks = 0;
        source.Click += (_, _) => { clicks++; if (remove) ribbon.QuickAccessItems.Remove(reference); else source.Visible = false; };
        Activate(); Assert.Equal(1, clicks); Assert.True(source.Checked); Assert.False(ribbon.Popups.HasOpen);
        ribbon.QuickAccessItems.Add(reference); remove = false;
        Activate(); Assert.Equal(2, clicks); Assert.False(source.Checked); Assert.False(ribbon.Popups.HasOpen);
        Assert.False(RibbonQuickAccess.Available(reference));

        void Activate()
        {
            ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow)); var surface = ribbon.Popups.Top!;
            ribbon.Activate(surface.Targets.Single(t => ReferenceEquals(t.Element, reference)), surface);
            Assert.True(surface.IsDisposed);
        }
    });

    [Fact]
    public void Source_removal_reinsertion_disposal_and_foreign_ownership_are_safe() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 700 }; using var other = new RibbonControl();
        var group = new RibbonGroup(); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var source = new RibbonButtonItem { Text = "Copy" }; group.Items.Add(source);
        var reference = new RibbonQuickAccessReference { Source = source }; ribbon.QuickAccessItems.Add(reference);
        int calls = 0; source.Click += (_, _) => calls++;
        group.Items.Remove(source); var target = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference));
        Assert.False(target.Enabled); ribbon.Activate(target); Assert.Equal(0, calls);
        Assert.Same(reference, ribbon.QuickAccessItems[0]); Assert.False(source.IsDisposed);
        group.Items.Add(source); Assert.True(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)).Enabled);
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference))); Assert.Equal(1, calls);
        var foreign = new RibbonButtonItem(); other.QuickAccessItems.Add(foreign);
        Assert.Throws<ArgumentException>(() => reference.Source = foreign); Assert.Same(source, reference.Source);
        Assert.Throws<ArgumentException>(() => other.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = source }));
        source.Dispose(); Assert.False(RibbonQuickAccess.Available(reference));
        ribbon.Activate(target); Assert.Equal(1, calls); reference.Dispose(); Assert.Empty(ribbon.QuickAccessItems);
        var owned = new RibbonButtonItem(); ribbon.QuickAccessItems.Add(owned); ribbon.Dispose(); Assert.True(owned.IsDisposed);
    });

    [Fact]
    public void Reference_disposal_borrows_source_and_collection_rejects_unsupported_or_duplicate_owners() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var source = new RibbonToggleItem(); ribbon.QuickAccessItems.Add(source);
        var reference = new RibbonQuickAccessReference { Source = source }; ribbon.QuickAccessItems.Add(reference); reference.Dispose();
        Assert.False(source.IsDisposed); Assert.Same(ribbon, source.Parent);
        Assert.Throws<InvalidOperationException>(() => ribbon.QuickAccessItems.Add(source));
        Assert.Throws<ArgumentException>(() => ribbon.QuickAccessItems.Add(new RibbonDropDownItem()));
        Assert.Throws<ArgumentException>(() => ribbon.QuickAccessItems.Add(new RibbonCheckBoxItem()));
        Assert.Throws<ArgumentException>(() => ribbon.QuickAccessItems.Add(new RibbonButtonItem { Id = source.Id }));
    });

    [Fact]
    public void Placement_visibility_and_minimize_preserve_order_and_grow_only_downward() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 900 }; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        int height = ribbon.Height; var entries = Enumerable.Range(0, 14).Select(i => new RibbonButtonItem { Text = "Command " + i }).ToArray(); ribbon.QuickAccessItems.AddRange(entries);
        var top = ribbon.Snapshot; Assert.Equal(height, ribbon.Height); Assert.True(top.QuickAccessBounds.Right <= top.TabViewport.Left);
        var topTabs = top.Targets.Single(t => t.Kind == TargetKind.Tab).Bounds;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; var below = ribbon.Snapshot;
        Assert.Equal(height + ribbon.QuickAccessHeight, ribbon.Height); Assert.Equal(ribbon.Width, below.QuickAccessBounds.Width);
        Assert.Equal(top.BodyBounds, below.BodyBounds); Assert.Equal(topTabs.Top, below.Targets.Single(t => t.Kind == TargetKind.Tab).Bounds.Top);
        Assert.Equal(height, below.QuickAccessBounds.Top); Assert.Empty(below.QuickAccessOverflowItems);
        ribbon.QuickAccessVisible = false; Assert.Equal(height, ribbon.Height); Assert.DoesNotContain(ribbon.Snapshot.Targets, t => t.Kind == TargetKind.QuickAccess);
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition); Assert.Equal(entries, ribbon.QuickAccessItems);
        ribbon.QuickAccessVisible = true; ribbon.IsMinimized = true;
        Assert.Equal(ribbon.Snapshot.TabViewport.Height + ribbon.QuickAccessHeight, ribbon.Height);
        Assert.Equal(ribbon.Snapshot.TabViewport.Bottom, ribbon.Snapshot.QuickAccessBounds.Top);
        Assert.Empty(ribbon.Snapshot.Groups); Assert.Equal(entries, ribbon.QuickAccessItems);
        var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.QuickAccessItems)]!;
        Assert.Equal(DesignerSerializationVisibility.Content, property.SerializationVisibility);
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.QuickAccessMaxWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.QuickAccessPosition = (RibbonQuickAccessPosition)22);
    });

    [Fact]
    public void Overflow_keeps_every_visible_declaration_reachable_and_disjoint_at_narrow_widths() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        var entries = Enumerable.Range(0, 70).Select(i => new RibbonButtonItem { Text = "Command " + i }).ToArray(); ribbon.QuickAccessItems.AddRange(entries); entries[3].Visible = false;
        foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
        foreach (int width in new[] { 90, 260, 720, 1100 })
        {
            ribbon.QuickAccessPosition = placement; ribbon.Width = width; var layout = ribbon.Snapshot;
            var visible = layout.Targets.Where(t => t.Kind == TargetKind.QuickAccess).Select(t => t.Element!);
            Assert.Equal(entries.Where(e => e.Visible), visible.Concat(layout.QuickAccessOverflowItems));
            var overflow = Assert.Single(layout.Targets, t => t.Kind == TargetKind.QuickAccessOverflow); Assert.False(overflow.VisibleBounds.IsEmpty);
            Assert.All(layout.Targets.Where(t => t.Kind == TargetKind.Tab), t => Assert.False(t.VisibleBounds.IntersectsWith(overflow.VisibleBounds)));
            using var popup = new RibbonPopupSurface(ribbon, ribbon.QuickAccessOverflow, new(500, 300));
            popup.CreateControl(); popup.PerformLayout();
            Assert.Equal(layout.QuickAccessOverflowItems, popup.Targets.Select(t => t.Element));
            var last = popup.Targets.Last().Element!; popup.FocusElement(last);
            Assert.Contains(popup.Targets, t => ReferenceEquals(t.Element, last) && !t.VisibleBounds.IsEmpty);
            var tips = KeyTipResolver.Resolve(popup.Targets.Select(t => (t.TipText, t.PreferredKeyTip)));
            var preferred = popup.Targets.Select(t => t.PreferredKeyTip).ToArray();
            Assert.All(preferred, key => Assert.DoesNotContain(preferred, other => other != key && other.StartsWith(key, StringComparison.Ordinal)));
            Assert.Equal(tips.Length, tips.Distinct().Count());
            Assert.All(tips, key => Assert.DoesNotContain(tips, other => other != key && other.StartsWith(key, StringComparison.Ordinal)));
        }
    });

    [Fact]
    public void Row_measurement_is_independent_of_host_origin_and_placement_policy() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); ribbon.QuickAccessItems.AddRange(new RibbonButtonItem(), new RibbonToggleItem());
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            // Native measurement uses the owning ribbon's current DPI; callers
            // supply device-pixel slots, including a future title-bar origin.
            var first = RibbonQuickAccessLayout.Build(ribbon, new(0, 0, dpi * 2, dpi / 3), false);
            var translated = RibbonQuickAccessLayout.Build(ribbon, new(73, 19, dpi * 2, dpi / 3), false);
            Assert.Equal(first.QuickAccessBounds with { X = 73, Y = 19 }, translated.QuickAccessBounds);
            Assert.Equal(first.Targets.Select(t => t.Bounds with { X = t.Bounds.X + 73, Y = t.Bounds.Y + 19 }), translated.Targets.Select(t => t.Bounds));
            Assert.Equal(first.Targets.Select(t => t.Element), translated.Targets.Select(t => t.Element));
        }
    });

    [Fact]
    public void ScreenTips_use_source_metadata_and_each_presentation_anchor() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var source = new RibbonButtonItem { Text = "Copy", ScreenTipTitle = "Copy selection", ScreenTipDescription = "Shared metadata." };
        group.Items.Add(source); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var first = new RibbonQuickAccessReference { Source = source }; var second = new RibbonQuickAccessReference { Source = source };
        ribbon.QuickAccessItems.AddRange(first, second); form.Show(); Application.DoEvents();
        var presentations = new RibbonElement[] { source, first, second };
        var anchors = presentations.Select(element =>
        {
            var target = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, element));
            var hit = ribbon.ScreenTips.FindTarget(ribbon.PointToScreen(new(target.VisibleBounds.Left + 2, target.VisibleBounds.Top + 2)));
            Assert.NotNull(hit); Assert.Same(source, hit!.Value.Item);
            Assert.Equal(ribbon.RectangleToScreen(target.VisibleBounds), hit.Value.Anchor); return hit.Value.Anchor;
        }).ToArray();
        Assert.Equal(3, anchors.Distinct().Count());
        source.Enabled = false;
        var firstTarget = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, first));
        Assert.NotNull(ribbon.ScreenTips.FindTarget(ribbon.PointToScreen(new(firstTarget.Bounds.Left + 2, firstTarget.Bounds.Top + 2))));
        source.Dispose(); Assert.Null(ribbon.ScreenTips.FindTarget(ribbon.PointToScreen(firstTarget.Bounds.Location)));
    });
}
