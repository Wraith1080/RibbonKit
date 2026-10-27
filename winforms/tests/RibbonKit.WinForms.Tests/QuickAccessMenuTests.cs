using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessMenuTests
{
    [Fact]
    public void Split_reference_has_disjoint_horizontal_actions_and_independent_keytips() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 900 }; using var foreign = new RibbonControl();
        var tab = new RibbonTab(); var group = new RibbonGroup(); var split = new RibbonSplitButtonItem { Text = "Paste", Layout = RibbonSplitButtonLayout.Vertical };
        group.Items.Add(split); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" });
        var reference = new RibbonQuickAccessDropDownReference { Source = split }; ribbon.QuickAccessItems.Add(reference);
        var targets = ribbon.Snapshot.Targets.Where(t => ReferenceEquals(t.Element, reference)).ToArray();
        Assert.Equal(2, targets.Length); Assert.False(targets[0].DropDownPart); Assert.True(targets[1].DropDownPart);
        Assert.Equal(targets[0].Bounds.Right, targets[1].Bounds.Left); Assert.Equal(targets[0].Bounds.Top, targets[1].Bounds.Top);
        Assert.Equal("2A", targets[0].PreferredKeyTip); Assert.Equal("2B", targets[1].PreferredKeyTip);
        reference.KeyTip = "P"; reference.DropDownKeyTip = "M";
        targets = ribbon.Snapshot.Targets.Where(t => ReferenceEquals(t.Element, reference)).ToArray();
        Assert.Equal("P", targets[0].PreferredKeyTip); Assert.Equal("M", targets[1].PreferredKeyTip);
        reference.KeyTip = ""; reference.DropDownKeyTip = "";
        Assert.Equal("2A", targets[0].PreferredKeyTip); Assert.Equal("2B", targets[1].PreferredKeyTip);
        ribbon.QuickAccessMaxWidth = 28;
        using var popup = new RibbonPopupSurface(ribbon, ribbon.QuickAccessOverflow, new(1000, 600));
        var rows = popup.Targets.Where(t => ReferenceEquals(t.Element, reference)).ToArray();
        Assert.Equal(2, rows.Length); Assert.Equal(rows[0].Bounds.Right, rows[1].Bounds.Left); Assert.True(rows[1].Bounds.Width > targets[1].Bounds.Width);
        Assert.Equal(typeof(RibbonDropDownItem), TypeDescriptor.GetProperties(reference)[nameof(reference.Source)]!.PropertyType);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(reference)[nameof(reference.IsDropDownOpen)]!.SerializationVisibility);
        Assert.Throws<ArgumentException>(() => foreign.QuickAccessItems.Add(new RibbonQuickAccessDropDownReference { Source = split }));
        reference.Dispose(); Assert.False(split.IsDisposed); Assert.Same(group, split.Parent);
    });

    [Fact]
    public void Inactive_source_menu_and_primary_action_share_models_and_events_once() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var home = new RibbonTab(); var other = new RibbonTab(); var group = new RibbonGroup();
        var source = new RibbonSplitButtonItem { Text = "Paste", CheckOnClick = true }; var choice = new RibbonButtonItem { Text = "Plain text" }; source.Items.Add(choice);
        group.Items.Add(source); home.Groups.Add(group); ribbon.Tabs.AddRange(home, other);
        var reference = new RibbonQuickAccessDropDownReference { Source = source }; ribbon.QuickAccessItems.Add(reference);
        int clicks = 0, chosen = 0, opening = 0, closed = 0; var order = new List<string>();
        source.Click += (_, _) => { clicks++; order.Add("click"); }; source.CheckedChanged += (_, _) => order.Add("check");
        source.DropDownOpening += (_, _) => opening++; source.DropDownClosed += (_, _) => closed++; choice.Click += (_, _) => chosen++;
        ribbon.SelectedTab = other; form.Show(); Application.DoEvents();
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference) && !t.DropDownPart));
        Assert.Equal(1, clicks); Assert.True(source.Checked); Assert.Equal(new[] { "check", "click" }, order); Assert.Equal(0, opening);
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference) && t.DropDownPart));
        Assert.True(reference.IsDropDownOpen); Assert.False(source.IsDropDownOpen); Assert.Equal(1, opening); Assert.Equal(1, clicks);
        Assert.Same(source, ribbon.Popups.Top!.MenuSource); Assert.Same(source, choice.Parent);
        ribbon.Activate(ribbon.Popups.Top.Targets.Single(t => ReferenceEquals(t.Element, choice)), ribbon.Popups.Top);
        Assert.Equal(1, chosen); Assert.Equal(1, closed); Assert.False(reference.IsDropDownOpen); Assert.Same(other, ribbon.SelectedTab);
        Assert.Same(source, choice.Parent); Assert.True(source.Checked);
    });

    [Fact]
    public void Opening_callbacks_source_switches_and_disabled_references_cannot_leave_stale_menus() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var source = new RibbonDropDownItem { Text = "Select" };
        source.Items.Add(new RibbonButtonItem { Text = "All" }); group.Items.Add(source); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var first = new RibbonQuickAccessDropDownReference { Source = source }; var second = new RibbonQuickAccessDropDownReference { Source = source };
        ribbon.QuickAccessItems.AddRange(first, second); form.Show(); Application.DoEvents();
        bool cancel = true; source.DropDownOpening += (_, e) => e.Cancel = cancel;
        Open(first); Assert.False(first.IsDropDownOpen); cancel = false;
        Open(first); Assert.True(first.IsDropDownOpen); Open(second); Assert.False(first.IsDropDownOpen); Assert.True(second.IsDropDownOpen); Assert.Single(ribbon.Popups.Surfaces);
        source.ShowDropDown(); Assert.True(source.IsDropDownOpen); Assert.False(second.IsDropDownOpen); Assert.Single(ribbon.Popups.Surfaces);
        source.CloseDropDown(); Open(first); source.CloseDropDown(); Assert.False(first.IsDropDownOpen);
        var dynamic = new RibbonButtonItem { Text = "Dynamic" };
        source.DropDownOpening += (_, _) => { if (!source.Items.Contains(dynamic)) source.Items.Add(dynamic); dynamic.Text += "!"; };
        ribbon.QuickAccessMaxWidth = 28;
        var opener = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow);
        ribbon.Activate(opener); var overflow = ribbon.Popups.Top!;
        ribbon.Activate(overflow.Targets.Single(t => ReferenceEquals(t.Element, first)), overflow);
        Assert.True(first.IsDropDownOpen); Assert.False(overflow.IsDisposed); Assert.Equal(2, ribbon.Popups.Surfaces.Count());
        Assert.Contains(ribbon.Popups.Top!.Targets, t => ReferenceEquals(t.Element, dynamic));
        ribbon.Popups.CloseAll(false); ribbon.QuickAccessMaxWidth = 240;
        Open(first); first.Enabled = false; Assert.False(first.IsDropDownOpen); first.Enabled = true;
        Open(first); source.Enabled = false; Assert.False(first.IsDropDownOpen); source.Enabled = true;
        CancelEventHandler replace = (_, _) => first.Source = null; source.DropDownOpening += replace;
        Open(first); Assert.False(first.IsDropDownOpen); Assert.False(ribbon.Popups.HasOpen); source.DropDownOpening -= replace;
        first.Source = source; Open(first); group.Items.Remove(source); Assert.False(first.IsDropDownOpen); Assert.False(RibbonQuickAccess.Available(first));
        group.Items.Add(source); Open(first); source.Dispose(); Assert.False(first.IsDropDownOpen); Assert.False(RibbonQuickAccess.Available(first));

        void Open(RibbonQuickAccessDropDownReference reference) => ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, reference)));
    });

    [Fact]
    public void Projected_menu_borrows_one_native_editor_across_presentations() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var source = new RibbonDropDownItem(); var text = new RibbonTextBoxItem { Text = "Name", Value = "Document" };
        source.Items.Add(text); group.Items.Add(source); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var first = new RibbonQuickAccessDropDownReference { Source = source }; var second = new RibbonQuickAccessDropDownReference { Source = source }; ribbon.QuickAccessItems.AddRange(first, second);
        form.Show(); Application.DoEvents();
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, first)));
        var editor = ribbon.Editors.EditorFor(text)!; nint handle = editor.Handle; var firstSurface = ribbon.Popups.Top!;
        Assert.True(firstSurface.Contains(editor)); Assert.Same(source, text.Parent);
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, second)));
        Assert.True(firstSurface.IsDisposed); Assert.Same(editor, ribbon.Editors.EditorFor(text)); Assert.Equal(handle, editor.Handle); Assert.True(ribbon.Popups.Top!.Contains(editor));
        source.CloseDropDown(); Assert.False(editor.IsDisposed); Assert.False(editor.Visible); Assert.Same(source, text.Parent);
    });
}
