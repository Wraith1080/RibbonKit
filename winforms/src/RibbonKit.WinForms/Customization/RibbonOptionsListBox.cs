using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Paint-only hover state over native list targets. Selection, HWND, scrolling,
// keyboard, accessibility and the owner's draft remain with ListBox.
internal sealed class RibbonOptionsListBox : ListBox
{
    private RibbonControl? _ribbon;
    private bool _navigation, _inside;
    private Point _pointer;
    internal int HotIndex { get; private set; } = -1;
    internal Rectangle FullBackgroundBounds(DrawItemEventArgs e)
    {
        var row = GetItemRectangle(e.Index);
        return new(new Point(e.Bounds.Left - row.Left, e.Bounds.Top - row.Top), ClientSize);
    }
    internal void BindTheme(RibbonControl ribbon, bool navigation = false) { _ribbon = ribbon; _navigation = navigation; }
    private bool TracksHot => _ribbon?.Theme.OptionsAppearance is { } a && (_navigation
        ? a.NavigationHoverBackground is not null || a.NavigationStateChrome?.HasOverrides == true
        : a.RowHoverBackground is not null || a.RowStateChrome?.HasOverrides == true);
    private void UpdateHot()
    {
        int index = Enabled && TracksHot && _inside && ClientRectangle.Contains(_pointer) ? IndexFromPoint(_pointer) : -1;
        if (index >= Items.Count || index >= 0 && !GetItemRectangle(index).Contains(_pointer)) index = -1;
        if (index == HotIndex) return;
        int previous = HotIndex; HotIndex = index;
        InvalidateRow(previous); InvalidateRow(index);
    }
    private void InvalidateRow(int index)
    {
        if (!IsHandleCreated || index < 0 || index >= Items.Count) return;
        var bounds = GetItemRectangle(index);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var row = new RowRect { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Bottom };
        // The owner paints the complete row. A native erase before DRAWITEM
        // exposes a blank between the previous and next hover frames.
        InvalidateRect(Handle, ref row, false);
        OnInvalidated(new InvalidateEventArgs(bounds));
    }
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (!TracksHot || DrawMode == DrawMode.Normal || e.Index < 0 || e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
        { base.OnDrawItem(e); return; }
        // Native selection/focus callbacks also enter here, not just WM_PAINT.
        // Publish the finished background/rim/icon/text/focus row in one blit.
        using var buffer = BufferedGraphicsManager.Current.Allocate(e.Graphics, e.Bounds);
        // Like the ribbon choice popup, use local bounds so GDI text and the
        // focus rectangle share the same origin as the background and icon.
        var graphics = buffer.Graphics;
        graphics.ResetTransform();
        graphics.TextRenderingHint = e.Graphics.TextRenderingHint;
        graphics.Clear(BackColor);
        using var row = new DrawItemEventArgs(graphics, e.Font, new Rectangle(Point.Empty, e.Bounds.Size),
            e.Index, e.State, e.ForeColor, e.BackColor);
        base.OnDrawItem(row);
        buffer.Render(e.Graphics);
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); _inside = true; _pointer = e.Location; UpdateHot(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _inside = false; UpdateHot(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); UpdateHot(); }
    protected override void OnHandleDestroyed(EventArgs e) { HotIndex = -1; _inside = false; base.OnHandleDestroyed(e); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); if (_ribbon?.Theme.OptionsAppearance?.ListBackgroundFill is not null) Invalidate(); }
    protected override void WndProc(ref Message m)
    {
        bool reset = m.Msg is 0x180 or 0x181 or 0x182 or 0x184; // LB_ADD/INSERT/DELETESTRING/RESETCONTENT
        if (reset) { HotIndex = -1; _inside = false; }
        if (!_navigation && _ribbon is not null && RibbonOptionsBackground.EraseNative(this, _ribbon, ref m)) return;
        var invalid = RibbonOptionsBackground.BeforeNativePaint(this, m);
        base.WndProc(ref m);
        if (!_navigation && _ribbon is not null) RibbonOptionsBackground.PaintUnusedNative(this, _ribbon, m, invalid, VisibleRows());
        if (m.Msg is 0x114 or 0x115 or 0x20A or 0x197) UpdateHot(); // scrolling/TopIndex
        // Native selection/key navigation can scroll via pixel copies too.
        if (m.Msg is 0x100 or 0x114 or 0x115 or 0x20A or 0x186 or 0x197 or 0x19E && !_navigation && _ribbon?.Theme.OptionsAppearance?.ListBackgroundFill is not null) Invalidate();
    }
    private IEnumerable<Rectangle> VisibleRows()
    {
        if (Items.Count == 0) yield break;
        for (int index = TopIndex; index < Items.Count; index++)
        {
            var bounds = GetItemRectangle(index);
            if (bounds.Top >= ClientSize.Height) yield break;
            yield return bounds;
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct RowRect { internal int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool InvalidateRect(nint window, ref RowRect rectangle, bool erase);
}
