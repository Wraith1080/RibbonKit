using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using RibbonKit.WinForms.Design.Protocol;
using RibbonKit.WinForms.Imaging;

namespace RibbonKit.WinForms.Design.Client;

internal sealed class RibbonIconPickerDialog : Form
{
    private readonly List<Entry> _entries = new();
    private readonly Dictionary<Entry, SvgAsset> _thumbnails = new();
    private readonly TextBox _search = new() { Name = "IconSearch", Dock = DockStyle.Fill, AccessibleName = "Search stock and project icons" };
    private readonly ListBox _list = new() { Name = "IconList", Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false, ItemHeight = 38, AccessibleName = "Stock and project icons" };
    private readonly CheckBox _foreground = new() { Name = "UseRibbonColor", Text = "Use ribbon &color", AutoSize = true };
    private readonly Panel _preview = new() { Name = "IconPreview", Dock = DockStyle.Fill, MinimumSize = new Size(140, 150), BorderStyle = BorderStyle.FixedSingle };
    private readonly Label _name = new() { AutoSize = true, Dock = DockStyle.Fill };
    private readonly Button _ok = new() { Name = "AcceptIcon", Text = "OK", AutoSize = true, MinimumSize = new Size(88, 30) };
    private SvgAsset? _selected;
    private bool _loading;
    internal RibbonDesignIcon? Selection { get; private set; }

