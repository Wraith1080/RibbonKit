using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;

namespace RibbonKit.Interop;

/// <summary>
/// The system backdrop material applied to a window via the Desktop Window Manager.
/// </summary>
public enum RibbonBackdrop
{
    /// <summary>No system backdrop (the default opaque window).</summary>
    None = 1,

    /// <summary>Mica — the tinted, desktop-wallpaper-aware material used by Windows 11 apps.</summary>
    Mica = 2,

    /// <summary>Acrylic — a translucent blur of whatever is behind the window.</summary>
    Acrylic = 3,

    /// <summary>Mica Alt (tabbed) — a stronger Mica variant.</summary>
    Tabbed = 4,
}

/// <summary>
/// Applies a Windows 11 system backdrop (Mica / Acrylic) to a window through the DWM.
/// </summary>
/// <remarks>
/// <para>
/// The backdrop only shows through parts of the window that are actually transparent, so the
/// caller must clear the window's <see cref="System.Windows.Controls.Control.Background"/> (and any opaque content that
/// should reveal the material) to <c>Transparent</c> while a backdrop is active, and restore
/// the opaque brushes when turning it off.
/// </para>
/// <para>
/// Requires Windows 11 22H2 (build 22621) or newer; <see cref="TrySetBackdrop"/> returns
/// <see langword="false"/> on unsupported systems so callers can fall back gracefully.
/// </para>
/// </remarks>
public static class MicaHelper
{
    // DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE (Windows 11 22H2+).
    private const int DwmwaSystemBackdropType = 38;

    // DWMWA_WINDOW_CORNER_PREFERENCE / DWMWA_BORDER_COLOR (Windows 11 21H2+, build 22000).
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwcpRound = 2; // DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND

    // Window-style access for stripping the system menu (see ShowNativeCaptionButtons).
    private const int GwlStyle = -16;
    private const int WsSysMenu = 0x00080000;

