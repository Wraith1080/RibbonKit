using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design.Client;

internal sealed partial class RibbonEditorDialog
{
    private readonly ToolStripButton _previewPopup = new("Preview &popup…") { Name = "PreviewPopup", DisplayStyle = ToolStripItemDisplayStyle.Text };
    private readonly SplitContainer _popupSplit = new() { Name = "PopupPreviewSplit", Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
        Size = new Size(550, 360), SplitterDistance = 150, Panel2Collapsed = true };
    private readonly PictureBox _popupPicture = new() { Name = "PopupPreviewImage", SizeMode = PictureBoxSizeMode.AutoSize, AccessibleName = "Static popup preview" };
    private readonly Label _popupCaption = new() { AutoSize = true, Margin = new Padding(6), Text = "Popup preview" };
    private readonly Button _popupBack = new() { Name = "PopupPreviewBack", Text = "Parent menu", AutoSize = true };
    private readonly Button _popupClose = new() { Name = "PopupPreviewClose", Text = "Close preview", AutoSize = true };
    private readonly Label _popupHelp = new() { Name = "PopupPreviewHelp", Text = "Click a row to edit its properties. Click an arrow to preview a submenu.",
        Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(4) };
    private RibbonPopupPreview? _popupPreview;
    private string _popupDraftStamp = "";
    private bool _renderingPopup;

    private Control InitializePopupPreview()
    {
        _popupSplit.Panel1.Controls.Add(_properties);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        actions.Controls.AddRange(new Control[] { _popupCaption, _popupBack, _popupClose });
        var viewport = new Panel { Name = "PopupPreviewViewport", Dock = DockStyle.Fill, AutoScroll = true, BackColor = SystemColors.AppWorkspace };
        viewport.Controls.Add(_popupPicture);
        _popupSplit.Panel2.Controls.Add(viewport);
        _popupSplit.Panel2.Controls.Add(_popupHelp);
        _popupSplit.Panel2.Controls.Add(actions);
        _previewPopup.Click += (_, _) => TryPreviewPopup();
        _popupClose.Click += (_, _) => ClosePopupPreview();
        _popupBack.Click += (_, _) => { try { PreviewParentPopup(); } catch (Exception error) { PopupPreviewFailed(error); } };
        _popupPicture.MouseClick += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            try { SelectPopupPreviewAt(e.Location); } catch (Exception error) { PopupPreviewFailed(error); }
        };
        _popupPicture.Paint += (_, e) =>
        {
            if (_popupPreview is null) return;
            using var pen = new Pen(SystemColors.Highlight, 2) { DashStyle = DashStyle.Dash };
            foreach (var hit in _popupPreview.Hits.Where(hit => !hit.OpensSubmenu && hit.Key == _draft.SelectionKey))
                e.Graphics.DrawRectangle(pen, hit.X + 1, hit.Y + 1, Math.Max(0, hit.Width - 3), Math.Max(0, hit.Height - 3));
        };
        return _popupSplit;
    }
    private RibbonDesignNode? PopupOwnerForSelection()
    {
        var node = SelectedModel();
        if (node is not null && node.Kind == "QatMenuReference")
            node = _draft.CommandNodes.FirstOrDefault(candidate => candidate.Key == node.SourceKey);
        while (node is not null)
        {
            if (node.Kind is "DropDown" or "SplitButton") return node;
            var current = node;
            node = _draft.CommandNodes.FirstOrDefault(parent => parent.Children.Contains(current));
        }
        return null;
    }
    private void UpdatePopupPreviewSelection()
    {
        var owner = PopupOwnerForSelection();
        _previewPopup.Enabled = _render is not null && owner is not null;
        if (_popupPreview is null || _renderingPopup) return;
        if (owner is null) { ClosePopupPreview(); return; }
        if (_popupPreview.OwnerKey != owner.Key && !_popupPreview.Hits.Any(hit => hit.Key == _draft.SelectionKey))
        {
            try { RenderPopupPreview(owner.Key); }
            catch (Exception error) { PopupPreviewFailed(error); }
        }
        else if (PopupDraftStamp(_popupPreview.OwnerKey) != _popupDraftStamp) RefreshPopupPreview();
        else _popupPicture.Invalidate();
    }
    private string PopupDraftStamp(string ownerKey)
    {
        var request = RibbonDesignSnapshot.Deserialize(_draft.Serialize()); request.SelectionKey = ownerKey;
        return request.Serialize();
    }
    private void TryPreviewPopup()
    {
        try { if (PopupOwnerForSelection() is { } owner) RenderPopupPreview(owner.Key); }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Popup preview", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    internal void RenderPopupPreview(string ownerKey)
    {
        if (_render is null) throw new InvalidOperationException("Popup preview is unavailable.");
        if (!_draft.CommandNodes.Any(node => node.Key == ownerKey && node.Kind is "DropDown" or "SplitButton"))
            throw new ArgumentException("Select a dropdown or split button from this ribbon.");
        _renderingPopup = true;
        try
        {
            // Selection is transient. Render a copy for this menu while keeping
            // the original draft's selected row and Properties object intact.
            var request = RibbonDesignSnapshot.Deserialize(_draft.Serialize()); request.SelectionKey = ownerKey;
            string stamp = request.Serialize();
            var result = _render(request).Popup ?? throw new InvalidOperationException("The designer did not return a popup preview.");
            using var bytes = new MemoryStream(Convert.FromBase64String(result.Png));
            using var decoded = Image.FromStream(bytes);
            var image = new Bitmap(decoded); var previous = _popupPicture.Image;
            _popupPicture.Image = image; previous?.Dispose();
            _popupPreview = result; _popupDraftStamp = stamp; _popupCaption.Text = result.Caption + " — popup";
            _popupHelp.Text = "Click a row to edit its properties. Click an arrow to preview a submenu.";
            _popupBack.Enabled = result.ParentKey.Length > 0;
            _popupSplit.Panel2Collapsed = false;
            _popupPicture.Invalidate();
        }
        finally { _renderingPopup = false; }
    }
    internal bool SelectPopupPreviewAt(Point point)
    {
        var hit = _popupPreview?.Hits.LastOrDefault(hit => new Rectangle(hit.X, hit.Y, hit.Width, hit.Height).Contains(point));
        if (hit is null) return false;
        var tree = Descendants(_root).FirstOrDefault(candidate => candidate.Tag is RibbonDesignNode node && node.Key == hit.Key);
        if (tree is null) return false;
        _tree.SelectedNode = tree; tree.EnsureVisible();
        if (hit.OpensSubmenu) RenderPopupPreview(hit.Key);
        return true;
    }
    internal void PreviewParentPopup()
    {
        if (_popupPreview?.ParentKey is { Length: > 0 } parent) RenderPopupPreview(parent);
    }
    private void RefreshPopupPreview()
    {
        if (_popupPreview is null || _renderingPopup) return;
        if (!_draft.CommandNodes.Any(node => node.Key == _popupPreview.OwnerKey)) { ClosePopupPreview(); return; }
        try { RenderPopupPreview(_popupPreview.OwnerKey); }
        catch (Exception error) { PopupPreviewFailed(error); }
    }
    private void PopupPreviewFailed(Exception error)
    {
        ClosePopupPreview(); _popupHelp.Text = "Preview unavailable: " + error.Message;
        _popupSplit.Panel2Collapsed = false;
    }
    internal void ClosePopupPreview()
    {
        _popupPreview = null; _popupDraftStamp = ""; var image = _popupPicture.Image; _popupPicture.Image = null; image?.Dispose();
        _popupSplit.Panel2Collapsed = true;
    }
}
