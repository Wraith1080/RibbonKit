using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

/// <summary>An opaque composited content panel for native GDI controls over a RibbonForm backdrop. Add application controls normally; they retain native handles, input and ownership. A Windows 8-or-later compatibility manifest is required for layered child windows.</summary>
public class RibbonBackdropPanel : Panel
{
    private bool _opaque;
    /// <summary>Creates an opaque native content surface. The application owns its child controls and normal Panel layout.</summary>
    public RibbonBackdropPanel() { BackColor = SystemColors.Window; }
    /// <inheritdoc />
    protected override CreateParams CreateParams
    {
        get { var value = base.CreateParams; value.ExStyle |= 0x02080000; return value; } // COMPOSITED | LAYERED
    }
    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e); _opaque = false; EnsureOpaque();
    }
    internal void EnsureOpaque()
    {
        if (_opaque || !IsHandleCreated || FindForm() is not { IsHandleCreated: true } form || GetAncestor(Handle, 2) != form.Handle) return;
        _opaque = SetLayeredWindowAttributes(Handle, 0, 255, 2);
        if (!_opaque && form is RibbonForm host) host.BackdropPaintFailed(new Win32Exception(Marshal.GetLastWin32Error(), "Opaque native content needs layered-child support."));
    }
    /// <inheritdoc />
    protected override void OnPaintBackground(PaintEventArgs e) { EnsureOpaque(); base.OnPaintBackground(e); }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetLayeredWindowAttributes(nint window, uint color, byte alpha, uint flags);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
}
