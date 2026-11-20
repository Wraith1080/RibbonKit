using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Common opaque frame, rounded clipping and layered soft shadow for every ribbon popup.
internal sealed class RibbonPopupWindow : ToolStripDropDown
{
    private readonly RibbonControl ribbon;
    private readonly Action activationChanged;
    private readonly bool nonActivating;
    internal RibbonPopupMotion? OpeningMotion { get; private set; }
    private bool _galleryAppearanceActive;
    internal RibbonApplicationMenuComposition ApplicationComposition;
    internal bool IsApplicationMenu;
    internal void PrepareOpening(Control surface)
    {
        SettleMotion(); OpeningMotion = ApplicationComposition.IsEmpty ? RibbonPopupMotion.Prepare(ribbon, this, surface) : null;
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
        AutoSize = false; Padding = new(RibbonPopupFrameMetrics.ContentInset(ribbon.Painter, ribbon.Theme, ribbon.DeviceDpi)); Margin = Padding.Empty;
        // AutoClose changes native topmost state and creates the HWND. Configure
        // its class shadow first, avoiding a second HWND and native setup pass.
        DropShadowEnabled = false; CausesValidation = false;
        Renderer = new PopupFrameRenderer(ribbon, this);
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
    private int _shapeInset = -1;
    private Rectangle _compositionCanvas;
    private RibbonApplicationMenuComposition _shapeComposition;
    private int UpdateShape()
    {
        int radius = Math.Clamp(ribbon.Painter.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi), 0, Math.Min(Width, Height) / 2);
        if (_shapeSize == Size && _shapeRadius == radius && _shapeInset == Padding.Left && _shapeComposition == ApplicationComposition && (ApplicationComposition.IsEmpty || _compositionCanvas == Bounds)) return radius;
        _shapeSize = Size; _shapeRadius = radius; _shapeInset = Padding.Left;
        _compositionCanvas = Bounds; _shapeComposition = ApplicationComposition;
        // Control owns its assigned Region, including replacement/disposal.
        var region = radius > 0 ? RoundedPainter.CreateOpaqueRegion(Size, radius) : ApplicationComposition.IsEmpty ? null : new Region(ClientRectangle);
        if (!ApplicationComposition.IsEmpty) { using var exclusion = ApplicationComposition.Exclusion(Bounds); region!.Exclude(exclusion); }
        Region = region;
        foreach (var host in Items.OfType<ToolStripControlHost>())
        {
            var childRegion = radius > 0 ? RoundedPainter.CreateOpaqueRegion(host.Control.Size, Math.Max(0, radius - _shapeInset)) : ApplicationComposition.IsEmpty ? null : new Region(host.Control.ClientRectangle);
            if (!ApplicationComposition.IsEmpty) { using var exclusion = ApplicationComposition.Exclusion(host.Control.RectangleToScreen(host.Control.ClientRectangle)); childRegion!.Exclude(exclusion); }
            host.Control.Region = childRegion;
        }
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

    private sealed class PopupFrameRenderer(RibbonControl ribbon, RibbonPopupWindow window) : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) =>
            ribbon.Painter.DrawSurface(new(e.Graphics, e.ToolStrip.ClientRectangle, ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density, IsApplicationMenu = window.IsApplicationMenu }, true);
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) =>
            ribbon.Painter.DrawPopupFrame(new(e.Graphics, e.ToolStrip.ClientRectangle, ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density, IsApplicationMenu = window.IsApplicationMenu });
    }
}
