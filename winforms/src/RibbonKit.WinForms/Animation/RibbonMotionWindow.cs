using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// A click-through, nonactivating drawing plane with a reusable 32-bit DIB. Frames
// update existing memory/DCs; endpoint capture and layout never run on the timer.
internal sealed class RibbonMotionWindow : NativeWindow, IDisposable
{
    private nint _dc, _dib, _old;
    private Bitmap? _bitmap;
    private Graphics? _graphics;
    private readonly ImageAttributes _alpha = new();
    private Rectangle _bounds;
    internal RibbonMotionWindow(Control owner) => CreateHandle(new CreateParams { Caption = "Ribbon motion", Style = unchecked((int)0x80000000), ExStyle = 0x080800A0, Parent = owner.Handle });
    internal void Render(RibbonControl ribbon, RibbonMotionScene from, RibbonMotionScene to, double progress)
    {
        var bounds = Rectangle.Union(from.Bounds, to.Bounds);
        Prepare(bounds); RibbonMotionScene.Render(_graphics!, bounds, ribbon, from, to, progress, _alpha); Present();
    }
    internal void RenderPopup(RibbonPopupMotion motion, double progress)
    {
        Prepare(motion.Bounds); motion.Draw(_graphics!, _alpha, progress); Present();
    }
    internal void RenderGroup(RibbonGroupMotion motion, double progress)
    {
        Prepare(motion.Bounds); motion.Draw(_graphics!, _alpha, progress); Present();
        // A row animation must remain below its live full-group/menu windows,
        // including popups flipped above their anchor by working-area fitting.
        if (motion.BehindWindow is var above && above != 0)
            SetWindowPos(Handle, above, 0, 0, 0, 0, 0x0213); // NOOWNERZORDER | NOACTIVATE | NOMOVE | NOSIZE
    }
    internal void RenderBackstage(RibbonBackstageMotion motion, double progress)
    {
        Prepare(motion.Bounds); motion.Draw(_graphics!, _alpha, progress); Present();
    }
    internal void RenderDismissal(Bitmap image, Rectangle bounds, double opacity, int offset)
    {
        Prepare(bounds); _graphics!.Clear(Color.Transparent); _alpha.SetColorMatrix(new ColorMatrix { Matrix33 = (float)opacity });
        _graphics.DrawImage(image, new Rectangle(0, offset, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, _alpha); Present();
    }
    private void Prepare(Rectangle bounds) { if (_bitmap?.Size != bounds.Size) Allocate(bounds.Size); _bounds = bounds; }
    private void Present()
    {
        _graphics!.Flush();
        var bounds = _bounds;
        var size = bounds.Size; var location = bounds.Location; Point source = Point.Empty; var blend = new Blend { Alpha = 255, Format = 1 };
        if (!UpdateLayeredWindow(Handle, 0, ref location, ref size, _dc, ref source, 0, ref blend, 2)) { int error = Marshal.GetLastWin32Error(); throw new Win32Exception(error, "Native drawing plane failed: " + error); }
        SetWindowPos(Handle, 0, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0050); // NOACTIVATE | SHOWWINDOW
    }
    private void Allocate(Size size)
    {
        ReleaseBuffer(); _dc = CreateCompatibleDC(0); var info = new BitmapInfo { Size = 40, Width = size.Width, Height = -size.Height, Planes = 1, BitCount = 32 };
        _dib = CreateDIBSection(_dc, ref info, 0, out var bits, 0, 0);
        if (_dc == 0 || _dib == 0 || bits == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        _old = SelectObject(_dc, _dib); _bitmap = new(size.Width, size.Height, size.Width * 4, PixelFormat.Format32bppPArgb, bits); _graphics = Graphics.FromImage(_bitmap);
    }
    internal Bitmap CaptureFrame() => (Bitmap)_bitmap!.Clone();
    internal Rectangle Bounds => _bounds;
    private void ReleaseBuffer()
    {
        _graphics?.Dispose(); _graphics = null; _bitmap?.Dispose(); _bitmap = null;
        if (_old != 0) { SelectObject(_dc, _old); _old = 0; } if (_dib != 0) { DeleteObject(_dib); _dib = 0; } if (_dc != 0) { DeleteDC(_dc); _dc = 0; }
    }
    public void Dispose() { ReleaseBuffer(); _alpha.Dispose(); if (Handle != 0) DestroyHandle(); }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { internal uint Size; internal int Width, Height; internal ushort Planes, BitCount; internal uint Compression, ImageSize; internal int X, Y; internal uint Colors, Important; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { internal byte Operation, Flags, Alpha, Format; }
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint item);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint item);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(nint window, nint destination, ref Point location, ref Size size, nint source, ref Point sourcePoint, uint key, ref Blend blend, uint flags);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}
