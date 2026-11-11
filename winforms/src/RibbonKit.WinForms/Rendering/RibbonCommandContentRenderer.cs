using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// These recipes share structure across the light families. The actual renderer
// retains the public indicator/image/arrow hooks and its checkbox-radius choice.
internal static class RibbonCommandContentRenderer
{
    private static Color Ink(Color? authored, Color fallback) => authored is { IsEmpty: false } ink ? ink : fallback;
    private static int Border(RibbonPaintContext c) => c.Theme.CommandContent?.IndicatorBorderWidth is { } value
        ? Math.Min(RibbonLayoutEngine.Scale(value, c.DeviceDpi), Math.Max(0, Math.Min(c.Bounds.Width, c.Bounds.Height) / 2)) : 1;

    internal static void DrawRadio(RibbonRenderer owner, RibbonPaintContext c, bool selected, bool enabled)
    {
        var settings = c.Theme.CommandContent;
        var border = !enabled ? c.Theme.DisabledColor : c.Hot || c.Pressed || c.Focused || selected ? c.Theme.Accent : c.Theme.SecondaryText;
        owner.PaintRounded(c, c.Bounds.Width / 2, c.Pressed ? c.Theme.Pressed : new(c.Theme.Surface), Ink(settings?.IndicatorBorderColor, border), Border(c));
        if (!selected) return;
        int inset = RibbonLayoutEngine.Scale(settings?.RadioDotInset ?? 4, c.DeviceDpi);
        if (inset * 2 >= Math.Min(c.Bounds.Width, c.Bounds.Height)) return;
        var dot = Rectangle.Inflate(c.Bounds, -inset, -inset);
        owner.PaintRounded(c with { Bounds = dot }, dot.Width / 2, new(Ink(settings?.RadioDotColor, enabled ? c.Theme.Accent : c.Theme.DisabledColor)), Color.Transparent, 0);
    }

    internal static void DrawCheck(RibbonRenderer owner, RibbonPaintContext c, CheckState check, bool enabled, int radius)
    {
        var settings = c.Theme.CommandContent;
        bool marked = check != CheckState.Unchecked;
        var border = !enabled ? c.Theme.DisabledColor : marked || c.Hot || c.Pressed || c.Focused ? c.Theme.Accent : c.Theme.SecondaryText;
        var fill = marked ? new RibbonFill(enabled ? c.Theme.Accent : c.Theme.DisabledColor) : c.Pressed ? c.Theme.Pressed : new(c.Theme.Surface);
        owner.PaintRounded(c, radius, fill, Ink(settings?.IndicatorBorderColor, border), Border(c));
        if (!marked) return;
        float stroke = settings?.CheckMarkStrokeWidth is { } width ? (float)(width * c.DeviceDpi / 96d) : Math.Max(1, 1.5f * c.DeviceDpi / 96);
        float scale = (float)(settings?.CheckMarkScale ?? 1);
        if (stroke <= 0 || scale <= 0) return;
        var bounds = c.Bounds;
        float centerX = bounds.Left + (bounds.Width - 1) / 2f, centerY = bounds.Top + (bounds.Height - 1) / 2f;
        // Retain the canonical tick's optical centering and default arithmetic.
        float left = centerX - bounds.Width * .515f, top = centerY - bounds.Height * .54f;
        PointF Scaled(float x, float y) => scale == 1 ? new(x, y) : new(centerX + (x - centerX) * scale, centerY + (y - centerY) * scale);
        var saved = c.Graphics.Save();
        try
        {
            if (settings?.CheckMarkStrokeWidth is not null || settings?.CheckMarkScale is not null) c.Graphics.SetClip(bounds, CombineMode.Intersect);
            c.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Ink(settings?.CheckMarkColor, c.Theme.Surface), stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            if (check == CheckState.Indeterminate) c.Graphics.DrawLine(pen, Scaled(centerX - bounds.Width * .25f, centerY), Scaled(centerX + bounds.Width * .25f, centerY));
            else c.Graphics.DrawLines(pen, [Scaled(left + bounds.Width * .22f, top + bounds.Height * .51f), Scaled(left + bounds.Width * .43f, top + bounds.Height * .72f), Scaled(left + bounds.Width * .79f, top + bounds.Height * .29f)]);
        }
        finally { c.Graphics.Restore(saved); }
    }

    internal static void DrawDisabledImage(RibbonPaintContext c, Image image, bool independentQuickAccess = false)
    {
        var settings = c.Theme.CommandContent;
        if (independentQuickAccess && settings?.DisabledImageOpacity is null && settings?.DisabledImageGrayscaleAmount is null)
        {
            using var bitmap = new Bitmap(image, c.Bounds.Size);
            ControlPaint.DrawImageDisabled(c.Graphics, bitmap, c.Bounds.X, c.Bounds.Y, c.Theme.Background);
            return;
        }
        using var attributes = new ImageAttributes();
        float gray = (float)(settings?.DisabledImageGrayscaleAmount ?? 1), color = 1 - gray;
        attributes.SetColorMatrix(new ColorMatrix(new float[][]
        {
            [0.3f * gray + color, 0.3f * gray, 0.3f * gray, 0, 0],
            [0.59f * gray, 0.59f * gray + color, 0.59f * gray, 0, 0],
            [0.11f * gray, 0.11f * gray, 0.11f * gray + color, 0, 0],
            [0, 0, 0, (float)(settings?.DisabledImageOpacity ?? .55), 0], [0, 0, 0, 0, 1]
        }));
        c.Graphics.DrawImage(image, c.Bounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
    }

    internal static void DrawQuickAccessMixedMark(RibbonPaintContext c, bool enabled, bool menu)
    {
        var settings = c.Theme.CommandContent;
        int S(double n) => RibbonLayoutEngine.Scale(n, c.DeviceDpi);
        double scale = settings?.CheckMarkScale ?? 1;
        int width = S(6 * scale), height = S(settings?.CheckMarkStrokeWidth ?? 2);
        if (width <= 0 || height <= 0) return;
        int center = menu ? c.Bounds.Left + S(14) : c.Bounds.Left + c.Bounds.Width / 2;
        int top = Math.Min(c.Bounds.Bottom - S(3), c.Bounds.Top + c.Bounds.Height / 2 + S(9));
        var saved = c.Graphics.Save();
        try
        {
            if (settings?.CheckMarkScale is not null || settings?.CheckMarkStrokeWidth is not null) c.Graphics.SetClip(c.Bounds, CombineMode.Intersect);
            using var brush = new SolidBrush(Ink(settings?.CheckMarkColor, enabled ? c.Theme.Foreground : c.Theme.DisabledColor));
            c.Graphics.FillRectangle(brush, center - S(3 * scale), top, width, height);
        }
        finally { c.Graphics.Restore(saved); }
    }
}
