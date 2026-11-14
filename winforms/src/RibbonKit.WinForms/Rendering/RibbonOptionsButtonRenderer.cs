using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

// Native Button properties/input remain authoritative. Reuse the dialog's owned
// painter and direct image/text/focus paths rather than command renderer hooks.
internal static class RibbonOptionsButtonRenderer
{
    internal static void Draw(RoundedPainter painter, RibbonPaintContext c, Button button, bool accent, bool hot, bool pressed, bool keyboardCues, bool focusCues, int fallbackRadius, Color parentBackground)
    {
        var theme = c.Theme; var m = new RibbonOptionsShellMetrics(theme, c.DeviceDpi); var bounds = c.Bounds;
        var fill = m.ButtonFill(accent, button.Enabled, hot, pressed);
        var color = m.ButtonForeground(accent, button.Enabled, hot, pressed);
        if (!accent) { using var background = new SolidBrush(parentBackground.A == 255 ? parentBackground : theme.Surface); c.Graphics.FillRectangle(background, bounds); }
        bool accentPress = button.Enabled && pressed && theme.OptionsButtonAccentStates is not null;
        var paint = m.ButtonBounds(bounds); var border = m.ButtonBorder(accentPress ? theme.Accent : accent ? fill.Start : theme.PopupBorder);
        var state = c with { Bounds = paint, Hot = button.Enabled && hot, Pressed = button.Enabled && pressed };
        var chrome = button.Enabled && (hot || pressed) ? theme.OptionsAppearance?.StateChrome : null;
        if (theme.OptionsAppearance is null || theme.OptionsAppearance.ButtonBorderColor.IsEmpty) border = chrome?.Border(state, border) ?? border;
        int stroke = m.ButtonStroke(paint, border, accent && !accentPress);
        if (chrome is not null && theme.OptionsAppearance?.ButtonBorderWidth is null && border.A != 0 && !chrome.Border(state, Color.Empty).IsEmpty) stroke = Math.Max(stroke, RibbonLayoutEngine.Scale(1, c.DeviceDpi));
        RibbonStateChromeRenderer.Paint(painter, state, RibbonFrameMetrics.Radius(theme.OptionsButtonCornerRadius, fallbackRadius, c.DeviceDpi), fill, border, stroke, chrome);
        var padding = button.Padding; var label = Rectangle.FromLTRB(bounds.Left + padding.Left, bounds.Top + padding.Top, Math.Max(bounds.Left + padding.Left, bounds.Right - padding.Right), Math.Max(bounds.Top + padding.Top, bounds.Bottom - padding.Bottom));
        if (button.Image is { } image)
        {
            int x = button.ImageAlign is ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft ? label.Left
                : button.ImageAlign is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight ? label.Right - image.Width : label.Left + (label.Width - image.Width) / 2;
            int y = button.ImageAlign is ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight ? label.Top
                : button.ImageAlign is ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight ? label.Bottom - image.Height : label.Top + (label.Height - image.Height) / 2;
            if (button.Enabled) c.Graphics.DrawImageUnscaled(image, x, y); else ControlPaint.DrawImageDisabled(c.Graphics, image, x, y, theme.Surface);
            if (button.TextImageRelation == TextImageRelation.ImageBeforeText) label = Rectangle.FromLTRB(Math.Min(label.Right, x + image.Width), label.Top, label.Right, label.Bottom);
            else if (button.TextImageRelation == TextImageRelation.TextBeforeImage) label.Width = Math.Max(0, x - label.Left);
        }
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        if (!accent)
        {
            if (button.TextAlign is ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft) flags &= ~TextFormatFlags.HorizontalCenter;
            else if (button.TextAlign is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight) flags = flags & ~TextFormatFlags.HorizontalCenter | TextFormatFlags.Right;
            if (button.TextAlign is ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight) flags &= ~TextFormatFlags.VerticalCenter;
            else if (button.TextAlign is ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight) flags = flags & ~TextFormatFlags.VerticalCenter | TextFormatFlags.Bottom;
        }
        if (!keyboardCues) flags |= TextFormatFlags.HidePrefix;
        TextRenderer.DrawText(c.Graphics, button.Text, c.Font, accent ? bounds : label, color, flags);
        if (focusCues) ControlPaint.DrawFocusRectangle(c.Graphics, m.ButtonFocusBounds(bounds), color, fill.Start);
    }
}
