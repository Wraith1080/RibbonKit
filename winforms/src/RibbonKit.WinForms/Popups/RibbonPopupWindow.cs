namespace RibbonKit.WinForms;

// Common opaque frame, rounded clipping and layered soft shadow for every ribbon popup.
internal sealed class RibbonPopupWindow : ToolStripDropDown
{
    private readonly RibbonControl ribbon;
    private readonly Action activationChanged;
    internal RibbonPopupMotion? OpeningMotion { get; private set; }
    internal void PrepareOpening(Control surface) { SettleMotion(); OpeningMotion = RibbonPopupMotion.Prepare(ribbon, this, surface); }
    internal void StartOpening() => OpeningMotion?.Start();
    internal void SettleMotion() { OpeningMotion?.Dispose(); OpeningMotion = null; }
    internal RibbonPopupWindow(RibbonControl ribbon, Action activationChanged)
    {
        this.ribbon = ribbon; this.activationChanged = activationChanged;
        AutoSize = false; AutoClose = false; Padding = new(1); Margin = Padding.Empty;
        DropShadowEnabled = false; CausesValidation = false;
        Renderer = new PopupFrameRenderer(ribbon);
    }
    private readonly PopupChromeWindow _chrome = new();
    private Size _shapeSize;
    private int _shapeRadius = -1;
    private int UpdateShape()
    {
        int radius = Math.Clamp(ribbon.Painter.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi), 0, Math.Min(Width, Height) / 2);
        if (_shapeSize == Size && _shapeRadius == radius) return radius;
        _shapeSize = Size; _shapeRadius = radius;
        // Control owns its assigned Region, including replacement/disposal.
        Region = radius > 0 ? RoundedPainter.CreateOpaqueRegion(Size, radius) : null;
        foreach (var host in Items.OfType<ToolStripControlHost>())
            host.Control.Region = radius > 0 ? RoundedPainter.CreateOpaqueRegion(host.Control.Size, Math.Max(0, radius - 1)) : null;
        return radius;
    }
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) _chrome.Update(this, ribbon, UpdateShape());
        else { SettleMotion(); _chrome.Dispose(); }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        int radius = UpdateShape();
        base.OnPaint(e);
        _chrome.Update(this, ribbon, radius);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { SettleMotion(); _chrome.Dispose(); }
        base.Dispose(disposing);
    }
    protected override void WndProc(ref Message m)
    {
        // WM_ACTIVATEAPP / WM_ACTIVATE also reach the popup when its owning
        // Form has already deactivated during the initial popup opening.
        bool deactivated = (m.Msg == 0x1C && m.WParam == 0) || (m.Msg == 0x6 && (m.WParam.ToInt64() & 0xffff) == 0);
        base.WndProc(ref m);
        if (deactivated && !IsDisposed)
            activationChanged();
    }

    private sealed class PopupFrameRenderer(RibbonControl ribbon) : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) =>
            ribbon.Painter.DrawSurface(new(e.Graphics, e.ToolStrip.ClientRectangle, ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density }, true);
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) =>
            ribbon.Painter.DrawPopupFrame(new(e.Graphics, e.ToolStrip.ClientRectangle, ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density });
    }
}
