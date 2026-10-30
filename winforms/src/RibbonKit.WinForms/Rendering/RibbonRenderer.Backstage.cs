using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Gets the page's top and navigation-side shadow clearance in device pixels. Zero keeps a flat page. Layout and background drawing share this value, clamped to available space and 32 logical pixels.</summary>
    public virtual int GetBackstagePageShadowInset(RibbonTheme theme, int deviceDpi) => 0;
    /// <summary>Paints the native Backstage surface and its measured navigation rail.</summary>
    public virtual void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
    {
        int width = Math.Clamp(navigationWidth, 0, context.Bounds.Width);
        int shadow = RibbonBackstageLayout.PageShadowInset(this, context.Theme, context.DeviceDpi, context.Bounds.Size);
        using var rail = new SolidBrush(context.Theme.BackstageNavigationBackground);
        context.Graphics.FillRectangle(rail, context.Bounds);
        var bounds = new Rectangle(context.Bounds.X + width, context.Bounds.Y + shadow, context.Bounds.Width - width, Math.Max(0, context.Bounds.Height - shadow));
        if (shadow > 0 && bounds.Width > 0 && bounds.Height > 0)
        {
            var state = context.Graphics.Save();
            try
            {
                context.Graphics.SetClip(context.Bounds, CombineMode.Intersect);
                _shadow.Draw(context.Graphics, bounds, 0, shadow, context.Theme.BodyShadowColor);
            }
            finally { context.Graphics.Restore(state); }
        }
        using var page = new SolidBrush(context.Theme.Surface);
        context.Graphics.FillRectangle(page, bounds);
    }
    /// <summary>Paints a native navigation button using shared state and geometry. A null item denotes the Back button.</summary>
    public virtual void DrawBackstageEntry(RibbonPaintContext context, RibbonItem? item, bool selected)
    {
        bool enabled = item?.EffectiveEnabled ?? true;
        if (!enabled) context = context with { Hot = false, Pressed = false, Focused = false };
        int S(int value) => RibbonLayoutEngine.Scale(value, context.DeviceDpi);
        // Each entry replaces its rail pixels. Source-over would apply the tint
        // again when composing a navigation image for motion, darkening rows.
        var mode = context.Graphics.CompositingMode;
        try
        {
            if (context.CompositedText) context.Graphics.CompositingMode = CompositingMode.SourceCopy;
            using var rail = new SolidBrush(context.Theme.BackstageNavigationBackground);
            context.Graphics.FillRectangle(rail, context.Bounds);
        }
        finally { context.Graphics.CompositingMode = mode; }
        var color = !enabled ? SystemColors.GrayText : selected ? context.Theme.Accent : context.Theme.Foreground;
        if (item is null)
        {
            int diameter = Math.Max(0, Math.Min(context.Bounds.Width, context.Bounds.Height) - 2 * S(2));
            var circle = new Rectangle(context.Bounds.Left + (context.Bounds.Width - diameter) / 2,
                context.Bounds.Top + (context.Bounds.Height - diameter) / 2, diameter, diameter);
            var fill = context.Pressed || context.Hot
                ? context.WindowBackdrop || context.CompositedText ? BackdropStateFill(context, context.Pressed) : context.Pressed ? context.Theme.Pressed : context.Theme.Hover
                : new RibbonFill(Color.Transparent);
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
                DrawChrome(context, context.WindowBackdrop || context.CompositedText ? BackdropStateFill(context, context.Pressed)
                    : context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
            if (selected)
            {
                var marker = new Rectangle(context.Bounds.Left + S(2), context.Bounds.Top + S(10), S(3), Math.Max(1, context.Bounds.Height - S(20)));
                PaintRounded(context with { Bounds = marker }, marker.Width / 2, new(context.Theme.Accent), Color.Transparent, 0);
            }
            var icon = new Rectangle(context.Bounds.Left + S(16), context.Bounds.Top + (context.Bounds.Height - S(16)) / 2, S(16), S(16));
            if (item.Icon is { } vector) vector.Draw(context.Graphics, icon, new(context.DeviceDpi, color, enabled));
            else if (item.Image is { } image) context.Graphics.DrawImage(image, icon);
            var text = Rectangle.FromLTRB(context.Bounds.Left + S(44), context.Bounds.Top, context.Bounds.Right - S(8), context.Bounds.Bottom);
            DrawText(context with { Bounds = text }, item.Text, color,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        if (context.Focused && item is not null) DrawFocus(context);
    }
}
