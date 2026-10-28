using System.ComponentModel;

namespace RibbonKit.WinForms;

/// <summary>A reusable native Customize Ribbon page with an isolated draft, command catalog and hierarchy editor.</summary>
[ToolboxItem(false)]
public sealed class RibbonCustomizationEditor : UserControl
{
    private readonly RibbonControl _ribbon;
    private readonly ListBox _available = new() { Name = "RibbonAvailableCommands", AccessibleName = "Available ribbon commands", DisplayMember = nameof(RibbonCommandCatalogEntry.Text), Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed };
    private readonly RibbonCustomizationTree _tree;
    private readonly TextBox _search = new() { Name = "RibbonCommandSearch", AccessibleName = "Search available ribbon commands", PlaceholderText = "Search commands", BorderStyle = BorderStyle.None };
    private readonly TextBox _treeSearch = new() { Name = "RibbonHierarchySearch", AccessibleName = "Search ribbon hierarchy", PlaceholderText = "Search ribbon", BorderStyle = BorderStyle.None };
    private readonly Label _status = new() { Name = "RibbonCustomizationStatus", AccessibleName = "Ribbon customization status", AutoSize = true, Dock = DockStyle.Fill };
    private readonly RibbonOptionsFrame _availableFrame, _treeFrame, _searchFrame, _treeSearchFrame;
    private readonly Button _add, _remove, _up, _down, _reset, _reload, _import, _export, _newTab, _newGroup, _edit;
    private readonly ImageList _icons = new() { ColorDepth = ColorDepth.Depth32Bit }, _checks = new() { ColorDepth = ColorDepth.Depth32Bit };
    private IReadOnlyList<RibbonCommandCatalogEntry> _catalog = [];
    private readonly Dictionary<string, int> _imageIndices = [];
    private readonly List<Bitmap> _bitmaps = [];
    private Bitmap? _importImage, _exportImage;
    private RibbonCustomizationLayout Draft => Session.Draft.Ribbon!;
    private sealed record Node(string Id, object Data, RibbonCustomizationTab Tab, RibbonCustomizationGroup? Group = null);
    private Node? Selected => _tree.SelectedNode?.Tag as Node;

