using System.Runtime.InteropServices;
using System.Windows;
using Xunit;
using Xunit.Sdk;

namespace RibbonKit.Portability.Tests;

// Validate the consumer's geometry oracle without depending on the runner's
// desktop dimensions. The actual popup matrix exercises the same oracle.
public sealed class OverflowPopupPlacementTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1800, 1.5)]
    public void Overlay_is_valid_only_when_neither_side_fits(double origin, double scale)
    {
        Rect Map(Rect rect) => new(origin + rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
        var parent = Map(new Rect(4, 2, 214, 378));
        var row = Map(new Rect(11, 342, 200, 32));
        var child = Map(new Rect(11, 376, 837, 170));
        var frame = Map(new Rect(7, 374, 849, 180));
        // The reported CI geometry is a valid fallback on a 1024-pixel desktop.
        OverflowPopupPlacement.AssertPlacement(parent, child, frame, row, Map(new Rect(0, 0, 1024, 768)), "overlay");
        // The identical overlap must still fail when a side has enough room.
        Assert.Throws<TrueException>(() => OverflowPopupPlacement.AssertPlacement(parent, child, frame, row,
            Map(new Rect(0, 0, 1536, 768)), "unnecessary overlay"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1800, 1.5)]
    public void Side_popup_keeps_the_strict_chrome_join(double origin, double scale)
    {
        Rect Map(Rect rect) => new(origin + rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
        var parent = Map(new Rect(4, 2, 214, 378));
        var row = Map(new Rect(11, 342, 200, 32));
        var child = Map(new Rect(218, 342, 837, 170));
        var frame = Map(new Rect(214, 340, 849, 180));
        var work = Map(new Rect(0, 0, 1536, 768));
        OverflowPopupPlacement.AssertPlacement(parent, child, frame, row, work, "joined");
        child.Offset(3 * scale, 0); frame.Offset(3 * scale, 0);
        Assert.Throws<TrueException>(() => OverflowPopupPlacement.AssertPlacement(parent, child, frame, row, work, "detached"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1800, 1.5)]
    public void Overlay_stays_anchored_and_clamps_to_the_work_area(double origin, double scale)
    {
        Rect Map(Rect rect) => new(origin + rect.X * scale, rect.Y * scale, rect.Width * scale, rect.Height * scale);
        var parent = Map(new Rect(4, 2, 214, 730));
        var row = Map(new Rect(11, 700, 200, 32));
        var child = Map(new Rect(11, 590, 837, 170));
        var frame = Map(new Rect(7, 588, 849, 180));
        var work = Map(new Rect(0, 0, 1024, 768));
        OverflowPopupPlacement.AssertPlacement(parent, child, frame, row, work, "clamped overlay");
        child.Offset(0, -10 * scale); frame.Offset(0, -10 * scale);
        Assert.Throws<TrueException>(() => OverflowPopupPlacement.AssertPlacement(parent, child, frame, row, work, "wrong row anchor"));
        child.Offset(-20 * scale, 10 * scale); frame.Offset(-20 * scale, 10 * scale);
        Assert.Throws<TrueException>(() => OverflowPopupPlacement.AssertPlacement(parent, child, frame, row, work, "outside work area"));
    }
}

internal static class OverflowPopupPlacement
{
    internal static void AssertPlacement(Rect parent, Rect child, Rect frame, Rect row, Rect work, string context)
    {
        string detail = $"{context}: parent={parent}, child={child}, frame={frame}, row={row}, work={work}";
        var bounded = work; bounded.Inflate(1, 1);
        Assert.True(bounded.Contains(frame), $"Popup frame outside work area: {detail}");
        // Account for transparent shadow/margin allocation when deciding whether
        // a joined popup frame could fit. All rectangles use physical pixels.
        double right = parent.Right - (child.Left - frame.Left);
        double left = parent.Left - frame.Width + (frame.Right - child.Right);
        bool fitsRight = right >= work.Left && right + frame.Width <= work.Right;
        bool fitsLeft = left >= work.Left && left + frame.Width <= work.Right;
        if (fitsRight || fitsLeft)
        {
            Assert.True(Math.Abs(child.Left - parent.Right) <= 1 || Math.Abs(parent.Left - child.Right) <= 1,
                $"Detached submenu with an available side: {detail}");
            return;
        }

        Assert.True(child.Left < row.Right && child.Right > row.Left, $"Fallback misses its row horizontally: {detail}");
        double top = Math.Clamp(row.Bottom, work.Top, Math.Max(work.Top, work.Bottom - frame.Height));
        Assert.True(Math.Abs(frame.Top - top) <= 1, $"Fallback is not below its row or vertically clamped: {detail}");
    }

    internal static Rect WorkArea(Rect row)
    {
        var bounds = new NativeRect { Left = (int)Math.Floor(row.Left), Top = (int)Math.Floor(row.Top),
            Right = (int)Math.Ceiling(row.Right), Bottom = (int)Math.Ceiling(row.Bottom) };
        nint monitor = MonitorFromRect(ref bounds, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        Assert.True(monitor != 0 && GetMonitorInfo(monitor, ref info), "Could not resolve the popup row's monitor work area.");
        return new Rect(info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
    }

    [DllImport("user32.dll")] private static extern nint MonitorFromRect(ref NativeRect rect, int flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public int Flags; }
}
