namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonBackstageLayout(Rectangle Back, Rectangle Navigation, Rectangle Page, int RowHeight, int Inset)
{
    internal Rectangle Footer { get; init; }
    internal bool FooterInline { get; init; }
    internal int SeparatorHeight { get; init; }
    internal int NavigationContentHeight { get; init; }
    internal int EntryHeight(RibbonItem item) => item is RibbonMenuSeparatorItem ? SeparatorHeight : RowHeight;
    internal IReadOnlyList<RibbonBackstageEntrySlot> Arrange(RibbonBackstage backstage, int scrollOffset = 0, RibbonItem? reveal = null)
    {
        var rows = new List<RibbonBackstageEntrySlot>(); int y = Navigation.Top - Math.Max(0, scrollOffset), bottom = Footer.Top;
        void Add(IEnumerable<RibbonItem> items, bool footer)
        {
            foreach (var item in items.Where(item => !item.IsDisposed && (item.Visible || ReferenceEquals(item, reveal))))
            {
                bool pinned = footer && !FooterInline; var clip = pinned ? Footer : Navigation;
                int height = EntryHeight(item), top = pinned ? bottom : y;
                rows.Add(new(item, new(clip.Left + Inset, top, Math.Max(0, clip.Width - 2 * Inset), height), clip, footer));
                if (pinned) bottom += height; else y += height;
            }
        }
        Add(backstage.Items, false); Add(backstage.FooterItems, true); return rows;
    }
    internal Rectangle PageContentBounds(RibbonTheme theme, int dpi, int border = 0)
    {
        var bounds = new Rectangle(Page.Left + border, Page.Top + border, Math.Max(0, Page.Width - border), Math.Max(0, Page.Height - border));
        int padding = RibbonLayoutEngine.Scale(theme.BackstagePagePadding ?? 0, dpi);
        int x = Math.Min(padding, bounds.Width / 2), y = Math.Min(padding, bounds.Height / 2);
        return Rectangle.FromLTRB(bounds.Left + x, bounds.Top + y, bounds.Right - x, bounds.Bottom - y);
    }
    internal static int PageShadowInset(RibbonRenderer renderer, RibbonTheme theme, int dpi, Size size) =>
        Math.Clamp(renderer.GetBackstagePageShadowInset(theme, dpi), 0, Math.Max(0,
            Math.Min(RibbonLayoutEngine.Scale(32, dpi), Math.Min(size.Width, size.Height - 1))));
    internal static int PageBorderWidth(RibbonRenderer renderer, RibbonTheme theme, int dpi, Size size) =>
        Math.Clamp(renderer.GetBackstagePageBorderWidth(theme, dpi), 0, Math.Max(0,
            Math.Min(RibbonLayoutEngine.Scale(32, dpi), Math.Min(size.Width, size.Height) - 1)));
    internal static int PageCornerRadius(RibbonRenderer renderer, RibbonTheme theme, int dpi, Size size) =>
        Math.Clamp(renderer.GetBackstagePageCornerRadius(theme, dpi), 0, Math.Max(0,
            Math.Min(RibbonLayoutEngine.Scale(32, dpi), Math.Min(size.Width, size.Height) / 2)));
    internal static RibbonBackstageLayout Build(Size size, Font font, int dpi, RibbonBackstage? backstage = null, RibbonItem? reveal = null)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var metrics = new RibbonBackstageMetrics(backstage?.Ribbon?.Theme, backstage?.Ribbon?.Density ?? RibbonDensity.Compact);
        int inset = S(metrics.NavigationPadding), row = Math.Max(S(metrics.RowHeight), font.Height + S(16));
        int width = Math.Max(0, size.Width);
        int shadow = backstage?.Ribbon is { HasWindowBackdrop: true } ribbon ? PageShadowInset(ribbon.Painter, ribbon.Theme, dpi, size) : 0;
        // Preserve 240 logical pixels for a page when possible; very narrow hosts
        // divide their space evenly rather than letting either region disappear.
        int available = Math.Clamp(Math.Max(width / 2, width - S(240)), 0, width);
        int ToDevice(int logical) => (int)Math.Clamp(Math.Round(logical * (double)dpi / 96), 0, available);
        int minimum = ToDevice(backstage?.MinimumNavigationWidth ?? 160);
        int maximum = Math.Max(minimum, ToDevice(backstage?.MaximumNavigationWidth ?? 320));
        int auto = S(200);
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
        int backTop = Math.Min(S(metrics.BackInset), Math.Max(0, size.Height));
        int back = Math.Min(S(metrics.BackSize), Math.Min(Math.Max(0, navigation - 2 * inset), Math.Max(0, size.Height - backTop)));
        int top = Math.Min(size.Height, backTop + back + S(metrics.BackGap));
        int EntryHeight(RibbonItem item) => item is RibbonMenuSeparatorItem ? S(9) : row;
        int mainHeight = backstage?.Items.Where(item => !item.IsDisposed && (item.Visible || ReferenceEquals(item, reveal))).Sum(EntryHeight) ?? 0;
        int bottomHeight = backstage?.FooterItems.Where(item => !item.IsDisposed && (item.Visible || ReferenceEquals(item, reveal))).Sum(EntryHeight) ?? 0;
        int availableHeight = Math.Max(0, size.Height - top), footerHeight = bottomHeight == 0 ? 0 : bottomHeight + inset;
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

internal sealed record RibbonBackstageEntrySlot(RibbonItem Item, Rectangle Bounds, Rectangle Clip, bool Footer)
{
    internal Rectangle VisibleBounds => Rectangle.Intersect(Bounds, Clip);
}
