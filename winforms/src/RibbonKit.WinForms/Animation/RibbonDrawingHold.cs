using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Hide live drawings while retaining native focus, input and editor HWNDs.
// The Windows 8+ host compatibility declaration permits layered child windows.
internal sealed class RibbonDrawingHold(nint window, long addedStyle = 0x80000) : IDisposable
{
    private bool _disposed;
    internal static bool HasOpacity(nint window) => (GetStyle(window) & 0x80000) != 0;
    internal static RibbonDrawingHold Create(nint window, byte alpha = 1, bool compositeChildren = false)
    {
        if (HasOpacity(window)) throw new InvalidOperationException("Application-owned opacity cannot be borrowed.");
        long style = GetStyle(window), added = 0x80000 | (compositeChildren ? 0x02000000 & ~style : 0);
        var hold = new RibbonDrawingHold(window, added); SetStyle(window, style | added);
        // Alpha 0 redirects native hit testing to the parent; alpha 1 retains it.
        if (!SetLayeredWindowAttributes(window, 0, alpha, 2)) { int error = Marshal.GetLastWin32Error(); hold.Dispose(); throw new Win32Exception(error, "Native child drawing hold failed: " + error); }
        return hold;
    }
    internal void SetOpacity(byte alpha) { if (!_disposed && IsWindow(window) && !SetLayeredWindowAttributes(window, 0, alpha, 2)) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    internal void CompositeChildren()
    {
        if (_disposed || !IsWindow(window)) return;
        long style = GetStyle(window);
        if ((style & 0x02000000) == 0) { SetStyle(window, style | 0x02000000); addedStyle |= 0x02000000; }
    }
    public void Dispose() { if (_disposed) return; _disposed = true; if (IsWindow(window)) { SetLayeredWindowAttributes(window, 0, 255, 2); SetStyle(window, GetStyle(window) & ~addedStyle); RedrawWindow(window, 0, 0, 0x185); } }
    private static long GetStyle(nint window) => (IntPtr.Size == 8 ? GetWindowLongPtr(window, -20) : GetWindowLong(window, -20)).ToInt64();
    private static void SetStyle(nint window, long style) { if (IntPtr.Size == 8) SetWindowLongPtr(window, -20, (nint)style); else SetWindowLong(window, -20, (nint)style); }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern nint GetWindowLong(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint window, int index, nint value);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern nint SetWindowLong(nint window, int index, nint value);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetLayeredWindowAttributes(nint window, uint color, byte alpha, uint flags);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern bool RedrawWindow(nint window, nint rectangle, nint region, uint flags);
}
