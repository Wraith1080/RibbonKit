using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

/// <summary>An opaque content panel for native GDI controls over a RibbonForm backdrop. Solid hosts use ordinary Panel painting; active material uses native composition. Child handles, input and ownership are retained. A Windows 8-or-later compatibility manifest is required for material composition.</summary>
public class RibbonBackdropPanel : Panel
{
    private bool _opaque, _updatingComposition;
    private nint _compositionRoot;
    /// <summary>Creates an opaque native content surface. The application owns its child controls and normal Panel layout.</summary>
    public RibbonBackdropPanel() { BackColor = SystemColors.Window; }
    /// <inheritdoc />
    protected override CreateParams CreateParams
    {
        get
        {
            var value = base.CreateParams;
            if (FindForm() is RibbonForm { HasWindowBackdrop: true }) value.ExStyle |= 0x00080000; // LAYERED; retain ordinary native child painting.
            return value;
        }
    }
    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e); _opaque = false; _compositionRoot = 0; EnsureOpaque();
    }
    internal void EnsureOpaque()
    {
        if (_updatingComposition || !IsHandleCreated || IsDisposed || Disposing) return;
        _updatingComposition = true;
        try
        {
            long style = ExtendedStyle();
            if (FindForm() is not RibbonForm { HasWindowBackdrop: true, IsHandleCreated: true } host)
            {
                _opaque = false; _compositionRoot = 0;
                if ((style & 0x00080000) != 0)
                {
                    SetExtendedStyle(style & ~0x00080000);
                    RedrawWindow(Handle, 0, 0, 0x185);
                }
                return;
            }
            // HandleCreated can run while WinForms still owns the HWND in its
            // parking window. Window-position/show messages retry after reparenting,
            // before any hover or first background paint is needed to reveal it.
            if (GetAncestor(Handle, 2) != host.Handle) return;
            if (_compositionRoot != host.Handle) { _opaque = false; _compositionRoot = host.Handle; }
            if (_opaque && (style & 0x00080000) != 0) return;
            SetExtendedStyle(style | 0x00080000);
            _opaque = SetLayeredWindowAttributes(Handle, 0, 255, 2);
            if (!_opaque) host.BackdropPaintFailed(new Win32Exception(Marshal.GetLastWin32Error(), "Opaque native content needs layered-child support."));
            else RedrawWindow(Handle, 0, 0, 0x185);
        }
        finally { _updatingComposition = false; }
    }
    /// <inheritdoc />
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg is 0x18 or 0x47) EnsureOpaque(); // SHOWWINDOW / WINDOWPOSCHANGED, after native parenting.
    }
    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e) { _opaque = false; _compositionRoot = 0; base.OnHandleDestroyed(e); }
    /// <inheritdoc />
    protected override void OnPaintBackground(PaintEventArgs e) { EnsureOpaque(); base.OnPaintBackground(e); }
    private long ExtendedStyle() => (IntPtr.Size == 8 ? GetWindowLongPtr(Handle, -20) : GetWindowLong(Handle, -20)).ToInt64();
    private void SetExtendedStyle(long style) { if (IntPtr.Size == 8) SetWindowLongPtr(Handle, -20, (nint)style); else SetWindowLong(Handle, -20, (nint)style); }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern nint GetWindowLong(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint window, int index, nint value);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern nint SetWindowLong(nint window, int index, nint value);
    [DllImport("user32.dll")] private static extern bool RedrawWindow(nint window, nint rectangle, nint region, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetLayeredWindowAttributes(nint window, uint color, byte alpha, uint flags);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
}
