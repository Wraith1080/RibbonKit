using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RibbonKit.Interop;

/// <summary>
/// Keeps a <see cref="WindowChrome"/>-style borderless window (WindowStyle=None + WindowChrome)
/// from spilling past the monitor work area when maximized. A maximized WindowChrome window is
/// sized by Windows to hang past every screen edge, which clips content, hides caption buttons,
/// and covers the taskbar. This attaches two defences, exactly as <see cref="Controls.RibbonWindow"/>
/// does for the main window:
/// <list type="number">
/// <item>a <c>WM_GETMINMAXINFO</c> hook that constrains the maximized rect to the work area;</item>
/// <item>a measured inset applied to a "root" element (the overhang past the work area, in DIPs)
/// so content sits flush at the visible edges at any DPI.</item>
/// </list>
/// Attach once (typically from the window's constructor), passing a delegate that returns the
/// element to inset (the template's outermost content border). Both mechanisms are idempotent and
/// self-correct on resize / DPI change / monitor move.
/// </summary>
/// <remarks>
/// This duplicates the mechanism in <see cref="Controls.RibbonWindow"/> rather than refactoring
/// that (verified) type to depend on it — a deliberate low-risk choice. The two could be
/// consolidated later, with <c>RibbonWindow</c> also delegating here.
/// </remarks>
internal static class MaximizeGuard
{
    /// <summary>
    /// Wires the maximize defences onto <paramref name="window"/>. <paramref name="rootProvider"/>
    /// returns the element to inset (usually resolved from the template in OnApplyTemplate); it may
    /// return <see langword="null"/> before the template is applied, in which case the inset is
    /// simply skipped until it's available.
    /// </summary>
    public static void Attach(Window window, Func<FrameworkElement?> rootProvider)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(rootProvider);

        void Update() => UpdateInset(window, rootProvider());

        window.SourceInitialized += (_, _) =>
        {
            IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
            HwndSource.FromHwnd(handle)?.AddHook(WindowHook);
            Update();
        };
        window.SizeChanged += (_, _) => Update();
        window.StateChanged += (_, _) => Update();
        window.DpiChanged += (_, _) => Update();
    }

    private static void UpdateInset(Window window, FrameworkElement? root)
    {
        if (root is null)
        {
            return;
        }

        Thickness target = default;

        if (window.WindowState == WindowState.Maximized)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out NativeRect win))
            {
                IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
                var monitorInfo = new NativeMonitorInfo { cbSize = Marshal.SizeOf<NativeMonitorInfo>() };
                if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref monitorInfo))
                {
                    NativeRect work = monitorInfo.rcWork;
                    DpiScale dpi = VisualTreeHelper.GetDpi(window);
                    double sx = dpi.DpiScaleX <= 0 ? 1.0 : dpi.DpiScaleX;
                    double sy = dpi.DpiScaleY <= 0 ? 1.0 : dpi.DpiScaleY;

                    target = new Thickness(
                        Math.Max(0, work.Left - win.Left) / sx,
                        Math.Max(0, work.Top - win.Top) / sy,
                        Math.Max(0, win.Right - work.Right) / sx,
                        Math.Max(0, win.Bottom - work.Bottom) / sy);
                }
            }
        }

        if (!ThicknessesClose(root.Margin, target))
        {
            root.Margin = target;
        }
    }

    private static bool ThicknessesClose(Thickness a, Thickness b)
    {
        const double eps = 0.5;
        return Math.Abs(a.Left - b.Left) < eps
            && Math.Abs(a.Top - b.Top) < eps
            && Math.Abs(a.Right - b.Right) < eps
            && Math.Abs(a.Bottom - b.Bottom) < eps;
    }

    private static IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (msg == WmGetMinMaxInfo && ConstrainMaximizedBounds(hwnd, lParam))
        {
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static bool ConstrainMaximizedBounds(IntPtr hwnd, IntPtr lParam)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new NativeMonitorInfo { cbSize = Marshal.SizeOf<NativeMonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return false;
        }

        var mmi = Marshal.PtrToStructure<NativeMinMaxInfo>(lParam);
        NativeRect work = monitorInfo.rcWork;   // desktop minus taskbar, device pixels
        NativeRect area = monitorInfo.rcMonitor; // full monitor, device pixels

        int width = work.Right - work.Left;
        int height = work.Bottom - work.Top;

        mmi.ptMaxPosition.X = work.Left - area.Left;
        mmi.ptMaxPosition.Y = work.Top - area.Top;
        mmi.ptMaxSize.X = width;
        mmi.ptMaxSize.Y = height;
        mmi.ptMaxTrackSize.X = width;
        mmi.ptMaxTrackSize.Y = height;

        Marshal.StructureToPtr(mmi, lParam, true);
        return true;
    }

    private const int MonitorDefaultToNearest = 0x00000002;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeMonitorInfo lpmi);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMinMaxInfo
    {
        public NativePoint ptReserved;
        public NativePoint ptMaxSize;
        public NativePoint ptMaxPosition;
        public NativePoint ptMinTrackSize;
        public NativePoint ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public int dwFlags;
    }
}
