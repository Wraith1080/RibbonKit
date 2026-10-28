using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MergeTests
{
    [Fact]
    public void Ordering_is_stable_across_sources_cycles_and_live_order_changes_without_transferring_ownership() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl(); var home = new RibbonTab { Id = "home" }; var insert = new RibbonTab { Id = "insert" }; host.Tabs.AddRange(home, insert);
        var original = new RibbonGroup { Id = "original" }; home.Groups.Add(original);
        using var first = Source("first", "home", 0); using var second = Source("second", "home", 0); using var before = Source("before", "home", -5);
        int changes = 0; first.IsMergedChanged += (_, _) => changes++;
        Assert.True(host.Merge(first)); Assert.False(host.Merge(first)); host.Merge(second); host.Merge(before);
        Assert.Equal(new[] { before.Tabs[0], home, insert, first.Tabs[0], second.Tabs[0] }, host.GetPresentedTabs());
        Assert.Equal(new[] { before.Groups[0].Group, original, first.Groups[0].Group, second.Groups[0].Group }, host.GetPresentedGroups(home));
        Assert.Equal(new[] { home, insert }, host.Tabs); Assert.Same(original, Assert.Single(home.Groups)); Assert.Same(first, first.Tabs[0].Parent); Assert.Same(first.Groups[0], first.Groups[0].Group.Parent);
        var snapshot = host.GetPresentedTabs();
        for (int i = 0; i < 3; i++)
        {
            Assert.True(host.Unmerge(first)); Assert.False(host.Unmerge(first)); host.Unmerge(second); host.Merge(second); host.Merge(first);
            Assert.Equal(snapshot, host.GetPresentedTabs());
            Assert.Equal(new[] { before.Groups[0].Group, original, first.Groups[0].Group, second.Groups[0].Group }, host.GetPresentedGroups(home));
        }
        first.Order = -10; first.Groups[0].Order = -10;
        Assert.Same(first.Tabs[0], host.GetPresentedTabs()[0]); Assert.Same(first.Groups[0].Group, host.GetPresentedGroups(home)[0]);
        Assert.Same(first, RibbonControl.GetMergeSource(first.Groups[0].Group.Items[0])); Assert.Same(host, first.MergedInto); Assert.Equal(7, changes);
        host.Unmerge(first); Assert.Null(first.MergedInto); Assert.False(first.IsMerged); Assert.Same(first, RibbonControl.GetMergeSource(first.Tabs[0]));
    });

    [Fact]
    public void Identity_validation_is_atomic_and_missing_group_targets_reconcile_with_authored_tabs() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl(); var home = new RibbonTab { Id = "home" }; host.Tabs.Add(home);
        using var source = Source("source", "missing", 0); source.Tabs[0].Id = "home";
        Assert.Throws<ArgumentException>(() => host.Merge(source)); Assert.False(source.IsMerged); Assert.Equal(new[] { home }, host.GetPresentedTabs());
        source.Tabs[0].Id = "tools"; Assert.True(host.Merge(source)); Assert.Empty(host.GetPresentedGroups(home));
        Assert.False(source.Groups[0].Group.EffectiveEnabled);
        var missing = new RibbonTab { Id = "missing", Text = "New target" }; host.Tabs.Add(missing);
        Assert.Same(source.Groups[0].Group, Assert.Single(host.GetPresentedGroups(missing))); Assert.True(source.Groups[0].Group.EffectiveEnabled);
        missing.Visible = false; Assert.False(source.Groups[0].Group.EffectiveEnabled); missing.Visible = true;
        Assert.Throws<ArgumentException>(() => source.Tabs.Add(new RibbonTab { Id = "home" }));
        Assert.Throws<ArgumentException>(() => source.Tabs[0].Id = "home"); Assert.Equal("tools", source.Tabs[0].Id);
        using var other = new RibbonControl(); Assert.Throws<InvalidOperationException>(() => other.Merge(source)); Assert.Same(host, source.MergedInto);
        source.Groups[0].TargetTabId = "home"; Assert.Same(source.Groups[0].Group, Assert.Single(host.GetPresentedGroups(home))); Assert.Empty(host.GetPresentedGroups(missing));
        host.Tabs.Remove(home); Assert.False(source.Groups[0].Group.EffectiveEnabled); Assert.True(source.IsMerged);
        host.Tabs.Add(home); Assert.True(source.Groups[0].Group.EffectiveEnabled);
    });

    [Fact]
    public void Selection_preserves_surviving_host_tabs_and_falls_back_deterministically_when_sources_or_tabs_leave() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl(); var home = new RibbonTab { Id = "home" }; var insert = new RibbonTab { Id = "insert" }; host.Tabs.AddRange(home, insert); host.SelectedTab = insert;
        using var before = Source("before", "home", -5); using var after = Source("after", "home", 5);
        host.Merge(before); host.Merge(after); Assert.Same(insert, host.SelectedTab);
        host.SelectedTab = before.Tabs[0]; host.Unmerge(before); Assert.Same(home, host.SelectedTab);
        host.SelectedTab = after.Tabs[0]; host.Unmerge(after); Assert.Same(insert, host.SelectedTab);
        host.Merge(before); host.SelectedTab = home; host.Tabs.Remove(home); Assert.Same(insert, host.SelectedTab);
        host.SelectedTab = before.Tabs[0]; before.Visible = false; Assert.Same(insert, host.SelectedTab); before.Visible = true;
        host.SelectedTab = before.Tabs[0]; before.Tabs.Clear(); Assert.Same(insert, host.SelectedTab); Assert.True(before.IsMerged);
    });

    [Fact]
    public void Native_presenters_bindings_and_qat_references_survive_unmerge_and_remerge() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1200, Height = 600 }; var host = new RibbonControl(); form.Controls.Add(host);
        var home = new RibbonTab { Id = "home", Text = "Home" }; host.Tabs.Add(home);
        using var source = Source("tools", "home", 0); var tools = source.Tabs[0]; var group = tools.Groups[0];
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Child document" }; var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true };
        gallery.Items.AddRange(new RibbonGalleryTile { Text = "First" }, new RibbonGalleryTile { Text = "Second" }); group.Items.AddRange(field, gallery);
        using var binding = new BindingSource(); var state = new ChildState(); binding.DataSource = state;
        field.DataBindings.Add(new Binding(nameof(field.Value), binding, nameof(ChildState.Name), true, DataSourceUpdateMode.OnPropertyChanged));
        var context = source.BindingContext; host.Merge(source); host.SelectedTab = tools; form.Show(); Application.DoEvents();
        var editor = host.Editors.EditorFor(field)!; var strip = host.GalleryStrips.SurfaceFor(gallery)!; nint editorHandle = editor.Handle, stripHandle = strip.Handle;
        editor.Select(2, 3); var menuReference = new RibbonQuickAccessDropDownReference { Source = gallery }; var commandReference = new RibbonQuickAccessReference { Source = (RibbonButtonItem)group.Items[0] };
        host.QuickAccessItems.AddRange(menuReference, commandReference); Assert.True(RibbonQuickAccess.Available(menuReference));
        host.Activate(host.Snapshot.Targets.Single(t => t.Element == menuReference)); Assert.True(menuReference.IsDropDownOpen);
        gallery.Preview((RibbonGalleryTile)gallery.Items[0]); host.Unmerge(source);
        Assert.False(menuReference.IsDropDownOpen); Assert.Null(gallery.PreviewItem); Assert.False(RibbonQuickAccess.Available(menuReference)); Assert.False(RibbonQuickAccess.Available(commandReference));
        Assert.False(editor.IsDisposed); Assert.False(strip.IsDisposed); Assert.False(strip.Visible); Assert.False(source.IsDisposed); Assert.Equal(2, host.QuickAccessItems.Count);
        Assert.Same(source, RibbonControl.GetMergeSource(menuReference)); Assert.Same(home, host.SelectedTab);
        host.BindingContext = new BindingContext(); Assert.Same(context, source.BindingContext); Assert.Same(context, field.BindingContext);
        host.Merge(source); host.SelectedTab = tools; Application.DoEvents();
        Assert.Same(editor, host.Editors.EditorFor(field)); Assert.Same(strip, host.GalleryStrips.SurfaceFor(gallery)); Assert.Equal(editorHandle, editor.Handle); Assert.Equal(stripHandle, strip.Handle); Assert.Equal(2, editor.SelectionStart); Assert.Equal(3, editor.SelectionLength);
        Assert.True(RibbonQuickAccess.Available(menuReference)); state.Name = "Updated child"; Application.DoEvents(); Assert.Equal("Updated child", field.Value); Assert.Equal("Updated child", editor.Text);
        source.Dispose(); Assert.True(editor.IsDisposed); Assert.True(strip.IsDisposed); Assert.False(RibbonQuickAccess.Available(menuReference)); Assert.Same(home, host.SelectedTab);
        Assert.Same(source, RibbonControl.GetMergeSource(menuReference)); Assert.Same(source, RibbonControl.GetMergeSource(commandReference));
        var authored = new RibbonButtonItem(); var authoredGroup = new RibbonGroup(); authoredGroup.Items.Add(authored); home.Groups.Add(authoredGroup); commandReference.Source = authored;
        Assert.Null(RibbonControl.GetMergeSource(commandReference));
    });

    [Fact]
    public void Host_disposal_borrows_models_and_retargeting_releases_the_old_hosts_native_cache() => FoundationTests.Sta(() =>
    {
        using var firstForm = new Form { Width = 1000 }; var first = new RibbonControl(); firstForm.Controls.Add(first); first.Tabs.Add(new RibbonTab { Id = "home" });
        using var second = new RibbonControl(); second.Tabs.Add(new RibbonTab { Id = "home" });
        using var source = Source("tools", "home", 0); var field = new RibbonTextBoxItem { Value = "Retained model" }; source.Tabs[0].Groups[0].Items.Add(field);
        first.Merge(source); first.SelectedTab = source.Tabs[0]; firstForm.Show(); Application.DoEvents(); var editor = first.Editors.EditorFor(field)!;
        first.Unmerge(source); Assert.False(editor.IsDisposed); second.Merge(source); Assert.True(editor.IsDisposed); Assert.Same(second, source.MergedInto);
        second.Dispose(); Assert.False(source.IsMerged); Assert.False(source.IsDisposed); Assert.False(field.IsDisposed); Assert.Equal("Retained model", field.Value);
        first.Merge(source); Assert.Same(first, source.MergedInto);
        source.IsMergedChanged += (_, _) => { if (source.IsMerged) first.Unmerge(source); };
        first.Unmerge(source); first.Merge(source); Assert.False(source.IsMerged); Assert.DoesNotContain(source.Tabs[0], first.GetPresentedTabs());
    });

    private static RibbonMergeSource Source(string name, string target, int order)
    {
        var source = new RibbonMergeSource { Text = name, Order = order }; var tab = new RibbonTab { Text = name, Id = name + ".tab" }; var group = new RibbonGroup { Text = name + " tools" };
        group.Items.Add(new RibbonButtonItem { Text = "Apply " + name }); tab.Groups.Add(group); source.Tabs.Add(tab);
        var contribution = new RibbonGroupContribution { TargetTabId = target, Order = order }; contribution.Group.Text = name + " contribution"; contribution.Group.Items.Add(new RibbonButtonItem { Text = "Contributed action" }); source.Groups.Add(contribution);
        return source;
    }
    [Fact]
    public void Preview_cancellation_can_dispose_the_departing_source_without_recursive_unmerge() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl(); var home = new RibbonTab { Id = "home" }; host.Tabs.Add(home);
        using var source = Source("tools", "home", 0); var gallery = new RibbonGalleryItem(); var tile = new RibbonGalleryTile(); gallery.Items.Add(tile); source.Tabs[0].Groups[0].Items.Add(gallery);
        host.Merge(source); host.SelectedTab = source.Tabs[0]; int cancels = 0;
        gallery.ItemPreviewCancelled += (_, _) => { cancels++; Assert.False(source.IsMerged); Assert.False(host.Unmerge(source)); source.Dispose(); };
        gallery.Preview(tile); Assert.True(host.Unmerge(source)); Assert.Equal(1, cancels); Assert.True(source.IsDisposed); Assert.Same(home, host.SelectedTab);
    });
    private sealed class ChildState : INotifyPropertyChanged
    {
        private string _name = "Child document";
        public string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