    internal RibbonIconPickerDialog(IEnumerable<RibbonDesignIcon> projectIcons, RibbonDesignIcon current)
    {
        SuspendLayout(); Text = "Choose icon"; Name = "RibbonIconPicker"; ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent; MinimizeBox = false; MaximizeBox = false;
        Font = SystemFonts.MessageBoxFont; AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(700, 470); MinimumSize = new Size(540, 400); Padding = new Padding(12);
        foreach (var stock in StockIcons.All) _entries.Add(new("Stock · " + stock.Name, "stock:" + stock.Key, true));
        foreach (var icon in projectIcons.Where(icon => icon.Kind == "Svg" && !icon.Source.StartsWith("stock:", StringComparison.Ordinal)).GroupBy(icon => icon.Source).Select(group => group.First()))
            _entries.Add(new("Project · " + (string.IsNullOrEmpty(icon.Name) ? "Imported SVG" : icon.Name), icon.Source, icon.UseForegroundColor));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = new Padding(0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var search = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
        search.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        search.Controls.Add(new Label { Text = "&Search", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0); search.Controls.Add(_search, 1, 0);
        layout.Controls.Add(search, 0, 0); layout.SetColumnSpan(search, 2); layout.Controls.Add(_list, 0, 1);
        var details = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 3, Padding = new Padding(12, 0, 0, 0), Margin = new Padding(0) };
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize)); details.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.Controls.Add(_name, 0, 0); details.Controls.Add(_preview, 0, 1); details.Controls.Add(_foreground, 0, 2); layout.Controls.Add(details, 1, 1);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 12, 0, 0), Margin = new Padding(0), WrapContents = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(88, 30) };
        var import = new Button { Name = "ImportSvg", Text = "&Import SVG…", AutoSize = true, MinimumSize = new Size(100, 30) };
        footer.Controls.AddRange(new Control[] { cancel, _ok, import }); layout.Controls.Add(footer, 0, 2); layout.SetColumnSpan(footer, 2);
        Controls.Add(layout); AcceptButton = _ok; CancelButton = cancel;
        _search.TextChanged += (_, _) => Filter(); _list.SelectedIndexChanged += (_, _) => SelectEntry(); _list.DrawItem += DrawEntry;
        _foreground.CheckedChanged += (_, _) =>
        {
            if (!_loading && _selected != null && _list.SelectedItem is Entry entry)
            { entry.Foreground = _foreground.Checked; _thumbnails.Remove(entry); _selected = new SvgAsset(_selected.Source, _foreground.Checked); _preview.Invalidate(); _list.Invalidate(); }
        };
        _preview.Paint += (_, e) =>
        {
            if (_selected == null) return;
            int scale = Math.Max(1, DeviceDpi); int small = 16 * scale / 96, large = 48 * scale / 96;
            _selected.Draw(e.Graphics, new Rectangle((_preview.Width - large) / 2, 20 * scale / 96, large, large), SystemColors.WindowText);
            _selected.Draw(e.Graphics, new Rectangle((_preview.Width - small) / 2, 90 * scale / 96, small, small), SystemColors.WindowText);
        };
        import.Click += (_, _) =>
        {
            using var file = new OpenFileDialog { Filter = "SVG artwork (*.svg)|*.svg", CheckFileExists = true, Multiselect = false };
            if (file.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                if (new FileInfo(file.FileName).Length > SvgAsset.MaximumLength) throw new ArgumentException("SVG artwork must be at most 512 KB.");
                AddImportedSvg(File.ReadAllText(file.FileName), Path.GetFileNameWithoutExtension(file.FileName));
            }
            catch (Exception error) { MessageBox.Show(this, error.Message, "Import SVG", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        _ok.Click += (_, _) => AcceptSelection();
        Filter();
        var active = _entries.FirstOrDefault(entry => entry.Source == current.Source);
        if (active != null) { active.Foreground = current.UseForegroundColor; _list.SelectedItem = active; SelectEntry(); }
        ResumeLayout(true);
    }
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e); ConstrainWindow(DeviceDpi);
    }
    protected override void OnDpiChanged(DpiChangedEventArgs e) { base.OnDpiChanged(e); ConstrainWindow(e.DeviceDpiNew); _preview.Invalidate(); }
    private void ConstrainWindow(int dpi)
    {
        var area = Screen.FromControl(this).WorkingArea;
        MinimumSize = new Size(Math.Min(540 * dpi / 96, area.Width), Math.Min(400 * dpi / 96, area.Height));
        Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height)); _list.ItemHeight = Math.Max(Font.Height + 8, 38 * dpi / 96);
    }
    private void Filter()
    {
        var previous = _list.SelectedItem as Entry; _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var entry in _entries.Where(entry => entry.Name.IndexOf(_search.Text, StringComparison.CurrentCultureIgnoreCase) >= 0)) _list.Items.Add(entry);
            _list.SelectedItem = previous != null && _list.Items.Contains(previous) ? previous : _list.Items.Cast<Entry>().FirstOrDefault();
        }
        finally { _list.EndUpdate(); }
        SelectEntry();
    }
    private void SelectEntry()
    {
        _loading = true;
        try
        {
            if (_list.SelectedItem is Entry entry) { _selected = new SvgAsset(entry.Source, entry.Foreground); _name.Text = entry.Name; _foreground.Checked = entry.Foreground; }
            else { _selected = null; _name.Text = "No matching icons"; }
            _ok.Enabled = _foreground.Enabled = _selected != null; _preview.Invalidate();
        }
        finally { _loading = false; }
    }
    private void DrawEntry(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _list.Items.Count) return;
        var entry = (Entry)_list.Items[e.Index]; e.DrawBackground();
        if (!_thumbnails.TryGetValue(entry, out var asset)) _thumbnails.Add(entry, asset = new SvgAsset(entry.Source, entry.Foreground));
        int side = Math.Min(e.Bounds.Height - 8, 28 * DeviceDpi / 96);
        asset.Draw(e.Graphics, new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + (e.Bounds.Height - side) / 2, side, side), e.ForeColor);
        TextRenderer.DrawText(e.Graphics, entry.Name, e.Font, new Rectangle(e.Bounds.Left + side + 12, e.Bounds.Top, e.Bounds.Width - side - 16, e.Bounds.Height), e.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        e.DrawFocusRectangle();
    }
    internal void AddImportedSvg(string source, string name)
    {
        if (source.TrimStart().StartsWith("stock:", StringComparison.Ordinal)) throw new ArgumentException("Import an SVG document, not a stock identifier.");
        var validated = new SvgAsset(source, false);
        if (validated.Name == "Imported SVG")
        {
            var xml = XDocument.Parse(source); xml.Root!.AddFirst(new XElement(xml.Root.Name.Namespace + "title", name)); source = xml.ToString(SaveOptions.DisableFormatting);
        }
        var entry = new Entry("Project · " + name, source, false); _entries.Add(entry); _search.Clear(); Filter(); _list.SelectedItem = entry; SelectEntry();
    }
    internal void AcceptSelection()
    {
        if (_selected == null) return;
        Selection = new RibbonDesignIcon { Kind = "Svg", Source = _selected.Source, Name = _selected.Name, UseForegroundColor = _selected.UseForeground };
        DialogResult = DialogResult.OK;
    }
    private sealed class Entry(string name, string source, bool foreground)
    {
        internal string Name { get; } = name;
        internal string Source { get; } = source;
        internal bool Foreground { get; set; } = foreground;
        public override string ToString() => Name;
    }
}
