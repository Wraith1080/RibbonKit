namespace RibbonKit.WinForms;

public sealed partial class RibbonSimplifiedCustomizationEditor
{
    private void OpenActions(Button anchor, string kind)
    {
        _menu?.Dispose(); _menu = new(_ribbon) { Font = Font };
        var tab = Tab; var group = Group; var row = _current.SelectedItem as Row;
        switch (kind)
        {
            case "New":
                _menu.Action("New tab", () =>
                {
                    var created = new RibbonCustomizationTab { Label = "New Tab", Groups = [new() { Label = "New Group" }] };
                    Structure.Tabs.Insert(tab is null ? Structure.Tabs.Count : Structure.Tabs.IndexOf(tab) + 1, created); RefreshDraft(); SelectTab(_targetTab, created.Id); RefreshCurrent();
                });
                _menu.Action("New group", () => { var created = new RibbonCustomizationGroup { Label = "New Group" }; using var dialog = new RibbonCustomizationGroupDialog(_ribbon, this, created, created.Label, true); if (dialog.ShowDialog(FindForm()) == DialogResult.OK) { tab!.Groups.Add(created); RefreshCurrent("header:" + created.Id); } }, tab is not null);
                break;
            case "Edit":
                _menu.Action("Rename tab…", () => { if (Prompt("Rename tab", DisplayName(tab!.Id, tab.Label)) is { } name) { tab.Label = name; RefreshDraft(); } }, tab is not null);
                _menu.Action(group?.IsCustom == true ? "Edit group…" : "Rename group…", () => { if (group!.IsCustom) { using var dialog = new RibbonCustomizationGroupDialog(_ribbon, this, group, DisplayName(group.Id, group.Label), false); if (dialog.ShowDialog(FindForm()) == DialogResult.OK) RefreshCurrent(); } else if (Prompt("Rename group", DisplayName(group.Id, group.Label)) is { } name) { group.Label = name; RefreshCurrent(); } }, group is not null);
                var customCommand = group?.IsCustom == true && row is { Header: false } ? group.Commands.FirstOrDefault(c => c.CommandId == row.Id) : null;
                _menu.Action("Rename command…", () => { if (Prompt("Rename command", row!.Caption) is { } name) { customCommand!.Label = name; RefreshCurrent(row.Id); } }, customCommand is not null);
                _menu.Separator();
                int at = tab is null ? -1 : Structure.Tabs.IndexOf(tab);
                _menu.Action("Move tab up", () => { (Structure.Tabs[at - 1], Structure.Tabs[at]) = (Structure.Tabs[at], Structure.Tabs[at - 1]); RefreshDraft(); }, at > 0);
                _menu.Action("Move tab down", () => { (Structure.Tabs[at + 1], Structure.Tabs[at]) = (Structure.Tabs[at], Structure.Tabs[at + 1]); RefreshDraft(); }, at >= 0 && at < Structure.Tabs.Count - 1);
                _menu.Separator();
                _menu.Action("Show label", ShowLabel, row is { Header: false }, Choices().FirstOrDefault(c => c.CommandId == row?.Id)?.Size == RibbonItemSize.Medium);
                _menu.Action("Remove custom group", RemoveGroup, group?.IsCustom == true);
                _menu.Action("Remove custom tab", () => { Structure.Tabs.Remove(tab!); Session.Draft.Simplified!.Tabs.RemoveAll(t => t.TabId == tab!.Id); RefreshDraft(); }, tab?.IsCustom == true && Structure.Tabs.Any(t => t.Visible && !ReferenceEquals(t, tab)));
                break;
            case "Reset":
                _menu.Action("Reset this tab's Simplified choices", () => { Session.Draft.Simplified!.Tabs.RemoveAll(t => t.TabId == tab!.Id); RefreshCurrent(); }, tab is not null);
                _menu.Action("Reset all Simplified choices", () => { Session.Draft.Simplified = new(); RefreshCurrent(); });
                _menu.Action("Reset Ribbon structure and Simplified choices", () => { Session.Reset(); RefreshDraft(); });
                _menu.Separator(); _menu.Action("Reload current settings", () => { Session.Reload(); RefreshDraft(); });
                break;
        }
        _menu.Show(anchor, new Point(0, anchor.Height));
    }
    private void RemoveGroup()
    {
        if (Tab is not { } tab || Group is not { IsCustom: true } group) return;
        var ids = Commands(tab).Where(c => c.GroupId == group.Id).Select(c => c.Id).ToHashSet(); Choices(true).RemoveAll(c => ids.Contains(c.CommandId)); tab.Groups.Remove(group); RefreshCurrent();
    }
    private string? Prompt(string title, string current)
    {
        int gap = S(8), height = RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density);
        using var dialog = new Form { Text = title, Font = Font, AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new(DeviceDpi, DeviceDpi), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, BackColor = _ribbon.Theme.Surface, ForeColor = _ribbon.Theme.Foreground, ClientSize = new(Math.Max(S(320), Font.Height * 22), Font.Height * 2 + height * 2 + gap * 4) };
        var text = new TextBox { Name = "SimplifiedDisplayName", AccessibleName = "Display name", Text = current, MaxLength = 512, Dock = DockStyle.Top, Margin = new(gap) };
        var label = new Label { Text = "Display name:", AutoSize = true, Dock = DockStyle.Top, Padding = new(0, 0, 0, gap) };
        var body = new Panel { Dock = DockStyle.Fill, Padding = new(gap) }; body.Controls.Add(text); body.Controls.Add(label);
        var ok = new RibbonOptionsAccentButton(_ribbon) { Text = "OK", Size = new(S(86), height) }; var cancel = new Button { Text = "Cancel", Size = new(S(86), height), DialogResult = DialogResult.Cancel, CausesValidation = false };
        ok.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(text.Text)) dialog.DialogResult = DialogResult.OK; else text.Focus(); };
        var footer = Flow(false, ok, cancel); footer.Dock = DockStyle.Bottom; footer.Padding = new(gap); dialog.Controls.Add(body); dialog.Controls.Add(footer); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        return dialog.ShowDialog(FindForm()) == DialogResult.OK ? text.Text.Trim() : null;
    }
}
