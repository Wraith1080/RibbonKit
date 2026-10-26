namespace RibbonKit.WinForms.Layout;

internal enum TargetKind
{
    Tab, Group, Item, TabBack, TabNext, BodyBack, BodyNext
}
internal sealed record RibbonTarget(TargetKind Kind, RibbonElement? Element, Rectangle Bounds, Rectangle Clip, RibbonItemSize Size = RibbonItemSize.Large, bool ReserveIcon = false)
{
    internal Rectangle VisibleBounds => Rectangle.Intersect(Bounds, Clip);
    internal bool Enabled => Element?.EffectiveEnabled ?? true;
    internal string Identity => Element?.Id ?? Kind.ToString();
}
internal sealed record GroupSlot(RibbonGroup Group, Rectangle Bounds, bool Collapsed);
internal sealed class RibbonLayoutSnapshot
{
    internal List<RibbonTarget> Targets { get; } = [];
    internal List<GroupSlot> Groups { get; } = [];
    internal Rectangle BodyViewport
    {
        get; set;
    }
    internal int BodyExtent
    {
        get; set;
    }
    internal int TabExtent
    {
        get; set;
    }
    internal Rectangle TabViewport
    {
        get; set;
    }
}
internal sealed record MeasuredItem(RibbonItem Item, Rectangle Bounds, RibbonItemSize Size, bool ReserveIcon = false);
internal sealed record MeasuredGroup(int Width, List<MeasuredItem> Items);

