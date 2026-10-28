using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A reusable native QAT options page. Owns its draft session and child controls; borrows the ribbon and command icons.</summary>
[ToolboxItem(false)]
public sealed class RibbonQuickAccessCustomizationEditor : UserControl
{
    private readonly RibbonControl _ribbon;
    private readonly TableLayoutPanel _layout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
    private readonly ListBox _available = List("AvailableCommands", "Available commands");
    private readonly ListBox _current = List("ToolbarCommands", "Commands on the Quick Access Toolbar");
    private readonly TextBox _search = new() { Name = "CommandSearch", AccessibleName = "Search available commands", BorderStyle = BorderStyle.None, PlaceholderText = "Search commands" };
    private readonly TextBox _currentSearch = new() { Name = "ToolbarSearch", AccessibleName = "Search current toolbar commands", BorderStyle = BorderStyle.None, PlaceholderText = "Search toolbar commands" };
    private readonly RibbonOptionsFrame _searchFrame, _currentSearchFrame, _availableFrame, _currentFrame;
    private readonly FlowLayoutPanel _actions = new() { AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Label _status = new() { Name = "CustomizationStatus", AutoSize = true, Dock = DockStyle.Fill, AccessibleName = "Customization status" };
    private readonly Button _add, _remove, _up, _down;
    private readonly Button _reset, _reload;
    private IReadOnlyList<RibbonCommandCatalogEntry> _catalog = [];
    private sealed record Row(string Id, RibbonCommandCatalogEntry? Entry, string Fallback)
    {
        public override string ToString() => Entry is { } e ? e.Text + ", " + e.Category + (e.IsGroup ? ", group" : "") : Fallback + ", unavailable command";
    }

    /// <summary>Creates a page with its own isolated draft for the supplied application identity. Disposing the page never disposes the ribbon.</summary>
    public RibbonQuickAccessCustomizationEditor(RibbonControl ribbon, string applicationId)
    {
        ArgumentNullException.ThrowIfNull(ribbon); ribbon.VerifyAccess();
        _ribbon = ribbon; Session = ribbon.CommandCatalog.BeginCustomization(applicationId, RibbonCustomizationTarget.QuickAccess);
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new(96, 96);
        Font = ribbon.Font; BackColor = ribbon.Theme.Surface; ForeColor = ribbon.Theme.Foreground;
        Name = "QuickAccessCustomizationEditor";
        _searchFrame = Frame("CommandSearchFrame", _search, DockStyle.Top);
        _currentSearchFrame = Frame("ToolbarSearchFrame", _currentSearch, DockStyle.Top);
        _availableFrame = Frame("AvailableCommandsFrame", _available, DockStyle.Fill);
        _currentFrame = Frame("ToolbarCommandsFrame", _current, DockStyle.Fill);
        _searchFrame.Resize += (_, _) => LayoutSearch(); _currentSearchFrame.Resize += (_, _) => LayoutSearch();
        _searchFrame.MouseDown += (_, _) => _search.Focus();
        _currentSearchFrame.MouseDown += (_, _) => _currentSearch.Focus();
        _add = Button("AddCommand", "&Add »", AddSelected);
        _remove = Button("RemoveCommand", "« &Remove", RemoveSelected);
        _up = Button("MoveCommandUp", "▲", () => MoveSelected(-1)); _up.AccessibleName = "Move command up";
        _down = Button("MoveCommandDown", "▼", () => MoveSelected(1)); _down.AccessibleName = "Move command down";
        _reset = Button("ResetCustomization", "&Reset", ResetCommands);
        _reload = Button("ReloadCustomization", "Re&load", () => { Session.Reload(); RefreshDraft(); });
        var panes = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3, Margin = Padding.Empty };
        panes.ColumnStyles.Add(new(SizeType.Percent, 50)); panes.ColumnStyles.Add(new(SizeType.AutoSize)); panes.ColumnStyles.Add(new(SizeType.Percent, 50)); panes.ColumnStyles.Add(new(SizeType.AutoSize));
        panes.RowStyles.Add(new(SizeType.AutoSize)); panes.RowStyles.Add(new(SizeType.AutoSize)); panes.RowStyles.Add(new(SizeType.Percent, 100));
        var order = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.None, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        order.Controls.AddRange([_up, _down]);
        var transfer = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.None, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        transfer.Controls.AddRange([_add, _remove]);
        panes.Controls.Add(new Label { Name = "AvailableCommandsLabel", Text = "Choose commands from the ribbon:", AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }, 0, 0);
        panes.Controls.Add(new Label { Name = "ToolbarCommandsLabel", Text = "Current Quick Access Toolbar:", AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }, 2, 0);
        panes.Controls.Add(_searchFrame, 0, 1); panes.Controls.Add(_currentSearchFrame, 2, 1);
        panes.Controls.Add(_availableFrame, 0, 2); panes.Controls.Add(transfer, 1, 2); panes.Controls.Add(_currentFrame, 2, 2); panes.Controls.Add(order, 3, 2);
        _layout.RowStyles.Add(new(SizeType.Percent, 100));
        for (int i = 0; i < 2; i++) _layout.RowStyles.Add(new(SizeType.AutoSize));
        _layout.Controls.Add(panes, 0, 0);
        _actions.Controls.AddRange([_reset, _reload]);
        _layout.Controls.Add(_actions, 0, 1); _layout.Controls.Add(_status, 0, 2); Controls.Add(_layout);
        _available.DrawItem += DrawRow; _current.DrawItem += DrawRow;
        _available.SelectedIndexChanged += (_, _) => UpdateActions(); _current.SelectedIndexChanged += (_, _) => UpdateActions();
        _available.DoubleClick += (_, _) => AddSelected(); _current.DoubleClick += (_, _) => RemoveSelected();
        _search.TextChanged += (_, _) => RefreshAvailable();
        _currentSearch.TextChanged += (_, _) => RefreshCurrent();
        RefreshDraft(); UpdateMetrics();
    }

