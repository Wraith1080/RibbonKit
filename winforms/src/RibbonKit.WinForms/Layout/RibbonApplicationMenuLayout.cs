namespace RibbonKit.WinForms.Layout;

// All rectangles are device pixels. Offsets are applied once by the presentation.
internal sealed class RibbonApplicationMenuLayout
{
    internal Size Size { get; private init; }
    internal Rectangle Frame { get; private init; }
    internal Rectangle Navigation { get; private init; }
    // The viewport excludes an optional scrollbar; this boundary never does.
    internal int NavigationDividerX { get; private init; }
    internal Rectangle Pane { get; private init; }
    internal Rectangle Header { get; private init; }
    internal Rectangle Footer { get; private init; }
    internal int NavigationExtent { get; private init; }
    internal int PaneExtent { get; private init; }
    internal int FooterExtent { get; private init; }
    internal List<RibbonTarget> Targets { get; } = [];
    internal static RibbonApplicationMenuLayout Build(RibbonControl ribbon, RibbonDropDownItem pane, Size maximum)
    {
        int dpi = ribbon.DeviceDpi;
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        var font = ribbon.Font; var density = ribbon.Density; bool touch = density == RibbonDensity.Touch;
        int pad = S(5), inset = S(RibbonApplicationMenuMetrics.PanePadding(ribbon.Theme)), bar = SystemInformation.GetVerticalScrollBarWidthForDpi(dpi);
        var left = ribbon.FileMenu.Items.Where(i => i.Visible).ToArray();
        var contentMetrics = new RibbonApplicationMenuContentMetrics(ribbon.Theme, dpi, density);
        var right = pane.Items.Where(i => i.Visible).ToArray();
        var footer = ribbon.ApplicationMenu.FooterItems.Where(i => i.Visible).ToArray();
        int navWidth = Math.Clamp(left.Where(i => i is not RibbonMenuSeparatorItem)
            .Select(i => TextRenderer.MeasureText(i.Text, font).Width + contentMetrics.NavigationWidthPadding + (i is RibbonDropDownItem ? contentMetrics.Arrow : 0))
            .DefaultIfEmpty(S(150)).Max(), S(150), S(260));
        if (left.Length * S(RibbonApplicationMenuMetrics.NavigationMinimum(density, ribbon.Theme)) > maximum.Height - S(48)) navWidth = Math.Min(S(260), navWidth + bar);
        int extraInset = Math.Max(0, inset - S(10));
        // Roomier navigation reaches the former spare strip beside its rows.
        // Reserve that width as well as the right pane's authored content inset.
        navWidth += extraInset;
        int extraPane = 2 * extraInset;
        int width = Math.Max(1, Math.Min(maximum.Width, navWidth + S(340) + extraPane + 2 * pad + 3));
        navWidth = Math.Min(navWidth, Math.Max(1, width * 3 / 5));
        int paneWidth = Math.Max(1, width - navWidth - 2 * pad - 3);
        if (ribbon.Theme.ApplicationMenuPaneHorizontalPadding is not null) inset = Math.Min(inset, Math.Max(0, (paneWidth - 1) / 2));
        int[] Heights(RibbonItem[] items, int w, bool navigation) => items.Select(i => RibbonApplicationMenuMetrics.RowHeight(i, w, font, dpi, density, navigation, ribbon.Theme)).ToArray();
        var lh = Heights(left, Math.Max(1, navWidth - bar), true);
        var rh = Heights(right, Math.Max(1, paneWidth - 2 * inset - bar), false);
        int header = pane.DropDownHeader.Length == 0 ? 0 : Math.Max(S(28), font.Height + S(12));
        int footerRow = RibbonApplicationMenuMetrics.FooterRowHeight(footer, font, dpi, density, ribbon.Theme), footerGap = S(RibbonApplicationMenuMetrics.FooterGap(ribbon.Theme));
        var footerRects = new List<Rectangle>(); int fx = 0, fy = 0;
        int footerAvailable = Math.Max(1, width - 2 * pad - bar);
        foreach (var item in footer)
        {
            bool hasIcon = RibbonApplicationMenuMetrics.HasIcon(item) || contentMetrics.CustomFooterGeometry && (item is RibbonCheckBoxItem or RibbonRadioButtonItem);
            int fw = item is RibbonMenuSeparatorItem ? contentMetrics.SeparatorHeight : Math.Max(S(touch ? 94 : 88), TextRenderer.MeasureText(item.Text, font).Width + contentMetrics.FooterPreferredPadding(hasIcon)
                + (item is RibbonDropDownItem ? contentMetrics.Arrow : 0));
            fw = Math.Min(fw, footerAvailable);
            if (fx > 0 && fx + fw > footerAvailable) { fx = 0; fy += footerRow + footerGap; }
            footerRects.Add(new(fx, fy, fw, footerRow)); fx += fw + footerGap;
        }
        // Right align each wrapped row. A scrollbar has reserved horizontal clearance.
        foreach (var row in footerRects.Select((r, i) => (r, i)).GroupBy(p => p.r.Y))
        {
            int shift = footerAvailable - row.Max(p => p.r.Right);
            foreach (var entry in row) footerRects[entry.i] = entry.r with { X = entry.r.X + shift };
        }
        int footerExtent = footer.Length == 0 ? 0 : fy + footerRow;
        int preferredPaneHeight = pane.Content is { } content ? Math.Max(S(240), content.Height) : rh.Sum() + header;
        int height = Math.Max(1, Math.Min(maximum.Height, Math.Max(S(280), Math.Max(lh.Sum(), preferredPaneHeight)) + 2 * pad + 2 + (footer.Length == 0 ? 0 : Math.Min(footerExtent, 3 * (footerRow + footerGap)) + S(8))));
        int footerHeight = footer.Length == 0 ? 0 : Math.Min(footerExtent, Math.Max(1, Math.Min(height / 3, 3 * (footerRow + footerGap))));
        if (footerExtent <= footerHeight) for (int i = 0; i < footerRects.Count; i++) footerRects[i] = footerRects[i] with { X = footerRects[i].X + bar };
        int frameBottom = Math.Max(pad + 3, height - pad - footerHeight - (footer.Length == 0 ? 0 : S(8)));
        var frame = new Rectangle(pad, pad, Math.Max(1, width - 2 * pad), Math.Max(1, frameBottom - pad));
        var nav = new Rectangle(frame.Left + 1, frame.Top + 1, Math.Max(1, navWidth), Math.Max(1, frame.Height - 2));
        int paneLeft = nav.Right + 1;
        int paneTop = Math.Min(frame.Bottom - 2, nav.Top + header + S(4));
        var paneRect = new Rectangle(paneLeft + inset, paneTop, Math.Max(1, paneWidth - 2 * inset), Math.Max(1, frame.Bottom - 1 - paneTop - S(4)));
        var footerRect = new Rectangle(pad, height - pad - footerHeight, Math.Max(1, width - 2 * pad), footerHeight);
        if (lh.Sum() > nav.Height) nav.Width = Math.Max(1, nav.Width - bar);
        if (pane.Content is null && rh.Sum() > paneRect.Height) paneRect.Width = Math.Max(1, paneRect.Width - bar);
        if (footerExtent > footerRect.Height) footerRect.Width = Math.Max(1, footerRect.Width - bar);
        var result = new RibbonApplicationMenuLayout
        {
            Size = new(width, height), Frame = frame, Navigation = nav, NavigationDividerX = paneLeft - 1, Pane = paneRect,
            Header = header == 0 ? Rectangle.Empty : ReferenceEquals(pane, ribbon.ApplicationMenu.DefaultPane)
                ? new(paneLeft + inset, nav.Top, Math.Max(1, paneWidth - 2 * inset), header)
                : new(paneLeft, nav.Top, Math.Max(1, frame.Right - 1 - paneLeft), header),
            Footer = footerRect, NavigationExtent = lh.Sum(), PaneExtent = rh.Sum(), FooterExtent = footerExtent
        };
        void Rows(RibbonItem[] items, int[] heights, Rectangle viewport)
        {
            int y = viewport.Top;
            int label = items.Where(i => i is IRibbonEditorItem).Select(i => RibbonEditorLayout.LabelWidth(i, font, dpi)).DefaultIfEmpty(0).Max();
            for (int i = 0; i < items.Length; i++)
            {
                result.Targets.Add(new(TargetKind.Item, items[i], new(viewport.Left, y, viewport.Width, heights[i]), viewport, RibbonItemSize.Medium, LabelWidth: label)); y += heights[i];
            }
        }
        Rows(left, lh, nav);
        if (pane.Content is null && pane.ContentType is null) Rows(right, rh, paneRect);
        for (int i = 0; i < footer.Length; i++)
        {
            var bounds = footerRects[i]; bounds.Offset(footerRect.Left, footerRect.Top);
            result.Targets.Add(new(TargetKind.Item, footer[i], bounds, footerRect, RibbonItemSize.Medium));
        }
        return result;
    }
}
