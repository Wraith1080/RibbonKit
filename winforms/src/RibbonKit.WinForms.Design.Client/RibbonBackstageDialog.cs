using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design.Client;

internal sealed class RibbonBackstageDialog : Form
{
    private readonly RibbonBackstageSnapshot _draft;
    private readonly Func<RibbonBackstageSnapshot, string?> _commit;
    private readonly Func<RibbonBackstageSnapshot, RibbonBackstagePreview> _render;
    private readonly IContainer _components = new Container();
    private readonly TreeView _tree = new() { Name = "BackstageTree", Dock = DockStyle.Fill, HideSelection = false, AccessibleName = "Backstage pages and commands" };
    private readonly TreeNode _root = new("Backstage") { Name = "BackstageRoot" };
    private readonly TreeNode _main = new("Main entries") { Name = "MainEntries" }, _bottom = new("Bottom entries") { Name = "BottomEntries" };
    private readonly PropertyGrid _properties = new() { Name = "BackstageProperties", Dock = DockStyle.Fill, HelpVisible = true };
    private readonly Label _content = new() { Name = "PageContentInfo", AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 6) };
    private readonly Label _previewStatus = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Panel _previewPanel = new() { Name = "NavigationPreview", Dock = DockStyle.Fill, AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };
    private readonly PictureBox _picture = new() { Name = "BackstagePreviewImage", Location = Point.Empty, SizeMode = PictureBoxSizeMode.Normal };
    private readonly ToolStrip _tools = new() { Name = "BackstageTools", Dock = DockStyle.Fill, GripStyle = ToolStripGripStyle.Hidden, CanOverflow = true };
    private readonly ToolStripButton _up = new("Move &up") { Name = "MoveUp" }, _down = new("Move &down") { Name = "MoveDown" }, _remove = new("&Remove") { Name = "Remove" };
    private readonly SplitContainer _split = new() { Dock = DockStyle.Fill, Size = new Size(930, 560), SplitterDistance = 250, FixedPanel = FixedPanel.Panel1 };
    private readonly System.Windows.Forms.Timer _timer;
    private RibbonBackstagePreview? _frame;
    private bool _showing;
    internal RibbonBackstageDialog(RibbonBackstageSnapshot draft, Func<RibbonBackstageSnapshot, string?> commit, Func<RibbonBackstageSnapshot, RibbonBackstagePreview> render)
    {
        _draft = draft; _commit = commit; _render = render; _timer = new System.Windows.Forms.Timer(_components) { Interval = 200 };
        SuspendLayout(); Text = "Edit Backstage"; Name = "RibbonBackstageDialog"; StartPosition = FormStartPosition.CenterParent;
        Font = SystemFonts.MessageBoxFont; AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(960, 740); MinimumSize = new Size(800, 600); Padding = new Padding(12); ShowInTaskbar = false; MinimizeBox = false; MaximizeBox = false;
        var layout = Column(); layout.RowCount = 4;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "Arrange Backstage pages and commands. Design native page bodies separately as UserControls.", AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        var add = new ToolStripDropDownButton("&Add") { Name = "Add" }; ((ToolStripDropDownMenu)add.DropDown).ShowImageMargin = false;
        add.DropDownItems.Add(Action("&Page", "AddPage", () => Add("Page"))); add.DropDownItems.Add(Action("&Command", "AddCommand", () => Add("Button")));
        add.DropDownItems.Add(Action("&Separator", "AddSeparator", () => Add("Separator")));
        _tools.Items.AddRange(new ToolStripItem[] { add, new ToolStripSeparator(), _up, _down, new ToolStripSeparator(), _remove });
        layout.Controls.Add(_tools, 0, 1); layout.Controls.Add(_split, 0, 2); _split.Panel1.Padding = new Padding(0, 6, 6, 0); _split.Panel1.Controls.Add(_tree);
        var details = Column(); details.Padding = new Padding(6, 6, 0, 0); details.RowCount = 5;
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize)); details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); details.RowStyles.Add(new RowStyle(SizeType.AutoSize)); details.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        var icons = new FlowLayoutPanel { Name = "BackstageIconActions", AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        var choose = new Button { Name = "ChooseBackstageIcon", Text = "Choose &icon…", AutoSize = true };
        var clear = new Button { Name = "ClearBackstageIcon", Text = "Clear vector icon", AutoSize = true };
        icons.Controls.AddRange(new Control[] { choose, clear }); details.Controls.Add(icons, 0, 0); details.Controls.Add(_content, 0, 1);
        details.Controls.Add(_properties, 0, 2); details.Controls.Add(_previewStatus, 0, 3); details.Controls.Add(_previewPanel, 0, 4);
        _previewPanel.Controls.Add(_picture); _split.Panel2.Controls.Add(details);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
        var cancel = new Button { Text = "Cancel", Name = "CancelBackstage", AutoSize = true, MinimumSize = new Size(88, 30), DialogResult = DialogResult.Cancel };
        var ok = new Button { Text = "OK", Name = "AcceptBackstage", AutoSize = true, MinimumSize = new Size(88, 30) };
        footer.Controls.Add(cancel); footer.Controls.Add(ok); layout.Controls.Add(footer, 0, 3); Controls.Add(layout); AcceptButton = ok; CancelButton = cancel;
        _tree.AfterSelect += (_, _) => ShowSelection();
        _tree.NodeMouseClick += (_, e) => { if (e.Button == MouseButtons.Right) _tree.SelectedNode = e.Node; };
        _tree.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && e.Modifiers == Keys.None) { Remove(); e.SuppressKeyPress = true; }
            else if (e.Control && e.KeyCode is Keys.Up or Keys.Down) { MoveNode(e.KeyCode == Keys.Up ? -1 : 1); e.SuppressKeyPress = true; }
        };
        var menu = new ContextMenuStrip(_components) { ShowImageMargin = false };
        menu.Items.Add(Action("Add page", "AddPage", () => Add("Page"))); menu.Items.Add(Action("Add command", "AddCommand", () => Add("Button")));
        menu.Items.Add(Action("Add separator", "AddSeparator", () => Add("Separator")));
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(Action("Move up", "MoveUp", () => MoveNode(-1))); menu.Items.Add(Action("Move down", "MoveDown", () => MoveNode(1))); menu.Items.Add(Action("Remove", "Remove", Remove));
        menu.Opening += (_, _) => { menu.Items["MoveUp"]!.Enabled = _up.Enabled; menu.Items["MoveDown"]!.Enabled = _down.Enabled; menu.Items["Remove"]!.Enabled = _remove.Enabled; }; _tree.ContextMenuStrip = menu;
        _up.Click += (_, _) => MoveNode(-1); _down.Click += (_, _) => MoveNode(1); _remove.Click += (_, _) => Remove(); ok.Click += (_, _) => AcceptDraft();
        choose.Click += (_, _) =>
        {
            if (_tree.SelectedNode?.Tag is not RibbonDesignNode node) return;
            using var picker = new RibbonIconPickerDialog(_draft.Entries.Select(entry => entry.Icon), node.Icon);
            if (picker.ShowDialog(this) == DialogResult.OK && picker.Selection is { } icon) SetIcon(icon);
        };
        clear.Click += (_, _) => SetIcon(new RibbonDesignIcon());
        _properties.PropertyValueChanged += (_, e) =>
        {
            var node = _tree.SelectedNode?.Tag as RibbonDesignNode;
            if (node is not null && e.ChangedItem?.PropertyDescriptor?.Name == "Placement") { _draft.Entries.Remove(node); _draft.Entries.Add(node); RebuildTree(node.Key); return; }
            if (node is not null) _tree.SelectedNode!.Text = Label(node);
            _content.Text = ContentInfo(node); QueuePreview();
        };
        _picture.MouseClick += (_, e) => SelectPreview(e.Location);
        _timer.Tick += (_, _) => { _timer.Stop(); RefreshPreview(); };
        _previewPanel.SizeChanged += (_, _) => QueuePreview();
        _tree.Nodes.Add(_root); _root.Nodes.AddRange(new[] { _main, _bottom }); RebuildTree(draft.SelectionKey);
        ShowSelection(); ResumeLayout(true);
    }
    private static TableLayoutPanel Column() => new() { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0), ColumnStyles = { new ColumnStyle(SizeType.Percent, 100) } };
    private static ToolStripMenuItem Action(string text, string name, Action action) { var item = new ToolStripMenuItem(text) { Name = name }; item.Click += (_, _) => action(); return item; }
    private static string Label(RibbonDesignNode node) => (node.Kind == "Separator" ? "Separator" : node.Caption) + "  [" + (node.Kind == "Page" ? "Page" : node.Kind == "Separator" ? "Separator" : "Command") + "]"
        + (node.Properties.Single(property => property.Name == "Visible").Value == "False" ? " (hidden)" : "");
    protected override void OnLoad(EventArgs e) { base.OnLoad(e); FitToScreen(); QueuePreview(); }
    protected override void OnDpiChanged(DpiChangedEventArgs e) { base.OnDpiChanged(e); FitToScreen(); QueuePreview(); }
    private void FitToScreen()
    {
        var area = Screen.FromControl(this).WorkingArea; double scale = DeviceDpi / 96d;
        MinimumSize = new Size(Math.Min((int)(800 * scale), area.Width), Math.Min((int)(600 * scale), area.Height));
        Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
    }
    private void ShowSelection()
    {
        if (_showing) return; _showing = true;
        try
        {
            var node = _tree.SelectedNode?.Tag as RibbonDesignNode; _draft.SelectionKey = node?.Key ?? "";
            bool section = ReferenceEquals(_tree.SelectedNode, _main) || ReferenceEquals(_tree.SelectedNode, _bottom);
            _properties.SelectedObject = section ? null : new RibbonEditorDialog.DraftProperties(node ?? new RibbonDesignNode { Kind = "Backstage", ComponentName = "Backstage", Properties = _draft.Settings });
            _content.Text = section ? _tree.SelectedNode!.Text + " — use Add to create a page, command or separator." : ContentInfo(node);
            _split.Panel2.Controls.Find("BackstageIconActions", true).Single().Visible = node is not null && node.Kind != "Separator";
            _up.Enabled = node is not null && _tree.SelectedNode!.Index > 0;
            _down.Enabled = node is not null && _tree.SelectedNode!.Index < _tree.SelectedNode.Parent!.Nodes.Count - 1; _remove.Enabled = node is not null;
            QueuePreview();
        }
        finally { _showing = false; }
    }
    private string ContentInfo(RibbonDesignNode? node) => node?.Kind == "Page" ? "UserControl: " + node.Properties.Single(property => property.Name == "ContentType").Value
        + "\r\nChoose ContentType below; edit its class in the UserControl designer.\r\nBuild the project to refresh available classes."
        + (_draft.ContentNames.TryGetValue(node.Key, out var name) && name != "No content assigned" ? "\r\nCurrent instance: " + name : "")
        : node is null ? "File routing changes only when you edit these settings. Changes apply on OK." : node.Kind == "Separator" ? "Decorative separator — no activation, keyboard focus or KeyTips." : "Command — no page content. Clicks in this editor only select.";
    private static bool IsBottom(RibbonDesignNode node) => node.Properties.Single(property => property.Name == "Placement").Value == "Bottom";
    private IEnumerable<TreeNode> EntryNodes => _main.Nodes.Cast<TreeNode>().Concat(_bottom.Nodes.Cast<TreeNode>());
    private void RebuildTree(string key)
    {
        _showing = true; _tree.BeginUpdate();
        try
        {
            _draft.Entries = _draft.Entries.OrderBy(IsBottom).ToList(); _main.Nodes.Clear(); _bottom.Nodes.Clear();
            foreach (var node in _draft.Entries) (IsBottom(node) ? _bottom : _main).Nodes.Add(new TreeNode(Label(node)) { Tag = node });
            _root.Expand(); _main.Expand(); _bottom.Expand();
            _tree.SelectedNode = EntryNodes.FirstOrDefault(tree => ((RibbonDesignNode)tree.Tag!).Key == key) ?? _root;
        }
        finally { _tree.EndUpdate(); _showing = false; }
        ShowSelection();
    }
    internal void SetIcon(RibbonDesignIcon icon)
    {
        if (_tree.SelectedNode?.Tag is not RibbonDesignNode node || node.Kind == "Separator") return;
        icon.HasRasterImage = node.Icon.HasRasterImage; node.Icon = icon; QueuePreview();
    }
    private void Add(string kind)
    {
        var template = _draft.Templates.Single(node => node.Kind == kind);
        var clone = RibbonDesignSnapshot.Deserialize(new RibbonDesignSnapshot { Tabs = { template } }.Serialize()).Tabs[0];
        string stem = kind == "Page" ? "backstagePage" : kind == "Separator" ? "backstageSeparator" : "backstageCommand", name; int suffix = 1;
        var names = _draft.ReservedNames.Concat(_draft.Entries.Select(node => node.ComponentName)).ToArray();
        do { name = stem + suffix++; } while (names.Contains(name, StringComparer.OrdinalIgnoreCase));
        clone.Key = "new:" + Guid.NewGuid().ToString("N"); clone.ComponentName = name;
        clone.Properties.Single(property => property.Name == "Id").Value = "rk:" + Guid.NewGuid().ToString("N");
        if (kind != "Separator") clone.Properties.Single(property => property.Name == "Text").Value = kind == "Page" ? "New page" : "New command";
        clone.Properties.Single(property => property.Name == "Placement").Value = ReferenceEquals(_tree.SelectedNode, _bottom) || ReferenceEquals(_tree.SelectedNode?.Parent, _bottom) ? "Bottom" : "Main";
        _draft.Entries.Add(clone); RebuildTree(clone.Key);
    }
    private void MoveNode(int offset)
    {
        if (_tree.SelectedNode?.Tag is not RibbonDesignNode node) return; var tree = _tree.SelectedNode; var parent = tree.Parent!; int to = tree.Index + offset;
        if (to < 0 || to >= parent.Nodes.Count) return;
        int target = _draft.Entries.IndexOf((RibbonDesignNode)parent.Nodes[to].Tag!);
        _draft.Entries.Remove(node); _draft.Entries.Insert(target, node); tree.Remove(); parent.Nodes.Insert(to, tree); _tree.SelectedNode = tree; ShowSelection();
    }
    private void Remove()
    {
        if (_tree.SelectedNode?.Tag is not RibbonDesignNode node) return; int index = _tree.SelectedNode.Index; var parent = _tree.SelectedNode.Parent!;
        _draft.Entries.Remove(node); _tree.SelectedNode.Remove();
        _tree.SelectedNode = _draft.Entries.Count == 0 ? _root : parent.Nodes.Count == 0 ? parent : parent.Nodes[Math.Min(index, parent.Nodes.Count - 1)]; ShowSelection();
    }
    private void QueuePreview() { if (IsDisposed || Disposing) return; _timer.Stop(); _timer.Start(); }
    internal void RefreshPreview()
    {
        try
        {
            _draft.PreviewWidth = Math.Max(320, _previewPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            _draft.PreviewHeight = Math.Max(180, _previewPanel.ClientSize.Height); _draft.PreviewDpi = DeviceDpi;
            var frame = _render(_draft);
            using var stream = new MemoryStream(Convert.FromBase64String(frame.Png)); using var source = Image.FromStream(stream);
            var image = new Bitmap(source); var old = _picture.Image; _picture.Image = image; _picture.Size = image.Size; old?.Dispose(); _frame = frame;
            _previewStatus.Text = "Solid navigation preview — click an entry to select it. Native page body is a placeholder.";
        }
        catch (Exception error)
        {
            _frame = null; var old = _picture.Image; _picture.Image = null; old?.Dispose();
            _previewStatus.Text = "Preview unavailable: " + error.Message;
        }
    }
    internal void SelectPreview(Point point)
    {
        var region = _frame?.Regions.FirstOrDefault(region => new Rectangle(region.X, region.Y, region.Width, region.Height).Contains(point));
        if (region is not null) _tree.SelectedNode = EntryNodes.FirstOrDefault(tree => ((RibbonDesignNode)tree.Tag!).Key == region.Key) ?? _root;
    }
    internal void AcceptDraft()
    {
        try
        {
            if (!ValidateChildren()) return; var error = _commit(_draft);
            if (error is not null) { MessageBox.Show(this, error, "Edit Backstage", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            DialogResult = DialogResult.OK;
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Edit Backstage", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _components.Dispose(); _picture.Image?.Dispose(); _picture.Image = null; }
        base.Dispose(disposing);
    }
}
