namespace RibbonKit.WinForms;

internal static class RibbonOptionsChrome
{
    internal static int Radius(RibbonControl ribbon, int dpi) => Math.Min(
        ribbon.Painter.GetPopupCornerRadius(ribbon.Theme, dpi),
        (int)Math.Round(ribbon.Theme.CornerRadius * dpi / 96d));

    internal static void Surface(RoundedPainter painter, Graphics graphics, Rectangle bounds, RibbonControl ribbon,
        int dpi, RibbonFill fill, Color border, int stroke = 1)
        => painter.Draw(graphics, bounds, Radius(ribbon, dpi), fill, border, stroke);

    // Native TreeView state images keep native selection/scrolling while using
    // exactly the renderer's ordinary checkbox glyph and theme roles.
    internal static Bitmap TabCheck(RibbonControl ribbon, Font font, int dpi, int slot, bool marked)
    {
        var image = new Bitmap(slot, slot);
        using var graphics = Graphics.FromImage(image);
        using var model = new RibbonCheckBoxItem { Checked = marked };
        int side = Math.Min(slot, Layout.RibbonLayoutEngine.Scale(16, dpi));
        var bounds = new Rectangle((slot - side) / 2, (slot - side) / 2, side, side);
        ribbon.Painter.DrawCheckBoxIndicator(new(graphics, bounds, ribbon.Theme, font, dpi) { Density = ribbon.Density }, model);
        return image;
    }
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
        if (_ribbon is null || Controls.Cast<Control>().FirstOrDefault(c => c is ListBox or TreeView or ListView) is not { } child) return;
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
internal class RibbonOptionsButton : Button
{
    private readonly RibbonControl _ribbon;
    private readonly RoundedPainter _painter = new();
    private readonly bool _accent;
    private bool _hot, _pressed;
    internal RibbonOptionsButton(RibbonControl ribbon, bool accent = false)
    {
        _ribbon = ribbon; _accent = accent;
        if (accent)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; UseVisualStyleBackColor = false; BackColor = Color.Transparent;
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = _ribbon.Theme;
        if (!_accent && theme.OptionsButtonCornerRadius is null) { base.OnPaint(e); return; }
        var fill = _accent ? !Enabled ? theme.Hover : _pressed ? theme.Pressed : _hot ? theme.Checked : new RibbonFill(theme.Accent)
            : !Enabled ? new(theme.Surface) : _pressed ? theme.Pressed : _hot ? theme.Hover : new(theme.Surface);
        var color = _accent ? !Enabled ? SystemColors.GrayText : _hot || _pressed ? theme.Foreground : theme.Surface
            : Enabled ? theme.Foreground : theme.DisabledColor;
        if (!_accent)
        {
            using var background = new SolidBrush(Parent?.BackColor is { A: 255 } parentColor ? parentColor : theme.Surface);
            e.Graphics.FillRectangle(background, ClientRectangle);
        }
        int radius = RibbonKit.WinForms.Layout.RibbonFrameMetrics.Radius(theme.OptionsButtonCornerRadius, RibbonOptionsChrome.Radius(_ribbon, DeviceDpi), DeviceDpi);
        _painter.Draw(e.Graphics, ClientRectangle, radius, fill, _accent ? fill.Start : theme.PopupBorder, _accent || theme.PopupBorder.A == 0 ? 0 : 1);
        var label = Rectangle.FromLTRB(Padding.Left, Padding.Top, Math.Max(Padding.Left, Width - Padding.Right), Math.Max(Padding.Top, Height - Padding.Bottom));
        if (Image is { } image)
        {
            int x = ImageAlign is ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft ? label.Left
                : ImageAlign is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight ? label.Right - image.Width : label.Left + (label.Width - image.Width) / 2;
            int y = ImageAlign is ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight ? label.Top
                : ImageAlign is ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight ? label.Bottom - image.Height : label.Top + (label.Height - image.Height) / 2;
            if (Enabled) e.Graphics.DrawImageUnscaled(image, x, y); else ControlPaint.DrawImageDisabled(e.Graphics, image, x, y, theme.Surface);
            if (TextImageRelation == TextImageRelation.ImageBeforeText) label = Rectangle.FromLTRB(Math.Min(label.Right, x + image.Width), label.Top, label.Right, label.Bottom);
            else if (TextImageRelation == TextImageRelation.TextBeforeImage) label.Width = Math.Max(0, x - label.Left);
        }
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        if (!_accent)
        {
            if (TextAlign is ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft) flags &= ~TextFormatFlags.HorizontalCenter;
            else if (TextAlign is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight) flags = flags & ~TextFormatFlags.HorizontalCenter | TextFormatFlags.Right;
            if (TextAlign is ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight) flags &= ~TextFormatFlags.VerticalCenter;
            else if (TextAlign is ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight) flags = flags & ~TextFormatFlags.VerticalCenter | TextFormatFlags.Bottom;
        }
        if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        TextRenderer.DrawText(e.Graphics, Text, Font, _accent ? ClientRectangle : label, color, flags);
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

internal sealed class RibbonOptionsAccentButton(RibbonControl ribbon) : RibbonOptionsButton(ribbon, accent: true);
