using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

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
/// caller must clear the window's <see cref="Window.Background"/> (and any opaque content that
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

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

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
}
