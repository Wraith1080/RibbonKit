using System.Runtime.InteropServices;
using RibbonKit.WinForms.Design;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePreviewLayerTests
{
    [Fact]
    public void Loading_does_not_create_root_handles_or_preview_frames() => FoundationTests.Sta(() =>
    {
        using var root = new Form(); using var layer = new RibbonBackstagePreviewLayer(root); int frames = 0;
        Assert.False(layer.Present(new(0, 0, 500, 300), () => { frames++; return Frame(new(500, 300), Color.Blue); }));
        Assert.False(root.IsHandleCreated); Assert.False(layer.IsHandleCreated); Assert.Equal(0, frames); Assert.Empty(root.Controls);
    });

    [Fact]
    public void Preview_is_an_opaque_passive_native_child_outside_the_authored_control_tree() => FoundationTests.Sta(() =>
    {
        using var root = new Form { ClientSize = new(700, 500) }; var editor = new TextBox { Text = "Native content" }; root.Controls.Add(editor);
        _ = root.Handle; var editorHandle = editor.Handle; editor.Select(2, 4); var controls = root.Controls.Cast<Control>().ToArray();
        using var layer = new RibbonBackstagePreviewLayer(root); var bounds = new Rectangle(0, 40, 700, 460);
        Assert.True(layer.Present(bounds, () => Frame(bounds.Size, Color.DarkCyan)));
        Assert.Equal(root.Handle, GetParent(layer.Handle)); Assert.Null(layer.Parent); Assert.Null(layer.Site);
        Assert.Equal(controls, root.Controls.Cast<Control>()); Assert.Equal(bounds, layer.Bounds);
        Assert.False(layer.CanSelect); Assert.False(layer.TabStop); Assert.False(layer.Enabled);
        Assert.Equal(0, GetWindowLong(layer.Handle, -20) & 0x20); // No transparent-window underlay repaint.
        Assert.Equal((nint)(-1), SendMessage(layer.Handle, 0x84, 0, 0)); Assert.Equal((nint)3, SendMessage(layer.Handle, 0x21, 0, 0));
        Assert.Equal(editorHandle, editor.Handle); Assert.Equal("Native content", editor.Text); Assert.Equal(2, editor.SelectionStart); Assert.Equal(4, editor.SelectionLength);
        var handle = layer.Handle; Assert.True(layer.Present(bounds, () => Frame(bounds.Size, Color.DarkBlue))); Assert.Equal(handle, layer.Handle);
        using var pixels = Print(layer); Assert.Equal(Color.DarkBlue.ToArgb(), pixels.GetPixel(300, 200).ToArgb());
        layer.HidePreview(); Assert.False(layer.IsPresented); Assert.False(IsWindowVisible(handle)); Assert.Equal(controls, root.Controls.Cast<Control>());
    });

    [Fact]
    public void Retained_pixels_survive_underlay_refresh_and_failed_rendering_without_recapturing_content() => FoundationTests.Sta(() =>
    {
        using var root = new Form { ClientSize = new(700, 500) }; _ = root.Handle;
        using var layer = new RibbonBackstagePreviewLayer(root); var bounds = new Rectangle(0, 40, 700, 460); int renders = 0;
        layer.Present(bounds, () => { renders++; return Frame(bounds.Size, Color.DarkCyan); }); var handle = layer.Handle;
        root.Invalidate(true); root.Update(); layer.Invalidate(); layer.Update();
        Assert.Throws<InvalidOperationException>(() => layer.Present(bounds, () => throw new InvalidOperationException("Incomplete frame")));
        Assert.True(layer.IsPresented); Assert.Equal(handle, layer.Handle); Assert.Equal(1, renders);
        using var pixels = Print(layer); Assert.Equal(Color.DarkCyan.ToArgb(), pixels.GetPixel(400, 200).ToArgb());
    });

    [Fact]
    public void Retained_adorner_paint_keeps_caption_origin_and_graphics_clip_without_rendering_again() => FoundationTests.Sta(() =>
    {
        using var root = new Form(); _ = root.Handle; using var layer = new RibbonBackstagePreviewLayer(root); int renders = 0;
        var bounds = new Rectangle(0, 40, 500, 300); layer.Present(bounds, () => { renders++; return Frame(bounds.Size, Color.DarkCyan); });
        using var pixels = new Bitmap(600, 400); using var graphics = Graphics.FromImage(pixels); graphics.Clear(Color.Magenta);
        graphics.SetClip(new Rectangle(50, 100, 200, 80)); Assert.True(layer.PaintRetained(graphics, new(20, 30)));
        Assert.Equal(Color.Magenta.ToArgb(), pixels.GetPixel(30, 35).ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), pixels.GetPixel(49, 110).ToArgb());
        Assert.Equal(Color.DarkCyan.ToArgb(), pixels.GetPixel(60, 110).ToArgb()); Assert.Equal(Color.Magenta.ToArgb(), pixels.GetPixel(251, 110).ToArgb());
        Assert.Equal(1, renders); layer.HidePreview(); Assert.False(layer.PaintRetained(graphics, Point.Empty));
    });

    [Fact]
    public void Opaque_layer_clips_underlying_sibling_drawing_at_the_native_window_boundary() => FoundationTests.Sta(() =>
    {
        using var root = new Form { ClientSize = new(500, 350), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new(40, 40) };
        var behind = new Panel { Dock = DockStyle.Fill, BackColor = Color.Magenta }; root.Controls.Add(behind); root.Show();
        using var layer = new RibbonBackstagePreviewLayer(root); var bounds = new Rectangle(0, 50, 500, 300);
        layer.Present(bounds, () => Frame(bounds.Size, Color.White));
        var dc = GetDC(behind.Handle);
        try
        {
            Assert.True(PtVisible(dc, 20, 20), "The exposed sibling must be drawable above the preview.");
            Assert.False(PtVisible(dc, 20, 100), "The ribbon/document must not paint through the opaque preview.");
        }
        finally { ReleaseDC(behind.Handle, dc); }
        layer.HidePreview(); dc = GetDC(behind.Handle);
        try { Assert.True(PtVisible(dc, 20, 100), "Closing the preview restores normal sibling drawing."); }
        finally { ReleaseDC(behind.Handle, dc); }
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Opaque_layer_also_clips_parent_background_drawing_below_the_retained_caption(bool integrated) => FoundationTests.Sta(() =>
    {
        using Form root = integrated ? new RibbonForm() : new Form(); root.ClientSize = new(500, 350);
        root.ShowInTaskbar = false; root.StartPosition = FormStartPosition.Manual; root.Location = new(40, 40); root.Show();
        using var layer = new RibbonBackstagePreviewLayer(root); var bounds = new Rectangle(0, 40, root.ClientSize.Width, root.ClientSize.Height - 40);
        layer.Present(bounds, () => Frame(bounds.Size, Color.White)); var dc = GetDC(root.Handle);
        try
        {
            Assert.True(PtVisible(dc, 20, 20), "Caption-area parent drawing must remain available.");
            Assert.False(PtVisible(dc, 20, 100), "Parent erasure must not expose background through the preview body.");
        }
        finally { ReleaseDC(root.Handle, dc); }
        layer.HidePreview();
    });

    [Fact]
    public void Root_handle_recreation_and_disposal_release_the_temporary_window_safely() => FoundationTests.Sta(() =>
    {
        using var root = new RecreatedRoot(); _ = root.Handle; using var layer = new RibbonBackstagePreviewLayer(root);
        layer.Present(new(0, 0, 200, 200), () => Frame(new(200, 200), Color.White));
        root.Rebuild(); Assert.False(layer.IsPresented);
        Assert.True(layer.Present(new(0, 0, 200, 200), () => Frame(new(200, 200), Color.White)));
        Assert.Equal(root.Handle, GetParent(layer.Handle)); root.Dispose(); Assert.True(layer.IsDisposed); Assert.False(layer.IsHandleCreated);
        Assert.False(layer.Present(new(0, 0, 200, 200), () => throw new Exception("No late rendering")));
    });

    private static Bitmap Frame(Size size, Color color) { var frame = new Bitmap(size.Width, size.Height); using var graphics = Graphics.FromImage(frame); graphics.Clear(color); return frame; }
    private static Bitmap Print(Control layer)
    {
        var pixels = new Bitmap(layer.Width, layer.Height); using var graphics = Graphics.FromImage(pixels); var dc = graphics.GetHdc();
        try { SendMessage(layer.Handle, 0x318, dc, 4); } // PRINTCLIENT: only the synthetic preview, never app pages.
        finally { graphics.ReleaseHdc(dc); }
        return pixels;
    }
    private sealed class RecreatedRoot : Form { internal void Rebuild() => RecreateHandle(); }
    [DllImport("user32.dll")] private static extern nint GetParent(nint window);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint window, uint message, nint wparam, nint lparam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern bool PtVisible(nint dc, int x, int y);
}
