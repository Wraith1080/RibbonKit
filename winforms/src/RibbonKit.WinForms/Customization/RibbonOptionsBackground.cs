using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// Paint only library-owned containers. Never traverse an application's Options
// pages. Each body uses one client canvas; each native well uses its own viewport.
internal static class RibbonOptionsBackground
{
    private static readonly ConditionalWeakTable<Control, RibbonControl> Roots = new();
    internal static void Bind(Control root, RibbonControl ribbon)
    {
        if (ribbon.Theme.OptionsAppearance?.BackgroundFill is null) return;
        Roots.GetValue(root, _ => ribbon);
        void Prepare(Control control)
        {
            if (control is RibbonOptionsFrame or RibbonOptionsFooter) return;
            if (control is Panel or UserControl or Form)
            {
                if (control is not Form) control.BackColor = Color.Transparent;
                control.Paint += (_, e) => PaintBody(e.Graphics, control.ClientRectangle, control, ribbon.Theme);
                control.Resize += (_, _) => control.Invalidate(true);
                foreach (Control child in control.Controls) Prepare(child);
            }
            else if (control is Label) control.BackColor = Color.Transparent;
        }
        Prepare(root);
    }
    internal static bool PaintBody(Graphics graphics, Rectangle bounds, Control control, RibbonTheme theme)
    {
        if (theme.OptionsAppearance?.BackgroundFill is not { } fill) return false;
        Control? root = null;
        RibbonControl? ribbon = null;
        for (Control? parent = control; parent is not null; parent = parent.Parent)
            if (Roots.TryGetValue(parent, out var owner)) { root = parent; ribbon = owner; }
        if (root is null || ribbon is null) return false;
        var canvas = new Rectangle(control.PointToClient(root.PointToScreen(Point.Empty)), root.ClientSize);
        ribbon.Painter.PaintFill(new(graphics, bounds, theme, control.Font, control.DeviceDpi) { BackgroundBounds = canvas }, fill);
        return true;
    }
    internal static void PaintList(RibbonRenderer owner, RibbonPaintContext c) =>
        owner.PaintFill(c, c.Theme.OptionsAppearance?.ListBackgroundFill ?? new(RibbonOptionsRenderer.ListBackground(c.Theme)));
    internal static void PaintList(RoundedPainter painter, RibbonPaintContext c) =>
        painter.Draw(c.Graphics, c.Bounds, 0, c.Theme.OptionsAppearance?.ListBackgroundFill ?? new(RibbonOptionsRenderer.ListBackground(c.Theme)), Color.Transparent, 0, fillBounds: c.BackgroundBounds);

    internal static Rectangle BeforeNativePaint(Control control, Message message)
    {
        if (message.Msg == 0xF && GetUpdateRect(control.Handle, out var rect, false)) return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return control.ClientRectangle;
    }
    internal static bool EraseNative(Control control, RibbonControl ribbon, ref Message message)
    {
        if (message.Msg != 0x14 || message.WParam == 0 || ribbon.Theme.OptionsAppearance?.ListBackgroundFill is null) return false;
        using var graphics = Graphics.FromHdc(message.WParam);
        PaintList(ribbon.Painter, new(graphics, control.ClientRectangle, ribbon.Theme, control.Font, control.DeviceDpi));
        message.Result = 1; return true;
    }
    internal static void PaintUnusedNative(Control control, RibbonControl ribbon, Message message, Rectangle invalid, IEnumerable<Rectangle> rows)
    {
        if (message.Msg is not (0xF or 0x317 or 0x318) || ribbon.Theme.OptionsAppearance?.ListBackgroundFill is null || invalid.IsEmpty) return;
        using var graphics = message.Msg == 0xF ? Graphics.FromHwnd(control.Handle) : Graphics.FromHdc(message.WParam);
        graphics.SetClip(invalid, CombineMode.Intersect);
        foreach (var row in rows) graphics.ExcludeClip(row);
        PaintList(ribbon.Painter, new(graphics, control.ClientRectangle, ribbon.Theme, control.Font, control.DeviceDpi));
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetUpdateRect(nint window, out NativeRect rectangle, bool erase);
}
