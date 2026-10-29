using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Alpha-preserving client paint for the optional DWM header. This is a regular
// child HWND, not another input/presentation window. Reuse its DIB between paints.
internal sealed class RibbonBackdropBuffer : IDisposable
{
    private nint _dc, _dib, _old;
    private Bitmap? _image;
    private Graphics? _graphics;
    internal Bitmap Image => _image!;
    internal void Paint(Graphics destination, Size size, int headerHeight, Color fallback, Action<Graphics> paint)
    {
        if (size.Width < 1 || size.Height < 1 || (long)size.Width * size.Height > 4_194_304) throw new Win32Exception("Backdrop drawing exceeds its bounded pixel budget.");
        if (_image?.Size != size) Allocate(size);
        var state = _graphics!.Save();
        try
        {
            _graphics.Clear(Color.Transparent);
            using (var background = new SolidBrush(fallback)) _graphics.FillRectangle(background, 0, headerHeight, size.Width, Math.Max(0, size.Height - headerHeight));
            paint(_graphics); _graphics.Flush();
        }
        finally { _graphics.Restore(state); }
        nint output = destination.GetHdc();
        try { if (!BitBlt(output, 0, 0, size.Width, size.Height, _dc, 0, 0, 0x00CC0020)) throw new Win32Exception(Marshal.GetLastWin32Error()); }
        finally { destination.ReleaseHdc(output); }
    }
    private void Allocate(Size size)
    {
        Dispose();
        try
        {
            _dc = CreateCompatibleDC(0); if (_dc == 0) throw new Win32Exception();
            var info = new BitmapInfo { Size = 40, Width = size.Width, Height = -size.Height, Planes = 1, Bits = 32 };
            _dib = CreateDIBSection(_dc, ref info, 0, out nint bits, 0, 0); if (_dib == 0) throw new Win32Exception();
            _old = SelectObject(_dc, _dib);
            _image = new(size.Width, size.Height, size.Width * 4, PixelFormat.Format32bppPArgb, bits);
            _graphics = Graphics.FromHdc(_dc);
        }
        catch { Dispose(); throw; }
    }
    internal static void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags)
    {
        if (string.IsNullOrEmpty(text) || context.Bounds.Width <= 0 || context.Bounds.Height <= 0) return;
        // GDI TextRenderer does not maintain glyph alpha. Composited theme text
        // paints glyph coverage into the DIB, including black text over material.
        nint theme = OpenThemeData(0, "WINDOW");
        if (theme == 0) throw new Win32Exception("Composited window text is unavailable.");
        var clip = Rectangle.Round(context.Graphics.ClipBounds);
        nint dc = context.Graphics.GetHdc(), font = 0, old = 0; int saved = SaveDC(dc);
        try
        {
            IntersectClipRect(dc, clip.Left, clip.Top, clip.Right, clip.Bottom);
            font = context.Font.ToHfont(); old = SelectObject(dc, font);
            var bounds = context.Bounds;
            if ((flags & TextFormatFlags.NoPadding) == 0)
            {
                int inset = (int)Math.Ceiling(context.Font.Height / 6d);
                bounds.Inflate(-Math.Min(inset, bounds.Width / 2), 0);
            }
            var rectangle = new NativeRect { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Bottom };
            var options = new TextOptions { Size = Marshal.SizeOf<TextOptions>(), Flags = 0x2001, TextColor = color.R | color.G << 8 | color.B << 16 };
            int result = DrawThemeTextEx(theme, dc, 0, 0, text, text.Length, (uint)flags & 0x00FFFFFF, ref rectangle, ref options);
            if (result < 0) Marshal.ThrowExceptionForHR(result);
        }
        finally
        {
            if (old != 0) SelectObject(dc, old); if (font != 0) DeleteObject(font);
            RestoreDC(dc, saved); context.Graphics.ReleaseHdc(dc); CloseThemeData(theme);
        }
    }
    public void Dispose()
    {
        _graphics?.Dispose(); _graphics = null; _image?.Dispose(); _image = null;
        if (_dc != 0 && _old != 0) SelectObject(_dc, _old);
        if (_dib != 0) DeleteObject(_dib); if (_dc != 0) DeleteDC(_dc);
        _dc = _dib = _old = 0;
    }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { internal int Size, Width, Height; internal short Planes, Bits; internal int Compression, ImageSize, X, Y, Colors, Important; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct TextOptions
    {
        internal int Size, Flags, TextColor, BorderColor, ShadowColor, ShadowType;
        internal Point ShadowOffset;
        internal int BorderSize, FontProperty, ColorProperty, State, ApplyOverlay, GlowSize;
        internal nint Callback, CallbackData;
    }
    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern int SaveDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool RestoreDC(nint dc, int saved);
    [DllImport("gdi32.dll")] private static extern int IntersectClipRect(nint dc, int left, int top, int right, int bottom);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] private static extern nint OpenThemeData(nint window, string classes);
    [DllImport("uxtheme.dll")] private static extern int CloseThemeData(nint theme);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] private static extern int DrawThemeTextEx(nint theme, nint dc, int part, int state, string text, int length, uint flags, ref NativeRect bounds, ref TextOptions options);
}
