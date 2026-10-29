namespace RibbonKit.WinForms;

/// <summary>Office 2024 inspired rounded chrome over the shared ribbon rendering foundation.</summary>
public class RibbonOffice2024Renderer : RibbonRenderer
{
    // WPF reference: two logical pixels around the groups row plus ten
    // inside the group separator. Retain a crisp one-device-pixel hairline.
    private const int GroupSeparatorInset = 12;
    /// <summary>Initializes the default Office 2024 inspired renderer.</summary>
    public RibbonOffice2024Renderer() { }
    /// <inheritdoc />
    public override bool JoinBelowRibbonQuickAccess => true;
    /// <inheritdoc />
    public override bool JoinMessageBarToBody => true;
    internal override bool PaintConnectedMessageShadow(RibbonPaintContext context, Rectangle upper, Rectangle messages)
    {
        PaintShadow(context with { Bounds = Rectangle.Union(upper, messages), IsTopEdgeConnected = false, IsBottomEdgeConnected = false }, context.Theme.CornerRadius * context.DeviceDpi / 48);
        return true;
    }
    /// <inheritdoc />
    public override void DrawMessageBar(RibbonPaintContext context)
    {
        int radius = context.Theme.CornerRadius * context.DeviceDpi / 48;
        PaintShadow(context, radius);
        // The preceding body/QAT already owns the shared horizontal edge.
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(context.Bounds, System.Drawing.Drawing2D.CombineMode.Intersect);
            var bounds = context.Bounds;
            if (context.IsTopEdgeConnected) { bounds.Y--; bounds.Height++; }
            PaintRounded(context with { Bounds = bounds }, radius, context.Theme.MessageBackground, context.Theme.MessageBorder);
        }
        finally { context.Graphics.Restore(saved); }
    }
    /// <inheritdoc />
    public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
    {
        if (position != RibbonQuickAccessPosition.BelowRibbon || !JoinBelowRibbonQuickAccess)
        {
            base.DrawQuickAccessToolbar(context, position);
            return;
        }
        if (!context.IsQuickAccessToolbarJoined)
            DrawBody(context); // A minimized ribbon leaves a complete standalone card.
        else if (context.Bounds.Width > 2)
        {
            using var separator = new SolidBrush(context.Theme.Border);
            context.Graphics.FillRectangle(separator, context.Bounds.Left + 1, context.Bounds.Top, context.Bounds.Width - 2, 1);
        }
    }
    /// <inheritdoc />
    public override void DrawGalleryHeading(RibbonPaintContext context, string text)
    {
        context = context with { Hot = false, Pressed = false, Focused = false };
        DrawChrome(context, context.Theme.GalleryHeadingBackground, Color.Transparent);
        int inset = Layout.RibbonLayoutEngine.Scale(6, context.DeviceDpi);
        using var font = new Font(context.Font, context.Font.Style | FontStyle.Bold);
        DrawText(context with { Bounds = Rectangle.Inflate(context.Bounds, -inset, 0), Font = font }, text, context.Theme.GalleryHeadingForeground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    /// <inheritdoc />
    public override int GetPopupCornerRadius(RibbonTheme theme, int deviceDpi) => Layout.RibbonLayoutEngine.Scale(theme.PopupCornerRadius, deviceDpi);
    /// <inheritdoc />
    protected override void DrawChrome(RibbonPaintContext context, RibbonFill fill, Color border) => DrawRounded(context, fill, border);
    /// <inheritdoc />
    public override void DrawCheckBoxIndicator(RibbonPaintContext context, RibbonCheckBoxItem item) =>
        PaintCheckIndicator(item.EffectiveEnabled ? context : context with { Hot = false, Pressed = false, Focused = false },
            item.CheckState, item.EffectiveEnabled, Layout.RibbonLayoutEngine.Scale(2, context.DeviceDpi));
    /// <inheritdoc />
    public override void DrawBody(RibbonPaintContext context)
    {
        int radius = context.Theme.CornerRadius * context.DeviceDpi / 48;
        PaintShadow(context, radius);
        PaintRounded(context, radius, new(context.Theme.Surface), context.Theme.Border);
    }
    /// <inheritdoc />
    public override void DrawScrollButton(RibbonPaintContext context, bool backward)
        => DrawBodyScrollButton(context, backward);
    /// <inheritdoc />
    public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        bool enabled = tab.EffectiveEnabled;
        DrawText(context, tab.DisplayText, !enabled ? SystemColors.GrayText : tab.IsContextual ? ContextualTabColor(context, tab, selected) : selected ? context.Theme.Accent : context.Theme.Foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        if (enabled && !context.SuppressTabIndicators && (selected || context.Hot || context.Pressed || context.Focused))
        {
            DrawTabIndicator(context with { Bounds = GetTabIndicatorBounds(context, tab) }, tab, selected);
        }
    }
    /// <inheritdoc />
    public override Rectangle GetTabIndicatorBounds(RibbonPaintContext context, RibbonTab tab)
    {
        int S(int value) => Layout.RibbonLayoutEngine.Scale(value, context.DeviceDpi);
        int labelWidth = Layout.RibbonCaptionLayout.TextWidth(tab.DisplayText, context.Font);
        int width = Math.Max(0, Math.Min(labelWidth + 2 * S(4), context.Bounds.Width - 2 * S(4)));
        int height = Math.Min(S(3), Math.Max(0, context.Bounds.Height - S(1)));
        if (width == 0 || height == 0) return Rectangle.Empty;
        return new(context.Bounds.Left + (context.Bounds.Width - width) / 2, context.Bounds.Bottom - height - S(1), width, height);
    }
    /// <inheritdoc />
    public override void DrawTabIndicator(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        if (!tab.EffectiveEnabled || context.Bounds.IsEmpty) return;
        var tint = selected ? tab.IsContextual ? ContextualTabColor(context, tab, true) : context.Theme.Accent : context.Theme.SecondaryText;
        PaintRounded(context, context.Bounds.Height / 2, new(tint), Color.Transparent, 0);
    }
    /// <inheritdoc />
    public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
    {
        base.DrawGroup(context, group, collapsed);
        if (context.IsSimplifiedGroup) return;
        var frame = context.GroupSeparatorBounds.IsEmpty ? context.Bounds : context.GroupSeparatorBounds;
        int inset = Layout.RibbonLayoutEngine.Scale(GroupSeparatorInset, context.DeviceDpi);
        int height = frame.Height - 2 * inset;
        if (frame.Width > 0 && height > 0)
        {
            using var brush = new SolidBrush(context.Theme.Border);
            int divider = Layout.RibbonLayoutEngine.GroupDividerWidth;
            context.Graphics.FillRectangle(brush, frame.Right - divider, frame.Top + inset, divider, height);
        }
    }
    /// <inheritdoc />
    public override void DrawFocus(RibbonPaintContext context) =>
        PaintRounded(context with { Bounds = Rectangle.Inflate(context.Bounds, -2, -2) }, Math.Max(1, context.Theme.CornerRadius * context.DeviceDpi / 96), new(Color.Transparent), context.Theme.Accent);
    /// <inheritdoc />
    public override void DrawKeyTip(RibbonPaintContext context, string keys)
    {
        PaintRounded(context, 2, new(Color.FromArgb(255, 255, 225)), context.Theme.Border);
        DrawText(context, keys, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
