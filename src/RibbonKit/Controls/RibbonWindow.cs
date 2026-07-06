using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RibbonKit.Controls;

/// <summary>
/// A window with Office-style chrome: a custom title bar hosting the window title,
/// optional <see cref="TitleBarContent"/> (quick access buttons live well there),
/// and themed caption buttons — while keeping native behaviors (drag, double-click
/// maximize, resize borders, system menu) via <see cref="System.Windows.Shell.WindowChrome"/>.
/// <code language="xaml">
/// &lt;rk:RibbonWindow ...&gt;
///     &lt;rk:RibbonWindow.TitleBarContent&gt;
///         &lt;StackPanel Orientation="Horizontal"&gt; ...quick access buttons... &lt;/StackPanel&gt;
///     &lt;/rk:RibbonWindow.TitleBarContent&gt;
///     ...
/// &lt;/rk:RibbonWindow&gt;
/// </code>
/// </summary>
[TemplatePart(Name = WindowRootPartName, Type = typeof(FrameworkElement))]
public class RibbonWindow : Window
{
    private const string WindowRootPartName = "PART_WindowRoot";

    /// <summary>Identifies the <see cref="TitleBarContent"/> dependency property.</summary>
    public static readonly DependencyProperty TitleBarContentProperty =
        DependencyProperty.Register(
            nameof(TitleBarContent),
            typeof(object),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsTitleBarContentVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsTitleBarContentVisibleProperty =
        DependencyProperty.Register(
            nameof(IsTitleBarContentVisible),
            typeof(bool),
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(true));

    private FrameworkElement? _windowRoot;

    static RibbonWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonWindow),
            new FrameworkPropertyMetadata(typeof(RibbonWindow)));
    }

    /// <summary>Initializes the window and wires the caption button commands.</summary>
    public RibbonWindow()
    {
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.MinimizeWindowCommand,
            (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.MaximizeWindowCommand,
            (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.RestoreWindowCommand,
            (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new System.Windows.Input.CommandBinding(
            SystemCommands.CloseWindowCommand,
            (_, _) => SystemCommands.CloseWindow(this)));

        // A maximized WindowChrome window is resized by Windows to hang past every screen
        // edge (the layout of the resize frame), and re-measuring on SizeChanged keeps the
        // compensation inset current if the window is dragged to a monitor of a different
        // resolution/DPI while maximized.
        SizeChanged += (_, _) => UpdateMaximizeInset();
    }

    /// <summary>
    /// Content shown in the title bar between the window edge and the centered title —
    /// the natural home for quick access buttons.
    /// </summary>
    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    /// <summary>
    /// Whether <see cref="TitleBarContent"/> is currently shown. The hosted
    /// <see cref="Ribbon"/> sets this false while its backstage is open, matching
    /// Office.
    /// </summary>
    public bool IsTitleBarContentVisible
    {
        get => (bool)GetValue(IsTitleBarContentVisibleProperty);
        set => SetValue(IsTitleBarContentVisibleProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _windowRoot = GetTemplateChild(WindowRootPartName) as FrameworkElement;
        UpdateMaximizeInset();
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // First line of defence: ask Windows to keep the maximized window inside the
        // monitor's WORK AREA (so it respects the taskbar and doesn't overhang). This is
        // the classic WM_GETMINMAXINFO fix and it's enough for a bare Window — but a
        // WindowChrome window re-introduces the overhang through its own (miscalculated)
        // non-client frame sizing, so the measured inset below is what actually guarantees
        // the caption buttons and ribbon stay on-screen. Keeping the hook is still worth
        // it: when it does constrain the window, the measured overhang simply comes out as
        // zero, so the two mechanisms never double up.
        IntPtr handle = new WindowInteropHelper(this).EnsureHandle();
        HwndSource.FromHwnd(handle)?.AddHook(WindowHook);
        UpdateMaximizeInset();
    }

    /// <inheritdoc />
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateMaximizeInset();
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        UpdateMaximizeInset();
    }

    /// <summary>
    /// Insets the window root by the exact amount the maximized window spills past the
    /// monitor's work area, so content sits flush at the visible edges (nothing clipped,
    /// the caption buttons stay on-screen, and the ribbon card keeps its side margin).
    /// The overhang is MEASURED from the real window rect vs. the monitor rect and
    /// converted from device pixels to DIPs, so it is exact at every DPI — no reliance on
    /// <see cref="SystemParameters.WindowResizeBorderThickness"/>, whose value is
    /// ambiguous across Windows versions and is what makes the usual fixes flaky.
    /// </summary>
    private void UpdateMaximizeInset()
    {
        if (_windowRoot is null)
        {
            return;
        }

        Thickness target = default;

        if (WindowState == WindowState.Maximized)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero
                && GetWindowRect(hwnd, out NativeRect win))
            {
                IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
                var monitorInfo = new NativeMonitorInfo { cbSize = Marshal.SizeOf<NativeMonitorInfo>() };
                if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref monitorInfo))
                {
                    NativeRect work = monitorInfo.rcWork;
                    DpiScale dpi = VisualTreeHelper.GetDpi(this);
                    double sx = dpi.DpiScaleX <= 0 ? 1.0 : dpi.DpiScaleX;
                    double sy = dpi.DpiScaleY <= 0 ? 1.0 : dpi.DpiScaleY;

                    // How far the window spills past the work area on each edge (device px),
                    // clamped to >= 0, then converted to DIPs for the WPF layout margin.
                    target = new Thickness(
                        Math.Max(0, work.Left - win.Left) / sx,
                        Math.Max(0, work.Top - win.Top) / sy,
                        Math.Max(0, win.Right - work.Right) / sx,
                        Math.Max(0, win.Bottom - work.Bottom) / sy);
                }
            }
        }

        // Avoid re-triggering layout (SizeChanged -> UpdateMaximizeInset -> ...) when the
        // inset hasn't actually changed.
        if (!ThicknessesClose(_windowRoot.Margin, target))
        {
            _windowRoot.Margin = target;
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
        NativeRect work = monitorInfo.rcWork;   // desktop minus taskbar, in device pixels
        NativeRect area = monitorInfo.rcMonitor; // full monitor, in device pixels

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
