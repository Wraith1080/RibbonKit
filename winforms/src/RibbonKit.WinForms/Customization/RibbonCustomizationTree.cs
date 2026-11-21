using RibbonKit.WinForms.Layout;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Keep native node navigation, scrolling, selection and expansion state. Paint
// the complete row because Windows fixes state/expand glyphs at a small size,
// independently of a TreeView's enlarged touch row and state ImageList.
internal sealed class RibbonCustomizationTree : TreeView
{
    private readonly RibbonControl _ribbon;
    private readonly RibbonCheckBoxItem _check = new();
    private readonly RoundedPainter _painter = new();
    private TreeNode? _hot, _pressed;
    private TreeNode? _hotRow;
    private Point _pointer;
    private bool _inside;
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
    private RibbonOptionsTreeMetrics Metrics => new(_ribbon.Theme, DeviceDpi, _ribbon.Density);
    internal int CheckSide => Metrics.CheckSide;
    internal int ChevronSide => Metrics.ChevronSide;
    internal void UpdateMetrics() { Indent = Metrics.Indent; Invalidate(); }
    internal readonly record struct Parts(Rectangle Expand, Rectangle Check, Rectangle Icon, Rectangle Text);
    internal Parts Geometry(TreeNode node)
    {
        var slots = Metrics.Resolve(node.Bounds, node.Level, Indent, IsHandleCreated ? GetScrollPos(Handle, 0) : 0, ItemHeight, ClientSize.Width, node.StateImageIndex >= 0);
        return new(slots.Expand, slots.Check, slots.Icon, slots.Text);
    }
    protected override void OnDrawNode(DrawTreeNodeEventArgs e)
    {
        if (e.Node is not { } node || _ribbon is null) return;
        var parts = Geometry(node); var row = new Rectangle(0, node.Bounds.Top, ClientSize.Width, ItemHeight);
        var context = new RibbonPaintContext(e.Graphics, row, _ribbon.Theme, Font, DeviceDpi,
            ReferenceEquals(node, _hot) && _hotCheck, ReferenceEquals(node, _pressed) && _pressedCheck,
            Focused && node.IsSelected) { Density = _ribbon.Density, StateHot = ReferenceEquals(node, _hotRow), BackgroundBounds = ClientRectangle };
        RibbonOptionsTreeRenderer.Draw(_ribbon.Painter, _painter, context, new(parts.Expand, parts.Check, parts.Icon, parts.Text),
            node.Text, node.IsSelected, node.IsExpanded, node.Nodes.Count > 0, parts.Check.IsEmpty ? null : node.StateImageIndex,
            Enabled, ShowFocusCues, _check, (graphics, bounds) => PaintIcon?.Invoke(graphics, node, bounds));
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
        if (_ribbon is not null && RibbonOptionsBackground.EraseNative(this, _ribbon, ref m)) return;
        var invalid = RibbonOptionsBackground.BeforeNativePaint(this, m);
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
        if (_ribbon is not null) RibbonOptionsBackground.PaintUnusedNative(this, _ribbon, m, invalid, VisibleRows());
        if (m.Msg is 0x114 or 0x115 or 0x20A) UpdateHotRow();
        if (m.Msg is 0x100 or 0x114 or 0x115 or 0x20A or 0x1102 or 0x110B or 0x1114 && _ribbon?.Theme.OptionsAppearance?.ListBackgroundFill is not null) Invalidate();
    }
    private IEnumerable<Rectangle> VisibleRows()
    {
        for (var node = TopNode; node is not null && node.Bounds.Top < ClientSize.Height; node = node.NextVisibleNode)
            yield return new(0, node.Bounds.Top, ClientSize.Width, ItemHeight);
    }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if (_ribbon?.Theme.OptionsAppearance?.ListBackgroundFill is not null) Invalidate(); }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); _pointer = e.Location; _inside = true; UpdateHotRow(); var hit = Hit(e.Location);
        var hot = hit.Check ? hit.Node : null;
        if (!ReferenceEquals(hot, _hot)) { var previous = _hot; _hot = hot; _hotCheck = hit.Check; InvalidateNode(previous); InvalidateNode(hot); }
    }
    private void InvalidateNode(TreeNode? node) { if (node is not null) Invalidate(new Rectangle(0, node.Bounds.Top, ClientSize.Width, ItemHeight)); }
    private void UpdateHotRow()
    {
        TreeNode? next = null;
        if (Enabled && _inside && ClientRectangle.Contains(_pointer) && (_ribbon.Theme.OptionsAppearance?.TreeHoverBackground is not null || _ribbon.Theme.OptionsAppearance?.TreeStateChrome?.HasOverrides == true))
            for (var node = TopNode; node is not null && node.Bounds.Top <= _pointer.Y; node = node.NextVisibleNode)
                if (_pointer.Y < node.Bounds.Top + ItemHeight) { next = node; break; }
        if (ReferenceEquals(next, _hotRow)) return;
        var previous = _hotRow; _hotRow = next; InvalidateNode(previous); InvalidateNode(next);
    }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); var pressed = _pressed; _pressed = null; InvalidateNode(pressed); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _inside = false; UpdateHotRow(); var hot = _hot; var pressed = _pressed; _hot = _pressed = null; InvalidateNode(hot); InvalidateNode(pressed); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); if (_ribbon is not null) UpdateHotRow(); }
    protected override void OnHandleDestroyed(EventArgs e) { _inside = false; _hotRow = null; base.OnHandleDestroyed(e); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); var pressed = _pressed; _pressed = null; InvalidateNode(pressed); }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); UpdateMetrics(); }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) { _check.Dispose(); _painter.Dispose(); } }
    [DllImport("user32.dll")] private static extern int GetScrollPos(nint window, int bar);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
