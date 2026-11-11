namespace RibbonKit.WinForms;

/// <summary>Office 2024 inspired rounded chrome over the shared ribbon rendering foundation.</summary>
public class RibbonOffice2024Renderer : RibbonRenderer
{
    // WPF reference: two logical pixels around the groups row plus ten
    // inside the group separator. Retain a crisp one-device-pixel hairline.
    /// <summary>Initializes the default Office 2024 inspired renderer.</summary>
    public RibbonOffice2024Renderer() { }
    /// <inheritdoc />
    public override int GetBodyCornerRadius(RibbonTheme theme, int deviceDpi) => theme.BodyCornerRadius is { } radius ? Layout.RibbonLayoutEngine.Scale(radius, deviceDpi) : theme.CornerRadius * deviceDpi / 48;
    /// <inheritdoc />
    public override Padding GetBodyShadowPadding(RibbonTheme theme, int deviceDpi)
    {
        var metrics = new Layout.RibbonFrameMetrics(theme);
        if (metrics.BodyBlur == 0 || theme.BodyShadowColor.A == 0) return Padding.Empty;
        int blur = Layout.RibbonLayoutEngine.Scale(metrics.BodyBlur, deviceDpi), offset = Layout.RibbonLayoutEngine.Scale(metrics.BodyOffset, deviceDpi);
        return new(blur, Math.Max(0, blur - offset), blur, Math.Max(0, blur + offset));
    }
    /// <inheritdoc />
    public override int GetBackstagePageShadowInset(RibbonTheme theme, int deviceDpi) => BackstageStyleRenderer(theme).GetBackstagePageShadowInset(theme, deviceDpi);
    /// <inheritdoc />
    public override int GetBackstagePageBorderWidth(RibbonTheme theme, int deviceDpi) => BackstageStyleRenderer(theme).GetBackstagePageBorderWidth(theme, deviceDpi);
    /// <inheritdoc />
    public override int GetBackstagePageCornerRadius(RibbonTheme theme, int deviceDpi) => BackstageStyleRenderer(theme).GetBackstagePageCornerRadius(theme, deviceDpi);
    /// <inheritdoc />
    public override void DrawBackstageBackground(RibbonPaintContext context, int navigationWidth)
        => BackstageStyleRenderer(context.Theme).DrawBackground(this, context, navigationWidth);
    /// <inheritdoc />
    public override bool JoinBelowRibbonQuickAccess => true;
    /// <inheritdoc />
    public override bool JoinMessageBarToBody => true;
    internal override bool PaintConnectedMessageShadow(RibbonPaintContext context, Rectangle upper, Rectangle messages)
    {
        PaintShadow(context with { Bounds = Rectangle.Union(upper, messages), IsTopEdgeConnected = false, IsBottomEdgeConnected = false }, GetBodyCornerRadius(context.Theme, context.DeviceDpi));
        return true;
    }
    /// <inheritdoc />
    public override void DrawMessageBar(RibbonPaintContext context)
    {
        int radius = GetBodyCornerRadius(context.Theme, context.DeviceDpi);
        PaintShadow(context, radius);
        // The preceding body/QAT already owns the shared horizontal edge.
        var saved = context.Graphics.Save();
        try
        {
            context.Graphics.SetClip(context.Bounds, System.Drawing.Drawing2D.CombineMode.Intersect);
            var bounds = context.Bounds;
            if (context.IsTopEdgeConnected) { bounds.Y--; bounds.Height++; }
            PaintMessageFrame(context with { Bounds = bounds }, radius);
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
            using var separator = new SolidBrush(context.Theme.BodyBorderColor);
            int inset = context.Theme.BodyBottomBorderOnly ? 0 : 1;
            context.Graphics.FillRectangle(separator, context.Bounds.Left + inset, context.Bounds.Top, context.Bounds.Width - 2 * inset, 1);
        }
    }
    /// <inheritdoc />
    public override void DrawGalleryHeading(RibbonPaintContext context, string text)
    {
        context = context with { Hot = false, Pressed = false, Focused = false };
        DrawChrome(context, context.IsMenuHeading ? context.Theme.MenuHeadingBackground ?? context.Theme.GalleryHeadingBackground : context.Theme.GalleryHeadingBackground, Color.Transparent);
        using var font = new Font(context.Font, context.Font.Style | FontStyle.Bold);
        var foreground = context.IsMenuHeading && !context.Theme.MenuHeadingForeground.IsEmpty ? context.Theme.MenuHeadingForeground : context.Theme.GalleryHeadingForeground;
        var textBounds = context.IsMenuHeading ? Layout.RibbonMenuLayout.HeaderTextBounds(context.Bounds, context.DeviceDpi, context.Density, context.Theme) : new Layout.RibbonGalleryMetrics(context.Theme, context.Density).HeadingTextBounds(context.Bounds, context.DeviceDpi);
        DrawText(context with { Bounds = textBounds, Font = font }, text, foreground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
    }
    /// <inheritdoc />
    public override int GetPopupCornerRadius(RibbonTheme theme, int deviceDpi) => Layout.RibbonLayoutEngine.Scale(theme.PopupCornerRadius, deviceDpi);
    /// <inheritdoc />
    protected override void DrawChrome(RibbonPaintContext context, RibbonFill fill, Color border) => DrawRounded(context, fill, border);
    /// <inheritdoc />
    public override void DrawCheckBoxIndicator(RibbonPaintContext context, RibbonCheckBoxItem item) =>
        PaintCheckIndicator(item.EffectiveEnabled ? context : context with { Hot = false, Pressed = false, Focused = false },
            item.CheckState, item.EffectiveEnabled, Layout.RibbonFrameMetrics.Radius(context.Theme.CheckBoxCornerRadius, Layout.RibbonLayoutEngine.Scale(2, context.DeviceDpi), context.DeviceDpi));
    /// <inheritdoc />
    public override void DrawBody(RibbonPaintContext context)
    {
        int radius = GetBodyCornerRadius(context.Theme, context.DeviceDpi);
        PaintShadow(context, radius);
        if (context.Theme.BodyBottomBorderOnly)
        {
            PaintRounded(context, radius, context.Theme.BodyFill, Color.Transparent, 0);
            using var border = new SolidBrush(context.Theme.BodyBorderColor);
            if (context.Bounds.Height > 0) context.Graphics.FillRectangle(border, context.Bounds.Left, context.Bounds.Bottom - 1, context.Bounds.Width, 1);
        }
        else PaintRounded(context, radius, context.Theme.BodyFill, context.Theme.BodyBorderColor);
    }
    /// <inheritdoc />
    public override void DrawScrollButton(RibbonPaintContext context, bool backward)
        => DrawBodyScrollButton(context, backward);
    /// <inheritdoc />
    public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        var header = RibbonPresentationResolver.Header(context.Theme);
        header.Renderer.DrawTab(this, context, tab, selected, header.ResolveMetrics(context.Theme, context.Density));
    }
    /// <inheritdoc />
    public override Rectangle GetTabIndicatorBounds(RibbonPaintContext context, RibbonTab tab)
    {
        var header = RibbonPresentationResolver.Header(context.Theme);
        return header.Renderer.GetTabIndicatorBounds(context, tab, header.ResolveMetrics(context.Theme, context.Density));
    }
    /// <inheritdoc />
    public override void DrawTabIndicator(RibbonPaintContext context, RibbonTab tab, bool selected)
    {
        var header = RibbonPresentationResolver.Header(context.Theme);
        header.Renderer.DrawTabIndicator(this, context, tab, selected, header.ResolveMetrics(context.Theme, context.Density));
    }
    /// <inheritdoc />
    public override void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu)
    {
        var header = RibbonPresentationResolver.Header(context.Theme);
        header.Renderer.DrawFileButton(this, context, menu, header.ResolveMetrics(context.Theme, context.Density));
    }
    /// <inheritdoc />
    public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
    {
        base.DrawGroup(context, group, collapsed);
        if (context.IsSimplifiedGroup) return;
        var frame = context.GroupSeparatorBounds.IsEmpty ? context.Bounds : context.GroupSeparatorBounds;
        int inset = Math.Min(frame.Height / 2, Layout.RibbonLayoutEngine.Scale(new Layout.RibbonChromeMetrics(context.Theme, context.Density).SeparatorInset(), context.DeviceDpi));
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
        PaintRounded(context with { Bounds = Rectangle.Inflate(context.Bounds, -2, -2) }, Layout.RibbonFrameMetrics.Radius(context.Theme.FocusCornerRadius, Math.Max(1, context.Theme.CornerRadius * context.DeviceDpi / 96), context.DeviceDpi), new(Color.Transparent), context.Theme.Accent);
    /// <inheritdoc />
    public override void DrawKeyTip(RibbonPaintContext context, string keys)
    {
        PaintRounded(context, Layout.RibbonFrameMetrics.Radius(context.Theme.KeyTipCornerRadius, 2, context.DeviceDpi), context.Theme.KeyTipBackground ?? new(Color.FromArgb(255, 255, 225)),
            context.Theme.KeyTipBorder.IsEmpty ? context.Theme.Border : context.Theme.KeyTipBorder);
        DrawText(context, keys, context.Theme.KeyTipForeground.IsEmpty ? Color.Black : context.Theme.KeyTipForeground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}