internal sealed class RibbonLayoutEngine
{
    private readonly Dictionary<(RibbonGroup, int, int), MeasuredGroup> _cache = [];
    internal void Clear() => _cache.Clear();
    internal static int Scale(double value, int dpi) => (int)Math.Round(value * dpi / 96d);
    internal MeasuredGroup Measure(RibbonGroup group, int state, Font font, int dpi, RibbonTheme theme)
    {
        if (_cache.TryGetValue((group, state, dpi), out var cached))
            return cached;
        int S(double n) => Scale(n, dpi);
        if (state == 3)
            return _cache[(group, state, dpi)] = new(S(62), []);
        int x = S(6), row = 0, columnWidth = 0, columnStart = 0;
        int fontLogical = (int)Math.Ceiling(font.Height * 96d / dpi);
        int rowLogical = Math.Max(26, fontLogical + 8), captionLogical = Math.Max(20, fontLogical + 6);
        var items = new List<MeasuredItem>();
        void FinishColumn()
        {
            if (row == 0)
                return;
            bool reserve = items.Skip(columnStart).Any(entry => entry.Item.Icon is not null || entry.Item.Image is not null || entry.Item.LargeImage is not null);
            for (int index = columnStart; index < items.Count; index++)
            {
                var entry = items[index];
                items[index] = entry with
                {
                    Bounds = entry.Bounds with
                    {
                        Width = entry.Size == RibbonItemSize.Small ? entry.Bounds.Width : columnWidth
                    },
                    ReserveIcon = reserve
                };
            }
            x += columnWidth + S(3);
            row = 0;
            columnWidth = 0;
            columnStart = items.Count;
        }
        foreach (var item in group.Items.Where(i => i.Visible))
        {
            var size = item.SizePolicy.At(state);
            bool large = size == RibbonItemSize.Large;
            int label = TextRenderer.MeasureText(item.Text, font, new Size(1000, 1000), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
            int width = large ? Math.Max(S(60), Math.Min(S(108), label + S(14))) : size == RibbonItemSize.Small ? S(28) : Math.Max(S(54), Math.Min(S(180), label + S(38)));
            if (large)
            {
                FinishColumn();
                items.Add(new(item, new(x, S(4), width, S(theme.BodyHeight - captionLogical - 8)), size));
                x += width + S(3);
                columnStart = items.Count;
            }
            else
            {
                items.Add(new(item, new(x, S(4 + row * rowLogical), width, S(rowLogical - 1)), size));
                columnWidth = Math.Max(columnWidth, width);
                if (++row == 3)
                    FinishColumn();
            }
        }
        FinishColumn();
        int header = TextRenderer.MeasureText(group.Text, font).Width + S(20);
        return _cache[(group, state, dpi)] = new(Math.Max(x + S(5), header), items);
    }
    internal RibbonLayoutSnapshot Build(RibbonControl ribbon, int tabOffset, int bodyOffset)
    {
        var result = new RibbonLayoutSnapshot();
        int dpi = ribbon.DeviceDpi;
        int S(double n) => Scale(n, dpi);
        int tabHeight = S(ribbon.LayoutTheme.TabHeight), bodyHeight = S(ribbon.LayoutTheme.BodyHeight);
        var client = ribbon.ClientRectangle;
        var tabs = ribbon.Tabs.Where(t => t.Visible).ToArray();
        var widths = tabs.Select(t => Math.Max(S(58), TextRenderer.MeasureText(t.Text, ribbon.Font).Width + S(28))).ToArray();
        int tabTotal = widths.Sum();
        bool tabOverflow = tabTotal > client.Width;
        var tabViewport = new Rectangle(tabOverflow ? S(22) : 0, 0, Math.Max(0, client.Width - (tabOverflow ? S(44) : 0)), tabHeight);
        result.TabExtent = tabTotal;
        result.TabViewport = tabViewport;
        int tx = tabViewport.Left - Math.Clamp(tabOffset, 0, Math.Max(0, tabTotal - tabViewport.Width));
        for (int i = 0; i < tabs.Length; i++)
        {
            result.Targets.Add(new(TargetKind.Tab, tabs[i], new(tx, 0, widths[i], tabHeight), tabViewport));
            tx += widths[i];
        }
        if (tabOverflow)
        {
            result.Targets.Add(new(TargetKind.TabBack, null, new(0, 0, S(22), tabHeight), client));
            result.Targets.Add(new(TargetKind.TabNext, null, new(client.Width - S(22), 0, S(22), tabHeight), client));
        }
        if (ribbon.SelectedTab is not { } selected)
            return result;
        var groups = selected.Groups.Where(g => g.Visible).ToArray();
        var maps = groups.Select(g => !g.CanResize ? new[] { 0 } : g.ReductionMode switch { RibbonGroupReductionMode.Resize => new[] { 0, 1, 2 }, RibbonGroupReductionMode.ResizeThenCollapse => new[] { 0, 1, 2, 3 }, _ => new[] { 0, 3 } }).ToArray();
        var tables = groups.Select((g, i) => maps[i].Select(s => (double)Measure(g, s, ribbon.Font, dpi, ribbon.LayoutTheme).Width).ToArray()).ToArray();
        var order = groups.Select((g, i) => (g, i)).Where(p => p.g.CanResize)
            .OrderByDescending(p => p.g.ReductionPriority.HasValue).ThenByDescending(p => p.g.ReductionPriority ?? int.MinValue)
            .ThenByDescending(p => p.g.ReductionPriority.HasValue ? 0 : tables[p.i][0]).ThenByDescending(p => p.i).Select(p => p.i).ToArray();
        int available = Math.Max(0, client.Width - S(8));
        int[] states = ReductionAlgorithm.ComputeStates(available, tables, order);
        int total = groups.Select((g, i) => (int)tables[i][states[i]]).Sum();
        bool overflow = total > available;
        if (overflow)
        {
            available = Math.Max(0, available - S(44));
            states = ReductionAlgorithm.ComputeStates(available, tables, order);
            total = groups.Select((g, i) => (int)tables[i][states[i]]).Sum();
        }
        var viewport = new Rectangle(S(4) + (overflow ? S(22) : 0), tabHeight, available, bodyHeight);
        result.BodyViewport = viewport;
        result.BodyExtent = total;
        int x = viewport.Left - Math.Clamp(bodyOffset, 0, Math.Max(0, total - available));
        for (int i = 0; i < groups.Length; i++)
        {
            int state = maps[i][states[i]];
            var measure = Measure(groups[i], state, ribbon.Font, dpi, ribbon.LayoutTheme);
            var bounds = new Rectangle(x, tabHeight, measure.Width, bodyHeight);
            result.Groups.Add(new(groups[i], bounds, state == 3));
            if (state == 3)
                result.Targets.Add(new(TargetKind.Group, groups[i], Rectangle.Inflate(bounds, -S(3), -S(3)), viewport));
            else
                foreach (var item in measure.Items)
                    result.Targets.Add(new(TargetKind.Item, item.Item, new(x + item.Bounds.X, tabHeight + item.Bounds.Y, item.Bounds.Width, item.Bounds.Height), viewport, item.Size, item.ReserveIcon));
            x += measure.Width;
        }
        if (overflow)
        {
            result.Targets.Add(new(TargetKind.BodyBack, null, new(0, tabHeight, S(22), bodyHeight), client));
            result.Targets.Add(new(TargetKind.BodyNext, null, new(client.Width - S(22), tabHeight, S(22), bodyHeight), client));
        }
        return result;
    }
}
