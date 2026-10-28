using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CustomizationEditorTests
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd, uint msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    private static T Child<T>(Control parent, string name) where T : Control => (T)parent.Controls.Find(name, true).Single();
    private static RibbonButtonItem MenuButton(RibbonDropDownItem menu, string id) => menu.Items.OfType<RibbonButtonItem>().Single(i => i.Id == id);

    [Fact]
    public void Page_buttons_stage_order_and_membership_and_apply_retains_latest_checkpoint() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out var group, out var copy, out var undo);
        int applied = 0; ribbon.CustomizationApplied += (_, _) => applied++;
        using var page = new RibbonQuickAccessCustomizationEditor(ribbon, "test.app") { Size = new(900, 600) };
        var search = Child<TextBox>(page, "CommandSearch"); var available = Child<ListBox>(page, "AvailableCommands");
        var current = Child<ListBox>(page, "ToolbarCommands");
        Assert.Contains(group.Text, available.GetItemText(available.Items[0])); Assert.Contains(copy.Text, available.GetItemText(available.Items[1]));
        search.Text = "Copy"; Assert.Single(available.Items.Cast<object>());
        Child<Button>(page, "AddCommand").PerformClick();
        Assert.Single(ribbon.QuickAccessItems); Assert.Equal(2, page.Session.Draft.QuickAccess.Count);
        Child<Button>(page, "MoveCommandUp").PerformClick(); Assert.Equal(copy.Id, page.Session.Draft.QuickAccess[0].CommandId);
        Assert.Empty(page.Controls.Find("ToolbarPosition", true)); Assert.Empty(page.Controls.Find("ToolbarVisible", true));
        Assert.True(page.ApplyChanges()); Assert.Equal(1, applied); Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.QuickAccessPosition); Assert.True(ribbon.QuickAccessVisible);
        current.SelectedIndex = 1; Child<Button>(page, "RemoveCommand").PerformClick(); Assert.Single(page.Session.Draft.QuickAccess);
        page.Session.Cancel(); page.RefreshDraft(); Assert.Equal(2, current.Items.Count);
        page.Session.Reset(); page.RefreshDraft(); Assert.Single(current.Items.Cast<object>()); Assert.Equal(2, ribbon.QuickAccessItems.Count);
        page.Session.Reload(); page.RefreshDraft(); Assert.Equal(2, current.Items.Count);
        search.Text = group.Text; available.SelectedIndex = 0; Assert.True(Child<Button>(page, "AddCommand").Enabled);
        Child<Button>(page, "AddCommand").PerformClick(); Assert.Equal(group.Id, page.Session.Draft.QuickAccess[^1].CommandId);
        Assert.Contains(ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess), e => e.Source == undo);
    });

    [Fact]
    public void Page_reports_stale_apply_but_host_persistence_exceptions_propagate() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out _, out _, out _);
        using var page = new RibbonQuickAccessCustomizationEditor(ribbon, "test.app");
        ribbon.QuickAccessVisible = false; Assert.False(page.ApplyChanges());
        Assert.Contains("Reload", Child<Label>(page, "CustomizationStatus").Text);
        page.Session.Reload(); page.RefreshDraft();
        ribbon.CustomizationApplied += (_, _) => throw new InvalidOperationException("Host storage failure");
        Assert.Equal("Host storage failure", Assert.Throws<InvalidOperationException>(() => page.ApplyChanges()).Message);
    });

    [Fact]
    public void Reset_in_the_editor_restores_commands_without_changing_menu_only_preferences() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out _, out var copy, out var undo);
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; ribbon.QuickAccessVisible = false;
        using var page = new RibbonQuickAccessCustomizationEditor(ribbon, "test.app");
        page.Session.Draft.QuickAccess.Clear(); page.Session.Draft.QuickAccess.Add(new() { CommandId = copy.Id });
        page.RefreshDraft(); Child<Button>(page, "ResetCustomization").PerformClick();
        Assert.Equal(undo.Id, Assert.Single(page.Session.Draft.QuickAccess).CommandId);
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, page.Session.Draft.QuickAccessPosition); Assert.False(page.Session.Draft.QuickAccessVisible);
        Assert.True(page.ApplyChanges()); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition); Assert.False(ribbon.QuickAccessVisible);
    });

    [Fact]
    public void Context_commands_share_sessions_and_disabled_sources_remain_removable() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out var group, out var copy, out var undo);
        ribbon.CustomizationApplicationId = "test.app"; copy.Enabled = false;
        using (var menu = ribbon.CreateCustomizationMenu(copy)) MenuButton(menu, "AddToQuickAccess").PerformClick();
        Assert.Equal(2, ribbon.QuickAccessItems.Count);
        var presentation = ribbon.QuickAccessItems[1];
        using (var menu = ribbon.CreateCustomizationMenu(presentation)) MenuButton(menu, "RemoveFromQuickAccess").PerformClick();
        Assert.Single(ribbon.QuickAccessItems); Assert.False(copy.IsDisposed);
        using (var menu = ribbon.CreateCustomizationMenu(group)) MenuButton(menu, "AddToQuickAccess").PerformClick();
        Assert.IsType<RibbonQuickAccessGroupReference>(ribbon.QuickAccessItems[1]);
        using (var menu = ribbon.CreateCustomizationMenu(undo)) MenuButton(menu, "RemoveFromQuickAccess").PerformClick();
        Assert.Same(ribbon.CommandCatalog, undo.Parent);
        using var again = ribbon.CreateCustomizationMenu(undo); Assert.True(MenuButton(again, "AddToQuickAccess").Enabled);
        MenuButton(again, "MoveQuickAccess").PerformClick(); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition);
        MenuButton(again, "ToggleQuickAccess").PerformClick(); Assert.False(ribbon.QuickAccessVisible);
    });

    [Fact]
    public void Context_message_and_keyboard_honor_opt_out_and_application_menu() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out _, out var copy, out _); using var form = new Form { Width = 1000, Height = 500 };
        form.Controls.Add(ribbon); form.Show(); Application.DoEvents(); ribbon.FocusElement(copy);
        Assert.True(ribbon.ProcessCustomizationKey(Keys.Shift | Keys.F10)); Assert.True(ribbon.HasCustomizationMenu); ribbon.CloseCustomizationMenu(); Application.DoEvents();
        using var applicationMenu = new ContextMenuStrip(); applicationMenu.Items.Add("Host command"); ribbon.ContextMenuStrip = applicationMenu;
        Assert.False(ribbon.ShowCustomizationMenu(ribbon, ribbon.PointToScreen(Point.Empty), copy));
        ribbon.ContextMenuStrip = null; ribbon.QuickAccessCustomizationEnabled = false;
        Assert.False(ribbon.ShowCustomizationMenu(ribbon, ribbon.PointToScreen(Point.Empty), copy));
        ribbon.QuickAccessCustomizationEnabled = true;
        var target = ribbon.Snapshot.Targets.First(t => ReferenceEquals(t.Element, copy)); var screen = ribbon.PointToScreen(target.VisibleBounds.Location);
        SendMessage(ribbon.Handle, 0x7B, ribbon.Handle, (nint)((screen.Y << 16) | (screen.X & 0xffff)));
        Application.DoEvents(); Assert.True(ribbon.HasCustomizationMenu);
        ribbon.CloseCustomizationMenu(); Application.DoEvents();
        var field = new RibbonTextBoxItem { Id = "cmd.field", Text = "Name" }; ribbon.Tabs[0].Groups[0].Items.Add(field); field.PerformClick();
        Assert.True(ribbon.Editors.ContainsFocus); Assert.False(ribbon.ProcessCustomizationKey(Keys.Shift | Keys.F10));
    });

    [Theory]
    [InlineData(96, 15)] [InlineData(120, 19)] [InlineData(144, 23)] [InlineData(192, 30)] [InlineData(192, 58)]
    public void Editor_metrics_fit_compact_rows_and_touch_targets_at_dpi_and_large_fonts(int dpi, int fontHeight)
    {
        int compact = RibbonQuickAccessEditorMetrics.RowHeight(dpi, fontHeight, RibbonDensity.Compact);
        int touch = RibbonQuickAccessEditorMetrics.RowHeight(dpi, fontHeight, RibbonDensity.Touch);
        Assert.True(compact >= fontHeight + Math.Ceiling(8 * dpi / 96d)); Assert.True(touch >= compact);
        Assert.True(RibbonQuickAccessEditorMetrics.ButtonHeight(dpi, fontHeight, RibbonDensity.Touch) >= 44 * dpi / 96d);
        Assert.True(RibbonQuickAccessEditorMetrics.Minimum(dpi, fontHeight, RibbonDensity.Touch).Height > touch * 2);
        var work = new Rectangle(-1024, 40, 1024, 728);
        var window = RibbonQuickAccessEditorMetrics.ClampWindow(new(-1400, -100, 1700, 1200), work, dpi);
        Assert.True(work.Contains(window)); Assert.True(window.Width > 0 && window.Height > 0);
    }

    [Fact]
    public void Contexts_in_group_minimized_body_and_overflow_keep_source_identity_and_preserve_flyout() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out var group, out var copy, out var undo); using var form = new Form { Width = 1000, Height = 500 };
        var groupEntry = new RibbonQuickAccessGroupReference { Id = "qat.group", Source = group }; ribbon.QuickAccessItems.Add(groupEntry);
        form.Controls.Add(ribbon); form.Show(); form.Activate(); SetForegroundWindow(form.Handle); Application.DoEvents();
        ribbon.Activate(ribbon.Snapshot.Targets.First(t => ReferenceEquals(t.Element, groupEntry)));
        Context(ribbon.Popups.Top!, copy, "AddToQuickAccess");
        ribbon.QuickAccessItems.Remove(groupEntry);
        ribbon.IsMinimized = true; Application.DoEvents();
        form.Activate(); SetForegroundWindow(form.Handle);
        ribbon.Activate(ribbon.Snapshot.Targets.First(t => t.Kind == TargetKind.Tab));
        Context(ribbon.Popups.Top!, copy, "AddToQuickAccess");
        ribbon.IsMinimized = false; ribbon.QuickAccessItems.Add(groupEntry); ribbon.QuickAccessMaxWidth = RibbonQuickAccess.ButtonWidth; Application.DoEvents();
        form.Activate(); SetForegroundWindow(form.Handle);
        ribbon.Activate(ribbon.Snapshot.Targets.First(t => t.Kind == TargetKind.QuickAccessOverflow));
        Context(ribbon.Popups.Top!, groupEntry, "RemoveFromQuickAccess");

        void Context(RibbonPopupSurface popup, RibbonElement element, string command)
        {
            Assert.NotNull(popup); Assert.True(popup.Targets.Any(t => ReferenceEquals(t.Element, element)), string.Join(", ", popup.Targets.Select(t => t.Kind + ":" + t.Element?.Id)));
            var target = popup.Targets.First(t => ReferenceEquals(t.Element, element)); var screen = popup.PointToScreen(target.VisibleBounds.Location);
            SendMessage(popup.Handle, 0x7B, popup.Handle, (nint)((screen.Y << 16) | (screen.X & 0xffff)));
            Assert.True(ribbon.HasCustomizationMenu); Assert.Equal(2, ribbon.Popups.Surfaces.Count()); Assert.False(popup.IsDisposed);
            Assert.NotNull(MenuButton(ribbon.CustomizationMenu!, command)); ribbon.CloseCustomizationMenu(); Application.DoEvents();
            Assert.Same(popup, Assert.Single(ribbon.Popups.Surfaces)); Assert.False(popup.IsDisposed); ribbon.Popups.CloseAll(false);
        }
    });

    [Fact]
    public void Alt_f4_from_context_menu_closes_host_and_respects_host_cancellation() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out _, out var copy, out _); using var form = new Form { Width = 1000, Height = 500 };
        form.Controls.Add(ribbon); form.Show(); form.Activate(); SetForegroundWindow(form.Handle); Application.DoEvents();
        int closing = 0; form.FormClosing += (_, e) => { closing++; e.Cancel = true; };
        ribbon.ShowCustomizationMenu(ribbon, ribbon.PointToScreen(Point.Empty), copy);
        using var filter = new RibbonInputFilter(ribbon);
        Assert.True(ribbon.HasCustomizationMenu);
        var message = Message.Create(form.Handle, 0x104, (nint)Keys.F4, (nint)(1L << 29));
        Assert.True(filter.PreFilterMessage(ref message)); Assert.Equal(1, closing); Assert.False(form.IsDisposed); Assert.False(ribbon.HasCustomizationMenu);
    });

    [Fact]
    public void Options_pages_apply_independently_validate_before_ok_and_keep_applied_checkpoint_on_cancel() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out _, out var copy, out _); using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "test.app");
        var appPage = new RibbonOptionsPage { Text = "Editor" }; var check = new CheckBox { Text = "Wrap text", Checked = true };
        appPage.Controls.Add(check); dialog.Pages.Insert(0, appPage);
        bool checkpoint = false; int appApplied = 0, cancelled = 0; bool valid = false;
        appPage.Applying += (_, e) => e.Cancel = !valid;
        appPage.Applied += (_, _) => { checkpoint = check.Checked; appApplied++; };
        appPage.Cancelled += (_, _) => { check.Checked = checkpoint; cancelled++; };
        dialog.Editor.Session.Draft.QuickAccess.Add(new() { CommandId = copy.Id });
        dialog.Show(); Application.DoEvents(); dialog.SelectedPage = appPage;
        Child<Button>(dialog, "ApplyCustomization").PerformClick(); Assert.Equal(0, appApplied); Assert.Single(ribbon.QuickAccessItems);
        valid = true; Child<Button>(dialog, "ApplyCustomization").PerformClick(); Assert.Equal(1, appApplied); Assert.True(checkpoint); Assert.Single(ribbon.QuickAccessItems);
        check.Checked = false; valid = false;
        Child<Button>(dialog, "AcceptCustomization").PerformClick(); Assert.True(dialog.Visible); Assert.Single(ribbon.QuickAccessItems); Assert.Same(appPage, dialog.SelectedPage);
        dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "QuickAccessOptionsPage"); Child<Button>(dialog, "ApplyCustomization").PerformClick(); Assert.Equal(2, ribbon.QuickAccessItems.Count); Assert.Equal(1, appApplied);
        Child<Button>(dialog, "CancelCustomization").PerformClick(); Assert.Equal(1, cancelled); Assert.True(check.Checked); Assert.Equal(2, ribbon.QuickAccessItems.Count);
    });

    [Fact]
    public void Options_page_ownership_and_selection_follow_collection_without_reparenting_host_controls() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out _, out _, out _); using var dialog = new RibbonOptionsDialog(ribbon);
        using var other = new RibbonOptionsDialog(ribbon); var page = new RibbonOptionsPage { Text = "Application" }; var input = new TextBox(); page.Controls.Add(input);
        dialog.Pages.Add(page); Assert.Same(page, dialog.SelectedPage); Assert.Same(page, input.Parent);
        Assert.Throws<InvalidOperationException>(() => other.Pages.Add(page)); dialog.Pages.Remove(page); Assert.False(page.IsDisposed); Assert.Null(page.Parent);
        other.Pages.Add(page); Assert.Same(page, other.SelectedPage); other.Dispose(); Assert.True(page.IsDisposed); Assert.True(input.IsDisposed);
    });

    [Fact]
    public void Cancel_after_a_host_vetoed_ok_discards_pending_page_edits_without_losing_applied_settings() => FoundationTests.Sta(() =>
    {
        using var ribbon = Ribbon(out _, out _, out _); using var dialog = new RibbonOptionsDialog(ribbon);
        var page = new RibbonOptionsPage { Text = "Application" }; dialog.Pages.Add(page);
        int applied = 0, cancelled = 0; page.Applied += (_, _) => applied++; page.Cancelled += (_, _) => cancelled++;
        bool veto = true; dialog.FormClosing += (_, e) => e.Cancel = veto;
        dialog.Show(); Application.DoEvents();
        Child<Button>(dialog, "AcceptCustomization").PerformClick(); Assert.True(dialog.Visible); Assert.Equal(1, applied);
        veto = false; Assert.False(Child<Button>(dialog, "CancelCustomization").CausesValidation);
        Child<Button>(dialog, "CancelCustomization").PerformClick(); Assert.Equal(1, cancelled); Assert.Equal(1, applied);
    });

    private static RibbonControl Ribbon(out RibbonGroup group, out RibbonButtonItem copy, out RibbonButtonItem undo)
    {
        var ribbon = new RibbonControl { Width = 1000, BindingContext = new BindingContext() };
        var tab = new RibbonTab { Id = "tab.home", Text = "Home" }; group = new() { Id = "group.clipboard", Text = "Clipboard" };
        copy = new() { Id = "cmd.copy", Text = "Copy" }; group.Items.Add(copy); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        undo = new() { Id = "cmd.undo", Text = "Undo" }; ribbon.QuickAccessItems.Add(undo); ribbon.CommandCatalog.CaptureDefaults(); return ribbon;
    }
}
