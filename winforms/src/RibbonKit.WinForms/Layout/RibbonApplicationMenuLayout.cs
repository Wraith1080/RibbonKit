namespace RibbonKit.WinForms.Layout;

// All rectangles are device pixels. Offsets are applied once by the presentation.
internal sealed class RibbonApplicationMenuLayout
{
    internal Size Size { get; private init; }
    internal Rectangle Navigation { get; private init; }
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
        var font = ribbon.Font;
        var metrics = RibbonDensityMetrics.For(ribbon.Density);
        int pad = S(4), bar = SystemInformation.GetVerticalScrollBarWidthForDpi(dpi);
        int row = Math.Max(S(metrics.Touch ? 44 : 40), font.Height + S(12));
        var left = ribbon.FileMenu.Items.Where(i => i.Visible).ToArray();
        var right = pane.Items.Where(i => i.Visible).ToArray();
        var footer = ribbon.ApplicationMenu.FooterItems.Where(i => i.Visible).ToArray();
        int navWidth = Math.Clamp(left.Where(i => i is not RibbonMenuSeparatorItem)
            .Select(i => TextRenderer.MeasureText(i.Text, font).Width + S(metrics.MenuTextInset + metrics.MenuTextRightPadding + (i is RibbonDropDownItem ? metrics.SplitArrow : 0)))
            .DefaultIfEmpty(S(150)).Max(), S(150), S(260));
        if (left.Length * row > maximum.Height - S(48)) navWidth = Math.Min(S(260), navWidth + bar);
        int width = Math.Max(1, Math.Min(maximum.Width, navWidth + S(340) + 3 * pad + 1));
        navWidth = Math.Min(navWidth, Math.Max(1, width * 3 / 5));
        int paneWidth = Math.Max(1, width - navWidth - 3 * pad - 1);
        int[] Heights(RibbonItem[] items, int w) => items.Select(i => i is RibbonMenuSeparatorItem ? S(9) : Math.Max(row, RibbonMenuLayout.RowHeight(i, w, font, dpi, ribbon.Density, false))).ToArray();
        var lh = Heights(left, Math.Max(1, navWidth - bar));
        var rh = Heights(right, Math.Max(1, paneWidth - bar));
        int header = pane.DropDownHeader.Length == 0 ? 0 : Math.Max(S(32), font.Height + S(12));
        int footerRow = Math.Max(S(metrics.Touch ? 44 : 36), font.Height + S(12));
        var footerRects = new List<Rectangle>();
        int fx = pad, fy = 0;
        foreach (var item in footer)
        {
            int fw = item is RibbonMenuSeparatorItem ? S(9) : Math.Max(S(metrics.Touch ? 88 : 72), TextRenderer.MeasureText(item.Text, font).Width + S(metrics.MenuTextInset + metrics.MenuTextRightPadding + (item is RibbonDropDownItem ? metrics.SplitArrow : 0)));
            fw = Math.Min(fw, Math.Max(1, width - 2 * pad - bar));
            if (fx > pad && fx + fw > width - pad - bar) { fx = pad; fy += footerRow; }
            footerRects.Add(new(fx, fy, fw, footerRow)); fx += fw + pad;
        }
        int footerExtent = footer.Length == 0 ? 0 : fy + footerRow;
        int preferredPaneHeight = pane.Content is { } content ? Math.Max(S(240), content.Height) : rh.Sum() + header;
        int height = Math.Max(1, Math.Min(maximum.Height, Math.Max(S(280), Math.Max(lh.Sum(), preferredPaneHeight)) + 2 * pad + Math.Min(footerExtent, 3 * footerRow) + (footer.Length == 0 ? 0 : pad + 1)));
        int footerHeight = footer.Length == 0 ? 0 : Math.Min(footerExtent, Math.Max(1, Math.Min(height / 3, 3 * footerRow)));
        int bottom = Math.Max(pad + 1, height - pad - footerHeight - (footer.Length == 0 ? 0 : pad + 1));
        var nav = new Rectangle(pad, pad, Math.Max(1, navWidth), Math.Max(1, bottom - pad));
        var paneRect = new Rectangle(nav.Right + pad + 1, Math.Min(bottom - 1, pad + header), paneWidth, Math.Max(1, bottom - pad - header));
        var footerRect = new Rectangle(pad, height - pad - footerHeight, Math.Max(1, width - 2 * pad), footerHeight);
        if (lh.Sum() > nav.Height) nav.Width = Math.Max(1, nav.Width - bar);
        if (pane.Content is null && rh.Sum() > paneRect.Height) paneRect.Width = Math.Max(1, paneRect.Width - bar);
        if (footerExtent > footerRect.Height) footerRect.Width = Math.Max(1, footerRect.Width - bar);
        var result = new RibbonApplicationMenuLayout
        {
            Size = new(width, height), Navigation = nav, Pane = paneRect,
            Header = header == 0 ? Rectangle.Empty : new(paneRect.Left, pad, paneWidth, Math.Max(1, paneRect.Top - pad)),
            Footer = footerRect, NavigationExtent = lh.Sum(), PaneExtent = rh.Sum(), FooterExtent = footerExtent
        };
        void Rows(RibbonItem[] items, int[] heights, Rectangle viewport)
        {
            int y = viewport.Top;
            int label = items.Where(i => i is IRibbonEditorItem).Select(i => RibbonEditorLayout.LabelWidth(i, font, dpi)).DefaultIfEmpty(0).Max();
            for (int i = 0; i < items.Length; i++)
            {
                result.Targets.Add(new(TargetKind.Item, items[i], new(viewport.Left, y, viewport.Width, heights[i]), viewport, RibbonItemSize.Medium, LabelWidth: label));
                y += heights[i];
            }
        }
        Rows(left, lh, nav);
        if (pane.Content is null && pane.ContentType is null) Rows(right, rh, paneRect);
        for (int i = 0; i < footer.Length; i++)
        {
            var bounds = footerRects[i]; bounds.Offset(0, footerRect.Top);
            result.Targets.Add(new(TargetKind.Item, footer[i], bounds, footerRect, RibbonItemSize.Medium));
        }
        return result;
    }
}
