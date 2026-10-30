using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

// These checks create hidden HWNDs without showing or activating a window.
public sealed class BackdropBitmapAlphaTests
{
    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Native_bitmap_alpha_preserves_caption_reapplies_after_restart_and_clears_on_release(RibbonWindowBackdrop material) => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26100) || SystemInformation.HighContrast) return;
        using var form = new Probe { Size = new(800, 600), Backdrop = material };
        nint handle = form.Handle;
        Assert.Equal(material, form.EffectiveBackdrop); Assert.True(form.UsesBackdropBitmapAlpha);
        Assert.Equal(0x00C00000, GetWindowLong(handle, -16) & 0x00C00000);
        AssertMaterial(form, material);

        int disabled = 1; Assert.Equal(0, DwmSetWindowAttribute(handle, 38, ref disabled, sizeof(int)));
        SendMessage(handle, 0x31E, 0, 0); // Composition restart must reapply an unchanged preference.
        AssertMaterial(form, material); Assert.True(form.UsesBackdropBitmapAlpha); Assert.Equal(handle, form.Handle);
        form.RecreateNativeHandle();
        AssertMaterial(form, material); Assert.True(form.UsesBackdropBitmapAlpha);
        Assert.Equal(0x00C00000, GetWindowLong(form.Handle, -16) & 0x00C00000);

        handle = form.Handle; form.Backdrop = RibbonWindowBackdrop.None;
        AssertMaterial(form, RibbonWindowBackdrop.None); Assert.False(form.UsesBackdropBitmapAlpha);
        Assert.Equal(RibbonWindowBackdrop.None, form.EffectiveBackdrop); Assert.Equal(handle, form.Handle);
        Assert.Equal(0x00C00000, GetWindowLong(handle, -16) & 0x00C00000);

        form.Backdrop = material; form.FormBorderStyle = FormBorderStyle.None;
        Assert.False(form.UsesBackdropBitmapAlpha);
        Assert.Equal(material, form.Backdrop); Assert.Equal(RibbonWindowBackdrop.None, form.EffectiveBackdrop);
        Assert.False(form.Visible);
    });

    private static void AssertMaterial(RibbonForm form, RibbonWindowBackdrop expected)
    {
        // The redirection-alpha attribute is set-only. Inspect the real caption
        // style and queryable material alongside the owned mode instead.
        Assert.Equal(0, DwmGetWindowAttribute(form.Handle, 38, out int native, sizeof(int)));
        Assert.Equal(expected == RibbonWindowBackdrop.Mica ? 2 : expected == RibbonWindowBackdrop.Acrylic ? 3 : 1, native);
        Assert.Equal(expected, form.EffectiveBackdrop);
    }
    private sealed class Probe : RibbonForm { internal void RecreateNativeHandle() => RecreateHandle(); }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wparam, nint lparam);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}
