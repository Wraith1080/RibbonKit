namespace RibbonKit.WinForms;

internal static class RibbonOptionsChrome
{
    internal static int Radius(RibbonControl ribbon, int dpi) => Math.Min(
        ribbon.Painter.GetPopupCornerRadius(ribbon.Theme, dpi),
        (int)Math.Round(ribbon.Theme.CornerRadius * dpi / 96d));

    internal static void Surface(RoundedPainter painter, Graphics graphics, Rectangle bounds, RibbonControl ribbon,
        int dpi, RibbonFill fill, Color border, int stroke = 1)
        => painter.Draw(graphics, bounds, Radius(ribbon, dpi), fill, border, stroke);
}

// Keep native text editing, list selection, scrolling and accessibility. The
// wrapper owns only the frame; a rounded child region protects its corner joins.
internal sealed class RibbonOptionsFrame : Panel
{
    private readonly RibbonControl _ribbon;
    private readonly RoundedPainter _painter = new();
    private Size _childSize;
    private int _childRadius = -1;
    internal RibbonOptionsFrame(RibbonControl ribbon)
    {
        _ribbon = ribbon;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent; Padding = new(1);
    }
    internal void RefreshAppearance() { ShapeChild(); Invalidate(); }
    protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); ShapeChild(); }
    private void ShapeChild()
    {
        if (_ribbon is null || Controls.OfType<ListBox>().FirstOrDefault() is not { } child) return;
        int radius = Math.Max(0, RibbonOptionsChrome.Radius(_ribbon, DeviceDpi) - 1);
        if (_childSize == child.Size && _childRadius == radius) return;
        _childSize = child.Size; _childRadius = radius;
        child.Region = radius > 0 && child.Width > 0 && child.Height > 0 ? RoundedPainter.CreateOpaqueRegion(child.Size, radius) : null;
    }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); RefreshAppearance(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        RibbonOptionsChrome.Surface(_painter, e.Graphics, ClientRectangle, _ribbon, DeviceDpi, new(_ribbon.Theme.Surface), _ribbon.Theme.PopupBorder);
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _painter.Dispose(); }
}

// Button keeps the WinForms IButtonControl/default-button and keyboard behavior;
// only its painting uses the same palette and mirrored corners as other chrome.
internal sealed class RibbonOptionsAccentButton : Button
{
    private readonly RibbonControl _ribbon;
    private readonly RoundedPainter _painter = new();
    private bool _hot, _pressed;
    internal RibbonOptionsAccentButton(RibbonControl ribbon)
    {
        _ribbon = ribbon;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; UseVisualStyleBackColor = false; BackColor = Color.Transparent;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = _ribbon.Theme;
        var fill = !Enabled ? theme.Hover : _pressed ? theme.Pressed : _hot ? theme.Checked : new RibbonFill(theme.Accent);
        var color = !Enabled ? SystemColors.GrayText : _hot || _pressed ? theme.Foreground : theme.Surface;
        RibbonOptionsChrome.Surface(_painter, e.Graphics, ClientRectangle, _ribbon, DeviceDpi, fill, fill.Start, 0);
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, color, flags);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4), color, fill.Start);
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) _pressed = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }
    protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); _pressed = false; Invalidate(); }
    protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space) { _pressed = true; Invalidate(); } }
    protected override void OnKeyUp(KeyEventArgs e) { base.OnKeyUp(e); _pressed = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); _pressed = false; Invalidate(); }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) _painter.Dispose(); }
}
