namespace RibbonKit.WinForms.Layout;

internal sealed partial class RibbonLayoutEngine
{
    private static RibbonLayoutSnapshot BuildSimplified(RibbonControl ribbon, RibbonTab tab, int width, int bodyOffset, int top)
    {
        int S(double value) => Scale(value, ribbon.DeviceDpi);
        var metrics = RibbonDensityMetrics.For(ribbon.Density); var theme = ribbon.LayoutTheme; var margin = theme.BodyMargin;
        var result = new RibbonLayoutSnapshot();
        var body = new Rectangle(S(margin.Left), top + S(margin.Top), Math.Max(0, width - S(margin.Horizontal)), S(theme.BodyHeight));
        result.BodyBounds = result.BodyFrameBounds = body;
        var groups = ribbon.GetPresentedGroups(tab).Where(g => g.Visible).ToArray();
        var selected = new List<(RibbonItem? Item, RibbonGroup Group, string Id, RibbonItemSize Size, int Priority)>();
        var collapsed = new HashSet<RibbonGroup>();
        foreach (var pair in ribbon.Simplified.Selected(tab))
        {
            var group = pair.Command.Group;
            if (ribbon.Popups.BorrowsGroup(group))
            {
                if (collapsed.Add(group)) selected.Add((null, group, group.Id, RibbonItemSize.Medium, int.MaxValue));
            }
            else selected.Add((pair.Command.Item, group, pair.Command.Id, pair.Size, pair.Command.Item.SimplifiedPriority));
        }
        var measured = selected.Select(pair =>
        {
            var item = pair.Item; int size;
            if (item is null) size = Math.Max(S(metrics.MediumWidth), TextRenderer.MeasureText(pair.Group.DisplayText, ribbon.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width
                + 2 * S(6) + S(16) + (pair.Group.Icon is null ? 0 : S(24) + S(6)));
            else if (item is RibbonComboBoxItem combo) size = S(combo.InputWidth + 6) + (pair.Size == RibbonItemSize.Medium ? RibbonEditorLayout.LabelWidth(item, ribbon.Font, ribbon.DeviceDpi) : 0);
            else
            {
                int label = pair.Size == RibbonItemSize.Small ? 0 : TextRenderer.MeasureText(item.Text, ribbon.Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine).Width;
                int icon = item.Icon is not null || item.Image is not null || item.LargeImage is not null || item is RibbonCheckBoxItem or RibbonRadioButtonItem ? S(24) : 0;
                size = Math.Max(S(metrics.SmallWidth), label + icon + S(12));
                if (item is RibbonDropDownItem) size += S(item is RibbonSplitButtonItem ? metrics.Touch ? metrics.HorizontalSplitArrow : metrics.SplitArrow : metrics.Touch ? 28 : 16);
            }
            return new SimplifiedCommandMeasurement(pair.Id, pair.Group.Id, size, pair.Priority);
        }).ToArray();
        int inset = S(4), available = Math.Max(0, body.Width - 2 * inset), moreWidth = S(metrics.SimplifiedMore), gap = S(metrics.SimplifiedGap), divider = S(metrics.SimplifiedDivider);
        var plan = RibbonSimplifiedSelection.Compute(measured, groups.Select(g => g.Id).ToArray(), available, moreWidth, gap, divider);
        int scroll = plan.RequiresScrolling ? Math.Min(S(metrics.ScrollButton), Math.Max(0, (available - S(16)) / 2)) : 0;
        if (scroll > 0) { available = Math.Max(0, available - 2 * scroll); plan = RibbonSimplifiedSelection.Compute(measured, groups.Select(g => g.Id).ToArray(), available, moreWidth, gap, divider); }
        result.BodyViewport = new(body.Left + inset + scroll, body.Top, available, body.Height);
        result.BodyExtent = (int)Math.Min(int.MaxValue, plan.Extent);
        int x = result.BodyViewport.Left - Math.Clamp(bodyOffset, 0, Math.Max(0, result.BodyExtent - available)), clearance = S(metrics.SimplifiedClearance);
        string? previousGroup = null;
        foreach (int index in plan.Inline)
        {
            var pair = selected[index]; var item = pair.Item;
            if (previousGroup is not null)
            {
                if (previousGroup != pair.Group.Id) { result.SimplifiedDividers.Add(new(x + divider / 2, body.Top + S(6), 1, Math.Max(0, body.Height - S(12)))); x += divider; }
                else x += gap;
            }
            var bounds = new Rectangle(x, body.Top + clearance, measured[index].Width, Math.Max(0, body.Height - 2 * clearance));
            int labelWidth = item is RibbonComboBoxItem && pair.Size == RibbonItemSize.Medium ? RibbonEditorLayout.LabelWidth(item, ribbon.Font, ribbon.DeviceDpi) : 0;
            if (item is null) { result.Groups.Add(new(pair.Group, bounds, true, bounds)); result.Targets.Add(new(TargetKind.Group, pair.Group, bounds, result.BodyViewport, RibbonItemSize.Medium)); }
            else result.Targets.Add(new(TargetKind.Item, item, bounds, result.BodyViewport, pair.Size, LabelWidth: labelWidth, GalleryDropDown: item is RibbonGalleryItem));
            x += bounds.Width; previousGroup = pair.Group.Id;
        }
        ribbon.Simplified.SetMore(plan.Overflow.Select(i => selected[i].Item).OfType<RibbonItem>(), groups);
        if (groups.Length > 0) result.Targets.Add(new(TargetKind.QuickAccessOverflow, ribbon.Simplified.More, new(Math.Max(x, result.BodyViewport.Right - moreWidth), body.Top + clearance, moreWidth, Math.Max(0, body.Height - 2 * clearance)), result.BodyViewport));
        if (scroll > 0)
        {
            result.Targets.Add(new(TargetKind.BodyBack, null, new(body.Left + inset, body.Top, scroll, body.Height), body));
            result.Targets.Add(new(TargetKind.BodyNext, null, new(body.Right - inset - scroll, body.Top, scroll, body.Height), body));
        }
        return result;
    }
}