    /// <summary>Gets the owned draft session. After changing it programmatically, call RefreshDraft to update the page.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonCustomizationSession Session { get; }

    /// <summary>Reloads the displayed catalog and current draft without applying or discarding edits.</summary>
    public void RefreshDraft()
    {
        _catalog = _ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess);
        RefreshAvailable(); RefreshCurrent(); Status("");
        UpdateActions();
    }

    /// <summary>Attempts to apply the draft. Validation or stale-state errors are shown on the page and return false; application event exceptions propagate.</summary>
    public bool ApplyChanges()
    {
        // Capture errors from preflight/commit only. Host persistence handlers keep
        // their ordinary exception semantics rather than being reported as draft failures.
        int version = Session.ApplyVersion;
        try
        {
            if (!Session.Apply()) { Status("Complete the highlighted ribbon input before applying."); return false; }
            RefreshDraft(); Status("Toolbar changes applied."); return true;
        }
        catch (InvalidOperationException e) when (version == Session.ApplyVersion) { Status(e.Message); return false; }
        catch (ArgumentException e) when (version == Session.ApplyVersion) { Status(e.Message); return false; }
    }
    private void Status(string text) { _status.Text = text; _status.Visible = text.Length > 0; }
    private void ResetCommands()
    {
        // This page edits commands; placement and visibility belong to the menu.
        var position = Session.Draft.QuickAccessPosition; bool visible = Session.Draft.QuickAccessVisible;
        Session.Reset();
        Session.Draft.QuickAccessPosition = position; Session.Draft.QuickAccessVisible = visible;
        RefreshDraft();
    }

    private static ListBox List(string name, string label) => new() { Name = name, AccessibleName = label, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Margin = Padding.Empty, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, HorizontalScrollbar = false };
    private RibbonOptionsFrame Frame(string name, Control child, DockStyle dock)
    {
        var frame = new RibbonOptionsFrame(_ribbon) { Name = name, Dock = dock, Margin = Padding.Empty };
        frame.Controls.Add(child); return frame;
    }
    private static Button Button(string name, string text, Action action)
    {
        var button = new Button { Name = name, Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, UseVisualStyleBackColor = true };
        button.Click += (_, _) => action(); return button;
    }
    private void RefreshAvailable()
    {
        string? id = (_available.SelectedItem as Row)?.Id;
        _available.BeginUpdate(); _available.Items.Clear();
        foreach (var e in _catalog.Where(e => (e.Text + " " + e.Category).Contains(_search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)))
            _available.Items.Add(new Row(e.CommandId, e, e.Text));
        Select(_available, id); _available.EndUpdate(); UpdateActions();
    }
    private void RefreshCurrent(string? selected = null)
    {
        selected ??= (_current.SelectedItem as Row)?.Id;
        _current.BeginUpdate(); _current.Items.Clear();
        foreach (var e in Session.Draft.QuickAccess)
        {
            var entry = _catalog.FirstOrDefault(c => c.CommandId == e.CommandId);
            if (((entry?.Text ?? e.CommandId) + " " + entry?.Category).Contains(_currentSearch.Text.Trim(), StringComparison.CurrentCultureIgnoreCase))
                _current.Items.Add(new Row(e.Id, entry, e.CommandId));
        }
        Select(_current, selected); _current.EndUpdate(); UpdateActions();
    }
    private static void Select(ListBox list, string? id)
    {
        int index = Enumerable.Range(0, list.Items.Count).FirstOrDefault(i => ((Row)list.Items[i]).Id == id, -1);
        list.SelectedIndex = index >= 0 ? index : list.Items.Count > 0 ? 0 : -1;
    }
    private void AddSelected()
    {
        if (!_add.Enabled || _available.SelectedItem is not Row { Entry: { } e }) return;
        var row = new RibbonCustomizationEntry { CommandId = e.CommandId }; Session.Draft.QuickAccess.Add(row); RefreshCurrent(row.Id);
    }
    private void RemoveSelected()
    {
        int index = _current.SelectedIndex; if (_current.SelectedItem is not Row row) return;
        Session.Draft.QuickAccess.RemoveAll(e => e.Id == row.Id); RefreshCurrent();
        if (_current.Items.Count > 0) _current.SelectedIndex = Math.Min(index, _current.Items.Count - 1);
    }
    private void MoveSelected(int delta)
    {
        int index = SelectedDraftIndex(), next = index + delta;
        if (index < 0 || next < 0 || next >= Session.Draft.QuickAccess.Count) return;
        var row = Session.Draft.QuickAccess[index]; Session.Draft.QuickAccess.RemoveAt(index); Session.Draft.QuickAccess.Insert(next, row); RefreshCurrent(row.Id);
    }
    private void UpdateActions()
    {
        if (_add is null) return;
        _add.Enabled = _available.SelectedItem is Row { Entry: { } e } && !Session.Draft.QuickAccess.Any(r => r.CommandId == e.CommandId);
        int index = SelectedDraftIndex();
        _remove.Enabled = index >= 0; _up.Enabled = index > 0;
        _down.Enabled = index >= 0 && index < Session.Draft.QuickAccess.Count - 1;
    }
    private int SelectedDraftIndex() => _current.SelectedItem is Row row ? Session.Draft.QuickAccess.FindIndex(e => e.Id == row.Id) : -1;
    private void DrawRow(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox list || list.Items[e.Index] is not Row row) return;
        bool selected = e.State.HasFlag(DrawItemState.Selected);
        var theme = _ribbon.Theme; var fill = selected ? theme.Checked.Start : theme.Surface;
        using var background = new SolidBrush(fill); e.Graphics.FillRectangle(background, e.Bounds);
        int gap = ScaleLogical(6), icon = ScaleLogical(20);
        var bounds = Rectangle.Inflate(e.Bounds, -gap, 0);
        if (row.Entry?.Source is RibbonItem { Icon: { } image })
            image.Draw(e.Graphics, new(bounds.Left, e.Bounds.Top + (e.Bounds.Height - icon) / 2, icon, icon), new(DeviceDpi, theme.Foreground, true));
        else if (row.Entry?.Source is RibbonItem { Image: { } bitmap })
            e.Graphics.DrawImage(bitmap, new Rectangle(bounds.Left, e.Bounds.Top + (e.Bounds.Height - icon) / 2, icon, icon));
        bounds.X += icon + gap; bounds.Width = Math.Max(0, bounds.Width - icon - gap);
        var text = row.Entry is { } entry ? (ReferenceEquals(list, _available) ? entry.Category + " › " : "") + entry.Text : row.Fallback + " (unavailable)";
        TextRenderer.DrawText(e.Graphics, text, Font, bounds, theme.Foreground, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
        e.DrawFocusRectangle();
    }
    internal int ScaleLogical(int value) => (int)Math.Ceiling(value * DeviceDpi / 96d);
    internal Size PageMinimum
    {
        get
        {
            var measured = RibbonQuickAccessEditorMetrics.Minimum(DeviceDpi, Font.Height, _ribbon.Density);
            return new(Math.Max(MinimumSize.Width, measured.Width), Math.Max(MinimumSize.Height, measured.Height));
        }
    }
    private void UpdateMetrics()
    {
        if (_ribbon is null || _add is null || _currentFrame is null) return;
        SuspendLayout();
        int gap = ScaleLogical(8), height = RibbonQuickAccessEditorMetrics.RowHeight(DeviceDpi, Font.Height, _ribbon.Density);
        var minimum = RibbonQuickAccessEditorMetrics.Minimum(DeviceDpi, Font.Height, _ribbon.Density);
        MinimumSize = new(Math.Max(MinimumSize.Width, minimum.Width), minimum.Height);
        Padding = Padding.Empty; _available.ItemHeight = height; _current.ItemHeight = height;
        foreach (var frame in new[] { _searchFrame, _currentSearchFrame })
        { frame.Height = RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density); frame.Margin = new(0, gap, 0, gap); }
        foreach (var search in new[] { _search, _currentSearch }) { search.BackColor = _ribbon.Theme.Surface; search.ForeColor = _ribbon.Theme.Foreground; }
        LayoutSearch();
        foreach (var button in new[] { _add, _remove, _up, _down, _reset, _reload })
        {
            button.MinimumSize = new(ScaleLogical(button == _up || button == _down ? 40 : 86), RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density));
            button.Padding = new(ScaleLogical(5), 0, ScaleLogical(5), 0); button.Margin = new(gap / 2);
        }
        _reset.Margin = _reload.Margin = new(0, gap, gap, 0);
        _available.BackColor = _current.BackColor = _ribbon.Theme.Surface; _available.ForeColor = _current.ForeColor = _ribbon.Theme.Foreground;
        foreach (var frame in new[] { _searchFrame, _currentSearchFrame, _availableFrame, _currentFrame }) frame.RefreshAppearance();
        ResumeLayout(true); Invalidate(true);
    }
    private void LayoutSearch()
    {
        int inset = ScaleLogical(4);
        if (_currentSearchFrame is null) return;
        foreach (var (search, frame) in new[] { (_search, _searchFrame), (_currentSearch, _currentSearchFrame) })
            search.SetBounds(inset, Math.Max(0, (frame.ClientSize.Height - search.Height) / 2), Math.Max(1, frame.ClientSize.Width - 2 * inset), search.Height);
    }
    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); UpdateMetrics(); }
    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); UpdateMetrics(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Session?.Dispose(); base.Dispose(disposing); }
}

