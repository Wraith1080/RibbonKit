using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design.Client;

// Shares the protocol property adapter and icon picker; no application code runs here.
internal sealed class RibbonApplicationMenuDialog : Form
{
    private readonly RibbonBackstageSnapshot _draft;
    private readonly Func<RibbonBackstageSnapshot, string?> _commit;
    private readonly Func<RibbonBackstageSnapshot, RibbonBackstagePreview> _render;
    private readonly TreeView _tree = new() { Name = "ApplicationMenuTree", Dock = DockStyle.Fill, HideSelection = false, AccessibleName = "Application Menu sections and commands" };
    private readonly PropertyGrid _properties = new() { Name = "ApplicationMenuProperties", Dock = DockStyle.Fill };
    private readonly Panel _preview = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly PictureBox _picture = new() { Name = "ApplicationMenuPreview", SizeMode = PictureBoxSizeMode.Normal };
    private readonly Label _status = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly ToolStripDropDownButton _add = new("Addâ€¦");
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 200 };
    private RibbonBackstagePreview? _frame;
    private bool _showing;
    internal RibbonApplicationMenuDialog(RibbonBackstageSnapshot draft, Func<RibbonBackstageSnapshot, string?> commit, Func<RibbonBackstageSnapshot, RibbonBackstagePreview> render)
    {
        _draft = draft; _commit = commit; _render = render;
        Text = "Edit Application Menu"; Name = "RibbonApplicationMenuDialog"; StartPosition = FormStartPosition.CenterParent;
        Font = SystemFonts.MessageBoxFont; AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1000, 760); MinimumSize = new Size(720, 540); ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { AutoSize = true, Dock = DockStyle.Fill, Text = "File commands â†’ right panes â†’ footer. Select a dropdown to edit its pane; design native bodies separately as UserControls. Changes apply on OK." }, 0, 0);
        var tools = new ToolStrip { Dock = DockStyle.Fill, GripStyle = ToolStripGripStyle.Hidden };
        tools.Items.Add(_add);
        void Tool(string text, Action action) { var button = new ToolStripButton(text); button.Click += (_, _) => action(); tools.Items.Add(button); }
        Tool("Move up", () => MoveNode(-1)); Tool("Move down", () => MoveNode(1)); Tool("Remove", Remove);
        Tool("Choose iconâ€¦", () => { if (Selected is { Kind: not "Section" } node) { using var picker = new RibbonIconPickerDialog(All.Select(n => n.Icon), node.Icon); if (picker.ShowDialog(this) == DialogResult.OK && picker.Selection is { } icon) { node.Icon = icon; QueuePreview(); } } });
        Tool("Clear icon", () => { if (Selected is { Kind: not "Section" } node) { node.Icon = new RibbonDesignIcon { HasRasterImage = node.Icon.HasRasterImage }; QueuePreview(); } });
        layout.Controls.Add(tools, 0, 1);
        var split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(970, 650), SplitterDistance = 280 };
        split.Panel1.Controls.Add(_tree);
        var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); details.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); details.RowStyles.Add(new RowStyle(SizeType.AutoSize)); details.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        details.Controls.Add(_properties, 0, 0); details.Controls.Add(_status, 0, 1); details.Controls.Add(_preview, 0, 2); _preview.Controls.Add(_picture);
        split.Panel2.Controls.Add(details); layout.Controls.Add(split, 0, 2);
        var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var ok = new Button { Text = "OK", AutoSize = true };
        footer.Controls.AddRange(new Control[] { cancel, ok }); layout.Controls.Add(footer, 0, 3); Controls.Add(layout); AcceptButton = ok; CancelButton = cancel;
        _tree.AfterSelect += (_, _) => SelectionChanged();
        _tree.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) { Remove(); e.SuppressKeyPress = true; } else if (e.Control && e.KeyCode is Keys.Up or Keys.Down) { MoveNode(e.KeyCode == Keys.Up ? -1 : 1); e.SuppressKeyPress = true; } };
        _properties.PropertyValueChanged += (_, _) => { if (_tree.SelectedNode is { } tree && Selected is { } node) tree.Text = Label(node); QueuePreview(); };
        _picture.MouseClick += (_, e) => { var hit = _frame?.Regions.FirstOrDefault(r => new Rectangle(r.X, r.Y, r.Width, r.Height).Contains(e.Location)); if (hit is not null) SelectKey(hit.Key); };
        _timer.Tick += (_, _) => { _timer.Stop(); RenderPreview(); };
        _preview.SizeChanged += (_, _) => QueuePreview();
        ok.Click += (_, _) => { _properties.Refresh(); var error = _commit(_draft); if (error is null) { DialogResult = DialogResult.OK; Close(); } else MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); };
        Rebuild(draft.SelectionKey);
    }
    private IEnumerable<RibbonDesignNode> All => RibbonDesignSnapshot.Flatten(_draft.Entries);
    private RibbonDesignNode? Selected => _tree.SelectedNode?.Tag as RibbonDesignNode;
    private static string Label(RibbonDesignNode node) => node.Kind == "Section" ? node.Key == "file" ? "File commands (left)" : node.Key == "default" ? "Default right pane" : "Footer actions"
        : (node.Kind == "Separator" ? "Section separator" : node.Caption) + " [" + node.Kind + "]";
    private void Rebuild(string key)
    {
        _showing = true; _tree.BeginUpdate();
        try
        {
            TreeNode Map(RibbonDesignNode node) { var tree = new TreeNode(Label(node)) { Tag = node }; foreach (var child in node.Children) tree.Nodes.Add(Map(child)); return tree; }
            _tree.Nodes.Clear(); var root = new TreeNode("Application Menu"); _tree.Nodes.Add(root);
            foreach (var section in _draft.Entries) root.Nodes.Add(Map(section)); _tree.ExpandAll(); SelectKey(key); _tree.SelectedNode ??= root;
        }
        finally { _tree.EndUpdate(); _showing = false; }
        SelectionChanged();
    }
    private void SelectKey(string key)
    {
        IEnumerable<TreeNode> Walk(TreeNodeCollection nodes) { foreach (TreeNode node in nodes) { yield return node; foreach (var child in Walk(node.Nodes)) yield return child; } }
        var tree = Walk(_tree.Nodes).FirstOrDefault(n => (n.Tag as RibbonDesignNode)?.Key == key); if (tree is not null) _tree.SelectedNode = tree;
    }
    private void SelectionChanged()
    {
        if (_showing) return;
        var node = Selected; _draft.SelectionKey = node?.Key ?? "";
        _properties.SelectedObject = new RibbonEditorDialog.DraftProperties(node ?? new RibbonDesignNode { Kind = "Settings", ComponentName = "Application Menu", Properties = _draft.Settings }, named: node is not null && node.Kind != "Section");
        _add.DropDownItems.Clear();
        string kind = node?.Kind == "Section" || node is null ? "DropDown" : node.Kind;
        foreach (string child in RibbonDesignSnapshot.ChildKinds(kind))
        { string addKind = child; var action = new ToolStripMenuItem(child == "DropDown" ? "Pane / dropdown" : child == "Separator" ? "Section separator" : child); action.Click += (_, _) => Add(addKind); _add.DropDownItems.Add(action); }
        _add.Enabled = _add.DropDownItems.Count != 0; QueuePreview();
    }
    private void Add(string kind)
    {
        var parent = Selected ?? _draft.Entries[0];
        if (parent.Kind != "Section" && !RibbonDesignSnapshot.ChildKinds(parent.Kind).Contains(kind)) return;
        var template = _draft.Templates.Single(t => t.Kind == kind);
        var node = RibbonDesignSnapshot.Deserialize(new RibbonDesignSnapshot { Tabs = { template } }.Serialize()).Tabs[0];
        string stem = "application" + kind, name; int suffix = 1;
        var names = _draft.ReservedNames.Concat(All.Select(n => n.ComponentName)).ToArray(); do { name = stem + suffix++; } while (names.Contains(name, StringComparer.OrdinalIgnoreCase));
        node.Key = "new:" + Guid.NewGuid().ToString("N"); node.ComponentName = name;
        node.Properties.Single(p => p.Name == "Id").Value = "application:" + Guid.NewGuid().ToString("N");
        var text = node.Properties.FirstOrDefault(p => p.Name == "Text"); if (text is not null) text.Value = kind == "DropDown" ? "New pane" : "New " + kind;
        parent.Children.Add(node); Rebuild(node.Key);
    }
    private List<RibbonDesignNode>? Siblings(RibbonDesignNode node) => All.FirstOrDefault(n => n.Children.Contains(node))?.Children;
    private void MoveNode(int direction)
    {
        if (Selected is not { Kind: not "Section" } node || Siblings(node) is not { } items) return;
        int index = items.IndexOf(node), next = index + direction; if (next < 0 || next >= items.Count) return;
        items.RemoveAt(index); items.Insert(next, node); Rebuild(node.Key);
    }
    private void Remove() { if (Selected is { Kind: not "Section" } node && Siblings(node) is { } items) { items.Remove(node); Rebuild(""); } }
    private void QueuePreview() { if (IsDisposed) return; _timer.Stop(); _timer.Start(); }
    private void RenderPreview()
    {
        try
        {
            _draft.PreviewWidth = Math.Max(320, _preview.ClientSize.Width); _draft.PreviewHeight = Math.Max(300, _preview.ClientSize.Height);
            _draft.PreviewDpi = DeviceDpi; _frame = _render(_draft);
            using var stream = new MemoryStream(Convert.FromBase64String(_frame.Png)); using var decoded = Image.FromStream(stream);
            var old = _picture.Image; _picture.Image = new Bitmap(decoded); _picture.Size = _picture.Image.Size; old?.Dispose();
            _status.Text = "Inert preview: clicks select draft entries. Native UserControls are placeholders.";
        }
        catch (Exception error) { _status.Text = error.Message; }
    }
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e); var area = Screen.FromControl(this).WorkingArea; MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width), Math.Min(MinimumSize.Height, area.Height));
        Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height)); QueuePreview();
    }
    protected override void Dispose(bool disposing) { if (disposing) { _timer.Dispose(); _picture.Image?.Dispose(); } base.Dispose(disposing); }
}
