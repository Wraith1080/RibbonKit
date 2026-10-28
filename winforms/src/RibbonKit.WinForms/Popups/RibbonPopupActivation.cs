using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// ToolStrip activation transfers keyboard focus to an owned window. During that
// synchronous opening, keep the host's nonclient frame active, as one window
// family. Real activation of another application still follows normal handling.
internal sealed class RibbonPopupActivation : NativeWindow, IDisposable
{
    private readonly nint _form, _popup;
    internal RibbonPopupActivation(Form? form, Control popup)
    {
        _popup = popup.Handle;
        if (form is { IsHandleCreated: true }) { _form = form.Handle; AssignHandle(_form); }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x86 && m.WParam == 0 && WithinFamily(GetForegroundWindow())) m.WParam = 1;
        base.WndProc(ref m);
    }
    private bool WithinFamily(nint window)
    {
        for (int depth = 0; window != 0 && depth < 32; depth++, window = GetWindow(window, 4))
            if (window == _form || window == _popup) return true;
        return false;
    }
    public void Dispose() { if (Handle != 0) ReleaseHandle(); }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint relation);
}