internal static class RibbonQuickAccessEditorMetrics
{
    private static int Scale(int value, int dpi) => (int)Math.Ceiling(value * dpi / 96d);
    internal static int RowHeight(int dpi, int fontHeight, RibbonDensity density) => Math.Max(Scale(density == RibbonDensity.Touch ? 44 : 24, dpi), fontHeight + Scale(8, dpi));
    internal static int ButtonHeight(int dpi, int fontHeight, RibbonDensity density) => Math.Max(Scale(density == RibbonDensity.Touch ? 44 : 28, dpi), fontHeight + Scale(12, dpi));
    internal static Size Minimum(int dpi, int fontHeight, RibbonDensity density) => new(Math.Max(Scale(650, dpi), fontHeight * 28), Scale(24, dpi) + fontHeight * 2 + RowHeight(dpi, fontHeight, density) * 3 + ButtonHeight(dpi, fontHeight, density) * 2);
    internal static Rectangle ClampWindow(Rectangle requested, Rectangle work, int dpi)
    {
        int inset = Math.Min(Scale(8, dpi), Math.Max(0, Math.Min(work.Width, work.Height) / 4));
        int width = Math.Clamp(requested.Width, 1, Math.Max(1, work.Width - 2 * inset));
        int height = Math.Clamp(requested.Height, 1, Math.Max(1, work.Height - 2 * inset));
        return new(Math.Clamp(requested.Left, work.Left + inset, work.Right - inset - width), Math.Clamp(requested.Top, work.Top + inset, work.Bottom - inset - height), width, height);
    }
}
