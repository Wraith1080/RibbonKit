using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>Flat rendering foundation with shared content drawing over library-measured device-pixel geometry.</summary>
public partial class RibbonRenderer : IDisposable
{
    private readonly RoundedPainter _rounded = new();
    private readonly ShadowPainter _shadow = new();
    private readonly RibbonCaptionLayout _captions = new();
    /// <summary>Gets the floating popup corner radius in device pixels. The flat renderer returns zero; frame, clipping and shadow share this value.</summary>
    public virtual int GetPopupCornerRadius(RibbonTheme theme, int deviceDpi) => 0;
    /// <summary>Paints the outer frame of a floating ribbon popup. The hosted content is inset by one device pixel.</summary>
    public virtual void DrawPopupFrame(RibbonPaintContext context) =>
        PaintRounded(context, GetPopupCornerRadius(context.Theme, context.DeviceDpi), new(context.Theme.Surface), context.Theme.PopupBorder);
    /// <summary>Paints a floating popup's shadow onto a transparent surface. Bounds describe the popup; eight logical pixels of clearance surround it.</summary>
    public virtual void DrawPopupShadow(RibbonPaintContext context)
    {
        var bounds = context.Bounds;
        bounds.Offset(0, RibbonLayoutEngine.Scale(2, context.DeviceDpi));
        _shadow.Draw(context.Graphics, bounds, GetPopupCornerRadius(context.Theme, context.DeviceDpi), RibbonLayoutEngine.Scale(6, context.DeviceDpi), context.Theme.PopupShadowColor);
    }
    /// <summary>Paints the ribbon or popup background.</summary>
    public virtual void DrawSurface(RibbonPaintContext context, bool popup)
    {
        using var brush = new SolidBrush(popup ? context.Theme.Surface : context.Theme.Background);
        context.Graphics.FillRectangle(brush, context.Bounds);
    }
    /// <summary>Paints the ribbon body panel. Shadows may extend into its surrounding margins; item clipping is handled separately.</summary>
    public virtual void DrawBody(RibbonPaintContext context)
    {
        using var brush = new SolidBrush(context.Theme.Surface);
        context.Graphics.FillRectangle(brush, context.Bounds);
    }
    /// <summary>Paints a measured horizontal tab-strip or command-body scroll button.</summary>
    public virtual void DrawScrollButton(RibbonPaintContext context, bool backward)
    {
        if (context.IsBodyScrollButton)
        {
            DrawBodyScrollButton(context, backward);
            return;
        }
        else
        {
            using var brush = new SolidBrush(context.Theme.Background);
            context.Graphics.FillRectangle(brush, context.Bounds);
        }
        DrawText(context, backward ? "‹" : "›", context.Theme.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
    internal void DrawBodyScrollButton(RibbonPaintContext context, bool backward)
    {
        DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : new(context.Theme.Surface),
            context.Hot || context.Pressed ? context.Theme.Accent : context.Theme.PopupBorder);
        RibbonArrowPainter.DrawHorizontal(context.Graphics, context.Bounds, context.DeviceDpi, context.Theme.Foreground, backward);
    }
    /// <summary>Paints an opaque vertical overflow-menu scroll band in device-pixel bounds, including its direction and endpoint availability.</summary>
    public virtual void DrawOverflowScrollButton(RibbonPaintContext context, bool upward, bool enabled)
    {
        using (var background = new SolidBrush(Color.FromArgb(255, context.Theme.Surface)))
            context.Graphics.FillRectangle(background, context.Bounds);
        if (enabled && (context.Hot || context.Pressed))
            DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
        RibbonArrowPainter.DrawVertical(context.Graphics, context.Bounds, context.DeviceDpi,
            enabled ? context.Theme.Foreground : SystemColors.GrayText, upward);
    }
    /// <summary>Paints the measured tab-row minimize/expand button. A minimized ribbon shows a downward chevron.</summary>
    public virtual void DrawMinimizeButton(RibbonPaintContext context, bool minimized)
    {
        // Like the tab scrollers, this utility owns its opaque tab-row slot.
        // GDI text from an overflowing tab must not show through beneath it.
        using (var background = new SolidBrush(context.Theme.Background)) context.Graphics.FillRectangle(background, context.Bounds);
        context = context with { Bounds = RibbonHeaderButtonLayout.PaintBounds(context.Bounds, context.Theme, context.DeviceDpi) };
        if (context.Bounds.Width <= 0 || context.Bounds.Height <= 0) return;
        var state = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(context.Bounds, System.Drawing.Drawing2D.CombineMode.Intersect);
            if (context.Hot || context.Pressed) DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
            RibbonArrowPainter.DrawVertical(context.Graphics, context.Bounds, context.DeviceDpi, context.Theme.Foreground, !minimized);
        }
        finally { context.Graphics.Restore(state); }
    }
    /// <summary>Paints the measured Quick Access strip using the shared palette.</summary>
    public virtual void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
    {
        using var brush = new SolidBrush(context.Theme.Background);
        context.Graphics.FillRectangle(brush, context.Bounds);
        using var border = new SolidBrush(context.Theme.Border);
        if (position == RibbonQuickAccessPosition.TabRow)
            context.Graphics.FillRectangle(border, context.Bounds.Right - 1, context.Bounds.Top + 6, 1, Math.Max(0, context.Bounds.Height - 12));
    }
    /// <summary>Paints a toolbar entry or overflow row. References read command state without changing its owner or model.</summary>
    public virtual void DrawQuickAccessItem(RibbonPaintContext context, RibbonElement entry, bool menu)
    {
        bool enabled = RibbonQuickAccess.Available(entry);
        context = EnabledContext(context, enabled);
        var command = RibbonQuickAccess.Command(entry);
        bool check = command is RibbonToggleItem { Checked: true } or RibbonCheckBoxItem { Checked: true } or RibbonRadioButtonItem { Checked: true } or RibbonSplitButtonItem { Checked: true };
        bool open = entry is RibbonQuickAccessComboBoxReference { IsDropDownOpen: true } or RibbonQuickAccessDropDownReference { IsDropDownOpen: true } or RibbonQuickAccessGroupReference { IsDropDownOpen: true };
        if (check || open || context.Hot || context.Pressed)
            DrawChrome(context, open || context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, Color.Transparent);
        DrawQuickAccessContent(context, entry, menu);
        if (command is RibbonCheckBoxItem { CheckState: CheckState.Indeterminate })
        {
            // Preserve the action icon; a small bar below it distinguishes mixed from checked.
            int S(double value) => RibbonLayoutEngine.Scale(value, context.DeviceDpi);
            int center = menu ? context.Bounds.Left + S(14) : context.Bounds.Left + context.Bounds.Width / 2;
            int top = Math.Min(context.Bounds.Bottom - S(3), context.Bounds.Top + context.Bounds.Height / 2 + S(9));
            using var mark = new SolidBrush(enabled ? context.Theme.Foreground : SystemColors.GrayText);
            context.Graphics.FillRectangle(mark, center - S(3), top, S(6), S(2));
        }
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints a borrowed split command with separate measured primary and menu states. The source's checked state belongs to the primary half.</summary>
    public virtual void DrawQuickAccessSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonQuickAccessDropDownReference entry, bool menu)
    {
        bool enabled = RibbonQuickAccess.Available(entry);
        primary = EnabledContext(primary, enabled); dropDown = EnabledContext(dropDown, enabled);
        bool check = entry.Source is RibbonSplitButtonItem { Checked: true };
        var whole = primary with { Bounds = Rectangle.Union(primary.Bounds, dropDown.Bounds), Hot = false, Pressed = false, Focused = false };
        bool open = enabled && entry.IsDropDownOpen;
        if (check || open || primary.Hot || dropDown.Hot || primary.Pressed || dropDown.Pressed)
        {
            Half(primary, check, false); Half(dropDown, false, open);
            using var separator = new SolidBrush(primary.Theme.Border);
            primary.Graphics.FillRectangle(separator, dropDown.Bounds.Left, whole.Bounds.Top + 1, 1, Math.Max(0, whole.Bounds.Height - 2));
        }
        DrawQuickAccessContent(primary, entry, menu);
        var color = enabled ? primary.Theme.Foreground : SystemColors.GrayText;
        if (menu) DrawSubmenuArrow(dropDown, color); else DrawDropDownArrow(dropDown, color);
        if (primary.Focused) DrawFocus(primary);
        if (dropDown.Focused) DrawFocus(dropDown);

        void Half(RibbonPaintContext part, bool marked, bool expanded)
        {
            var saved = whole.Graphics.Save();
            try
            {
                whole.Graphics.SetClip(part.Bounds, CombineMode.Intersect);
                DrawChrome(whole, part.Pressed || expanded ? whole.Theme.Pressed : part.Hot ? whole.Theme.Hover : marked ? whole.Theme.Checked : new(whole.Theme.Surface), Color.Transparent);
            }
            finally { whole.Graphics.Restore(saved); }
        }
    }
    private void DrawQuickAccessContent(RibbonPaintContext context, RibbonElement entry, bool menu)
    {
        bool enabled = RibbonQuickAccess.Available(entry);
        var command = RibbonQuickAccess.Command(entry);
        bool chooser = entry is RibbonQuickAccessComboBoxReference or RibbonQuickAccessGroupReference || entry is RibbonQuickAccessDropDownReference { Source: not RibbonSplitButtonItem };
        var caption = RibbonQuickAccess.Caption(entry);
        var color = enabled ? context.Theme.Foreground : SystemColors.GrayText;
        var contentContext = context;
        if (chooser) contentContext = context with { Bounds = context.Bounds with { Width = Math.Max(0, context.Bounds.Width - RibbonLayoutEngine.Scale(menu ? 16 : 12, context.DeviceDpi)) } };
        if (menu && entry is RibbonQuickAccessGroupReference)
        {
            var label = contentContext with { Bounds = Rectangle.FromLTRB(contentContext.Bounds.Left + RibbonLayoutEngine.Scale(34, context.DeviceDpi),
                contentContext.Bounds.Top, contentContext.Bounds.Right, contentContext.Bounds.Bottom) };
            DrawText(label, caption, color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        else if (menu && command is not null)
            DrawItemContent(contentContext, command, RibbonItemSize.Medium, true, appendArrow: false, textOverride: caption, enabledOverride: enabled);
        else
        {
            int iconSize = RibbonLayoutEngine.Scale(16, context.DeviceDpi);
            var icon = new Rectangle(contentContext.Bounds.Left + (contentContext.Bounds.Width - iconSize) / 2, context.Bounds.Top + (context.Bounds.Height - iconSize) / 2, iconSize, iconSize);
            if (command is not null && (command.Icon is not null || command.Image is not null || command.LargeImage is not null))
                DrawQuickAccessImage(context with { Bounds = icon }, command, enabled);
            else
                DrawText(contentContext, menu ? caption : caption.Length == 0 ? "" : System.Globalization.StringInfo.GetNextTextElement(caption), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        if (chooser)
        {
            var arrow = context with { Bounds = Rectangle.FromLTRB(contentContext.Bounds.Right, context.Bounds.Top, context.Bounds.Right, context.Bounds.Bottom) };
            if (menu) DrawSubmenuArrow(arrow, color); else DrawDropDownArrow(arrow, color);
        }
    }
    private void DrawQuickAccessImage(RibbonPaintContext context, RibbonItem command, bool enabled)
    {
        if (enabled == command.EffectiveEnabled) { DrawItemImage(context, command, RibbonItemSize.Small); return; }
        if (command.Icon is { } icon) icon.Draw(context.Graphics, context.Bounds, new(context.DeviceDpi, SystemColors.GrayText, false));
        else if ((command.Image ?? command.LargeImage) is { } image)
        {
            using var bitmap = new Bitmap(image, context.Bounds.Size);
            ControlPaint.DrawImageDisabled(context.Graphics, bitmap, context.Bounds.X, context.Bounds.Y, context.Theme.Background);
        }
    }
    /// <summary>Paints the measured overflow opener. Keyboard and pointer activation open the same owned popup.</summary>
    public virtual void DrawQuickAccessOverflow(RibbonPaintContext context)
    {
        if (context.Hot || context.Pressed) DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
        DrawDropDownArrow(context, context.Theme.Foreground);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints text inside a measured content slot, preserving its layout flags.</summary>
    public virtual void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) =>
        TextRenderer.DrawText(context.Graphics, text, context.Font, context.Bounds, color, flags);
    /// <summary>Paints passive ScreenTip content in library-measured title and description slots. The popup frame and shadow use the existing popup hooks.</summary>
    public virtual void DrawScreenTip(RibbonPaintContext context, string title, string description, Rectangle titleBounds, Rectangle descriptionBounds)
    {
        using var titleFont = new Font(context.Font, context.Font.Style | FontStyle.Bold);
        DrawText(context with { Bounds = titleBounds, Font = titleFont }, title, context.Theme.Foreground, RibbonScreenTipLayout.TextFlags);
        if (!descriptionBounds.IsEmpty)
            DrawText(context with { Bounds = descriptionBounds }, description, context.Theme.Foreground, RibbonScreenTipLayout.TextFlags);
    }
    /// <summary>Paints a DPI-scaled dropdown indicator centered inside a measured slot.</summary>
    public virtual void DrawDropDownArrow(RibbonPaintContext context, Color color) =>
        RibbonArrowPainter.DrawDown(context.Graphics, context.Bounds, context.DeviceDpi, color);
    /// <summary>Paints the right-pointing indicator for a nested command menu in its measured slot.</summary>
    public virtual void DrawSubmenuArrow(RibbonPaintContext context, Color color) =>
        RibbonArrowPainter.DrawHorizontal(context.Graphics, context.Bounds, context.DeviceDpi, color, false);
    /// <summary>Paints a decorative command-menu separator within its measured row. It has no interaction state.</summary>
    public virtual void DrawMenuSeparator(RibbonPaintContext context, RibbonMenuSeparatorItem item)
    {
        int inset = RibbonLayoutEngine.Scale(8, context.DeviceDpi);
        if (context.Bounds.Width <= 2 * inset || context.Bounds.Height <= 0) return;
        using var brush = new SolidBrush(context.Theme.Border);
        context.Graphics.FillRectangle(brush, context.Bounds.Left + inset, context.Bounds.Top + (context.Bounds.Height - 1) / 2, context.Bounds.Width - 2 * inset, 1);
    }
    /// <summary>Paints a native editor's label and frame. The editor itself owns text, caret and selection.</summary>
    public virtual void DrawTextBox(RibbonPaintContext context, RibbonTextBoxItem item, Rectangle editorBounds)
    {
        DrawEditorLabel(context, item, editorBounds);
        DrawChrome(context with { Bounds = editorBounds }, new(context.Theme.Surface),
            context.Focused && item.EffectiveEnabled ? context.Theme.Accent : context.Theme.Border);
    }
    /// <summary>Paints a ComboBox label, frame and arrow. Text input remains in the native editor.</summary>
    public virtual void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle editorBounds)
    {
        DrawEditorLabel(context, item, editorBounds);
        context = EnabledContext(context, item.EffectiveEnabled);
        bool open = item.IsDropDownOpen && item.EffectiveEnabled;
        var field = context with { Bounds = editorBounds };
        var border = context.Hot || context.Focused || context.Pressed || open ? context.Theme.Accent : context.Theme.Border;
        DrawChrome(field, new(context.Theme.Surface), border);
        var button = RibbonEditorLayout.ComboButton(editorBounds, context.DeviceDpi, context.Density);
        if (context.Hot || context.Pressed || open)
        {
            var saved = context.Graphics.Save();
            try
            {
                context.Graphics.SetClip(button, CombineMode.Intersect);
                DrawChrome(field, context.Pressed ? context.Theme.Pressed : open ? context.Theme.Checked : context.Theme.Hover, border);
            }
            finally { context.Graphics.Restore(saved); }
        }
        DrawDropDownArrow(context with { Bounds = button }, item.EffectiveEnabled ? context.Theme.Foreground : SystemColors.GrayText);
    }
    /// <summary>Paints a native choice row. Selected is the native selection or keyboard preview; Hot and Pressed describe pointer interaction separately.</summary>
    public virtual void DrawComboBoxItem(RibbonPaintContext context, RibbonComboBoxItem item, int index, bool selected)
    {
        using var surface = new SolidBrush(context.Theme.Surface);
        context.Graphics.FillRectangle(surface, context.Bounds);
        context = EnabledContext(context, item.EffectiveEnabled);
        if (selected || context.Hot || context.Pressed)
            DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, Color.Transparent);
        DrawText(context with { Bounds = Rectangle.Inflate(context.Bounds, -RibbonLayoutEngine.Scale(6, context.DeviceDpi), 0) },
            item.Items[index], item.EffectiveEnabled ? context.Theme.Foreground : SystemColors.GrayText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
    private void DrawEditorLabel(RibbonPaintContext context, RibbonItem item, Rectangle editorBounds) =>
        DrawText(context with { Bounds = new(context.Bounds.Left, context.Bounds.Top, Math.Max(0, editorBounds.Left - context.Bounds.Left - RibbonLayoutEngine.Scale(6, context.DeviceDpi)), context.Bounds.Height) },
            item.Text, item.EffectiveEnabled ? context.Theme.Foreground : SystemColors.GrayText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    /// <summary>Paints the File menu header using library-measured bounds and its original open/availability state.</summary>
    public virtual void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu)
    {
        bool enabled = menu.EffectiveEnabled;
        context = EnabledContext(context, enabled);
        bool open = enabled && menu.IsDropDownOpen;
        if (open || context.Hot || context.Pressed)
            DrawChrome(context, open || context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
        DrawText(context, menu.Text, !enabled ? SystemColors.GrayText : open ? context.Theme.Accent : context.Theme.Foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints a tab using library-measured bounds.</summary>
    public virtual void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        context = EnabledContext(context, tab.EffectiveEnabled);
        if (selected || context.Hot)
            DrawChrome(context, selected ? new(context.Theme.Surface) : context.Theme.Hover, Color.Transparent);
        var color = !tab.EffectiveEnabled ? SystemColors.GrayText : tab.IsContextual ? ContextualTabColor(context, tab, selected) : context.Theme.Foreground;
        DrawText(context, tab.Text, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        if (context.Focused)
            DrawFocus(context);
    }
    internal static Color ContextualTabColor(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        var color = tab.ContextualColor.IsEmpty ? context.Theme.Accent : tab.ContextualColor;
        // WPF Office 2024 ContextualUnselectedOpacity: composite before GDI text drawing.
        double opacity = color.A / 255d * (selected ? 1 : 0.6);
        var background = context.Theme.Background;
        int Mix(int foreground, int behind) => (int)Math.Round(foreground * opacity + behind * (1 - opacity));
        return Color.FromArgb(Mix(color.R, background.R), Mix(color.G, background.G), Mix(color.B, background.B));
    }
    /// <summary>Paints the group caption or collapsed opener.</summary>
    public virtual void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
    {
        context = EnabledContext(context, group.EffectiveEnabled);
        if (collapsed)
        {
            bool open = group.EffectiveEnabled && group.Ribbon?.Popups.IsExpanded(group) == true;
            if (open || context.Hot || context.Pressed)
                DrawChrome(context, open || context.Pressed ? context.Theme.Pressed : context.Theme.Hover, context.Theme.Border);
            var bounds = Rectangle.Inflate(context.Bounds, -5, -5);
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            int arrowHeight = RibbonLayoutEngine.Scale(6, context.DeviceDpi), gap = RibbonLayoutEngine.Scale(5, context.DeviceDpi);
            int textHeight = Math.Min(Math.Max(0, bounds.Height - arrowHeight - gap), TextRenderer.MeasureText(group.Text, context.Font, new Size(Math.Max(1, bounds.Width), int.MaxValue), flags).Height);
            int top = bounds.Top + (bounds.Height - textHeight - gap - arrowHeight) / 2;
            var color = group.EffectiveEnabled ? context.Theme.Foreground : SystemColors.GrayText;
            DrawText(context with { Bounds = new(bounds.Left, top, bounds.Width, textHeight) }, group.Text, color, flags);
            DrawDropDownArrow(context with { Bounds = new(bounds.Left, top + textHeight + gap, bounds.Width, arrowHeight) }, color);
            if (context.Focused)
                DrawFocus(context);
        }
        else
        {
            int height = Math.Max(context.Font.Height + RibbonLayoutEngine.Scale(6, context.DeviceDpi), RibbonLayoutEngine.Scale(20, context.DeviceDpi));
            var caption = new Rectangle(context.Bounds.X + 4, context.Bounds.Bottom - height, Math.Max(0, context.Bounds.Width - 8), height);
            DrawText(context with { Bounds = caption }, group.Text, context.Theme.SecondaryText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
    /// <summary>Paints a command using library-measured bounds and shared content alignment.</summary>
    public virtual void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
    {
        if (item is RibbonMenuSeparatorItem separator)
        {
            if (menu) DrawMenuSeparator(context with { Hot = false, Pressed = false, Focused = false }, separator);
            return;
        }
        bool enabled = item.EffectiveEnabled;
        context = EnabledContext(context, enabled);
        if (menu && RibbonMenuLayout.IsIllustratedChoice(item)) { DrawIllustratedChoice(context, item); return; }
        if (item is RibbonGalleryItem { IsInRibbon: true } && size == RibbonItemSize.Large && !menu && !context.IsGalleryDropDown)
        { DrawGalleryStripFrame(context); return; }
        if (item is RibbonCheckBoxItem or RibbonRadioButtonItem)
        {
            if (context.Hot || context.Pressed)
                DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
            var content = RibbonContentLayout.Calculate(context.Bounds, RibbonItemSize.Medium, menu, true, context.DeviceDpi, context.Font);
            if (menu) content = RibbonMenuLayout.Content(content, item, context.Bounds, context.Font, context.DeviceDpi, context.Density);
            if (item is RibbonCheckBoxItem box) DrawCheckBoxIndicator(context with { Bounds = content.Icon }, box);
            else DrawRadioButtonIndicator(context with { Bounds = content.Icon }, (RibbonRadioButtonItem)item);
            DrawText(context with { Bounds = content.Text }, item.Text, enabled ? context.Theme.Foreground : SystemColors.GrayText, content.Flags);
            if (menu) DrawMenuDescription(context, item, content.Text);
            if (context.Focused) DrawFocus(context);
            return;
        }
        bool check = item is RibbonToggleItem { Checked: true };
        bool open = enabled && item is RibbonDropDownItem { IsDropDownOpen: true };
        if (open || check || context.Hot || context.Pressed)
            DrawChrome(context, open || context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, Color.Transparent);
        DrawItemContent(context, item, size, menu);
        if (context.Focused)
            DrawFocus(context);
    }
    /// <summary>Paints a noninteractive command-dropdown heading. Uses the renderer's existing themed heading treatment.</summary>
    public virtual void DrawMenuHeader(RibbonPaintContext context, string text) => DrawGalleryHeading(context, text);

    private void DrawIllustratedChoice(RibbonPaintContext context, RibbonItem item)
    {
        bool selected = item is RibbonToggleItem { Checked: true } or RibbonRadioButtonItem { Checked: true } or RibbonCheckBoxItem { Checked: true };
        if (selected || context.Hot || context.Pressed)
            DrawChrome(context, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, Color.Transparent);
        var content = RibbonMenuLayout.IllustratedContent(context.Bounds, context.Font, context.DeviceDpi, RibbonMenuLayout.HasDescription(item));
        var saved = context.Graphics.Save();
        try { context.Graphics.SetClip(content.Icon, CombineMode.Intersect); DrawItemImage(context with { Bounds = content.Icon }, item, RibbonItemSize.Large); }
        finally { context.Graphics.Restore(saved); }
        using var titleFont = new Font(context.Font, context.Font.Style | FontStyle.Bold);
        DrawText(context with { Bounds = content.Text, Font = titleFont }, item.Text, item.EffectiveEnabled ? context.Theme.Foreground : SystemColors.GrayText, content.Flags);
        DrawMenuDescription(context, item, content.Text, illustrated: true);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints a check indicator in its measured compact slot. The flat foundation uses square corners.</summary>
    public virtual void DrawCheckBoxIndicator(RibbonPaintContext context, RibbonCheckBoxItem item) =>
        PaintCheckIndicator(EnabledContext(context, item.EffectiveEnabled), item.CheckState, item.EffectiveEnabled, 0);
    /// <summary>Paints a circular radio indicator in its measured compact slot.</summary>
    public virtual void DrawRadioButtonIndicator(RibbonPaintContext context, RibbonRadioButtonItem item)
    {
        bool enabled = item.EffectiveEnabled;
        context = EnabledContext(context, enabled);
        var border = !enabled ? SystemColors.GrayText : context.Hot || context.Pressed || context.Focused || item.Checked ? context.Theme.Accent : context.Theme.SecondaryText;
        PaintRounded(context, context.Bounds.Width / 2, context.Pressed ? context.Theme.Pressed : new(context.Theme.Surface), border);
        if (item.Checked)
        {
            int inset = Layout.RibbonLayoutEngine.Scale(4, context.DeviceDpi);
            var dot = Rectangle.Inflate(context.Bounds, -inset, -inset);
            PaintRounded(context with { Bounds = dot }, dot.Width / 2, new(enabled ? context.Theme.Accent : SystemColors.GrayText), Color.Transparent, 0);
        }
    }
    internal void PaintCheckIndicator(RibbonPaintContext context, CheckState check, bool enabled, int radius)
    {
        bool marked = check != CheckState.Unchecked;
        var border = !enabled ? SystemColors.GrayText : marked || context.Hot || context.Pressed || context.Focused ? context.Theme.Accent : context.Theme.SecondaryText;
        var fill = marked ? new RibbonFill(enabled ? context.Theme.Accent : SystemColors.GrayText)
            : context.Pressed ? context.Theme.Pressed : new(context.Theme.Surface);
        PaintRounded(context, radius, fill, border);
        if (!marked) return;
        var bounds = context.Bounds;
        float centerX = bounds.Left + (bounds.Width - 1) / 2f;
        float centerY = bounds.Top + (bounds.Height - 1) / 2f;
        // The bend and long arm carry unequal ink. Centre that visual weight
        // on the box's pixel centre, retaining the tick's shape and size.
        float checkLeft = centerX - bounds.Width * .515f;
        float checkTop = centerY - bounds.Height * .54f;
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(context.Theme.Surface, Math.Max(1, 1.5f * context.DeviceDpi / 96))
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            if (check == CheckState.Indeterminate)
                context.Graphics.DrawLine(pen, centerX - bounds.Width * .25f, centerY,
                    centerX + bounds.Width * .25f, centerY);
            else context.Graphics.DrawLines(pen, new PointF[]
            {
                new(checkLeft + bounds.Width * .22f, checkTop + bounds.Height * .51f),
                new(checkLeft + bounds.Width * .43f, checkTop + bounds.Height * .72f),
                new(checkLeft + bounds.Width * .79f, checkTop + bounds.Height * .29f)
            });
        }
        finally { context.Graphics.Restore(saved); }
    }
    /// <summary>Paints a split command from its independently measured primary and dropdown action contexts.</summary>
    public virtual void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
    {
        primary = EnabledContext(primary, item.EffectiveEnabled);
        dropDown = EnabledContext(dropDown, item.EffectiveEnabled);
        var whole = primary with { Bounds = Rectangle.Union(primary.Bounds, dropDown.Bounds), Hot = false, Pressed = false, Focused = false };
        bool open = item.EffectiveEnabled && item.IsDropDownOpen;
        bool active = item.Checked || open || primary.Hot || dropDown.Hot || primary.Pressed || dropDown.Pressed;
        bool vertical = RibbonSplitLayout.IsVertical(item, size, menu);
        if (active)
        {
            // Each half clips the same outer contour. Insets and rounding belong
            // to the perimeter, leaving the shared edge square and gap-free.
            DrawHalf(primary, item.Checked, false);
            DrawHalf(dropDown, false, open);
            using var separator = new SolidBrush(whole.Theme.Border);
            var seam = vertical
                ? new Rectangle(whole.Bounds.Left + 1, dropDown.Bounds.Top, Math.Max(0, whole.Bounds.Width - 2), 1)
                : new Rectangle(dropDown.Bounds.Left, whole.Bounds.Top + 1, 1, Math.Max(0, whole.Bounds.Height - 2));
            whole.Graphics.FillRectangle(separator, seam);
        }
        DrawItemContent(vertical ? whole : primary, item, size, menu, vertical, vertical);
        if (!vertical)
        {
            var arrowContext = whole with { Bounds = dropDown.Bounds };
            var color = item.EffectiveEnabled ? whole.Theme.Foreground : SystemColors.GrayText;
            if (menu) DrawSubmenuArrow(arrowContext, color);
            else DrawDropDownArrow(arrowContext, color);
        }
        if (primary.Focused)
            DrawFocus(primary);
        if (dropDown.Focused)
            DrawFocus(dropDown);

        void DrawHalf(RibbonPaintContext half, bool check, bool expanded)
        {
            var state = whole.Graphics.Save();
            try
            {
                whole.Graphics.SetClip(half.Bounds, CombineMode.Intersect);
                var fill = expanded || half.Pressed ? half.Theme.Pressed : half.Hot ? half.Theme.Hover : check ? half.Theme.Checked : new RibbonFill(Color.Transparent);
                DrawChrome(whole, fill, whole.Theme.Border);
            }
            finally { whole.Graphics.Restore(state); }
        }
    }
    private void DrawItemContent(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu, bool appendArrow = true, bool arrowBelowSingle = false, string? textOverride = null, bool? enabledOverride = null)
    {
        bool enabled = enabledOverride ?? item.EffectiveEnabled;
        bool check = item is RibbonToggleItem { Checked: true } or RibbonSplitButtonItem { Checked: true }
            || enabledOverride.HasValue && item is RibbonCheckBoxItem { CheckState: CheckState.Checked } or RibbonRadioButtonItem { Checked: true };
        bool hasIcon = item.Icon is not null || item.Image is not null || item.LargeImage is not null;
        bool touchVertical = arrowBelowSingle && context.Density == RibbonDensity.Touch;
        int captionRows = touchVertical ? _captions.Wrap(item.Text, context.Font, context.DeviceDpi,
            Math.Max(0, context.Bounds.Width - 2 * RibbonLayoutEngine.Scale(5, context.DeviceDpi))).Lines.Length : 2;
        var content = RibbonContentLayout.Calculate(context.Bounds, size, menu, context.ReserveIcon || hasIcon, context.DeviceDpi, context.Font, context.Density, touchVertical, captionRows);
        if (menu) content = RibbonMenuLayout.Content(content, item, context.Bounds, context.Font, context.DeviceDpi, context.Density);
        var iconBounds = content.Icon;
        var textBounds = content.Text;
        var arrow = Rectangle.Empty;
        bool large = size == RibbonItemSize.Large && !menu;
        int arrowSpan = appendArrow && item is RibbonDropDownItem ? RibbonLayoutEngine.Scale(menu && context.Density == RibbonDensity.Touch ? 28 : 16, context.DeviceDpi) : 0;
        if (arrowSpan > 0 && !large)
        {
            arrow = menu ? new(textBounds.Right - arrowSpan, context.Bounds.Top, arrowSpan, context.Bounds.Height)
                : new(textBounds.Right - arrowSpan, textBounds.Top, arrowSpan, Math.Min(context.Font.Height, textBounds.Height));
            textBounds.Width = Math.Max(0, textBounds.Width - arrowSpan);
        }
        var flags = content.Flags;
        var color = enabled ? context.Theme.Foreground : SystemColors.GrayText;
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(iconBounds, CombineMode.Intersect);
            if (enabledOverride.HasValue) DrawQuickAccessImage(context with { Bounds = iconBounds }, item, enabled);
            else DrawItemImage(context with { Bounds = iconBounds }, item, size);
            if (!hasIcon && menu && check)
                DrawText(context with { Bounds = iconBounds }, "✓", color, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }
        finally { context.Graphics.Restore(saved); }
        if (size != RibbonItemSize.Small || menu || !hasIcon)
        {
            if (large && textBounds.Width > 0)
            {
                var caption = _captions.Wrap(item.Text, context.Font, context.DeviceDpi, textBounds.Width, touchVertical ? 0 : arrowSpan, arrowBelowSingle);
                int advance = RibbonCaptionLayout.LineAdvance(context.Font, context.DeviceDpi);
                for (int index = 0; index < caption.Lines.Length; index++)
                {
                    int top = textBounds.Top + index * advance;
                    var line = new Rectangle(textBounds.Left, top, textBounds.Width, Math.Min(context.Font.Height, Math.Max(0, textBounds.Bottom - top)));
                    if (arrowSpan > 0 && index == caption.Lines.Length - 1)
                    {
                        if (touchVertical)
                            arrow = RibbonContentLayout.LargeDropDownArrow(context.Bounds, context.Font, context.DeviceDpi, context.Density, caption.Lines.Length);
                        else if (arrowBelowSingle && caption.Lines.Length == 1)
                            arrow = new(context.Bounds.Left, context.Bounds.Bottom - RibbonLayoutEngine.Scale(13, context.DeviceDpi), context.Bounds.Width, RibbonLayoutEngine.Scale(12, context.DeviceDpi));
                        else
                        {
                            // Centre the last caption row and arrow as one unit.
                            line.X += (line.Width - caption.Widths[index] - arrowSpan) / 2;
                            line.Width = caption.Widths[index];
                            arrow = new(line.Right, line.Top, arrowSpan, line.Height);
                        }
                    }
                    if (line.Height > 0)
                        DrawText(context with { Bounds = line }, caption.Lines[index], color, (flags & ~(TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis)) | TextFormatFlags.SingleLine);
                }
            }
            else DrawText(context with { Bounds = textBounds }, textOverride ?? item.Text, color, flags);
        }
        if (menu) DrawMenuDescription(context, item, textBounds, enabledOverride);
        if (!arrow.IsEmpty)
        {
            if (menu) DrawSubmenuArrow(context with { Bounds = arrow }, color);
            else DrawDropDownArrow(context with { Bounds = arrow }, color);
        }
    }
    private void DrawMenuDescription(RibbonPaintContext context, RibbonItem item, Rectangle title, bool? enabledOverride = null, bool illustrated = false)
    {
        if (!RibbonMenuLayout.HasDescription(item)) return;
        var bounds = RibbonMenuLayout.Description(title, context.Bounds, context.Font, context.DeviceDpi, illustrated ? RibbonDensity.Compact : context.Density);
        if (bounds.Width > 0 && bounds.Height > 0)
            DrawText(context with { Bounds = bounds }, item.MenuDescription, (enabledOverride ?? item.EffectiveEnabled) ? context.Theme.SecondaryText : SystemColors.GrayText, RibbonMenuLayout.DescriptionFlags);
    }
    /// <summary>Paints a borrowed icon or image in its measured and clipped icon slot.</summary>
    protected virtual void DrawItemImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size)
    {
        bool enabled = item.EffectiveEnabled;
        var color = enabled ? context.Theme.Foreground : SystemColors.GrayText;
        if (item.Icon is { } icon)
            icon.Draw(context.Graphics, context.Bounds, new(context.DeviceDpi, color, enabled));
        else if ((size == RibbonItemSize.Large ? item.LargeImage ?? item.Image : item.Image ?? item.LargeImage) is { } image)
        {
            if (enabled)
                context.Graphics.DrawImage(image, context.Bounds);
            else
            {
                using var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    [0.3f, 0.3f, 0.3f, 0, 0], [0.59f, 0.59f, 0.59f, 0, 0],
                    [0.11f, 0.11f, 0.11f, 0, 0], [0, 0, 0, 0.55f, 0], [0, 0, 0, 0, 1]
                }));
                context.Graphics.DrawImage(image, context.Bounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
        }
    }
    /// <summary>Paints a visible keyboard focus outline.</summary>
    public virtual void DrawFocus(RibbonPaintContext context)
    {
        var bounds = Rectangle.Inflate(context.Bounds, -2, -2);
        ControlPaint.DrawFocusRectangle(context.Graphics, bounds, context.Theme.Foreground, context.Theme.Surface);
    }
    /// <summary>Paints a resolved keyboard badge.</summary>
    public virtual void DrawKeyTip(RibbonPaintContext context, string keys)
    {
        PaintRounded(context, 0, new(context.Theme.Surface), context.Theme.Border);
        DrawText(context, keys, context.Theme.Foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
    /// <summary>Paints common item chrome; the foundation uses square edges and theme-provided fills.</summary>
    protected virtual void DrawChrome(RibbonPaintContext context, RibbonFill fill, Color border) =>
        PaintRounded(context with { Bounds = Rectangle.Inflate(context.Bounds, -1, -1) }, 0, fill, border, border.A == 0 ? 0 : 1);
    /// <summary>Paints a fill and outline using mirrored corner coverage.</summary>
    protected void DrawRounded(RibbonPaintContext context, RibbonFill fill, Color border) => _rounded.Draw(context.Graphics, Rectangle.Inflate(context.Bounds, -1, -1), context.Theme.CornerRadius * context.DeviceDpi / 96, fill, border, border.A == 0 ? 0 : 1);
    internal void PaintRounded(RibbonPaintContext context, int radius, RibbonFill fill, Color border, int stroke = 1) =>
        _rounded.Draw(context.Graphics, context.Bounds, radius, fill, border, stroke);
    internal void PaintShadow(RibbonPaintContext context, int radius)
    {
        var bounds = context.Bounds;
        bounds.Offset(0, RibbonLayoutEngine.Scale(2, context.DeviceDpi));
        _shadow.Draw(context.Graphics, bounds, radius, RibbonLayoutEngine.Scale(6, context.DeviceDpi), context.Theme.BodyShadowColor);
    }
    private static RibbonPaintContext EnabledContext(RibbonPaintContext context, bool enabled) =>
        enabled ? context : context with
        {
            Hot = false,
            Pressed = false,
            Focused = false
        };
    /// <summary>Releases renderer-owned resources; borrowed icons and images remain caller-owned.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>Releases renderer-owned resources; borrowed icons and images remain caller-owned.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rounded.Dispose();
            _shadow.Dispose();
            _captions.Clear();
        }
    }
}