    // SetWindowPos: apply a frame change without moving, resizing, reordering, or activating.
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    // GWL_STYLE holds a 32-bit style DWORD, so the non-Ptr overloads are correct even on x64
    // (GetWindowLongPtr is only required for the index slots that store real pointers/handles).
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, uint flags);

    /// <summary>Whether the running OS supports the DWM system-backdrop attribute (Win11 22H2+).</summary>
    public static bool IsSupported =>
        Environment.OSVersion.Platform == PlatformID.Win32NT
        && Environment.OSVersion.Version.Build >= 22621;

    /// <summary>
    /// Requests <paramref name="backdrop"/> for <paramref name="window"/>. Returns
    /// <see langword="true"/> if the DWM accepted it. The caller is responsible for making the
    /// window (and the content that should reveal it) transparent — see the type remarks.
    /// </summary>
    public static bool TrySetBackdrop(Window window, RibbonBackdrop backdrop)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (!IsSupported)
        {
            return false;
        }

        IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        int value = (int)backdrop;
        return DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref value, sizeof(int)) == 0;
    }

    /// <summary>
    /// Extends the DWM glass frame across the whole client area (<paramref name="full"/> =
    /// <see langword="true"/>) or restores it to none. A Mica/Acrylic backdrop only composites
    /// where the glass reaches, so a transparent window WITHOUT a full-client glass frame
    /// renders black — call this with <see langword="true"/> whenever a backdrop is active.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implemented through <see cref="WindowChrome.GlassFrameThickness"/> (rather than a raw
    /// <c>DwmExtendFrameIntoClientArea</c> call) so WindowChrome keeps re-applying it across
    /// resize/DPI/restyle updates instead of silently resetting it to zero. A fresh
    /// <see cref="WindowChrome"/> is assigned so a frozen/shared style instance is never mutated.
    /// </para>
    /// <para>
    /// <b>Caution:</b> <paramref name="full"/> = <see langword="false"/> collapses the glass to
    /// <c>0</c>. On a WindowChrome window (which strips the native non-client frame) the extended
    /// glass frame is what the DWM uses to draw the window border and Windows 11 rounded corners,
    /// so restoring to <c>0</c> removes them. If a window relies on glass for its border/corners
    /// (as <see cref="Controls.RibbonWindow"/>'s template does, with <c>GlassFrameThickness="-1"</c>),
    /// leave the glass extended when turning a backdrop off — an opaque window background is enough
    /// to avoid the black-background problem.
    /// </para>
    /// </remarks>
    public static void ExtendGlassFrame(Window window, bool full)
    {
        ArgumentNullException.ThrowIfNull(window);

        WindowChrome? existing = WindowChrome.GetWindowChrome(window);
        var chrome = new WindowChrome
        {
            CaptionHeight = existing?.CaptionHeight ?? 34d,
            ResizeBorderThickness = existing?.ResizeBorderThickness ?? SystemParameters.WindowResizeBorderThickness,
            CornerRadius = existing?.CornerRadius ?? new CornerRadius(0),
            UseAeroCaptionButtons = existing?.UseAeroCaptionButtons ?? false,
            GlassFrameThickness = full ? new Thickness(-1) : new Thickness(0),
        };

        WindowChrome.SetWindowChrome(window, chrome);
    }

    /// <summary>
    /// Shows or hides the window's native caption buttons (minimize / maximize / close) by
    /// toggling the <c>WS_SYSMENU</c> window style. Pass <see langword="false"/> while a
    /// transparent, glass-extended title bar is active so the DWM-drawn native buttons stop
    /// showing through and overlapping the custom caption buttons; pass <see langword="true"/>
    /// to restore them when the backdrop is turned off.
    /// </summary>
    /// <remarks>
    /// This is surgical on purpose: it leaves <see cref="Window.WindowStyle"/> untouched (so the
    /// window keeps its normal frame, maximize/snap, and work-area handling) and only clears the
    /// system-menu bit that governs the native caption controls. The change is applied live via
    /// <c>SetWindowPos(SWP_FRAMECHANGED)</c> — no HWND recreation — so it can follow a runtime
    /// backdrop toggle. Trade-off: with <c>WS_SYSMENU</c> gone the Alt+Space system menu and the
    /// window-icon menu are unavailable, which is expected for a fully custom-chrome window whose
    /// own buttons replace them. No-op if the window has no HWND yet.
    /// </remarks>
    public static void ShowNativeCaptionButtons(Window window, bool visible)
    {
        ArgumentNullException.ThrowIfNull(window);

        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        int style = GetWindowLong(hwnd, GwlStyle);
        int updated = visible ? (style | WsSysMenu) : (style & ~WsSysMenu);
        if (updated == style)
        {
            return;
        }

        SetWindowLong(hwnd, GwlStyle, updated);
        SetWindowPos(
            hwnd,
            IntPtr.Zero,
            0, 0, 0, 0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    /// <summary>
    /// Asks the DWM to round <paramref name="window"/>'s corners (and, when
    /// <paramref name="borderColor"/> is given, draw a matching border), so a
    /// <c>WindowStyle="None"</c> + <see cref="WindowChrome"/> window still gets the Windows 11
    /// rounded look (custom-chrome windows don't round by default). No-op before Windows 11
    /// (build 22000) or before the window has an HWND — call it from
    /// <see cref="Window.OnSourceInitialized"/>.
    /// </summary>
    /// <param name="window">The window whose corners to round; must already have an HWND.</param>
    /// <param name="borderColor">Optional border color as <c>0x00BBGGRR</c> (a COLORREF); omit
    /// to leave the DWM's default border.</param>
    public static void SetRoundedCorners(Window window, int? borderColor = null)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (Environment.OSVersion.Platform != PlatformID.Win32NT
            || Environment.OSVersion.Version.Build < 22000)
        {
            return;
        }

        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        int preference = DwmwcpRound;
        DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));

        if (borderColor is int color)
        {
            DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref color, sizeof(int));
        }
    }
}
