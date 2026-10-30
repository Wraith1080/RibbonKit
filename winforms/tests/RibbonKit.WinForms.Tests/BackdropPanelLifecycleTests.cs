using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackdropPanelLifecycleTests
{
    [Fact]
    public void Solid_host_uses_an_ordinary_panel_even_when_its_handle_was_created_before_parenting() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Size = new(800, 500) };
        using var content = new RibbonBackdropPanel { Dock = DockStyle.Fill };
        content.Controls.Add(new TextBox { Multiline = true, Dock = DockStyle.Fill, Text = "Visible on the first frame" });
        content.Controls.Add(new StatusStrip { Items = { new ToolStripStatusLabel("Ready") } });
        nint handle = content.Handle;
        var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }; wrapper.Controls.Add(content); form.Controls.Add(wrapper);
        form.Show();
        Assert.Equal(handle, content.Handle);
        Assert.Equal(0, Style(content.Handle) & 0x02080000);
    });

    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Material_initializes_a_precreated_panel_before_hover_or_first_background_paint(RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Size = new(800, 500), Backdrop = backdrop };
        using var content = new RibbonBackdropPanel { Dock = DockStyle.Fill };
        var document = new TextBox { Multiline = true, Dock = DockStyle.Fill, Text = "Retained native document" }; content.Controls.Add(document);
        nint handle = content.Handle, editor = document.Handle;
        var wrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }; wrapper.Controls.Add(content); form.Controls.Add(wrapper);
        bool observed = false;
        form.Shown += (_, _) =>
        {
            observed = true;
            Assert.Equal(backdrop, form.EffectiveBackdrop);
            Assert.True(GetLayeredWindowAttributes(content.Handle, out _, out byte alpha, out _));
            Assert.Equal(255, alpha);
        };
        form.Show(); Application.DoEvents(); Assert.True(observed);
        foreach (var next in new[] { RibbonWindowBackdrop.None, backdrop, RibbonWindowBackdrop.None })
        {
            form.Backdrop = next;
            Assert.Equal(handle, content.Handle); Assert.Equal(editor, document.Handle);
            Assert.Equal("Retained native document", document.Text);
            Assert.Equal(next == RibbonWindowBackdrop.None ? 0 : 0x00080000, Style(content.Handle) & 0x02080000);
        }
    });

    private static long Style(nint window) => (IntPtr.Size == 8 ? GetWindowLongPtr(window, -20) : GetWindowLong(window, -20)).ToInt64();
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern nint GetWindowLong(nint window, int index);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetLayeredWindowAttributes(nint window, out uint key, out byte alpha, out uint flags);
}
