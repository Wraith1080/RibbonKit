using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// The shadow and antialiased frame edge are layered. Native editors remain in
// the opaque popup; fully covered pixels share the same mirrored corner mask.
// This window cannot activate or intercept input and is disposed with its popup.
internal sealed class PopupChromeWindow : NativeWindow, IDisposable
{
    private RibbonTheme? _theme;
    private RibbonRenderer? _renderer;
    private Size _size;
    private int _dpi;
    private int _radius, _padding;
    private bool _shadowEnabled;
    private nint _owner;

    internal void Update(Control popup, RibbonControl ribbon, int radius, bool show = true)
    {
        bool shadow = ribbon.Theme.PopupShadowEnabled && SystemInformation.IsDropShadowEnabled;
        if (!popup.Visible || (!shadow && radius == 0))
        {
            Dispose();
            return;
        }
        nint owner = GetWindow(popup.Handle, 4); // GW_OWNER
        if (owner == 0) owner = ribbon.FindForm()?.Handle ?? 0;
        if (Handle != 0 && _owner != owner) Dispose();
        if (Handle == 0)
        {
            CreateHandle(new CreateParams
            {
                Caption = "Ribbon popup frame and shadow",
                Style = unchecked((int)0x80000000), // WS_POPUP
                ExStyle = 0x080800A0, // NOACTIVATE | LAYERED | TOOLWINDOW | TRANSPARENT
                Parent = owner
            });
            _owner = owner;
        }
        int padding = RibbonFrameMetrics.PopupPadding(ribbon.Painter, ribbon.Theme, ribbon.DeviceDpi);
        var bounds = Rectangle.Inflate(popup.Bounds, padding, padding);
        if (_size != bounds.Size || _theme != ribbon.Theme || !ReferenceEquals(_renderer, ribbon.Painter) || _dpi != ribbon.DeviceDpi || _radius != radius || _padding != padding || _shadowEnabled != shadow)
        {
            using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var context = new RibbonPaintContext(graphics, new(padding, padding, popup.Width, popup.Height), ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density };
                if (shadow) ribbon.Painter.DrawPopupShadow(context);
                if (radius > 0) ribbon.Painter.DrawPopupFrame(context);
            }
            Upload(bitmap, bounds.Location);
            _size = bounds.Size;
            _theme = ribbon.Theme;
            _renderer = ribbon.Painter;
            _dpi = ribbon.DeviceDpi;
            _radius = radius; _padding = padding;
            _shadowEnabled = shadow;
        }
        // Share the popup's native owner, rather than owning this from the
        // popup itself (owned windows must stay above their owner). A nested
        // popup's frame must follow that nested ownership level too: keeping
        // every frame on the host Form lets later owned tips lift a nested
        // frame above its own opaque content.
        // Hover repaints must not repeatedly enter native window positioning.
        // Still repair real moves, visibility and sibling Z-order changes (for
        // example when another owned popup or ScreenTip has been shown).
        if (!show)
            SetWindowPos(Handle, popup.Handle, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0090); // NOACTIVATE | HIDEWINDOW
        else if (!GetWindowRect(Handle, out var current) || current.Rectangle != bounds || !IsWindowVisible(Handle) || GetWindow(popup.Handle, 2) != Handle)
            SetWindowPos(Handle, popup.Handle, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0050); // NOACTIVATE | SHOWWINDOW
    }

    internal void RevealAtHandoff(Control popup, nint drawingWindow)
    {
        // Swap the inert shadow for the live chrome in one native position batch,
        // so overlapping shadow alpha cannot create a dark pulse at the endpoint.
        nint batch = BeginDeferWindowPos(2);
        if (batch != 0 && Handle != 0) batch = DeferWindowPos(batch, Handle, popup.Handle, 0, 0, 0, 0, 0x0253);
        if (batch != 0 && drawingWindow != 0) batch = DeferWindowPos(batch, drawingWindow, 0, 0, 0, 0, 0, 0x0297);
        if (batch != 0 && EndDeferWindowPos(batch)) return;
        if (Handle != 0) SetWindowPos(Handle, popup.Handle, 0, 0, 0, 0, 0x0253);
        if (drawingWindow != 0) SetWindowPos(drawingWindow, 0, 0, 0, 0, 0, 0x0297);
    }

    private void Upload(Bitmap bitmap, Point location)
    {
        nint screen = GetDC(0), dc = 0, image = 0, previous = 0;
        try
        {
            dc = CreateCompatibleDC(screen);
            image = bitmap.GetHbitmap(Color.FromArgb(0));
            previous = SelectObject(dc, image);
            var size = bitmap.Size;
            Point source = Point.Empty;
            var blend = new Blend { Alpha = 255, Format = 1 }; // AC_SRC_OVER / AC_SRC_ALPHA
            if (!UpdateLayeredWindow(Handle, screen, ref location, ref size, dc, ref source, 0, ref blend, 2))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            if (previous != 0) SelectObject(dc, previous);
            if (image != 0) DeleteObject(image);
            if (dc != 0) DeleteDC(dc);
            if (screen != 0) ReleaseDC(0, screen);
        }
    }
    public void Dispose()
    {
        if (Handle != 0) DestroyHandle();
        _theme = null;
        _renderer = null;
        _size = Size.Empty;
        _owner = 0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Blend { internal byte Operation, Flags, Alpha, Format; }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { internal int Left, Top, Right, Bottom; internal readonly Rectangle Rectangle => Rectangle.FromLTRB(Left, Top, Right, Bottom); }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeRect rectangle);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint image);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint image);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(nint window, nint screen, ref Point location, ref Size size, nint sourceDC, ref Point source, uint key, ref Blend blend, uint flags);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern nint BeginDeferWindowPos(int count);
    [DllImport("user32.dll")] private static extern nint DeferWindowPos(nint batch, nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool EndDeferWindowPos(nint batch);
}
