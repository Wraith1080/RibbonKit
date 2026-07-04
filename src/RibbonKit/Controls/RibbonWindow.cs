using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

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
public class RibbonWindow : Window
{
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
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Constrain the maximized window to the monitor's WORK AREA. Without this, a
        // WindowChrome window maximizes larger than the screen (overhanging by the
        // resize frame), which clips content at the edges and eats the ribbon card's
        // side margin. Handling WM_GETMINMAXINFO keeps the maximized size exact, so no
        // fragile per-DPI compensation margin is needed and the card keeps its margin.
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowHook);
    }

    private static IntPtr WindowHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WmGetMinMaxInfo = 0x0024;
        if (msg == WmGetMinMaxInfo)
        {
            ConstrainMaximizedBounds(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void ConstrainMaximizedBounds(IntPtr hwnd, IntPtr lParam)
    {
        const int MonitorDefaultToNearest = 0x00000002;
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var mmi = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        Rect work = monitorInfo.rcWork;
        Rect area = monitorInfo.rcMonitor;

        // Position and size the maximized window to the work area (monitor-relative),
        // so it never overhangs the screen or covers the taskbar.
        mmi.ptMaxPosition.X = work.Left - area.Left;
        mmi.ptMaxPosition.Y = work.Top - area.Top;
        mmi.ptMaxSize.X = work.Right - work.Left;
        mmi.ptMaxSize.Y = work.Bottom - work.Top;

        Marshal.StructureToPtr(mmi, lParam, true);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point ptReserved;
        public Point ptMaxSize;
        public Point ptMaxPosition;
        public Point ptMinTrackSize;
        public Point ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public int dwFlags;
    }
}
