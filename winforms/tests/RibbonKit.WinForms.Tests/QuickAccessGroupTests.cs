using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessGroupTests
{
    [Fact]
    public void Membership_collapses_without_changing_authored_policy_and_last_removal_restores_adaptive_layout() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 1000 }; var tab = new RibbonTab();
        var group = new RibbonGroup { Text = "Formatting", CanResize = false, ReductionMode = RibbonGroupReductionMode.Resize, ReductionPriority = 7 };
        for (int i = 0; i < 4; i++) group.Items.Add(new RibbonButtonItem { Text = "Command " + i });
        tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        int expanded = ribbon.Snapshot.Groups.Single().Bounds.Width;
        var first = new RibbonQuickAccessGroupReference { Source = group }; var second = new RibbonQuickAccessGroupReference { Source = group };
        ribbon.QuickAccessItems.AddRange(first, second);
        Assert.True(ribbon.Snapshot.Groups.Single().Collapsed); Assert.True(ribbon.Snapshot.Groups.Single().Bounds.Width < expanded);
        Assert.False(group.CanResize); Assert.Equal(RibbonGroupReductionMode.Resize, group.ReductionMode); Assert.Equal(7, group.ReductionPriority);
        Assert.DoesNotContain(ribbon.Snapshot.Targets, t => t.Element is RibbonItem);
        first.Visible = false; second.Enabled = false; ribbon.QuickAccessVisible = false;
        Assert.True(ribbon.Snapshot.Groups.Single().Collapsed);
        ribbon.QuickAccessItems.Remove(first); Assert.True(ribbon.Snapshot.Groups.Single().Collapsed);
        ribbon.QuickAccessItems.Remove(second); Assert.False(ribbon.Snapshot.Groups.Single().Collapsed); Assert.Equal(expanded, ribbon.Snapshot.Groups.Single().Bounds.Width);
        group.CanResize = true; group.ReductionMode = RibbonGroupReductionMode.Collapse; ribbon.Width = 150;
        ribbon.QuickAccessItems.Add(first); ribbon.QuickAccessItems.Remove(first); Assert.True(ribbon.Snapshot.Groups.Single().Collapsed);
        Assert.Same(group, group.Items[0].Parent); Assert.False(group.IsDisposed);
        Assert.Equal(typeof(RibbonGroup), TypeDescriptor.GetProperties(first)[nameof(first.Source)]!.PropertyType);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(first)[nameof(first.IsDropDownOpen)]!.SerializationVisibility);
    });

    [Fact]
    public void Group_presentations_reuse_one_native_editor_and_toggles_keep_the_shared_flyout_open() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1100 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup { Text = "Formatting" };
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Document" }; var toggle = new RibbonToggleItem { Text = "Bold" };
        var check = new RibbonCheckBoxItem { Text = "Ruler" }; var split = new RibbonSplitButtonItem { Text = "Track", CheckOnClick = true };
        var radioA = new RibbonRadioButtonItem { Text = "Normal", Checked = true }; var radioB = new RibbonRadioButtonItem { Text = "Print" };
        var command = new RibbonButtonItem { Text = "Copy" };
        group.Items.AddRange(field, toggle, check, radioA, radioB, split, command); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Show(); Application.DoEvents();
        var editor = ribbon.Editors.EditorFor(field)!; nint handle = editor.Handle;
        var first = new RibbonQuickAccessGroupReference { Source = group }; var second = new RibbonQuickAccessGroupReference { Source = group };
        ribbon.QuickAccessItems.AddRange(first, second); Assert.False(editor.Visible); Assert.False(editor.IsDisposed);
        Open(first); var firstSurface = ribbon.Popups.Top!; Assert.Same(group, firstSurface.GroupSource); Assert.True(firstSurface.Contains(editor));
        Open(second); Assert.True(firstSurface.IsDisposed); Assert.False(first.IsDropDownOpen); Assert.True(second.IsDropDownOpen); Assert.Single(ribbon.Popups.Surfaces);
        Assert.Same(editor, ribbon.Editors.EditorFor(field)); Assert.Equal(handle, editor.Handle); Assert.True(ribbon.Popups.Top!.Contains(editor));
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, group)));
        Assert.False(second.IsDropDownOpen); Assert.False(ribbon.Popups.HasOpen);
        Assert.Same(editor, ribbon.Editors.EditorFor(field)); Assert.Equal(handle, editor.Handle);
        // A source group button closes the borrowed presentation; the next press
        // reopens the same group from its source, matching the retained-group contract.
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, group)));
        var sourceSurface = ribbon.Popups.Top!; Assert.Same(group, sourceSurface.OwnerElement); Assert.True(sourceSurface.Contains(editor)); Assert.Equal(handle, editor.Handle);
        int calls = 0; toggle.Click += (_, _) => calls++; check.Click += (_, _) => calls++; radioB.Click += (_, _) => calls++; split.Click += (_, _) => calls++; command.Click += (_, _) => calls++;
        foreach (var item in new RibbonItem[] { toggle, check, radioB, split })
        {
            ribbon.Activate(sourceSurface.Targets.Single(t => ReferenceEquals(t.Element, item) && !t.DropDownPart), sourceSurface);
            Assert.Same(sourceSurface, ribbon.Popups.Top); Assert.Same(item, sourceSurface.FocusedElement);
        }
        Assert.True(toggle.Checked); Assert.True(check.Checked); Assert.True(radioB.Checked); Assert.False(radioA.Checked); Assert.True(split.Checked); Assert.Equal(4, calls);
        ribbon.Activate(sourceSurface.Targets.Single(t => ReferenceEquals(t.Element, command)), sourceSurface);
        Assert.Equal(5, calls); Assert.False(ribbon.Popups.HasOpen); Assert.False(editor.IsDisposed); Assert.Equal(handle, editor.Handle);

        void Open(RibbonQuickAccessGroupReference reference) => ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)));
    });

    [Fact]
    public void Source_switch_disable_detach_reinsert_and_disposal_close_stale_flyouts_without_owning_sources() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon); using var foreign = new RibbonControl();
        var tab = new RibbonTab(); var group = new RibbonGroup { Text = "Formatting" }; var alternate = new RibbonGroup { Text = "Other" };
        var field = new RibbonTextBoxItem { Value = "Document" }; group.Items.Add(field); tab.Groups.AddRange(group, alternate); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessGroupReference { Source = group }; ribbon.QuickAccessItems.Add(reference); form.Show(); Application.DoEvents();
        Assert.Throws<ArgumentException>(() => foreign.QuickAccessItems.Add(new RibbonQuickAccessGroupReference { Source = group }));
        Open(); var editor = ribbon.Editors.EditorFor(field)!; group.Enabled = false; Assert.False(reference.IsDropDownOpen); Assert.False(RibbonQuickAccess.Available(reference)); group.Enabled = true;
        Open(); reference.Enabled = false; Assert.False(reference.IsDropDownOpen); reference.Enabled = true;
        Open(); reference.Source = alternate; Assert.False(reference.IsDropDownOpen); Assert.False(RibbonQuickAccess.RetainsGroup(group)); Assert.True(RibbonQuickAccess.RetainsGroup(alternate));
        reference.Source = group; Open(); tab.Groups.Remove(group); Assert.False(reference.IsDropDownOpen); Assert.False(RibbonQuickAccess.Available(reference));
        Assert.True(editor.IsDisposed); Assert.Equal("Document", field.Value); Assert.False(field.IsDisposed);
        tab.Groups.Add(group); Open(); Assert.True(reference.IsDropDownOpen); var replacement = ribbon.Editors.EditorFor(field)!; Assert.NotSame(editor, replacement);
        group.Dispose(); Assert.False(reference.IsDropDownOpen); Assert.True(replacement.IsDisposed);
        reference.Source = alternate; reference.Dispose(); Assert.False(alternate.IsDisposed); Assert.False(RibbonQuickAccess.RetainsGroup(alternate));

        void Open() => ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)));
    });

    [Fact]
    public void Inactive_group_opens_beside_overflow_and_nested_dismissal_restores_each_opener() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl { QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var home = new RibbonTab(); var other = new RibbonTab(); var group = new RibbonGroup { Text = "Formatting" };
        var menu = new RibbonDropDownItem { Text = "More" }; menu.Items.Add(new RibbonButtonItem { Text = "Action" }); group.Items.Add(menu);
        home.Groups.Add(group); ribbon.Tabs.AddRange(home, other); var reference = new RibbonQuickAccessGroupReference { Source = group }; ribbon.QuickAccessItems.Add(reference);
        ribbon.SelectedTab = other; form.Show(); Application.DoEvents();
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow)); var overflow = ribbon.Popups.Top!;
        var row = overflow.Targets.Single(t => ReferenceEquals(t.Element, reference));
        ribbon.Activate(row); var flyout = ribbon.Popups.Top!;
        Assert.True(reference.IsDropDownOpen); Assert.Equal(2, ribbon.Popups.Surfaces.Count()); Assert.Same(other, ribbon.SelectedTab);
        Assert.True(flyout.PointToScreen(Point.Empty).X >= overflow.RectangleToScreen(row.VisibleBounds).Right);
        ribbon.Activate(flyout.Targets.Single(t => ReferenceEquals(t.Element, menu)), flyout); Assert.True(menu.IsDropDownOpen);
        ribbon.Popups.CloseTop(true); Assert.True(reference.IsDropDownOpen); Assert.Same(menu, flyout.FocusedElement);
        ribbon.Popups.CloseTop(true); Assert.False(reference.IsDropDownOpen); Assert.Same(reference, overflow.FocusedElement);
        ribbon.Popups.CloseTop(true); Assert.False(ribbon.Popups.HasOpen); Assert.True(ribbon.Focused);
        Assert.Same(group, menu.Parent);
    });
}
