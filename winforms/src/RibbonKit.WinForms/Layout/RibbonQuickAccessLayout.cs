namespace RibbonKit.WinForms.Layout;

// Placement supplies only a slot. A later title-bar surface can reuse this row
// measurement and the renderer without reparenting or cloning command models.
internal static class RibbonQuickAccessLayout
{
    internal static Rectangle BelowBounds(Size client, int rowHeight, Padding margin, bool joined, int dpi)
    {
        int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
        int left = joined ? Math.Min(S(margin.Left), client.Width) : 0;
        int right = joined ? Math.Min(S(margin.Right), Math.Max(0, client.Width - left)) : 0;
        int bottom = joined ? S(margin.Bottom) : 0;
        return new(left, Math.Max(0, client.Height - bottom - rowHeight), Math.Max(0, client.Width - left - right), rowHeight);
    }
    internal static RibbonLayoutSnapshot Build(RibbonControl ribbon, Rectangle slotBounds, bool fillWidth)
    {
        var result = new RibbonLayoutSnapshot();
        if (slotBounds.Width <= 0 || slotBounds.Height <= 0) return result;
        int S(double n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
        var entries = ribbon.QuickAccessItems.Where(e => e.Visible).ToArray();
        var metrics = RibbonDensityMetrics.For(ribbon.Density);
        int slot = S(metrics.QuickAccessButton), padding = Math.Min(S(2), slotBounds.Width / 2);
        var widths = entries.Select(e => S(RibbonQuickAccess.Width(e) == RibbonQuickAccess.ButtonWidth ? metrics.QuickAccessButton : metrics.QuickAccessMenu)).ToArray();
        bool overflow = widths.Sum() + 2 * padding > slotBounds.Width;
        int count = 0, used = 0, available = Math.Max(0, slotBounds.Width - 2 * padding - (overflow ? slot : 0));
        while (count < entries.Length && used + widths[count] <= available) used += widths[count++];
        int width = fillWidth ? slotBounds.Width : Math.Min(slotBounds.Width, used + (overflow ? slot : 0) + 2 * padding);
        var clip = slotBounds with { Width = width };
        result.QuickAccessBounds = clip;
        int rowHeight = fillWidth ? clip.Height : Math.Min(clip.Height, S(metrics.QuickAccessRow));
        int top = clip.Top + (clip.Height - rowHeight) / 2 + (fillWidth ? 0 : Math.Min(S(metrics.HeaderQuickAccessOffset), (clip.Height - rowHeight) / 2));
        int left = clip.Left + padding;
        for (int i = 0; i < count; i++)
        {
            var target = new RibbonTarget(TargetKind.QuickAccess, entries[i], new(left, top + S(2), widths[i], Math.Max(0, rowHeight - S(4))), clip,
                RibbonItemSize.Small, AutomaticKeyTip: RibbonQuickAccess.AutomaticKeyTip(i, entries.Length));
            result.Targets.AddRange(RibbonSplitLayout.Expand(target, ribbon.DeviceDpi, false));
            left += widths[i];
        }
        if (!overflow) return result;
        result.QuickAccessOverflowItems.AddRange(entries.Skip(count));
        result.Targets.Add(new(TargetKind.QuickAccessOverflow, ribbon.QuickAccessOverflow,
            new(left, top + S(2), Math.Max(0, Math.Min(slot, width - 2 * padding - used)), Math.Max(0, rowHeight - S(4))), clip));
        return result;
    }
}
