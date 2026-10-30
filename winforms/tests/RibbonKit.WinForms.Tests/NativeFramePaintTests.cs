using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class NativeFramePaintTests
{
    [Theory] [InlineData(FormBorderStyle.Sizable)] [InlineData(FormBorderStyle.FixedSingle)] [InlineData(FormBorderStyle.Fixed3D)]
    public void Solid_frame_preserves_native_rect_queries_and_reclaims_only_committed_caption(FormBorderStyle style) => FoundationTests.Sta(() =>
    {
        using var form = new CalculationProbe { FormBorderStyle = style };
        _ = form.Handle;
        var simple = form.Calculate(false); var extended = form.Calculate(true);
        Assert.Equal(100, extended.Top);
        Assert.True(simple.Top > extended.Top);
        Assert.Equal(extended.Left, simple.Left); Assert.Equal(extended.Right, simple.Right); Assert.Equal(extended.Bottom, simple.Bottom);
        Assert.False(form.Visible);
    });
    [Theory] [InlineData(RibbonWindowBackdrop.None)] [InlineData(RibbonWindowBackdrop.Mica)]
    public void Maximized_custom_client_uses_resize_insets_without_native_caption_height(RibbonWindowBackdrop material) => FoundationTests.Sta(() =>
    {
        using var form = new CalculationProbe { Backdrop = material }; _ = form.Handle;
        var screen = Screen.FromHandle(form.Handle); var limits = form.Limits();
        Assert.Equal(limits.MaxPosition.X - screen.WorkingArea.Left + screen.Bounds.Left,
            limits.MaxPosition.Y - screen.WorkingArea.Top + screen.Bounds.Top);
        Assert.Equal(limits.MaxSize.X - screen.WorkingArea.Width, limits.MaxSize.Y - screen.WorkingArea.Height);
        Assert.False(form.Visible);
    });
    private sealed class CalculationProbe : RibbonForm
    {
        internal MinMaxInfo Limits()
        {
            nint data = Marshal.AllocHGlobal(Marshal.SizeOf<MinMaxInfo>());
            try
            {
                Marshal.StructureToPtr(new MinMaxInfo(), data, false);
                var message = Message.Create(Handle, 0x24, 0, data); WndProc(ref message);
                return Marshal.PtrToStructure<MinMaxInfo>(data);
            }
            finally { Marshal.FreeHGlobal(data); }
        }
        internal Rectangle Calculate(bool extended)
        {
            nint data = Marshal.AllocHGlobal(128), position = Marshal.AllocHGlobal(64);
            try
            {
                Marshal.Copy(new byte[128], 0, data, 128); Marshal.Copy(new byte[64], 0, position, 64);
                Marshal.StructureToPtr(new NativeRect { Left = 100, Top = 100, Right = 1000, Bottom = 700 }, data, false);
                if (extended) Marshal.WriteIntPtr(data, 48, position);
                var message = Message.Create(Handle, 0x83, extended ? 1 : 0, data); WndProc(ref message);
                var result = Marshal.PtrToStructure<NativeRect>(data);
                return Rectangle.FromLTRB(result.Left, result.Top, result.Right, result.Bottom);
            }
            finally { Marshal.FreeHGlobal(data); Marshal.FreeHGlobal(position); }
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo { internal Point Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }

    [Fact]
    public void Nonclient_repaint_does_not_overwrite_the_reclaimed_top_client_edge() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Location = new(180, 140), Size = new(760, 500) };
        using var ribbon = new RibbonControl();
        var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(new RibbonGroup { Text = "Commands" }); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Show(); form.Activate(); Application.DoEvents(); form.Refresh(); ribbon.Refresh();
        var origin = form.PointToScreen(Point.Empty);
        using var before = new Bitmap(400, 5); using var after = new Bitmap(400, 5);
        using (var graphics = Graphics.FromImage(before)) graphics.CopyFromScreen(new(origin.X + 150, origin.Y + 1), Point.Empty, before.Size);
        // Force the frame paint without a following client paint repairing it.
        SendMessage(form.Handle, 0x85, 1, 0);
        using (var graphics = Graphics.FromImage(after)) graphics.CopyFromScreen(new(origin.X + 150, origin.Y + 1), Point.Empty, after.Size);
        int changed = 0;
        for (int y = 0; y < before.Height; y++) for (int x = 0; x < before.Width; x++)
            if (before.GetPixel(x, y).ToArgb() != after.GetPixel(x, y).ToArgb()) changed++;
        Assert.True(changed < 50, $"Native frame paint changed {changed} custom-client pixels before its next paint.");
    });
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint window, uint message, nint parameter, nint flags);
}
