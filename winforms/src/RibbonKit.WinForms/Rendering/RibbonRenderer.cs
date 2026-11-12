using System.Drawing.Drawing2D;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

/// <summary>Flat rendering foundation with shared content drawing over library-measured device-pixel geometry.</summary>
public partial class RibbonRenderer : IDisposable
{
    /// <summary>Gets whether a below-ribbon Quick Access row uses the body margins and shares its outer frame. The default keeps a separate full-width row. Renderer changes refresh the shared measurement.</summary>
    public virtual bool JoinBelowRibbonQuickAccess => false;
    private readonly RoundedPainter _rounded = new();
    private readonly ShadowPainter _shadow = new();
    private readonly RibbonCaptionLayout _captions = new();
    /// <summary>Gets the floating popup corner radius in device pixels. The flat renderer returns zero; frame, clipping and shadow share this value.</summary>
    public virtual int GetPopupCornerRadius(RibbonTheme theme, int deviceDpi) => 0;
    /// <summary>Gets floating-popup border width in device pixels. Null PopupAppearance.BorderWidth retains one device pixel; explicit logical widths scale with DPI. Frame paint and hosted content clearance use this hook, bounded to 128 logical pixels by native consumers.</summary>
    public virtual int GetPopupBorderWidth(RibbonTheme theme, int deviceDpi) => new RibbonPopupFrameMetrics(theme, deviceDpi, RibbonPresentationResolver.PopupFrame).BorderWidth;
    /// <summary>Paints the outer frame of a floating ribbon popup. Hosted content reserves at least one device pixel, or the actual renderer's wider border.</summary>
    public virtual void DrawPopupFrame(RibbonPaintContext context) => RibbonPopupFrameRenderer.DrawFrame(this, context);
    /// <summary>Paints a floating popup's shadow onto a transparent surface. Bounds describe the popup; GetPopupShadowPadding reserves its complete shadow/frame clearance.</summary>
    public virtual void DrawPopupShadow(RibbonPaintContext context) => RibbonPopupFrameRenderer.DrawShadow(this, context);
    /// <summary>Paints the ribbon or popup background.</summary>
    public virtual void DrawSurface(RibbonPaintContext context, bool popup)
    {
        using var brush = new SolidBrush(popup ? context.Theme.PopupColor : context.Theme.Background);
        context.Graphics.FillRectangle(brush, context.Bounds);
    }
    /// <summary>Paints the ribbon body panel. Shadows may extend into its surrounding margins; item clipping is handled separately.</summary>
    public virtual void DrawBody(RibbonPaintContext context)
        => RibbonBodyRenderer.DrawBody(this, context, new RibbonBodyFrameMetrics(context.Theme, context.DeviceDpi, RibbonPresentationResolver.NeutralBody));
    /// <summary>Paints a measured Classic/Simplified group-divider decoration. Bounds contain the resolved paint width and vertical clearance; command slots and reduction are unchanged. Empty BodyAppearance.GroupSeparatorColor follows Border.</summary>
    public virtual void DrawGroupSeparator(RibbonPaintContext context)
    {
        var appearance = context.Theme.BodyAppearance;
        using var brush = new SolidBrush(appearance is null || appearance.GroupSeparatorColor.IsEmpty ? context.Theme.Border : appearance.GroupSeparatorColor);
        context.Graphics.FillRectangle(brush, context.Bounds);
    }
    /// <summary>Paints a measured horizontal tab-strip or command-body scroll button.</summary>
    public virtual void DrawScrollButton(RibbonPaintContext context, bool backward)
        => DrawBodyScrollButton(context, backward);
    internal void DrawBodyScrollButton(RibbonPaintContext context, bool backward)
    {
        if (!context.IsBodyScrollButton && (context.Theme.HeaderScrollButtonsMatchTabHeight || context.Theme.HeaderScrollButtonWidth is not null
            || context.Theme.HeaderScrollButtonBackground is not null || !context.Theme.HeaderScrollButtonBorder.IsEmpty))
        {
            var fill = context.Pressed ? context.Theme.HeaderPressed ?? context.Theme.Pressed : context.Hot ? context.Theme.HeaderHover ?? context.Theme.Hover
                : context.Theme.HeaderScrollButtonBackground ?? new(context.Theme.Surface);
            var border = context.Theme.HeaderScrollButtonBorder.IsEmpty ? context.Theme.PopupBorder : context.Theme.HeaderScrollButtonBorder;
            PaintHeaderScroller(context, fill, border, 0);
            RibbonArrowPainter.DrawHorizontal(context.Graphics, context.Bounds, context.DeviceDpi, context.Theme.Foreground, backward, context.Theme.Chevrons);
            return;
        }
        DrawChrome(context with { IsHeaderScrollButton = !context.IsBodyScrollButton }, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.BodyFill,
            context.Hot || context.Pressed ? context.Theme.Accent : context.Theme.PopupBorder);
        RibbonArrowPainter.DrawHorizontal(context.Graphics, context.Bounds, context.DeviceDpi, context.Theme.Foreground, backward, context.Theme.Chevrons);
    }
    /// <summary>Paints an opaque vertical overflow-menu scroll band in device-pixel bounds, including its direction and endpoint availability.</summary>
    public virtual void DrawOverflowScrollButton(RibbonPaintContext context, bool upward, bool enabled) =>
        RibbonMenuDecorationRenderer.DrawOverflow(this, context, upward, enabled);
    /// <summary>Paints the measured tab-row minimize/expand button. A minimized ribbon shows a downward chevron.</summary>
    public virtual void DrawMinimizeButton(RibbonPaintContext context, bool minimized)
    {
        // Like the tab scrollers, this utility owns its opaque tab-row slot.
        // GDI text from an overflowing tab must not show through beneath it.
        using (var background = new SolidBrush(context.Theme.Background)) context.Graphics.FillRectangle(background, context.Bounds);
        context = context with { Bounds = RibbonHeaderButtonLayout.PaintBounds(context.Bounds, context.Theme, context.DeviceDpi, square: true) };
        if (context.Bounds.Width <= 0 || context.Bounds.Height <= 0) return;
        var state = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(context.Bounds, System.Drawing.Drawing2D.CombineMode.Intersect);
            if (context.Hot || context.Pressed) DrawChrome(context, QuickAccessFill(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, false), Color.Transparent);
            RibbonArrowPainter.DrawVertical(context.Graphics, context.Bounds, context.DeviceDpi, context.Theme.Foreground, !minimized, context.Theme.Chevrons);
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
            PaintCommandState(context, QuickAccessFill(context, open || context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, menu,
                pressed: open || context.Pressed, marked: check && !context.Hot), Color.Transparent);
        DrawQuickAccessContent(context, entry, menu);
        if (command is RibbonCheckBoxItem { CheckState: CheckState.Indeterminate })
        {
            RibbonCommandContentRenderer.DrawQuickAccessMixedMark(context, enabled, menu);
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
        RibbonCommandRenderer.DrawSplitChrome(this, primary, dropDown, whole, check, open, vertical: false, quickAccess: true, menu);
        DrawQuickAccessContent(primary, entry, menu);
        var color = enabled ? primary.Theme.Foreground : primary.Theme.DisabledColor;
        if (menu) DrawSubmenuArrow(dropDown, color); else DrawDropDownArrow(dropDown, color);
        if (primary.Focused) DrawFocus(primary);
        if (dropDown.Focused) DrawFocus(dropDown);

    }
    internal static RibbonFill QuickAccessFill(RibbonPaintContext context, RibbonFill fill, bool menu, bool pressed = false, bool marked = false)
    {
        if (!context.CompositedText || menu) return fill;
        fill = marked && !pressed && !context.Pressed ? context.Theme.BackdropPressed ?? new(Tint(context.Theme.Accent, 40)) : BackdropStateFill(context, pressed || context.Pressed);
        Color Alpha(Color color) => Color.FromArgb((int)Math.Round(color.A * context.Theme.QuickAccessBackdropOpacity), color);
        return new(Alpha(fill.Start), Alpha(fill.End));
    }
    private static Color Tint(Color color, int alpha) => Color.FromArgb((int)Math.Round(color.A * alpha / 255d), color);
    private static RibbonFill BackdropStateFill(RibbonPaintContext context, bool pressed) => pressed
        ? context.Theme.BackdropPressed ?? new(Tint(context.Theme.Foreground, 40))
        : context.Theme.BackdropHover ?? new(Tint(context.Theme.Foreground, 24));
    private void DrawQuickAccessContent(RibbonPaintContext context, RibbonElement entry, bool menu)
    {
        bool enabled = RibbonQuickAccess.Available(entry);
        var command = RibbonQuickAccess.Command(entry);
        bool chooser = entry is RibbonQuickAccessComboBoxReference or RibbonQuickAccessGroupReference || entry is RibbonQuickAccessDropDownReference { Source: not RibbonSplitButtonItem };
        var caption = RibbonQuickAccess.Caption(entry);
        var color = enabled ? context.Theme.Foreground : context.Theme.DisabledColor;
        var contentContext = context;
        if (chooser) contentContext = context with { Bounds = context.Bounds with { Width = Math.Max(0, context.Bounds.Width - (menu
            ? RibbonMenuLayout.ArrowCaptionReservation(context.Bounds.Width, context.DeviceDpi, context.Density, context.Theme, true)
            : RibbonLayoutEngine.Scale(12, context.DeviceDpi))) } };
        if (menu && entry is RibbonQuickAccessGroupReference)
        {
            var metrics = RibbonMenuMetrics.For(context.Density, context.Theme);
            if (entry is RibbonQuickAccessGroupReference { Source.Icon: { } groupIcon })
            {
                int side = RibbonLayoutEngine.Scale(metrics.MenuIcon, context.DeviceDpi);
                groupIcon.Draw(context.Graphics, new(contentContext.Bounds.Left + RibbonLayoutEngine.Scale(metrics.MenuIconInset, context.DeviceDpi), context.Bounds.Top + (context.Bounds.Height - side) / 2, side, side), new(context.DeviceDpi, color, enabled));
            }
            var text = RibbonMenuLayout.TextBounds(contentContext.Bounds, context.Font, context.DeviceDpi, context.Density, context.Theme);
            var label = contentContext with { Bounds = new(text.Left, context.Bounds.Top, text.Width, context.Bounds.Height) };
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
            else if (entry is RibbonQuickAccessGroupReference { Source.Icon: { } groupIcon })
                groupIcon.Draw(context.Graphics, icon, new(context.DeviceDpi, color, enabled));
            else
                DrawText(contentContext, menu ? caption : caption.Length == 0 ? "" : System.Globalization.StringInfo.GetNextTextElement(caption), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }
        if (chooser)
        {
            var arrow = context with { Bounds = menu ? RibbonMenuLayout.ArrowColumn(context.Bounds, context.DeviceDpi, context.Density, context.Theme)
                : Rectangle.FromLTRB(contentContext.Bounds.Right, context.Bounds.Top, context.Bounds.Right, context.Bounds.Bottom) };
            if (menu) DrawSubmenuArrow(arrow, color); else DrawDropDownArrow(arrow, color);
        }
    }
    private void DrawQuickAccessImage(RibbonPaintContext context, RibbonItem command, bool enabled)
    {
        if (enabled == command.EffectiveEnabled) { DrawItemImage(context, command, RibbonItemSize.Small); return; }
        if (command.Icon is { } icon) icon.Draw(context.Graphics, context.Bounds, new(context.DeviceDpi, context.Theme.DisabledColor, false));
        else if ((command.Image ?? command.LargeImage) is { } image)
            RibbonCommandContentRenderer.DrawDisabledImage(context, image, independentQuickAccess: true);
    }
    /// <summary>Paints the measured overflow opener. Keyboard and pointer activation open the same owned popup.</summary>
    public virtual void DrawQuickAccessOverflow(RibbonPaintContext context)
    {
        if (context.Hot || context.Pressed) PaintCommandState(context, QuickAccessFill(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, false), Color.Transparent);
        DrawDropDownArrow(context, context.Theme.Foreground);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints text inside a measured content slot, preserving its layout flags and glyph alpha on library material surfaces. Custom renderers can delegate here for composited text.</summary>
    public virtual void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags)
    {
        if (context.ApplicationMenuPart != RibbonApplicationMenuPart.None) flags |= TextFormatFlags.PreserveGraphicsClipping;
        if (context.CompositedText) RibbonBackdropBuffer.DrawText(context, text, color, flags);
        else TextRenderer.DrawText(context.Graphics, text, context.Font, context.Bounds, color, flags);
    }
    /// <summary>Paints passive ScreenTip content in library-measured title and description slots. The popup frame and shadow use the existing popup hooks.</summary>
    public virtual void DrawScreenTip(RibbonPaintContext context, string title, string description, Rectangle titleBounds, Rectangle descriptionBounds)
        => RibbonTipRenderer.DrawScreenTip(this, context, title, description, titleBounds, descriptionBounds);
    /// <summary>Paints a DPI-scaled dropdown indicator centered inside a measured slot.</summary>
    public virtual void DrawDropDownArrow(RibbonPaintContext context, Color color) =>
        RibbonArrowPainter.DrawDown(context.Graphics, context.Bounds, context.DeviceDpi, color, context.Theme.CommandContent);
    /// <summary>Paints the right-pointing indicator for a nested command menu in its measured slot.</summary>
    public virtual void DrawSubmenuArrow(RibbonPaintContext context, Color color) =>
        RibbonArrowPainter.DrawHorizontal(context.Graphics, context.Bounds, context.DeviceDpi, color, false, context.Theme.Chevrons);
    /// <summary>Paints a decorative command-menu separator within its measured row. It has no interaction state.</summary>
    public virtual void DrawMenuSeparator(RibbonPaintContext context, RibbonMenuSeparatorItem item) =>
        RibbonMenuDecorationRenderer.DrawSeparator(context);
    /// <summary>Paints a decorative vertical separator within its measured group slot, including full-group flyouts. It has no interaction state.</summary>
    public virtual void DrawGroupSeparator(RibbonPaintContext context, RibbonMenuSeparatorItem item)
    {
        int inset = RibbonLayoutEngine.Scale(3, context.DeviceDpi);
        if (context.Bounds.Width <= 0 || context.Bounds.Height <= 2 * inset) return;
        using var brush = new SolidBrush(context.Theme.Border);
        context.Graphics.FillRectangle(brush, context.Bounds.Left + (context.Bounds.Width - 1) / 2,
            context.Bounds.Top + inset, 1, context.Bounds.Height - 2 * inset);
    }
    /// <summary>Paints a native editor's label and frame. The editor itself owns text, caret and selection.</summary>
    public virtual void DrawTextBox(RibbonPaintContext context, RibbonTextBoxItem item, Rectangle editorBounds)
    {
        DrawEditorLabel(context, item, editorBounds);
        RibbonEditorRenderer.DrawField(this, context, editorBounds, combo: false, open: false, item.EffectiveEnabled);
    }

    /// <summary>Paints a ComboBox label, frame and arrow. Text input remains in the native editor.</summary>
    public virtual void DrawComboBox(RibbonPaintContext context, RibbonComboBoxItem item, Rectangle editorBounds)
    {
        DrawEditorLabel(context, item, editorBounds);
        context = EnabledContext(context, item.EffectiveEnabled);
        RibbonEditorRenderer.DrawField(this, context, editorBounds, combo: true, item.IsDropDownOpen && item.EffectiveEnabled, item.EffectiveEnabled);
    }

    /// <summary>Paints a native choice row. Selected is the native selection or keyboard preview; Hot and Pressed describe pointer interaction separately.</summary>
    public virtual void DrawComboBoxItem(RibbonPaintContext context, RibbonComboBoxItem item, int index, bool selected)
    {
        context = EnabledContext(context, item.EffectiveEnabled);
        RibbonEditorRenderer.DrawChoice(this, context, item.Items[index], selected, item.EffectiveEnabled);
    }

    private void DrawEditorLabel(RibbonPaintContext context, RibbonItem item, Rectangle editorBounds)
    {
        if (item is RibbonComboBoxItem { ShowLabel: false }) return;
        DrawText(context with { Bounds = new(context.Bounds.Left, context.Bounds.Top, Math.Max(0, editorBounds.Left - context.Bounds.Left - RibbonLayoutEngine.Scale(RibbonEditorMetrics.For(context.Density, context.Theme).LabelGap, context.DeviceDpi)), context.Bounds.Height) },
            item.Text, item.EffectiveEnabled ? context.Theme.EditorTextColor : context.Theme.DisabledColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
    /// <summary>Paints the File menu header using library-measured bounds and its original open/availability state.</summary>
    public virtual void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu)
    {
        if (DrawApplicationFileButton(context, menu)) return;
        bool enabled = menu.EffectiveEnabled;
        context = EnabledContext(context, enabled);
        bool open = enabled && menu.IsDropDownOpen && !context.FileMenuClosing;
        if (open || context.Hot || context.Pressed)
            DrawChrome(context, open || context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
        DrawText(context, menu.Text, !enabled ? context.Theme.DisabledColor : open ? context.Theme.Accent : context.Theme.HeaderTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints a tab using library-measured bounds.</summary>
    public virtual void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        context = EnabledContext(context, tab.EffectiveEnabled);
        if (selected || context.Hot)
            DrawChrome(context, selected ? new(context.Theme.Surface) : context.Theme.Hover, Color.Transparent);
        var color = !tab.EffectiveEnabled ? context.Theme.DisabledColor : tab.IsContextual ? ContextualTabColor(context, tab, selected) : selected ? context.Theme.Foreground : context.Theme.HeaderTextColor;
        // Scrolled tab bounds can extend across File/QAT; GDI must retain the viewport clip.
        DrawText(context, tab.DisplayText, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);
        if (context.Focused)
            DrawFocus(context);
        if (tab.EffectiveEnabled && !context.SuppressTabIndicators && (selected || context.Hot || context.Pressed || context.Focused))
        {
            var indicator = GetTabIndicatorBounds(context, tab);
            if (!indicator.IsEmpty) DrawTabIndicator(context with { Bounds = indicator }, tab, selected);
        }
    }
    /// <summary>Measures a tab's bottom indicator in device pixels from its full header bounds. Empty means this renderer has no separate indicator.</summary>
    public virtual Rectangle GetTabIndicatorBounds(RibbonPaintContext context, RibbonTab tab) => Rectangle.Empty;
    /// <summary>Paints a tab indicator in the supplied measured device-pixel bounds. The neutral renderer has no separate indicator. Geometry motion calls this independently of captured tab text.</summary>
    public virtual void DrawTabIndicator(RibbonPaintContext context, RibbonTab tab, bool selected) { }
    internal static Color ContextualTabColor(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        var color = tab.ContextualColor.IsEmpty ? context.Theme.Accent : tab.ContextualColor;
        // WPF Office 2024 ContextualUnselectedOpacity: composite before GDI text drawing.
        double opacity = color.A / 255d * (selected ? 1 : context.Theme.HeaderAppearance?.ContextualUnselectedOpacity ?? 0.6);
        var background = context.Theme.Background;
        int Mix(int foreground, int behind) => (int)Math.Round(foreground * opacity + behind * (1 - opacity));
        return Color.FromArgb(Mix(color.R, background.R), Mix(color.G, background.G), Mix(color.B, background.B));
    }
    /// <summary>Paints the group caption or collapsed opener.</summary>
    public virtual void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
    {
        context = EnabledContext(context, group.EffectiveEnabled);
        if (context.IsSimplifiedGroup)
        {
            bool open = group.Ribbon?.Popups.BorrowsGroup(group) == true;
            if (open || context.Hot || context.Pressed) DrawChrome(context, open || context.Pressed ? context.Theme.Pressed : context.Theme.Hover, context.Theme.CollapsedGroupBorder.IsEmpty ? context.Theme.Border : context.Theme.CollapsedGroupBorder);
            int S(int n) => RibbonLayoutEngine.Scale(n, context.DeviceDpi);
            var color = group.EffectiveEnabled ? context.Theme.Foreground : context.Theme.DisabledColor;
            int groupInset = Math.Min(S(new RibbonChromeMetrics(context.Theme, context.Density).GroupPadding()), context.Bounds.Width / 2);
            var text = Rectangle.Inflate(context.Bounds, -groupInset, 0); int arrow = Math.Min(text.Width, S(16));
            if (group.Icon is { } icon)
            {
                int side = Math.Min(S(24), text.Height); icon.Draw(context.Graphics, new(text.Left, text.Top + (text.Height - side) / 2, side, side), new(context.DeviceDpi, color, group.EffectiveEnabled));
                text.X += side + S(6); text.Width = Math.Max(0, text.Width - side - S(6));
            }
            DrawDropDownArrow(context with { Bounds = new(text.Right - arrow, text.Top, arrow, text.Height) }, color); text.Width = Math.Max(0, text.Width - arrow);
            DrawText(context with { Bounds = text }, group.DisplayText, color, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            if (context.Focused) DrawFocus(context); return;
        }
        if (collapsed)
        {
            bool open = group.EffectiveEnabled && group.Ribbon?.Popups.IsExpanded(group) == true;
            if (open || context.Hot || context.Pressed)
                DrawChrome(context, open || context.Pressed ? context.Theme.Pressed : context.Theme.Hover, context.Theme.CollapsedGroupBorder.IsEmpty ? context.Theme.Border : context.Theme.CollapsedGroupBorder);
            int inset = RibbonLayoutEngine.Scale(RibbonLayoutEngine.CollapsedGroupCaptionInset, context.DeviceDpi);
            var bounds = Rectangle.Inflate(context.Bounds, -inset, -inset);
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            int arrowHeight = Math.Min(bounds.Height, RibbonLayoutEngine.Scale(6, context.DeviceDpi));
            int gap = Math.Min(Math.Max(0, bounds.Height - arrowHeight), RibbonLayoutEngine.Scale(5, context.DeviceDpi));
            var caption = _captions.Wrap(group.DisplayText, context.Font, context.DeviceDpi, bounds.Width);
            int availableText = Math.Max(0, bounds.Height - arrowHeight - gap);
            int iconGap = group.Icon is null ? 0 : Math.Min(gap, availableText);
            int iconSide = group.Icon is null ? 0 : Math.Min(Math.Min(bounds.Width, RibbonLayoutEngine.Scale(32, context.DeviceDpi)), Math.Max(0, availableText - context.Font.Height - iconGap));
            availableText = Math.Max(0, availableText - iconSide - iconGap);
            int lineHeight = Math.Min(context.Font.Height, availableText);
            int rows = lineHeight == 0 ? 0 : Math.Min(caption.Lines.Length, availableText / lineHeight);
            int textHeight = rows * lineHeight;
            int top = bounds.Top + (bounds.Height - iconSide - iconGap - textHeight - gap - arrowHeight) / 2;
            var color = group.EffectiveEnabled ? context.Theme.Foreground : context.Theme.DisabledColor;
            if (iconSide > 0) { group.Icon!.Draw(context.Graphics, new(bounds.Left + (bounds.Width - iconSide) / 2, top, iconSide, iconSide), new(context.DeviceDpi, color, group.EffectiveEnabled)); top += iconSide + iconGap; }
            for (int row = 0; row < rows; row++)
                DrawText(context with { Bounds = new(bounds.Left, top + row * lineHeight, bounds.Width, lineHeight) },
                    rows < caption.Lines.Length ? group.DisplayText.Replace('\r', ' ').Replace('\n', ' ') : caption.Lines[row], color, flags);
            DrawDropDownArrow(context with { Bounds = new(bounds.Left, top + textHeight + gap, bounds.Width, arrowHeight) }, color);
            if (context.Focused)
                DrawFocus(context);
        }
        else
        {
            int height = Math.Max(context.Font.Height + RibbonLayoutEngine.Scale(6, context.DeviceDpi), RibbonLayoutEngine.Scale(20, context.DeviceDpi));
            var caption = new Rectangle(context.Bounds.X + 4, context.Bounds.Bottom - height, Math.Max(0, context.Bounds.Width - 8), height);
            DrawText(context with { Bounds = caption }, group.DisplayText, context.Theme.SecondaryText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
    /// <summary>Paints a command using library-measured bounds and shared content alignment.</summary>
    public virtual void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
    {
        if (item is RibbonMenuSeparatorItem separator)
        {
            context = context with { Hot = false, Pressed = false, Focused = false };
            if (menu) DrawMenuSeparator(context, separator);
            else DrawGroupSeparator(context, separator);
            return;
        }
        bool enabled = item.EffectiveEnabled;
        context = EnabledContext(context, enabled);
        if (context.ApplicationMenuPart != RibbonApplicationMenuPart.None) { DrawApplicationMenuItem(context, item); return; }
        if (!menu && item is RibbonComboBoxItem combo && size == RibbonItemSize.Small)
        {
            bool expanded = enabled && combo.IsDropDownOpen;
            if (expanded || context.Hot || context.Pressed)
                PaintCommandState(context, expanded || context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
            int arrowWidth = RibbonLayoutEngine.Scale(12, context.DeviceDpi), side = RibbonLayoutEngine.Scale(16, context.DeviceDpi);
            var content = context with { Bounds = context.Bounds with { Width = Math.Max(0, context.Bounds.Width - arrowWidth) } };
            var color = enabled ? context.Theme.Foreground : context.Theme.DisabledColor;
            if (item.Icon is not null || item.Image is not null || item.LargeImage is not null)
                DrawItemImage(content with { Bounds = new(content.Bounds.Left + (content.Bounds.Width - side) / 2, content.Bounds.Top + (content.Bounds.Height - side) / 2, side, side) }, item, RibbonItemSize.Small);
            else DrawText(content, item.Text.Length == 0 ? "" : System.Globalization.StringInfo.GetNextTextElement(item.Text), color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            DrawDropDownArrow(context with { Bounds = new(context.Bounds.Right - arrowWidth, context.Bounds.Top, arrowWidth, context.Bounds.Height) }, color);
            if (context.Focused) DrawFocus(context);
            return;
        }
        if (menu && RibbonMenuLayout.IsIllustratedChoice(item)) { DrawIllustratedChoice(context, item); return; }
        if (item is RibbonGalleryItem { IsInRibbon: true } && size == RibbonItemSize.Large && !menu && !context.IsGalleryDropDown)
        { DrawGalleryStripFrame(context); return; }
        if (item is RibbonCheckBoxItem or RibbonRadioButtonItem)
        {
            if (context.Hot || context.Pressed)
                PaintCommandState(context, context.Pressed ? context.Theme.Pressed : context.Theme.Hover, Color.Transparent);
            var content = RibbonContentLayout.Calculate(context.Bounds, RibbonItemSize.Medium, menu, true, context.DeviceDpi, context.Font, context.Density, theme: menu ? null : context.Theme);
            if (menu) content = RibbonMenuLayout.Content(content, item, context.Bounds, context.Font, context.DeviceDpi, context.Density, context.Theme);
            if (item is RibbonCheckBoxItem box) DrawCheckBoxIndicator(context with { Bounds = content.Icon }, box);
            else DrawRadioButtonIndicator(context with { Bounds = content.Icon }, (RibbonRadioButtonItem)item);
            DrawText(context with { Bounds = content.Text }, item.Text, enabled ? context.Theme.Foreground : context.Theme.DisabledColor, content.Flags);
            if (menu) DrawMenuDescription(context, item, content.Text);
            if (context.Focused) DrawFocus(context);
            return;
        }
        bool check = item is RibbonToggleItem { Checked: true };
        bool open = enabled && item is RibbonDropDownItem { IsDropDownOpen: true };
        if (open || check || context.Hot || context.Pressed)
            PaintCommandState(context, open || context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, Color.Transparent);
        DrawItemContent(context, item, size, menu);
        if (context.Focused)
            DrawFocus(context);
    }
    /// <summary>Paints a noninteractive command-dropdown heading. Uses the renderer's existing themed heading treatment.</summary>
    public virtual void DrawMenuHeader(RibbonPaintContext context, string text)
    {
        if (context.ApplicationMenuPart != RibbonApplicationMenuPart.None) DrawApplicationMenuHeader(context, text);
        else DrawGalleryHeading(context with { IsMenuHeading = true }, text);
    }

    private void DrawIllustratedChoice(RibbonPaintContext context, RibbonItem item)
    {
        bool selected = item is RibbonToggleItem { Checked: true } or RibbonRadioButtonItem { Checked: true } or RibbonCheckBoxItem { Checked: true };
        if (selected || context.Hot || context.Pressed)
            PaintCommandState(context, context.Pressed ? context.Theme.Pressed : context.Hot ? context.Theme.Hover : context.Theme.Checked, Color.Transparent);
        var content = RibbonMenuLayout.IllustratedContent(context.Bounds, context.Font, context.DeviceDpi, RibbonMenuLayout.HasDescription(item), context.Theme);
        if (content.Icon.Width > 0 && content.Icon.Height > 0)
        {
            var saved = context.Graphics.Save();
            try { context.Graphics.SetClip(content.Icon, CombineMode.Intersect); DrawItemImage(context with { Bounds = content.Icon }, item, RibbonItemSize.Large); }
            finally { context.Graphics.Restore(saved); }
        }
        using var titleFont = new Font(context.Font, context.Font.Style | FontStyle.Bold);
        DrawText(context with { Bounds = content.Text, Font = titleFont }, item.Text, item.EffectiveEnabled ? context.Theme.Foreground : context.Theme.DisabledColor, content.Flags);
        DrawMenuDescription(context, item, content.Text, illustrated: true);
        if (context.Focused) DrawFocus(context);
    }
    /// <summary>Paints a check indicator in its measured compact slot. The flat foundation uses square corners.</summary>
    public virtual void DrawCheckBoxIndicator(RibbonPaintContext context, RibbonCheckBoxItem item) =>
        PaintCheckIndicator(EnabledContext(context, item.EffectiveEnabled), item.CheckState, item.EffectiveEnabled, 0);
    /// <summary>Paints a circular radio indicator in its measured compact slot.</summary>
    public virtual void DrawRadioButtonIndicator(RibbonPaintContext context, RibbonRadioButtonItem item) =>
        RibbonCommandContentRenderer.DrawRadio(this, EnabledContext(context, item.EffectiveEnabled), item.Checked, item.EffectiveEnabled);
    internal void PaintCheckIndicator(RibbonPaintContext context, CheckState check, bool enabled, int radius) =>
        RibbonCommandContentRenderer.DrawCheck(this, context, check, enabled, radius);
    /// <summary>Paints a split command from its independently measured primary and dropdown action contexts.</summary>
    public virtual void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
    {
        if (primary.ApplicationMenuPart != RibbonApplicationMenuPart.None) { DrawApplicationMenuSplit(primary, dropDown, item); return; }
        primary = EnabledContext(primary, item.EffectiveEnabled);
        dropDown = EnabledContext(dropDown, item.EffectiveEnabled);
        var whole = primary with { Bounds = Rectangle.Union(primary.Bounds, dropDown.Bounds), Hot = false, Pressed = false, Focused = false };
        bool open = item.EffectiveEnabled && item.IsDropDownOpen;
        bool vertical = RibbonSplitLayout.IsVertical(item, size, menu);
        RibbonCommandRenderer.DrawSplitChrome(this, primary, dropDown, whole, item.Checked, open, vertical, quickAccess: false, menu);
        DrawItemContent(vertical ? whole : primary, item, size, menu, vertical, vertical);
        if (!vertical)
        {
            var arrowContext = whole with { Bounds = dropDown.Bounds };
            var color = item.EffectiveEnabled ? whole.Theme.Foreground : whole.Theme.DisabledColor;
            if (menu) DrawSubmenuArrow(arrowContext, color);
            else DrawDropDownArrow(arrowContext, color);
        }
        if (primary.Focused)
            DrawFocus(primary);
        if (dropDown.Focused)
            DrawFocus(dropDown);

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
        var content = RibbonContentLayout.Calculate(context.Bounds, size, menu, context.ReserveIcon || hasIcon, context.DeviceDpi, context.Font, context.Density, touchVertical, captionRows,
            menu || enabledOverride.HasValue ? null : context.Theme);
        if (menu) content = RibbonMenuLayout.Content(content, item, context.Bounds, context.Font, context.DeviceDpi, context.Density, context.Theme, projectedEditorCaption: enabledOverride.HasValue);
        var iconBounds = content.Icon;
        var textBounds = content.Text;
        var arrow = Rectangle.Empty;
        bool large = size == RibbonItemSize.Large && !menu;
        int arrowSpan = appendArrow && item is RibbonDropDownItem ? menu
            ? RibbonMenuLayout.ArrowCaptionReservation(context.Bounds.Width, context.DeviceDpi, context.Density, context.Theme, false)
            : RibbonLayoutEngine.Scale(16, context.DeviceDpi) : 0;
        RibbonLargeCommandLayout? centeredLarge = null;
        RibbonCaptionLayout.Caption? centeredCaption = null;
        if (large && !enabledOverride.HasValue && RibbonLargeCommandLayout.Enabled(context.Theme))
        {
            var measured = _captions.Wrap(item.Text, context.Font, context.DeviceDpi, textBounds.Width, arrowBelowSingle ? 0 : arrowSpan, arrowBelowSingle);
            if (RibbonLargeCommandLayout.TryCalculate(context.Bounds, context.Font, context.DeviceDpi, context.Density, context.Theme,
                arrowBelowSingle, hasIcon, out var centered))
            {
                centeredLarge = centered; centeredCaption = measured; iconBounds = centered.Icon; textBounds = centered.Caption;
            }
        }
        if (arrowSpan > 0 && !large)
        {
            if (!menu && size == RibbonItemSize.Small && context.Density == RibbonDensity.Touch)
                iconBounds = RibbonContentLayout.SmallIconBounds(context.Bounds with
                {
                    Width = Math.Max(0, context.Bounds.Width - RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(context.Density).CommandDropDownArrow, context.DeviceDpi))
                }, context.DeviceDpi);
            arrow = menu ? RibbonMenuLayout.ArrowColumn(context.Bounds, context.DeviceDpi, context.Density, context.Theme)
                : new(textBounds.Right - arrowSpan, textBounds.Top, arrowSpan, Math.Min(context.Font.Height, textBounds.Height));
            textBounds.Width = Math.Max(0, textBounds.Width - arrowSpan);
        }
        var flags = content.Flags;
        var color = enabled ? context.Theme.Foreground : context.Theme.DisabledColor;
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
                var caption = centeredCaption ?? _captions.Wrap(item.Text, context.Font, context.DeviceDpi, textBounds.Width, touchVertical ? 0 : arrowSpan, arrowBelowSingle);
                int advance = RibbonCaptionLayout.LineAdvance(context.Font, context.DeviceDpi);
                for (int index = 0; index < caption.Lines.Length; index++)
                {
                    int top = textBounds.Top + index * advance;
                    var line = new Rectangle(textBounds.Left, top, textBounds.Width, Math.Min(context.Font.Height, Math.Max(0, textBounds.Bottom - top)));
                    if (arrowSpan > 0 && index == caption.Lines.Length - 1)
                    {
                        if (centeredLarge is { } centered && arrowBelowSingle)
                            arrow = centered.Arrow;
                        else if (touchVertical)
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
        var bounds = illustrated ? RibbonMenuLayout.IllustratedDescription(title, context.Bounds, context.Font, context.DeviceDpi, context.Theme)
            : RibbonMenuLayout.Description(title, context.Bounds, context.Font, context.DeviceDpi, context.Density, context.Theme);
        if (bounds.Width > 0 && bounds.Height > 0)
            DrawText(context with { Bounds = bounds }, item.MenuDescription, (enabledOverride ?? item.EffectiveEnabled) ? context.Theme.SecondaryText : context.Theme.DisabledColor, RibbonMenuLayout.DescriptionFlags);
    }
    /// <summary>Paints a borrowed icon or image in its measured and clipped icon slot.</summary>
    protected virtual void DrawItemImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size)
    {
        bool enabled = item.EffectiveEnabled;
        var color = enabled ? context.Theme.Foreground : context.Theme.DisabledColor;
        if (item.Icon is { } icon)
            icon.Draw(context.Graphics, context.Bounds, new(context.DeviceDpi, color, enabled));
        else if ((size == RibbonItemSize.Large ? item.LargeImage ?? item.Image : item.Image ?? item.LargeImage) is { } image)
        {
            if (enabled)
                context.Graphics.DrawImage(image, context.Bounds);
            else
            {
                RibbonCommandContentRenderer.DrawDisabledImage(context, image);
            }
        }
    }
    /// <summary>Paints a visible keyboard focus outline.</summary>
    public virtual void DrawFocus(RibbonPaintContext context)
    {
        var bounds = RibbonSharedEffectMetrics.FocusBounds(context);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        if (context.Theme.FocusStrokeWidth is not null)
        {
            int stroke = Math.Min(RibbonSharedEffectMetrics.FocusStroke(context), Math.Max(1, Math.Min(bounds.Width, bounds.Height) / 2));
            var saved = context.Graphics.Save();
            try
            {
                context.Graphics.SetClip(context.Bounds, CombineMode.Intersect);
                using var pen = new Pen(context.Theme.Foreground, stroke) { DashStyle = DashStyle.Dot };
                context.Graphics.DrawRectangle(pen, bounds.X + stroke / 2f, bounds.Y + stroke / 2f, Math.Max(0, bounds.Width - stroke), Math.Max(0, bounds.Height - stroke));
            }
            finally { context.Graphics.Restore(saved); }
        }
        else if (context.CompositedText)
        {
            using var pen = new Pen(context.Theme.Foreground) { DashStyle = DashStyle.Dot };
            context.Graphics.DrawRectangle(pen, bounds);
        }
        else ControlPaint.DrawFocusRectangle(context.Graphics, bounds, context.Theme.Foreground, context.Theme.Surface);
    }
    /// <summary>Paints a resolved keyboard badge.</summary>
    public virtual void DrawKeyTip(RibbonPaintContext context, string keys)
        => RibbonTipRenderer.DrawKeyTip(this, context, keys, office: false);
    /// <summary>Paints common item chrome; the foundation uses square edges and theme-provided fills.</summary>
    protected virtual void DrawChrome(RibbonPaintContext context, RibbonFill fill, Color border)
    {
        if (TryPaintBackstageState(context, fill, border, 0)) return;
        if (TryPaintHeaderScroller(context, fill, border, 0)) return;
        if (TryPaintMenuDecoration(context, fill, border, 0)) return;
        if (TryPaintGalleryChrome(context, fill, border, 0)) return;
        if (TryPaintEditorChrome(context, fill, border, 0)) return;
        if (TryPaintCommandState(context, fill, border, 0)) return;
        PaintRounded(context with { Bounds = Rectangle.Inflate(context.Bounds, -1, -1) }, 0, fill, border, border.A == 0 ? 0 : 1);
    }
    /// <summary>Paints a fill and outline using mirrored corner coverage.</summary>
    protected void DrawRounded(RibbonPaintContext context, RibbonFill fill, Color border)
    {
        if (TryPaintBackstageState(context, fill, border, context.Theme.CornerRadius * context.DeviceDpi / 96)) return;
        if (TryPaintHeaderScroller(context, fill, border, context.Theme.CornerRadius * context.DeviceDpi / 96)) return;
        if (TryPaintMenuDecoration(context, fill, border, context.Theme.CornerRadius * context.DeviceDpi / 96)) return;
        if (TryPaintGalleryChrome(context, fill, border, context.Theme.CornerRadius * context.DeviceDpi / 96)) return;
        if (TryPaintEditorChrome(context, fill, border, context.Theme.CornerRadius * context.DeviceDpi / 96)) return;
        if (TryPaintCommandState(context, fill, border, context.Theme.CornerRadius * context.DeviceDpi / 96)) return;
        _rounded.Draw(context.Graphics, Rectangle.Inflate(context.Bounds, -1, -1), context.Theme.CornerRadius * context.DeviceDpi / 96, fill, border, border.A == 0 ? 0 : 1);
    }
    internal void PaintRounded(RibbonPaintContext context, int radius, RibbonFill fill, Color border, int stroke = 1) =>
        _rounded.Draw(context.Graphics, context.Bounds, radius, fill, border, stroke, context.IsTopEdgeConnected, context.IsBottomEdgeConnected);
    internal void PaintShadow(RibbonPaintContext context, int radius)
    {
        if (context.SuppressShadow) return;
        var bounds = context.Bounds;
        var metrics = new RibbonFrameMetrics(context.Theme);
        bounds.Offset(0, RibbonLayoutEngine.Scale(metrics.BodyOffset, context.DeviceDpi));
        var saved = context.Graphics.Save();
        try
        {
            int blur = RibbonLayoutEngine.Scale(metrics.BodyBlur, context.DeviceDpi);
            if (context.IsBottomEdgeConnected)
                context.Graphics.SetClip(Rectangle.FromLTRB(bounds.Left - blur, bounds.Top - blur, bounds.Right + blur, context.Bounds.Bottom), CombineMode.Intersect);
            _shadow.Draw(context.Graphics, bounds, radius, blur, context.Theme.BodyShadowColor, context.IsTopEdgeConnected, context.IsBottomEdgeConnected);
        }
        finally { context.Graphics.Restore(saved); }
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
