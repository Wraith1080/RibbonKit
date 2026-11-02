using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class WindowBackdropCloseTests
{
    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica, false)] [InlineData(RibbonWindowBackdrop.Mica, true)]
    [InlineData(RibbonWindowBackdrop.Acrylic, false)] [InlineData(RibbonWindowBackdrop.Acrylic, true)]
    public void Closing_or_disposing_keeps_material_and_bitmap_alpha_until_native_destruction(RibbonWindowBackdrop material, bool directDispose) => FoundationTests.Sta(() =>
    {
        if (!Supported) return;
        using var form = Create(material); form.Show(); Application.DoEvents();
        Assert.Equal(material, form.EffectiveBackdrop); bool bitmapAlpha = form.UsesBackdropBitmapAlpha;
        form.Observe = true;
        if (directDispose) form.Dispose(); else form.Close();
        var destroyed = Assert.Single(form.Destructions);
        Assert.Equal(material == RibbonWindowBackdrop.Mica ? 2 : 3, destroyed.Material);
        if (bitmapAlpha) Assert.True(destroyed.BitmapAlpha);
        Assert.True(form.IsDisposed); Assert.Equal(RibbonWindowBackdrop.None, form.EffectiveBackdrop);
    });

    [Fact]
    public void Cancelled_close_keeps_the_live_material_editor_and_selection_until_a_later_accepted_close() => FoundationTests.Sta(() =>
    {
        if (!Supported) return;
        using var form = Create(RibbonWindowBackdrop.Mica); var combo = (RibbonComboBoxItem)form.Ribbon!.Tabs[0].Groups[0].Items[0];
        form.Show(); Application.DoEvents(); var editor = form.Ribbon.Editors.EditorFor(combo)!; var handle = editor.Handle;
        bool cancel = true; int closing = 0, closed = 0;
        form.FormClosing += (_, args) => { closing++; args.Cancel = cancel; }; form.FormClosed += (_, _) => closed++;
        form.Observe = true; form.Close();
        Assert.Equal(1, closing); Assert.Equal(0, closed); Assert.False(form.IsDisposed); Assert.True(form.Visible);
        Assert.Empty(form.Destructions); Assert.Equal(RibbonWindowBackdrop.Mica, form.EffectiveBackdrop); Assert.Equal(2, Read(form.Handle, 38));
        Assert.Equal(handle, editor.Handle); Assert.Equal("Retained text", combo.Value);
        cancel = false; form.Close(); Assert.Equal(2, closing); Assert.Equal(1, closed); Assert.Equal(2, Assert.Single(form.Destructions).Material);
    });

    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Handle_recreation_reestablishes_material_and_explicit_solid_release_still_updates_the_live_window(RibbonWindowBackdrop material) => FoundationTests.Sta(() =>
    {
        if (!Supported) return;
        using var form = Create(material); form.Show(); Application.DoEvents();
        var combo = (RibbonComboBoxItem)form.Ribbon!.Tabs[0].Groups[0].Items[0];
        form.RebuildHandle(); Application.DoEvents(); Assert.Equal(material, form.EffectiveBackdrop); Assert.Equal("Retained text", combo.Value);
        Assert.Equal(material == RibbonWindowBackdrop.Mica ? 2 : 3, Read(form.Handle, 38));
        form.Backdrop = RibbonWindowBackdrop.None;
        Assert.Equal(RibbonWindowBackdrop.None, form.EffectiveBackdrop); Assert.Equal(1, Read(form.Handle, 38));
        Assert.False(form.UsesBackdropBitmapAlpha);
        Assert.True(form.Visible); Assert.Equal("Retained text", combo.Value);
    });

    private static bool Supported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) && !SystemInformation.HighContrast;
    private static Probe Create(RibbonWindowBackdrop material)
    {
        var form = new Probe { Backdrop = material, StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 850, 500) };
        var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup();
        var combo = new RibbonComboBoxItem { Text = "Font", IsEditable = true, Value = "Retained text" }; group.Items.Add(combo); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon; return form;
    }
    private sealed class Probe : RibbonForm
    {
        internal bool Observe;
        internal readonly List<(int Material, bool BitmapAlpha)> Destructions = [];
        internal void RebuildHandle() => RecreateHandle();
        protected override void WndProc(ref Message message)
        {
            // WM_DESTROY entry observes the last configuration seen by DWM.
            // Teardown must not publish an opaque-black frame before this point.
            if (Observe && message.Msg == 0x2) Destructions.Add((Read(message.HWnd, 38), UsesBackdropBitmapAlpha));
            base.WndProc(ref message);
        }
    }
    private static int Read(nint window, int attribute) => DwmGetWindowAttribute(window, attribute, out int value, sizeof(int)) >= 0 ? value : -1;
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}
