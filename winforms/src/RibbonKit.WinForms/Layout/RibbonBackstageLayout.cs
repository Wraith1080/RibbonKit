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
    internal static RibbonBackstageLayout Build(Size size, Font font, int dpi, RibbonBackstage? backstage = null, RibbonItem? reveal = null, int captionHeight = 0)
        => RibbonPresentationResolver.Backstage(backstage?.Ribbon?.Theme).Layout.Build(size, font, dpi, backstage, reveal, captionHeight);
}

internal sealed record RibbonBackstageEntrySlot(RibbonItem Item, Rectangle Bounds, Rectangle Clip, bool Footer)
{
    internal Rectangle VisibleBounds => Rectangle.Intersect(Bounds, Clip);
}
