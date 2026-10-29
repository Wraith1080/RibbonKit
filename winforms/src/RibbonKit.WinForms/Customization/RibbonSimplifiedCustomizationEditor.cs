using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A source-backed, grouped Simplified customization page with a detached structural/selection draft and independent QAT settings.</summary>
[ToolboxItem(false)]
public sealed partial class RibbonSimplifiedCustomizationEditor : UserControl
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonCustomizationColumns _panes;
    private readonly ComboBox _sourceTab, _targetTab;
    private readonly ListBox _available = List("SimplifiedAvailableCommands", "Available commands grouped by source"), _current = List("SimplifiedSelectedCommands", "Simplified commands grouped by source");
    private readonly TextBox _search = Search("SimplifiedAvailableSearch", "Search commands"), _currentSearch = Search("SimplifiedSelectedSearch", "Search selected commands");
    private readonly Label _status = new() { Name = "SimplifiedCustomizationStatus", AutoSize = true, Dock = DockStyle.Fill };
    private readonly RibbonOptionsFrame _left, _right, _leftSearch, _rightSearch;
    private readonly Button _add, _remove, _up, _down, _reset, _import, _export, _new, _edit;
    private readonly List<Font> _fonts = [];
    private Font? _headerFont;
    private Bitmap? _importIcon, _exportIcon;
    private bool _refreshing;
    private RibbonOptionsActionMenu? _menu;
    internal RibbonOptionsActionMenu? ActionMenu => _menu;
    private sealed record TabChoice(string Id, string Caption) { public override string ToString() => Caption; }
    private sealed record Row(string Id, string Caption, string GroupId, RibbonItem? Source, bool Header = false) { public override string ToString() => Caption; }
    private RibbonCustomizationLayout Structure => Session.Draft.Ribbon!;
    private RibbonCustomizationTab? Tab => Structure.Tabs.FirstOrDefault(t => t.Id == (_targetTab.SelectedItem as TabChoice)?.Id);
    private RibbonCustomizationGroup? Group => Tab?.Groups.FirstOrDefault(g => g.Id == (_current.SelectedItem as Row)?.GroupId) ?? Tab?.Groups.FirstOrDefault();
    /// <summary>Creates a Simplified-scoped editor. It borrows the ribbon and artwork and owns its session and native child controls.</summary>
    public RibbonSimplifiedCustomizationEditor(RibbonControl ribbon, string applicationId)
    {
        ArgumentNullException.ThrowIfNull(ribbon); ribbon.VerifyAccess(); _ribbon = ribbon;
        _sourceTab = Choice(ribbon, "SimplifiedSourceTab", "Available command tab"); _targetTab = Choice(ribbon, "SimplifiedTargetTab", "Ribbon tab to customize");
        Session = ribbon.CommandCatalog.BeginCustomization(applicationId, RibbonCustomizationTarget.Simplified);
        Name = "SimplifiedCustomizationEditor"; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new(96, 96); Font = ribbon.Font;
        BackColor = ribbon.Theme.Surface; ForeColor = ribbon.Theme.Foreground;
        _left = Frame(_available, DockStyle.Fill); _right = Frame(_current, DockStyle.Fill); _leftSearch = Frame(_search, DockStyle.Top); _rightSearch = Frame(_currentSearch, DockStyle.Top);
        _leftSearch.Resize += (_, _) => LayoutSearch(); _rightSearch.Resize += (_, _) => LayoutSearch();
        _add = Button("SimplifiedAdd", "Add »", Add); _remove = Button("SimplifiedRemove", "« Remove", Remove);
        _up = Button("SimplifiedMoveUp", "▲", () => MoveSelected(-1)); _up.AccessibleName = "Move command up";
        _down = Button("SimplifiedMoveDown", "▼", () => MoveSelected(1)); _down.AccessibleName = "Move command down";
        _reset = Button("SimplifiedReset", "Reset ▾", () => OpenActions(_reset!, "Reset"));
        _new = Button("SimplifiedNew", "New ▾", () => OpenActions(_new!, "New")); _edit = Button("SimplifiedEdit", "Edit… ▾", () => OpenActions(_edit!, "Edit"));
        _import = Button("SimplifiedImport", "", () => ImportRequested?.Invoke(this, EventArgs.Empty)); _import.AccessibleName = "Import customization profile";
        _export = Button("SimplifiedExport", "", () => ExportRequested?.Invoke(this, EventArgs.Empty)); _export.AccessibleName = "Export customization profile";
        var panes = _panes = new RibbonCustomizationColumns { RowCount = 5 };
        for (int i = 0; i < 5; i++) panes.RowStyles.Add(new(i == 3 ? SizeType.Percent : SizeType.AutoSize, i == 3 ? 100 : 0));
        panes.Controls.Add(new Label { Text = "Choose commands from the ribbon:", AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }, 0, 0);
        panes.Controls.Add(new Label { Text = "Customize the ribbon:", AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }, 2, 0);
        panes.Controls.Add(_sourceTab, 0, 1); panes.Controls.Add(_targetTab, 2, 1); panes.Controls.Add(_leftSearch, 0, 2); panes.Controls.Add(_rightSearch, 2, 2);
        panes.Controls.Add(_left, 0, 3); panes.AddActions(1, 3, _add, _remove);
        panes.Controls.Add(_right, 2, 3); panes.AddActions(3, 3, _up, _down);
        var leftActions = Flow(false, _reset, _import, _export); leftActions.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        var rightActions = Flow(false, _new, _edit); rightActions.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        panes.Controls.Add(leftActions, 0, 4); panes.Controls.Add(rightActions, 2, 4);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty }; layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.Controls.Add(panes); layout.Controls.Add(_status); Controls.Add(layout);
        _available.DrawItem += DrawRow; _current.DrawItem += DrawRow;
        _sourceTab.SelectedIndexChanged += (_, _) => { if (!_refreshing) RefreshAvailable(); };
        _targetTab.SelectedIndexChanged += (_, _) => { if (!_refreshing) RefreshCurrent(); };
        _available.SelectedIndexChanged += (_, _) => UpdateActions(); _current.SelectedIndexChanged += (_, _) => UpdateActions();
        _available.DoubleClick += (_, _) => Add(); _current.DoubleClick += (_, _) => { if (_current.SelectedItem is Row { Header: false }) ShowLabel(); };
        _search.TextChanged += (_, _) => RefreshAvailable(); _currentSearch.TextChanged += (_, _) => RefreshCurrent();
        RefreshDraft(); UpdateMetrics();
    }
    /// <summary>Gets the owned draft session, including ordinary structure and Simplified selections. QAT applies independently.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public RibbonCustomizationSession Session { get; }
    /// <summary>Requests application-owned import through Session.ImportJson followed by RefreshDraft.</summary>
    public event EventHandler? ImportRequested;
    /// <summary>Requests application-owned storage of Session.ExportJson.</summary>
    public event EventHandler? ExportRequested;
    /// <summary>Updates grouped views without applying or discarding pending edits.</summary>
    public void RefreshDraft()
    {
        string? source = (_sourceTab.SelectedItem as TabChoice)?.Id, target = (_targetTab.SelectedItem as TabChoice)?.Id;
        _refreshing = true; _sourceTab.Items.Clear(); _targetTab.Items.Clear();
        _sourceTab.Items.Add(new TabChoice("", "All commands"));
        foreach (var tab in _ribbon.Tabs.Where(t => !t.IsContextual && !t.IsModal && !_ribbon.CommandCatalog.Structure.Custom(t))) _sourceTab.Items.Add(new TabChoice(tab.Id, tab.DisplayText));
        foreach (var tab in Structure.Tabs) _targetTab.Items.Add(new TabChoice(tab.Id, DisplayName(tab.Id, tab.Label) + (tab.IsCustom ? " (Custom)" : "")));
        SelectTab(_sourceTab, source ?? _ribbon.SelectedTab?.Id); SelectTab(_targetTab, target ?? _ribbon.SelectedTab?.Id);
        _refreshing = false; RefreshAvailable(); RefreshCurrent(); Status("");
    }
    /// <summary>Applies this page only in Simplified mode. Preflight and validation errors are shown inline; application event failures propagate.</summary>
    public bool ApplyChanges()
    {
        if (_ribbon.LayoutMode != RibbonLayoutMode.Simplified) { Status("Switch to Simplified Ribbon to apply this page."); return false; }
        int version = Session.ApplyVersion;
        try { if (!Session.Apply()) { Status("Complete the highlighted ribbon input before applying."); return false; } RefreshDraft(); return true; }
        catch (InvalidOperationException e) when (Session.ApplyVersion == version) { Status(e.Message); return false; }
        catch (ArgumentException e) when (Session.ApplyVersion == version) { Status(e.Message); return false; }
    }
    private string DisplayName(string id, string? label) => label ?? _ribbon.CommandCatalog.Structure.Definition(id)?.Text ?? id;
    private IReadOnlyList<Row> Commands(RibbonCustomizationTab tab)
    {
        var catalog = _ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon).ToDictionary(e => e.CommandId, StringComparer.Ordinal);
        var rows = new List<Row>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in tab.Groups)
        {
            if (group.IsCustom)
            {
                foreach (var entry in group.Commands)
                    if (catalog.GetValueOrDefault(entry.CommandId)?.Source is RibbonItem source && seen.Add(entry.CommandId)) rows.Add(new(entry.CommandId, entry.Label ?? source.Text, group.Id, source));
            }
            else if (_ribbon.CommandCatalog.Structure.Definition(group.Id) is RibbonGroup definition)
                foreach (var item in definition.Items.Where(i => i is not RibbonMenuSeparatorItem))
                {
                    string id = _ribbon.CommandCatalog.GetSource(item).Id; if (seen.Add(id)) rows.Add(new(id, item.Text, group.Id, item));
                }
        }
        return rows;
    }
    private List<RibbonSimplifiedCommand> Choices(bool edit = false)
    {
        if (Tab is not { } tab) return [];
        Session.Draft.Simplified ??= new();
        if (Session.Draft.Simplified.Tabs.FirstOrDefault(t => t.TabId == tab.Id) is { } existing) return existing.Commands;
        var defaults = Commands(tab).Where(c => c.Source is { } item && _ribbon.Simplified.Permanent(item) && RibbonSimplifiedManager.Supported(item) && item.SimplifiedPlacement == RibbonSimplifiedPlacement.Inline).Select(c => new RibbonSimplifiedCommand { CommandId = c.Id, Size = c.Source!.SimplifiedSize }).ToList();
        if (edit) Session.Draft.Simplified.Tabs.Add(new() { TabId = tab.Id, Commands = defaults }); return defaults;
    }
    private void RefreshAvailable()
    {
        if (_refreshing || _add is null) return; string? selected = (_available.SelectedItem as Row)?.Id;
        var entries = _ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon).Where(e => _ribbon.Simplified.Permanent(e.Source));
        if ((_sourceTab.SelectedItem as TabChoice)?.Id is { Length: > 0 } id)
        {
            var tab = _ribbon.Tabs.FirstOrDefault(t => t.Id == id); var ids = tab is null ? new HashSet<string>() : tab.Groups.SelectMany(g => g.Items).Select(i => _ribbon.CommandCatalog.GetSource(i).Id).ToHashSet(); entries = entries.Where(e => ids.Contains(e.CommandId));
        }
        var rows = new List<Row>();
        foreach (var group in entries.Where(e => (e.Category + " " + e.Text).Contains(_search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).GroupBy(e => e.Category))
        { rows.Add(new("header:" + group.Key, group.Key, "", null, true)); rows.AddRange(group.Select(e => new Row(e.CommandId, e.Text, "", e.Source as RibbonItem))); }
        Fill(_available, rows, selected); UpdateActions();
    }
    private void RefreshCurrent(string? selected = null)
    {
        if (_refreshing || _add is null) return; selected ??= (_current.SelectedItem as Row)?.Id; var rows = new List<Row>();
        if (Tab is { } tab)
        {
            var commands = Commands(tab); string? previous = null; var used = new HashSet<string>(); int run = 0;
            foreach (var choice in Choices())
            {
                var command = commands.FirstOrDefault(c => c.Id == choice.CommandId); if (command is null || !command.Caption.Contains(_currentSearch.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)) continue;
                if (command.GroupId != previous) { var group = tab.Groups.First(g => g.Id == command.GroupId); rows.Add(new("header:" + group.Id + ":" + run++, DisplayName(group.Id, group.Label) + (group.IsCustom ? " (Custom)" : ""), group.Id, null, true)); }
                rows.Add(command); used.Add(command.GroupId); previous = command.GroupId;
            }
            if (_currentSearch.TextLength == 0) foreach (var group in tab.Groups.Where(g => !used.Contains(g.Id))) rows.Add(new("header:" + group.Id, DisplayName(group.Id, group.Label) + (group.IsCustom ? " (Custom)" : ""), group.Id, null, true));
        }
        Fill(_current, rows, selected); UpdateActions();
    }
    private static void Fill(ListBox list, IEnumerable<Row> rows, string? selected)
    { list.BeginUpdate(); list.Items.Clear(); list.Items.AddRange(rows.Cast<object>().ToArray()); list.SelectedIndex = Enumerable.Range(0, list.Items.Count).FirstOrDefault(i => ((Row)list.Items[i]!).Id == selected, list.Items.Count > 0 ? 0 : -1); list.EndUpdate(); }
    private void Add()
    {
        if (!_add.Enabled || Tab is not { } tab || _available.SelectedItem is not Row { Header: false, Source: { } source } row) return;
        var choices = Choices(true);
        if (!Commands(tab).Any(c => c.Id == row.Id)) Group!.Commands.Add(new() { CommandId = row.Id });
        int index = _current.SelectedItem is Row current ? choices.FindIndex(c => c.CommandId == current.Id) + 1 : choices.Count;
        choices.Insert(Math.Clamp(index, 0, choices.Count), new() { CommandId = row.Id, Size = source.SimplifiedSize }); RefreshCurrent(row.Id);
    }
    private void Remove()
    {
        if (!_remove.Enabled || _current.SelectedItem is not Row row) return;
        if (row.Header && Group is { IsCustom: true } group) { var ids = Commands(Tab!).Where(c => c.GroupId == group.Id).Select(c => c.Id).ToHashSet(); Choices(true).RemoveAll(c => ids.Contains(c.CommandId)); Tab!.Groups.Remove(group); }
        else Choices(true).RemoveAll(c => c.CommandId == row.Id);
        RefreshCurrent();
    }
    private void MoveSelected(int delta)
    {
        if (_current.SelectedItem is not Row { Header: false } row) return; var choices = Choices(true); int index = choices.FindIndex(c => c.CommandId == row.Id), next = index + delta;
        if (index < 0 || next < 0 || next >= choices.Count) return; (choices[index], choices[next]) = (choices[next], choices[index]); RefreshCurrent(row.Id);
    }
    private void ShowLabel()
    {
        if (_current.SelectedItem is not Row { Header: false } row) return;
        var command = Choices(true).FirstOrDefault(c => c.CommandId == row.Id); if (command is null) return;
        command.Size = command.Size == RibbonItemSize.Small ? RibbonItemSize.Medium : RibbonItemSize.Small; RefreshCurrent(row.Id);
    }
    private void UpdateActions()
    {
        if (_add is null) return; var choices = Choices(); var row = _current.SelectedItem as Row; int index = row is { Header: false } ? choices.FindIndex(c => c.CommandId == row.Id) : -1;
        _add.Enabled = Tab is { } tab && _available.SelectedItem is Row { Header: false, Source: { } item } source && RibbonSimplifiedManager.Supported(item) && !choices.Any(c => c.CommandId == source.Id) && (Commands(tab).Any(c => c.Id == source.Id) || Group is { IsCustom: true });
        _remove.Enabled = index >= 0 || row is { Header: true } && Group is { IsCustom: true };
        _up.Enabled = index > 0; _down.Enabled = index >= 0 && index < choices.Count - 1; _new.Enabled = _edit.Enabled = Tab is not null;
    }
    private static ComboBox Choice(RibbonControl ribbon, string name, string label) => new RibbonOptionsComboBox(ribbon) { Name = name, AccessibleName = label, AutoSize = true, Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = true };
    private static ListBox List(string name, string label) => new() { Name = name, AccessibleName = label, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false, Margin = Padding.Empty };
    private static TextBox Search(string name, string placeholder) => new() { Name = name, AccessibleName = placeholder, PlaceholderText = placeholder, BorderStyle = BorderStyle.None };
    private RibbonOptionsFrame Frame(Control child, DockStyle dock) { var frame = new RibbonOptionsFrame(_ribbon) { Dock = dock, Margin = Padding.Empty }; frame.Controls.Add(child); return frame; }
    private static Button Button(string name, string text, Action click) { var button = new Button { Name = name, Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, UseVisualStyleBackColor = true }; button.Click += (_, _) => click(); return button; }
    private static FlowLayoutPanel Flow(bool vertical, params Control[] controls) { var flow = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.None, FlowDirection = vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty }; flow.Controls.AddRange(controls); return flow; }
    private static void SelectTab(ComboBox list, string? id) => list.SelectedIndex = Enumerable.Range(0, list.Items.Count).FirstOrDefault(i => ((TabChoice)list.Items[i]!).Id == id, list.Items.Count > 0 ? 0 : -1);
    private void Status(string text) { _status.Text = text; _status.Visible = text.Length > 0; }
    private int S(int value) => (int)Math.Ceiling(value * DeviceDpi / 96d);
    private void DrawRow(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ListBox list || e.Index < 0 || list.Items[e.Index] is not Row row) return; var theme = _ribbon.Theme;
        using var brush = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? theme.Checked.Start : theme.Surface); e.Graphics.FillRectangle(brush, e.Bounds);
        var bounds = Rectangle.Inflate(e.Bounds, -S(8), 0);
        if (!row.Header)
        {
            int side = S(20); var icon = new Rectangle(bounds.Left, bounds.Top + (bounds.Height - side) / 2, side, side);
            if (row.Source?.Icon is { } vector) vector.Draw(e.Graphics, icon, new(DeviceDpi, theme.Foreground, true)); else if (row.Source?.Image is { } image) e.Graphics.DrawImage(image, icon);
            bounds.X += side + S(8); bounds.Width = Math.Max(0, bounds.Width - side - S(8));
        }
        TextRenderer.DrawText(e.Graphics, row.Caption, row.Header ? _headerFont ?? Font : Font, bounds, theme.Foreground, TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix); e.DrawFocusRectangle();
    }
    internal Size PageMinimum => new(Math.Max(MinimumSize.Width, RibbonQuickAccessEditorMetrics.Minimum(DeviceDpi, Font.Height, _ribbon.Density).Width), RibbonQuickAccessEditorMetrics.Minimum(DeviceDpi, Font.Height, _ribbon.Density).Height + S(40));
    private void UpdateMetrics()
    {
        if (_ribbon is null || _edit is null) return; SuspendLayout(); int gap = S(8), height = RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density);
        _panes.UpdateMetrics(DeviceDpi, Font, _ribbon.Density);
        _headerFont = new(Font, FontStyle.Bold); _fonts.Add(_headerFont); MinimumSize = PageMinimum;
        _available.ItemHeight = _current.ItemHeight = RibbonQuickAccessEditorMetrics.RowHeight(DeviceDpi, Font.Height, _ribbon.Density);
        foreach (var combo in new[] { _sourceTab, _targetTab }) { combo.ItemHeight = Math.Max(Font.Height + S(4), height - S(6)); combo.Margin = new(0, gap, 0, 0); combo.BackColor = _ribbon.Theme.Surface; combo.ForeColor = _ribbon.Theme.Foreground; }
        foreach (var frame in new[] { _leftSearch, _rightSearch }) { frame.Height = height; frame.Margin = new(0, gap, 0, gap); }
        foreach (var search in new[] { _search, _currentSearch }) { search.BackColor = _ribbon.Theme.Surface; search.ForeColor = _ribbon.Theme.Foreground; } LayoutSearch();
        foreach (var button in new[] { _add, _remove, _up, _down, _reset, _import, _export, _new, _edit }) { button.MinimumSize = new(S(button == _up || button == _down ? 40 : button == _import || button == _export ? 28 : 86), height); button.Margin = new(gap / 2); }
        foreach (var button in new[] { _reset, _import, _export, _new, _edit }) button.Margin = new(0, gap, gap, 0);
        _import.Image = _export.Image = null; _importIcon?.Dispose(); _exportIcon?.Dispose();
        _import.Image = _importIcon = RibbonOptionsTransferIcons.Image(_ribbon, S(_ribbon.Density == RibbonDensity.Touch ? 24 : 20), false); _export.Image = _exportIcon = RibbonOptionsTransferIcons.Image(_ribbon, S(_ribbon.Density == RibbonDensity.Touch ? 24 : 20), true);
        _available.BackColor = _current.BackColor = _ribbon.Theme.Surface; _available.ForeColor = _current.ForeColor = _ribbon.Theme.Foreground;
        foreach (var frame in new[] { _left, _right, _leftSearch, _rightSearch }) frame.RefreshAppearance(); ResumeLayout(true);
    }
    private void LayoutSearch() { if (_rightSearch is null) return; foreach (var (search, frame) in new[] { (_search, _leftSearch), (_currentSearch, _rightSearch) }) search.SetBounds(S(4), Math.Max(0, (frame.Height - search.Height) / 2), Math.Max(1, frame.Width - S(8)), search.Height); }
    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); UpdateMetrics(); }
    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); UpdateMetrics(); }
    /// <inheritdoc />
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (!Visible) _menu?.Close(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _menu?.Dispose(); Session?.Dispose(); } base.Dispose(disposing); if (disposing) { _importIcon?.Dispose(); _exportIcon?.Dispose(); foreach (var font in _fonts) font.Dispose(); } }
}
