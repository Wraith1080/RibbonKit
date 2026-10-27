using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Paints the native Backstage surface and its measured navigation rail.</summary>
    public virtual void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
    {
        using var page = new SolidBrush(context.Theme.Surface);
        context.Graphics.FillRectangle(page, context.Bounds);
        using var rail = new SolidBrush(context.Theme.BackstageNavigationBackground);
        context.Graphics.FillRectangle(rail, context.Bounds.X, context.Bounds.Y, Math.Clamp(navigationWidth, 0, context.Bounds.Width), context.Bounds.Height);
    }
    /// <summary>Paints a native navigation button using shared state and geometry. A null item denotes the Back button.</summary>
    public virtual void DrawBackstageEntry(RibbonPaintContext context, RibbonItem? item, bool selected)
    {
        bool enabled = item?.EffectiveEnabled ?? true;
        if (!enabled) context = context with { Hot = false, Pressed = false, Focused = false };
        int S(int value) => RibbonLayoutEngine.Scale(value, context.DeviceDpi);
        using (var rail = new SolidBrush(context.Theme.BackstageNavigationBackground)) context.Graphics.FillRectangle(rail, context.Bounds);
        var color = !enabled ? SystemColors.GrayText : selected ? context.Theme.Accent : context.Theme.Foreground;
        if (item is null)
        {
            int diameter = Math.Max(0, Math.Min(context.Bounds.Width, context.Bounds.Height) - 2 * S(2));
            var circle = new Rectangle(context.Bounds.Left + (context.Bounds.Width - diameter) / 2,
                context.Bounds.Top + (context.Bounds.Height - diameter) / 2, diameter, diameter);
            var fill = context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : new RibbonFill(context.Theme.BackstageNavigationBackground);
            PaintRounded(context with { Bounds = circle }, diameter / 2, fill,
                context.Focused ? context.Theme.Accent : color, Math.Max(1, RibbonLayoutEngine.Scale(1.5, context.DeviceDpi)));
            var saved = context.Graphics.Save();
            try
            {
                context.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(color, Math.Max(1f, 1.5f * context.DeviceDpi / 96f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                float x = context.Bounds.Left + context.Bounds.Width / 2f, y = context.Bounds.Top + context.Bounds.Height / 2f;
                using var arrow = new GraphicsPath();
                arrow.AddLines(new PointF[] { new(x, y - S(5)), new(x - S(5), y), new(x, y + S(5)) });
                arrow.StartFigure();
                arrow.AddLine(x - S(5), y, x + S(6), y);
                context.Graphics.DrawPath(pen, arrow);
            }
            finally { context.Graphics.Restore(saved); }
        }
        else
        {
            if (context.Hot || context.Pressed || selected)
                DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
            if (selected)
            {
                var marker = new Rectangle(context.Bounds.Left + S(2), context.Bounds.Top + S(10), S(3), Math.Max(1, context.Bounds.Height - S(20)));
                PaintRounded(context with { Bounds = marker }, marker.Width / 2, new(context.Theme.Accent), Color.Transparent, 0);
            }
            var icon = new Rectangle(context.Bounds.Left + S(16), context.Bounds.Top + (context.Bounds.Height - S(16)) / 2, S(16), S(16));
            if (item.Icon is { } vector) vector.Draw(context.Graphics, icon, new(context.DeviceDpi, color, enabled));
            else if (item.Image is { } image) context.Graphics.DrawImage(image, icon);
            var text = Rectangle.FromLTRB(context.Bounds.Left + S(44), context.Bounds.Top, context.Bounds.Right - S(8), context.Bounds.Bottom);
            TextRenderer.DrawText(context.Graphics, item.Text, context.Font, text, color,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        if (context.Focused && item is not null) DrawFocus(context);
    }
}
