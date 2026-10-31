using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design.Client;

internal sealed class RibbonEditorDialog : Form
{
    private readonly RibbonDesignSnapshot _draft;
    private readonly Func<RibbonDesignSnapshot, string?> _commit;
    private readonly TreeView _tree = new() { Dock = DockStyle.Fill, HideSelection = false, AccessibleName = "Ribbon structure" };
    private readonly TreeNode _root = new("Ribbon") { Name = "RibbonRoot" };
    private readonly PropertyGrid _properties = new() { Dock = DockStyle.Fill, HelpVisible = true, AccessibleName = "Selected component properties" };
    private readonly IContainer _components = new Container();
    private readonly ToolStrip _tools = new() { Name = "StructureTools", Dock = DockStyle.Fill, GripStyle = ToolStripGripStyle.Hidden, CanOverflow = true, Padding = new Padding(0, 4, 0, 4) };
    private readonly ToolStripDropDownButton _add = new("&Add") { Name = "Add", DisplayStyle = ToolStripItemDisplayStyle.Text };
    private readonly ToolStripButton _up = new("Move &up") { Name = "MoveUp", DisplayStyle = ToolStripItemDisplayStyle.Text };
    private readonly ToolStripButton _down = new("Move &down") { Name = "MoveDown", DisplayStyle = ToolStripItemDisplayStyle.Text };
    private readonly ToolStripButton _remove = new("&Remove") { Name = "Remove", DisplayStyle = ToolStripItemDisplayStyle.Text };
    private readonly SplitContainer _split = new() { Name = "StructureSplit", Dock = DockStyle.Fill, Size = new Size(936, 480), SplitterDistance = 270, FixedPanel = FixedPanel.Panel1 };
    private readonly Label _selection = new() { Name = "SelectionCaption", AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 6) };
    private readonly GroupBox _tabHeader = new() { Name = "TabHeader", Text = "Tab header", AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(8), Margin = new Padding(0, 0, 0, 8) };
    private readonly CheckBox _contextual = new() { Name = "ContextualTab", Text = "&Contextual tab", AutoSize = true };
    private readonly Button _color = new() { Name = "HeaderColor", Text = "Header &color…", AutoSize = true };
    private readonly Button _resetColor = new() { Name = "UseThemeAccent", Text = "Use theme &accent", AutoSize = true };
    private readonly Panel _swatch = new() { Name = "HeaderColorSwatch", Size = new Size(22, 22), BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(4) };
    private readonly Label _colorValue = new() { Name = "HeaderColorValue", AutoSize = true, Dock = DockStyle.Fill };
    private readonly TextBox _choices = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true, AccessibleName = "ComboBox choices, one per line" };
    private readonly GroupBox _choiceBox = new() { Text = "Choices (one per line)", Dock = DockStyle.Fill, Height = 130, Padding = new Padding(8), Margin = new Padding(0, 8, 0, 0) };
    private readonly ContextMenuStrip _contextMenu;
    private bool _showing, _sizing;
    private int _metricsDpi = 96;

    internal RibbonEditorDialog(RibbonDesignSnapshot draft, Func<RibbonDesignSnapshot, string?> commit)
    {
        SuspendLayout();
        _draft = draft; _commit = commit; _contextMenu = new ContextMenuStrip(_components) { ShowImageMargin = false };
        Text = "Edit Ribbon"; Name = "RibbonEditorDialog"; StartPosition = FormStartPosition.CenterParent;
        Font = SystemFonts.MessageBoxFont; AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(780, 520); ClientSize = new Size(960, 640); MinimizeBox = false; MaximizeBox = false;
        ShowInTaskbar = false; Padding = new Padding(12);
        var layout = ColumnLayout(); layout.RowCount = 4;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "Arrange tabs, groups and commands.", AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        ((ToolStripDropDownMenu)_add.DropDown).ShowImageMargin = false;
        _tools.Items.AddRange(new ToolStripItem[] { _add, new ToolStripSeparator(), _up, _down, new ToolStripSeparator(), _remove });
        layout.Controls.Add(_tools, 0, 1); layout.Controls.Add(_split, 0, 2);
        var structure = ColumnLayout(); structure.RowCount = 2;
        structure.RowStyles.Add(new RowStyle(SizeType.AutoSize)); structure.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        structure.Padding = new Padding(0, 6, 6, 0);
        structure.Controls.Add(new Label { Text = "Ribbon structure", AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 0, 0, 6) }, 0, 0);
        _tree.Name = "StructureTree"; _tree.ContextMenuStrip = _contextMenu; structure.Controls.Add(_tree, 0, 1); _split.Panel1.Controls.Add(structure);
        var details = ColumnLayout(); details.RowCount = 4; details.Padding = new Padding(6, 6, 0, 0);
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize)); details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.Controls.Add(_selection, 0, 0);
        var header = ColumnLayout(); header.AutoSize = true; header.Dock = DockStyle.Top; header.RowCount = 3;
        header.Controls.Add(_contextual, 0, 0);
        var headerActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0) };
        headerActions.Controls.AddRange(new Control[] { _color, _resetColor, _swatch });
        header.Controls.Add(headerActions, 0, 1); header.Controls.Add(_colorValue, 0, 2); _tabHeader.Controls.Add(header);
        details.Controls.Add(_tabHeader, 0, 1); _properties.Name = "ComponentProperties"; details.Controls.Add(_properties, 0, 2);
        _choiceBox.Controls.Add(_choices); details.Controls.Add(_choiceBox, 0, 3); _split.Panel2.Controls.Add(details);
        var footer = new TableLayoutPanel { Name = "DialogFooter", ColumnCount = 3, RowCount = 1, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), Margin = new Padding(0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var ok = new Button { Name = "AcceptDraft", Text = "OK", AutoSize = true, MinimumSize = new Size(88, 30), Padding = new Padding(8, 2, 8, 2), Margin = new Padding(8, 0, 0, 0) };
        var cancel = new Button { Name = "CancelDraft", Text = "Cancel", AutoSize = true, MinimumSize = new Size(88, 30), Padding = new Padding(8, 2, 8, 2), Margin = new Padding(8, 0, 0, 0), DialogResult = DialogResult.Cancel };
        footer.Controls.Add(new Label { Text = "Changes apply when you choose OK.", AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        footer.Controls.Add(ok, 1, 0); footer.Controls.Add(cancel, 2, 0); AcceptButton = ok; CancelButton = cancel;
        layout.Controls.Add(footer, 0, 3); Controls.Add(layout);
        _up.Click += (_, _) => MoveNode(-1); _down.Click += (_, _) => MoveNode(1); _remove.Click += (_, _) => Remove(); ok.Click += (_, _) => AcceptDraft();
        _tree.AfterSelect += (_, _) => ShowSelection();
        _tree.NodeMouseClick += (_, e) => { if (e.Button == MouseButtons.Right) _tree.SelectedNode = e.Node; };
        _tree.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && e.Modifiers == Keys.None) { Remove(); e.SuppressKeyPress = true; }
            else if (e.Control && e.KeyCode is Keys.Up or Keys.Down) { MoveNode(e.KeyCode == Keys.Up ? -1 : 1); e.SuppressKeyPress = true; }
        };
        _properties.PropertyValueChanged += (_, _) => RefreshSelection();
        _contextual.CheckedChanged += (_, _) =>
        {
            if (!_showing && _tree.SelectedNode?.Tag is RibbonDesignNode node)
            { node.Properties.Single(property => property.Name == "IsContextual").Value = _contextual.Checked ? "True" : "False"; RefreshSelection(); }
        };
        _color.Click += (_, _) =>
        {
            if (_tree.SelectedNode?.Tag is not RibbonDesignNode node) return;
            var current = HeaderColor(node);
            using var picker = new ColorDialog { Color = current.IsEmpty ? SystemColors.Highlight : current, FullOpen = true, AnyColor = true };
            if (picker.ShowDialog(this) == DialogResult.OK) SetHeaderColor(picker.Color);
        };
        _resetColor.Click += (_, _) => SetHeaderColor(Color.Empty);
        _choices.TextChanged += (_, _) =>
        {
            if (!_showing && _tree.SelectedNode?.Tag is RibbonDesignNode node && node.Kind == "ComboBox")
                node.Choices = _choices.Text.Length == 0 ? new List<string>() : _choices.Text.Replace("\r", "").Split('\n').ToList();
        };
        _split.SizeChanged += (_, _) => UpdateSplitterConstraints();
        _tree.Nodes.Add(_root);
        foreach (var tab in draft.Tabs) _root.Nodes.Add(CreateTree(tab));
        _tree.ExpandAll(); _tree.SelectedNode = _root; ShowSelection();
        ResumeLayout(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _components.Dispose();
        base.Dispose(disposing);
    }
    private static TableLayoutPanel ColumnLayout() => new() { ColumnCount = 1, Dock = DockStyle.Fill, Margin = new Padding(0), ColumnStyles = { new ColumnStyle(SizeType.Percent, 100) } };
    protected override void OnLoad(EventArgs e) { base.OnLoad(e); ApplyDpiConstraints(DeviceDpi); }
    protected override void OnDpiChanged(DpiChangedEventArgs e) { base.OnDpiChanged(e); ApplyDpiConstraints(e.DeviceDpiNew); }
    private void ApplyDpiConstraints(int dpi)
    {
        _metricsDpi = dpi;
        var area = Screen.FromControl(this).WorkingArea;
        MinimumSize = new Size(Math.Min(ScaleMetric(780), area.Width), Math.Min(ScaleMetric(520), area.Height));
        Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
        _split.SplitterWidth = ScaleMetric(5); UpdateSplitterConstraints();
    }
    private int ScaleMetric(int value) => (int)Math.Round(value * _metricsDpi / 96d);
    private void UpdateSplitterConstraints()
    {
        if (_sizing || _split.Width < 40) return;
        _sizing = true;
        try
        {
            int available = _split.ClientSize.Width - _split.SplitterWidth;
            int left = Math.Min(ScaleMetric(210), available / 3), right = Math.Min(ScaleMetric(320), available - left);
            int distance = Math.Max(left, Math.Min(_split.SplitterDistance, available - right));
            _split.Panel1MinSize = 0; _split.Panel2MinSize = 0;
            _split.Panel1MinSize = left; _split.Panel2MinSize = right; _split.SplitterDistance = distance;
        }
        finally { _sizing = false; }
    }
    private static string KindLabel(string kind) => kind switch { "CheckBox" => "Check box", "RadioButton" => "Radio button", "DropDown" => "Drop-down", "SplitButton" => "Split button", "TextBox" => "Text box", "ComboBox" => "Combo box", "GalleryTile" => "Gallery tile", _ => kind };
    private static bool IsContextual(RibbonDesignNode node) => node.Kind == "Tab" && node.Properties.Single(property => property.Name == "IsContextual").Value == "True";
    private static string Label(RibbonDesignNode node) => (string.IsNullOrWhiteSpace(node.Caption) ? node.ComponentName : node.Caption) + "  [" + (IsContextual(node) ? "Contextual tab" : KindLabel(node.Kind)) + "]";
    private static TreeNode CreateTree(RibbonDesignNode node)
    {
        var tree = new TreeNode(Label(node)) { Tag = node };
        foreach (var child in node.Children) tree.Nodes.Add(CreateTree(child)); return tree;
    }
    private TreeNode? ParentFor(string kind)
    {
        for (var parent = _tree.SelectedNode; parent != null; parent = parent.Parent)
            if (parent.Tag is RibbonDesignNode owner && RibbonDesignSnapshot.ChildKinds(owner.Kind).Contains(kind)) return parent;
        return null;
    }
    private List<RibbonDesignNode> Children(TreeNode? parent) => parent?.Tag is RibbonDesignNode node ? node.Children : _draft.Tabs;
    private void ShowSelection()
    {
        var node = _tree.SelectedNode?.Tag as RibbonDesignNode;
        _showing = true;
        try
        {
            _properties.SelectedObject = ReferenceEquals(_tree.SelectedNode, _root) ? new RibbonRootProperties(_draft) : node == null ? null : new DraftProperties(node);
            _choiceBox.Visible = node?.Kind == "ComboBox"; _choices.Text = node == null ? "" : string.Join(Environment.NewLine, node.Choices);
        }
        finally { _showing = false; }
        RefreshSelection();
    }
    private void RefreshSelection()
    {
        var node = _tree.SelectedNode?.Tag as RibbonDesignNode;
        _showing = true;
        try
        {
            _selection.Text = ReferenceEquals(_tree.SelectedNode, _root) ? "Ribbon — root" : node == null ? "Properties — select a component" : node.Caption + " — " + KindLabel(node.Kind);
            _tabHeader.Visible = node?.Kind == "Tab";
            if (node?.Kind == "Tab")
            {
                _contextual.Checked = IsContextual(node); var color = HeaderColor(node);
                _color.Enabled = _resetColor.Enabled = _contextual.Checked;
                _swatch.BackColor = color.IsEmpty ? SystemColors.Control : color;
                _swatch.Visible = _contextual.Checked && !color.IsEmpty;
                _colorValue.Text = (color.IsEmpty ? "Theme accent" : TypeDescriptor.GetConverter(typeof(Color)).ConvertToInvariantString(color)) + " · header text and indicator";
                _colorValue.Enabled = _contextual.Checked;
            }
            if (_tree.SelectedNode is { Tag: RibbonDesignNode selectedNode } selected) selected.Text = Label(selectedNode);
            _properties.Refresh();
            _remove.Enabled = node != null;
            _up.Enabled = node != null && _tree.SelectedNode is { Index: > 0 };
            _down.Enabled = node != null && _tree.SelectedNode is { } current && current.Index < Children(current.Parent).Count - 1;
            PopulateAddMenu(_add.DropDownItems); ClearMenu(_contextMenu.Items);
            var add = new ToolStripMenuItem("Add") { Name = "Add" }; ((ToolStripDropDownMenu)add.DropDown).ShowImageMargin = false;
            PopulateAddMenu(add.DropDownItems); _contextMenu.Items.Add(add); _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(MenuAction("Move up", "MoveUp", () => MoveNode(-1), _up.Enabled));
            _contextMenu.Items.Add(MenuAction("Move down", "MoveDown", () => MoveNode(1), _down.Enabled));
            _contextMenu.Items.Add(new ToolStripSeparator()); _contextMenu.Items.Add(MenuAction("Remove", "Remove", Remove, _remove.Enabled));
        }
        finally { _showing = false; }
    }
    private static void ClearMenu(ToolStripItemCollection items) { while (items.Count != 0) { var item = items[0]; items.RemoveAt(0); item.Dispose(); } }
    private static ToolStripMenuItem MenuAction(string text, string name, Action action, bool enabled = true)
    {
        var item = new ToolStripMenuItem(text) { Name = name, Enabled = enabled }; item.Click += (_, _) => action(); return item;
    }
    private void PopulateAddMenu(ToolStripItemCollection items)
    {
        ClearMenu(items);
        items.Add(MenuAction("&Tab", "AddTab", () => Add("Tab")));
        items.Add(MenuAction("&Contextual tab", "AddContextualTab", () => Add("Tab", true)));
        items.Add(MenuAction("&Group", "AddGroup", () => Add("Group"), ParentFor("Group") != null));
        items.Add(new ToolStripSeparator());
        var kinds = RibbonDesignSnapshot.ChildKinds("Group").Concat(new[] { "Separator", "GalleryTile" }).Where(kind => ParentFor(kind) != null).ToArray();
        foreach (var kind in kinds) items.Add(MenuAction(KindLabel(kind), "Add" + kind, () => Add(kind)));
        if (kinds.Length == 0) items.Add(new ToolStripMenuItem("Select a group to add commands") { Enabled = false });
    }
    private static Color HeaderColor(RibbonDesignNode node) => (Color)TypeDescriptor.GetConverter(typeof(Color)).ConvertFromInvariantString(node.Properties.Single(property => property.Name == "ContextualColor").Value)!;
    internal void SetHeaderColor(Color color)
    {
        if (_tree.SelectedNode?.Tag is not RibbonDesignNode { Kind: "Tab" } node) return;
        node.Properties.Single(property => property.Name == "ContextualColor").Value = TypeDescriptor.GetConverter(typeof(Color)).ConvertToInvariantString(color) ?? "";
        RefreshSelection();
    }
    private void Add(string kind, bool contextual = false)
    {
        var parent = kind == "Tab" ? _root : ParentFor(kind); if (parent == null) return;
        var template = _draft.Templates.Single(node => node.Kind == kind);
        var usedNames = new HashSet<string>(_draft.ReservedNames.Concat(_draft.Nodes.Select(node => node.ComponentName)), StringComparer.OrdinalIgnoreCase);
        string name; int suffix = 1; do { name = "ribbon" + (contextual ? "ContextualTab" : kind) + suffix++; } while (usedNames.Contains(name));
        var created = new RibbonDesignNode { Key = "new:" + Guid.NewGuid().ToString("N"), Kind = kind, ComponentName = name,
            Properties = template.Properties.Select(property => new RibbonDesignProperty { Name = property.Name, Category = property.Category, Description = property.Description, Type = property.Type, Value = property.Value, Options = property.Options.ToArray() }).ToList(),
            Choices = template.Choices.ToList() };
        created.Properties.Single(property => property.Name == "Id").Value = "rk:" + Guid.NewGuid().ToString("N");
        var caption = created.Properties.FirstOrDefault(property => property.Name == "Text"); if (caption != null) caption.Value = contextual ? "Contextual tab" : "New " + KindLabel(kind).ToLowerInvariant();
        if (contextual) created.Properties.Single(property => property.Name == "IsContextual").Value = "True";
        Children(parent).Add(created); var tree = CreateTree(created); parent.Nodes.Add(tree);
        parent.Expand(); _tree.SelectedNode = tree;
    }
    private void Remove()
    {
        if (_tree.SelectedNode is not { } selected || ReferenceEquals(selected, _root)) return;
        var parent = selected.Parent; int index = selected.Index; var siblings = Children(parent); siblings.RemoveAt(index); selected.Remove();
        var trees = parent?.Nodes ?? _tree.Nodes;
        _tree.SelectedNode = trees.Count == 0 ? parent : trees[Math.Min(index, trees.Count - 1)]; ShowSelection();
    }
    private void MoveNode(int offset)
    {
        if (_tree.SelectedNode is not { } selected || ReferenceEquals(selected, _root)) return;
        var siblings = Children(selected.Parent); int from = selected.Index, to = from + offset;
        if (to < 0 || to >= siblings.Count) return;
        var trees = selected.Parent?.Nodes ?? _tree.Nodes; var model = siblings[from]; siblings.RemoveAt(from); siblings.Insert(to, model);
        selected.Remove(); trees.Insert(to, selected); _tree.SelectedNode = selected;
    }
    internal void AcceptDraft()
    {
        try
        {
            if (!ValidateChildren()) return;
            _draft.Validate(); var error = _commit(_draft);
            if (error != null) { MessageBox.Show(this, error, "Edit Ribbon", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            DialogResult = DialogResult.OK;
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Edit Ribbon", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private sealed class RibbonRootProperties(RibbonDesignSnapshot draft)
    {
        [Category("Structure"), Description("The number of authored tabs in this ribbon."), ReadOnly(true)]
        public int Tabs => draft.Tabs.Count;
    }
    private sealed class DraftProperties(RibbonDesignNode node) : CustomTypeDescriptor
    {
        public override string GetClassName() => node.Kind;
        public override string GetComponentName() => node.ComponentName;
        public override PropertyDescriptorCollection GetProperties() => GetProperties(null);
        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => new(node.Properties.Where(property => node.Kind != "Tab" || property.Name is not ("IsContextual" or "ContextualColor")).Select(property => (PropertyDescriptor)new DraftProperty(property)).ToArray());
        public override object GetPropertyOwner(PropertyDescriptor? propertyDescriptor) => this;
    }
    private sealed class DraftProperty : PropertyDescriptor
    {
        private readonly RibbonDesignProperty _property;
        internal DraftProperty(RibbonDesignProperty property) : base(property.Name, new Attribute[] { new CategoryAttribute(property.Category), new DescriptionAttribute(property.Description) }) => _property = property;
        public override Type ComponentType => typeof(DraftProperties);
        public override Type PropertyType => _property.Type switch { "Boolean" => typeof(bool), "Int32" => typeof(int), "Single" => typeof(float), "Color" => typeof(Color), _ => typeof(string) };
        public override bool IsReadOnly => false;
        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component) { }
        public override bool ShouldSerializeValue(object component) => true;
        public override TypeConverter Converter => _property.Options.Length == 0 ? TypeDescriptor.GetConverter(PropertyType)
            : _property.Type == "RibbonSizePolicy" ? new SizePolicyChoiceConverter(_property.Options) : new ChoiceConverter(_property.Options);
        public override object? GetValue(object? component) => PropertyType == typeof(string) ? _property.Value : Converter.ConvertFromInvariantString(_property.Value);
        public override void SetValue(object? component, object? value)
        {
            _property.Value = _property.Type == "RibbonSizePolicy" ? (string)Converter.ConvertFromInvariantString(value?.ToString() ?? "")!
                : PropertyType == typeof(string) ? value?.ToString() ?? "" : Converter.ConvertToInvariantString(value) ?? "";
            OnValueChanged(component, EventArgs.Empty);
        }
    }
    private class ChoiceConverter(string[] choices) : StringConverter
    {
        protected string[] Choices { get; } = choices;
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(Choices);
    }
    private sealed class SizePolicyChoiceConverter(string[] choices) : ChoiceConverter(choices)
    {
        private static string Caption(string value)
        {
            var sizes = value.Split(',').Select(size => size.Trim()).ToArray();
            return sizes.Distinct(StringComparer.Ordinal).Count() == 1 ? "Fixed " + sizes[0] : string.Join(" → ", sizes);
        }
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
            destinationType == typeof(string) && value is string policy ? Caption(policy) : base.ConvertTo(context, culture, value, destinationType);
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string text)
                return Choices.FirstOrDefault(choice => choice == text || Caption(choice) == text)
                    ?? throw new FormatException("Select a size policy from the list.");
            return base.ConvertFrom(context, culture, value);
        }
    }
}
