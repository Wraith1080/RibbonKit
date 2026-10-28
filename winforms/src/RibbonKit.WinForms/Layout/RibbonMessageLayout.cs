namespace RibbonKit.WinForms.Layout;

internal sealed record MeasuredMessage(RibbonMessage Message, Rectangle Bounds, Rectangle Title, Rectangle Text, Rectangle Icon, Rectangle Action, Rectangle Close);
internal sealed record MessageLayout(int Height, List<MeasuredMessage> Rows);

internal static class RibbonMessageLayout
{
    internal static MessageLayout Measure(RibbonControl ribbon, int width, int dpi)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var rows = new List<MeasuredMessage>(); int y = 0;
        if (!ribbon.MessageBar.HasOpenMessages || width <= 0) return new(0, rows);
        bool touch = ribbon.Density == RibbonDensity.Touch;
        var padding = ribbon.Theme.MessageContentPadding;
        using var bold = new Font(ribbon.Font, ribbon.Font.Style | FontStyle.Bold);
        int MeasureText(string text, Font font, int maxWidth, bool wrap) => text.Length == 0 ? 0 : TextRenderer.MeasureText(text, font,
            new Size(Math.Max(1, maxWidth), int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | (wrap ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine)).Height;
        foreach (var message in ribbon.MessageBar.Items.Where(m => !m.IsDisposed && m.Visible && m.IsOpen))
        {
            int close = message.IsDismissible && !message.Close.IsDisposed ? S(touch ? 44 : 22) : 0;
            int left = Math.Min(width, S(padding.Left) + S(28)), right = Math.Max(left, width - S(padding.Right) - (close > 0 ? close + S(8) : 0));
            int actionWidth = message.HasAction ? Math.Min(Math.Max(1, width - S(padding.Horizontal)),
                Math.Max(S(touch ? 44 : 0), RibbonCaptionLayout.TextWidth(message.Action.Text, ribbon.Font) + S(message.Action.Icon is not null || message.Action.Image is not null ? 48 : 20))) : 0;
            bool actionBelow = actionWidth > 0 && right - left - actionWidth - S(10) < S(100);
            int textRight = right - (actionWidth > 0 && !actionBelow ? actionWidth + S(10) : 0), textWidth = Math.Max(1, textRight - left);
            int titleWidth = RibbonCaptionLayout.TextWidth(message.Title, bold), bodyWidth = RibbonCaptionLayout.TextWidth(message.Text, ribbon.Font);
            bool inline = message.Title.Length > 0 && message.Text.Length > 0 && titleWidth + S(8) + bodyWidth <= textWidth;
            int titleHeight = MeasureText(message.Title, bold, textWidth, !inline), bodyHeight = MeasureText(message.Text, ribbon.Font, textWidth, !inline);
            int textHeight = inline ? Math.Max(titleHeight, bodyHeight) : titleHeight + bodyHeight;
            int actionHeight = S(touch ? 44 : 26), mainHeight = Math.Max(S(20), Math.Max(textHeight, close)), contentHeight = mainHeight;
            if (actionWidth > 0 && actionBelow) contentHeight += S(4) + actionHeight;
            else if (actionWidth > 0) contentHeight = Math.Max(contentHeight, actionHeight);
            int height = Math.Max(S(Math.Max(ribbon.Theme.MessageMinimumHeight, touch ? 44 : 0)), contentHeight + S(padding.Vertical));
            int textTop = y + (actionBelow ? S(padding.Top) + (mainHeight - textHeight) / 2 : (height - textHeight) / 2);
            var title = message.Title.Length == 0 ? Rectangle.Empty : new Rectangle(left, textTop, inline ? titleWidth : textWidth, titleHeight);
            var text = message.Text.Length == 0 ? Rectangle.Empty : new Rectangle(inline ? left + titleWidth + S(8) : left,
                inline ? textTop : textTop + titleHeight, inline ? bodyWidth : textWidth, bodyHeight);
            var action = actionWidth == 0 ? Rectangle.Empty : new Rectangle(actionBelow ? left : right - actionWidth,
                actionBelow ? y + height - S(padding.Bottom) - actionHeight : y + (height - actionHeight) / 2, Math.Min(actionWidth, Math.Max(0, width - left - S(padding.Right))), actionHeight);
            var closeBounds = close == 0 ? Rectangle.Empty : new Rectangle(Math.Max(0, width - S(padding.Right) - close),
                y + (actionBelow ? S(padding.Top) + (mainHeight - close) / 2 : (height - close) / 2), Math.Min(width, close), close);
            rows.Add(new(message, new(0, y, width, height), title, text,
                new(S(padding.Left), y + (actionBelow ? S(padding.Top) + (mainHeight - S(20)) / 2 : (height - S(20)) / 2), S(20), S(20)), action, closeBounds));
            y += height;
        }
        return new(y, rows);
    }
    internal static MeasuredMessage Offset(MeasuredMessage row, int x, int y)
    {
        Rectangle Move(Rectangle r) => r.IsEmpty ? r : r with { X = r.X + x, Y = r.Y + y };
        return row with { Bounds = Move(row.Bounds), Title = Move(row.Title), Text = Move(row.Text), Icon = Move(row.Icon), Action = Move(row.Action), Close = Move(row.Close) };
    }
}
