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
    private Control? _shapedChild;
    private readonly bool _summedTextPadding;
    private bool _layingOut;
    private bool _authoredPadding;
    private Padding _nativePadding;
    internal RibbonOptionsFrame(RibbonControl ribbon, bool summedTextPadding = false)
    {
        _ribbon = ribbon; _summedTextPadding = summedTextPadding;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent; Padding = new(1);
    }
    private Layout.RibbonOptionsFieldMetrics Metrics => new(_ribbon.Theme, DeviceDpi);
    internal int RequiredTextHeight(int desired) => Metrics.TextFrameHeight(desired, Controls.OfType<TextBox>().FirstOrDefault()?.PreferredHeight ?? 0);
    internal Rectangle TextBounds(int textHeight) => Metrics.SearchTextBounds(ClientRectangle, textHeight, _summedTextPadding);
    internal void RefreshAppearance() { PerformLayout(); Invalidate(); }
    protected override void OnLayout(LayoutEventArgs e)
    {
        if (_ribbon is null || _layingOut) { base.OnLayout(e); return; }
        _layingOut = true;
        try
        {
            if (_ribbon.Theme.OptionsAppearance?.FrameBorderWidth is not null)
            {
                if (!_authoredPadding) { _nativePadding = Padding; _authoredPadding = true; }
                var padding = new Padding(Metrics.Clearance(ClientSize)); if (Padding != padding) Padding = padding;
            }
            else if (_authoredPadding) { Padding = _nativePadding; _authoredPadding = false; }
            base.OnLayout(e);
            if (Controls.OfType<TextBox>().FirstOrDefault() is { } text) { text.Bounds = TextBounds(text.Height); text.ForeColor = _ribbon.Theme.EditorTextColor; }
            ShapeChild();
        }
        finally { _layingOut = false; }
    }
    private void ShapeChild()
    {
        if (_ribbon is null || Controls.Cast<Control>().FirstOrDefault(c => c is ListBox or TreeView or ListView) is not { } child) return;
        int radius = Metrics.ChildRadius(ClientSize, RibbonOptionsChrome.Radius(_ribbon, DeviceDpi));
        if (ReferenceEquals(_shapedChild, child) && _childSize == child.Size && _childRadius == radius) return;
        _shapedChild = child; _childSize = child.Size; _childRadius = radius;
        child.Region = radius > 0 && child.Width > 0 && child.Height > 0 ? RoundedPainter.CreateOpaqueRegion(child.Size, radius) : null;
    }
    protected override void OnDpiChangedAfterParent(EventArgs e) { base.OnDpiChangedAfterParent(e); RefreshAppearance(); }
    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
        if (_authoredPadding) _nativePadding = new((int)Math.Round(_nativePadding.Left * factor.Width), (int)Math.Round(_nativePadding.Top * factor.Height), (int)Math.Round(_nativePadding.Right * factor.Width), (int)Math.Round(_nativePadding.Bottom * factor.Height));
        base.ScaleControl(factor, specified);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        RibbonOptionsFieldRenderer.DrawFrame(_painter, new(e.Graphics, ClientRectangle, _ribbon.Theme, Font, DeviceDpi), RibbonOptionsChrome.Radius(_ribbon, DeviceDpi));
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
        if (!_accent && theme.OptionsButtonCornerRadius is null && theme.OptionsAppearance?.HasButtonOverrides(accent: false) != true) { base.OnPaint(e); return; }
        RibbonOptionsButtonRenderer.Draw(_painter, new(e.Graphics, ClientRectangle, theme, Font, DeviceDpi) { Density = _ribbon.Density }, this,
            _accent, _hot, _pressed, ShowKeyboardCues, Focused && ShowFocusCues, RibbonOptionsChrome.Radius(_ribbon, DeviceDpi), Parent?.BackColor ?? theme.Surface);
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
