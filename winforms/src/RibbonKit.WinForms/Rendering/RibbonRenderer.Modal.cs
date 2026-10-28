using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Paints the modal close command with shared theme states and library-measured header bounds.</summary>
    public virtual void DrawModalClose(RibbonPaintContext context, string text)
    {
        context = context with { Bounds = RibbonHeaderButtonLayout.PaintBounds(context.Bounds, context.Theme, context.DeviceDpi) };
        if (context.Bounds.Width <= 0 || context.Bounds.Height <= 0) return;
        var clip = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(context.Bounds, CombineMode.Intersect);
            if (context.Hot || context.Pressed) DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
            int S(int value) => RibbonLayoutEngine.Scale(value, context.DeviceDpi);
            var glyph = new Rectangle(context.Bounds.Left + S(6), context.Bounds.Top + (context.Bounds.Height - S(8)) / 2, S(8), S(8));
            var state = context.Graphics.Save();
            try
            {
                context.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(context.Theme.Foreground, Math.Max(1, 1.5f * context.DeviceDpi / 96)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                context.Graphics.DrawLine(pen, glyph.Left, glyph.Top, glyph.Right - 1, glyph.Bottom - 1);
                context.Graphics.DrawLine(pen, glyph.Right - 1, glyph.Top, glyph.Left, glyph.Bottom - 1);
            }
            finally { context.Graphics.Restore(state); }
            int left = glyph.Right + S(6);
            var label = Rectangle.FromLTRB(left, context.Bounds.Top, Math.Max(left, context.Bounds.Right - S(4)), context.Bounds.Bottom);
            if (label.Width > 0) DrawText(context with { Bounds = label }, text,
                context.Theme.Foreground, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            if (context.Focused) DrawFocus(context);
        }
        finally { context.Graphics.Restore(clip); }
    }
}
