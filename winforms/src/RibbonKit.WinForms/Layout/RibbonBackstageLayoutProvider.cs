namespace RibbonKit.WinForms.Layout;

// Shared space negotiation and DPI arithmetic; families own their defaults.
internal abstract class RibbonBackstageLayoutProvider
{
    private static readonly RibbonBackstageLayoutProvider Modern = new RibbonBackstage2024Layout();
    private static readonly RibbonBackstageLayoutProvider Classic = new RibbonBackstage2019Layout();
    internal static RibbonBackstageLayoutProvider For(RibbonTheme? theme) => (theme?.BackstageStyle ?? RibbonBackstageStyle.Modern) switch
    {
        RibbonBackstageStyle.Modern => Modern,
        RibbonBackstageStyle.Classic => Classic,
        _ => throw new ArgumentOutOfRangeException(nameof(theme))
    };
    internal abstract int NavigationPadding { get; }
    internal abstract int IconInset { get; }
    internal abstract int AutoMinimumWidth { get; }
    internal abstract int FooterBottomPadding(int navigationPadding);
    internal virtual bool SeparateFooterClearance => false;
    internal virtual Rectangle HostBounds(RibbonControl ribbon, Form form)
    {
        int caption = !ribbon.HasFullHeightBackstage && form is RibbonForm { CustomCaption: true } window ? window.CaptionHeight : 0;
        return new(0, caption, form.ClientSize.Width, Math.Max(0, form.ClientSize.Height - caption));
    }
    internal virtual RibbonBackstageLayout Build(Size size, Font font, int dpi, RibbonBackstage? backstage = null, RibbonItem? reveal = null, int captionHeight = 0)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var metrics = new RibbonBackstageMetrics(backstage?.Ribbon?.Theme, backstage?.Ribbon?.Density ?? RibbonDensity.Compact);
        int inset = S(metrics.NavigationPadding), row = Math.Max(S(metrics.RowHeight), font.Height + S(16));
        int width = Math.Max(0, size.Width);
        int shadow = backstage?.Ribbon is { HasWindowBackdrop: true } ribbon ? RibbonBackstageLayout.PageShadowInset(ribbon.Painter, ribbon.Theme, dpi, size) : 0;
        // Preserve 240 logical pixels for a page when possible; very narrow hosts
        // divide their space evenly rather than letting either region disappear.
        int available = Math.Clamp(Math.Max(width / 2, width - S(240)), 0, width);
        int ToDevice(int logical) => (int)Math.Clamp(Math.Round(logical * (double)dpi / 96), 0, available);
        int minimum = ToDevice(backstage?.MinimumNavigationWidth ?? 160);
        int maximum = Math.Max(minimum, ToDevice(backstage?.MaximumNavigationWidth ?? 320));
        int auto = S(metrics.AutoMinimumWidth);
        if (backstage is not null)
            foreach (var item in backstage.NavigationItems.Where(i => (i.Visible || ReferenceEquals(i, reveal)) && !i.IsDisposed && i is not RibbonMenuSeparatorItem))
            {
                int label = TextRenderer.MeasureText(item.Text, font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                // Reserve the same entry text offset as painting, row side
                // padding, trailing clearance and the native vertical scrollbar.
                auto = (int)Math.Min(int.MaxValue, Math.Max((long)auto, (long)label + S(metrics.AutoWidthPadding) + shadow));
            }
        int rail = Math.Clamp(backstage?.NavigationWidth is { } authored ? ToDevice(authored) : auto, minimum, maximum);
        int navigation = rail;
        inset = Math.Min(inset, Math.Max(0, (navigation - shadow) / 2));
        int backTop = Math.Min(Math.Max(0, captionHeight) + S(metrics.BackInset), Math.Max(0, size.Height));
        int back = Math.Min(S(metrics.BackSize), Math.Min(Math.Max(0, navigation - 2 * inset), Math.Max(0, size.Height - backTop)));
        int top = Math.Min(size.Height, backTop + back + S(metrics.BackGap));
        int EntryHeight(RibbonItem item) => item is RibbonMenuSeparatorItem ? S(9) : row;
        int mainHeight = backstage?.Items.Where(item => !item.IsDisposed && (item.Visible || ReferenceEquals(item, reveal))).Sum(EntryHeight) ?? 0;
        int bottomHeight = backstage?.FooterItems.Where(item => !item.IsDisposed && (item.Visible || ReferenceEquals(item, reveal))).Sum(EntryHeight) ?? 0;
        int footerPadding = metrics.Theme?.BackstageFooterBottomPadding is null && !SeparateFooterClearance ? inset : Math.Min(S(metrics.FooterBottomPadding), Math.Max(0, size.Height));
        int availableHeight = Math.Max(0, size.Height - top), footerHeight = bottomHeight == 0 ? 0 : bottomHeight + footerPadding;
        bool inline = footerHeight > 0 && footerHeight + Math.Min(row, mainHeight) > availableHeight;
        if (inline) footerHeight = 0;
        var nav = new Rectangle(0, top, Math.Max(0, navigation - shadow), availableHeight - footerHeight);
        return new(new(Math.Min(S(metrics.BackInset), Math.Max(0, navigation - back)), backTop, back, back), nav,
            new(rail, shadow, width - rail, Math.Max(0, size.Height - shadow)), row, inset)
        {
            Footer = footerHeight > 0 ? new(0, size.Height - footerHeight, nav.Width, footerHeight) : Rectangle.Empty,
            FooterInline = inline, SeparatorHeight = S(9), NavigationContentHeight = mainHeight + (inline ? bottomHeight : 0)
        };
    }
}
