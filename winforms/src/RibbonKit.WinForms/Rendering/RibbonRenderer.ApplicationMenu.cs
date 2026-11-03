using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

public partial class RibbonRenderer
{
    /// <summary>Paints the Application Menu's measured columns and fixed footer using shared menu surface and border roles. Native application content keeps its own styling.</summary>
    public virtual void DrawApplicationMenuBackground(RibbonPaintContext context, Rectangle navigation, Rectangle pane, Rectangle footer)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, context.DeviceDpi);
        using (var outer = new SolidBrush(ApplicationPaneColor(context with { ApplicationMenuDefaultPane = true }))) context.Graphics.FillRectangle(outer, context.Bounds);
        var frame = ApplicationFrame(context.Bounds, navigation);
        int radius = Math.Max(0, GetPopupCornerRadius(context.Theme, context.DeviceDpi) - 1);
        PaintRounded(context with { Bounds = frame }, radius, new(context.Theme.PopupColor), context.Theme.Border);
        int divider = pane.Left - S(10) - 1;
        var state = context.Graphics.Save();
        try
        {
            var inner = Rectangle.Inflate(frame, -1, -1);
            using var clip = RoundedPainter.CreateOpaqueRegion(inner.Size, Math.Max(0, radius - 1)); clip.Translate(inner.Left, inner.Top);
            context.Graphics.SetClip(clip, CombineMode.Intersect);
            var right = new Rectangle(divider + 1, frame.Top + 1, Math.Max(0, frame.Right - divider - 2), Math.Max(0, frame.Height - 2));
            using var fill = new SolidBrush(ApplicationPaneColor(context)); context.Graphics.FillRectangle(fill, right);
            using var border = new Pen(context.Theme.Border); context.Graphics.DrawLine(border, divider, frame.Top + 1, divider, frame.Bottom - 2);
        }
        finally { context.Graphics.Restore(state); }
    }
    internal static Rectangle ApplicationFrame(Rectangle bounds, Rectangle navigation) => new(navigation.Left - 1, navigation.Top - 1,
        Math.Max(1, bounds.Width - 2 * (navigation.Left - 1)), navigation.Height + 2);
    private static Color ApplicationPaneColor(RibbonPaintContext context)
    {
        var background = context.Theme.PopupColor; var border = context.Theme.Border;
        return !context.ApplicationMenuDefaultPane ? background : Color.FromArgb(255,
            (7 * background.R + border.R) / 8, (7 * background.G + border.G) / 8, (7 * background.B + border.B) / 8);
    }
    private void DrawApplicationMenuHeader(RibbonPaintContext context, string text)
    {
        var bounds = context.Bounds; int inset = RibbonLayoutEngine.Scale(context.ApplicationMenuDefaultPane ? 2 : 10, context.DeviceDpi);
        var background = context.Theme.MenuHeadingBackground ?? (context.ApplicationMenuDefaultPane ? (RibbonFill?)null : context.Theme.GalleryHeadingBackground);
        if (background is { } fill) PaintRounded(context, 0, fill, Color.Transparent, 0);
        using var font = new Font(context.Font, context.Font.Style | FontStyle.Bold);
        DrawText(context with { Bounds = Rectangle.Inflate(bounds, -inset, 0), Font = font }, text,
            context.Theme.MenuHeadingForeground.IsEmpty ? context.Theme.Accent : context.Theme.MenuHeadingForeground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        using var rule = new Pen(context.Theme.Border); context.Graphics.DrawLine(rule, bounds.Left, bounds.Bottom - 1, bounds.Right - 1, bounds.Bottom - 1);
    }
    private void DrawApplicationMenuItem(RibbonPaintContext context, RibbonItem item, bool arrow = true, bool paintChrome = true)
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, context.DeviceDpi);
        bool enabled = item.EffectiveEnabled; context = EnabledContext(context, enabled);
        var bounds = context.Bounds; bool footer = context.ApplicationMenuPart == RibbonApplicationMenuPart.Footer;
        if (item is RibbonMenuSeparatorItem separator) { DrawMenuSeparator(context, separator); return; }
        bool check = item is RibbonToggleItem { Checked: true } or RibbonCheckBoxItem { Checked: true } or RibbonRadioButtonItem { Checked: true } or RibbonSplitButtonItem { Checked: true };
        bool open = enabled && item is RibbonDropDownItem { IsDropDownOpen: true };
        if (paintChrome && (footer || context.Hot || context.Pressed || check || open))
        {
            var fill = context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : open ? context.Theme.Pressed : check ? context.Theme.Checked : new RibbonFill(context.Theme.PopupColor);
            PaintRounded(context with { Bounds = Rectangle.Inflate(bounds, -1, -1) }, footer ? S(3) : 0, fill, footer ? context.Theme.PopupBorder : Color.Transparent, footer ? 1 : 0);
        }
        bool navigation = context.ApplicationMenuPart == RibbonApplicationMenuPart.Navigation;
        bool rich = context.ApplicationMenuPart == RibbonApplicationMenuPart.Pane && RibbonApplicationMenuMetrics.Illustrated(item);
        bool hasIcon = RibbonApplicationMenuMetrics.HasIcon(item) || item is RibbonCheckBoxItem or RibbonRadioButtonItem;
        int iconSide = navigation || rich ? S(32) : S(16);
        int iconInset = footer ? S(10) : navigation || rich ? S(8) : S(6);
        int textInset = navigation || rich ? S(52) : hasIcon ? iconInset + iconSide + S(8) : S(6);
        var color = enabled ? context.Theme.Foreground : context.Theme.DisabledColor;
        int arrowWidth = item is RibbonDropDownItem && (arrow || item is RibbonSplitButtonItem) ? RibbonApplicationMenuMetrics.ArrowWidth(context.DeviceDpi, context.Density) : 0;
        var text = new Rectangle(bounds.Left + textInset, bounds.Top, Math.Max(0, bounds.Width - textInset - S(6) - arrowWidth), bounds.Height);
        var image = new Rectangle(bounds.Left + iconInset, bounds.Top + (bounds.Height - iconSide) / 2, iconSide, iconSide);
        if (footer)
        {
            int labelWidth = TextRenderer.MeasureText(item.Text, context.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int contentWidth = Math.Min(bounds.Width - S(12) - arrowWidth, labelWidth + (hasIcon ? S(24) : 0));
            int left = bounds.Left + Math.Max(S(6), (bounds.Width - arrowWidth - contentWidth) / 2);
            image.X = left; text.X = left + (hasIcon ? S(24) : 0); text.Width = Math.Max(0, bounds.Right - S(6) - arrowWidth - text.Left);
        }
        if (hasIcon)
        {
            if (item is RibbonCheckBoxItem box) DrawCheckBoxIndicator(context with { Bounds = image }, box);
            else if (item is RibbonRadioButtonItem radio) DrawRadioButtonIndicator(context with { Bounds = image }, radio);
            else DrawItemImage(context with { Bounds = image }, item, navigation || rich ? RibbonItemSize.Large : RibbonItemSize.Small);
        }
        bool described = !footer && item.MenuDescription.Length > 0;
        if (described)
        {
            int descriptionHeight = Math.Min(2 * context.Font.Height, TextRenderer.MeasureText(item.MenuDescription, context.Font, new(Math.Max(1, text.Width), int.MaxValue), RibbonMenuLayout.DescriptionFlags).Height);
            int top = bounds.Top + Math.Max(S(4), (bounds.Height - context.Font.Height - S(2) - descriptionHeight) / 2);
            text.Y = top; text.Height = context.Font.Height;
            using var title = rich ? new Font(context.Font, context.Font.Style | FontStyle.Bold) : null;
            DrawText(context with { Bounds = text, Font = title ?? context.Font }, item.Text, rich && enabled ? context.Theme.SecondaryText : color,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            var description = new Rectangle(text.Left, text.Bottom + S(2), text.Width, Math.Max(0, Math.Min(descriptionHeight, bounds.Bottom - S(4) - text.Bottom - S(2))));
            DrawText(context with { Bounds = description }, item.MenuDescription, color, RibbonMenuLayout.DescriptionFlags);
        }
        else DrawText(context with { Bounds = text }, item.Text, color, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        if (arrow && arrowWidth > 0) DrawSubmenuArrow(context with { Bounds = new(bounds.Right - arrowWidth, bounds.Top, arrowWidth, bounds.Height) }, enabled ? context.Theme.SecondaryText : color);
        if (context.Focused) DrawFocus(context);
    }
    private void DrawApplicationMenuSplit(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item)
    {
        primary = EnabledContext(primary, item.EffectiveEnabled); dropDown = EnabledContext(dropDown, item.EffectiveEnabled);
        var whole = primary with { Bounds = Rectangle.Union(primary.Bounds, dropDown.Bounds), Hot = false, Pressed = false, Focused = false };
        bool footer = primary.ApplicationMenuPart == RibbonApplicationMenuPart.Footer;
        bool open = item.EffectiveEnabled && item.IsDropDownOpen;
        bool rowHot = primary.Hot || dropDown.Hot;
        bool active = open || item.Checked || primary.Hot || dropDown.Hot || primary.Pressed || dropDown.Pressed;
        if (footer || active)
        {
            Half(primary, item.Checked, false, selected: open); Half(dropDown, false, open, selected: false);
            if (active)
            {
                using var pen = new Pen(primary.Theme.Border);
                primary.Graphics.DrawLine(pen, dropDown.Bounds.Left, whole.Bounds.Top + 1, dropDown.Bounds.Left, whole.Bounds.Bottom - 2);
            }
        }
        DrawApplicationMenuItem(whole, item, false, paintChrome: false);
        DrawSubmenuArrow(dropDown, item.EffectiveEnabled ? primary.Theme.SecondaryText : primary.Theme.DisabledColor);
        if (primary.Focused) DrawFocus(primary); if (dropDown.Focused) DrawFocus(dropDown);
        static Color SelectedPrimary(Color background, Color pressed) => Color.FromArgb(255,
            (3 * background.R + pressed.R) / 4, (3 * background.G + pressed.G) / 4, (3 * background.B + pressed.B) / 4);
        void Half(RibbonPaintContext half, bool check, bool expanded, bool selected)
        {
            var state = whole.Graphics.Save();
            try
            {
                // Both action regions clip the same inset perimeter. The seam
                // stays square, and the arrow never grows past the primary.
                whole.Graphics.SetClip(half.Bounds, CombineMode.Intersect);
                var fill = half.Pressed ? half.Theme.Pressed : rowHot ? half.Theme.Hover : expanded ? half.Theme.Pressed
                    : selected ? new RibbonFill(SelectedPrimary(half.Theme.PopupColor, half.Theme.Pressed.Start), SelectedPrimary(half.Theme.PopupColor, half.Theme.Pressed.End))
                    : check ? half.Theme.Checked : new RibbonFill(half.Theme.PopupColor);
                PaintRounded(whole with { Bounds = Rectangle.Inflate(whole.Bounds, -1, -1) }, footer ? RibbonLayoutEngine.Scale(3, whole.DeviceDpi) : 0,
                    fill, footer ? whole.Theme.PopupBorder : Color.Transparent, footer ? 1 : 0);
            }
            finally { whole.Graphics.Restore(state); }
        }
    }
    internal bool DrawApplicationFileButton(RibbonPaintContext context, RibbonDropDownItem menu)
    {
        if (!menu.EffectiveEnabled || !menu.IsDropDownOpen || context.FileMenuClosing || menu.Ribbon?.FileMenuMode != RibbonFileMenuMode.CommandMenu) return false;
        int inset = RibbonLayoutEngine.Scale(3, context.DeviceDpi);
        var bounds = Rectangle.Inflate(context.Bounds, -inset, -inset);
        PaintRounded(context with { Bounds = bounds }, inset, new(context.Theme.Accent), Color.Transparent, 0);
        static double Channel(byte channel) { double v = channel / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
        var accent = context.Theme.Accent;
        var foreground = .2126 * Channel(accent.R) + .7152 * Channel(accent.G) + .0722 * Channel(accent.B) > .179 ? Color.Black : Color.White;
        DrawText(context with { Bounds = bounds }, menu.Text, foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (context.Focused) DrawFocus(context with { Bounds = bounds }); return true;
    }
}
