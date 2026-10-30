namespace RibbonKit.WinForms.Layout;

internal enum TargetKind
{
    Tab, Group, Item, TabBack, TabNext, BodyBack, BodyNext, Minimize, QuickAccess, QuickAccessOverflow, OverflowUp, OverflowDown, FileMenu, ModalClose, MessageAction, MessageClose
}
internal sealed record RibbonTarget(TargetKind Kind, RibbonElement? Element, Rectangle Bounds, Rectangle Clip, RibbonItemSize Size = RibbonItemSize.Large, bool ReserveIcon = false, bool DropDownPart = false, int LabelWidth = 0, string? AutomaticKeyTip = null, bool GalleryDropDown = false)
{
    internal Rectangle VisibleBounds => Rectangle.Intersect(Bounds, Clip);
    internal bool Enabled => Kind == TargetKind.QuickAccess ? Element is not null && RibbonQuickAccess.Available(Element) : Element is not RibbonMenuSeparatorItem && (Element?.EffectiveEnabled ?? true);
    internal string Identity => (Element?.Id ?? Kind.ToString()) + (DropDownPart ? ":dropdown" : "");
    internal string TipText => (Kind == TargetKind.QuickAccess && Element is not null ? RibbonQuickAccess.Caption(Element) : Element?.DisplayText ?? "") + (DropDownPart ? " options" : "");
    internal string PreferredKeyTip => Element is RibbonQuickAccessDropDownReference { Source: RibbonSplitButtonItem } reference && DropDownPart
        ? reference.DropDownKeyTip.Length > 0 ? reference.DropDownKeyTip : reference.KeyTip.Length > 0 ? "" : AutomaticKeyTip ?? ""
        : DropDownPart && Element is RibbonSplitButtonItem split ? split.DropDownKeyTip
        : Element?.KeyTip is { Length: > 0 } authored ? authored : AutomaticKeyTip ?? "";
}
internal sealed record GroupSlot(RibbonGroup Group, Rectangle Bounds, bool Collapsed, Rectangle Frame);
internal sealed class RibbonLayoutSnapshot
{
    internal Rectangle BodyBounds { get; set; }
    internal Rectangle BodyFrameBounds { get; set; }
    internal Rectangle QuickAccessBounds { get; set; }
    internal Rectangle MessageBounds { get; set; }
    internal List<MeasuredMessage> Messages { get; } = [];
    internal List<RibbonElement> QuickAccessOverflowItems { get; } = [];
    internal List<RibbonTarget> Targets { get; } = [];
    internal List<GroupSlot> Groups { get; } = [];
    internal List<Rectangle> SimplifiedDividers { get; } = [];
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
internal sealed record MeasuredItem(RibbonItem Item, Rectangle Bounds, RibbonItemSize Size, bool ReserveIcon = false, int LabelWidth = 0, bool GalleryDropDown = false);
internal sealed record MeasuredGroup(int Width, List<MeasuredItem> Items);

internal sealed partial class RibbonLayoutEngine
{
    internal const int CommandTopInset = 6;
    internal const int CommandBottomInset = 8;
    internal const int CommandVerticalInset = CommandTopInset + CommandBottomInset;
    internal const int GroupSideInset = 6;
    internal const int CollapsedGroupSideInset = 3;
    internal const int CollapsedGroupCaptionInset = 5;
    internal const int GroupDividerWidth = 1;
    private readonly Dictionary<(RibbonGroup, int, int), MeasuredGroup> _cache = [];
    internal void Clear() => _cache.Clear();
    private static int LargeItemWidth(RibbonItem item, Font font, int dpi, RibbonTheme theme)
    {
        int S(double n) => Scale(n, dpi);
        int label = RibbonCaptionLayout.TextWidth(item.Text, font);
        var metrics = RibbonDensityMetrics.For(item.Ribbon?.Density ?? RibbonDensity.Compact);
        int padding = item is RibbonDropDownItem and not RibbonSplitButtonItem ? 30 : 14;
        int width = Math.Clamp(label + S(padding + (metrics.Touch ? 8 : 0)), S(metrics.LargeWidth), S(Math.Max(metrics.LargeWidth, theme.LargeButtonMaxWidth)));
        if (metrics.Touch && item is RibbonSplitButtonItem { Layout: RibbonSplitButtonLayout.Horizontal })
            // Reserve the wider arrow without reducing the existing caption budget.
            width = Math.Max(S(metrics.LargeHorizontalSplitPrimaryMinimum) + S(metrics.HorizontalSplitArrow),
                width + S(metrics.HorizontalSplitArrow) - S(metrics.SplitArrow));
        return width;
    }
    internal static int Scale(double value, int dpi) => (int)Math.Round(value * dpi / 96d);
    internal MeasuredGroup Measure(RibbonGroup group, int state, Font font, int dpi, RibbonTheme theme)
    {
        if (_cache.TryGetValue((group, state, dpi), out var cached))
            return cached;
        int S(double n) => Scale(n, dpi);
        var metrics = RibbonDensityMetrics.For(group.Ribbon?.Density ?? RibbonDensity.Compact);
        if (state == 3)
        {
            int caption = RibbonCaptionLayout.TextWidth(group.DisplayText, font);
            int desired = caption + S(2 * (CollapsedGroupSideInset + CollapsedGroupCaptionInset));
            int width = Math.Clamp(desired, S(metrics.Touch ? 80 : 62), S(theme.CollapsedGroupMaxWidth));
            return _cache[(group, state, dpi)] = new(width + GroupDividerWidth, []);
        }
        int x = S(GroupSideInset), row = 0, columnWidth = 0, columnStart = 0;
        int fontLogical = (int)Math.Ceiling(font.Height * 96d / dpi);
        int rowLogical = Math.Max(metrics.CommandRow, fontLogical + 8), captionLogical = Math.Max(20, fontLogical + 6);
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
            x += columnWidth + S(metrics.ColumnGap);
            row = 0;
            columnWidth = 0;
            columnStart = items.Count;
        }
        foreach (var item in group.Items.Where(i => i.Visible))
        {
            var size = item is IRibbonEditorItem or RibbonCheckBoxItem or RibbonRadioButtonItem ? RibbonItemSize.Medium : item.SizePolicy.At(state);
            // A reduced in-ribbon gallery stays a full-column command. Its
            // strip/dropdown presentation is independent of the painted size.
            bool galleryDropDown = item is RibbonGalleryItem { IsInRibbon: true } && size != RibbonItemSize.Large;
            if (galleryDropDown) size = RibbonItemSize.Large;
            bool large = size == RibbonItemSize.Large;
            int label = TextRenderer.MeasureText(item.Text, font, new Size(1000, 1000), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
            bool dropDownOnly = item is RibbonDropDownItem and not RibbonSplitButtonItem;
            int width = large ? LargeItemWidth(item, font, dpi, theme) : size == RibbonItemSize.Small ? S(metrics.SmallWidth) : Math.Max(S(metrics.MediumWidth), Math.Min(S(180 + (metrics.Touch ? 16 : 0)), label + S(metrics.CaptionPadding)));
            if (item is IRibbonEditorItem editor)
                width = RibbonEditorLayout.LabelWidth(item, font, dpi) + S(editor.InputWidth + 6);
            if (large && !galleryDropDown && item is RibbonGalleryItem { IsInRibbon: true } gallery)
                width = S(gallery.InRibbonColumnCount * Math.Max(gallery.TileWidth, metrics.GalleryWidth) + metrics.GalleryUtility + 3 * RibbonGalleryStripLayout.Padding);
            if (item is RibbonSplitButtonItem && !large)
                width = Math.Max(width, S(metrics.HorizontalSplitPrimaryMinimum)) + S(metrics.HorizontalSplitArrow);
            else if (dropDownOnly && !large)
                width += S(metrics.Touch ? 28 : 16);
            if (large)
            {
                FinishColumn();
                items.Add(new(item, new(x, S(CommandTopInset), width, S(theme.BodyHeight - captionLogical - CommandVerticalInset)), size, GalleryDropDown: galleryDropDown));
                x += width + S(metrics.ColumnGap);
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
        int header = TextRenderer.MeasureText(group.DisplayText, font).Width + S(20);
        int contentWidth = items.Count == 0 ? 0 : items.Max(i => i.Bounds.Right) - S(GroupSideInset);
        int groupWidth = Math.Max(contentWidth + 2 * S(GroupSideInset), header);
        // Caption-driven width is shared equally, without retaining the final
        // inter-column gap. The right divider owns a separate device-pixel lane.
        if ((groupWidth - contentWidth) % 2 != 0) groupWidth++;
        int offset = (groupWidth - contentWidth) / 2 - S(GroupSideInset);
        if (offset != 0)
            for (int i = 0; i < items.Count; i++) items[i] = items[i] with { Bounds = items[i].Bounds with { X = items[i].Bounds.X + offset } };
        return _cache[(group, state, dpi)] = new(groupWidth + GroupDividerWidth, items);
    }
    internal RibbonLayoutSnapshot Build(RibbonControl ribbon, int tabOffset, int bodyOffset)
    {
        var result = new RibbonLayoutSnapshot();
        int dpi = ribbon.DeviceDpi;
        int S(double n) => Scale(n, dpi);
        int tabHeight = S(ribbon.LayoutTheme.TabHeight), tabTop = ribbon.WindowCaptionHeight;
        var placement = ribbon.EffectiveQuickAccessPosition;
        var client = ribbon.ClientRectangle;
        var tabs = ribbon.GetPresentedTabs().Where(t => t.Visible).ToArray();
        var widths = tabs.Select(t => Math.Max(S(58), TextRenderer.MeasureText(t.DisplayText, ribbon.Font).Width + S(28))).ToArray();
        var action = ribbon.HeaderAction;
        var metrics = RibbonDensityMetrics.For(ribbon.Density);
        int actionWidth = action?.Element is { } command ? TextRenderer.MeasureText(command.Text, ribbon.Font).Width + S(metrics.Touch ? 44 : 36) : S(metrics.SmallWidth);
        int tabTotal = widths.Sum(), utilityWidth = action is null ? 0 : Math.Min(actionWidth, Math.Max(0, client.Width - (action?.Element is null ? 0 : S(24))));
        int tabsStart = 0;
        if (ribbon.HasFileMenu)
        {
            int keepQuickAccess = ribbon.ShowQuickAccess && placement == RibbonQuickAccessPosition.TabRow ? S(metrics.QuickAccessButton + 4) : 0;
            int available = Math.Max(0, client.Width - utilityWidth - S(24) - keepQuickAccess);
            int left = Math.Min(S(ribbon.LayoutTheme.BodyMargin.Left), available);
            int width = Math.Min(Math.Max(S(50), TextRenderer.MeasureText(ribbon.FileMenu.Text, ribbon.Font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width + S(28)), Math.Max(0, available - left));
            if (width > 0)
            {
                var bounds = new Rectangle(left, tabTop, width, tabHeight);
                result.Targets.Add(new(TargetKind.FileMenu, ribbon.FileMenu, bounds, bounds));
                tabsStart = bounds.Right;
            }
        }
        if (ribbon.ShowQuickAccess)
        {
            bool below = placement == RibbonQuickAccessPosition.BelowRibbon;
            int available = below ? client.Width : Math.Min(S(ribbon.QuickAccessMaxWidth), Math.Max(0, client.Width - tabsStart - utilityWidth - S(24)));
            var slot = placement == RibbonQuickAccessPosition.TitleBar ? ribbon.WindowHost!.QuickAccessSlot(ribbon) : below ? RibbonQuickAccessLayout.BelowBounds(new(client.Width, Math.Max(0, client.Height - ribbon.MessageHeight)), ribbon.QuickAccessHeight, ribbon.LayoutTheme.BodyMargin, ribbon.JoinBelowRibbonQuickAccess, dpi)
                : new Rectangle(tabsStart, tabTop, available, tabHeight);
            var quickAccess = RibbonQuickAccessLayout.Build(ribbon, slot, below);
            result.QuickAccessBounds = quickAccess.QuickAccessBounds;
            result.QuickAccessOverflowItems.AddRange(quickAccess.QuickAccessOverflowItems);
            result.Targets.AddRange(quickAccess.Targets);
            if (placement == RibbonQuickAccessPosition.TabRow) tabsStart += result.QuickAccessBounds.Width;
        }
        int tabsWidth = Math.Max(0, client.Width - utilityWidth - tabsStart);
        bool tabOverflow = tabTotal > tabsWidth;
        // Keep part of a tab reachable when the utility and two full-size
        // scrollers would otherwise consume the complete narrow tab strip.
        int tabScrollWidth = tabOverflow ? Math.Min(S(metrics.ScrollButton), Math.Max(0, (tabsWidth - S(24)) / 2)) : 0;
        var tabViewport = new Rectangle(tabsStart + tabScrollWidth, tabTop, Math.Max(0, tabsWidth - 2 * tabScrollWidth), tabHeight);
        result.TabExtent = tabTotal;
        result.TabViewport = tabViewport;
        int tx = tabViewport.Left - Math.Clamp(tabOffset, 0, Math.Max(0, tabTotal - tabViewport.Width));
        for (int i = 0; i < tabs.Length; i++)
        {
            result.Targets.Add(new(TargetKind.Tab, tabs[i], new(tx, tabTop, widths[i], tabHeight), tabViewport));
            tx += widths[i];
        }
        if (tabOverflow)
        {
            var tabsClip = new Rectangle(tabsStart, tabTop, tabsWidth, tabHeight);
            result.Targets.Add(new(TargetKind.TabBack, null, new(tabsStart, tabTop, tabScrollWidth, tabHeight), tabsClip));
            result.Targets.Add(new(TargetKind.TabNext, null, new(tabsStart + tabsWidth - tabScrollWidth, tabTop, tabScrollWidth, tabHeight), tabsClip));
        }
        if (action is { } header) result.Targets.Add(new(header.Kind, header.Element, new(client.Width - utilityWidth, tabTop, utilityWidth, tabHeight), client));
        if (!ribbon.IsMinimized && ribbon.SelectedTab is { } selected)
        {
            var bodyLayout = BuildBody(ribbon, selected, client.Width, bodyOffset, tabTop + tabHeight);
            result.BodyBounds = bodyLayout.BodyBounds; result.BodyViewport = bodyLayout.BodyViewport; result.BodyExtent = bodyLayout.BodyExtent;
            result.BodyFrameBounds = ribbon.JoinBelowRibbonQuickAccess && !result.QuickAccessBounds.IsEmpty
                ? Rectangle.Union(result.BodyBounds, result.QuickAccessBounds) : result.BodyBounds;
            result.Groups.AddRange(bodyLayout.Groups);
            result.SimplifiedDividers.AddRange(bodyLayout.SimplifiedDividers);
            result.Targets.AddRange(bodyLayout.Targets.SelectMany(t => RibbonSplitLayout.Expand(t, dpi, false)));
        }
        var messages = RibbonMessageLayout.Measure(ribbon, ribbon.MessageWidth, dpi);
        if (messages.Height > 0)
        {
            int left = ribbon.JoinMessageBar ? Math.Min(client.Width, S(ribbon.Theme.BodyMargin.Left)) : 0;
            int top = client.Height - messages.Height - (ribbon.JoinMessageBar ? S(ribbon.Theme.BodyMargin.Bottom) : 0);
            result.MessageBounds = new(left, top, ribbon.MessageWidth, messages.Height);
            int index = 0;
            foreach (var measured in messages.Rows)
            {
                var row = RibbonMessageLayout.Offset(measured, left, top); result.Messages.Add(row); index++;
                if (!row.Action.IsEmpty) result.Targets.Add(new(TargetKind.MessageAction, row.Message.Action, row.Action, row.Bounds, RibbonItemSize.Medium, AutomaticKeyTip: $"M{index}A"));
                if (!row.Close.IsEmpty) result.Targets.Add(new(TargetKind.MessageClose, row.Message.Close, row.Close, row.Bounds, RibbonItemSize.Small, AutomaticKeyTip: $"M{index}X"));
            }
        }
        return result;
    }
    internal RibbonLayoutSnapshot BuildBody(RibbonControl ribbon, RibbonTab selected, int width, int bodyOffset, int top = 0)
    {
        if (ribbon.LayoutMode == RibbonLayoutMode.Simplified) return BuildSimplified(ribbon, selected, width, bodyOffset, top);
        var result = new RibbonLayoutSnapshot();
        int dpi = ribbon.DeviceDpi;
        int S(double n) => Scale(n, dpi);
        int bodyHeight = S(ribbon.LayoutTheme.BodyHeight);
        int scrollWidth = S(RibbonDensityMetrics.For(ribbon.Density).ScrollButton);
        var groups = ribbon.GetPresentedGroups(selected).Where(g => g.Visible).ToArray();
        var maps = groups.Select(g => RibbonQuickAccess.RetainsGroup(g) ? new[] { 3 } : !g.CanResize ? new[] { 0 } : g.ReductionMode switch { RibbonGroupReductionMode.Resize => new[] { 0, 1, 2 }, RibbonGroupReductionMode.ResizeThenCollapse => new[] { 0, 1, 2, 3 }, _ => new[] { 0, 3 } }).ToArray();
        var tables = groups.Select((g, i) => maps[i].Select(s => (double)Measure(g, s, ribbon.Font, dpi, ribbon.LayoutTheme).Width).ToArray()).ToArray();
        var order = groups.Select((g, i) => (g, i)).Where(p => p.g.CanResize && !RibbonQuickAccess.RetainsGroup(p.g))
            .OrderByDescending(p => p.g.ReductionPriority.HasValue).ThenByDescending(p => p.g.ReductionPriority ?? int.MinValue)
            .ThenByDescending(p => p.g.ReductionPriority.HasValue ? 0 : tables[p.i][0]).ThenByDescending(p => p.i).Select(p => p.i).ToArray();
        var margin = ribbon.LayoutTheme.BodyMargin;
        var body = new Rectangle(S(margin.Left), top + S(margin.Top),
            Math.Max(0, width - S(margin.Left) - S(margin.Right)), bodyHeight);
        result.BodyBounds = body;
        result.BodyFrameBounds = body;
        int inset = Math.Min(S(2), body.Width / 2);
        int available = Math.Max(0, body.Width - 2 * inset);
        int[] states = ReductionAlgorithm.ComputeStates(available, tables, order);
        int total = groups.Select((g, i) => (int)tables[i][states[i]]).Sum();
        bool overflow = total > available;
        if (overflow)
        {
            available = Math.Max(0, available - 2 * scrollWidth);
            states = ReductionAlgorithm.ComputeStates(available, tables, order);
            total = groups.Select((g, i) => (int)tables[i][states[i]]).Sum();
        }
        var viewport = new Rectangle(body.Left + inset + (overflow ? scrollWidth : 0), body.Top, available, bodyHeight);
        result.BodyViewport = viewport;
        result.BodyExtent = total;
        int x = viewport.Left - Math.Clamp(bodyOffset, 0, Math.Max(0, total - available));
        for (int i = 0; i < groups.Length; i++)
        {
            int state = maps[i][states[i]];
            var measure = Measure(groups[i], state, ribbon.Font, dpi, ribbon.LayoutTheme);
            var bounds = new Rectangle(x, body.Top, measure.Width, bodyHeight);
            var frame = bounds;
            if (state == 3) bounds = Rectangle.FromLTRB(bounds.Left + S(CollapsedGroupSideInset), bounds.Top + S(CommandTopInset),
                bounds.Right - GroupDividerWidth - S(CollapsedGroupSideInset), bounds.Bottom - S(CommandBottomInset));
            result.Groups.Add(new(groups[i], bounds, state == 3, frame));
            if (state == 3)
                result.Targets.Add(new(TargetKind.Group, groups[i], bounds, viewport));
            else
                foreach (var item in measure.Items)
                    result.Targets.Add(new(TargetKind.Item, item.Item, new(x + item.Bounds.X, body.Top + item.Bounds.Y, item.Bounds.Width, item.Bounds.Height), viewport, item.Size, item.ReserveIcon, LabelWidth: item.LabelWidth, GalleryDropDown: item.GalleryDropDown));
            x += measure.Width;
        }
        if (overflow)
        {
            int scrollInset = Math.Min(S(Math.Min(ribbon.LayoutTheme.BodyScrollButtonVerticalInset, ribbon.LayoutTheme.BodyHeight)), bodyHeight / 2);
            int scrollHeight = bodyHeight - 2 * scrollInset;
            result.Targets.Add(new(TargetKind.BodyBack, null, new(body.Left + inset, body.Top + scrollInset, scrollWidth, scrollHeight), body));
            result.Targets.Add(new(TargetKind.BodyNext, null, new(body.Right - inset - scrollWidth, body.Top + scrollInset, scrollWidth, scrollHeight), body));
        }
        return result;
    }
}
