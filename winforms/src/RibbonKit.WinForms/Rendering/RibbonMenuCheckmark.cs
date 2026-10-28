using System.Drawing.Drawing2D;

namespace RibbonKit.WinForms;

// Shared plain tick for checked menu commands, with no checkbox frame or fill.
internal sealed class RibbonMenuCheckmark : IRibbonIcon
{
    internal static readonly RibbonMenuCheckmark Instance = new();
    public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
    {
        var saved = graphics.Save();
        try
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(context.Foreground, Math.Max(1, 1.7f * context.DeviceDpi / 96)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            graphics.DrawLines(pen, new PointF[] { new(bounds.Left + bounds.Width * .22f, bounds.Top + bounds.Height * .51f), new(bounds.Left + bounds.Width * .43f, bounds.Top + bounds.Height * .72f), new(bounds.Left + bounds.Width * .79f, bounds.Top + bounds.Height * .29f) });
        }
        finally { graphics.Restore(saved); }
    }
}
