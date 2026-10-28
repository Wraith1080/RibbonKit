using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Retains native ComboBox selection, events and keyboard traversal. Its field
// and independently owned choice list use the same painters as ribbon editors.
internal sealed class RibbonOptionsComboBox : ComboBox, IMessageFilter
{
    private readonly RibbonControl _appearance;
    private readonly RibbonOptionsPopupHost _host;
    private readonly RibbonComboBoxItem _model = new();
    private RibbonChoicePopup? _choice;
    private int _pending;
    private bool _hot;
    internal bool ChoiceOpen => _choice?.Visible == true;
    internal RibbonOptionsComboBox(RibbonControl ribbon)
    {
        _appearance = ribbon; _host = new(ribbon);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }
    public override Size GetPreferredSize(Size proposedSize) { var size = base.GetPreferredSize(proposedSize); return new(size.Width, Math.Max(size.Height, Height)); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Parent?.PerformLayout(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        using var brush = new SolidBrush(_appearance.Theme.Surface); e.Graphics.FillRectangle(brush, ClientRectangle);
        _model.Enabled = Enabled;
        var context = new RibbonPaintContext(e.Graphics, ClientRectangle, _appearance.Theme, Font, DeviceDpi, Hot: _hot, Pressed: ChoiceOpen, Focused: Focused) { Density = _appearance.Density };
        _appearance.Painter.DrawComboBox(context, _model, ClientRectangle);
        int inset = RibbonLayoutEngine.Scale(6, DeviceDpi); var arrow = RibbonEditorLayout.ComboButton(ClientRectangle, DeviceDpi, _appearance.Density);
        var text = new Rectangle(inset, 0, Math.Max(0, arrow.Left - inset), Height);
        _appearance.Painter.DrawText(context with { Bounds = text }, GetItemText(SelectedItem) ?? "", Enabled ? _appearance.Theme.Foreground : SystemColors.GrayText,
            TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        base.OnPaint(e);
    }
    internal void OpenChoice()
    {
        if (!Enabled || Items.Count == 0 || ChoiceOpen) return;
        _host.Prepare(this); _model.Id = Name; _model.Items.Clear(); _model.Items.AddRange(Items.Cast<object>().Select(item => GetItemText(item) ?? "").ToArray()); _model.SelectedIndex = _pending = SelectedIndex;
        Application.AddMessageFilter(this);
        _choice = new(_host.Ribbon, _model, RectangleToScreen(ClientRectangle), index => _pending = index, () => CloseChoice(true, true), () => CloseChoice(false, false));
        Invalidate();
    }
    internal void CloseChoice(bool accept, bool restoreFocus)
    {
        var choice = _choice; _choice = null; Application.RemoveMessageFilter(this); choice?.Dispose();
        if (accept && _pending >= 0 && _pending < Items.Count) SelectedIndex = _pending;
        if (restoreFocus && CanFocus) Focus(); Invalidate();
    }
    public bool PreFilterMessage(ref Message m)
    {
        if (!ChoiceOpen) return false;
        if (m.Msg is 0x201 or 0x204 or 0x207)
        {
            uint position = GetMessagePos(); var point = new Point(unchecked((short)position), unchecked((short)(position >> 16)));
            if (!_choice!.ContainsPoint(point) && !RectangleToScreen(ClientRectangle).Contains(point)) CloseChoice(false, false);
            return false;
        }
        bool own = ContainsFocus || _choice!.OwnsWindow(m.HWnd);
        if (!own) return false;
        if (m.Msg is 0x100 or 0x104)
        {
            var keys = (Keys)(int)m.WParam | ModifierKeys;
            if (keys == (Keys.Alt | Keys.F4)) { var form = FindForm(); CloseChoice(false, false); form?.Close(); return true; }
            return ChoiceKey(keys);
        }
        if (m.Msg == 0x102 && !char.IsControl((char)(int)m.WParam)) { _choice!.Character((char)(int)m.WParam); return true; }
        return false;
    }
    private bool ChoiceKey(Keys keys)
    {
        if (keys is Keys.Escape) { CloseChoice(false, true); return true; }
        if (keys is Keys.Enter or Keys.F4 or (Keys.Alt | Keys.Down)) { CloseChoice(true, true); return true; }
        if (keys is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown) { _choice!.Move(keys); return true; }
        return false;
    }
    protected override bool ProcessCmdKey(ref Message m, Keys keyData)
    {
        if (ChoiceOpen && ChoiceKey(keyData)) return true;
        if (!ChoiceOpen && keyData is Keys.F4 or (Keys.Alt | Keys.Down) or Keys.Space) { OpenChoice(); return true; }
        return base.ProcessCmdKey(ref m, keyData);
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x14F) { if (m.WParam != 0) OpenChoice(); else CloseChoice(true, true); m.Result = 0; return; }
        if (m.Msg is 0x201 or 0x203) { Focus(); if (ChoiceOpen) CloseChoice(true, true); else OpenChoice(); m.Result = 0; return; }
        if (m.Msg == 0x202) { m.Result = 0; return; }
        base.WndProc(ref m);
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }
    protected override void OnSelectedIndexChanged(EventArgs e) { if (ChoiceOpen) CloseChoice(false, false); base.OnSelectedIndexChanged(e); Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (!Visible) CloseChoice(false, false); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); if (!Enabled) CloseChoice(false, false); Invalidate(); }
    protected override void OnFontChanged(EventArgs e) { CloseChoice(false, false); base.OnFontChanged(e); }
    protected override void OnDpiChangedAfterParent(EventArgs e) { CloseChoice(false, false); base.OnDpiChangedAfterParent(e); Invalidate(); }
    protected override void Dispose(bool disposing) { if (disposing) { CloseChoice(false, false); _host.Dispose(); _model.Dispose(); } base.Dispose(disposing); }
    [DllImport("user32.dll")] private static extern uint GetMessagePos();
}
