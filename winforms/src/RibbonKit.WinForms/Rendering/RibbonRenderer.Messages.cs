using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    internal virtual bool PaintConnectedMessageShadow(RibbonPaintContext context, Rectangle upper, Rectangle messages) => false;
    /// <summary>Gets whether message frames use body margins and connect to its/QAT's lower edge. Neutral rendering keeps a separate full-width stack.</summary>
    public virtual bool JoinMessageBarToBody => false;
    /// <summary>Paints the measured message stack frame; IsTopEdgeConnected distinguishes attached from standalone presentations.</summary>
    public virtual void DrawMessageBar(RibbonPaintContext context) => PaintMessageFrame(context, 0);
    internal void PaintMessageFrame(RibbonPaintContext context, int radius)
    {
        radius = MessageCornerRadius(context, radius);
        var fill = context.Theme.MessageAppearance?.BackgroundPerRow == true ? new RibbonFill(Color.Transparent) : context.Theme.MessageBackground;
        int stroke = RibbonBodyFrameMetrics.MessageBorderWidth(context);
        if (context.Theme.MessageSideBordersVisible) { PaintRounded(context, radius, fill, context.Theme.MessageBorder, stroke); return; }
        PaintRounded(context, radius, fill, Color.Transparent, 0);
        using var border = new SolidBrush(context.Theme.MessageBorder);
        stroke = Math.Min(stroke, Math.Max(0, context.Bounds.Height));
        if (!context.IsTopEdgeConnected && stroke > 0) context.Graphics.FillRectangle(border, context.Bounds.Left, context.Bounds.Top, context.Bounds.Width, stroke);
        if (!context.IsBottomEdgeConnected && stroke > 0) context.Graphics.FillRectangle(border, context.Bounds.Left, context.Bounds.Bottom - stroke, context.Bounds.Width, stroke);
    }
    private static int MessageCornerRadius(RibbonPaintContext c, int fallback) => c.Theme.MessageAppearance?.CornerRadius is { } n ? RibbonLayoutEngine.Scale(n, c.DeviceDpi) : fallback;
    /// <summary>Paints a message's library-measured title/body/icon slots and optional row separator. Action and close targets have separate callbacks.</summary>
    public virtual void DrawMessage(RibbonPaintContext context, RibbonMessage message, Rectangle titleBounds, Rectangle textBounds, Rectangle iconBounds, bool first)
    {
        var saved = context.Graphics.Save();
        try
        {
            int stroke = RibbonBodyFrameMetrics.MessageBorderWidth(context);
            if (context.Theme.MessageAppearance?.BackgroundPerRow == true)
            {
                context.Graphics.SetClip(context.Bounds, CombineMode.Intersect);
                var frame = context.BackgroundBounds.IsEmpty ? context.Bounds : context.BackgroundBounds;
                if (JoinMessageBarToBody && context.IsTopEdgeConnected) { frame.Y -= stroke; frame.Height += stroke; }
                int radius = MessageCornerRadius(context, GetBodyCornerRadius(context.Theme, context.DeviceDpi));
                if (!context.Theme.MessageSideBordersVisible)
                    context.Graphics.SetClip(Rectangle.FromLTRB(frame.Left, frame.Top + (!context.IsTopEdgeConnected ? stroke : 0), frame.Right, Math.Max(frame.Top, frame.Bottom - stroke)), CombineMode.Intersect);
                // The frame owns its rim. Fill once inside that rim, sampling
                // this row while retaining the stack's exposed corner coverage.
                _rounded.Draw(context.Graphics, frame, radius, context.Theme.MessageBackground, Color.Transparent,
                    context.Theme.MessageSideBordersVisible ? stroke : 0, context.IsTopEdgeConnected, false, context.Bounds);
            }
            int inset = context.Theme.MessageSideBordersVisible ? Math.Min(stroke, Math.Max(0, context.Bounds.Width / 2)) : 0;
            context.Graphics.SetClip(new Rectangle(context.Bounds.Left + inset, context.Bounds.Top, Math.Max(0, context.Bounds.Width - 2 * inset), Math.Max(0, context.Bounds.Height - stroke)), CombineMode.Intersect);
            if (!first)
            {
                using var border = new SolidBrush(context.Theme.MessageBorder);
                context.Graphics.FillRectangle(border, context.Bounds.Left + inset, context.Bounds.Top, Math.Max(0, context.Bounds.Width - 2 * inset), 1);
            }
            var color = message.EffectiveEnabled ? context.Theme.MessageForeground : context.Theme.DisabledColor;
            if (!titleBounds.IsEmpty)
            {
                using var bold = new Font(context.Font, context.Theme.MessageAppearance?.TitleBold == false ? context.Font.Style : context.Font.Style | FontStyle.Bold);
                DrawText(context with { Bounds = titleBounds, Font = bold }, message.Title, color, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
            if (!textBounds.IsEmpty) DrawText(context with { Bounds = textBounds }, message.Text, color, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            if (message.Icon is { } icon) icon.Draw(context.Graphics, iconBounds, new(context.DeviceDpi, context.Theme.MessageIconForeground, message.EffectiveEnabled));
            else if (message.Image is { } image) context.Graphics.DrawImage(image, iconBounds);
            else
            {
                context.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(context.Theme.MessageIconForeground, Math.Max(1, context.DeviceDpi / 96f));
                float x = iconBounds.Left, y = iconBounds.Top, w = iconBounds.Width, h = iconBounds.Height;
                context.Graphics.DrawPolygon(pen, new PointF[] { new(x + w / 2, y + 1), new(x + w - 2, y + h / 4), new(x + w - 3, y + h * .7f), new(x + w / 2, y + h - 1), new(x + 3, y + h * .7f), new(x + 2, y + h / 4) });
                if (context.Theme.MessageAppearance?.ShowDefaultIconExclamation == true)
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    context.Graphics.DrawLine(pen, x + w / 2, y + h * .28f, x + w / 2, y + h * .61f);
                    using var dot = new SolidBrush(pen.Color); float size = Math.Max(1, context.DeviceDpi / 96f * 1.5f);
                    context.Graphics.FillEllipse(dot, x + w / 2 - size / 2, y + h * .76f - size / 2, size, size);
                }
            }
        }
        finally { context.Graphics.Restore(saved); }
    }
    /// <summary>Paints the measured optional message action, including ordinary enabled, hover, pressed and keyboard-focus states.</summary>
    public virtual void DrawMessageAction(RibbonPaintContext context, RibbonButtonItem action)
    {
        context = EnabledContext(context, action.EffectiveEnabled);
        var a = context.Theme.MessageAppearance;
        var fill = context.Pressed ? a?.ActionPressedBackground ?? context.Theme.Pressed : context.Hot ? a?.ActionHoverBackground ?? context.Theme.Hover : a?.ActionBackground ?? new(context.Theme.Surface);
        if (a?.HasActionChrome == true)
            PaintStateChrome(context, RibbonLayoutEngine.Scale(a.ActionCornerRadius ?? context.Theme.CornerRadius, context.DeviceDpi), fill,
                a.ActionStateChrome?.Border(context, context.Theme.Border) ?? context.Theme.Border,
                a.ActionBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, context.DeviceDpi) : 1, a.ActionStateChrome);
        else DrawChrome(context, fill, context.Theme.Border);
        int S(int n) => RibbonLayoutEngine.Scale(n, context.DeviceDpi);
        var text = Rectangle.Inflate(context.Bounds, -S(6), 0);
        if (action.Icon is not null || action.Image is not null)
        {
            DrawItemImage(context with { Bounds = new(context.Bounds.Left + S(6), context.Bounds.Top + (context.Bounds.Height - S(16)) / 2, S(16), S(16)) }, action, RibbonItemSize.Small);
            text = Rectangle.FromLTRB(text.Left + S(24), text.Top, text.Right, text.Bottom);
        }
        DrawText(context with { Bounds = text }, action.Text, action.EffectiveEnabled ? a is { ActionForeground.IsEmpty: false } ? a.ActionForeground : context.Theme.MessageForeground : context.Theme.DisabledColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints a measured message close target with the shared hover/pressed/focus policy.</summary>
    public virtual void DrawMessageClose(RibbonPaintContext context, bool enabled)
    {
        context = EnabledContext(context, enabled);
        if (context.Hot || context.Pressed)
        {
            var a = context.Theme.MessageAppearance;
            if (a?.HasCloseChrome == true)
                PaintStateChrome(context, RibbonLayoutEngine.Scale(a.CloseCornerRadius ?? context.Theme.CornerRadius, context.DeviceDpi), context.Pressed ? context.Theme.Pressed : context.Theme.Hover,
                    a.CloseStateChrome?.Border(context, context.Theme.MessageBorder) ?? context.Theme.MessageBorder,
                    a.CloseBorderWidth is { } width ? RibbonLayoutEngine.Scale(width, context.DeviceDpi) : 1, a.CloseStateChrome);
            else DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, context.Theme.MessageBorder);
        }
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float x = context.Bounds.Left + context.Bounds.Width / 2f, y = context.Bounds.Top + context.Bounds.Height / 2f, half = RibbonLayoutEngine.Scale(4, context.DeviceDpi);
            using var pen = new Pen(enabled ? context.Theme.MessageForeground : context.Theme.DisabledColor, Math.Max(1, 1.5f * context.DeviceDpi / 96)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            context.Graphics.DrawLine(pen, x - half, y - half, x + half, y + half); context.Graphics.DrawLine(pen, x - half, y + half, x + half, y - half);
        }
        finally { context.Graphics.Restore(saved); }
        if (context.Focused) DrawFocus(context);
    }
}
