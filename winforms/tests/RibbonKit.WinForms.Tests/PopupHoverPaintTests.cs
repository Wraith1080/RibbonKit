using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PopupHoverPaintTests
{
    [Fact]
    public void Pointer_motion_inside_the_same_menu_item_does_not_repeat_invalidation() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Visible = false };
        using var menu = new RibbonDropDownItem(); menu.Items.AddRange(new RibbonButtonItem { Text = "First" }, new RibbonButtonItem { Text = "Second" });
        using var surface = new RibbonPopupSurface(ribbon, menu, new(1000, 700)) { Visible = false };
        _ = ribbon.Handle; _ = surface.Handle;
        var move = surface.GetType().GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var leave = surface.GetType().GetMethod("OnMouseLeave", BindingFlags.Instance | BindingFlags.NonPublic)!;
        int invalidated = 0; surface.Invalidated += (_, _) => invalidated++;
        var first = surface.Targets[0].Bounds;
        void Move(int x, int y) => move.Invoke(surface, [new MouseEventArgs(MouseButtons.None, 0, x, y, 0)]);
        Move(first.Left + 4, first.Top + 4); Assert.Equal(1, invalidated);
        for (int i = 0; i < 5; i++) Move(first.Left + 5 + i, first.Top + 5);
        Assert.Equal(1, invalidated);
        var second = surface.Targets[1].Bounds; Move(second.Left + 4, second.Top + 4); Assert.Equal(2, invalidated);
        leave.Invoke(surface, [EventArgs.Empty]); Assert.Equal(3, invalidated);
        leave.Invoke(surface, [EventArgs.Empty]); Assert.Equal(3, invalidated);
        Assert.False(surface.Visible); Assert.False(ribbon.Visible);
    });

    [Fact]
    public void Unchanged_shadow_does_not_repeat_native_positioning_but_tracks_move_and_z_order() => FoundationTests.Sta(() =>
    {
        // A native, nonactivating popup outside every display. No desktop input
        // or foreground change; exercise actual positioning notifications.
        using var popup = new OffscreenPopup { StartPosition = FormStartPosition.Manual, Bounds = new(-30000, -30000, 240, 180), ShowInTaskbar = false, FormBorderStyle = FormBorderStyle.None };
        using var ribbon = new RibbonControl();
        popup.Show();
        using var chrome = new PopupChromeWindow(); chrome.Update(popup, ribbon, 4);
        using var probe = new PositionProbe(chrome.Handle);
        for (int i = 0; i < 5; i++) chrome.Update(popup, ribbon, 4);
        Assert.Equal(0, probe.Changes);
        popup.Left += 20; chrome.Update(popup, ribbon, 4); Assert.Equal(1, probe.Changes);
        Assert.Equal(chrome.Handle, GetWindow(popup.Handle, 2));
        SetWindowPos(chrome.Handle, 0, 0, 0, 0, 0, 0x13); // NOSIZE | NOMOVE | NOACTIVATE; disturb only the test shadow's Z order.
        Assert.NotEqual(chrome.Handle, GetWindow(popup.Handle, 2));
        chrome.Update(popup, ribbon, 4); Assert.Equal(chrome.Handle, GetWindow(popup.Handle, 2));
    });
    private sealed class OffscreenPopup : Form { protected override bool ShowWithoutActivation => true; }
    private sealed class PositionProbe : NativeWindow, IDisposable
    {
        internal int Changes;
        internal PositionProbe(nint handle) => AssignHandle(handle);
        protected override void WndProc(ref Message m) { if (m.Msg == 0x46) Changes++; base.WndProc(ref m); }
        public void Dispose() => ReleaseHandle();
    }
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
}
