using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class OptionsListRepaintTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Finished_rows_publish_once_with_text_and_focus_in_the_correct_row(int dpi) => FoundationTests.Sta(() =>
    {
        int Scale(int value) => (int)Math.Round(value * dpi / 96d);
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue };
        using var list = new RibbonOptionsListBox { DrawMode = DrawMode.OwnerDrawFixed, BackColor = Color.White };
        list.BindTheme(ribbon);
        using var font = new Font("Segoe UI", Scale(12), GraphicsUnit.Pixel);
        using var actual = new Bitmap(Scale(300), Scale(170));
        using var expected = new Bitmap(actual.Width, actual.Height);
        using var target = Graphics.FromImage(actual);
        using var direct = Graphics.FromImage(expected);
        var bounds = new Rectangle(Scale(13), Scale(74), Scale(260), Scale(36));
        var state = DrawItemState.Selected | DrawItemState.Focus;
        target.Clear(Color.Fuchsia);
        direct.Clear(Color.Fuchsia);
        int callbacks = 0;
        list.DrawItem += (_, e) =>
        {
            Assert.Equal(3, e.Index);
            Assert.Equal(state, e.State);
            Assert.Same(font, e.Font);
            Paint(e);
            Assert.Equal(Color.Fuchsia.ToArgb(), actual.GetPixel(bounds.Left + 2, bounds.Top + 2).ToArgb());
            callbacks++;
        };
        using var args = new DrawItemEventArgs(target, font, bounds, 3, state, Color.White, Color.Navy);
        using var reference = new DrawItemEventArgs(direct, font, bounds, 3, state, Color.White, Color.Navy);
        Invoke(list, args);
        Paint(reference);
        Assert.Equal(1, callbacks);
        var textBounds = Rectangle.Inflate(bounds, -Scale(18), -Scale(3));
        for (int y = 0; y < actual.Height; y++)
        for (int x = 0; x < actual.Width; x++)
            if (!textBounds.Contains(x, y))
                Assert.Equal(expected.GetPixel(x, y).ToArgb(), actual.GetPixel(x, y).ToArgb());

        // The memory DC and bitmap adapter can use different antialiasing.
        // Check visible glyph placement without requiring identical edge colors.
        var actualInk = TextInk(actual);
        var expectedInk = TextInk(expected);
        Assert.True(actualInk.Count > 20);
        Assert.InRange(actualInk.Count, expectedInk.Count * 3 / 4, expectedInk.Count * 5 / 4);
        Assert.InRange(Math.Abs(actualInk.Bounds.Left - expectedInk.Bounds.Left), 0, 1);
        Assert.InRange(Math.Abs(actualInk.Bounds.Top - expectedInk.Bounds.Top), 0, 1);
        Assert.InRange(Math.Abs(actualInk.Bounds.Right - expectedInk.Bounds.Right), 0, 1);
        Assert.InRange(Math.Abs(actualInk.Bounds.Bottom - expectedInk.Bounds.Bottom), 0, 1);

        (Rectangle Bounds, int Count) TextInk(Bitmap image)
        {
            int count = 0;
            var ink = Rectangle.Empty;
            for (int y = textBounds.Top; y < textBounds.Bottom; y++)
            for (int x = textBounds.Left; x < textBounds.Right; x++)
            {
                var pixel = image.GetPixel(x, y);
                if (pixel.R < 160 || pixel.G < 160) continue;
                var point = new Rectangle(x, y, 1, 1);
                ink = count == 0 ? point : Rectangle.Union(ink, point);
                count++;
            }
            return (ink, count);
        }
        void Paint(DrawItemEventArgs e)
        {
            var row = e.Bounds;
            e.Graphics.FillRectangle(Brushes.Navy, row);
            e.Graphics.FillRectangle(Brushes.Lime, row.Left + Scale(3), row.Top + Scale(8), Scale(9), Scale(9));
            TextRenderer.DrawText(e.Graphics, "Native row three", font, Rectangle.Inflate(row, -Scale(18), 0),
                Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            e.DrawFocusRectangle();
        }
    });

    [Fact]
    public void Failed_row_callback_never_publishes_partial_paint_and_next_callback_can_draw() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue };
        using var list = new RibbonOptionsListBox { DrawMode = DrawMode.OwnerDrawFixed };
        list.BindTheme(ribbon);
        using var image = new Bitmap(220, 110);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Fuchsia);
        var row = new Rectangle(8, 52, 200, 36);
        bool reject = true;
        list.DrawItem += (_, e) =>
        {
            e.Graphics.FillRectangle(Brushes.Navy, e.Bounds);
            if (reject) throw new InvalidOperationException("Rejected paint");
        };
        using var args = new DrawItemEventArgs(graphics, list.Font, row, 1, DrawItemState.Selected);
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Invoke(list, args)).InnerException);
        Assert.Equal(Color.Fuchsia.ToArgb(), image.GetPixel(20, 60).ToArgb());
        reject = false;
        Invoke(list, args);
        Assert.Equal(Color.Navy.ToArgb(), image.GetPixel(20, 60).ToArgb());
    });

    [Fact]
    public void Hover_invalidates_only_changed_rows_without_native_erase_and_keeps_selection() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue };
        using var host = new Form { Bounds = new(-16000, -16000, 350, 240), ShowInTaskbar = false };
        using var list = new RibbonOptionsListBox
        {
            Bounds = new(0, 0, 280, 180), DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 30, IntegralHeight = false
        };
        list.BindTheme(ribbon);
        for (int i = 0; i < 12; i++) list.Items.Add("Command " + i);
        list.DrawItem += (_, e) => e.Graphics.FillRectangle(Brushes.Navy, e.Bounds);
        host.Controls.Add(list);
        host.Show();
        list.SelectedIndex = 0;
        list.Refresh();
        Application.DoEvents();

        var handle = list.Handle;
        using var trace = new EraseTrace(handle);
        var invalidated = new List<Rectangle>();
        list.Invalidated += (_, e) => invalidated.Add(e.InvalidRect);
        SendMessage(handle, 0x200, 0, (45 << 16) | 20);
        GetUpdateRect(handle, out _, true);
        Assert.Equal(0, trace.Erases);
        Assert.Equal(1, list.HotIndex);
        Assert.Equal(0, list.SelectedIndex);
        Assert.Equal(list.GetItemRectangle(1), Assert.Single(invalidated));

        list.Update();
        invalidated.Clear();
        trace.Erases = 0;
        SendMessage(handle, 0x200, 0, (75 << 16) | 20);
        GetUpdateRect(handle, out _, true);
        Assert.Equal(0, trace.Erases);
        Assert.Equal(2, list.HotIndex);
        Assert.Equal(0, list.SelectedIndex);
        Assert.Equal(new[] { list.GetItemRectangle(1), list.GetItemRectangle(2) }, invalidated);

        list.Update();
        trace.Erases = 0;
        SendMessage(handle, 0x201, 1, (75 << 16) | 20);
        SendMessage(handle, 0x202, 0, (75 << 16) | 20);
        list.Update();
        Assert.Equal(2, list.SelectedIndex);
        Assert.Equal(0, trace.Erases);
        SendMessage(handle, 0x100, 0x28, 0);
        SendMessage(handle, 0x101, 0x28, 0);
        Assert.Equal(3, list.SelectedIndex);
        list.TopIndex = 4;
        Assert.Equal(handle, list.Handle);
        Assert.Equal(3, list.SelectedIndex);
        list.Items.Clear();
        Assert.Equal(-1, list.HotIndex);
    });

    private static void Invoke(ListBox list, DrawItemEventArgs args) =>
        typeof(RibbonOptionsListBox).GetMethod("OnDrawItem", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(list, [args]);

    private sealed class EraseTrace : NativeWindow, IDisposable
    {
        internal int Erases;
        internal EraseTrace(nint handle) => AssignHandle(handle);
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x14) Erases++; // WM_ERASEBKGND
            base.WndProc(ref m);
        }
        public void Dispose() => ReleaseHandle();
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { internal int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetUpdateRect(nint window, out Rect rectangle, bool erase);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