    /// <summary>Creates an editor for the supplied ribbon and stable application identity. The ribbon and its icons remain application-owned.</summary>
    public RibbonCustomizationEditor(RibbonControl ribbon, string applicationId)
    {
        ArgumentNullException.ThrowIfNull(ribbon); ribbon.VerifyAccess(); _ribbon = ribbon;
        _tree = new(ribbon) { Name = "RibbonHierarchy", AccessibleName = "Customize the ribbon", Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, HideSelection = false };
        _tree.PaintIcon = (graphics, node, bounds) =>
        {
            var source = (node.Tag as Node)?.Data switch { RibbonItem item => item, RibbonCustomizationGroup group => _ribbon.CommandCatalog.Structure.Definition(group.Id), RibbonCustomizationCommand command => _catalog.FirstOrDefault(e => e.CommandId == command.CommandId)?.Source, _ => null };
            if (source is not null) DrawIcon(graphics, source, bounds);
        };
        Session = ribbon.CommandCatalog.BeginCustomization(applicationId, RibbonCustomizationTarget.Ribbon);
        AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new(96, 96); Font = ribbon.Font;
        Name = "RibbonCustomizationEditor"; BackColor = ribbon.Theme.Surface; ForeColor = ribbon.Theme.Foreground;
        _availableFrame = Frame("RibbonAvailableFrame", _available, DockStyle.Fill); _treeFrame = Frame("RibbonHierarchyFrame", _tree, DockStyle.Fill);
        _searchFrame = Frame("RibbonSearchFrame", _search, DockStyle.Top); _treeSearchFrame = Frame("RibbonHierarchySearchFrame", _treeSearch, DockStyle.Top);
        _searchFrame.Resize += (_, _) => LayoutSearch(); _treeSearchFrame.Resize += (_, _) => LayoutSearch();
        _searchFrame.MouseDown += (_, _) => _search.Focus(); _treeSearchFrame.MouseDown += (_, _) => _treeSearch.Focus();
        _add = Button("AddRibbonCommand", "&Add »", AddSelected); _remove = Button("RemoveRibbonItem", "« &Remove", RemoveSelected);
        _up = Button("MoveRibbonItemUp", "▲", () => MoveSelected(-1)); _up.AccessibleName = "Move ribbon item up";
        _down = Button("MoveRibbonItemDown", "▼", () => MoveSelected(1)); _down.AccessibleName = "Move ribbon item down";
        _reset = Button("ResetRibbon", "&Reset", () => { Session.Reset(); RefreshDraft(); });
        _reload = Button("ReloadRibbon", "Re&load", () => { Session.Reload(); RefreshDraft(); });
        _import = Button("ImportRibbonProfile", "", () => ImportRequested?.Invoke(this, EventArgs.Empty)); _import.AccessibleName = "Import customization profile";
        _export = Button("ExportRibbonProfile", "", () => ExportRequested?.Invoke(this, EventArgs.Empty)); _export.AccessibleName = "Export customization profile";
        _newTab = Button("NewRibbonTab", "New &Tab", NewTab); _newGroup = Button("NewRibbonGroup", "New &Group", NewGroup); _edit = Button("EditRibbonItem", "&Edit…", EditSelected);
        var panes = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4, Margin = Padding.Empty };
        panes.ColumnStyles.Add(new(SizeType.Percent, 50)); panes.ColumnStyles.Add(new(SizeType.AutoSize)); panes.ColumnStyles.Add(new(SizeType.Percent, 50)); panes.ColumnStyles.Add(new(SizeType.AutoSize));
        panes.RowStyles.Add(new(SizeType.AutoSize)); panes.RowStyles.Add(new(SizeType.AutoSize)); panes.RowStyles.Add(new(SizeType.Percent, 100)); panes.RowStyles.Add(new(SizeType.AutoSize));
        panes.Controls.Add(new Label { Text = "Choose commands from the ribbon:", AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }, 0, 0);
        panes.Controls.Add(new Label { Text = "Customize the ribbon:", AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty }, 2, 0);
        panes.Controls.Add(_searchFrame, 0, 1); panes.Controls.Add(_treeSearchFrame, 2, 1);
        panes.Controls.Add(_availableFrame, 0, 2); panes.Controls.Add(Flow(true, _add, _remove), 1, 2); panes.Controls.Add(_treeFrame, 2, 2); panes.Controls.Add(Flow(true, _up, _down), 3, 2);
        var left = Flow(false, _reset, _reload, _import, _export); left.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        var right = Flow(false, _newTab, _newGroup, _edit); right.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        panes.Controls.Add(left, 0, 3); panes.Controls.Add(right, 2, 3); panes.SetColumnSpan(right, 2);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.Controls.Add(panes, 0, 0); layout.Controls.Add(_status, 0, 1); Controls.Add(layout);
        _available.DrawItem += DrawRow; _available.SelectedIndexChanged += (_, _) => UpdateActions(); _available.DoubleClick += (_, _) => AddSelected();
        _tree.AfterSelect += (_, _) => UpdateActions(); _tree.CheckRequested += node => { _tree.SelectedNode = node; ToggleTab(); };
        _tree.KeyDown += (_, e) => { if (e.KeyCode == Keys.Space && Selected?.Data is RibbonCustomizationTab) { ToggleTab(); e.Handled = e.SuppressKeyPress = true; } };
        _search.TextChanged += (_, _) => RefreshAvailable(); _treeSearch.TextChanged += (_, _) => RefreshTree();
        UpdateMetrics(); RefreshDraft();
    }
    /// <summary>Gets the owned Ribbon-scoped draft session. Call RefreshDraft after programmatic changes or import.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RibbonCustomizationSession Session { get; }
    /// <summary>Requests application-owned profile import. The host calls Session.ImportJson and RefreshDraft; no files are accessed by the library.</summary>
    public event EventHandler? ImportRequested;
    /// <summary>Requests application-owned export or storage of Session.ExportJson. Export includes this page's draft and live QAT preferences.</summary>
    public event EventHandler? ExportRequested;
    /// <summary>Refreshes both views without applying or discarding the draft.</summary>
    public void RefreshDraft() { _catalog = _ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon); RefreshIcons(); RefreshAvailable(); RefreshTree(); Status(""); }
    /// <summary>Applies this page's draft, reporting preflight and validation errors inline. Application event exceptions retain their normal semantics.</summary>
    public bool ApplyChanges()
    {
        int version = Session.ApplyVersion;
        try { if (!Session.Apply()) { Status("Complete the highlighted ribbon input before applying."); return false; } RefreshDraft(); Status("Ribbon changes applied."); return true; }
        catch (InvalidOperationException e) when (version == Session.ApplyVersion) { Status(e.Message); return false; }
        catch (ArgumentException e) when (version == Session.ApplyVersion) { Status(e.Message); return false; }
    }
    private void Status(string text) { _status.Text = text; _status.Visible = text.Length > 0; }
    private static Button Button(string name, string text, Action click) { var b = new Button { Name = name, Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, UseVisualStyleBackColor = true }; b.Click += (_, _) => click(); return b; }
    private RibbonOptionsFrame Frame(string name, Control child, DockStyle dock) { var frame = new RibbonOptionsFrame(_ribbon) { Name = name, Dock = dock, Margin = Padding.Empty }; frame.Controls.Add(child); return frame; }
    private static FlowLayoutPanel Flow(bool vertical, params Control[] buttons)
    { var flow = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.None, FlowDirection = vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty }; flow.Controls.AddRange(buttons); return flow; }
    private void RefreshAvailable()
    {
        string? id = (_available.SelectedItem as RibbonCommandCatalogEntry)?.CommandId;
        _available.BeginUpdate(); _available.Items.Clear();
        foreach (var entry in _catalog.Where(e => Match(e.Category + " " + e.Text, _search.Text))) _available.Items.Add(entry);
        _available.SelectedIndex = Enumerable.Range(0, _available.Items.Count).FirstOrDefault(i => ((RibbonCommandCatalogEntry)_available.Items[i]).CommandId == id, _available.Items.Count > 0 ? 0 : -1);
        _available.EndUpdate(); UpdateActions();
    }
    private static bool Match(string text, string query) => text.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase);
    private string Label(string id, string? alias) => alias ?? _ribbon.CommandCatalog.Structure.Definition(id)?.Text ?? _catalog.FirstOrDefault(e => e.CommandId == id)?.Text ?? id;
    private static string Suffix(string label, bool custom) => label + (custom ? " (Custom)" : "");
    private void RefreshTree(string? selected = null)
    {
        selected ??= Selected?.Id;
        var expanded = AllNodes(_tree.Nodes).Where(n => n.IsExpanded).Select(n => ((Node)n.Tag!).Id).ToHashSet();
        bool first = _tree.Nodes.Count == 0, filtered = _treeSearch.TextLength > 0;
        _tree.BeginUpdate(); _tree.Nodes.Clear();
        foreach (var tab in Draft.Tabs)
        {
            var tn = new TreeNode(Suffix(Label(tab.Id, tab.Label), tab.IsCustom)) { Tag = new Node(tab.Id, tab, tab), StateImageIndex = tab.Visible ? 1 : 0, ImageIndex = 0, SelectedImageIndex = 0 };
            foreach (var group in tab.Groups)
            {
                var gn = new TreeNode(Suffix(Label(group.Id, group.Label), group.IsCustom)) { Tag = new Node(group.Id, group, tab, group), ImageIndex = 0, SelectedImageIndex = 0 };
                if (group.IsCustom)
                    foreach (var command in group.Commands) gn.Nodes.Add(CommandNode(command.Id, Label(command.CommandId, command.Label), command, tab, group, command.CommandId));
                else if (_ribbon.CommandCatalog.Structure.Definition(group.Id) is RibbonGroup source)
                    foreach (var item in source.Items) gn.Nodes.Add(CommandNode(item.Id, item.Text, item, tab, group, item.Id));
                if (!filtered || Match(tn.Text + " " + gn.Text, _treeSearch.Text)) { tn.Nodes.Add(gn); }
                else { foreach (var cn in gn.Nodes.Cast<TreeNode>().Where(n => !Match(n.Text, _treeSearch.Text)).ToArray()) gn.Nodes.Remove(cn); if (gn.Nodes.Count > 0) tn.Nodes.Add(gn); }
            }
            if (!filtered || tn.Nodes.Count > 0 || Match(tn.Text, _treeSearch.Text)) _tree.Nodes.Add(tn);
        }
        foreach (var node in AllNodes(_tree.Nodes)) if (first || filtered || expanded.Contains(((Node)node.Tag!).Id)) node.Expand();
        _tree.SelectedNode = AllNodes(_tree.Nodes).FirstOrDefault(n => ((Node)n.Tag!).Id == selected) ?? _tree.Nodes.Cast<TreeNode>().FirstOrDefault();
        _tree.EndUpdate(); UpdateActions();
    }
    private TreeNode CommandNode(string id, string text, object data, RibbonCustomizationTab tab, RibbonCustomizationGroup group, string commandId)
    { int image = _imageIndices.GetValueOrDefault(commandId); return new(text) { Tag = new Node(id, data, tab, group), ImageIndex = image, SelectedImageIndex = image }; }
    private static IEnumerable<TreeNode> AllNodes(TreeNodeCollection nodes) { foreach (TreeNode node in nodes) { yield return node; foreach (var child in AllNodes(node.Nodes)) yield return child; } }
    private void ToggleTab()
    {
        if (Selected?.Data is not RibbonCustomizationTab tab) return;
        if (tab.Visible && Draft.Tabs.Count(t => t.Visible) == 1) { Status("Keep at least one ribbon tab visible."); return; }
        tab.Visible = !tab.Visible; _tree.SelectedNode!.StateImageIndex = tab.Visible ? 1 : 0; Status("");
    }
    private void NewTab()
    {
        var tab = new RibbonCustomizationTab { Label = "New Tab", Groups = [new() { Label = "New Group" }] };
        int at = Selected is { } selection ? Draft.Tabs.IndexOf(selection.Tab) + 1 : Draft.Tabs.Count;
        Draft.Tabs.Insert(at, tab); _treeSearch.Clear(); RefreshTree(tab.Groups[0].Id);
    }
    private void NewGroup()
    {
        if (Selected is not { } selected) return;
        var group = new RibbonCustomizationGroup { Label = "New Group" }; int at = selected.Group is { } parent ? selected.Tab.Groups.IndexOf(parent) + 1 : selected.Tab.Groups.Count;
        selected.Tab.Groups.Insert(at, group); _treeSearch.Clear(); RefreshTree(group.Id);
    }
    private void AddSelected()
    {
        if (!_add.Enabled || Selected is not { Group.IsCustom: true } selected || _available.SelectedItem is not RibbonCommandCatalogEntry entry) return;
        var command = new RibbonCustomizationCommand { CommandId = entry.CommandId }; selected.Group.Commands.Add(command); _treeSearch.Clear(); RefreshTree(command.Id);
    }
    private void RemoveSelected()
    {
        if (!_remove.Enabled || Selected is not { } selected) return;
        string? parent = selected.Group?.Id;
        switch (selected.Data)
        {
            case RibbonCustomizationTab tab: Draft.Tabs.Remove(tab); parent = null; break;
            case RibbonCustomizationGroup group: selected.Tab.Groups.Remove(group); parent = selected.Tab.Id; break;
            case RibbonCustomizationCommand command: selected.Group!.Commands.Remove(command); break;
        }
        RefreshTree(parent);
    }
    private (int Index, int Count) Position()
    {
        return Selected switch
        {
            { Data: RibbonCustomizationTab tab } => (Draft.Tabs.IndexOf(tab), Draft.Tabs.Count),
            { Data: RibbonCustomizationGroup group, Tab: var tab } => (tab.Groups.IndexOf(group), tab.Groups.Count),
            { Data: RibbonCustomizationCommand command, Group: { } group } => (group.Commands.IndexOf(command), group.Commands.Count),
            _ => (-1, 0)
        };
    }
    private void MoveSelected(int offset)
    {
        var (index, count) = Position(); if (index < 0 || index + offset < 0 || index + offset >= count) return;
        static void Swap<T>(List<T> list, int a, int b) => (list[a], list[b]) = (list[b], list[a]);
        var selected = Selected!;
        switch (selected.Data) { case RibbonCustomizationTab: Swap(Draft.Tabs, index, index + offset); break; case RibbonCustomizationGroup: Swap(selected.Tab.Groups, index, index + offset); break; case RibbonCustomizationCommand: Swap(selected.Group!.Commands, index, index + offset); break; }
        RefreshTree(selected.Id);
    }
    private void UpdateActions()
    {
        if (_add is null) return;
        var selected = Selected;
        _add.Enabled = selected?.Group is { IsCustom: true } group && _available.SelectedItem is RibbonCommandCatalogEntry entry && !group.Commands.Any(c => c.CommandId == entry.CommandId);
        _remove.Enabled = selected?.Data is RibbonCustomizationTab { IsCustom: true } tab && Draft.Tabs.Count(t => t.Visible && !ReferenceEquals(t, tab)) > 0
            || selected?.Data is RibbonCustomizationGroup { IsCustom: true } or RibbonCustomizationCommand;
        _newGroup.Enabled = selected is not null; _edit.Enabled = selected?.Data is RibbonCustomizationTab or RibbonCustomizationGroup or RibbonCustomizationCommand;
        var (index, count) = Position(); _up.Enabled = index > 0; _down.Enabled = index >= 0 && index < count - 1;
    }
    private void EditSelected()
    {
        if (!_edit.Enabled || Selected is not { } selected) return;
        string label = selected.Data switch { RibbonCustomizationTab t => Label(t.Id, t.Label), RibbonCustomizationGroup g => Label(g.Id, g.Label), RibbonCustomizationCommand c => Label(c.CommandId, c.Label), _ => "" };
        using var dialog = new Form { Text = "Edit ribbon item", Font = Font, AutoScaleMode = AutoScaleMode.Dpi, AutoScaleDimensions = new(DeviceDpi, DeviceDpi), FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, StartPosition = FormStartPosition.CenterParent, BackColor = _ribbon.Theme.Surface, ForeColor = _ribbon.Theme.Foreground };
        var name = new TextBox { Name = "RibbonItemName", AccessibleName = "Display name", Text = label, Dock = DockStyle.Fill, MaxLength = 512 };
        var size = new ComboBox { Name = "RibbonItemSize", AccessibleName = "Command size", Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        size.Items.AddRange(Enum.GetValues<RibbonItemSize>().Cast<object>().ToArray()); size.SelectedItem = selected.Data is RibbonCustomizationCommand command ? command.Size : RibbonItemSize.Medium;
        int gap = ScaleLogical(8), height = RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density);
        dialog.ClientSize = new(Math.Max(ScaleLogical(330), Font.Height * 22), (selected.Data is RibbonCustomizationCommand ? Font.Height * 3 : Font.Height * 2) + height + gap * 8);
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(gap), ColumnCount = 1, RowCount = 4 };
        content.Controls.Add(new Label { Text = "Display name:", AutoSize = true }); content.Controls.Add(name);
        if (selected.Data is RibbonCustomizationCommand) content.Controls.Add(size);
        var ok = new RibbonOptionsAccentButton(_ribbon) { Text = "OK", Width = ScaleLogical(82), Height = height };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = ScaleLogical(82), Height = height };
        var footer = Flow(false, ok, cancel); footer.Dock = DockStyle.Bottom; footer.Padding = new(gap); dialog.Controls.Add(content); dialog.Controls.Add(footer); dialog.AcceptButton = ok; dialog.CancelButton = cancel;
        ok.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(name.Text)) dialog.DialogResult = DialogResult.OK; else name.Focus(); };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        switch (selected.Data) { case RibbonCustomizationTab t: t.Label = name.Text.Trim(); break; case RibbonCustomizationGroup g: g.Label = name.Text.Trim(); break; case RibbonCustomizationCommand c: c.Label = name.Text.Trim(); c.Size = (RibbonItemSize)size.SelectedItem!; break; }
        RefreshTree(selected.Id);
    }
    private void DrawRow(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || _available.Items[e.Index] is not RibbonCommandCatalogEntry entry) return;
        var theme = _ribbon.Theme; using var brush = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? theme.Checked.Start : theme.Surface); e.Graphics.FillRectangle(brush, e.Bounds);
        int gap = ScaleLogical(6), icon = ScaleLogical(20); var bounds = Rectangle.Inflate(e.Bounds, -gap, 0);
        DrawIcon(e.Graphics, entry.Source, new(bounds.Left, bounds.Top + (bounds.Height - icon) / 2, icon, icon)); bounds.X += icon + gap; bounds.Width = Math.Max(0, bounds.Width - icon - gap);
        TextRenderer.DrawText(e.Graphics, entry.Category + " › " + entry.Text, Font, bounds, theme.Foreground, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter); e.DrawFocusRectangle();
    }
    private void DrawIcon(Graphics graphics, RibbonElement source, Rectangle bounds)
    { if (source is RibbonGroup { Icon: { } groupIcon }) groupIcon.Draw(graphics, bounds, new(DeviceDpi, _ribbon.Theme.Foreground, true)); else if (source is RibbonItem { Icon: { } icon }) icon.Draw(graphics, bounds, new(DeviceDpi, _ribbon.Theme.Foreground, true)); else if (source is RibbonItem { Image: { } image }) graphics.DrawImage(image, bounds); }
    private void RefreshIcons()
    {
        _tree.ImageList = null; _tree.StateImageList = null; _icons.Images.Clear(); _checks.Images.Clear(); _imageIndices.Clear();
        foreach (var bitmap in _bitmaps) bitmap.Dispose(); _bitmaps.Clear();
        int side = Math.Clamp(ScaleLogical(16), 1, 256), checkSlot = Math.Clamp(ScaleLogical(_ribbon.Density == RibbonDensity.Touch ? 24 : 20), 1, 256);
        _icons.ImageSize = new(side, side); _checks.ImageSize = new(checkSlot, checkSlot);
        var empty = new Bitmap(side, side); _bitmaps.Add(empty); _icons.Images.Add(empty);
        foreach (var entry in _catalog) { var image = new Bitmap(side, side); _bitmaps.Add(image); using (var g = Graphics.FromImage(image)) DrawIcon(g, entry.Source, new(0, 0, side, side)); _imageIndices[entry.CommandId] = _icons.Images.Count; _icons.Images.Add(image); }
        foreach (bool check in new[] { false, true })
        { var image = RibbonOptionsChrome.TabCheck(_ribbon, Font, DeviceDpi, checkSlot, check); _bitmaps.Add(image); _checks.Images.Add(image); }
        _tree.ImageList = _icons; _tree.StateImageList = _checks;
    }
    private int ScaleLogical(int value) => (int)Math.Ceiling(value * DeviceDpi / 96d);
    internal Size PageMinimum { get { var minimum = RibbonQuickAccessEditorMetrics.Minimum(DeviceDpi, Font.Height, _ribbon.Density); return new(Math.Max(MinimumSize.Width, minimum.Width), minimum.Height); } }
    private void UpdateMetrics()
    {
        if (_ribbon is null || _edit is null) return;
        SuspendLayout(); int gap = ScaleLogical(8), height = RibbonQuickAccessEditorMetrics.ButtonHeight(DeviceDpi, Font.Height, _ribbon.Density);
        var minimum = RibbonQuickAccessEditorMetrics.Minimum(DeviceDpi, Font.Height, _ribbon.Density); MinimumSize = new(Math.Max(MinimumSize.Width, minimum.Width), minimum.Height);
        _available.ItemHeight = _tree.ItemHeight = RibbonQuickAccessEditorMetrics.RowHeight(DeviceDpi, Font.Height, _ribbon.Density); _tree.UpdateMetrics();
        foreach (var frame in new[] { _searchFrame, _treeSearchFrame }) { frame.Height = height; frame.Margin = new(0, gap, 0, gap); }
        foreach (var search in new[] { _search, _treeSearch }) { search.BackColor = _ribbon.Theme.Surface; search.ForeColor = _ribbon.Theme.Foreground; } LayoutSearch();
        foreach (var button in new[] { _add, _remove, _up, _down, _reset, _reload, _import, _export, _newTab, _newGroup, _edit })
        { button.MinimumSize = new(ScaleLogical(button == _up || button == _down ? 40 : button == _import || button == _export ? 28 : button == _add || button == _remove ? 86 : 70), height); button.Margin = new(gap / 2); }
        foreach (var button in new[] { _reset, _reload, _import, _export, _newTab, _newGroup, _edit }) button.Margin = new(0, gap, gap, 0);
        _import.Image = _export.Image = null; _importImage?.Dispose(); _exportImage?.Dispose();
        int iconSide = ScaleLogical(_ribbon.Density == RibbonDensity.Touch ? 24 : 20);
        _import.Image = _importImage = RibbonOptionsTransferIcons.Image(_ribbon, iconSide, true);
        _export.Image = _exportImage = RibbonOptionsTransferIcons.Image(_ribbon, iconSide, false);
        _available.BackColor = _tree.BackColor = _ribbon.Theme.Surface; _available.ForeColor = _tree.ForeColor = _ribbon.Theme.Foreground;
        foreach (var frame in new[] { _availableFrame, _treeFrame, _searchFrame, _treeSearchFrame }) frame.RefreshAppearance();
        RefreshIcons(); ResumeLayout(true); Invalidate(true);
    }
    private void LayoutSearch() { if (_treeSearchFrame is null) return; int inset = ScaleLogical(4); foreach (var (search, frame) in new[] { (_search, _searchFrame), (_treeSearch, _treeSearchFrame) }) search.SetBounds(inset, Math.Max(0, (frame.Height - search.Height) / 2), Math.Max(1, frame.Width - 2 * inset), search.Height); }
    /// <inheritdoc />
    protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); UpdateMetrics(); }
    /// <inheritdoc />
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); UpdateMetrics(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Session?.Dispose(); base.Dispose(disposing); if (disposing) { _icons.Dispose(); _checks.Dispose(); _importImage?.Dispose(); _exportImage?.Dispose(); foreach (var bitmap in _bitmaps) bitmap.Dispose(); } }
}
