using System.Runtime.InteropServices;

namespace RibbonKit.WinForms.Design;

// A temporary, opaque native child of the designer's root window. It is never
// added to the authored Controls collection or sited in the designer container.
// The adorner retains all hit testing/selection; this layer only owns synthetic
// preview pixels. Its HWND clips the ribbon/document beneath it during refresh.
internal sealed class RibbonBackstagePreviewLayer : Control
{
    private readonly Control _surface;
    private Bitmap? _frame;
    private bool _presented;
    internal bool IsPresented => _presented && IsHandleCreated;
    internal RibbonBackstagePreviewLayer(Control surface)
    {
        _surface = surface; TabStop = false; Enabled = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.Selectable, false);
        surface.HandleDestroyed += SurfaceHandleDestroyed; surface.Disposed += SurfaceDisposed;
    }
    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.Parent = _surface.Handle;
            parameters.Style = (parameters.Style | 0x40000000 | 0x04000000 | 0x08000000) & ~(0x10000000 | 0x00010000); // CHILD, CLIPSIBLINGS, DISABLED; initially hidden, no TABSTOP.
            parameters.ExStyle = (parameters.ExStyle & ~0x20) | 0x08000000; // Opaque, NOACTIVATE.
            return parameters;
        }
    }
    internal bool Present(Rectangle bounds, Func<Bitmap> render)
    {
        if (IsDisposed || _surface.IsDisposed || !_surface.IsHandleCreated || bounds.Width <= 0 || bounds.Height <= 0)
        { HidePreview(); return false; }
        // Finish rendering before changing the currently visible frame. A failed
        // renderer leaves the retained preview and native ownership untouched.
        Bitmap? next = render();
        try
        {
            if (next.Size != bounds.Size) throw new ArgumentException("Preview frame must match its bounds.", nameof(render));
            Bounds = bounds;
            var handle = Handle;
            var previous = _frame; _frame = next; next = null;
            try
            {
                SetWindowPos(handle, 0, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0010 | 0x0040); // NOACTIVATE, SHOWWINDOW, top child.
                _presented = true; Invalidate(); Update();
            }
            finally { previous?.Dispose(); }
            return true;
        }
        finally { next?.Dispose(); }
    }
    internal void HidePreview()
    {
        if (IsHandleCreated) ShowWindow(Handle, 0);
        _presented = false; _frame?.Dispose(); _frame = null;
    }
    internal bool PaintRetained(Graphics graphics, Point rootOrigin)
    {
        if (!IsPresented || _frame is null) return false;
        graphics.DrawImageUnscaled(_frame, rootOrigin.X + Left, rootOrigin.Y + Top);
        return true;
    }
    protected override void OnPaintBackground(PaintEventArgs e) { }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (_frame is not null) e.Graphics.DrawImageUnscaled(_frame, Point.Empty);
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x84) { message.Result = -1; return; } // NCHITTEST: transparent to designer input.
        if (message.Msg == 0x21) { message.Result = 3; return; } // MOUSEACTIVATE: never focus the preview.
        base.WndProc(ref message);
    }
    private void SurfaceHandleDestroyed(object? sender, EventArgs e) { HidePreview(); if (IsHandleCreated) DestroyHandle(); }
    private void SurfaceDisposed(object? sender, EventArgs e) => Dispose();
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _surface.HandleDestroyed -= SurfaceHandleDestroyed; _surface.Disposed -= SurfaceDisposed; HidePreview();
        }
        base.Dispose(disposing);
    }
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int command);
}
