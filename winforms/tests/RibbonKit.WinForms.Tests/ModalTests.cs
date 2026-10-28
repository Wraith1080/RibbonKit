using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ModalTests
{
    [Fact]
    public void Exclusive_presentation_preserves_authored_visibility_qat_and_blocked_values() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl { Width = 1100 }; var (home, other, preview) = Attach(host);
        host.FileMenu.Visible = true; host.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
        var action = (RibbonButtonItem)home.Groups[0].Items[0]; var reference = new RibbonQuickAccessReference { Source = action }; var fileReference = new RibbonQuickAccessDropDownReference { Source = host.FileMenu }; host.QuickAccessItems.AddRange(reference, fileReference);
        int actions = 0; action.Click += (_, _) => actions++; var events = new List<string>();
        host.ModalEntering += (_, e) => { Assert.Same(preview, e.Tab); Assert.False(e.IsForced); events.Add("entering"); Assert.False(host.IsModal); };
        host.ModalEntered += (_, _) => events.Add("entered"); host.ModalExiting += (_, _) => events.Add("exiting"); host.ModalExited += (_, _) => events.Add("exited");
        host.IsMinimized = true; Assert.True(host.EnterModal(preview)); Assert.True(host.IsModal); Assert.Same(preview, host.ModalTab); Assert.Same(preview, host.SelectedTab);
        Assert.Equal(new[] { preview }, host.GetPresentedTabs()); Assert.Equal(3, host.Tabs.Count); Assert.True(home.Visible && other.Visible && preview.Visible && host.FileMenu.Visible);
        Assert.DoesNotContain(host.Snapshot.Targets, t => t.Kind is TargetKind.FileMenu or TargetKind.Minimize); Assert.Single(host.Snapshot.Targets, t => t.Kind == TargetKind.ModalClose);
        Assert.True(RibbonQuickAccess.Available(reference)); Assert.False(RibbonQuickAccess.Available(fileReference));
        host.Activate(host.Snapshot.Targets.Single(t => t.Element == reference)); Assert.Equal(1, actions);
        host.IsMinimized = true; Assert.False(host.IsMinimized); Assert.Throws<ArgumentException>(() => host.SelectedTab = home);
        host.FileMenu.ShowDropDown(); Assert.False(host.FileMenu.IsDropDownOpen);
        host.IsBackstageOpen = true; Assert.False(host.IsBackstageOpen);
        Assert.True(host.ExitModal()); Assert.False(host.IsModal); Assert.Null(host.ModalTab); Assert.Same(home, host.SelectedTab); Assert.True(host.IsMinimized); Assert.True(host.HasFileMenu);
        Assert.Equal(new[] { "entering", "entered", "exiting", "exited" }, events); Assert.True(host.ExitModal());
        Assert.Equal(new[] { home, other, preview }, host.GetPresentedTabs());
    });

    [Fact]
    public void Cancellation_close_policy_caption_updates_and_reentrant_entry_are_bounded() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl { Width = 900 }; var (home, _, preview) = Attach(host); bool cancelEntry = true, cancelExit = true; int entered = 0, exited = 0;
        host.ModalEntering += (_, e) => { Assert.False(host.EnterModal(preview)); e.Cancel = cancelEntry; };
        host.ModalExiting += (_, e) => e.Cancel = cancelExit; host.ModalEntered += (_, _) => entered++; host.ModalExited += (_, _) => exited++;
        Assert.False(host.EnterModal(preview)); Assert.False(host.IsModal); Assert.Same(home, host.SelectedTab);
        cancelEntry = false; Assert.True(host.EnterModal(preview)); Assert.True(host.EnterModal(preview)); Assert.Equal(1, entered);
        Assert.False(host.ExitModal()); Assert.True(host.IsModal); Assert.Equal(0, exited);
        var second = new RibbonTab { Text = "Second mode", IsModal = true }; host.Tabs.Add(second); Assert.Throws<InvalidOperationException>(() => host.EnterModal(second));
        cancelExit = false; preview.CanClose = false; Assert.DoesNotContain(host.Snapshot.Targets, t => t.Kind == TargetKind.ModalClose); Assert.False(host.ExitModalByUser(RibbonModalReason.Escape));
        Assert.True(host.ExitModal()); Assert.Equal(1, exited);
        preview.CanClose = true; Assert.True(host.EnterModal(preview)); var original = host.Snapshot.Targets.Single(t => t.Kind == TargetKind.ModalClose);
        preview.CloseButtonText = "Close Print Preview"; var changed = host.Snapshot.Targets.Single(t => t.Kind == TargetKind.ModalClose); Assert.True(changed.Bounds.Width > original.Bounds.Width); Assert.Equal(preview.CloseButtonText, changed.Element!.Text);
        RibbonModalReason? reason = null; host.ModalExited += (_, e) => reason = e.Reason; host.Activate(changed); Assert.False(host.IsModal); Assert.Equal(RibbonModalReason.CloseButton, reason);
    });

    [Fact]
    public void Merges_and_authored_edits_during_modal_survive_forced_exit_and_ignore_veto() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl(); var (home, other, ownPreview) = Attach(host); ownPreview.Visible = false;
        using var source = new RibbonMergeSource(); var modal = new RibbonTab { IsModal = true, Text = "Merged preview" }; source.Tabs.Add(modal); host.Merge(source);
        Assert.True(host.EnterModal(modal));
        using var additional = new RibbonMergeSource(); var extra = new RibbonTab { Text = "Added while modal" }; additional.Tabs.Add(extra); host.Merge(additional);
        home.Visible = false; host.FileMenu.Visible = true; host.FileMenu.Visible = false; Assert.Equal(new[] { modal }, host.GetPresentedTabs()); Assert.True(extra.Visible); Assert.False(home.Visible);
        bool forced = false; host.ModalExiting += (_, e) => { e.Cancel = true; forced = e.IsForced; Assert.Equal(RibbonModalReason.TabUnavailable, e.Reason); };
        host.Unmerge(source); Assert.True(forced); Assert.False(host.IsModal); Assert.Same(other, host.SelectedTab); Assert.Contains(extra, host.GetPresentedTabs()); Assert.False(home.Visible); Assert.False(host.FileMenu.Visible);
        Assert.False(ownPreview.Visible); Assert.True(additional.IsMerged);
        home.Visible = true; host.SelectedTab = home; home.Enabled = false; Assert.Same(other, host.SelectedTab);
    });

    [Fact]
    public void Every_active_tab_loss_exits_coherently_and_cancelled_entry_mutation_is_safe() => FoundationTests.Sta(() =>
    {
        foreach (var loss in new Action<RibbonControl, RibbonTab>[] { (_, t) => t.Visible = false, (_, t) => t.Enabled = false, (_, t) => t.IsModal = false, (h, t) => h.Tabs.Remove(t), (h, _) => h.Tabs.Clear(), (_, t) => t.Dispose() })
        {
            using var host = new RibbonControl(); var (home, _, preview) = Attach(host); Assert.True(host.EnterModal(preview)); int exits = 0;
            host.ModalExiting += (_, e) => { Assert.True(e.IsForced); e.Cancel = true; }; host.ModalExited += (_, _) => exits++;
            loss(host, preview); Assert.False(host.IsModal); Assert.Equal(1, exits); Assert.Null(host.ModalTab); Assert.True(host.SelectedTab is null || host.SelectedTab == home);
        }
        using var changed = new RibbonControl(); var (_, _, candidate) = Attach(changed); changed.ModalEntering += (_, _) => changed.Tabs.Remove(candidate);
        Assert.False(changed.EnterModal(candidate)); Assert.False(changed.IsModal);
    });

    [Fact]
    public void Editor_validation_and_prior_native_focus_are_preserved_and_forced_exit_bypasses_validation() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1100, Height = 550 }; var host = new RibbonControl(); var document = new TextBox { Dock = DockStyle.Fill }; form.Controls.Add(document); form.Controls.Add(host);
        var (home, _, preview) = Attach(host); var normal = new RibbonTextBoxItem { Text = "Name", Value = "Valid" }; home.Groups[0].Items.Add(normal); var field = new RibbonTextBoxItem { Text = "Copies", Value = "1" }; preview.Groups[0].Items.Add(field);
        normal.ValueValidating += (_, e) => e.Cancel = normal.Value == ""; field.ValueValidating += (_, e) => e.Cancel = field.Value == "";
        form.Show(); Application.DoEvents(); normal.PerformClick(); var normalEditor = host.Editors.EditorFor(normal)!; normalEditor.Text = "";
        Assert.False(host.EnterModal(preview)); Assert.True(normalEditor.Focused); Assert.False(host.IsModal); normalEditor.Text = "Valid"; host.Editors.CommitFocused(); document.Focus();
        Assert.True(host.EnterModal(preview)); field.PerformClick(); var editor = host.Editors.EditorFor(field)!; nint handle = editor.Handle; editor.Text = "";
        Assert.False(host.ExitModal()); Assert.True(host.IsModal); Assert.True(editor.Focused); editor.Text = "2"; Assert.True(host.ExitModal()); Assert.True(document.Focused); Assert.False(editor.IsDisposed);
        Assert.True(host.EnterModal(preview)); field.PerformClick(); Assert.Equal(handle, editor.Handle); editor.Text = ""; preview.Visible = false;
        Assert.False(host.IsModal); Assert.Same(home, host.SelectedTab); Assert.True(document.Focused);
    });

    [Fact]
    public void Close_geometry_remains_disjoint_and_paints_with_shared_state_at_narrow_widths_and_large_fonts() => FoundationTests.Sta(() =>
    {
        using var host = new RibbonControl(); var (_, _, preview) = Attach(host); preview.CloseButtonText = "Close Print Preview"; host.FileMenu.Visible = true;
        foreach (int points in new[] { 9, 18 })
        {
            using var font = new Font("Segoe UI", points); host.Font = font; host.EnterModal(preview);
            foreach (int width in new[] { 24, 40, 120, 320, 900 })
            {
                host.Width = width; var close = host.Snapshot.Targets.Single(t => t.Kind == TargetKind.ModalClose);
                Assert.True(host.ClientRectangle.Contains(close.VisibleBounds) || close.VisibleBounds.IsEmpty);
                Assert.All(host.Snapshot.Targets.Where(t => t.Kind is TargetKind.Tab or TargetKind.QuickAccess or TargetKind.FileMenu), t => Assert.False(t.VisibleBounds.IntersectsWith(close.VisibleBounds)));
                using var bitmap = new Bitmap(Math.Max(1, width), host.Height); using var graphics = Graphics.FromImage(bitmap); graphics.SetClip(host.ClientRectangle);
                using var renderer = new RibbonOffice2024Renderer(); renderer.DrawModalClose(new(graphics, close.Bounds, host.Theme, host.Font, host.DeviceDpi, true, true, true), close.Element!.Text);
            }
            host.ExitModal();
        }
    });
    private static (RibbonTab Home, RibbonTab Other, RibbonTab Preview) Attach(RibbonControl host)
    {
        var home = new RibbonTab { Id = "home", Text = "Home" }; var other = new RibbonTab { Text = "Other" }; var preview = new RibbonTab { Text = "Print Preview", IsModal = true };
        foreach (var tab in new[] { home, other, preview }) { var group = new RibbonGroup { Text = "Commands" }; group.Items.Add(new RibbonButtonItem { Text = "Action" }); tab.Groups.Add(group); }
        host.Tabs.AddRange(home, other, preview); return (home, other, preview);
    }
    [Fact]
    public void Existing_backstage_closing_veto_blocks_entry_without_forcing_teardown() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000, Height = 550 }; var host = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage }; form.Controls.Add(host); var (_, _, preview) = Attach(host);
        using var page = new Panel(); host.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page }); host.FileMenu.Visible = true;
        form.Show(); Application.DoEvents(); host.IsBackstageOpen = true; Assert.True(host.IsBackstageOpen);
        bool cancel = true; host.Backstage.Closing += (_, e) => e.Cancel = cancel;
        Assert.False(host.EnterModal(preview)); Assert.False(host.IsModal); Assert.True(host.IsBackstageOpen);
        cancel = false; Assert.True(host.EnterModal(preview)); Assert.False(host.IsBackstageOpen); Assert.True(host.IsModal);
        host.ExitModal(); Assert.False(host.IsBackstageOpen); Assert.False(page.IsDisposed);
    });
}
