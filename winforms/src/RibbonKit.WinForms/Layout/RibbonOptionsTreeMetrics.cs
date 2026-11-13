namespace RibbonKit.WinForms.Layout;

internal sealed record RibbonOptionsTreeDefaults(int CompactIndent, int TouchIndent, int CompactCheckOffset, int TouchCheckOffset, int IconOffset, int IconSize, int IconGap, int CompactCheck, int TouchCheck, int CompactChevron, int TouchChevron, float CompactStroke, float TouchStroke, int FocusInset);
internal readonly record struct RibbonOptionsTreeSlots(Rectangle Expand, Rectangle Check, Rectangle Icon, Rectangle Text);
internal readonly record struct RibbonOptionsTreeChevron(float Length, float Depth, float Stroke, bool Authored);
internal readonly record struct RibbonOptionsTreeMetrics(RibbonTheme Theme, int Dpi, RibbonDensity Density)
{
    private static RibbonOptionsTreeDefaults D => RibbonPresentationResolver.OptionsTree;
    private bool Touch => Density == RibbonDensity.Touch;
    private int S(int n) => RibbonLayoutEngine.Scale(n, Dpi);
    internal int Indent => S(Touch ? D.TouchIndent : D.CompactIndent);
    internal int CheckSide => S(Theme.OptionsAppearance?.TreeCheckSize ?? (Touch ? D.TouchCheck : D.CompactCheck));
    internal int ChevronSide => S(Theme.Chevrons?.Length ?? (Touch ? D.TouchChevron : D.CompactChevron));
    internal int Radius => S(Theme.OptionsAppearance?.TreeCornerRadius ?? 0);
    internal RibbonOptionsTreeSlots Resolve(Rectangle node, int level, int indent, int scroll, int rowHeight, int width, bool check)
    {
        var a = Theme.OptionsAppearance; var expand = new Rectangle(level * indent - scroll, node.Top, indent, rowHeight);
        int textLeft = Math.Max(node.Left, expand.Right + S(check ? Touch ? D.TouchCheckOffset : D.CompactCheckOffset : D.IconOffset));
        var checkSlot = check ? Rectangle.FromLTRB(expand.Right, node.Top, textLeft, node.Top + rowHeight) : Rectangle.Empty;
        int side = S(a?.TreeIconSize ?? D.IconSize), gap = S(a?.TreeIconTextGap ?? D.IconGap), end = textLeft;
        if (a?.TreeIconSize is not null || a?.TreeIconTextGap is not null)
        {
            int start = Math.Max(0, expand.Right); end = Math.Min(textLeft, width); int available = Math.Max(0, end - start);
            gap = Math.Min(gap, available); side = Math.Min(side, Math.Max(0, Math.Min(rowHeight, available - gap)));
        }
        var icon = side > 0 ? new Rectangle(end - gap - side, node.Top + (rowHeight - side) / 2, side, side) : Rectangle.Empty;
        return new(expand, checkSlot, icon, new(textLeft, node.Top, Math.Max(0, width - textLeft), rowHeight));
    }
    internal Rectangle CheckBounds(Rectangle slot)
    {
        int side = Math.Min(CheckSide, Math.Max(0, slot.Width - S(D.IconGap)));
        if (Theme.OptionsAppearance?.TreeCheckSize is not null) side = Math.Min(side, Math.Max(0, slot.Height));
        return side > 0 ? new(slot.Left + (slot.Width - side) / 2, slot.Top + (slot.Height - side) / 2, side, side) : Rectangle.Empty;
    }
    internal Rectangle FocusBounds(Rectangle row)
    {
        int inset = Theme.OptionsAppearance?.TreeFocusInset is { } n ? Math.Min(S(n), Math.Max(0, (Math.Min(row.Width, row.Height) - 1) / 2)) : D.FocusInset;
        return Rectangle.Inflate(row, -inset, -inset);
    }
    internal RibbonOptionsTreeChevron Chevron(Rectangle slot, bool expanded)
    {
        var a = Theme.Chevrons; int legacy = S(Touch ? D.TouchChevron : D.CompactChevron);
        float length = a?.Length is { } n ? S(n) : legacy, depth = a?.Depth is { } d ? S(d) : legacy / 2f;
        float stroke = a?.StrokeWidth is { } s ? (float)(s * Dpi / 96d) : Math.Max(1, Dpi / 96f * (Touch ? D.TouchStroke : D.CompactStroke));
        bool authored = a is { Length: not null } or { Depth: not null } or { StrokeWidth: not null };
        if (authored) { length = Math.Min(length, Math.Max(0, (expanded ? slot.Width : slot.Height) - stroke)); depth = Math.Min(depth, Math.Max(0, (expanded ? slot.Height : slot.Width) - stroke)); }
        return new(length, depth, stroke, authored);
    }
}
