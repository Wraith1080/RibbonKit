using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PopupWindowCreationTests
{
    [Fact]
    public void Popup_configuration_does_not_destroy_and_recreate_its_native_window() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl();
        // Initialize WinForms' shared hidden owner before observing this popup.
        using (var warmup = new RibbonPopupWindow(ribbon, () => { })) _ = warmup.Handle;
        var destroyed = new List<nint>();
        Hook callback = (code, window, data) =>
        {
            if (code == 4) destroyed.Add(window); // HCBT_DESTROYWND
            return CallNextHookEx(0, code, window, data);
        };
        nint hook = SetWindowsHookEx(5, callback, 0, GetCurrentThreadId()); // WH_CBT, this STA only
        Assert.NotEqual(0, hook);
        RibbonPopupWindow? popup = null;
        try
        {
            popup = new RibbonPopupWindow(ribbon, () => { });
        }
        finally
        {
            _ = UnhookWindowsHookEx(hook);
            GC.KeepAlive(callback);
        }
        using (popup)
        {
            Assert.True(popup.IsHandleCreated);
            Assert.False(popup.AutoClose);
            Assert.False(popup.DropShadowEnabled);
            Assert.Equal(0u, GetClassLong(popup.Handle, -26) & 0x20000u); // no CS_DROPSHADOW
            Assert.Empty(destroyed);
        }
    });

    private delegate nint Hook(int code, nint window, nint data);
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetWindowsHookEx(int type, Hook callback, nint module, uint thread);
    [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint hook, int code, nint window, nint data);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll", EntryPoint = "GetClassLongW")] private static extern uint GetClassLong(nint window, int index);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
