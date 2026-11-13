using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Both layout editors share a detached group name/icon/layout editor.
internal sealed class RibbonCustomizationGroupDialog : Form
{
    private readonly TextBox _name;
    private readonly IconPicker _icons;
    private readonly RibbonOptionsComboBox _layout;
    private readonly RibbonCustomizationGroup _group;
    internal RibbonCustomizationGroupDialog(RibbonControl ribbon, Control owner, RibbonCustomizationGroup group, string displayName, bool creating)
    {
        _group = group; Text = creating ? "Create Group" : "Edit Group"; Name = "RibbonGroupDialog";
        Font = owner.Font; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new(owner.DeviceDpi, owner.DeviceDpi);
        StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MinimizeBox = MaximizeBox = false;
        BackColor = ribbon.Theme.Surface; ForeColor = ribbon.Theme.Foreground;
        int S(int value) => (int)Math.Ceiling(value * owner.DeviceDpi / 96d);
        int gap = S(8), height = RibbonQuickAccessEditorMetrics.ButtonHeight(owner.DeviceDpi, Font.Height, ribbon.Density);
        ClientSize = new(S(ribbon.Density == RibbonDensity.Touch ? 540 : 460), S(ribbon.Density == RibbonDensity.Touch ? 440 : 370));
        _name = new() { Name = "RibbonGroupName", AccessibleName = "Group name", Text = displayName, MaxLength = 512, BorderStyle = BorderStyle.None };
        var nameFrame = new RibbonOptionsFrame(ribbon, summedTextPadding: true) { Height = height, Dock = DockStyle.Top, Margin = new(0, 0, 0, gap) };
        nameFrame.Controls.Add(_name); nameFrame.Height = nameFrame.RequiredTextHeight(height); nameFrame.Layout += (_, _) => _name.Bounds = nameFrame.TextBounds(_name.PreferredHeight);
        _icons = new(ribbon) { Name = "RibbonGroupIcons", AccessibleName = "Group icon from ribbon commands", Dock = DockStyle.Fill };
        _icons.Populate(group.IconCommandId); var iconFrame = new RibbonOptionsFrame(ribbon) { Dock = DockStyle.Fill, Margin = new(0, 0, 0, gap) }; iconFrame.Controls.Add(_icons);
        _layout = new(ribbon) { Name = "RibbonGroupLayout", AccessibleName = "Group layout", Dock = DockStyle.Top, AutoSize = true, DropDownStyle = ComboBoxStyle.DropDownList, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = Math.Max(Font.Height + S(4), height - S(6)), Margin = Padding.Empty };
        _layout.Items.AddRange(Enum.GetValues<RibbonCustomizationGroupLayout>().Cast<object>().ToArray()); _layout.SelectedItem = group.Layout;
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new(S(12)), Margin = Padding.Empty };
        for (int i = 0; i < 6; i++) body.RowStyles.Add(new(i == 3 ? SizeType.Percent : SizeType.AutoSize, i == 3 ? 100 : 0));
        Label Label(string text) => new() { Text = text, AutoSize = true, Margin = new(0, 0, 0, S(4)) };
        body.Controls.Add(Label("Name:")); body.Controls.Add(nameFrame); body.Controls.Add(Label("Icon (from this ribbon's commands):")); body.Controls.Add(iconFrame); body.Controls.Add(Label("Group layout:")); body.Controls.Add(_layout);
        var ok = new RibbonOptionsAccentButton(ribbon) { Name = "AcceptGroup", Text = "OK", Size = new(S(86), height) };
        var cancel = new RibbonOptionsButton(ribbon) { Name = "CancelGroup", Text = "Cancel", Size = ok.Size, DialogResult = DialogResult.Cancel, CausesValidation = false };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new(S(12)), WrapContents = false }; footer.Controls.Add(cancel); footer.Controls.Add(ok);
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_name.Text)) { _name.Focus(); return; }
            _group.Label = _name.Text.Trim(); _group.IconCommandId = _icons.SelectedIcon; _group.Layout = (RibbonCustomizationGroupLayout)_layout.SelectedItem!;
            foreach (var command in _group.Commands) command.Size = RibbonCustomizationSerializer.GroupSize(_group.Layout, command.Size);
            DialogResult = DialogResult.OK;
        };
        Controls.Add(body); Controls.Add(footer); AcceptButton = ok; CancelButton = cancel;
    }
    private sealed class IconPicker : ListView
    {
        private readonly RibbonControl _ribbon;
        private readonly ImageList _placeholder = new() { ColorDepth = ColorDepth.Depth32Bit };
        private readonly RoundedPainter _painter = new();
        private readonly Bitmap _blank;
        private readonly Dictionary<string, IRibbonIcon> _artwork = [];
        private bool _updatingMetrics;
        private int _cellSide, _nativeImageMargin = -1;
        internal IconPicker(RibbonControl ribbon)
        {
            _ribbon = ribbon; View = View.LargeIcon; OwnerDraw = true; MultiSelect = false; HideSelection = false; BorderStyle = BorderStyle.None; ShowItemToolTips = true;
            BackColor = ribbon.Theme.Surface; ForeColor = ribbon.Theme.Foreground;
            int side = S(28); _placeholder.ImageSize = new(side, side); _blank = new Bitmap(side, side); _placeholder.Images.Add(_blank); LargeImageList = _placeholder;
        }
        private int S(int value) => (int)Math.Ceiling(value * DeviceDpi / 96d);
        internal string? SelectedIcon => SelectedItems.Count == 0 ? null : SelectedItems[0].Tag as string;
        internal void Populate(string? selected)
        {
            Items.Add(new ListViewItem("None", 0));
            var seen = new HashSet<IRibbonIcon>();
            foreach (var entry in _ribbon.CommandCatalog.GetAvailable(RibbonCustomizationTarget.Ribbon))
                if (RibbonCustomizationIcons.Icon(entry.Source) is { } icon && (seen.Add(icon) || entry.CommandId == selected)) { _artwork[entry.CommandId] = icon; Items.Add(new ListViewItem(entry.Text, 0) { Tag = entry.CommandId, ToolTipText = entry.Category + " › " + entry.Text, Name = entry.CommandId }); }
            (Items.Cast<ListViewItem>().FirstOrDefault(i => Equals(i.Tag, selected)) ?? Items[0]).Selected = true;
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Hide native label layout while retaining item names/tooltips. Long
            // labels must not enlarge the owner-drawn selection or hit rectangles.
            SendMessage(Handle, 0x1036, 0x30000, 0x30000); // LVS_EX_DOUBLEBUFFER | LVS_EX_HIDELABELS
            _cellSide = 0; _nativeImageMargin = -1; RefreshMetrics();
        }
        private Layout.RibbonOptionsIconPickerMetrics Metrics => new(_ribbon.Theme, DeviceDpi, _ribbon.Density);
        private void RefreshMetrics()
        {
            if (_ribbon is null || _placeholder is null || _updatingMetrics || !IsHandleCreated || Items.Count == 0) return;
            _updatingMetrics = true;
            try
            {
                if (_nativeImageMargin < 0) _nativeImageMargin = Math.Max(0, Items[0].GetBounds(ItemBoundsPortion.Icon).Height - _placeholder.ImageSize.Height);
                int side = Metrics.CellSide(Font.Height);
                if (_cellSide == side) return;
                var selected = SelectedItems.Cast<ListViewItem>().FirstOrDefault();
                int imageSide = Math.Clamp(side - _nativeImageMargin, 1, 256);
                _placeholder.ImageSize = new(imageSide, imageSide);
                SendMessage(Handle, 0x1035, 0, (nint)((side << 16) | side));
                _cellSide = side; ArrangeIcons(ListViewAlignment.SnapToGrid); selected?.EnsureVisible(); Invalidate();
            }
            finally { _updatingMetrics = false; }
        }
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); RefreshMetrics(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); _cellSide = 0; _nativeImageMargin = -1; RefreshMetrics(); }
        protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); _cellSide = 0; _nativeImageMargin = -1; RefreshMetrics(); }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg is 0xF or 0x317 or 0x318) RefreshMetrics(); // Prepare layout before native paint/print, never inside DrawItem.
            base.WndProc(ref m);
        }
        protected override void OnDrawItem(DrawListViewItemEventArgs e)
        {
            var tile = Metrics.TileBounds(e.Item.GetBounds(ItemBoundsPortion.Icon), _cellSide);
            IRibbonIcon? icon = e.Item.Tag is string id ? _artwork.GetValueOrDefault(id) : null;
            RibbonOptionsIconPickerRenderer.Draw(_painter, new(e.Graphics, tile, _ribbon.Theme, Font, DeviceDpi) { Density = _ribbon.Density }, icon, e.Item.Selected, e.Item.Selected ? RibbonOptionsChrome.Radius(_ribbon, DeviceDpi) : 0);
            if (Focused && e.Item.Selected) new DrawListViewItemEventArgs(e.Graphics, e.Item, tile, e.ItemIndex, e.State).DrawFocusRectangle();
        }
        protected override void Dispose(bool disposing) { if (disposing) { LargeImageList = null; _placeholder.Dispose(); _blank.Dispose(); _painter.Dispose(); } base.Dispose(disposing); }
        [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    }
}
