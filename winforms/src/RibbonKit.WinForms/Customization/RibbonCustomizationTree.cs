using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Keep native node navigation, scrolling, selection and expansion state. Paint
// the complete row because Windows fixes state/expand glyphs at a small size,
// independently of a TreeView's enlarged touch row and state ImageList.
internal sealed class RibbonCustomizationTree : TreeView
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonCheckBoxItem _check = new();
    private TreeNode? _hot, _pressed;
    private bool _hotCheck, _pressedCheck;
    internal Action<Graphics, TreeNode, Rectangle>? PaintIcon;
    internal event Action<TreeNode>? CheckRequested;
    internal RibbonCustomizationTree(RibbonControl ribbon)
    {
        _ribbon = ribbon; DrawMode = TreeViewDrawMode.OwnerDrawAll;
        ShowLines = false; ShowPlusMinus = true; FullRowSelect = true;
        DoubleBuffered = true;
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SendMessage(Handle, 0x112C, 4, 4); // TVM_SETEXTENDEDSTYLE / TVS_EX_DOUBLEBUFFER
    }
    private int S(int value) => RibbonKit.WinForms.Layout.RibbonLayoutEngine.Scale(value, DeviceDpi);
    private bool Touch => _ribbon.Density == RibbonDensity.Touch;
    internal int CheckSide => S(Touch ? 24 : 18);
    internal int ChevronSide => S(Touch ? 16 : 12);
    internal void UpdateMetrics() { Indent = S(Touch ? 32 : 24); Invalidate(); }
    internal readonly record struct Parts(Rectangle Expand, Rectangle Check, Rectangle Icon, Rectangle Text);
    internal Parts Geometry(TreeNode node)
    {
        var row = node.Bounds;
        int left = node.Level * Indent - (IsHandleCreated ? GetScrollPos(Handle, 0) : 0);
        var expand = new Rectangle(left, row.Top, Indent, ItemHeight);
        int textLeft = Math.Max(row.Left, expand.Right + S(node.StateImageIndex >= 0 ? Touch ? 28 : 22 : 20));
        var check = node.StateImageIndex >= 0 ? Rectangle.FromLTRB(expand.Right, row.Top, textLeft, row.Top + ItemHeight) : Rectangle.Empty;
        int iconSide = S(16);
        var icon = new Rectangle(textLeft - iconSide - S(2), row.Top + (ItemHeight - iconSide) / 2, iconSide, iconSide);
        return new(expand, check, icon, new(textLeft, row.Top, Math.Max(0, ClientSize.Width - textLeft), ItemHeight));
    }
    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        if (e.Node is not { } node || _ribbon is null) return;
        var theme = _ribbon.Theme; var parts = Geometry(node);
        var row = new Rectangle(0, node.Bounds.Top, ClientSize.Width, ItemHeight);
        using (var background = new SolidBrush(node.IsSelected ? theme.Checked.Start : theme.Surface)) e.Graphics.FillRectangle(background, row);
        if (node.Nodes.Count > 0)
        {
            float cx = parts.Expand.Left + (parts.Expand.Width - 1) / 2f, cy = row.Top + (row.Height - 1) / 2f, half = ChevronSide / 2f;
            var saved = e.Graphics.Save();
            try
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(theme.SecondaryText, Math.Max(1, DeviceDpi / 96f * (Touch ? 1.8f : 1.6f))) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                e.Graphics.DrawLines(pen, node.IsExpanded
                    ? new PointF[] { new(cx - half, cy - half / 2), new(cx, cy + half / 2), new(cx + half, cy - half / 2) }
                    : new PointF[] { new(cx - half / 2, cy - half), new(cx + half / 2, cy), new(cx - half / 2, cy + half) });
            }
            finally { e.Graphics.Restore(saved); }
        }
        if (!parts.Check.IsEmpty)
        {
            int side = Math.Min(CheckSide, parts.Check.Width - S(2));
            var bounds = new Rectangle(parts.Check.Left + (parts.Check.Width - side) / 2, row.Top + (row.Height - side) / 2, side, side);
            _check.Checked = node.StateImageIndex == 1; _check.Enabled = Enabled;
            _ribbon.Painter.DrawCheckBoxIndicator(new(e.Graphics, bounds, theme, Font, DeviceDpi,
                ReferenceEquals(node, _hot) && _hotCheck, ReferenceEquals(node, _pressed) && _pressedCheck,
                Focused && node.IsSelected) { Density = _ribbon.Density }, _check);
        }
        else PaintIcon?.Invoke(e.Graphics, node, parts.Icon);
        TextRenderer.DrawText(e.Graphics, node.Text, Font, parts.Text, Enabled ? theme.Foreground : SystemColors.GrayText,
            TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (Focused && node.IsSelected && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(row, -1, -1), theme.Foreground, theme.Checked.Start);
        base.OnDrawNode(e);
    }
    private (TreeNode? Node, bool Check) Hit(Point point)
    {
        for (var node = TopNode; node is not null && node.Bounds.Top <= point.Y; node = node.NextVisibleNode)
        {
            if (point.Y >= node.Bounds.Top + ItemHeight) continue;
            var parts = Geometry(node);
            if (!parts.Check.IsEmpty && parts.Check.Contains(point)) return (node, true);
            if (node.Nodes.Count > 0 && parts.Expand.Contains(point)) return (node, false);
        }
        return (null, false);
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg is 0x201 or 0x203 && Enabled)
        {
            var point = new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16)));
            var hit = Hit(point);
            if (hit.Node is { } node)
            {
                if (m.Msg == 0x201)
                {
                    Focus(); SelectedNode = node; _pressed = hit.Check ? node : null; _pressedCheck = hit.Check;
                    if (hit.Check) CheckRequested?.Invoke(node); else node.Toggle();
                    InvalidateNode(node);
                }
                return; // Prevent the smaller native hit region from toggling again.
            }
        }
        base.WndProc(ref m);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); var hit = Hit(e.Location);
        var hot = hit.Check ? hit.Node : null;
        if (!ReferenceEquals(hot, _hot)) { var previous = _hot; _hot = hot; _hotCheck = hit.Check; InvalidateNode(previous); InvalidateNode(hot); }
    }
    private void InvalidateNode(TreeNode? node) { if (node is not null) Invalidate(new Rectangle(0, node.Bounds.Top, ClientSize.Width, ItemHeight)); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); var pressed = _pressed; _pressed = null; InvalidateNode(pressed); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); var hot = _hot; var pressed = _pressed; _hot = _pressed = null; InvalidateNode(hot); InvalidateNode(pressed); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); var pressed = _pressed; _pressed = null; InvalidateNode(pressed); }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); UpdateMetrics(); }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _check.Dispose(); }
    [DllImport("user32.dll")] private static extern int GetScrollPos(nint window, int bar);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
