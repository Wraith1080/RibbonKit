using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    internal virtual bool PaintConnectedMessageShadow(RibbonPaintContext context, Rectangle upper, Rectangle messages) => false;
    /// <summary>Gets whether message frames use body margins and connect to its/QAT's lower edge. Neutral rendering keeps a separate full-width stack.</summary>
    public virtual bool JoinMessageBarToBody => false;
    /// <summary>Paints the measured message stack frame; IsTopEdgeConnected distinguishes attached from standalone presentations.</summary>
    public virtual void DrawMessageBar(RibbonPaintContext context) => PaintRounded(context, 0, context.Theme.MessageBackground, context.Theme.MessageBorder);
    /// <summary>Paints a message's library-measured title/body/icon slots and optional row separator. Action and close targets have separate callbacks.</summary>
    public virtual void DrawMessage(RibbonPaintContext context, RibbonMessage message, Rectangle titleBounds, Rectangle textBounds, Rectangle iconBounds, bool first)
    {
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(new Rectangle(context.Bounds.Left + 1, context.Bounds.Top, Math.Max(0, context.Bounds.Width - 2), Math.Max(0, context.Bounds.Height - 1)), CombineMode.Intersect);
            if (!first)
            {
                using var border = new SolidBrush(context.Theme.MessageBorder);
                context.Graphics.FillRectangle(border, context.Bounds.Left + 1, context.Bounds.Top, Math.Max(0, context.Bounds.Width - 2), 1);
            }
            var color = message.EffectiveEnabled ? context.Theme.MessageForeground : SystemColors.GrayText;
            if (!titleBounds.IsEmpty)
            {
                using var bold = new Font(context.Font, context.Font.Style | FontStyle.Bold);
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
            }
        }
        finally { context.Graphics.Restore(saved); }
    }
    /// <summary>Paints the measured optional message action, including ordinary enabled, hover, pressed and keyboard-focus states.</summary>
    public virtual void DrawMessageAction(RibbonPaintContext context, RibbonButtonItem action)
    {
        context = EnabledContext(context, action.EffectiveEnabled);
        DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : new(context.Theme.Surface), context.Theme.Border);
        int S(int n) => RibbonLayoutEngine.Scale(n, context.DeviceDpi);
        var text = Rectangle.Inflate(context.Bounds, -S(6), 0);
        if (action.Icon is not null || action.Image is not null)
        {
            DrawItemImage(context with { Bounds = new(context.Bounds.Left + S(6), context.Bounds.Top + (context.Bounds.Height - S(16)) / 2, S(16), S(16)) }, action, RibbonItemSize.Small);
            text = Rectangle.FromLTRB(text.Left + S(24), text.Top, text.Right, text.Bottom);
        }
        DrawText(context with { Bounds = text }, action.Text, action.EffectiveEnabled ? context.Theme.MessageForeground : SystemColors.GrayText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints a measured message close target with the shared hover/pressed/focus policy.</summary>
    public virtual void DrawMessageClose(RibbonPaintContext context, bool enabled)
    {
        if (context.Hot || context.Pressed) DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, context.Theme.MessageBorder);
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float x = context.Bounds.Left + context.Bounds.Width / 2f, y = context.Bounds.Top + context.Bounds.Height / 2f, half = RibbonLayoutEngine.Scale(4, context.DeviceDpi);
            using var pen = new Pen(enabled ? context.Theme.MessageForeground : SystemColors.GrayText, Math.Max(1, 1.5f * context.DeviceDpi / 96)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            context.Graphics.DrawLine(pen, x - half, y - half, x + half, y + half); context.Graphics.DrawLine(pen, x - half, y + half, x + half, y - half);
        }
        finally { context.Graphics.Restore(saved); }
        if (context.Focused) DrawFocus(context);
    }
}
