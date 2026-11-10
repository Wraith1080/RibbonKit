using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Gets optional material page shadow clearance in device pixels. Built-in renderers use zero and Office uses a border instead. Solid mode does not reserve this space. Layout and background drawing share this value, clamped to available space and 32 logical pixels.</summary>
    public virtual int GetBackstagePageShadowInset(RibbonTheme theme, int deviceDpi) => 0;
    /// <summary>Gets the page's top/left border width in device pixels for Solid and material modes. The default is zero. Background drawing and the native page inset share this value.</summary>
    public virtual int GetBackstagePageBorderWidth(RibbonTheme theme, int deviceDpi) => RibbonLayoutEngine.Scale(theme.BackstagePageBorderWidth ?? 0, deviceDpi);
    /// <summary>Gets the page's top-left corner radius in device pixels for Solid and material modes. The default is zero. Background drawing and the native page clip share this value; other corners stay square.</summary>
    public virtual int GetBackstagePageCornerRadius(RibbonTheme theme, int deviceDpi) => 0;
    /// <summary>Paints the native Backstage surface and its measured navigation rail.</summary>
    public virtual void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
        => DrawNeutralBackstageBackground(context, navigationWidth);
    internal void DrawNeutralBackstageBackground(RibbonPaintContext context, int navigationWidth)
    {
        int width = Math.Clamp(navigationWidth, 0, context.Bounds.Width);
        int shadow = context.WindowBackdrop || context.CompositedText ? RibbonBackstageLayout.PageShadowInset(this, context.Theme, context.DeviceDpi, context.Bounds.Size) : 0;
        using var rail = new SolidBrush(context.Theme.BackstageNavigationColor);
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
        int stroke = RibbonBackstageLayout.PageBorderWidth(this, context.Theme, context.DeviceDpi, bounds.Size);
        if (stroke > 0)
        {
            using var border = new SolidBrush(context.Theme.Border);
            context.Graphics.FillRectangle(border, bounds.Left, bounds.Top, bounds.Width, stroke);
            context.Graphics.FillRectangle(border, bounds.Left, bounds.Top, stroke, bounds.Height);
        }
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
            using var rail = new SolidBrush(context.Theme.BackstageNavigationColor);
            context.Graphics.FillRectangle(rail, context.Bounds);
        }
        finally { context.Graphics.CompositingMode = mode; }
        if (item is RibbonMenuSeparatorItem separator)
        { DrawMenuSeparator(context with { Hot = false, Pressed = false, Focused = false }, separator); return; }
        var color = !enabled ? context.Theme.BackstageDisabledForeground.IsEmpty ? context.Theme.DisabledColor : context.Theme.BackstageDisabledForeground
            : selected ? context.Theme.BackstageSelectedForeground.IsEmpty ? context.Theme.Accent : context.Theme.BackstageSelectedForeground : context.Theme.BackstageNavigationInk;
        if (item is null)
        {
            int diameter = Math.Max(0, Math.Min(context.Bounds.Width, context.Bounds.Height) - 2 * S(2));
            var circle = new Rectangle(context.Bounds.Left + (context.Bounds.Width - diameter) / 2,
                context.Bounds.Top + (context.Bounds.Height - diameter) / 2, diameter, diameter);
            var fill = context.Pressed || context.Hot
                ? context.Theme.BackstageFullWidthNavigation ? context.Pressed ? context.Theme.BackstageSelectedFill : context.Theme.BackstageHoverFill
                    : context.WindowBackdrop || context.CompositedText ? BackdropStateFill(context, context.Pressed) : context.Pressed ? context.Theme.Pressed : context.Theme.Hover
                : new RibbonFill(Color.Transparent);
            PaintRounded(context with { Bounds = circle }, RibbonFrameMetrics.Radius(context.Theme.BackstageBackButtonCornerRadius, diameter / 2, context.DeviceDpi), fill,
                context.Focused && context.Theme.BackstageNavigationForeground.IsEmpty ? context.Theme.Accent : color, Math.Max(1, RibbonLayoutEngine.Scale(1.5, context.DeviceDpi)));
            var saved = context.Graphics.Save();
            try
            {
                context.Graphics.SetClip(context.Bounds, CombineMode.Intersect);
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
            var presentation = RibbonPresentationResolver.Backstage(context.Theme);
            presentation.Renderer.DrawEntryState(this, context, selected);
            if (selected && context.Theme.BackstageSelectionIndicatorVisible)
            {
                var marker = new Rectangle(context.Bounds.Left + S(2), context.Bounds.Top + S(10), S(3), Math.Max(1, context.Bounds.Height - S(20)));
                PaintRounded(context with { Bounds = marker }, RibbonFrameMetrics.Radius(context.Theme.NavigationMarkerCornerRadius, marker.Width / 2, context.DeviceDpi), new(context.Theme.Accent), Color.Transparent, 0);
            }
            var metrics = presentation.ResolveMetrics(context.Theme, context.Density);
            int iconInset = Math.Min(S(metrics.IconInset), context.Bounds.Width);
            int iconSize = Math.Min(S(metrics.IconSize), Math.Min(Math.Max(0, context.Bounds.Width - iconInset), context.Bounds.Height));
            var icon = new Rectangle(context.Bounds.Left + iconInset, context.Bounds.Top + (context.Bounds.Height - iconSize) / 2, iconSize, iconSize);
            if (context.Theme.BackstageMonochromeIcons) DrawBackstageSilhouette(context, item, icon, color, enabled);
            else if (item.Icon is { } vector) vector.Draw(context.Graphics, icon, new(context.DeviceDpi, color, enabled));
            else if (item.Image is { } image) context.Graphics.DrawImage(image, icon);
            int textOffset = metrics.TextOffset;
            int right = Math.Max(context.Bounds.Left, context.Bounds.Right - S(metrics.TrailingPadding));
            var text = Rectangle.FromLTRB(Math.Min(right, context.Bounds.Left + S(textOffset)), context.Bounds.Top, right, context.Bounds.Bottom);
            DrawText(context with { Bounds = text }, item.Text, color,
                TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        if (context.Focused && item is not null) DrawFocus(context.Theme.BackstageNavigationForeground.IsEmpty ? context
            : context with { Theme = context.Theme with { Accent = context.Theme.BackstageNavigationInk } });
    }

    private static void DrawBackstageSilhouette(RibbonPaintContext context, RibbonItem item, Rectangle bounds, Color color, bool enabled)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || item.Icon is null && item.Image is null) return;
        // Render vectors at this monitor's exact device size before applying the
        // same alpha mask as WPF. Borrowed rich icons/images remain unchanged.
        using var source = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(source))
        {
            if (item.Icon is { } vector) vector.Draw(graphics, new(Point.Empty, bounds.Size), new(context.DeviceDpi, color, enabled));
            else graphics.DrawImage(item.Image!, new Rectangle(Point.Empty, bounds.Size));
        }
        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new float[][]
        {
            [0, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 0],
            [0, 0, 0, color.A / 255f, 0], [color.R / 255f, color.G / 255f, color.B / 255f, 0, 1]
        }));
        context.Graphics.DrawImage(source, bounds, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
    }
}
