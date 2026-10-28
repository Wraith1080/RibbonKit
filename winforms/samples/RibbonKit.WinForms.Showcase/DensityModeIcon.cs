using System.Drawing.Drawing2D;

namespace RibbonKit.WinForms.Showcase;

// Application artwork, borrowed by both the selector command and its choice rows.
internal sealed class DensityModeIcon(bool touch) : IRibbonIcon
{
    public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
    {
        var saved = graphics.Save();
        try
        {
            graphics.TranslateTransform(bounds.Left, bounds.Top); graphics.ScaleTransform(bounds.Width / 40f, bounds.Height / 40f); graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var outline = new Pen(context.Enabled ? Color.FromArgb(91, 101, 119) : SystemColors.GrayText, 2) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var accent = new Pen(context.Enabled ? Color.FromArgb(0, 120, 212) : SystemColors.GrayText, 2);
            using var paper = new SolidBrush(Color.White);
            if (touch)
            {
                graphics.DrawEllipse(accent, 5, 1, 15, 15); graphics.DrawEllipse(accent, 9, 5, 7, 7);
                using var hand = new GraphicsPath();
                hand.StartFigure(); hand.AddLine(12, 26, 12, 10); hand.AddBezier(12, 10, 12, 6, 17, 6, 17, 10); hand.AddLine(17, 10, 17, 22);
                hand.AddBezier(17, 22, 17, 18, 23, 18, 23, 22); hand.AddBezier(23, 22, 23, 19, 29, 20, 29, 24); hand.AddBezier(29, 24, 29, 21, 35, 23, 35, 27);
                hand.AddLine(35, 27, 35, 31); hand.AddBezier(35, 31, 35, 37, 32, 39, 26, 39); hand.AddLine(26, 39, 21, 39);
                hand.AddBezier(21, 39, 17, 39, 15, 36, 13, 34); hand.AddLine(13, 34, 6, 27); hand.AddBezier(6, 27, 6, 27, 4, 22, 8, 22); hand.AddBezier(8, 22, 8, 22, 10, 23, 12, 26); hand.CloseFigure();
                graphics.FillPath(paper, hand); graphics.DrawPath(outline, hand);
            }
            else
            {
                using var mouse = new GraphicsPath(); mouse.AddArc(5, 2, 20, 20, 180, 180); mouse.AddLine(25, 12, 25, 23); mouse.AddArc(5, 13, 20, 20, 0, 180); mouse.CloseFigure();
                graphics.FillPath(paper, mouse); graphics.DrawPath(outline, mouse); graphics.DrawLine(outline, 5, 13, 25, 13); graphics.DrawLine(outline, 15, 2, 15, 13); graphics.DrawLine(accent, 11, 5, 11, 10);
                PointF[] pointer = [new(21, 16), new(36, 30), new(29, 30), new(33, 37), new(29, 39), new(25, 32), new(21, 36)];
                graphics.FillPolygon(paper, pointer); graphics.DrawPolygon(outline, pointer);
            }
        }
        finally { graphics.Restore(saved); }
    }
}
