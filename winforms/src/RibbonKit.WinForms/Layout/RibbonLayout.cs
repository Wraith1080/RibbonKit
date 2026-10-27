namespace RibbonKit.WinForms.Layout;

internal enum TargetKind
{
    Tab, Group, Item, TabBack, TabNext, BodyBack, BodyNext, Minimize, QuickAccess, QuickAccessOverflow, OverflowUp, OverflowDown, FileMenu
}
internal sealed record RibbonTarget(TargetKind Kind, RibbonElement? Element, Rectangle Bounds, Rectangle Clip, RibbonItemSize Size = RibbonItemSize.Large, bool ReserveIcon = false, bool DropDownPart = false, int LabelWidth = 0, string? AutomaticKeyTip = null)
{
    internal Rectangle VisibleBounds => Rectangle.Intersect(Bounds, Clip);
    internal bool Enabled => Kind == TargetKind.QuickAccess ? Element is not null && RibbonQuickAccess.Available(Element) : Element is not RibbonMenuSeparatorItem && (Element?.EffectiveEnabled ?? true);
    internal string Identity => (Element?.Id ?? Kind.ToString()) + (DropDownPart ? ":dropdown" : "");
    internal string TipText => (Kind == TargetKind.QuickAccess && Element is not null ? RibbonQuickAccess.Caption(Element) : Element?.Text ?? "") + (DropDownPart ? " options" : "");
    internal string PreferredKeyTip => Element is RibbonQuickAccessDropDownReference { Source: RibbonSplitButtonItem } reference && DropDownPart
        ? reference.DropDownKeyTip.Length > 0 ? reference.DropDownKeyTip : reference.KeyTip.Length > 0 ? "" : AutomaticKeyTip ?? ""
        : DropDownPart && Element is RibbonSplitButtonItem split ? split.DropDownKeyTip
        : Element?.KeyTip is { Length: > 0 } authored ? authored : AutomaticKeyTip ?? "";
}
internal sealed record GroupSlot(RibbonGroup Group, Rectangle Bounds, bool Collapsed);
internal sealed class RibbonLayoutSnapshot
{
    internal Rectangle BodyBounds { get; set; }
    internal Rectangle QuickAccessBounds { get; set; }
    internal List<RibbonElement> QuickAccessOverflowItems { get; } = [];
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
internal sealed record MeasuredItem(RibbonItem Item, Rectangle Bounds, RibbonItemSize Size, bool ReserveIcon = false, int LabelWidth = 0);
internal sealed record MeasuredGroup(int Width, List<MeasuredItem> Items);

internal sealed class RibbonLayoutEngine
{
    internal const int CommandTopInset = 6;
    internal const int CommandBottomInset = 8;
    internal const int CommandVerticalInset = CommandTopInset + CommandBottomInset;
    private readonly Dictionary<(RibbonGroup, int, int), MeasuredGroup> _cache = [];
    internal void Clear() => _cache.Clear();
    private static int LargeItemWidth(RibbonItem item, Font font, int dpi, RibbonTheme theme)
    {
        int S(double n) => Scale(n, dpi);
        int label = RibbonCaptionLayout.TextWidth(item.Text, font);
        int padding = item is RibbonDropDownItem and not RibbonSplitButtonItem ? 30 : 14;
        return Math.Clamp(label + S(padding), S(60), S(theme.LargeButtonMaxWidth));
    }
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
            bool reserve = items.Skip(columnStart).Any(entry => entry.Item is RibbonCheckBoxItem or RibbonRadioButtonItem || entry.Item.Icon is not null || entry.Item.Image is not null || entry.Item.LargeImage is not null);
            int labelWidth = items.Skip(columnStart).Where(entry => entry.Item is IRibbonEditorItem)
                .Select(entry => RibbonEditorLayout.LabelWidth(entry.Item, font, dpi)).DefaultIfEmpty(0).Max();
            foreach (var entry in items.Skip(columnStart).Where(entry => entry.Item is IRibbonEditorItem))
                columnWidth = Math.Max(columnWidth, labelWidth + S(((IRibbonEditorItem)entry.Item).InputWidth + 6));
            for (int index = columnStart; index < items.Count; index++)
            {
                var entry = items[index];
                items[index] = entry with
                {
                    Bounds = entry.Bounds with
                    {
                        Width = entry.Size == RibbonItemSize.Small ? entry.Bounds.Width : columnWidth
                    },
                    ReserveIcon = reserve,
                    LabelWidth = entry.Item is IRibbonEditorItem ? labelWidth : 0
                };
            }
            x += columnWidth + S(3);
            row = 0;
            columnWidth = 0;
            columnStart = items.Count;
        }
        foreach (var item in group.Items.Where(i => i.Visible))
        {
            var size = item is IRibbonEditorItem or RibbonCheckBoxItem or RibbonRadioButtonItem ? RibbonItemSize.Medium : item.SizePolicy.At(state);
            bool large = size == RibbonItemSize.Large;
            int label = TextRenderer.MeasureText(item.Text, font, new Size(1000, 1000), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
            bool dropDownOnly = item is RibbonDropDownItem and not RibbonSplitButtonItem;
            int width = large ? LargeItemWidth(item, font, dpi, theme) : size == RibbonItemSize.Small ? S(28) : Math.Max(S(54), Math.Min(S(180), label + S(38)));
            if (item is IRibbonEditorItem editor)
                width = RibbonEditorLayout.LabelWidth(item, font, dpi) + S(editor.InputWidth + 6);
            if (large && item is RibbonGalleryItem { IsInRibbon: true } gallery)
                width = S(gallery.InRibbonColumnCount * gallery.TileWidth + RibbonGalleryStripLayout.UtilityWidth + 3 * RibbonGalleryStripLayout.Padding);
            if (item is RibbonSplitButtonItem && !large)
                width += S(20);
            else if (dropDownOnly && !large)
                width += S(16);
            if (large)
            {
                FinishColumn();
                items.Add(new(item, new(x, S(CommandTopInset), width, S(theme.BodyHeight - captionLogical - CommandVerticalInset)), size));
                x += width + S(3);
                columnStart = items.Count;
            }
            else
            {
                items.Add(new(item, new(x, S(CommandTopInset + row * rowLogical), width, S(rowLogical - 1)), size));
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
        int tabHeight = S(ribbon.LayoutTheme.TabHeight);
        var client = ribbon.ClientRectangle;
        var tabs = ribbon.Tabs.Where(t => t.Visible).ToArray();
        var widths = tabs.Select(t => Math.Max(S(58), TextRenderer.MeasureText(t.Text, ribbon.Font).Width + S(28))).ToArray();
        int tabTotal = widths.Sum(), utilityWidth = Math.Min(S(28), client.Width);
        int tabsStart = 0;
        if (ribbon.HasFileMenu)
        {
            int keepQuickAccess = ribbon.HasQuickAccess && ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TabRow ? S(RibbonQuickAccess.ButtonWidth + 4) : 0;
            int available = Math.Max(0, client.Width - utilityWidth - S(24) - keepQuickAccess);
            int left = Math.Min(S(ribbon.LayoutTheme.BodyMargin.Left), available);
            int width = Math.Min(Math.Max(S(50), TextRenderer.MeasureText(ribbon.FileMenu.Text, ribbon.Font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + S(28)), Math.Max(0, available - left));
            if (width > 0)
            {
                var bounds = new Rectangle(left, 0, width, tabHeight);
                result.Targets.Add(new(TargetKind.FileMenu, ribbon.FileMenu, bounds, bounds));
                tabsStart = bounds.Right;
            }
        }
        if (ribbon.HasQuickAccess)
        {
            bool below = ribbon.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon;
            int available = below ? client.Width : Math.Min(S(ribbon.QuickAccessMaxWidth), Math.Max(0, client.Width - tabsStart - utilityWidth - S(24)));
            int top = below ? client.Height - ribbon.QuickAccessHeight : 0;
            var quickAccess = RibbonQuickAccessLayout.Build(ribbon, new(below ? 0 : tabsStart, top, available, below ? ribbon.QuickAccessHeight : tabHeight), below);
            result.QuickAccessBounds = quickAccess.QuickAccessBounds;
            result.QuickAccessOverflowItems.AddRange(quickAccess.QuickAccessOverflowItems);
            result.Targets.AddRange(quickAccess.Targets);
            if (!below) tabsStart += result.QuickAccessBounds.Width;
        }
        int tabsWidth = Math.Max(0, client.Width - utilityWidth - tabsStart);
        bool tabOverflow = tabTotal > tabsWidth;
        // Keep part of a tab reachable when the utility and two full-size
        // scrollers would otherwise consume the complete narrow tab strip.
        int tabScrollWidth = tabOverflow ? Math.Min(S(22), Math.Max(0, (tabsWidth - S(24)) / 2)) : 0;
        var tabViewport = new Rectangle(tabsStart + tabScrollWidth, 0, Math.Max(0, tabsWidth - 2 * tabScrollWidth), tabHeight);
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
            var tabsClip = new Rectangle(tabsStart, 0, tabsWidth, tabHeight);
            result.Targets.Add(new(TargetKind.TabBack, null, new(tabsStart, 0, tabScrollWidth, tabHeight), tabsClip));
            result.Targets.Add(new(TargetKind.TabNext, null, new(tabsStart + tabsWidth - tabScrollWidth, 0, tabScrollWidth, tabHeight), tabsClip));
        }
        result.Targets.Add(new(TargetKind.Minimize, null, new(client.Width - utilityWidth, 0, utilityWidth, tabHeight), client));
        if (ribbon.IsMinimized || ribbon.SelectedTab is not { } selected) return result;
        var bodyLayout = BuildBody(ribbon, selected, client.Width, bodyOffset, tabHeight);
        result.BodyBounds = bodyLayout.BodyBounds; result.BodyViewport = bodyLayout.BodyViewport; result.BodyExtent = bodyLayout.BodyExtent;
        result.Groups.AddRange(bodyLayout.Groups);
        result.Targets.AddRange(bodyLayout.Targets.SelectMany(t => RibbonSplitLayout.Expand(t, dpi, false)));
        return result;
    }
    internal RibbonLayoutSnapshot BuildBody(RibbonControl ribbon, RibbonTab selected, int width, int bodyOffset, int top = 0)
    {
        var result = new RibbonLayoutSnapshot();
        int dpi = ribbon.DeviceDpi;
        int S(double n) => Scale(n, dpi);
        int bodyHeight = S(ribbon.LayoutTheme.BodyHeight);
        var groups = selected.Groups.Where(g => g.Visible).ToArray();
        var maps = groups.Select(g => RibbonQuickAccess.RetainsGroup(g) ? new[] { 3 } : !g.CanResize ? new[] { 0 } : g.ReductionMode switch { RibbonGroupReductionMode.Resize => new[] { 0, 1, 2 }, RibbonGroupReductionMode.ResizeThenCollapse => new[] { 0, 1, 2, 3 }, _ => new[] { 0, 3 } }).ToArray();
        var tables = groups.Select((g, i) => maps[i].Select(s => (double)Measure(g, s, ribbon.Font, dpi, ribbon.LayoutTheme).Width).ToArray()).ToArray();
        var order = groups.Select((g, i) => (g, i)).Where(p => p.g.CanResize && !RibbonQuickAccess.RetainsGroup(p.g))
            .OrderByDescending(p => p.g.ReductionPriority.HasValue).ThenByDescending(p => p.g.ReductionPriority ?? int.MinValue)
            .ThenByDescending(p => p.g.ReductionPriority.HasValue ? 0 : tables[p.i][0]).ThenByDescending(p => p.i).Select(p => p.i).ToArray();
        var margin = ribbon.LayoutTheme.BodyMargin;
        var body = new Rectangle(S(margin.Left), top + S(margin.Top),
            Math.Max(0, width - S(margin.Left) - S(margin.Right)), bodyHeight);
        result.BodyBounds = body;
        int inset = Math.Min(S(2), body.Width / 2);
        int available = Math.Max(0, body.Width - 2 * inset);
        int[] states = ReductionAlgorithm.ComputeStates(available, tables, order);
        int total = groups.Select((g, i) => (int)tables[i][states[i]]).Sum();
        bool overflow = total > available;
        if (overflow)
        {
            available = Math.Max(0, available - S(44));
            states = ReductionAlgorithm.ComputeStates(available, tables, order);
            total = groups.Select((g, i) => (int)tables[i][states[i]]).Sum();
        }
        var viewport = new Rectangle(body.Left + inset + (overflow ? S(22) : 0), body.Top, available, bodyHeight);
        result.BodyViewport = viewport;
        result.BodyExtent = total;
        int x = viewport.Left - Math.Clamp(bodyOffset, 0, Math.Max(0, total - available));
        for (int i = 0; i < groups.Length; i++)
        {
            int state = maps[i][states[i]];
            var measure = Measure(groups[i], state, ribbon.Font, dpi, ribbon.LayoutTheme);
            var bounds = new Rectangle(x, body.Top, measure.Width, bodyHeight);
            if (state == 3) bounds = Rectangle.FromLTRB(bounds.Left + S(3), bounds.Top + S(CommandTopInset),
                bounds.Right - S(3), bounds.Bottom - S(CommandBottomInset));
            result.Groups.Add(new(groups[i], bounds, state == 3));
            if (state == 3)
                result.Targets.Add(new(TargetKind.Group, groups[i], bounds, viewport));
            else
                foreach (var item in measure.Items)
                    result.Targets.Add(new(TargetKind.Item, item.Item, new(x + item.Bounds.X, body.Top + item.Bounds.Y, item.Bounds.Width, item.Bounds.Height), viewport, item.Size, item.ReserveIcon, LabelWidth: item.LabelWidth));
            x += measure.Width;
        }
        if (overflow)
        {
            int scrollInset = Math.Min(S(Math.Min(ribbon.LayoutTheme.BodyScrollButtonVerticalInset, ribbon.LayoutTheme.BodyHeight)), bodyHeight / 2);
            int scrollHeight = bodyHeight - 2 * scrollInset;
            result.Targets.Add(new(TargetKind.BodyBack, null, new(body.Left + inset, body.Top + scrollInset, S(22), scrollHeight), body));
            result.Targets.Add(new(TargetKind.BodyNext, null, new(body.Right - inset - S(22), body.Top + scrollInset, S(22), scrollHeight), body));
        }
        return result;
    }
}
