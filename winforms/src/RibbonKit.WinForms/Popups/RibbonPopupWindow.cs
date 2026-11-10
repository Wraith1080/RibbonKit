namespace RibbonKit.WinForms;

// Common opaque frame, rounded clipping and layered soft shadow for every ribbon popup.
internal sealed class RibbonPopupWindow : ToolStripDropDown
{
    private readonly RibbonControl ribbon;
    private readonly Action activationChanged;
    private readonly bool nonActivating;
    internal RibbonPopupMotion? OpeningMotion { get; private set; }
    private bool _galleryAppearanceActive;
    internal void PrepareOpening(Control surface)
    {
        SettleMotion(); OpeningMotion = RibbonPopupMotion.Prepare(ribbon, this, surface);
        _galleryAppearanceActive = OpeningMotion?.FadesGallery == true;
    }
    internal void StartOpening() => OpeningMotion?.Start();
    internal void SettleMotion() { OpeningMotion?.Dispose(); OpeningMotion = null; }
    internal void CompleteGalleryAppearance(nint drawingWindow)
    {
        _galleryAppearanceActive = false;
        if (Visible && !IsDisposed && IsHandleCreated)
        {
            _chrome.Update(this, ribbon, UpdateShape(), show: false);
            _chrome.RevealAtHandoff(this, drawingWindow);
        }
    }
    internal RibbonPopupWindow(RibbonControl ribbon, Action activationChanged, bool nonActivating = false)
    {
        this.ribbon = ribbon; this.activationChanged = activationChanged; this.nonActivating = nonActivating;
        AutoSize = false; Padding = new(1); Margin = Padding.Empty;
        // AutoClose changes native topmost state and creates the HWND. Configure
        // its class shadow first, avoiding a second HWND and native setup pass.
        DropShadowEnabled = false; CausesValidation = false;
        Renderer = new PopupFrameRenderer(ribbon);
        AutoClose = false;
    }
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            if (nonActivating) parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            return parameters;
        }
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
        if (Visible) _chrome.Update(this, ribbon, UpdateShape(), show: !_galleryAppearanceActive);
        else { SettleMotion(); _chrome.Dispose(); }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        int radius = UpdateShape();
        base.OnPaint(e);
        _chrome.Update(this, ribbon, radius, show: !_galleryAppearanceActive);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { SettleMotion(); _chrome.Dispose(); }
        base.Dispose(disposing);
    }
    protected override void WndProc(ref Message m)
    {
        // Choice lists already route keys through their opener. ToolStrip's
        // default handling allows hosted native controls to activate the popup
        // before WM_LBUTTONDOWN, deactivating the owner even if the row itself
        // avoids SetFocus. Keep the click (including scrollbar presses) without
        // activating this window; other popup editors retain normal activation.
        if (nonActivating && m.Msg == 0x21) { m.Result = 3; return; } // WM_MOUSEACTIVATE / MA_NOACTIVATE
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
